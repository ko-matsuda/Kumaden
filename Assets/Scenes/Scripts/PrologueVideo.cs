using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer), typeof(AudioSource))]
public class PrologueVideo : MonoBehaviour
{
    [Header("動画クリップ")]
    public VideoClip videoClip;
    
    [Header("BGMフェード設定")]
    public float bgmLeadTime = 3.9f;
    
    [Header("ゲームロジック制御")]
    [Tooltip("動画再生中は無効化するGameObject（ChartSpawner, Conductorなど）")]
    public GameObject[] gameLogicObjects;
    
    [Header("最適化設定")]
    [Tooltip("trueの場合、VideoPlayerCleanerを完全に無視（エミュレータ推奨）")]
    public bool disableCleaner = true;
    
    private VideoPlayer vp;
    private AudioSource audioSource;
    private PrologueOverlay overlay;
    private bool hasEnded = false;
    private bool bgmStarted = false;
    private bool[] originalStates;

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
            enabled = false;
            return;
        }
        
        Debug.Log("[PrologueVideo] Playing prologue video");
        
        // ★重要★ ゲームロジックを一時停止
        PauseGameLogic();
        
        if (videoClip == null)
        {
            Debug.LogError("[PrologueVideo] VideoClip が設定されていません");
            ResumeGameLogic();
            return;
        }
        
        vp = GetComponent<VideoPlayer>();
        var audioSource = GetComponent<AudioSource>();
        
        vp.source = VideoSource.VideoClip;
        vp.clip = videoClip;
        
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
        vp.waitForFirstFrame = false;
        vp.timeUpdateMode = VideoTimeUpdateMode.GameTime;

        overlay = FindObjectOfType<PrologueOverlay>();

        vp.prepareCompleted += OnPrepared;
        vp.loopPointReached += OnVideoEnd;
        vp.errorReceived += OnVideoError;
        
        // VideoPlayerCleanerを完全に無視
        if (disableCleaner)
        {
            var cleaner = GetComponent<VideoPlayerCleaner>();
            if (cleaner != null)
            {
                Debug.Log("[PrologueVideo] Disabling VideoPlayerCleaner");
                cleaner.enabled = false;
            }
        }
        
        Debug.Log("[PrologueVideo] Starting Prepare...");
        vp.Prepare();
    }
    
    void PauseGameLogic()
    {
        if (gameLogicObjects == null || gameLogicObjects.Length == 0)
        {
            Debug.LogWarning("[PrologueVideo] No game logic objects assigned to pause");
            return;
        }
        
        originalStates = new bool[gameLogicObjects.Length];
        
        for (int i = 0; i < gameLogicObjects.Length; i++)
        {
            if (gameLogicObjects[i] != null)
            {
                originalStates[i] = gameLogicObjects[i].activeSelf;
                gameLogicObjects[i].SetActive(false);
                Debug.Log($"[PrologueVideo] Paused: {gameLogicObjects[i].name}");
            }
        }
    }
    
    void ResumeGameLogic()
    {
        if (gameLogicObjects == null || originalStates == null)
        {
            return;
        }
        
        for (int i = 0; i < gameLogicObjects.Length; i++)
        {
            if (gameLogicObjects[i] != null && i < originalStates.Length)
            {
                gameLogicObjects[i].SetActive(originalStates[i]);
                Debug.Log($"[PrologueVideo] Resumed: {gameLogicObjects[i].name}");
            }
        }
    }

    void OnPrepared(VideoPlayer source)
    {
        Debug.Log($"[PrologueVideo] Prepared! Duration: {vp.length:F2} sec");
        vp.Play();
        Debug.Log("[PrologueVideo] Play() called");
    }
    
    void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"[PrologueVideo] Video error: {message}");
        ResumeGameLogic();
    }

    void Update()
    {
        if (vp == null || !vp.isPlaying || hasEnded) return;
        
        double fadeStartTime = vp.length - bgmLeadTime;
        if (!bgmStarted && vp.time >= fadeStartTime)
        {
            bgmStarted = true;
            Debug.Log($"[PrologueVideo] Starting BGM fade at {vp.time:F1} sec");
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
        
        // ★重要★ ゲームロジックを再開
        ResumeGameLogic();
        
        // プロローグを見たことを記録
        PlayerPrefs.SetInt("HasSeenPrologue", 1);
        PlayerPrefs.Save();
        Debug.Log("[PrologueVideo] Saved HasSeenPrologue flag");
        
        if (overlay != null)
        {
            overlay.OnVideoEnded();
        }
    }
    
    void OnDestroy()
    {
        if (vp != null)
        {
            vp.prepareCompleted -= OnPrepared;
            vp.loopPointReached -= OnVideoEnd;
            vp.errorReceived -= OnVideoError;
        }
    }
}