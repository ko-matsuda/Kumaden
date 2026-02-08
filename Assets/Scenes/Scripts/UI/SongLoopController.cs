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
    [SerializeField] private GameObject resultCanvas;
    [SerializeField] private ResultCaller resultCaller;  // ResultCallerへの参照

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
        
        // ResultCallerを使ってリザルトを表示
        if (resultCaller != null)
        {
            Debug.Log("[SongLoopController] Calling ResultCaller.TriggerResult()");
            resultCaller.TriggerResult();
        }
        else
        {
            Debug.LogError("[SongLoopController] ResultCaller reference is NULL!");
            
            // フォールバック: 直接ResultCanvasをアクティブ化
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

        // リザルト画面を非表示にする
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

        // MusicEndWatcher をリセットして有効化（Song Bからリザルト発動OK）
        var musicEndWatcher = FindObjectOfType<MusicEndWatcher>();
        if (musicEndWatcher != null)
        {
            musicEndWatcher.ResetForNewSong();
            musicEndWatcher.enabled = true;
            Debug.Log("[SongLoopController] MusicEndWatcher enabled for Song B");
        }

        // CookingResultSequence をリセットして有効化
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
        // ResultCanvas を非表示
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

        // FadeCanvas もクリア
        var cookingResult = FindObjectOfType<CookingResultSequence>();
        if (cookingResult != null && cookingResult.fadeGroup != null)
        {
            cookingResult.fadeGroup.alpha = 0f;
            cookingResult.fadeGroup.blocksRaycasts = false;
            cookingResult.fadeGroup.interactable = false;
        }

        // QuickRanking も非表示
        var quickRanking = FindObjectOfType<QuickRankingDisplay>();
        if (quickRanking != null)
        {
            quickRanking.gameObject.SetActive(false);
        }

        // ResultCaller もリセット
        if (resultCaller != null)
        {
            resultCaller.ResetForNewSong();
        }

        // SafeArea を再表示
        if (resultCanvas != null)
        {
            var safeArea = resultCanvas.transform.Find("SafeArea");
            if (safeArea != null)
            {
                safeArea.gameObject.SetActive(true);
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
