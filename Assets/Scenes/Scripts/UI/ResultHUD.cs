// Assets/Scenes/Scripts/UI/ResultHUD.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class ResultHUD : MonoBehaviour
{
    [Header("UI Groups")]
    [Tooltip("結果UIのルート。ここをフェードさせます")]
    [SerializeField] private CanvasGroup resultGroup;
    [Tooltip("（任意）ボタンだけ別にフェードしたいなら、ボタン用の CanvasGroup")]
    [SerializeField] private CanvasGroup buttonsGroup;

    [Header("Score Display")]
    [SerializeField] private TMPro.TMP_Text scoreText;
    [SerializeField] private TMPro.TMP_Text rankingText;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button titleButton;

    [Header("リザルト音")]
    [Tooltip("リザルト表示時の音")]
    [SerializeField] private AudioClip resultSE;
    [SerializeField, Range(0f, 1f)] private float resultVolume = 0.7f;
    
    [Header("FX (Optional)")]
    [Tooltip("クリック音（任意）")]
    [SerializeField] private AudioSource clickSE;
    [Tooltip("フェード秒数")]
    [SerializeField] private float fadeSeconds = 0.25f;

    [Header("Scenes")]
    [SerializeField] private string mainSceneName = "Main";
    [SerializeField] private string titleSceneName = "TitleScene";

    bool isShowing;
    bool isBusy;

    void Reset()
    {
        // ヒエラルキーから推測して自動割当（楽できるだけ）
        resultGroup  = GetComponent<CanvasGroup>();
        retryButton  = GameObject.Find("Btn-Retry")?.GetComponent<Button>();
        titleButton  = GameObject.Find("Btn-Title")?.GetComponent<Button>();
    }

    void Awake()
    {
        // GraphicRaycaster が無いとボタンが反応しない
        EnsureGraphicRaycaster();

        // 初期は非表示（GameObject は Active のまま。CanvasGroupで隠す）
        if (resultGroup == null) resultGroup = GetComponent<CanvasGroup>();
        if (resultGroup != null)
        {
            resultGroup.alpha = 0f;
            resultGroup.interactable = false;
            resultGroup.blocksRaycasts = false;
        }
        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        // 既存のOnClickは一旦クリアして、ここで統一配線
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetry);
        }
        if (titleButton != null)
        {
            titleButton.onClick.RemoveAllListeners();
            titleButton.onClick.AddListener(OnTitle);
        }
    }

    /// <summary>結果UIを表示（料理動画のあと等で1回呼ぶ）</summary>
    public void ShowResult()
    {
        // ResultCanvasを先にアクティブ化してからコルーチンを開始
        if (gameObject != null && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (isShowing || isBusy) return;
        isShowing = true;
        
        // ボタンのonClickを再設定（Awakeが呼ばれていない場合のため）
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetry);
        }
        if (titleButton != null)
        {
            titleButton.onClick.RemoveAllListeners();
            titleButton.onClick.AddListener(OnTitle);
        }
        
        // スコアとランキングを表示
        UpdateScoreDisplay();
        
        // SafeAreaは最初は表示したまま（ResultCallerが3秒後に消す）
        
        StartCoroutine(FadeInRoutine());
    }

    void OnRetry()
    {
        if (isBusy) return;
        if (clickSE) clickSE.Play();
        StartCoroutine(LoadSceneWithFade(mainSceneName));
    }

    void OnTitle()
    {
        if (isBusy) return;
        if (clickSE) clickSE.Play();
        StartCoroutine(LoadSceneWithFade(titleSceneName));
    }

    System.Collections.IEnumerator FadeInRoutine()
    {
        isBusy = true;

        // 表示に備えてレイキャストを有効化
        if (resultGroup != null)
        {
            resultGroup.blocksRaycasts = true;
            resultGroup.interactable = true;
        }
        if (buttonsGroup != null)
        {
            buttonsGroup.blocksRaycasts = true;
            buttonsGroup.interactable = true;
        }

        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime; // 一応タイムスケール非依存
            float a = Mathf.Clamp01(t / fadeSeconds);
            if (resultGroup != null) resultGroup.alpha = a;
            if (buttonsGroup != null) buttonsGroup.alpha = a;
            yield return null;
        }
        if (resultGroup != null) resultGroup.alpha = 1f;
        if (buttonsGroup != null) buttonsGroup.alpha = 1f;

        isBusy = false;
    }

    System.Collections.IEnumerator LoadSceneWithFade(string sceneName)
    {
        isBusy = true;

        // フェードアウト（結果UIだけ暗く）
        float t = 0f;
        float startA = resultGroup ? resultGroup.alpha : 1f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(startA, 0f, Mathf.Clamp01(t / fadeSeconds));
            if (resultGroup != null) resultGroup.alpha = a;
            if (buttonsGroup != null) buttonsGroup.alpha = a;
            yield return null;
        }
        if (resultGroup != null)
        {
            resultGroup.alpha = 0f;
            resultGroup.interactable = false;
            resultGroup.blocksRaycasts = false;
        }
        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        // シーン遷移
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[ResultHUD] Scene name is empty.");
            isBusy = false;
            yield break;
        }
        SceneManager.LoadScene(sceneName);
    }

    void EnsureGraphicRaycaster()
    {
        var canvas = GetComponent<Canvas>();
        if (!canvas) canvas = gameObject.AddComponent<Canvas>();
        // World Space設定を尊重（3D背景を透過表示するため）
        // renderModeは変更しない

        if (!TryGetComponent<UnityEngine.UI.GraphicRaycaster>(out _))
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }

    private void UpdateScoreDisplay()
    {
        var scoreMgr = ScoreManagerLite.Instance;
        if (scoreMgr == null)
        {
            Debug.LogWarning("[ResultHUD] ScoreManagerLite not found.");
            return;
        }
        
        // スコア計算
        int totalScore = scoreMgr.CalculateTotalScore();
        
        // スコア表示
        if (scoreText != null)
        {
            scoreText.text = totalScore.ToString("N0"); // カンマ区切り
        }
        
        // ランキング判定
        string ranking = DetermineRanking(totalScore);
        if (rankingText != null)
        {
            rankingText.text = "RANK: " + ranking;
        }
        
        Debug.Log($"[ResultHUD] Total Score: {totalScore}, Ranking: {ranking}");
    }

    private string DetermineRanking(int score)
    {
        // ランキング基準（調整可能）
        if (score >= 10000) return "S";
        if (score >= 7500) return "A";
        if (score >= 5000) return "B";
        if (score >= 2500) return "C";
        return "D";
    }
}
