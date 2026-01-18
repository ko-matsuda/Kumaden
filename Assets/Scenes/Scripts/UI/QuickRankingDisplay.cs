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
        
        int sessionCount = ScoreManagerLite.GetSessionCount();
        
        if (sessionCountText != null)
        {
            sessionCountText.text = $"Round {sessionCount}/3";
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
                Debug.LogWarning("[QuickRankingDisplay] AdPhaseManager not found");
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
