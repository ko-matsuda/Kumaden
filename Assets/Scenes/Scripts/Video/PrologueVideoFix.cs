using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class PrologueVideoFix : MonoBehaviour
{
    public VideoPlayer vp;     // プロローグ用 VideoPlayer
    public AudioSource vAudio; // その音（任意：無くてもOK）

    void Reset()
    {
        vp = GetComponent<VideoPlayer>();
        vAudio = GetComponent<AudioSource>() ?? GetComponentInChildren<AudioSource>(true);
    }

    void Awake()
    {
        if (!vp) vp = GetComponent<VideoPlayer>();
        if (!vp) return;

        // 安全な基本設定
        vp.playbackSpeed = 1f;
        vp.skipOnDrop    = false;  // コマ飛ばしで速く見える事故を防ぐ
        vp.isLooping     = false;
        vp.playOnAwake   = false;

        // オーディオ配線（あれば）
        if (vAudio)
        {
            vAudio.pitch        = 1f;
            vAudio.spatialBlend = 0f;    // 2D
            vAudio.mute         = false;
            if (vAudio.volume <= 0f) vAudio.volume = 1f;

            vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
            try { vp.SetTargetAudioSource(0, vAudio); } catch {}
        }

        // ★時間モードを“音が使えるときだけ音基準”、そうでなければGameTimeへ
        bool canUseAudioClock = (vp.audioTrackCount > 0) && vAudio != null;
        SetTimeMode(preferAudioClock: canUseAudioClock);
    }

    IEnumerator Start()
    {
        if (!vp) yield break;

        // 確実に Prepare → 最初のフレーム → 再生
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

        // ★進まなければ自動フォールバック（AudioClock → GameTime）して再キック
        yield return StartCoroutine(EnsureProgressOrFallback());
    }

    // —— ここからヘルパ —— //

    IEnumerator EnsureProgressOrFallback()
    {
        if (!vp) yield break;

        // 0.35秒ほど進捗監視（frame or time が増えればOK）
        double t0 = vp.time;
        long   f0 = vp.frame;
        float  w  = 0.35f;

        while (w > 0f)
        {
            if ((vp.isPrepared && vp.frame > f0) || (vp.time > t0 + 1e-4)) yield break;
            w -= Time.unscaledDeltaTime;
            yield return null;
        }

        // 進んでいない → 時間モードを GameTime 系に強制してリスタート
        SetTimeMode(preferAudioClock: false);

        vp.Stop();
        vp.frame = 0;
        vp.Play();
        if (vAudio) vAudio.Play();
    }

    // preferAudioClock=true の場合は AudioDSP/AudioTime を優先設定。存在しなければ GameTime/Unscaled に落とす
    void SetTimeMode(bool preferAudioClock)
    {
        try
        {
            // 新API: timeUpdateMode (enum VideoTimeUpdateMode)
            var tumProp = typeof(VideoPlayer).GetProperty("timeUpdateMode");
            if (tumProp != null)
            {
                var et = tumProp.PropertyType;
                object val;
                if (preferAudioClock &&
                    (TryEnum(et, "AudioDSPTime", out val) || TryEnum(et, "AudioTime", out val)))
                { tumProp.SetValue(vp, val, null); return; }

                // 非オーディオ系（確実に進む）
                if (TryEnum(et, "GameTime", out val) || TryEnum(et, "UnscaledGameTime", out val))
                { tumProp.SetValue(vp, val, null); return; }
            }

            // 旧API: timeReference (enum VideoTimeReference)
            var trProp = typeof(VideoPlayer).GetProperty("timeReference");
            if (trProp != null)
            {
                var et = trProp.PropertyType;
                object val;
                if (preferAudioClock &&
                    (TryEnum(et, "AudioDSPTime", out val) || TryEnum(et, "AudioTime", out val)))
                { trProp.SetValue(vp, val, null); return; }

                // フォールバック（Freerun は実装差があるが止まるよりマシ）
                if (TryEnum(et, "Freerun", out val))
                { trProp.SetValue(vp, val, null); return; }
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
