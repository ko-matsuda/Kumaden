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
    
    private Color selfColor = Color.white;
    private Color otherColor;

private void Awake()
    {
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
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetry);
        }
    }

public void ShowRanking(int myRank, int myScore, RankingEntry topPlayer, RankingEntry bottomPlayer)
    {
        Debug.Log("[QuickRankingDisplay] ShowRanking called");
        
        // Retryボタンのイベント再設定（Awakeが呼ばれていない場合のため）
        if (retryButton != null)
        {
            Debug.Log($"[QuickRankingDisplay] Setting up retry button - interactable={retryButton.interactable}");
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetry);
            Debug.Log("[QuickRankingDisplay] Retry button onClick set");
        }
        else
        {
            Debug.LogWarning("[QuickRankingDisplay] retryButton is null!");
        }
        
        StartCoroutine(DisplaySequence(myRank, myScore, topPlayer, bottomPlayer));
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
        Debug.Log("[QuickRankingDisplay] OnRetry called - skipping prologue");
        
        // Retry時はプロローグをスキップ
        GameFlags.SkipPrologueOnce = true;
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainSceneName);
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