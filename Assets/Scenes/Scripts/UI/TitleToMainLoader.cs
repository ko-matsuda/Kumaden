using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class TitleToMainLoader : MonoBehaviour
{
    public CanvasGroup fade;         // 黒パネル(CanvasGroup) 任意
    public TMP_Text tapText;         // 「タップでスタート」任意
    public float blinkSpeed = 1.6f;
    public string mainSceneName = "Main";
    public float fadeTime = 0.6f;

    float t; bool starting;

    void Start()
    {
        if (fade) { fade.alpha = 1f; StartCoroutine(FadeTo(0f, fadeTime)); }
    }

    void Update()
    {
        if (starting) return;

        if (tapText)
        {
            t += Time.unscaledDeltaTime * blinkSpeed;
            float a = Mathf.Lerp(0.3f, 1f, Mathf.Sin(t) * 0.5f + 0.5f);
            var c = tapText.color; c.a = a; tapText.color = c;
        }

        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.anyKeyDown)
        {
            starting = true;
            StartCoroutine(GoMain());
        }
    }

    System.Collections.IEnumerator GoMain()
    {
        if (fade) yield return FadeTo(1f, 0.4f);
        SceneManager.LoadScene(mainSceneName, LoadSceneMode.Single);
    }

    System.Collections.IEnumerator FadeTo(float target, float dur)
    {
        if (!fade) yield break;
        float from = fade.alpha, t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
            fade.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        fade.alpha = target;
        fade.blocksRaycasts = target > 0.5f;
        fade.interactable   = target > 0.5f;
    }
}
