using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// 曲終了で発火 → 黒 → 料理動画 → 黒の上にリザルト(+ジングル再生)
[DisallowMultipleComponent]
public class CookingResultSequence : MonoBehaviour
{
    public enum TriggerMode { ByFixedSeconds, ByAudioSource, External }

    [Header("Trigger")]
    public TriggerMode trigger = TriggerMode.ByAudioSource;

    [Tooltip("TriggerMode=ByFixedSeconds のとき：曲の総尺（秒）")]
    public float totalSongSeconds = 120f;

    [Tooltip("発火オフセット（秒）。終端ぴったり=0、少し後=+, 少し前=-")]
    public float fireOffsetSeconds = 0f;

    [Tooltip("TriggerMode=ByAudioSource のとき：インゲームBGMの AudioSource（ループOFF想定）")]
    public AudioSource gameMusic;

    [Header("Refs (必ずアサイン)")]
    public CanvasGroup fadeGroup;
    public CanvasGroup interstitialGroup;
    public RawImage    videoImage;
    public VideoPlayer videoPlayer;
    public AudioSource videoAudio;
    public GameObject  resultRoot;

    [Header("Result Jingle")]
    public AudioSource jingleSource;

    [Header("Timings / Guards")]
    public float prepareTimeout = 1.0f;
    public float settleWait = 0.05f;

    bool running;
    bool armed;

    void Awake()
    {
        if (fadeGroup)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable   = false;
            var cv = fadeGroup.GetComponent<Canvas>();
            if (cv) { cv.overrideSorting = true; cv.sortingOrder = 9000; }
        }
        if (interstitialGroup)
        {
            interstitialGroup.alpha = 0f;
            interstitialGroup.blocksRaycasts = false;
            interstitialGroup.interactable   = false;
            var cv = interstitialGroup.GetComponent<Canvas>();
            if (cv) { cv.overrideSorting = true; cv.sortingOrder = 10000; }
        }
        if (videoImage) videoImage.enabled = false;

        if (resultRoot)
        {
            resultRoot.SetActive(false);
            var cv = resultRoot.GetComponent<Canvas>();
            if (cv) { cv.overrideSorting = true; cv.sortingOrder = 10001; }
        }

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

        if (!jingleSource && resultRoot)
            jingleSource = resultRoot.GetComponent<AudioSource>() ?? resultRoot.GetComponentInChildren<AudioSource>(true);
    }

    void OnEnable()
    {
        armed = true;
        running = false;
    }

    void Update()
    {
        if (!armed || running) return;

        if (trigger == TriggerMode.ByAudioSource)
        {
            // 音楽が再生中で、終了間近かチェック
            if (gameMusic != null && gameMusic.clip != null)
            {
                // 音楽が再生されていて、終端に達した
                if (gameMusic.isPlaying)
                {
                    float remaining = gameMusic.clip.length - gameMusic.time;
                    if (remaining <= fireOffsetSeconds + 0.1f)
                    {
                        armed = false;
                        Run();
                    }
                }
                // 音楽が終了した（isPlaying = false で time が終端付近）
                else if (gameMusic.time > 0 && gameMusic.time >= gameMusic.clip.length - 0.5f)
                {
                    armed = false;
                    Run();
                }
            }
        }
        else if (trigger == TriggerMode.ByFixedSeconds)
        {
            // OnEnable からの経過時間で判定
            // （この方式は使わない方がいいので ByAudioSource 推奨）
        }
    }

    public void NotifyGameEnded() { if (!running) Run(); }

    public void Run() { if (Application.isPlaying && !running) StartCoroutine(RunCo()); }
    public void Play() => Run();
    public void StartSequence() => Run();

    IEnumerator RunCo()
    {
        running = true;
        Debug.Log("[CookingResultSequence] Starting result sequence");

        // 1) 黒にする
        if (fadeGroup)
        {
            if (!fadeGroup.gameObject.activeSelf) fadeGroup.gameObject.SetActive(true);
            fadeGroup.blocksRaycasts = true;
            fadeGroup.interactable   = false;
            fadeGroup.alpha          = 1f;
        }
        yield return new WaitForSecondsRealtime(settleWait);

        // 2) 動画
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

        // 3) リザルト表示
        if (videoImage) videoImage.enabled = false;
        if (interstitialGroup) interstitialGroup.alpha = 0f;

        if (videoAudio && videoAudio.isPlaying) videoAudio.Stop();

        if (resultRoot) resultRoot.SetActive(true);

        if (!jingleSource && resultRoot)
            jingleSource = resultRoot.GetComponent<AudioSource>() ?? resultRoot.GetComponentInChildren<AudioSource>(true);

        if (jingleSource)
        {
            jingleSource.ignoreListenerPause = true;
            jingleSource.spatialBlend = 0f;
            if (jingleSource.volume <= 0f) jingleSource.volume = 1f;
            jingleSource.Play();
        }

        running = false;
    }
}
