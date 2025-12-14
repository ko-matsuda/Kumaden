using UnityEngine;
using TMPro;

public class HudCounterBinder : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI milkText;
    [SerializeField] private TextMeshProUGUI flourText;
    [SerializeField] private TextMeshProUGUI eggText;

    [Header("Settings")]
    [SerializeField] private int tickInterval = 1;
    
    [Header("Debug")]
    [SerializeField] private int currentMilkCount = 0;
    [SerializeField] private int currentFlourCount = 0;
    [SerializeField] private int currentEggCount = 0;
    [SerializeField] private bool isCounting = false;
    [SerializeField] private int currentLane = -1;

    private int frameCounter = 0;
    private HoldTickPulse activeHoldTickPulse;

    private void Start()
    {
        // 自動検出
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
    }

    public void OnNormalNote()
    {
        // 通常ノーツは Flour としてカウント（既存動作を維持）
        currentFlourCount++;
        Debug.Log($"[HudCounterBinder] Normal note - Flour count: {currentFlourCount}");
        UpdateUI();
    }

    /// <summary>
    /// レーンを指定して通常ノーツをカウント
    /// </summary>
    public void OnNormalNote(int lane)
    {
        switch (lane)
        {
            case 0:
                currentMilkCount++;
                Debug.Log($"[HudCounterBinder] Normal note - Milk count: {currentMilkCount}");
                break;
            case 1:
                currentFlourCount++;
                Debug.Log($"[HudCounterBinder] Normal note - Flour count: {currentFlourCount}");
                break;
            case 2:
                currentEggCount++;
                Debug.Log($"[HudCounterBinder] Normal note - Egg count: {currentEggCount}");
                break;
        }
        UpdateUI();
    }

    public void OnHoldEnter()
    {
        // 現在アクティブな HoldTickPulse を探してレーン情報を取得
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

        // アクティブな HoldTickPulse からレーン情報を再取得（念のため）
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
                    break;
                case 1:
                    currentFlourCount++;
                    Debug.Log($"[HudCounterBinder] Flour count increased: {currentFlourCount}");
                    break;
                case 2:
                    currentEggCount++;
                    Debug.Log($"[HudCounterBinder] Egg count increased: {currentEggCount}");
                    break;
                default:
                    currentFlourCount++;
                    Debug.Log($"[HudCounterBinder] Unknown lane, defaulting to Flour: {currentFlourCount}");
                    break;
            }
            frameCounter = 0;
            UpdateUI();
        }
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

    private void OnDisable()
    {
        if (isCounting)
        {
            OnHoldExit();
        }
    }
}
