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
        Debug.Log($"[ResultVideoController] PlayResultVideo called - rank={rank}");
        
        if (videoPlayed) return;
        if (videoPlayer == null || resultSuccess == null || resultFail == null)
        {
            Debug.LogError("[ResultVideoController] Not initialized!");
            return;
        }
        
        // Rankに応じて動画を選択
        if (rank == "S" || rank == "A" || rank == "B")
        {
            videoPlayer.clip = resultSuccess;
            Debug.Log("[ResultVideoController] Playing SUCCESS video");
        }
        else
        {
            videoPlayer.clip = resultFail;
            Debug.Log("[ResultVideoController] Playing FAIL video");
        }
        
        videoPlayer.Play();
        videoPlayed = true;
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
        ShowResult();
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
            resultHUD.ShowResult();
        }
    }
    
    void ShowRanking()
    {
        // SafeAreaを非表示
        var safeArea = GameObject.Find("SafeArea");
        if (safeArea != null)
        {
            var canvasGroup = safeArea.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = safeArea.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            Debug.Log("[ResultVideoController] SafeArea hidden");
        }
        
        // ランキングを表示
        if (quickRanking != null)
        {
            var score = ScoreManagerLite.Instance;
            if (score != null)
            {
                int myScore = score.PerfectCount * 100 + score.GoodCount * 50;
                int myRank = Random.Range(3, 8);
                
                var topPlayer = new RankingEntry("Player_" + (char)('A' + Random.Range(0, 26)), myScore + Random.Range(100, 500));
                var bottomPlayer = new RankingEntry("Player_" + (char)('A' + Random.Range(0, 26)), myScore - Random.Range(100, 500));
                
                quickRanking.ShowRanking(myRank, myScore, topPlayer, bottomPlayer);
                Debug.Log("[ResultVideoController] QuickRanking shown");
            }
        }
        else
        {
            Debug.LogError("[ResultVideoController] QuickRanking is null!");
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
