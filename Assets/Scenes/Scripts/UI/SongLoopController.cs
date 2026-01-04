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

    [Header("Result UI")]
    [SerializeField] private GameObject resultCanvas;   // ★ 追加：ResultCanvas参照

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

        // ★ ResultCanvas を一度だけ取得（非アクティブOK）
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

        // SafeArea 非表示
        var safeArea = GameObject.Find("SafeArea");
        if (safeArea != null)
        {
            var cg = safeArea.GetComponent<CanvasGroup>();
            if (cg == null) cg = safeArea.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

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

            Debug.Log("[SongLoopController] ResultCanvas activated");
        }
        else
        {
            Debug.LogError("[SongLoopController] ResultCanvas reference is NULL");
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

        isTransitioning = false;
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
