using UnityEngine;

[System.Serializable]
public class SongData
{
    public AudioClip audioClip;
    public TextAsset chartJson;
}

public class SongLoopController : MonoBehaviour
{
    [Header("Songs Day (Easy)")]
    [SerializeField] private SongData[] songs = new SongData[2];

    [Header("Songs Dusk/Evening (Normal)")]
    [SerializeField] private SongData[] songsDusk = new SongData[2];

    [Header("Songs Night (Hard)")]
    [SerializeField] private SongData[] songsNight = new SongData[2];

    [Header("References")]
    [SerializeField] private ChartSpawner chartSpawner;
    [SerializeField] private Conductor conductor;

    [Header("Result UI")]
    [SerializeField] private GameObject resultCanvas;
    [SerializeField] private ResultCaller resultCaller;

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

        // 難易度に応じてsongsを切り替え
        if (DifficultyManager.Instance != null)
        {
            Difficulty diff = DifficultyManager.Instance.GetCurrentDifficulty();
            if (diff == Difficulty.Hard && songsNight[0].audioClip != null)
                songs = songsNight;
            else if (diff == Difficulty.Normal && songsDusk[0].audioClip != null)
                songs = songsDusk;
            Debug.Log($"[SongLoopController] Difficulty={diff}, using songs[0]={songs[0]?.audioClip?.name}");
        }

        // ResultCanvas を一度だけ取得（非アクティブOK）
        if (resultCanvas == null)
        {
            var canvases = FindObjectsOfType<Canvas>(true);
            foreach (var c in canvases)
            {
                if (c.name == "ResultCanvas")
                {
                    resultCanvas = c.gameObject;
                    break;
                }
            }
        }

        if (resultCanvas != null)
        {
            resultCanvas.SetActive(false);
            Debug.Log("[SongLoopController] ResultCanvas cached and hidden in Awake");
        }
        else
        {
            Debug.LogError("[SongLoopController] ResultCanvas NOT FOUND in Awake");
        }

        // SafeArea を表示
        var safeArea = GameObject.Find("SafeArea");
        if (safeArea != null)
        {
            var cg = safeArea.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        // ★★★ Song A中はMusicEndWatcherとCookingResultSequenceを無効化 ★★★
        var mew = FindObjectOfType<MusicEndWatcher>();
        if (mew != null)
        {
            mew.enabled = false;
            Debug.Log("[SongLoopController] MusicEndWatcher disabled during Song A");
        }
        var crs = FindObjectOfType<CookingResultSequence>();
        if (crs != null)
        {
            crs.enabled = false;
            Debug.Log("[SongLoopController] CookingResultSequence disabled during Song A");
        }

        // 初期曲セット
        musicSource.clip = songs[currentSongIndex].audioClip;
        LoadChart();

        Debug.Log($"[SongLoopController] Setup complete - clip={musicSource.clip.name}");
    }

    void Update()
    {
        if (musicSource == null || conductor == null)
            return;

        if (!isTransitioning && musicSource.isPlaying &&
            musicSource.time >= musicSource.clip.length - 0.5f)
        {
            if (enableLooping)
            {
                StartCoroutine(TransitionToNextSong());
            }
            else
            {
                if (currentSongIndex == 0)
                {
                    Debug.Log("[SongLoopController] Song A finished -> Song B");
                    StartCoroutine(TransitionToNextSong());
                }
                else
                {
                    Debug.Log("[SongLoopController] Song B finished -> Result");
                    isTransitioning = true;
                    ShowResult();
                }
            }
        }
    }

    void ShowResult()
    {
        Debug.Log("[SongLoopController] ShowResult called");
        
        if (resultCaller != null)
        {
            Debug.Log("[SongLoopController] Calling ResultCaller.TriggerResult()");
            resultCaller.TriggerResult();
        }
        else
        {
            Debug.LogError("[SongLoopController] ResultCaller reference is NULL!");
            
            if (resultCanvas != null)
            {
                resultCanvas.SetActive(true);
                var cg = resultCanvas.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                Debug.Log("[SongLoopController] ResultCanvas activated (fallback)");
            }
            else
            {
                Debug.LogError("[SongLoopController] ResultCanvas reference is also NULL");
            }
        }
    }

    private System.Collections.IEnumerator TransitionToNextSong()
    {
        isTransitioning = true;

        int nextIndex = currentSongIndex + 1;
        if (nextIndex >= songs.Length)
        {
            isTransitioning = false;
            yield break;
        }

        HideResultUI();

        if (chartSpawner != null)
            chartSpawner.ResetForNewSong();

        currentSongIndex = nextIndex;
        musicSource.clip = songs[currentSongIndex].audioClip;

        if (conductor != null)
            conductor.ResetTiming();

        musicSource.Play();

        yield return null;

        LoadChart();

        if (conductor != null && chartSpawner != null && chartSpawner.currentChart != null)
            conductor.bpm = chartSpawner.currentChart.bpm;

        var musicEndWatcher = FindObjectOfType<MusicEndWatcher>();
        if (musicEndWatcher != null)
        {
            musicEndWatcher.ResetForNewSong();
            musicEndWatcher.enabled = true;
            Debug.Log("[SongLoopController] MusicEndWatcher enabled for Song B");
        }

        var cookingResult = FindObjectOfType<CookingResultSequence>();
        if (cookingResult != null)
        {
            cookingResult.ResetForNewSong();
            cookingResult.enabled = true;
            Debug.Log("[SongLoopController] CookingResultSequence enabled for Song B");
        }

        isTransitioning = false;
        Debug.Log($"[SongLoopController] Transitioned to song {currentSongIndex}");
    }

    private void HideResultUI()
    {
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(false);
            var cg = resultCanvas.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
            Debug.Log("[SongLoopController] ResultCanvas hidden for next song");
        }

        var cookingResult = FindObjectOfType<CookingResultSequence>();
        if (cookingResult != null && cookingResult.fadeGroup != null)
        {
            cookingResult.fadeGroup.alpha = 0f;
            cookingResult.fadeGroup.blocksRaycasts = false;
            cookingResult.fadeGroup.interactable = false;
        }

        var quickRanking = FindObjectOfType<QuickRankingDisplay>();
        if (quickRanking != null)
        {
            quickRanking.gameObject.SetActive(false);
        }

        if (resultCaller != null)
        {
            resultCaller.ResetForNewSong();
        }

        if (resultCanvas != null)
        {
            var sa = resultCanvas.transform.Find("SafeArea");
            if (sa != null)
            {
                sa.gameObject.SetActive(true);
            }
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

        chartSpawner.ResetForNewSong();

        if (songs[currentSongIndex].chartJson != null)
            chartSpawner.LoadChartFromJson(songs[currentSongIndex].chartJson.text);
    }
}