using UnityEngine;
using UnityEngine.Video;

public class ResultCaller : MonoBehaviour
{
    [Header("動画プレイヤー(料理動画)")]
    public VideoPlayer videoPlayer;
    
    [Header("結果UI")]
    public ResultHUD resultHUD;
    public QuickRankingDisplay quickRanking;
    
    [Header("表示までの遅延(秒)")]
    public float delayAfterVideo = 0.35f;
    
    [Header("リザルト動画コントローラー")]
    public ResultVideoController resultVideoController;
    
    [Header("バックアップ(動画が無い/失敗時)")]
    public float fallbackSeconds = 8f;

    bool fired;
    private string _finalRank = "C";

    void Reset()
    {
        if (!videoPlayer) videoPlayer = FindObjectOfType<VideoPlayer>(true);
        if (!resultHUD) resultHUD = FindObjectOfType<ResultHUD>(true);
        if (!quickRanking) quickRanking = FindObjectOfType<QuickRankingDisplay>(true);
        if (!resultVideoController) resultVideoController = FindObjectOfType<ResultVideoController>(true);
    }

    void OnEnable()
    {
#if UNITY_EDITOR
        Debug.Log("[ResultCaller] OnEnable called - autoplay disabled");
#endif
    }
    
    public void TriggerResult()
    {
        _finalRank = CalculateRank();
        Debug.Log($"[ResultCaller] TriggerResult: _finalRank={_finalRank}");

        if (resultVideoController != null)
        {
            resultVideoController.rank = _finalRank;
            Hook();
        }
        else
        {
            if (resultHUD != null)
            {
                Hook();
                ShowResult();
            }
        }
    }

    string CalculateRank()
    {
        var score = ScoreManagerLite.Instance;
        if (score == null) return "C";
        int miss = score.MissCount;
        if (miss == 0) return "S";
        if (miss <= 1) return "A";
        if (miss <= 2) return "B";
        return "C";
    }

    void OnDisable() { Unhook(); }

    void Hook()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;
        else
            Invoke(nameof(FallbackShow), fallbackSeconds);
    }

    void Unhook()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
        CancelInvoke(nameof(FallbackShow));
    }

    void OnVideoFinished(VideoPlayer _)
    {
        if (fired) return;
        fired = true;
        Invoke(nameof(ShowResult), delayAfterVideo);
    }

    void FallbackShow()
    {
        if (fired) return;
        fired = true;
        ShowResult();
    }

    void ShowResult()
    {
        Time.timeScale = 1f;
        if (resultHUD == null) resultHUD = FindObjectOfType<ResultHUD>(true);
        if (resultHUD != null)
        {
            var canvasGroup = resultHUD.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            Invoke(nameof(ShowRanking), 3.0f);
        }
        else
        {
            Debug.LogError("[ResultCaller] ResultHUD が見つかりません。");
        }
    }
    
    void ShowRanking()
    {
        if (!quickRanking) { Debug.LogError("[ResultCaller] quickRanking is null!"); return; }
        var score = ScoreManagerLite.Instance;
        if (score == null) { Debug.LogError("[ResultCaller] ScoreManagerLite.Instance is null!"); return; }

        quickRanking.currentRank = _finalRank;
        Debug.Log($"[ResultCaller] quickRanking.currentRank = {_finalRank}");

        int thisPlayScore = score.CalculateTotalScore();
        ScoreManagerLite.AddToCumulativeScore(thisPlayScore);
        int cumulativeScore = ScoreManagerLite.GetCumulativeScore();
        
        int myRank;
        if (cumulativeScore >= 500000) myRank = Random.Range(1, 3);
        else if (cumulativeScore >= 300000) myRank = Random.Range(2, 5);
        else if (cumulativeScore >= 150000) myRank = Random.Range(3, 7);
        else if (cumulativeScore >= 50000) myRank = Random.Range(5, 10);
        else myRank = Random.Range(8, 20);
        
        string[] playerNames = { "Sakura", "Hiro", "Yuki", "Kaito", "Aoi", "Ren", "Sora", "Mio", "Riku", "Luna", "Kai", "Hana", "Taro", "Yui", "Ken", "Mai" };
        
        int topRank = myRank - 1;
        string topName = playerNames[Random.Range(0, playerNames.Length)];
        var topPlayer = new RankingEntry(topName, cumulativeScore + Random.Range(1000, 5000));
        
        int bottomRank = myRank + 1;
        string bottomName = playerNames[Random.Range(0, playerNames.Length)];
        while (bottomName == topName) bottomName = playerNames[Random.Range(0, playerNames.Length)];
        var bottomPlayer = new RankingEntry(bottomName, cumulativeScore - Random.Range(1000, 5000));
        
        if (resultHUD != null)
        {
            var safeArea = resultHUD.transform.Find("SafeArea");
            if (safeArea != null) safeArea.gameObject.SetActive(false);
        }
        
        quickRanking.ShowRanking(myRank, cumulativeScore, topPlayer, bottomPlayer, topRank, bottomRank);
    }
    
    public void ResetForNewSong()
    {
        fired = false;
        _finalRank = "C";
        CancelInvoke();
        Unhook();
        Debug.Log("[ResultCaller] Reset for new song");
    }
}
