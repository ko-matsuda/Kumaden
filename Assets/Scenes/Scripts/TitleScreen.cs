using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    public CanvasGroup fade;
    public TMP_Text tapText;
    public string nextSceneName = "Main";
    public float blinkSpeed = 1.6f;

    bool starting = false;
    float time = 0f;

    void Start()
    {
        if (fade) fade.alpha = 1f;  // 最初は真っ黒
        StartCoroutine(FadeIn());
    }

    void Update()
    {
        if (starting) return;

        // タップでスタートの文字をピカピカさせる
        if (tapText)
        {
            time += Time.deltaTime * blinkSpeed;
            float a = Mathf.Sin(time) * 0.5f + 0.5f;
            tapText.alpha = Mathf.Lerp(0.3f, 1f, a);
        }

        // クリックまたはタップされたらゲームへ
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            starting = true;
            StartCoroutine(FadeOut());
        }
    }

    System.Collections.IEnumerator FadeIn()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / 1.0f;
            fade.alpha = 1f - t;
            yield return null;
        }
        fade.alpha = 0f;
    }

    System.Collections.IEnumerator FadeOut()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / 0.6f;
            fade.alpha = t;
            yield return null;
        }
        SceneManager.LoadScene(nextSceneName);
    }
}
