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
    // 現在のランク（ResultCallerからセットされる）
    [HideInInspector] public string currentRank = "C";

    
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
                resultCanvasTransform.gameObject.SetActive(true);
            
            var resultCanvasGroup = resultCanvasTransform.GetComponent<CanvasGroup>();
            if (resultCanvasGroup != null)
            {
                resultCanvasGroup.alpha = 1f;
                resultCanvasGroup.interactable = true;
                resultCanvasGroup.blocksRaycasts = true;
            }
        }
        
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);
        
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
        
        // 難易度ラベル表示
        int currentRound = 1;
        if (DifficultyManager.Instance != null)
            currentRound = DifficultyManager.Instance.GetCurrentRound();
        
        if (sessionCountText != null)
        {
            string[] difficultyLabels = { "Easy", "Normal", "Hard" };
            sessionCountText.text = difficultyLabels[Mathf.Clamp(currentRound - 1, 0, 2)];
        }

        // ボタンテキストをランクで変える（currentRankはResultCallerから事前にセット済み）
        bool isSuccess = (currentRank == "S" || currentRank == "A" || currentRank == "B");
        Debug.Log($"[QuickRankingDisplay] ShowRanking - currentRank={currentRank}, isSuccess={isSuccess}");
        if (retryButton != null)
        {
            if (retryButton != null) retryButton.interactable = true;
            var btnText = retryButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = isSuccess ? "NEXT" : "RETRY";
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
        bool isSuccess = (currentRank == "S" || currentRank == "A" || currentRank == "B");
        Debug.Log($"[QuickRankingDisplay] HandleRetryLogic - Round:{currentRound}, Rank:{currentRank}, isSuccess:{isSuccess}");

        if (!isSuccess)
        {
            // 失敗 → 3回に1回広告を表示してリトライ
            int retryCount = PlayerPrefs.GetInt("RetryCount", 0) + 1;
            PlayerPrefs.SetInt("RetryCount", retryCount);
            PlayerPrefs.Save();
            Debug.Log($"[QuickRankingDisplay] リトライ {retryCount}回目");

            if (retryCount % 3 == 0)
            {
                Debug.Log("[QuickRankingDisplay] 3回に1回の広告タイミング！");
                var adManager = AdMobRewardedManager.Instance;
                if (adManager != null)
                {
                    adManager.ShowAd(() =>
                    {
                        DifficultyManager.Instance.SetRound(currentRound);
                        LoadMainScene();
                    });
                }
                else
                {
                    DifficultyManager.Instance.SetRound(currentRound);
                    LoadMainScene();
                }
            }
            else
            {
                DifficultyManager.Instance.SetRound(currentRound);
                LoadMainScene();
            }
            return;
        }

        // 成功時はリトライカウントをリセット
        PlayerPrefs.SetInt("RetryCount", 0);
        PlayerPrefs.Save();

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
            // 成功 → 次のラウンドへ
            Debug.Log($"[QuickRankingDisplay] 成功！Round {currentRound} → Round {currentRound + 1}");
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