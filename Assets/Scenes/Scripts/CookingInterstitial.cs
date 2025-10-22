using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CookingInterstitial : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup canvasGroup;     // InterstitialCanvas
    public RawImage videoScreen;        // VideoScreen (RawImage)
    public VideoPlayer videoPlayer;     // VideoPlayer on the same canvas
    public AudioSource videoAudio;      // AudioSource for video
    public AudioSource bgmAudio;        // Optional: current BGM

    [Header("Clip / RT")]
    public VideoClip cookingClip;       // result.mp4 (or .webm on WebGL)
    public RenderTexture targetRT;      // VideoRT_1080x1920

    [Header("Options")]
    public float fadeDur = 0.25f;
    public bool skippable = true;
    public float minShowTime = 0.70f;   // avoid instant skip
    public int inputIgnoreFrames = 3;   // プレイ直後の入力を無視

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!videoPlayer) videoPlayer = GetComponent<VideoPlayer>();
        Debug.Log("[Interstitial] Awake");

        // 接続の明示（念のため再設定）
        if (targetRT)
        {
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = targetRT;
            if (videoScreen) videoScreen.texture = targetRT;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        if (videoAudio) videoPlayer.SetTargetAudioSource(0, videoAudio);

        // 初期は見えない
        if (canvasGroup) canvasGroup.alpha = 0f;
        if (videoScreen) videoScreen.enabled = false;
    }

    public void Play(Action onComplete)
    {
        Debug.Log("[Interstitial] Play() called");
        StartCoroutine(PlayRoutine(onComplete));
    }

    IEnumerator PlayRoutine(Action onComplete)
    {
        if (!cookingClip)
        {
            Debug.LogWarning("[Interstitial] cookingClip is NULL. Skip interstitial.");
            onComplete?.Invoke();
            yield break;
        }

        // 直前の入力を捨てる（即スキップ防止）
        for (int i = 0; i < inputIgnoreFrames; i++) yield return null;

        // BGMデュック
        StartCoroutine(DuckBGM(true));

        // 準備
        videoPlayer.clip = cookingClip;
        videoPlayer.Prepare();
        Debug.Log("[Interstitial] Preparing video...");
        while (!videoPlayer.isPrepared) yield return null;
        Debug.Log("[Interstitial] Prepared.");

        if (videoScreen) videoScreen.enabled = true;

        // 最前面に（隠れ対策）
        var canvas = canvasGroup ? canvasGroup.GetComponent<Canvas>() : GetComponent<Canvas>();
        if (canvas) canvas.sortingOrder = 999;

        // フェードイン
        yield return Fade(0f, 1f, fadeDur);

        // 再生
        videoPlayer.Play();
        if (videoAudio) videoAudio.Play();
        Debug.Log("[Interstitial] Playing... length=" + videoPlayer.length + "s");

        float startTime = Time.time;
        bool finished = false;
        videoPlayer.loopPointReached += _ => finished = true;

        while (!finished)
        {
            if (skippable && (Time.time - startTime) > minShowTime && Input.anyKeyDown)
            {
                Debug.Log("[Interstitial] Skipped by input.");
                break;
            }
            // 動画が意図せず停止してたら中断
            if (!videoPlayer.isPlaying && (Time.time - startTime) > 0.2f)
            {
                Debug.LogWarning("[Interstitial] Video stopped unexpectedly.");
                break;
            }
            yield return null;
        }

        // フェードアウト
        yield return Fade(1f, 0f, fadeDur);

        videoPlayer.Stop();
        if (videoAudio) videoAudio.Stop();
        if (videoScreen) videoScreen.enabled = false;

        StartCoroutine(DuckBGM(false));

        onComplete?.Invoke();
    }

    IEnumerator Fade(float a, float b, float t)
    {
        if (!canvasGroup) yield break;
        for (float e = 0; e < t; e += Time.unscaledDeltaTime)
        {
            canvasGroup.alpha = Mathf.Lerp(a, b, e / t);
            yield return null;
        }
        canvasGroup.alpha = b;
    }

    IEnumerator DuckBGM(bool on)
    {
        if (!bgmAudio) yield break;
        float from = bgmAudio.volume;
        float to = on ? 0.25f : 0.8f; // 調整OK
        float t = 0.2f;
        for (float e = 0; e < t; e += Time.unscaledDeltaTime)
        {
            bgmAudio.volume = Mathf.Lerp(from, to, e / t);
            yield return null;
        }
        bgmAudio.volume = to;
    }
}
