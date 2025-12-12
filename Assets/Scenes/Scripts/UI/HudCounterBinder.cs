using UnityEngine;
using TMPro;

public class HudCounterBinder : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI flourText;

    [Header("Settings")]
    [SerializeField] private int tickInterval = 1;
    
    [Header("Debug")]
    [SerializeField] private int currentFlourCount = 0;
    [SerializeField] private bool isCountingFlour = false;

    private int frameCounter = 0;

    private void Start()
    {
        if (flourText == null)
        {
            var flourTextObj = GameObject.Find("FlourText");
            if (flourTextObj != null)
            {
                flourText = flourTextObj.GetComponent<TextMeshProUGUI>();
                Debug.Log("[HudCounterBinder] Auto-found FlourText");
            }
        }
    }

    // 通常ノーツ用：1カウント増やすだけ（リセットしない）
    public void OnNormalNote()
    {
        currentFlourCount++;
        Debug.Log($"[HudCounterBinder] Normal note - Flour count: {currentFlourCount}");
        UpdateUI();
    }

    public void OnHoldEnter()
    {
        Debug.Log($"[HudCounterBinder] OnHoldEnter - Starting Flour count");
        isCountingFlour = true;
        frameCounter = 0;
        // リセットしない
        UpdateUI();
    }

    public void OnHoldTick()
    {
        if (!isCountingFlour)
        {
            Debug.LogWarning($"[HudCounterBinder] OnHoldTick called but isCountingFlour is false!");
            return;
        }

        frameCounter++;
        if (frameCounter >= tickInterval)
        {
            currentFlourCount++;
            Debug.Log($"[HudCounterBinder] Flour count increased: {currentFlourCount}");
            frameCounter = 0;
            UpdateUI();
        }
    }

    public void OnHoldExit()
    {
        Debug.Log($"[HudCounterBinder] OnHoldExit - Stopping Flour count at {currentFlourCount}");
        isCountingFlour = false;
        frameCounter = 0;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (flourText != null)
        {
            flourText.text = currentFlourCount.ToString("00");
        }
    }

    private void OnDisable()
    {
        if (isCountingFlour)
        {
            Debug.Log($"[HudCounterBinder] OnDisable - forcing OnHoldExit");
            OnHoldExit();
        }
    }
}
