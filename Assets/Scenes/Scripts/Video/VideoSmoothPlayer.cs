using System.Collections;
using UnityEngine;
using UnityEngine.Video;

/// 進まない・カクつく を自動で回避する再生器：
/// - 音声トラックがあるなら「音基準」、無ければ「GameTime」へ自動選択
/// - 準備→最初のフレーム→再生を確実化（失敗時は再キック）
/// - 進捗が止まった/重くて詰まる時はフォールバック＋skipOnDrop切替で自己復旧
[DisallowMultipleComponent]
public class VideoSmoothPlayer : MonoBehaviour
{
    [Header("Refs")]
    public VideoPlayer vp;            // Video Player（必須）
    public AudioSource vAudio;        // 動画の音（任意：無ければnullでOK）
    public RenderTexture targetRT;    // 使っているRenderTexture（任意・監視のみ）

    [Header("Playback Options")]
    [Tooltip("最初はフレーム落とし無しで丁寧に再生。詰まったら自動でONになります。")]
    public bool startSkipOnDrop = false;
    [Tooltip("少なくともこの秒数は再生を維持（再キックの頻度を抑える）")]
    public float minRunSeconds = 0.20f;
    [Tooltip("進捗が止まっているとみなす閾値（秒）")]
    public float stallDetectSeconds = 0.35f;

    [Header("System Hints (任意)")]
    [Tooltip("0なら変更しない。60などに設定すると起動時に targetFrameRate を固定")]
    public int forceTargetFps = 60;
    [Tooltip("trueで起動時に vSyncCount=1 にします（CPU/GPUの無駄を抑える）")]
    public bool forceVSync = true;

    double lastTime;
    long   lastFrame;

    void Reset()
    {
        vp     = GetComponent<VideoPlayer>();
        vAudio = GetComponent<AudioSource>() ?? GetComponentInChildren<AudioSource>(true);
    }

    void Awake()
    {
        if (!vp) vp = GetComponent<VideoPlayer>();
        if (!vp) { enabled = false; return; }

        if (forceTargetFps > 0) Application.targetFrameRate = forceTargetFps;
        if (forceVSync) QualitySettings.vSyncCount = 1;

        // 基本設定
        vp.playOnAwake = false;
        vp.isLooping   = false;
        vp.skipOnDrop  = startSkipOnDrop;
        vp.playbackSpeed = 1f;

        // 音の配線（あれば）
        if (vAudio)
        {
            vAudio.pitch = 1f;
            vAudio.spatialBlend = 0f;          // 2D
            vAudio.mute = false;
            if (vAudio.volume <= 0f) vAudio.volume = 1f;

            vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
            try { vp.SetTargetAudioSource(0, vAudio); } catch {}
        }

        // 時間モード：音が使える時だけ音基準
        bool canAudioClock = (vp.audioTrackCount > 0) && (vAudio != null);
        SetTimeMode(canAudioClock);
    }

    void OnEnable() { StartCoroutine(PlayRoutine()); }

    IEnumerator PlayRoutine()
    {
        // Prepare → 最初のフレーム → 再生
        bool prepared = false;
        VideoPlayer.EventHandler onPrep = _ => prepared = true;
        vp.prepareCompleted += onPrep;
        vp.Prepare();

        float guard = 1.0f;
        while (!prepared && guard > 0f) { guard -= Time.unscaledDeltaTime; yield return null; }
        vp.prepareCompleted -= onPrep;

        vp.frame = 0;
        vp.Play();
        if (vAudio) vAudio.Play();

        // 進捗監視 & 自己復旧ループ
        float minRun = minRunSeconds;
        lastTime = vp.time;
        lastFrame = vp.frame;

        while (enabled)
        {
            // RTサイズと動画サイズが大きく乖離していたらデコード負荷の可能性
            // → skipOnDrop をONにして耐える（1度だけ切替）
            if (!vp.skipOnDrop && targetRT && vp.texture)
            {
                if (targetRT.width > vp.texture.width * 1.2f || targetRT.height > vp.texture.height * 1.2f)
                    vp.skipOnDrop = true; // 大きなスケールアップは重い
            }

            // 進捗チェック
            if (minRun > 0f) minRun -= Time.unscaledDeltaTime;
            else
            {
                bool noProgress =
                    (!vp.isPrepared) ||
                    ((vp.frame <= lastFrame) && (vp.time <= lastTime + 1e-4));

                if (noProgress)
                {
                    // フォールバック：時間モードをGameTime側に、skipOnDropをONにして再キック
                    SetTimeMode(false);
                    vp.skipOnDrop = true;
                    vp.Stop();
                    vp.frame = 0;
                    vp.Play();
                    if (vAudio) vAudio.Play();

                    // リセット
                    minRun = minRunSeconds;
                }
            }

            lastFrame = vp.frame;
            lastTime  = vp.time;

            // ストール検出の待機
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, stallDetectSeconds * 0.2f));
        }
    }

    // preferAudio=true なら AudioDSP/AudioTime を優先、無ければ GameTime/Unscaled に落とす
    void SetTimeMode(bool preferAudio)
    {
        try
        {
            var tum = typeof(VideoPlayer).GetProperty("timeUpdateMode");
            if (tum != null)
            {
                var et = tum.PropertyType;
                object val;
                if (preferAudio &&
                    (TryEnum(et, "AudioDSPTime", out val) || TryEnum(et, "AudioTime", out val)))
                { tum.SetValue(vp, val, null); return; }

                if (TryEnum(et, "GameTime", out val) || TryEnum(et, "UnscaledGameTime", out val))
                { tum.SetValue(vp, val, null); return; }
            }
            var tr = typeof(VideoPlayer).GetProperty("timeReference");
            if (tr != null)
            {
                var et = tr.PropertyType;
                object val;
                if (preferAudio &&
                    (TryEnum(et, "AudioDSPTime", out val) || TryEnum(et, "AudioTime", out val)))
                { tr.SetValue(vp, val, null); return; }

                if (TryEnum(et, "Freerun", out val))
                { tr.SetValue(vp, val, null); return; }
            }
        }
        catch { /* 何もできなければ既定のまま */ }
    }

    static bool TryEnum(System.Type enumType, string name, out object value)
    {
        value = null;
        if (enumType == null || !enumType.IsEnum) return false;
        foreach (var n in System.Enum.GetNames(enumType))
            if (n == name) { value = System.Enum.Parse(enumType, name); return true; }
        return false;
    }
}
