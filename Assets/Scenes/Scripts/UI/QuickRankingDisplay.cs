using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickRankingDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI rankTopRank;
    [SerializeField] private TextMeshProUGUI rankTopName;
    [SerializeField] private TextMeshProUGUI rankTopScore;
    
    [SerializeField] private TextMeshProUGUI rankSelfRank;
    [SerializeField] private TextMeshProUGUI rankSelfName;
    [SerializeField] private TextMeshProUGUI rankSelfScore;
    
    [SerializeField] private TextMeshProUGUI rankBottomRank;
    [SerializeField] private TextMeshProUGUI rankBottomName;
    [SerializeField] private TextMeshProUGUI rankBottomScore;
    
    [SerializeField] private TextMeshProUGUI sessionCountText;
    [SerializeField] private Button retryButton;
    
    private CanvasGroup canvasGroup;
    private Color selfColor;
    private Color otherColor;
    
    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        selfColor = Color.yellow;
        otherColor = Color.white;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetry);
        }
        else
        {
            Debug.LogWarning("[QuickRankingDisplay] Retry button not assigned");
        }
    }
    
    public void ShowRanking(int myRank, int myScore, RankingEntry topPlayer, RankingEntry bottomPlayer, int topRank, int bottomRank)
    {
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
            if (!resultCanvasTransform.gameObject.activeSelf)
            {
                resultCanvasTransform.gameObject.SetActive(true);
            }
            
            var resultCanvasGroup = resultCanvasTransform.GetComponent<CanvasGroup>();
            if (resultCanvasGroup != null)
            {
                resultCanvasGroup.alpha = 1f;
                resultCanvasGroup.interactable = true;
                resultCanvasGroup.blocksRaycasts = true;
            }
        }
        
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        var selfCanvasGroup = GetComponent<CanvasGroup>();
        if (selfCanvasGroup != null)
        {
            selfCanvasGroup.alpha = 1f;
            selfCanvasGroup.interactable = true;
            selfCanvasGroup.blocksRaycasts = true;
        }

        SetRankLine(rankTopRank, rankTopName, rankTopScore, topRank, topPlayer.playerName, topPlayer.score, otherColor);
        SetRankLine(rankSelfRank, rankSelfName, rankSelfScore, myRank, "YOU", myScore, selfColor);
        SetRankLine(rankBottomRank, rankBottomName, rankBottomScore, bottomRank, bottomPlayer.playerName, bottomPlayer.score, otherColor);
        
        // DifficultyManager のラウンド情報を使用
        int currentRound = 1;
        if (DifficultyManager.Instance != null)
        {
            currentRound = DifficultyManager.Instance.GetCurrentRound();
        }
        
        if (sessionCountText != null)
        {
            sessionCountText.text = $"Round {currentRound}/3";
        }
    }
    
    private void SetRankLine(TextMeshProUGUI rankText, TextMeshProUGUI nameText, TextMeshProUGUI scoreText, 
                              int rank, string name, int score, Color color)
    {
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
    
    private void OnRetry()
    {
        HandleRetryLogic();
    }
    
    private void HandleRetryLogic()
    {
        // DifficultyManager のラウンドシステムを使用
        if (DifficultyManager.Instance != null)
        {
            int currentRound = DifficultyManager.Instance.GetCurrentRound();
            
            Debug.Log($"[QuickRankingDisplay] Current Round: {currentRound}");
            
            // Round 3 終了後は広告表示してから次のラウンドへ
            if (currentRound >= 3)
            {
                Debug.Log("[QuickRankingDisplay] Round 3 完了！広告表示後、Round 1 へ");
                
                var adManager = AdPhaseManager.Instance;
                
                if (adManager != null)
                {
                    adManager.ShowAd(() => 
                    {
                        // 広告終了後に次のラウンドへ（自動的に Round 1 に戻る）
                        DifficultyManager.Instance.NextRound();
                        LoadMainScene();
                    });
                }
                else
                {
                    Debug.LogWarning("[QuickRankingDisplay] AdPhaseManager not found");
                    DifficultyManager.Instance.NextRound();
                    LoadMainScene();
                }
            }
            else
            {
                // Round 1 または 2 の場合は広告なしで次のラウンドへ
                Debug.Log($"[QuickRankingDisplay] Round {currentRound} → Round {currentRound + 1}");
                DifficultyManager.Instance.NextRound();
                LoadMainScene();
            }
        }
        else
        {
            // DifficultyManager がない場合は旧システムにフォールバック
            Debug.LogWarning("[QuickRankingDisplay] DifficultyManager not found, using fallback");
            
            if (ScoreManagerLite.ShouldShowAd())
            {
                var adManager = AdPhaseManager.Instance;
                
                if (adManager != null)
                {
                    adManager.ShowAd(() => {
                        LoadMainScene();
                    });
                }
                else
                {
                    ScoreManagerLite.ResetSessionCount();
                    LoadMainScene();
                }
            }
            else
            {
                ScoreManagerLite.IncrementSessionCount();
                LoadMainScene();
            }
        }
    }
    
    private void LoadMainScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
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
