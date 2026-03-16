using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// リザルト動画を Rank に応じて出し分けるコントローラー
/// S/A/B: Result.mp4（成功）
/// C: Result_Fail.mp4（失敗）
/// </summary>
public class ResultVideoController : MonoBehaviour
{
    [Header("Video Player")]
    public VideoPlayer videoPlayer;
    
    [Header("Video Clips")]
    public VideoClip resultSuccess;  // S/A/B用
    public VideoClip resultFail;     // C用
        private bool isPlayingResultVideo = false;  // リザルト動画再生中フラグ
    

    
    [Header("Result UI")]
    public ResultHUD resultHUD;
    public QuickRankingDisplay quickRanking;
    
    [Header("Rank (外部から設定される)")]
    public string rank = "";
    
    [Header("表示までの遅延(秒)")]
    public float delayAfterVideo = 0.35f;
    
    private bool videoPlayed = false;

void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        // プロローグ動画に反応しないよう、Awakeではフックしない
        // PlayResultVideo()が呼ばれた時にフックする
        
        Debug.Log($"[ResultVideoController] Awake - videoPlayer={videoPlayer.name} ({videoPlayer.GetType().Name}), resultSuccess={resultSuccess.name} ({resultSuccess.GetType().Name}), resultFail={resultFail.name} ({resultFail.GetType().Name})");
    }

    void Start()
    {
        if (resultHUD == null)
        {
            resultHUD = FindObjectOfType<ResultHUD>(true);
        }
        
        if (quickRanking == null)
        {
            quickRanking = FindObjectOfType<QuickRankingDisplay>(true);
        }
        
        Debug.Log($"[ResultVideoController] Start - resultHUD={resultHUD}, quickRanking={quickRanking}");
    }

    public void PlayResultVideo()
    {
        Debug.Log("[ResultVideoController] ===== PlayResultVideo CALLED =====");
        Debug.Log($"[ResultVideoController] PlayResultVideo called - rank={rank}");
        
        // 動画再生前にResultCanvasの背景を表示
        if (resultHUD != null)
        {
            var resultCanvasTransform = resultHUD.transform.root.Find("ResultCanvas");
            if (resultCanvasTransform != null)
            {
                resultCanvasTransform.gameObject.SetActive(true);
                var canvasGroup = resultCanvasTransform.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
                Debug.Log("[ResultVideoController] ResultCanvas background displayed before video");
            }
        }
        
        if (!videoPlayer || !resultSuccess || !resultFail)
        {
            Debug.LogError("[ResultVideoController] Not initialized!");
            return;
        }

        isPlayingResultVideo = true;
        // videoPlayer.loopPointReached += OnVideoFinished; // CookingResultSequenceが処理する
        
        VideoClip targetClip = (rank == "S" || rank == "A" || rank == "B") ? resultSuccess : resultFail;
        Debug.Log($"[ResultVideoController] Selected video clip: {targetClip.name}");
        
        var cleaner = GetComponent<VideoPlayerCleaner>();
        if (cleaner != null)
        {
            cleaner.PlayClean(targetClip);
        }
        else
        {
            videoPlayer.clip = targetClip;
            videoPlayer.Prepare();
            videoPlayer.Play();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        // プロローグ動画は無視
        if (vp.clip != resultSuccess && vp.clip != resultFail)
        {
            Debug.Log("[ResultVideoController] OnVideoFinished - but not result video, ignoring");
            return;
        }
        
        Debug.Log("[ResultVideoController] OnVideoFinished - showing result UI");
        // ShowResult(); // CookingResultSequenceが処理する
    }

void ShowResult()
    {
        // ResultCanvasを先にアクティブ化してからコルーチンを開始
        if (resultHUD != null && resultHUD.gameObject != null && !resultHUD.gameObject.activeSelf)
        {
            resultHUD.gameObject.SetActive(true);
        }
        
        if (resultHUD != null)
        {
                    
        // ResultCanvasの背景を表示
        var resultCanvasTransform = resultHUD.transform.root.Find("ResultCanvas");
        if (resultCanvasTransform != null)
        {
            resultCanvasTransform.gameObject.SetActive(true);
            var canvasGroup = resultCanvasTransform.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            Debug.Log("[ResultVideoController] ResultCanvas background displayed");
        }
        
resultHUD.ShowResult();
        }
    }
    
private string CalcRankFromScore()
    {
        var score = ScoreManagerLite.Instance;
        if (score == null) return "C";
        int total = score.PerfectCount + score.GoodCount + score.MissCount;
        if (total == 0) return "C";
        float r = (float)score.PerfectCount / total;
        if (r >= 0.95f && score.MissCount == 0) return "S";
        if (r >= 0.85f) return "A";
        if (r >= 0.70f) return "B";
        return "C";
    }

    
void ShowRanking()
    {
        if (!quickRanking)
        {
            Debug.LogError("[ResultVideoController] quickRanking is null!");
            return;
        }

        var score = ScoreManagerLite.Instance;
        if (score == null)
        {
            Debug.LogError("[ResultVideoController] ScoreManagerLite.Instance is null!");
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
                Debug.Log("[ResultVideoController] SafeArea hidden");
            }
            else
            {
                Debug.LogWarning("[ResultVideoController] SafeArea not found");
            }
        }
        
        // ResultCaller.TriggerResult()でセット済みのcurrentRankをそのまま使用（上書きしない）
        // rankフィールドが未設定の場合のみフォールバック
        if (string.IsNullOrEmpty(quickRanking.currentRank) || quickRanking.currentRank == "")
        {
            quickRanking.currentRank = rank;
            Debug.Log($"[ResultVideoController] currentRank fallback={rank}");
        }
        else
        {
            Debug.Log($"[ResultVideoController] currentRank already set={quickRanking.currentRank}, not overwriting");
        }
        quickRanking.ShowRanking(myRank, myScore, topPlayer, bottomPlayer, topRank, bottomRank);
        Debug.Log($"[ResultVideoController] QuickRanking.ShowRanking() called - myRank={myRank}, myScore={myScore}");
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
