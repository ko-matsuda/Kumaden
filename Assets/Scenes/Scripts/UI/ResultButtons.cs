using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ResultButtons : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button titleButton;

    [Header("Button Images (必ず各自の子Image)")]
    [SerializeField] private Image retryImage;
    [SerializeField] private Image titleImage;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeGroup;   // FadeCanvas の CanvasGroup
    [SerializeField, Range(0.05f, 1f)] private float fadeSeconds = 0.25f;

    [Header("SE (任意)")]
    [SerializeField] private AudioSource clickSE;

    [Header("Scene Names")]
    [SerializeField] private string mainSceneName = "Main";
    [SerializeField] private string titleSceneName = "Title";

    // 何かが書き換えても毎回元に戻す“強制修復”
    void OnEnable()
    {
        // もし未設定でも自動で探す（最低限の自己回復）
        if (retryButton == null || titleButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                var n = b.gameObject.name.ToLower();
                if (retryButton == null && n.Contains("retry")) retryButton = b;
                if (titleButton == null && n.Contains("title")) titleButton = b;
            }
        }
        if (retryImage == null && retryButton != null)
            retryImage = retryButton.GetComponent<Image>();
        if (titleImage == null && titleButton != null)
            titleImage = titleButton.GetComponent<Image>();

        // Graphic の取り違えを強制矯正
        ForceTargetGraphic(retryButton, retryImage);
        ForceTargetGraphic(titleButton, titleImage);

        // 他スクリプトが OnClick を触っていても上書きして固定
        WireOnClick(retryButton, OnRetryButton);
        WireOnClick(titleButton, OnTitleButton);

        // フェードが UI をブロックし続けないように安全化
        if (fadeGroup != null)
        {
            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = false;
            if (fadeGroup.alpha < 0f) fadeGroup.alpha = 0f;
        }
    }

    void LateUpdate()
    {
        // 毎フレーム、誰かが書き換えても元に戻す（干渉を無力化）
        ForceTargetGraphic(retryButton, retryImage);
        ForceTargetGraphic(titleButton, titleImage);
        if (fadeGroup != null)
        {
            // フェード幕が誤ってタップを遮らないよう監視
            if (fadeGroup.blocksRaycasts) fadeGroup.blocksRaycasts = false;
            if (fadeGroup.interactable)   fadeGroup.interactable   = false;
        }
    }

    static void ForceTargetGraphic(Button btn, Graphic shouldBe)
    {
        if (btn == null || shouldBe == null) return;
        if (btn.targetGraphic != shouldBe) btn.targetGraphic = shouldBe;
    }

    static void WireOnClick(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners(); // 外部の邪魔を掃除
        btn.onClick.AddListener(action);
    }

    public void OnRetryButton()
    {
        if (clickSE != null) clickSE.Play();
        StartCoroutine(LoadSceneWithFade(mainSceneName));
    }

    public void OnTitleButton()
    {
        if (clickSE != null) clickSE.Play();
        StartCoroutine(LoadSceneWithFade(titleSceneName));
    }

    IEnumerator LoadSceneWithFade(string scene)
    {
        // 操作感改善：即時でTimeScale戻す
        if (Time.timeScale != 1f) Time.timeScale = 1f;

        // 余計なオーバーレイがあれば無効化（保険）
        DisableBlockingOverlays();

        // フェード
        if (fadeGroup != null && fadeSeconds > 0f)
        {
            fadeGroup.blocksRaycasts = true; // フェード中は入力遮断
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeSeconds);
                yield return null;
            }
            fadeGroup.alpha = 1f;
        }

        // 確実にリロード
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }

    void DisableBlockingOverlays()
    {
        // よくある犯人：他CanvasGroupの blocksRaycasts が On のまま
        var groups = FindObjectsOfType<CanvasGroup>(true);
        foreach (var g in groups)
        {
            // フェード自身は直後に使うので除外
            if (g == fadeGroup) continue;
            if (g.blocksRaycasts) g.blocksRaycasts = false;
            if (g.interactable)   g.interactable   = false;
        }
    }
}
