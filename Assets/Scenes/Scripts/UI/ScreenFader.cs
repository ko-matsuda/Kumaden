using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Tooltip("Alpha=0（透明）になったら自動で非アクティブにする")]
    public bool autoDeactivateWhenClear = true;

    CanvasGroup cg;
    Coroutine running;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        // ★ここがポイント：開始時は必ず透明にする
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        cg.alpha = 0f; // ←最初は透明（プロローグが見える）
    }

    public bool IsFading => running != null;
    public float Alpha { get => cg ? cg.alpha : 0f; set { if (cg) cg.alpha = Mathf.Clamp01(value); } }

    public IEnumerator FadeIn(float duration = 0.6f)  // 黒→透明（明るく）
        => Fade(1f, 0f, duration);

    public IEnumerator FadeOut(float duration = 0.6f) // 透明→黒（暗く）
        => Fade(0f, 1f, duration);

    IEnumerator Fade(float from, float to, float duration)
    {
        if (!cg) yield break;

        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(FadeRoutine(from, to, Mathf.Max(0.01f, duration)));
        yield return running;
        running = null;

        if (autoDeactivateWhenClear && Mathf.Approximately(cg.alpha, 0f))
            gameObject.SetActive(false);
    }

    IEnumerator FadeRoutine(float from, float to, float duration)
    {
        cg.alpha = from;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // Time.timeScale=0 でも動く
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}
