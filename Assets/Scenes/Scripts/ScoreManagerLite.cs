using TMPro;
using UnityEngine;
using System.Collections;

public class ScoreManagerLite : MonoBehaviour
{
    public static ScoreManagerLite Instance { get; private set; }

    [Header("UI（必ず割当て）")]
    public TextMeshProUGUI flourText;
    public TextMeshProUGUI milkText;
    public TextMeshProUGUI eggText;
    public TextMeshProUGUI comboText;   // 画面上部の COMBO 表示
    public TextMeshProUGUI judgeText;   // 画面上部の PERFECT/GOOD/MISS 表示

    [Header("色テーマの参照（ResultCanvas 側のラベルをドラッグ）")]
    public TextMeshProUGUI refMaxComboLabel; // ResultCanvas → JudgeBox/MaxCombo/Label
    public TextMeshProUGUI refPerfectLabel;  // ResultCanvas → JudgeBox/Row-Perfect/Label
    public TextMeshProUGUI refGoodLabel;     // ResultCanvas → JudgeBox/Row-Good/Label
    public TextMeshProUGUI refMissLabel;     // ResultCanvas → JudgeBox/Row-Miss/Label

    // 内部カウンタ
    private int flour, milk, egg, combo;

    // Result用
    [SerializeField] private int perfectCount;
    [SerializeField] private int goodCount;
    [SerializeField] private int missCount;
    [SerializeField] private int maxChain;

    // 公開（ResultCaller が読む）
    public int FlourCount => flour;
    public int MilkCount  => milk;
    public int EggCount   => egg;
    public int PerfectCount => perfectCount;
    public int GoodCount    => goodCount;
    public int MissCount    => missCount;
    public int MaxChain     => maxChain;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 起動時に COMBO の見た目を Result の "MAX COMBO" と揃える
        if (comboText && refMaxComboLabel)
            CopyTMPStyle(refMaxComboLabel, comboText);

        UpdateUI();
    }

    /// <summary>ノーツ取得・判定結果の通知</summary>
    public void OnPick(IngredientType type, string judge)
    {
        if (judge == "PERFECT")
        {
            perfectCount++;
            GainItemAndCombo(type);
            if (judgeText && refPerfectLabel) CopyTMPStyle(refPerfectLabel, judgeText);
        }
        else if (judge == "GOOD")
        {
            goodCount++;
            GainItemAndCombo(type);
            if (judgeText && refGoodLabel) CopyTMPStyle(refGoodLabel, judgeText);
        }
        else // MISS
        {
            missCount++;
            combo = 0;
            if (judgeText && refMissLabel) CopyTMPStyle(refMissLabel, judgeText);
        }

        if (judgeText) judgeText.text = judge;
        StopAllCoroutines();
        StartCoroutine(ClearJudgeAfter(0.5f));

        UpdateUI();
    }

    private void GainItemAndCombo(IngredientType type)
    {
        switch (type)
        {
            case IngredientType.Flour: flour++; break;
            case IngredientType.Milk:  milk++;  break;
            case IngredientType.Egg:   egg++;   break;
        }
        combo++;
        if (combo > maxChain) maxChain = combo;
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

        // ✅ ここを変更：「コンボが0のときは非表示」
        if (comboText)
            comboText.text = combo > 0 ? $"COMBO {combo:00}" : "";
    }

    // 色・グラデ・（フォント一致時のみ）マテリアルをコピー
    private static void CopyTMPStyle(TextMeshProUGUI src, TextMeshProUGUI dst)
    {
        if (!src || !dst) return;

        dst.color = src.color;
        dst.alpha = src.alpha;

        dst.enableVertexGradient = src.enableVertexGradient;
        if (src.enableVertexGradient)
            dst.colorGradient = src.colorGradient;

        // フォントが同じときだけマテリアルをコピー（文字化け防止）
        if (src.font == dst.font && src.fontSharedMaterial != null)
            dst.fontSharedMaterial = src.fontSharedMaterial;
        else if (dst.font != null)
            dst.fontSharedMaterial = dst.font.material;
    }
}
