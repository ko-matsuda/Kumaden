// Assets/Scripts/CookingResultSequence.cs
// これを1ファイルまるごとコピペして使うだけでOK。
// シーン内の InterstitialCanvas（動画用）→ 再生 → 終了後に ResultCanvas を表示します。
// F6で手動テスト可能。ゲーム終了時は StartSequence() を呼ぶだけ。

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CookingResultSequence : MonoBehaviour
{
    [Header("Scene Object Names (そのままでOK)")]
    public string interstitialCanvasName = "InterstitialCanvas";
    public string resultCanvasName = "ResultCanvas";
    public string prologueCanvasName = "PrologueCanvas";

    [Header("Clip / RenderTexture")]
    public VideoClip cookingClip;            // result.mp4 を割り当て（必須）
    public RenderTexture targetRT;           // VideoRT_1080x1920 を割り当て（必須）

    [Header("Test")]
    public KeyCode testHotkey = KeyCode.F6;  // 再生テスト：F6

    // 内部参照
    Canvas interstitialCanvas;
    CanvasGroup cg;
    RawImage screen;
    VideoPlayer vp;
    AudioSource videoAudio;

    bool running;

    void Awake()
    {
        // InterstitialCanvas を探す
        var ic = GameObject.Find(interstitialCanvasName);
        if (ic == null) { Debug.LogError("[CRS] InterstitialCanvas が見つかりません"); return; }

        interstitialCanvas = ic.GetComponent<Canvas>();
        if (interstitialCanvas == null) interstitialCanvas = ic.AddComponent<Canvas>();
        interstitialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        interstitialCanvas.sortingOrder = 999; // 最前面

        // CanvasGroup
        cg = ic.GetComponent<CanvasGroup>();
        if (cg == null) cg = ic.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // RawImage（VideoScreen）
        screen = ic.GetComponentInChildren<RawImage>(true);
        if (screen == null)
        {
            var go = new GameObject("VideoScreen", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(ic.transform, false);
            screen = go.GetComponent<RawImage>();
            var rt = screen.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        screen.color = Color.white; // 黒だと映らないので必ず白

        // VideoPlayer
        vp = ic.GetComponent<VideoPlayer>();
        if (vp == null) vp = ic.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.renderMode = VideoRenderMode.RenderTexture;

        // AudioSource（動画の音用）
        videoAudio = ic.GetComponent<AudioSource>();
        if (videoAudio == null) videoAudio = ic.AddComponent<AudioSource>();
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.SetTargetAudioSource(0, videoAudio);

        // RT配線（必須）
        if (targetRT != null)
        {
            vp.targetTexture = targetRT;
            screen.texture = targetRT;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(testHotkey))
        {
            StartSequence();
        }
    }

    // ゲーム終了時にこれを呼ぶだけ
    public void StartSequence()
    {
        if (running) return;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        running = true;

        // 事前停止：他のVideoPlayer・プロローグなど
        StopOthersAndHidePrologue();

        // クリップ必須
        if (cookingClip == null) { Debug.LogError("[CRS] cookingClip が未設定"); running = false; yield break; }
        if (targetRT == null)   { Debug.LogError("[CRS] targetRT が未設定");   running = false; yield break; }

        // リザルトは一旦消す
        var result = GameObject.Find(resultCanvasName);
        if (result) result.SetActive(false);

        // 準備
        interstitialCanvas.sortingOrder = 999;
        cg.alpha = 1f;
        screen.enabled = true;

        vp.clip = cookingClip;
        vp.Prepare();
        while (!vp.isPrepared) yield return null;

        // 再生
        bool ended = false;
        vp.errorReceived += (_, e) => Debug.LogError("[CRS] Video ERROR: " + e);
        vp.loopPointReached += _ => ended = true;

        vp.Play();
        videoAudio.Play();

        while (!ended && vp.isPlaying) yield return null;

        // 後処理
        vp.Stop();
        videoAudio.Stop();
        cg.alpha = 0f;
        screen.enabled = false;

        // リザルト表示
        if (result) result.SetActive(true);

        running = false;
    }

    void StopOthersAndHidePrologue()
    {
        // 他の VideoPlayer を止める
        foreach (var other in FindObjectsOfType<VideoPlayer>())
        {
            if (vp != null && other == vp) continue;
            other.Stop();
        }
        // プロローグCanvas を隠す
        var prologue = GameObject.Find(prologueCanvasName);
        if (prologue) prologue.SetActive(false);
    }
}
