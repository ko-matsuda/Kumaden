using UnityEngine;
using UnityEngine.Video;
using System.Collections;

[RequireComponent(typeof(VideoPlayer), typeof(AudioSource))]
public class PrologueVideo : MonoBehaviour
{
    [Header("動画クリップ")]
    public VideoClip videoClip;
    
    [Header("BGMフェード設定")]
    public float bgmLeadTime = 3.9f;
    
    [Header("ゲームロジック制御")]
    [Tooltip("動画再生中は無効化するGameObject（重要: Conductorは含めないでください！）")]
    public GameObject[] gameLogicObjects;
    
    [Header("再生遅延設定（エミュレータ対策）")]
    [Tooltip("Prepare完了後、この秒数待ってから再生（デコーダー初期化のため）")]
    public float delayBeforePlay = 1.5f;
    
    [Header("超軽量モード（エミュレータ用）")]
    [Tooltip("ONにすると音声なしで最軽量設定")]
    public bool ultraLightMode = false;
    
    [Header("最適化設定")]
    public bool disableCleaner = true;
    public bool verboseLog = true;
    
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
        
        
        // ゲームロジックを一時停止（Conductor以外）
        PauseGameLogic();
        
        if (videoClip == null)
        {
            Debug.LogError("[PrologueVideo] VideoClip が設定されていません");
            ResumeGameLogic();
            return;
        }
        
        vp = GetComponent<VideoPlayer>();
        
        vp.source = VideoSource.VideoClip;
        vp.clip = videoClip;
        
        // 超軽量モード
        if (ultraLightMode)
        {
            Debug.Log("[PrologueVideo] Ultra Light Mode enabled - No audio");
            vp.audioOutputMode = VideoAudioOutputMode.None;
        }
        else
        {
            if (audioSource != null)
            {
                vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
                vp.SetTargetAudioSource(0, audioSource);
            }
            else
            {
                vp.audioOutputMode = VideoAudioOutputMode.Direct;
            }
        }
        
        // 最軽量設定（BlueStacks対応）
        vp.playbackSpeed = 1f;
        vp.isLooping = false;
        vp.skipOnDrop = true;
        vp.waitForFirstFrame = false;  // BlueStacksでフリーズを防ぐ
        vp.timeUpdateMode = VideoTimeUpdateMode.GameTime;
        vp.targetCamera = null;

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
        
        Debug.Log("[PrologueVideo] Calling Prepare()...");
        vp.Prepare();
    }
    
    void PauseGameLogic()
    {
        if (gameLogicObjects == null || gameLogicObjects.Length == 0)
        {
            Debug.LogWarning("[PrologueVideo] No game logic objects to pause");
            return;
        }
        
        originalStates = new bool[gameLogicObjects.Length];
        
        for (int i = 0; i < gameLogicObjects.Length; i++)
        {
            if (gameLogicObjects[i] != null)
            {
                originalStates[i] = gameLogicObjects[i].activeSelf;
                gameLogicObjects[i].SetActive(false);
                if (verboseLog) Debug.Log($"[PrologueVideo] Paused: {gameLogicObjects[i].name}");
            }
        }
        
        Debug.Log($"[PrologueVideo] Paused {gameLogicObjects.Length} game objects");
    }
    
    void ResumeGameLogic()
    {
        if (gameLogicObjects == null || originalStates == null) return;
        
        for (int i = 0; i < gameLogicObjects.Length; i++)
        {
            if (gameLogicObjects[i] != null && i < originalStates.Length)
            {
                gameLogicObjects[i].SetActive(originalStates[i]);
                if (verboseLog) Debug.Log($"[PrologueVideo] Resumed: {gameLogicObjects[i].name}");
            }
        }
        
        Debug.Log($"[PrologueVideo] Resumed {gameLogicObjects.Length} game objects");
    }

void OnPrepared(VideoPlayer source)
    {
        Debug.Log($"[PrologueVideo] Prepared! Duration: {vp.length:F2}s - Playing immediately");
        vp.Play();
    }
    
    IEnumerator DelayedPlay()
    {
        // デコーダー初期化のために待機
        yield return new WaitForSeconds(delayBeforePlay);
        
        Debug.Log("[PrologueVideo] Delay complete, starting playback!");
        vp.Play();
        
        // 再生開始を確認
        yield return new WaitForSeconds(0.1f);
        
        if (vp.isPlaying)
        {
            Debug.Log($"[PrologueVideo] Playback confirmed! Time: {vp.time:F2}s");
        }
        else
        {
            Debug.LogWarning("[PrologueVideo] Video is not playing after delay!");
        }
    }
    
    void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"[PrologueVideo] Video error: {message}");
        ResumeGameLogic();
    }

    void Update()
    {
        if (vp == null || !vp.isPlaying || hasEnded) return;
        
        // デバッグ：30フレームごとに状態を出力
        if (verboseLog && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[PrologueVideo] Playing - Time: {vp.time:F2}s / {vp.length:F2}s");
        }
        
        double fadeStartTime = vp.length - bgmLeadTime;
        if (!bgmStarted && vp.time >= fadeStartTime)
        {
            bgmStarted = true;
            Debug.Log($"[PrologueVideo] Starting BGM fade at {vp.time:F1}s");
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
        
        // ゲームロジックを再開
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