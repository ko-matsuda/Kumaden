// Assets/Scripts/CookingResultSequence.cs
// InterstitialCanvas にアタッチ。動画 → 終了後に ResultCanvas を表示＆BGM復帰までを一括管理。

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CookingResultSequence : MonoBehaviour
{
    [Header("必須アサイン")]
    public VideoClip cookingClip;          // result.mp4 をドラッグ
    public RenderTexture targetRT;         // VideoRT_1080x1920 をドラッグ
    public GameObject resultCanvas;        // ResultCanvas をドラッグ
    public AudioSource resultBGM;          // （任意）リザルトBGMのAudioSource

    // 内部参照（自動取得）
    Canvas canvasRef;
    CanvasGroup cg;
    RawImage screen;
    VideoPlayer vp;
    AudioSource videoAudio;

    bool running;

    void Awake()
    {
        // コンポーネント確保
        canvasRef = GetComponent<Canvas>();           if (!canvasRef)   canvasRef   = gameObject.AddComponent<Canvas>();
        cg        = GetComponent<CanvasGroup>();      if (!cg)          cg          = gameObject.AddComponent<CanvasGroup>();
        vp        = GetComponent<VideoPlayer>();      if (!vp)          vp          = gameObject.AddComponent<VideoPlayer>();
        videoAudio= GetComponent<AudioSource>();      if (!videoAudio)  videoAudio  = gameObject.AddComponent<AudioSource>();
        screen    = GetComponentInChildren<RawImage>(true);
        if (!screen)
        {
            var go = new GameObject("VideoScreen", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(transform, false);
            screen = go.GetComponent<RawImage>();
            var rt = screen.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        // 表示設定
        canvasRef.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasRef.sortingOrder = 999;      // 最前面
        cg.alpha = 0f;                     // ふだんは非表示
        screen.color = Color.white;        // 黒だと映りません

        // VideoPlayer 設定
        vp.playOnAwake = false; vp.isLooping = false;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.SetTargetAudioSource(0, videoAudio);

        // RT配線
        if (targetRT) { vp.targetTexture = targetRT; screen.texture = targetRT; }
    }

    public void StartSequence()
    {
        if (running) return;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        running = true;

        // 必須チェック
        if (!cookingClip || !targetRT || !resultCanvas)
        {
            Debug.LogError("[CRS] 必須参照が未設定 (cookingClip / targetRT / resultCanvas)");
            running = false; yield break;
        }

        // 他Video停止（重複再生防止）
        foreach (var other in FindObjectsOfType<VideoPlayer>())
            if (other != vp) other.Stop();

        // Result は一旦隠す
        resultCanvas.SetActive(false);

        // リザルトBGMを一時ミュート
        float prevVol = -1f;
        if (resultBGM) { prevVol = resultBGM.volume; resultBGM.volume = 0f; }

        // 画を出す準備
        cg.alpha = 1f; screen.enabled = true;
        vp.clip = cookingClip;
        vp.Prepare(); while (!vp.isPrepared) yield return null;

        // 再生
        bool ended = false;
        vp.loopPointReached += _ => ended = true;
        vp.errorReceived += (_, e) => Debug.LogError("[CRS] Video ERROR: " + e);

        vp.Play(); videoAudio.Play();
        while (!ended && vp.isPlaying) yield return null;

        // 片付け
        vp.Stop(); videoAudio.Stop();
        cg.alpha = 0f; screen.enabled = false;

        // BGM復帰 → Result表示
        if (resultBGM && prevVol >= 0f) resultBGM.volume = prevVol;
        resultCanvas.SetActive(true);

        running = false;
    }
}
