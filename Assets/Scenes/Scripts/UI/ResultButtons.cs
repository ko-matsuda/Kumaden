using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class ResultButtons : MonoBehaviour
{
    [Header("Buttons")]
    public Button retryButton;
    public Button titleButton;

    [Header("Button Images (任意)")]
    public Image retryImage;
    public Image titleImage;

    [Header("Fade")]
    public CanvasGroup fadeGroup;   // ← FadeCanvas の CanvasGroup をドラッグ
    public float fadeSeconds = 0.25f;

    [Header("SE (任意)")]
    public AudioSource clickSE;     // ← クリック音のAudioSource(結果キャンバスの音源など)

    [Header("Scene Names")]
    public string mainSceneName = "Main";
    public string titleSceneName = "TitleScene";

    void Awake()
    {
        // 念のため初期化（押せる状態に）
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = false;
        }

        // ボタンの配線（EditorのOnClickが空でも動く）
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryButton);
        }
        if (titleButton != null)
        {
            titleButton.onClick.RemoveAllListeners();
            titleButton.onClick.AddListener(OnTitleButton);
        }
    }

    public void OnRetryButton()
    {
        if (clickSE) clickSE.Play();
        StartCoroutine(FadeAndLoad(mainSceneName));
    }

    public void OnTitleButton()
    {
        if (clickSE) clickSE.Play();
        StartCoroutine(FadeAndLoad(titleSceneName));
    }

    IEnumerator FadeAndLoad(string sceneName)
    {
        // ここで必ず動くように、TimeScaleに依らないフェードにする
        if (fadeGroup != null && fadeSeconds > 0f)
        {
            fadeGroup.blocksRaycasts = true; // 多重クリック防止
            fadeGroup.interactable = false;

            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime; // ← 重要: unscaled
                fadeGroup.alpha = Mathf.Clamp01(t / fadeSeconds);
                yield return null;
            }
        }

        // 万一どこかで0にされていても遷移できるように戻す
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        // Build Profilesに入っていないとロードに失敗するので注意（下で手順再掲）
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
