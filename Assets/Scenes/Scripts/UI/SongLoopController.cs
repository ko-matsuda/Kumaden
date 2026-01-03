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

    
    private bool isTransitioning = false;
private int currentSongIndex = 0;
    private AudioSource musicSource;

    void Awake()
    {
        Debug.Log("[SongLoopController] Awake");

        if (conductor == null)
            conductor = FindObjectOfType<Conductor>();

        if (conductor == null)
        {
            Debug.LogError("[SongLoopController] Conductor not found");
            return;
        }

        musicSource = conductor.GetComponent<AudioSource>();
        if (musicSource == null)
        {
            Debug.LogError("[SongLoopController] AudioSource not found on Conductor");
            return;
        }

        PlayCurrentSong();
    }

void Update()
    {
        if (musicSource == null || conductor == null || isTransitioning)
            return;
        
        // 音楽が再生中で、終了間近になったら次の曲へ
        if (musicSource.isPlaying && musicSource.time >= musicSource.clip.length - 0.2f)
        {
            Debug.Log($"[SongLoopController] Song {currentSongIndex} about to end - time={musicSource.time}, length={musicSource.clip.length}");
            StartCoroutine(TransitionToNextSong());
        }
    }

private System.Collections.IEnumerator TransitionToNextSong()
    {
        isTransitioning = true;
        
        Debug.Log("[SongLoopController] TransitionToNextSong started");
        
        // 次の曲インデックス
        int nextIndex = (currentSongIndex + 1) % songs.Length;
        
        // 現在の音楽が終わる正確な時刻を計算
        double endTime = AudioSettings.dspTime + (musicSource.clip.length - musicSource.time);
        
        // Conductorのタイミングを調整（曲切り替え前）
        if (conductor != null)
        {
            conductor.ResetTiming();
        }
        
        // 前の曲のノーツをクリア
        if (chartSpawner != null)
        {
            chartSpawner.ResetForNewSong();
        }
        
        yield return null;
        
        // 曲を切り替え
        currentSongIndex = nextIndex;
        musicSource.clip = songs[currentSongIndex].audioClip;
        
        // 次の曲のチャートを読み込み
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
        
        // 正確なタイミングで音楽開始
        musicSource.PlayScheduled(endTime);
        
        Debug.Log($"[SongLoopController] Song {currentSongIndex} scheduled at {endTime}");
        
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
