using UnityEngine;

[System.Serializable]
public class SongData
{
    public AudioClip audioClip;
    public TextAsset chartJson;
}

public class SongLoopController : MonoBehaviour
{
    [Header("Songs (Size must be 2)")]
    [SerializeField] private SongData[] songs = new SongData[2];

    [Header("References")]
    [SerializeField] private ChartSpawner chartSpawner;
    [SerializeField] private Conductor conductor;

    
    
    [Header("Loop Settings")]
    public bool enableLooping = false;
private bool isTransitioning = false;
private int currentSongIndex = 0;
    private AudioSource musicSource;

void Awake()
    {
        Debug.Log("[SongLoopController] Awake called");
        
        if (conductor == null)
            conductor = FindObjectOfType<Conductor>();
        
        if (conductor != null)
            musicSource = conductor.GetComponent<AudioSource>();
        
        // ResultCanvasを強制的に非表示（ゲーム開始時のみ）
        var resultCanvas = GameObject.Find("ResultCanvas");
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(false);
            Debug.Log("[SongLoopController] ResultCanvas hidden in Awake");
        }
        
        // SafeAreaを再表示
        var safeArea = GameObject.Find("SafeArea");
        if (safeArea != null)
        {
            var canvasGroup = safeArea.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            Debug.Log("[SongLoopController] SafeArea shown in Awake");
        }
        
        Debug.Log($"[SongLoopController] Setting up song {currentSongIndex}");
        musicSource.clip = songs[currentSongIndex].audioClip;
        LoadChart();
        
        Debug.Log($"[SongLoopController] Setup complete - clip={musicSource.clip.name}");
    }

void Update()
    {
        if (musicSource == null || conductor == null)
            return;
        
        // ループが有効な場合：曲切り替えを続ける
        if (enableLooping)
        {
            if (!isTransitioning && musicSource.isPlaying && musicSource.time >= musicSource.clip.length - 0.5f)
            {
                Debug.Log($"[SongLoopController] Song {currentSongIndex} about to end - preloading next song");
                StartCoroutine(TransitionToNextSong());
            }
        }
        // ループが無効な場合：2曲目終了時にリザルト表示
        else
        {
            if (!isTransitioning && musicSource.isPlaying && musicSource.time >= musicSource.clip.length - 0.5f)
            {
                // 1曲目終了：次の曲へ
                if (currentSongIndex == 0)
                {
                    Debug.Log($"[SongLoopController] Song 0 finished - transitioning to song 1");
                    StartCoroutine(TransitionToNextSong());
                }
                // 2曲目終了：リザルト表示
                else if (currentSongIndex == 1)
                {
                    Debug.Log($"[SongLoopController] Song 1 finished - showing result");
                    isTransitioning = true;
                    ShowResult();
                }
            }
        }
    }

void ShowResult()
    {
        Debug.Log("[SongLoopController] ShowResult called");
        
        // SafeAreaを非表示
        var safeArea = GameObject.Find("SafeArea");
        if (safeArea != null)
        {
            var canvasGroup = safeArea.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = safeArea.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Debug.Log("[SongLoopController] SafeArea hidden");
        }
        
        // ResultCanvasを有効化
        var resultCanvas = GameObject.Find("ResultCanvas");
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(true);
            
            // ResultCanvasのCanvasGroupを確実に表示
            var canvasGroup = resultCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            Debug.Log("[SongLoopController] ResultCanvas activated");
        }
        else
        {
            Debug.LogError("[SongLoopController] ResultCanvas not found!");
        }
    }


private System.Collections.IEnumerator TransitionToNextSong()
    {
        isTransitioning = true;
        
        Debug.Log("[SongLoopController] TransitionToNextSong started");
        
        // 次の曲インデックス
        int nextIndex;
        if (enableLooping)
        {
            // ループ有効：循環
            nextIndex = (currentSongIndex + 1) % songs.Length;
        }
        else
        {
            // ループ無効：0→１のみ
            nextIndex = currentSongIndex + 1;
            if (nextIndex >= songs.Length)
            {
                Debug.LogWarning("[SongLoopController] No more songs to play");
                isTransitioning = false;
                yield break;
            }
        }
        
        // 現在の音楽が終わる正確な時刻を計算
        double endTime = AudioSettings.dspTime + (musicSource.clip.length - musicSource.time);
        
        // 前の曲のノーツをクリア
        if (chartSpawner != null)
        {
            chartSpawner.ResetForNewSong();
        }
        
        // 曲を切り替え
        currentSongIndex = nextIndex;
        musicSource.clip = songs[currentSongIndex].audioClip;
        
        // Conductorのタイミングを調整
        if (conductor != null)
        {
            conductor.ResetTiming();
        }
        
        // 正確なタイミングで音楽開始
        musicSource.PlayScheduled(endTime);
        
        Debug.Log($"[SongLoopController] Song {currentSongIndex} scheduled at {endTime}");
        
        // 1フレーム待つ（音楽は間に合う）
        yield return null;
        
        // 次のフレームでチャート読み込み（重い処理）
        if (chartSpawner != null && songs[nextIndex].chartJson != null)
        {
            chartSpawner.LoadChartFromJson(songs[nextIndex].chartJson.text);
            Debug.Log($"[SongLoopController] Chart loaded for song {nextIndex}");
        }
        
        // BPM更新
        if (conductor != null && chartSpawner != null && chartSpawner.currentChart != null)
        {
            conductor.bpm = chartSpawner.currentChart.bpm;
            Debug.Log($"[SongLoopController] BPM updated to {conductor.bpm}");
        }
        
        isTransitioning = false;
    }


void NextSong()
    {
        Debug.Log("[SongLoopController] NextSong called");
        
        currentSongIndex = (currentSongIndex + 1) % songs.Length;
        
        Debug.Log($"[SongLoopController] Switching to song {currentSongIndex}");
        
        // 前の曲のノーツをクリア
        if (chartSpawner != null)
        {
            chartSpawner.ResetForNewSong();
        }
        
        // 音楽を即座に切り替え
        musicSource.Stop();
        musicSource.clip = songs[currentSongIndex].audioClip;
        musicSource.Play();
        
        // チャート読み込み
        if (chartSpawner != null && songs[currentSongIndex].chartJson != null)
        {
            chartSpawner.LoadChartFromJson(songs[currentSongIndex].chartJson.text);
            Debug.Log($"[SongLoopController] Chart loaded for song {currentSongIndex}");
        }
        
        // BPM更新
        if (conductor != null && chartSpawner != null && chartSpawner.currentChart != null)
        {
            conductor.bpm = chartSpawner.currentChart.bpm;
            Debug.Log($"[SongLoopController] BPM set to {conductor.bpm}");
        }
        
        // Conductorのタイミングをリセット
        if (conductor != null)
        {
            conductor.ResetTiming();
        }
        
        Debug.Log($"[SongLoopController] Song switched to {musicSource.clip.name}");
    }

    void PlayCurrentSong()
    {
        if (songs[currentSongIndex].audioClip == null)
        {
            Debug.LogError("[SongLoopController] AudioClip missing at index " + currentSongIndex);
            return;
        }

        Debug.Log("[SongLoopController] Play song index " + currentSongIndex);

        musicSource.clip = songs[currentSongIndex].audioClip;
        musicSource.time = 0f;

        LoadChart();

        if (conductor != null)
        {
            conductor.StartMusic();
        }
    }

    void LoadChart()
    {
        if (chartSpawner == null)
            chartSpawner = FindObjectOfType<ChartSpawner>();

        if (chartSpawner == null)
        {
            Debug.LogError("[SongLoopController] ChartSpawner not found");
            return;
        }

        if (songs[currentSongIndex].chartJson == null)
        {
            Debug.LogError("[SongLoopController] Chart JSON missing at index " + currentSongIndex);
            return;
        }

        // 前の曲のノーツをクリア
        chartSpawner.ResetForNewSong();

        // 新しい譜面をロード
        chartSpawner.LoadChartFromJson(songs[currentSongIndex].chartJson.text);

        Debug.Log("[SongLoopController] Chart loaded for song " + currentSongIndex);
    }
}
