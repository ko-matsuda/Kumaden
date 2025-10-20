using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoSmoothPlayer : MonoBehaviour
{
    [Header("必須")]
    public VideoPlayer videoPlayer;         // 同じGameObjectに付けてもOK
    public RawImage targetImage;            // 画面に表示するRawImage（UI）
    public Vector2Int targetResolution = new Vector2Int(1920, 1080);

    [Header("任意: 再生中だけOFFにする重いオブジェクト")]
    public GameObject[] heavyObjects;       // PostProcessのVolume等を入れる

    [Header("任意: ファイル名（StreamingAssets直下）")]
    public string fileName = "Prologue.mp4";

    private RenderTexture rt;
    private bool originalVsyncSaved;
    private int originalVsyncCount;

    private void Reset()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        targetImage = FindObjectOfType<RawImage>();
    }

    private void Awake()
    {
        if (!videoPlayer) videoPlayer = GetComponent<VideoPlayer>();
        if (!videoPlayer) { Debug.LogError("[VideoSmoothPlayer] VideoPlayer が見つかりません"); enabled = false; return; }

        // VideoPlayer の推奨設定を強制
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource; // 音不要なら None に変更可

        // URL を StreamingAssets から自動設定（必ず file:// を付与）
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        if (!File.Exists(path)) Debug.LogWarning($"[VideoSmoothPlayer] ファイルが見つかりません: {path}");
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        videoPlayer.url = "file:///" + path.Replace("\\", "/");
#else
        videoPlayer.url = "file://" + path;
#endif

        // RenderTexture を実解像度で用意
        if (rt == null || rt.width != targetResolution.x || rt.height != targetResolution.y)
        {
            rt = new RenderTexture(targetResolution.x, targetResolution.y, 0, RenderTextureFormat.ARGB32);
            rt.useMipMap = false;
            rt.autoGenerateMips = false;
            rt.Create();
        }
        videoPlayer.targetTexture = rt;
        if (targetImage) targetImage.texture = rt;
    }

    private void OnEnable()
    {
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        // 再生中はVSyncを安定側に（必要に応じてコメントアウト可）
        originalVsyncCount = QualitySettings.vSyncCount;
        QualitySettings.vSyncCount = 1;

        // 重い物をOFF
        SetHeavyObjectsActive(false);

        videoPlayer.Prepare();
        // 完全ロード待ち
        while (!videoPlayer.isPrepared) yield return null;

        // 最初のフレームが描画可能になるまで少し待つ
        yield return null;

        videoPlayer.Play();

        // 再生完了待ち
        while (videoPlayer.isPlaying) yield return null;

        // 後片付け
        SetHeavyObjectsActive(true);
        QualitySettings.vSyncCount = originalVsyncCount;
    }

    private void OnDisable()
    {
        if (videoPlayer) videoPlayer.Stop();
        SetHeavyObjectsActive(true);
        QualitySettings.vSyncCount = originalVsyncCount;
    }

    private void OnDestroy()
    {
        if (rt != null)
        {
            if (videoPlayer) videoPlayer.targetTexture = null;
            rt.Release();
            Destroy(rt);
        }
    }

    private void SetHeavyObjectsActive(bool active)
    {
        if (heavyObjects == null) return;
        foreach (var go in heavyObjects)
            if (go) go.SetActive(active);
    }
}
