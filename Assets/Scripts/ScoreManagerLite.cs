using TMPro;
using UnityEngine;
using System.Collections;

/// <summary>
/// 超軽量スコアUI管理
/// ・OnPickで "PERFECT"/"GOOD"/"MISS" を受け取り更新
/// ・MISS時は必ずCOMBO=0
/// </summary>
public class ScoreManagerLite : MonoBehaviour
{
    public static ScoreManagerLite Instance { get; private set; }

    [Header("UI（必ず割当て）")]
    public TextMeshProUGUI flourText;
    public TextMeshProUGUI milkText;
    public TextMeshProUGUI eggText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI judgeText;

    private int flour, milk, egg, combo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        UpdateUI();
    }

    /// <summary>
    /// ノーツ取得・判定結果の通知
    /// </summary>
    public void OnPick(IngredientType type, string judge)
    {
        // 加点（MISSでは食材は増えない）
        if (judge == "PERFECT" || judge == "GOOD")
        {
            switch (type)
            {
                case IngredientType.Flour: flour++; break;
                case IngredientType.Milk:  milk++;  break;
                case IngredientType.Egg:   egg++;   break;
            }
            combo++;
        }
        else // MISS
        {
            combo = 0;
        }

        // 判定表示を更新（短時間でクリア）
        if (judgeText) judgeText.text = judge;
        StopAllCoroutines();
        StartCoroutine(ClearJudgeAfter(0.5f));

        UpdateUI();
    }

    private IEnumerator ClearJudgeAfter(float sec)
    {
        yield return new WaitForSeconds(sec);
        if (judgeText) judgeText.text = "";
    }

    private void UpdateUI()
    {
        if (flourText) flourText.text = flour.ToString("00");
        if (milkText)  milkText.text  = milk.ToString("00");
        if (eggText)   eggText.text   = egg.ToString("00");
        if (comboText) comboText.text = combo.ToString("00");
    }
}
