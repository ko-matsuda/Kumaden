using UnityEngine;
using TMPro;
using System.Collections;

public class ComboProbe : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI comboText;

    [Header("Settings")]
    [SerializeField] private int tickInterval = 1;
    
    [Header("Animation Settings")]
    [SerializeField, Range(1.0f, 2.0f)] private float punchScale = 1.3f;
    [SerializeField, Range(0.05f, 0.3f)] private float punchDuration = 0.15f;
    [SerializeField] private AnimationCurve punchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField, Range(0.05f, 0.3f)] private float colorFadeDuration = 0.2f;
    
    [Header("Debug")]
    [SerializeField] private int currentComboCount = 0;
    [SerializeField] private bool isCountingCombo = false;

    private int frameCounter = 0;
    private Vector3 originalScale = Vector3.one;
    private Coroutine punchCoroutine;
    private Coroutine colorCoroutine;

    private void Start()
    {
        if (comboText == null)
        {
            var comboTextObj = GameObject.Find("ComboText");
            if (comboTextObj != null)
            {
                comboText = comboTextObj.GetComponent<TextMeshProUGUI>();
                Debug.Log("[ComboProbe] Auto-found ComboText");
            }
        }
        
        if (comboText != null)
        {
            originalScale = comboText.transform.localScale;
            comboText.color = normalColor;
        }
    }

    public void OnNormalNote()
    {
        currentComboCount++;
        Debug.Log($"[ComboProbe] Normal note - COMBO count: {currentComboCount}");
        UpdateUI();
        PlayPunchAnimation();
    }

    public void OnHoldEnter()
    {
        Debug.Log($"[ComboProbe] OnHoldEnter - Starting COMBO count");
        isCountingCombo = true;
        frameCounter = 0;
        UpdateUI();
    }

    public void OnHoldTick()
    {
        currentComboCount++;
        Debug.Log($"[ComboProbe] COMBO count increased: {currentComboCount}");
        UpdateUI();
        PlayPunchAnimation();
    }

    public void OnHoldExit()
    {
        Debug.Log($"[ComboProbe] OnHoldExit - Stopping COMBO count at {currentComboCount}");
        isCountingCombo = false;
        frameCounter = 0;
        UpdateUI();
    }

    public void ResetCombo()
    {
        Debug.Log($"[ComboProbe] ResetCombo - COMBO reset from {currentComboCount} to 0");
        currentComboCount = 0;
        isCountingCombo = false;
        frameCounter = 0;
        UpdateUI();
        
        if (punchCoroutine != null)
        {
            StopCoroutine(punchCoroutine);
            punchCoroutine = null;
        }
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
            colorCoroutine = null;
        }
        
        if (comboText != null)
        {
            comboText.transform.localScale = originalScale;
            comboText.color = normalColor;
        }
    }

    public void OnNoteMiss()
    {
        Debug.Log("[ComboProbe] OnNoteMiss - Miss detected");
        ResetCombo();
    }

    private void UpdateUI()
    {
        if (comboText != null)
        {
            if (currentComboCount >= 2)
            {
                comboText.text = "COMBO " + currentComboCount.ToString("00");
            }
            else
            {
                comboText.text = currentComboCount.ToString("00");
            }
        }
    }

    private void PlayPunchAnimation()
    {
        if (comboText == null) return;
        
        if (punchCoroutine != null)
        {
            StopCoroutine(punchCoroutine);
        }
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
        }
        
        punchCoroutine = StartCoroutine(PunchScaleCoroutine());
        colorCoroutine = StartCoroutine(ColorFlashCoroutine());
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
            comboText.transform.localScale = originalScale * scale;
            
            yield return null;
        }
        
        comboText.transform.localScale = originalScale;
        punchCoroutine = null;
    }

    private IEnumerator ColorFlashCoroutine()
    {
        float elapsed = 0f;
        
        while (elapsed < colorFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / colorFadeDuration;
            
            comboText.color = Color.Lerp(highlightColor, normalColor, t);
            
            yield return null;
        }
        
        comboText.color = normalColor;
        colorCoroutine = null;
    }

    private void OnDisable()
    {
        if (isCountingCombo)
        {
            OnHoldExit();
        }
    }
}
