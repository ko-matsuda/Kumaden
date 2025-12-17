using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// 曲の秒数 or AudioSource で発火 → 黒 → 料理動画 → 黒の上にリザルト(+ジングル再生)
[DisallowMultipleComponent]
public class CookingResultSequence : MonoBehaviour
{
    public enum TriggerMode { ByFixedSeconds, ByAudioSource, External }

    [Header("Trigger")]
    public TriggerMode trigger = TriggerMode.ByFixedSeconds;

    [Tooltip("TriggerMode=ByFixedSeconds のとき：曲の総尺（秒）")]
    public float totalSongSeconds = 120f;

    [Tooltip("発火オフセット（秒）。終端ぴったり=0、少し後=+, 少し前=-")]
    public float fireOffsetSeconds = 0f;

    [Tooltip("TriggerMode=ByAudioSource のとき：インゲームBGMの AudioSource（ループOFF想定）")]
    public AudioSource gameMusic;

    [Header("Refs (必ずアサイン)")]
    public CanvasGroup fadeGroup;          // FadeCanvas の CanvasGroup（初期 alpha=0）
    public CanvasGroup interstitialGroup;  // InterstitialCanvas の CanvasGroup（初期 alpha=0）
    public RawImage    videoImage;         // VideoScreen (RawImage, texture=VideoRT)
    public VideoPlayer videoPlayer;        // VideoPlayer (TargetTexture=VideoRT, Loop/PlayOnAwake=OFF)
    public AudioSource videoAudio;         // 動画音 (2D, Volume=1)
    public GameObject  resultRoot;         // ResultCanvas（開始時 OFF 推奨）

    [Header("Result Jingle")]
    [Tooltip("未指定なら resultRoot から自動検出")]
    public AudioSource jingleSource;       // ★リザルト用ジングル

    [Header("Timings / Guards")]
    [Tooltip("動画Prepare待ちタイムアウト（秒）")]
    public float prepareTimeout = 1.0f;
    [Tooltip("各遷移の小休止（秒）")]
    public float settleWait = 0.05f;

    bool running;
    bool initialized;
    double scheduledFireDsp = -1;

    // -------------------- 初期化 --------------------
    void Awake()
    {
        // 黒は通常時は完全透明
        if (fadeGroup)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable   = false;
            var cv = fadeGroup.GetComponent<Canvas>();
            if (cv) { cv.overrideSorting = true; cv.sortingOrder = 9000; }
        }
        // 動画Canvasは非表示
        if (interstitialGroup)
        {
            interstitialGroup.alpha = 0f;
            interstitialGroup.blocksRaycasts = false;
            interstitialGroup.interactable   = false;
            var cv = interstitialGroup.GetComponent<Canvas>();
            if (cv) { cv.overrideSorting = true; cv.sortingOrder = 10000; }
        }
        if (videoImage) videoImage.enabled = false;

        // リザルトは非表示（最前面）
        if (resultRoot)
        {
            resultRoot.SetActive(false);
            var cv = resultRoot.GetComponent<Canvas>();
            if (cv) { cv.overrideSorting = true; cv.sortingOrder = 10001; }
        }

        // 動画オーディオの保険
        if (videoPlayer)
        {
            videoPlayer.isLooping   = false;
            videoPlayer.playOnAwake = false;
            if (videoAudio)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                try { videoPlayer.SetTargetAudioSource(0, videoAudio); } catch {}
                videoAudio.mute = false; videoAudio.volume = 1f; videoAudio.spatialBlend = 0f;
            }
        }

        // ジングル自動検出（未指定なら）
        if (!jingleSource && resultRoot)
            jingleSource = resultRoot.GetComponent<AudioSource>() ?? resultRoot.GetComponentInChildren<AudioSource>(true);

        initialized = true;
    }

    // タイマー開始を OnEnable に移動（プロローグ中は enabled=false なので呼ばれない）
    void OnEnable()
    {
        if (initialized)
        {
            ArmBySeconds();
        }
    }

    void ArmBySeconds()
    {
        if (trigger == TriggerMode.ByFixedSeconds)
        {
            double now = AudioSettings.dspTime;
            scheduledFireDsp = now + Mathf.Max(0f, totalSongSeconds + fireOffsetSeconds);
            StartCoroutine(WaitUntilDsp(scheduledFireDsp));
        }
        else if (trigger == TriggerMode.ByAudioSource)
        {
            if (gameMusic && gameMusic.clip)
            {
                double now = AudioSettings.dspTime;
                float remaining = Mathf.Max(0f, gameMusic.clip.length - gameMusic.time);
                scheduledFireDsp = now + remaining + fireOffsetSeconds;
                StartCoroutine(WaitUntilDsp(scheduledFireDsp));
            }
        }
        // External は Run() を外部から1回呼ぶ
    }

    IEnumerator WaitUntilDsp(double fireDsp)
    {
        while (AudioSettings.dspTime < fireDsp && !running)
            yield return null;
        if (!running) Run();
    }

    // 外部通知（任意）
    public void NotifyGameEnded() { if (!running) Run(); }

    // 互換API
    public void Run() { if (Application.isPlaying && !running) StartCoroutine(RunCo()); }
    public void Play() => Run();
    public void StartSequence() => Run();
    public void StartSequence(float _) => Run();
    public void StartSequence(float _, params object[] __) => Run();
    public void StartSequence(object __) => Run();
    public void StartSequence(object __, object ___) => Run();

    // -------------------- 本体：黒 → 動画 → リザルト(+ジングル) --------------------
    IEnumerator RunCo()
    {
        running = true;

        // 1) 黒にする
        if (fadeGroup)
        {
            if (!fadeGroup.gameObject.activeSelf) fadeGroup.gameObject.SetActive(true);
            fadeGroup.blocksRaycasts = true;
            fadeGroup.interactable   = false;
            fadeGroup.alpha          = 1f;
        }
        yield return new WaitForSecondsRealtime(settleWait);

        // 2) 動画（失敗しても続行）
        if (resultRoot) resultRoot.SetActive(false);
        if (interstitialGroup) { interstitialGroup.alpha = 1f; interstitialGroup.blocksRaycasts = false; }
        if (videoImage) videoImage.enabled = true;

        if (videoPlayer)
        {
            if (videoAudio)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                try { videoPlayer.SetTargetAudioSource(0, videoAudio); } catch {}
                videoAudio.mute = false; videoAudio.volume = 1f; videoAudio.spatialBlend = 0f;
            }
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping   = false;
            videoPlayer.skipOnDrop  = true;
            videoPlayer.playbackSpeed = 1f;

            if (videoPlayer.isPlaying) videoPlayer.Stop();

            bool prepared = false;
            VideoPlayer.EventHandler onPrep = _ => prepared = true;
            videoPlayer.prepareCompleted += onPrep;

            bool hasSource = (videoPlayer.clip != null) || !string.IsNullOrEmpty(videoPlayer.url);
            if (hasSource)
            {
                videoPlayer.Prepare();
                float t = 0f, timeout = Mathf.Max(0.5f, prepareTimeout);
                while (!prepared && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            }
            videoPlayer.prepareCompleted -= onPrep;

            if (prepared)
            {
                videoPlayer.frame = 0;
                videoPlayer.Play();
                if (videoAudio) videoAudio.Play();

                float guard = 0.3f;
                while (guard > 0f && videoPlayer.isPrepared && videoPlayer.frame <= 0)
                { guard -= Time.unscaledDeltaTime; yield return null; }

                if (!videoPlayer.isPlaying)
                {
                    videoPlayer.Stop();
                    videoPlayer.Play();
                    if (videoAudio) videoAudio.Play();
                }

                float minPlay = 0.2f;
                while (videoPlayer.isPlaying || (minPlay > 0f))
                { minPlay -= Time.unscaledDeltaTime; yield return null; }
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.2f);
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.2f);
        }

        yield return new WaitForSecondsRealtime(settleWait);

        // 3) リザルト表示（ジングル必ず再生）
        if (videoImage) videoImage.enabled = false;
        if (interstitialGroup) interstitialGroup.alpha = 0f;

        // 動画オーディオが残っていたら止める
        if (videoAudio && videoAudio.isPlaying) videoAudio.Stop();

        // リザルトを出す
        if (resultRoot) resultRoot.SetActive(true);

        // ★ジングル再生（自動検出 or 明示指定）
        if (!jingleSource && resultRoot)
            jingleSource = resultRoot.GetComponent<AudioSource>() ?? resultRoot.GetComponentInChildren<AudioSource>(true);

        if (jingleSource)
        {
            jingleSource.ignoreListenerPause = true; // ポーズやTimeScaleに影響されにくく
            jingleSource.spatialBlend = 0f;          // 2Dで確実に聞こえる
            if (jingleSource.volume <= 0f) jingleSource.volume = 1f;
            jingleSource.Play();
        }

        running = false;
    }
}
