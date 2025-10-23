using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ResultButtons : MonoBehaviour
{
    [Header("Buttons")]
    public Button RetryButton;
    public Button TitleButton;

    [Header("Button Images (任意)")]
    public Image RetryImage;
    public Image TitleImage;

    [Header("Fade")]
    public CanvasGroup FadeGroup;
    public float FadeSeconds = 0.25f;

    [Header("SE (任意)")]
    public AudioSource ClickSE;

    [Header("Scene Names")]
    public string MainSceneName = "Main";
    public string TitleSceneName = "TitleScene";  // ←ここを TitleScene に変更済み！

    private bool isTransitioning = false;

    void Start()
    {
        if (RetryButton != null)
            RetryButton.onClick.AddListener(OnRetryButton);

        if (TitleButton != null)
            TitleButton.onClick.AddListener(OnTitleButton);
    }

    public void OnRetryButton()
    {
        if (isTransitioning) return;
        StartCoroutine(LoadSceneWithFade(MainSceneName));
    }

    public void OnTitleButton()
    {
        if (isTransitioning) return;
        StartCoroutine(LoadSceneWithFade(TitleSceneName));
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        isTransitioning = true;

        if (ClickSE != null)
            ClickSE.Play();

        // フェードアウト
        if (FadeGroup != null)
        {
            float t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.deltaTime;
                FadeGroup.alpha = Mathf.Lerp(0, 1, t / FadeSeconds);
                yield return null;
            }
        }

        // シーン遷移
        SceneManager.LoadScene(sceneName);
    }
}
