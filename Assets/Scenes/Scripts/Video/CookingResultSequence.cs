using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class CookingResultSequence : MonoBehaviour
{
    public enum TriggerMode { ByFixedSeconds, ByAudioSource, External }

    [Header("Trigger")]
    public TriggerMode trigger = TriggerMode.ByAudioSource;
    public float totalSongSeconds = 120f;
    public float fireOffsetSeconds = 0f;
    public AudioSource gameMusic;

    [Header("Refs")]
    public CanvasGroup fadeGroup;
    public CanvasGroup interstitialGroup;
    public RawImage videoImage;
    public VideoPlayer videoPlayer;
    public AudioSource videoAudio;
    public GameObject resultRoot;
    public AudioSource jingleSource;
    
    [Header("Result Videos")]
    public VideoClip resultSuccessClip;
    public VideoClip resultFailClip;

    [Header("Timings")]
    public float prepareTimeout = 1.0f;
    public float settleWait = 0.05f;

    bool isRunning;
    bool isArmed;

    void Awake()
    {
        if (fadeGroup)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;
            var cv = fadeGroup.GetComponent<Canvas>();
            if (cv) { cv.overrideSorting = true; cv.sortingOrder = 9000; }
        }
        if (interstitialGroup)
        {
            interstitialGroup.alpha = 0f;
            interstitialGroup.blocksRaycasts = false;
            interstitialGroup.interactable = false;
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
            videoPlayer.isLooping = false;
            videoPlayer.playOnAwake = false;
            if (videoAudio)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                try { videoPlayer.SetTargetAudioSource(0, videoAudio); } catch {}
                videoAudio.mute = false;
                videoAudio.volume = 1f;
                videoAudio.spatialBlend = 0f;
            }
        }

        if (!jingleSource && resultRoot)
            jingleSource = resultRoot.GetComponent<AudioSource>() ?? resultRoot.GetComponentInChildren<AudioSource>(true);
    }

    void OnEnable()
    {
        isArmed = true;
        isRunning = false;
    }

    void Update()
    {
        if (!isArmed || isRunning) return;

        if (trigger == TriggerMode.ByAudioSource)
        {
            if (gameMusic != null && gameMusic.clip != null)
            {
                if (gameMusic.isPlaying)
                {
                    float remaining = gameMusic.clip.length - gameMusic.time;
                    if (remaining <= fireOffsetSeconds + 0.1f)
                    {
                        isArmed = false;
                        TriggerResultSequence();
                    }
                }
                else if (gameMusic.time > 0 && gameMusic.time >= gameMusic.clip.length - 0.5f)
                {
                    isArmed = false;
                    TriggerResultSequence();
                }
            }
        }
    }

    public void NotifyGameEnded()
    {
        if (!isRunning) TriggerResultSequence();
    }

    public void TriggerResultSequence()
    {
        if (Application.isPlaying && !isRunning)
            StartCoroutine(ResultSequenceCoroutine());
    }

    IEnumerator ResultSequenceCoroutine()
    {
        isRunning = true;
        Debug.Log("[CookingResultSequence] Starting");

        StoreResultData();
        
        

        if (fadeGroup)
        {
            if (!fadeGroup.gameObject.activeSelf) fadeGroup.gameObject.SetActive(true);
            fadeGroup.blocksRaycasts = true;
            fadeGroup.interactable = false;
            fadeGroup.alpha = 1f;
        }
        yield return new WaitForSecondsRealtime(settleWait);

        if (resultRoot) resultRoot.SetActive(false);
        
        if (interstitialGroup)
        {
            if (!interstitialGroup.gameObject.activeSelf)
                interstitialGroup.gameObject.SetActive(true);
            interstitialGroup.alpha = 1f;
            interstitialGroup.blocksRaycasts = false;
        }
        if (videoImage) videoImage.enabled = true;

        if (videoPlayer)
        {
            // ★★★ RenderTextureをクリア ★★★
            if (videoPlayer.targetTexture != null)
            {
                RenderTexture rt = videoPlayer.targetTexture;
                RenderTexture.active = rt;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = null;
                Debug.Log("[CookingResultSequence] RenderTexture cleared");
            }
            
            if (videoAudio)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                try { videoPlayer.SetTargetAudioSource(0, videoAudio); } catch {}
                videoAudio.mute = false;
                videoAudio.volume = 1f;
                videoAudio.spatialBlend = 0f;
            }
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.skipOnDrop = true;
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
                while (!prepared && t < timeout)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            videoPlayer.prepareCompleted -= onPrep;

            if (prepared)
            {
                videoPlayer.frame = 0;
                videoPlayer.Play();
                if (videoAudio) videoAudio.Play();

                float guard = 0.3f;
                while (guard > 0f && videoPlayer.isPrepared && videoPlayer.frame <= 0)
                {
                    guard -= Time.unscaledDeltaTime;
                    yield return null;
                }

                if (!videoPlayer.isPlaying)
                {
                    videoPlayer.Stop();
                    videoPlayer.Play();
                    if (videoAudio) videoAudio.Play();
                }

                float minPlay = 0.2f;
                while (videoPlayer.isPlaying || (minPlay > 0f))
                {
                    minPlay -= Time.unscaledDeltaTime;
                    yield return null;
                }
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

        if (videoImage) videoImage.enabled = false;
        if (interstitialGroup)
        {
            interstitialGroup.alpha = 0f;
            interstitialGroup.gameObject.SetActive(false);
        }

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

        isRunning = false;
    }

void StoreResultData()
    {
        var score = ScoreManagerLite.Instance;
        if (score == null)
        {
            Debug.LogWarning("[CookingResultSequence] ScoreManagerLite not found");
            return;
        }

        int perfect = score.PerfectCount;
        int good = score.GoodCount;
        int miss = score.MissCount;
        int maxCombo = score.MaxChain;
        int milk = score.MilkCount;
        int flour = score.FlourCount;
        int egg = score.EggCount;
        float playTime = Time.timeSinceLevelLoad;

        string rank;
        if (miss == 0) rank = "S";
        else if (miss <= 1) rank = "A";
        else if (miss <= 2) rank = "B";
        else rank = "C";

        ResultStore.Save(maxCombo, perfect, good, miss, egg, flour, milk, rank, playTime);
        Debug.Log($"[CookingResultSequence] Saved: P={perfect} G={good} M={miss} Combo={maxCombo} Rank={rank}");
        
        // ★★★ ここで動画を設定（PlayerPrefs保存の前）★★★
        if (videoPlayer != null)
        {
            Debug.Log($"[CookingResultSequence] Setting video for rank: {rank}");
            
            if (rank == "S" || rank == "A" || rank == "B")
            {
                if (resultSuccessClip != null)
                {
                    videoPlayer.clip = resultSuccessClip;
                    Debug.Log($"[CookingResultSequence] Set SUCCESS video: {resultSuccessClip.name}");
                }
            }
            else
            {
                if (resultFailClip != null)
                {
                    videoPlayer.clip = resultFailClip;
                    Debug.Log($"[CookingResultSequence] Set FAIL video: {resultFailClip.name}");
                }
            }
        }
    }
}