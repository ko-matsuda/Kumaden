using UnityEngine;
using TMPro;

public class ComboProbe : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI comboText;

    [Header("Settings")]
    [SerializeField] private int tickInterval = 1;
    
    [Header("Debug")]
    [SerializeField] private int currentComboCount = 0;
    [SerializeField] private bool isCountingCombo = false;

    private int frameCounter = 0;

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
    }

    // 通常ノーツ用：1カウント増やすだけ（リセットしない）
    public void OnNormalNote()
    {
        currentComboCount++;
        Debug.Log($"[ComboProbe] Normal note - COMBO count: {currentComboCount}");
        UpdateUI();
    }

    public void OnHoldEnter()
    {
        Debug.Log($"[ComboProbe] OnHoldEnter - Starting COMBO count");
        isCountingCombo = true;
        frameCounter = 0;
        // リセットしない
        UpdateUI();
    }

    public void OnHoldTick()
    {
        if (!isCountingCombo)
        {
            Debug.LogWarning($"[ComboProbe] OnHoldTick called but isCountingCombo is false!");
            return;
        }

        frameCounter++;
        if (frameCounter >= tickInterval)
        {
            currentComboCount++;
            Debug.Log($"[ComboProbe] COMBO count increased: {currentComboCount}");
            frameCounter = 0;
            UpdateUI();
        }
    }

    public void OnHoldExit()
    {
        Debug.Log($"[ComboProbe] OnHoldExit - Stopping COMBO count at {currentComboCount}");
        isCountingCombo = false;
        frameCounter = 0;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (comboText != null)
        {
            comboText.text = currentComboCount.ToString("00");
        }
    }

    private void OnDisable()
    {
        if (isCountingCombo)
        {
            Debug.Log($"[ComboProbe] OnDisable - forcing OnHoldExit");
            OnHoldExit();
        }
    }
}
