using UnityEngine;
using TMPro;
using System.Collections;

public class HudCounterBinder : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI milkText;
    [SerializeField] private TextMeshProUGUI flourText;
    [SerializeField] private TextMeshProUGUI eggText;

    [Header("Settings")]
    [SerializeField] private int tickInterval = 1;
    
    [Header("Animation Settings")]
    [SerializeField, Range(1.0f, 1.5f)] private float punchScale = 1.2f;
    [SerializeField, Range(0.05f, 0.2f)] private float punchDuration = 0.1f;
    [SerializeField] private AnimationCurve punchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Debug")]
    [SerializeField] private int currentMilkCount = 0;
    [SerializeField] private int currentFlourCount = 0;
    [SerializeField] private int currentEggCount = 0;
    [SerializeField] private bool isCounting = false;
    [SerializeField] private int currentLane = -1;

    private int frameCounter = 0;
    private HoldTickPulse activeHoldTickPulse;
    
    private Vector3 milkOriginalScale;
    private Vector3 flourOriginalScale;
    private Vector3 eggOriginalScale;
    private Coroutine milkPunchCoroutine;
    private Coroutine flourPunchCoroutine;
    private Coroutine eggPunchCoroutine;

    private void Start()
    {
        if (milkText == null)
        {
            var obj = GameObject.Find("MilkText");
            if (obj != null) milkText = obj.GetComponent<TextMeshProUGUI>();
        }
        if (flourText == null)
        {
            var obj = GameObject.Find("FlourText");
            if (obj != null)
            {
                flourText = obj.GetComponent<TextMeshProUGUI>();
                Debug.Log("[HudCounterBinder] Auto-found FlourText");
            }
        }
        if (eggText == null)
        {
            var obj = GameObject.Find("EggText");
            if (obj != null) eggText = obj.GetComponent<TextMeshProUGUI>();
        }
        
        if (milkText != null) milkOriginalScale = milkText.transform.localScale;
        if (flourText != null) flourOriginalScale = flourText.transform.localScale;
        if (eggText != null) eggOriginalScale = eggText.transform.localScale;
    }

    public void OnNormalNote()
    {
        currentFlourCount++;
        Debug.Log($"[HudCounterBinder] Normal note - Flour count: {currentFlourCount}");
        UpdateUI();
        PlayPunchAnimation(1);
    }

    public void OnNormalNote(int lane)
    {
        switch (lane)
        {
            case 0:
                currentMilkCount++;
                Debug.Log($"[HudCounterBinder] Normal note - Milk count: {currentMilkCount}");
                PlayPunchAnimation(0);
                break;
            case 1:
                currentFlourCount++;
                Debug.Log($"[HudCounterBinder] Normal note - Flour count: {currentFlourCount}");
                PlayPunchAnimation(1);
                break;
            case 2:
                currentEggCount++;
                Debug.Log($"[HudCounterBinder] Normal note - Egg count: {currentEggCount}");
                PlayPunchAnimation(2);
                break;
        }
        UpdateUI();
    }

    public void OnHoldEnter()
    {
        var holdTickPulses = FindObjectsOfType<HoldTickPulse>();
        foreach (var pulse in holdTickPulses)
        {
            if (pulse.IsActive)
            {
                activeHoldTickPulse = pulse;
                currentLane = pulse.laneIndex;
                break;
            }
        }

        string laneType = currentLane == 0 ? "Milk" : currentLane == 1 ? "Flour" : currentLane == 2 ? "Egg" : "Unknown";
        Debug.Log($"[HudCounterBinder] OnHoldEnter - Starting {laneType} count (lane={currentLane})");
        isCounting = true;
        frameCounter = 0;
        UpdateUI();
    }

    public void OnHoldTick()
    {
        if (!isCounting)
        {
            return;
        }

        if (activeHoldTickPulse != null)
        {
            currentLane = activeHoldTickPulse.laneIndex;
        }

        frameCounter++;
        if (frameCounter >= tickInterval)
        {
            switch (currentLane)
            {
                case 0:
                    currentMilkCount++;
                    Debug.Log($"[HudCounterBinder] Milk count increased: {currentMilkCount}");
                    PlayPunchAnimation(0);
                    break;
                case 1:
                    currentFlourCount++;
                    Debug.Log($"[HudCounterBinder] Flour count increased: {currentFlourCount}");
                    PlayPunchAnimation(1);
                    break;
                case 2:
                    currentEggCount++;
                    Debug.Log($"[HudCounterBinder] Egg count increased: {currentEggCount}");
                    PlayPunchAnimation(2);
                    break;
                default:
                    currentFlourCount++;
                    Debug.Log($"[HudCounterBinder] Unknown lane, defaulting to Flour: {currentFlourCount}");
                    PlayPunchAnimation(1);
                    break;
            }
            frameCounter = 0;
            UpdateUI();
        }
    }

    public void OnHoldTick(int lane)
    {
        switch (lane)
        {
            case 0:
                currentMilkCount++;
                Debug.Log($"[HudCounterBinder] Hold tick - Milk count: {currentMilkCount}");
                PlayPunchAnimation(0);
                break;
            case 1:
                currentFlourCount++;
                Debug.Log($"[HudCounterBinder] Hold tick - Flour count: {currentFlourCount}");
                PlayPunchAnimation(1);
                break;
            case 2:
                currentEggCount++;
                Debug.Log($"[HudCounterBinder] Hold tick - Egg count: {currentEggCount}");
                PlayPunchAnimation(2);
                break;
        }
        UpdateUI();
    }

    public void OnHoldExit()
    {
        string laneType = currentLane == 0 ? "Milk" : currentLane == 1 ? "Flour" : currentLane == 2 ? "Egg" : "Unknown";
        Debug.Log($"[HudCounterBinder] OnHoldExit - Stopping {laneType} count");
        isCounting = false;
        frameCounter = 0;
        currentLane = -1;
        activeHoldTickPulse = null;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (milkText != null)
        {
            milkText.text = currentMilkCount.ToString("00");
        }
        if (flourText != null)
        {
            flourText.text = currentFlourCount.ToString("00");
        }
        if (eggText != null)
        {
            eggText.text = currentEggCount.ToString("00");
        }
    }
    
    private void PlayPunchAnimation(int lane)
    {
        switch (lane)
        {
            case 0:
                if (milkText == null) return;
                if (milkPunchCoroutine != null) StopCoroutine(milkPunchCoroutine);
                milkPunchCoroutine = StartCoroutine(PunchScaleCoroutine(milkText, milkOriginalScale));
                break;
            case 1:
                if (flourText == null) return;
                if (flourPunchCoroutine != null) StopCoroutine(flourPunchCoroutine);
                flourPunchCoroutine = StartCoroutine(PunchScaleCoroutine(flourText, flourOriginalScale));
                break;
            case 2:
                if (eggText == null) return;
                if (eggPunchCoroutine != null) StopCoroutine(eggPunchCoroutine);
                eggPunchCoroutine = StartCoroutine(PunchScaleCoroutine(eggText, eggOriginalScale));
                break;
        }
    }

    private IEnumerator PunchScaleCoroutine(TextMeshProUGUI targetText, Vector3 originalScale)
    {
        float elapsed = 0f;
        
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            float curveValue = punchCurve.Evaluate(t);
            
            float scale = Mathf.Lerp(punchScale, 1f, curveValue);
            targetText.transform.localScale = originalScale * scale;
            
            yield return null;
        }
        
        targetText.transform.localScale = originalScale;
    }

    private void OnDisable()
    {
        if (isCounting)
        {
            OnHoldExit();
        }
    }
}
