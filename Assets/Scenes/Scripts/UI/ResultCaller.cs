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
        string rank = "C";
        var scoreManager = FindObjectOfType<ScoreManagerLite>();
        if (scoreManager != null)
        {
            int perfect = scoreManager.PerfectCount;
            int good = scoreManager.GoodCount;
            int miss = scoreManager.MissCount;
            int total = perfect + good + miss;
            
            if (total > 0)
            {
                float perfectRate = (float)perfect / total;
                if (perfectRate >= 0.95f && miss == 0) rank = "S";
                else if (perfectRate >= 0.85f) rank = "A";
                else if (perfectRate >= 0.70f) rank = "B";
                else rank = "C";
            }
        }

        if (!string.IsNullOrEmpty(rank) && rank != "F")
        {
            if (resultVideoController != null)
            {
                Hook();
            }
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
        
        int totalNotes = score.PerfectCount + score.GoodCount + score.MissCount;
        if (totalNotes == 0) return "C";
        
        float perfectRate = (float)score.PerfectCount / totalNotes;
        
        if (perfectRate >= 0.95f) return "S";
        if (perfectRate >= 0.85f) return "A";
        if (perfectRate >= 0.70f) return "B";
        return "C";
    }

    void OnDisable()
    {
        Unhook();
    }

    void Hook()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        else
        {
            Invoke(nameof(FallbackShow), fallbackSeconds);
        }
    }

    void Unhook()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
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

        if (resultHUD == null)
        {
            resultHUD = FindObjectOfType<ResultHUD>(true);
        }

        if (resultHUD != null)
        {
            var canvasGroup = resultHUD.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            Invoke(nameof(ShowRanking), 5.0f);
        }
        else
        {
            Debug.LogError("[ResultCaller] ResultHUD が見つかりません。ResultCanvas に ResultHUD を付けてください。");
        }
    }
    
    void ShowRanking()
    {
        if (!quickRanking)
        {
            Debug.LogError("[ResultCaller] quickRanking is null!");
            return;
        }

        var score = ScoreManagerLite.Instance;
        if (score == null)
        {
            Debug.LogError("[ResultCaller] ScoreManagerLite.Instance is null!");
            return;
        }

        int thisPlayScore = score.CalculateTotalScore();
        ScoreManagerLite.AddToCumulativeScore(thisPlayScore);
        int cumulativeScore = ScoreManagerLite.GetCumulativeScore();
        
        int myRank;
        if (cumulativeScore >= 500000) myRank = Random.Range(1, 3);
        else if (cumulativeScore >= 300000) myRank = Random.Range(2, 5);
        else if (cumulativeScore >= 150000) myRank = Random.Range(3, 7);
        else if (cumulativeScore >= 50000) myRank = Random.Range(5, 10);
        else myRank = Random.Range(8, 20);
        
        string[] playerNames = {
            "Sakura", "Hiro", "Yuki", "Kaito", "Aoi", "Ren", "Sora", "Mio",
            "Riku", "Luna", "Kai", "Hana", "Taro", "Yui", "Ken", "Mai"
        };
        
        int topRank = myRank - 1;
        int topScoreDiff = Random.Range(1000, 5000);
        string topName = playerNames[Random.Range(0, playerNames.Length)];
        var topPlayer = new RankingEntry(topName, cumulativeScore + topScoreDiff);
        
        int bottomRank = myRank + 1;
        int bottomScoreDiff = Random.Range(1000, 5000);
        string bottomName = playerNames[Random.Range(0, playerNames.Length)];
        while (bottomName == topName)
        {
            bottomName = playerNames[Random.Range(0, playerNames.Length)];
        }
        var bottomPlayer = new RankingEntry(bottomName, cumulativeScore - bottomScoreDiff);
        
        if (resultHUD != null)
        {
            var safeArea = resultHUD.transform.Find("SafeArea");
            if (safeArea != null)
            {
                safeArea.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[ResultCaller] SafeArea not found");
            }
        }
        
        quickRanking.ShowRanking(myRank, cumulativeScore, topPlayer, bottomPlayer, topRank, bottomRank);
    }
    
    private void OnRetry()
    {
        GameFlags.SkipPrologueOnce = true;
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }


public void ResetForNewSong()
    {
        fired = false;
        CancelInvoke();
        Unhook();
        Debug.Log("[ResultCaller] Reset for new song");
    }
}
