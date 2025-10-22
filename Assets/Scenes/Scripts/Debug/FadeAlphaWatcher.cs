using System.Diagnostics;
using UnityEngine;

// Attach to FadeCanvas (CanvasGroup on the same GameObject)
[RequireComponent(typeof(CanvasGroup))]
public class FadeAlphaWatcher : MonoBehaviour
{
    private CanvasGroup cg;
    private float last;
    public float threshold = 0.0001f; // 変化検出閾値
    public float pollInterval = 0.05f;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        last = cg.alpha;
        StartCoroutine(Poll());
    }

    System.Collections.IEnumerator Poll()
    {
        var wait = new WaitForSecondsRealtime(pollInterval);
        while (true)
        {
            float a = cg.alpha;
            if (Mathf.Abs(a - last) > threshold)
            {
                UnityEngine.Debug.LogWarning($"[FadeAlphaWatcher] alpha changed {last} -> {a}\nStack:\n{GetStack()}");
                last = a;
            }
            yield return wait;
        }
    }

    string GetStack()
    {
        // 簡易スタック（MonoBehaviour呼び出し元を見つける）
        var st = new StackTrace(2, true);
        return st.ToString();
    }
}
