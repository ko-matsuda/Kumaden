using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer), typeof(AudioSource))]
public class PrologueVideo : MonoBehaviour
{
    [Header("動画クリップ")]
    public VideoClip videoClip;
    
    [Header("BGMフェード設定")]
    public float bgmLeadTime = 3.9f;
    
    private VideoPlayer vp;
    private AudioSource audioSource;
    private PrologueOverlay overlay;
    private bool hasEnded = false;
    private bool bgmStarted = false;

    void Awake()
    {
        vp = GetComponent<VideoPlayer>();
        audioSource = GetComponent<AudioSource>();
        
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;
    }

    void Start()
    {
        // Retry時はプロローグをスキップ
        bool hasSeenPrologue = PlayerPrefs.GetInt("HasSeenPrologue", 0) == 1;
        bool skipFlag = GameFlags.SkipPrologueOnce;
        
        Debug.Log($"[PrologueVideo] Start - hasSeenPrologue={hasSeenPrologue}, SkipPrologueOnce={skipFlag}");
        
        if (hasSeenPrologue || skipFlag)
        {
            Debug.Log("[PrologueVideo] Skipping prologue video");
            // スキップ時はゲームを通常速度に戻す
            Time.timeScale = 1f;
            enabled = false;
            return;
        }
        
        Debug.Log("[PrologueVideo] Playing prologue video");
        
        // 動画再生中はゲームを停止
        Time.timeScale = 0f;
        
        if (videoClip == null)
        {
            Debug.LogError("[PrologueVideo] VideoClip が設定されていません");
            // エラー時もゲームを再開
            Time.timeScale = 1f;
            return;
        }
        
        vp = GetComponent<VideoPlayer>();
        var audioSource = GetComponent<AudioSource>();
        
        vp.source = VideoSource.VideoClip;
        
        if (audioSource != null)
        {
            vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
            vp.SetTargetAudioSource(0, audioSource);
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
        else
        {
            vp.audioOutputMode = VideoAudioOutputMode.Direct;
        }
        
        vp.playbackSpeed = 1f;
        vp.isLooping = false;
        vp.skipOnDrop = true;
        vp.timeUpdateMode = VideoTimeUpdateMode.DSPTime;

        overlay = FindObjectOfType<PrologueOverlay>();

        vp.prepareCompleted += OnPrepared;
        vp.loopPointReached += OnVideoEnd;
        
        var cleaner = GetComponent<VideoPlayerCleaner>();
        if (cleaner != null)
        {
            cleaner.PlayClean(videoClip);
        }
        else
        {
            vp.clip = videoClip;
            vp.Prepare();
        }
    }

    void OnPrepared(VideoPlayer source)
    {
        Debug.Log($"[PrologueVideo] Prepared. Duration: {vp.length} sec");
        if (GetComponent<VideoPlayerCleaner>() == null)
        {
            vp.Play();
        }
    }

    void Update()
    {
        if (vp == null || !vp.isPlaying || hasEnded) return;
        
        double fadeStartTime = vp.length - bgmLeadTime;
        if (!bgmStarted && vp.time >= fadeStartTime)
        {
            bgmStarted = true;
            Debug.Log($"[PrologueVideo] Starting BGM at {vp.time:F1} sec");
            if (overlay != null)
            {
                overlay.StartBgmFadeIn(bgmLeadTime);
            }
        }
    }

    void OnVideoEnd(VideoPlayer source)
    {
        if (hasEnded) return;
        hasEnded = true;
        
        Debug.Log("[PrologueVideo] Video ended");
        
        // ゲームを再開
        Time.timeScale = 1f;
        
        // プロローグを見たことを記録
        PlayerPrefs.SetInt("HasSeenPrologue", 1);
        PlayerPrefs.Save();
        Debug.Log("[PrologueVideo] Saved HasSeenPrologue flag");
        
        if (overlay != null)
        {
            overlay.OnVideoEnded();
        }
    }
}