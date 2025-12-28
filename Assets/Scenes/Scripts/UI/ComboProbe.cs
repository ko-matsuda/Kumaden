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
        UpdateUI();
    }

public void OnHoldTick()
    {
        currentComboCount++;
        Debug.Log($"[ComboProbe] COMBO count increased: {currentComboCount}");
        UpdateUI();
    }

    public void OnHoldExit()
    {
        Debug.Log($"[ComboProbe] OnHoldExit - Stopping COMBO count at {currentComboCount}");
        isCountingCombo = false;
        frameCounter = 0;
        UpdateUI();
    }

    // MISS 時にコンボをリセット
    public void ResetCombo()
    {
        Debug.Log($"[ComboProbe] ResetCombo - COMBO reset from {currentComboCount} to 0");
        currentComboCount = 0;
        isCountingCombo = false;
        frameCounter = 0;
        UpdateUI();
    }

public void OnNoteMiss() { Debug.Log("[ComboProbe] OnNoteMiss - Miss detected"); ResetCombo(); }


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

    private void OnDisable()
    {
        if (isCountingCombo)
        {
            OnHoldExit();
        }
    }
}