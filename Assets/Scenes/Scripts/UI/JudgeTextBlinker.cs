using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// ホールドノーツのカウント時にPERFECT表示を明滅させる
/// </summary>
public class JudgeTextBlinker : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI judgeText;
    
    [Header("Blink Settings")]
    [SerializeField] private float blinkDuration = 0.15f;
    [SerializeField] private float minAlpha = 0.5f;
    [SerializeField] private float maxAlpha = 1.0f;
    
    private Color originalColor;
    private Coroutine blinkCoroutine;

    private void Start()
    {
        if (judgeText == null)
        {
            judgeText = GetComponent<TextMeshProUGUI>();
        }
        
        if (judgeText != null)
        {
            originalColor = judgeText.color;
        }
    }

    /// <summary>
    /// ホールドノーツのカウント時に呼ばれる
    /// </summary>
    public void OnHoldTick()
    {
        if (judgeText == null) return;
        
        // 既存の明滅を停止
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        
        // 新しい明滅を開始
        blinkCoroutine = StartCoroutine(BlinkCoroutine());
    }

/// <summary>
    /// 明滅を停止して元の状態に戻す
    /// </summary>
public void StopBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        if (judgeText != null)
        {
            judgeText.alpha = 1.0f;
        }
    }


    private IEnumerator BlinkCoroutine()
    {
        float elapsed = 0f;
        
        // 暗くする (0.075秒)
        float halfDuration = blinkDuration * 0.5f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float alpha = Mathf.Lerp(maxAlpha, minAlpha, t);
            judgeText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        // 明るくする (0.075秒)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
            judgeText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        // 元に戻す
        judgeText.color = originalColor;
        blinkCoroutine = null;
    }
}
