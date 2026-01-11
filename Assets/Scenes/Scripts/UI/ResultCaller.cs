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
        // OnEnableでの自動再生を無効化
        // ゲーム終了時に明示的にTriggerResult()を呼ぶこと
        Debug.Log("[ResultCaller] OnEnable called - autoplay disabled");
    }
    
    // ゲーム終了時に呼ぶメソッド
public void TriggerResult()
    {
        Debug.Log("[ResultCaller] TriggerResult called");
        
        // ScoreManagerLiteからランクを取得
        string rank = "C"; // デフォルト
        var scoreManager = FindObjectOfType<ScoreManagerLite>();
        if (scoreManager != null)
        {
            // ScoreManagerLiteからスコア情報を取得してランクを計算
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
        
        Debug.Log($"[ResultCaller] rank={rank}");

        if (!string.IsNullOrEmpty(rank) && rank != "F")
        {
            Debug.Log($"[ResultCaller] rank={rank}, playing result video");
            
            if (resultVideoController != null)
            {
                            
Debug.Log($"[ResultCaller] Calling PlayResultVideo, controller={resultVideoController.name} ({resultVideoController.GetType().Name})");
                
                // ResultVideoControllerにrankを設定
                // resultVideoController.rank = rank; // CookingResultSequenceが処理する
                
                Hook();
                // resultVideoController.PlayResultVideo(); // CookingResultSequenceが処理する
            }
        }
        else
        {
            Debug.Log($"[ResultCaller] rank={rank}, showing result directly");
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
        Debug.Log("[ResultCaller] ===== ShowResult CALLED =====");
        Time.timeScale = 1f;

        if (resultHUD == null)
        {
            resultHUD = FindObjectOfType<ResultHUD>(true);
        }

        if (resultHUD != null)
        {
                    
                // ResultCanvasの背景を表示（resultHUDはResultCanvasに直接アタッチされている）
        // resultHUD.gameObject.SetActive(true); // CookingResultSequenceが処理する
        var canvasGroup = resultHUD.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Debug.Log("[ResultCaller] ResultCanvas background displayed");
        }
        
        // resultHUD.gameObject.SetActive(true); // CookingResultSequenceが処理する
            // resultHUD.ShowResult(); // CookingResultSequenceが処理する
            
            // 3秒後にランキング表示
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

        int myScore = score.CalculateTotalScore();
        
        // スコアに基づいて順位を決定（より高いスコア = より良い順位）
        int myRank;
        if (myScore >= 100000) myRank = Random.Range(1, 3);      // 10万点以上: 1-2位
        else if (myScore >= 80000) myRank = Random.Range(2, 5);  // 8万点以上: 2-4位
        else if (myScore >= 60000) myRank = Random.Range(3, 7);  // 6万点以上: 3-6位
        else if (myScore >= 40000) myRank = Random.Range(5, 10); // 4万点以上: 5-9位
        else myRank = Random.Range(8, 15);                       // それ以下: 8-14位
        
        // 自然な名前リスト
        string[] playerNames = {
            "Sakura", "Hiro", "Yuki", "Kaito", "Aoi", "Ren", "Sora", "Mio",
            "Riku", "Luna", "Kai", "Hana", "Taro", "Yui", "Ken", "Mai"
        };
        
        // 上位プレイヤー（myRankの1つ上）
        int topRank = myRank - 1;
        int topScoreDiff = Random.Range(200, 800);  // 200〜800点差
        string topName = playerNames[Random.Range(0, playerNames.Length)];
        var topPlayer = new RankingEntry(topName, myScore + topScoreDiff);
        
        // 下位プレイヤー（myRankの1つ下）
        int bottomRank = myRank + 1;
        int bottomScoreDiff = Random.Range(200, 800);  // 200〜800点差
        string bottomName = playerNames[Random.Range(0, playerNames.Length)];
        // 同じ名前を避ける
        while (bottomName == topName)
        {
            bottomName = playerNames[Random.Range(0, playerNames.Length)];
        }
        var bottomPlayer = new RankingEntry(bottomName, myScore - bottomScoreDiff);
        
        // SafeAreaを非表示にする（リザルト画面の中身）
        if (resultHUD != null)
        {
            var safeArea = resultHUD.transform.Find("SafeArea");
            if (safeArea != null)
            {
                safeArea.gameObject.SetActive(false);
                Debug.Log("[ResultCaller] SafeArea hidden");
            }
            else
            {
                Debug.LogWarning("[ResultCaller] SafeArea not found");
            }
        }
        
        quickRanking.ShowRanking(myRank, myScore, topPlayer, bottomPlayer, topRank, bottomRank);
        Debug.Log($"[ResultCaller] QuickRanking.ShowRanking() called - myRank={myRank}, myScore={myScore}");
    }
    
    private void OnRetry()
    {
        Debug.Log("[ResultCaller] OnRetry called - skipping prologue");
        
        // Retry時はプロローグをスキップ
        GameFlags.SkipPrologueOnce = true;
        
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }
}
