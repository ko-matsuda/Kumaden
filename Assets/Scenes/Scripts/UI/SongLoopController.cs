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
    
    private int currentSongIndex = 0;
    private bool isPlaying = false;
    private AudioSource musicSource;
    
void Awake()
    {
        Debug.Log("[SongLoopController] Awake called");
        
        if (conductor == null)
            conductor = FindObjectOfType<Conductor>();
        
        if (conductor != null)
            musicSource = conductor.GetComponent<AudioSource>();
        
        Debug.Log($"[SongLoopController] Setting up song {currentSongIndex}");
        musicSource.clip = songs[currentSongIndex].audioClip;
        LoadChart();
        
        Debug.Log($"[SongLoopController] Setup complete - clip={musicSource.clip.name}");
    }
    
void Update()
    {
        if (musicSource == null || conductor == null)
            return;
        
        // 音楽が再生中で、終了間近になったら次の曲へ
        if (musicSource.isPlaying && musicSource.time >= musicSource.clip.length - 0.5f)
        {
            Debug.Log($"[SongLoopController] Song {currentSongIndex} about to end - time={musicSource.time}, length={musicSource.clip.length}");
            NextSong();
        }
    }

void NextSong()
    {
        Debug.Log("[SongLoopController] NextSong called");
        
        // UIをリセット
        ResetUI();
        
        currentSongIndex = (currentSongIndex + 1) % songs.Length;
        
        Debug.Log($"[SongLoopController] Switching to song {currentSongIndex}");
        
        musicSource.Stop();
        musicSource.clip = songs[currentSongIndex].audioClip;
        
        // チャート読み込み
        LoadChart();
        
        // BPM更新
        if (conductor != null && chartSpawner != null && chartSpawner.currentChart != null)
        {
            conductor.bpm = chartSpawner.currentChart.bpm;
            Debug.Log($"[SongLoopController] BPM set to {conductor.bpm}");
        }
        
        // 音楽開始
        conductor.StartMusic();
        
        Debug.Log($"[SongLoopController] Song switched to {musicSource.clip.name}");
    }

void ResetUI()
    {
        Debug.Log("[SongLoopController] ResetUI called");
        
        // ResultCanvasを非表示
        var resultCanvas = GameObject.Find("ResultCanvas");
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(false);
            Debug.Log("[SongLoopController] ResultCanvas hidden");
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
                Debug.Log("[SongLoopController] SafeArea shown");
            }
        }
        
        // スコアリセット
        var scoreManager = ScoreManagerLite.Instance;
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
            Debug.Log("[SongLoopController] Score reset");
        }
    }


    
    void PlayCurrentSong()
    {
        if (songs[currentSongIndex].audioClip == null)
        {
            Debug.LogError($"[SongLoopController] Song {currentSongIndex} has no AudioClip!");
            return;
        }
        
        musicSource.clip = songs[currentSongIndex].audioClip;
        
        LoadChart();
        
        if (conductor != null)
        {
            conductor.StartMusic();
        }
        
        isPlaying = true;
        
        Debug.Log($"[SongLoopController] Playing song {currentSongIndex}: {musicSource.clip.name}");
    }
    
void LoadChart()
    {
        Debug.Log($"[SongLoopController] LoadChart called for song {currentSongIndex}");
        
        if (chartSpawner == null)
        {
            chartSpawner = FindObjectOfType<ChartSpawner>();
        }
        
        if (chartSpawner != null && songs[currentSongIndex].chartJson != null)
        {
            // 前の曲のノーツをクリア
            chartSpawner.ResetForNewSong();
            
            // 新しいチャートを読み込み
            chartSpawner.LoadChartFromJson(songs[currentSongIndex].chartJson.text);
            Debug.Log($"[SongLoopController] Chart loaded for song {currentSongIndex}");
        }
        else
        {
            Debug.LogWarning($"[SongLoopController] ChartSpawner or chartJson is null for song {currentSongIndex}");
        }
    }
    
    void OnSongFinished()
    {
        Debug.Log($"[SongLoopController] Song {currentSongIndex} finished");
        
        currentSongIndex = (currentSongIndex + 1) % 2;
        
        PlayCurrentSong();
    }
}