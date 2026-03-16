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

#if UNITY_EDITOR
        // デバッグ: DebugStartSongIndex が指定されていれば Song B から開始
        int debugSongIndex = PlayerPrefs.GetInt("DebugStartSongIndex", 0);
        if (debugSongIndex > 0)
        {
            PlayerPrefs.DeleteKey("DebugStartSongIndex");
            PlayerPrefs.Save();
            currentSongIndex = Mathf.Clamp(debugSongIndex, 0, songs.Length - 1);
            Debug.Log($"[SongLoopController] DebugPlay: Song index {currentSongIndex} からスタート");
        }
#endif

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

        var mew = FindObjectOfType<MusicEndWatcher>();
        var crs = FindObjectOfType<CookingResultSequence>();

        if (currentSongIndex == 0)
        {
            // Song A: MusicEndWatcher / CookingResultSequence を無効化
            if (mew != null) { mew.enabled = false; Debug.Log("[SongLoopController] MusicEndWatcher disabled during Song A"); }
            if (crs != null) { crs.enabled = false; Debug.Log("[SongLoopController] CookingResultSequence disabled during Song A"); }
        }
        else
        {
            // Song B から開始: MusicEndWatcher / CookingResultSequence を有効化
            if (mew != null)
            {
                mew.ResetForNewSong();
                if (songs[currentSongIndex].audioClip != null)
                    mew.verse1EndSec = songs[currentSongIndex].audioClip.length - 0.3f;
                mew.enabled = true;
                Debug.Log($"[SongLoopController] DebugPlay: MusicEndWatcher enabled, verse1EndSec={mew.verse1EndSec}");
            }
            if (crs != null)
            {
                crs.ResetForNewSong();
                crs.enabled = true;
                Debug.Log("[SongLoopController] DebugPlay: CookingResultSequence enabled");
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

        // ★ここで ResetForNewSong() を呼ばない
        // 画面上を移動中の Song A のノーツをすぐ消してしまうのを防ぐ
        // LoadChart() 内の ResetForNewSong() がまとめて処理する

        currentSongIndex = nextIndex;

        // ResetTiming() は clip を Song B に切り替える前に呼ぶ
        // (Song A の clip.length で _dspSongStartTime を進めるため)
        if (conductor != null)
            conductor.ResetTiming();

        musicSource.clip = songs[currentSongIndex].audioClip;
        musicSource.Play();

        yield return null;

        
// resultVideoController.rankをリセット（曲B開始時に前回の値が残らないよう）
        var resultVideoCtrl = FindObjectOfType<ResultVideoController>();
        if (resultVideoCtrl != null) resultVideoCtrl.rank = "";

        
// ここで初めてノーツをリセット＆新チャートロード
        LoadChart();

        if (conductor != null && chartSpawner != null && chartSpawner.currentChart != null)
            conductor.bpm = chartSpawner.currentChart.bpm;

        var musicEndWatcher = FindObjectOfType<MusicEndWatcher>();
        if (musicEndWatcher != null)
        {
            musicEndWatcher.ResetForNewSong();
            if (musicSource.clip != null)
                musicEndWatcher.verse1EndSec = musicSource.clip.length - 0.3f;
            musicEndWatcher.enabled = true;
            Debug.Log($"[SongLoopController] MusicEndWatcher enabled, verse1EndSec={musicEndWatcher.verse1EndSec}");
        }

        var cookingResult = FindObjectOfType<CookingResultSequence>();
        if (cookingResult != null)
        {
            cookingResult.ResetForNewSong();
            cookingResult.enabled = true;
            Debug.Log("[SongLoopController] CookingResultSequence enabled for Song B");
        }

        isTransitioning = false;
        Debug.Log($"[SongLoopController] Transitioned to song {currentSongIndex}: {musicSource.clip.name}");
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