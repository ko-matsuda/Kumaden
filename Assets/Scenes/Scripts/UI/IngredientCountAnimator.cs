using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 食材カウントのテキストを拡縮アニメーションさせる
/// </summary>
public class IngredientCountAnimator : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI countText;
    
    [Header("Animation Settings")]
    [SerializeField, Range(1.0f, 2.0f)] private float punchScale = 1.3f;
    [SerializeField, Range(0.05f, 0.3f)] private float punchDuration = 0.15f;
    [SerializeField] private AnimationCurve punchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private Vector3 originalScale = Vector3.one;
    private Coroutine punchCoroutine;
    private string lastText = "";

    private void Start()
    {
        if (countText == null)
        {
            countText = GetComponent<TextMeshProUGUI>();
        }
        
        if (countText != null)
        {
            originalScale = countText.transform.localScale;
            lastText = countText.text;
        }
    }

    private void Update()
    {
        if (countText == null) return;
        
        // テキストが変更されたら拡縮アニメーション
        if (countText.text != lastText)
        {
            lastText = countText.text;
            PlayPunchAnimation();
        }
    }

    private void PlayPunchAnimation()
    {
        if (punchCoroutine != null)
        {
            StopCoroutine(punchCoroutine);
        }
        
        punchCoroutine = StartCoroutine(PunchScaleCoroutine());
    }

    private IEnumerator PunchScaleCoroutine()
    {
        float elapsed = 0f;
        
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            float curveValue = punchCurve.Evaluate(t);
            
            float scale = Mathf.Lerp(punchScale, 1f, curveValue);
            countText.transform.localScale = originalScale * scale;
            
            yield return null;
        }
        
        countText.transform.localScale = originalScale;
        punchCoroutine = null;
    }
}
