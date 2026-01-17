using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 高速ゲームサイクル前提の簡易ランキング表示
/// 0.8〜1.2秒の自動表示・自動終了
/// 3カラムレイアウト（順位・名前・スコア）
/// </summary>
public class QuickRankingDisplay : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private TextMeshProUGUI sessionCountText;
    
    
[SerializeField] private TextMeshProUGUI titleText;
    
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Top Rank")]
    [SerializeField] private TextMeshProUGUI rankTopRank;
    [SerializeField] private TextMeshProUGUI rankTopName;
    [SerializeField] private TextMeshProUGUI rankTopScore;
    
    [Header("Self Rank")]
    [SerializeField] private TextMeshProUGUI rankSelfRank;
    [SerializeField] private TextMeshProUGUI rankSelfName;
    [SerializeField] private TextMeshProUGUI rankSelfScore;
    
    [Header("Bottom Rank")]
    [SerializeField] private TextMeshProUGUI rankBottomRank;
    [SerializeField] private TextMeshProUGUI rankBottomName;
    [SerializeField] private TextMeshProUGUI rankBottomScore;
    
        [Header("Buttons")]
    [SerializeField] private UnityEngine.UI.Button retryButton;
    [SerializeField] private string mainSceneName = "Main";
[Header("Display Settings")]
    [SerializeField, Range(0.05f, 0.15f)] private float fadeInDuration = 0.1f;
    [SerializeField, Range(0.6f, 1.2f)] private float holdDuration = 0.8f;
    [SerializeField, Range(0.05f, 0.15f)] private float fadeOutDuration = 0.1f;
    [SerializeField, Range(0.6f, 0.8f)] private float otherPlayerAlpha = 0.65f;
    
    private Color selfColor = Color.yellow;
    private Color otherColor;

private void Awake()
    {
        Debug.Log("[QuickRankingDisplay] Awake called");
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        // 自分：黄色、他：白
        selfColor = Color.yellow;
        otherColor = Color.white;
        
        
if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        
        // Retryボタンのイベント設定
        if (retryButton != null)
        {
            Debug.Log("[QuickRankingDisplay] Retry button found, setting up listener");
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetry);
        }
        else
        {
            Debug.LogWarning("[QuickRankingDisplay] Retry button is NULL in Awake!");
        }
    }

public void ShowRanking(int myRank, int myScore, RankingEntry topPlayer, RankingEntry bottomPlayer, int topRank, int bottomRank)
    {
        Debug.Log("[QuickRankingDisplay] ShowRanking called");
        
        // ResultCanvasを探してアクティブにする
        Transform resultCanvasTransform = null;
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.name == "ResultCanvas")
            {
                resultCanvasTransform = current;
                break;
            }
            current = current.parent;
        }
        
        if (resultCanvasTransform != null)
        {
            // ResultCanvasをアクティブにする
            if (!resultCanvasTransform.gameObject.activeSelf)
            {
                resultCanvasTransform.gameObject.SetActive(true);
                Debug.Log("[QuickRankingDisplay] ResultCanvas activated");
            }
            
            // ResultCanvasのCanvasGroupのalphaを1にする
            var resultCanvasGroup = resultCanvasTransform.GetComponent<CanvasGroup>();
            if (resultCanvasGroup != null)
            {
                resultCanvasGroup.alpha = 1f;
                resultCanvasGroup.interactable = true;
                resultCanvasGroup.blocksRaycasts = true;
                Debug.Log("[QuickRankingDisplay] ResultCanvas CanvasGroup alpha set to 1");
            }
        }
        
        // GameObjectを確実にアクティブにする
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
            Debug.Log("[QuickRankingDisplay] GameObject activated");
        }
        
        // QuickRanking自身のCanvasGroupのalphaを1にする
        var selfCanvasGroup = GetComponent<CanvasGroup>();
        if (selfCanvasGroup != null)
        {
            selfCanvasGroup.alpha = 1f;
            selfCanvasGroup.interactable = true;
            selfCanvasGroup.blocksRaycasts = true;
            Debug.Log("[QuickRankingDisplay] Self CanvasGroup alpha set to 1");
        }

        Color selfColor = Color.yellow;
        Color otherColor = Color.white;

        SetRankLine(rankTopRank, rankTopName, rankTopScore, topRank, topPlayer.playerName, topPlayer.score, otherColor);
        SetRankLine(rankSelfRank, rankSelfName, rankSelfScore, myRank, "YOU", myScore, selfColor);
        SetRankLine(rankBottomRank, rankBottomName, rankBottomScore, bottomRank, bottomPlayer.playerName, bottomPlayer.score, otherColor);
        
        // サイクル数表示のみ
        int sessionCount = ScoreManagerLite.GetSessionCount();
        
        if (sessionCountText != null)
        {
            sessionCountText.text = $"Round {sessionCount}/3";
        }
        
        Debug.Log($"[QuickRankingDisplay] Ranking displayed: top={topRank}, self={myRank}, bottom={bottomRank}, session={sessionCount}/3, cumulative={myScore}");
    }

private IEnumerator DisplaySequence(int myRank, int myScore, RankingEntry topPlayer, RankingEntry bottomPlayer)
    {
        // フェードイン
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        
        // 重要: ボタンを押せるようにする
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        Debug.Log("[QuickRankingDisplay] CanvasGroup interaction enabled");
        
        // タイトル表示
        if (titleText != null)
        {
            titleText.text = "RANKING";
        }
        
        // ランキング情報表示（自分＝黄色、他＝白）
        SetRankLine(rankTopRank, rankTopName, rankTopScore, 2, topPlayer.playerName, topPlayer.score, otherColor);
        SetRankLine(rankSelfRank, rankSelfName, rankSelfScore, myRank, "YOU", myScore, selfColor);
        SetRankLine(rankBottomRank, rankBottomName, rankBottomScore, 4, bottomPlayer.playerName, bottomPlayer.score, otherColor);
    }
    
private void SetRankLine(TextMeshProUGUI rankText, TextMeshProUGUI nameText, TextMeshProUGUI scoreText, 
                              int rank, string name, int score, Color color)
    {
        Debug.Log($"[QuickRankingDisplay] SetRankLine called - rank={rank}, name={name}, color={color}");
        
        if (rankText != null)
        {
            rankText.text = $"#{rank}";
            rankText.color = color;
        }
        
        if (nameText != null)
        {
            nameText.text = name;
            nameText.color = color;
        }
        
        if (scoreText != null)
        {
            scoreText.text = score.ToString("N0");
            scoreText.color = color;
        }
    }
    
    private void HideRankLine(TextMeshProUGUI rankText, TextMeshProUGUI nameText, TextMeshProUGUI scoreText)
    {
        if (rankText != null) rankText.text = "";
        if (nameText != null) nameText.text = "";
        if (scoreText != null) scoreText.text = "";
    }
    
        
private void OnRetry()
    {
        Debug.Log("[QuickRankingDisplay] ===== OnRetry called =====");
        Debug.Log($"[QuickRankingDisplay] Current session count: {ScoreManagerLite.GetSessionCount()}");
        Debug.Log($"[QuickRankingDisplay] Should show ad: {ScoreManagerLite.ShouldShowAd()}");
        
        // アニメーションをスキップして即座に処理（シーンリロードを防ぐ）
        HandleRetryLogic();
    }

private void LoadMainScene()
    {
        // Retry時はプロローグをスキップ
        GameFlags.SkipPrologueOnce = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainSceneName);
    }


private void HandleRetryLogic()
    {
        Debug.Log("[QuickRankingDisplay] HandleRetryLogic called");
        
        // 先に広告判定（カウント加算前）
        if (ScoreManagerLite.ShouldShowAd())
        {
            Debug.Log("[QuickRankingDisplay] 3 sessions completed - showing ad phase");
            
            var adManager = AdPhaseManager.Instance;
            Debug.Log($"[QuickRankingDisplay] AdPhaseManager.Instance = {adManager}");
            
            if (adManager != null)
            {
                adManager.ShowAd(() => {
                    Debug.Log("[QuickRankingDisplay] Ad completed - loading next cycle");
                    LoadMainScene();
                });
            }
            else
            {
                Debug.LogWarning("[QuickRankingDisplay] AdPhaseManager not found - skipping ad");
                ScoreManagerLite.ResetSessionCount();
                LoadMainScene();
            }
        }
        else
        {
            Debug.Log("[QuickRankingDisplay] Normal retry - incrementing session count");
            // 通常リトライ（カウント加算）
            ScoreManagerLite.IncrementSessionCount();
            LoadMainScene();
        }
    }
    
    
private IEnumerator RetryButtonScaleAnimation()
    {
        Transform buttonTransform = retryButton.transform;
        Vector3 originalScale = buttonTransform.localScale;
        
        // 拡大 (0.1秒)
        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            buttonTransform.localScale = Vector3.Lerp(originalScale, originalScale * 1.2f, t);
            yield return null;
        }
        
        // 縮小 (0.1秒)
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            buttonTransform.localScale = Vector3.Lerp(originalScale * 1.2f, originalScale, t);
            yield return null;
        }
        
        buttonTransform.localScale = originalScale;
        
        // ロジック実行
        HandleRetryLogic();
    }

public void ForceHide()
    {
        StopAllCoroutines();
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}

[System.Serializable]
public class RankingEntry
{
    public string playerName;
    public int score;
    
    public RankingEntry(string name, int score)
    {
        this.playerName = name;
        this.score = score;
    }
}