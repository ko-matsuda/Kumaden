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
                resultVideoController.rank = rank;
                
                Hook();
                resultVideoController.PlayResultVideo();
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
        Time.timeScale = 1f;

        if (resultHUD == null)
        {
            resultHUD = FindObjectOfType<ResultHUD>(true);
        }

        if (resultHUD != null)
        {
            resultHUD.gameObject.SetActive(true);
            resultHUD.ShowResult();
            
            // 3秒後にランキング表示
            Invoke(nameof(ShowRanking), 7.0f);
        }
        else
        {
            Debug.LogError("[ResultCaller] ResultHUD が見つかりません。ResultCanvas に ResultHUD を付けてください。");
        }
    }
    
void ShowRanking()
    {
        // SafeAreaを非表示（CanvasGroupのアルファで制御）
        var safeArea = GameObject.Find("SafeArea");
        if (safeArea != null)
        {
            var safeAreaCanvas = safeArea.GetComponent<CanvasGroup>();
            if (safeAreaCanvas == null)
            {
                safeAreaCanvas = safeArea.AddComponent<CanvasGroup>();
            }
            safeAreaCanvas.alpha = 0f;
            safeAreaCanvas.interactable = false;
            safeAreaCanvas.blocksRaycasts = false;
            
            Debug.Log("[ResultCaller] SafeArea hidden via CanvasGroup");
        }
        
        // ResultCanvasのalphaは消さない（QuickRankingが表示されるため）
        
        // ランキングを表示（ずっと表示）
        if (quickRanking != null)
        {
            var score = ScoreManagerLite.Instance;
            if (score != null)
            {
                // スコア計算（コンボボーナス含む）
                int myScore = score.CalculateTotalScore();
                int myRank = Random.Range(3, 8);
                
                var topPlayer = new RankingEntry("Player_" + (char)('A' + Random.Range(0, 26)), myScore + Random.Range(100, 500));
                var bottomPlayer = new RankingEntry("Player_" + (char)('A' + Random.Range(0, 26)), myScore - Random.Range(100, 500));
                
                quickRanking.ShowRanking(myRank, myScore, topPlayer, bottomPlayer);
                Debug.Log($"[ResultCaller] QuickRanking.ShowRanking() called - myScore={myScore}");
            }
            else
            {
                Debug.LogError("[ResultCaller] ScoreManagerLite.Instance is null!");
            }
        }
        else
        {
            Debug.LogError("[ResultCaller] quickRanking is null!");
        }
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
