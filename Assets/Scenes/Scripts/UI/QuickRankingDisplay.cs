using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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
    
    [Header("SE設定")]
    [SerializeField] private AudioClip retryClickSE;
    
    private CanvasGroup canvasGroup;
    private Color selfColor;
    private Color otherColor;
    private AudioSource audioSource;
    
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
        
        // AudioSource を取得または追加
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && retryButton != null)
        {
            audioSource = retryButton.GetComponent<AudioSource>();
        }
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 1.0f;
        
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
        // SE再生してから処理を実行
        StartCoroutine(PlaySEAndRetry());
    }
    
private IEnumerator PlaySEAndRetry()
    {
        // ボタンを無効化（連打防止）
        if (retryButton != null)
        {
            retryButton.interactable = false;
        }
        
        // SE再生（待たずにすぐ次へ）
        if (retryClickSE != null && audioSource != null)
        {
            audioSource.PlayOneShot(retryClickSE);
        }
        
        yield return null;
        
        // Retry処理実行
        HandleRetryLogic();
    }
    
private void HandleRetryLogic()
    {
        if (DifficultyManager.Instance == null)
        {
            Debug.LogWarning("[QuickRankingDisplay] DifficultyManager not found, fallback");
            LoadMainScene();
            return;
        }

        int currentRound = DifficultyManager.Instance.GetCurrentRound();
        Debug.Log($"[QuickRankingDisplay] Current Round: {currentRound}");

        if (currentRound >= 3)
        {
            // Round 3 完了 → 広告表示 → Round 1 へ
            Debug.Log("[QuickRankingDisplay] Round 3 完了！広告表示後、Round 1 へ");

            var adManager = AdMobRewardedManager.Instance;
            if (adManager != null)
            {
                adManager.ShowAd(() =>
                {
                    DifficultyManager.Instance.SetRound(1);
                    LoadMainScene();
                });
            }
            else
            {
                Debug.LogWarning("[QuickRankingDisplay] AdMobRewardedManager not found");
                DifficultyManager.Instance.SetRound(1);
                LoadMainScene();
            }
        }
        else
        {
            // Round 1, 2 → 広告なしで次へ
            Debug.Log($"[QuickRankingDisplay] Round {currentRound} → Round {currentRound + 1}");
            DifficultyManager.Instance.SetRound(currentRound + 1);
            LoadMainScene();
        }
    }
    
private void LoadMainScene()
    {
        GameFlags.SkipPrologueOnce = true;
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