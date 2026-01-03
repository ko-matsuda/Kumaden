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
        if (musicSource == null)
            return;

        if (!musicSource.isPlaying)
            return;

        // 曲の終了直前で次の曲へ
        if (musicSource.time >= musicSource.clip.length - 0.1f)
        {
            NextSong();
        }
    }

    void NextSong()
    {
        Debug.Log("[SongLoopController] NextSong");

        currentSongIndex = (currentSongIndex + 1) % songs.Length;

        musicSource.Stop();

        PlayCurrentSong();
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
