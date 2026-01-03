using TMPro;
using UnityEngine;
using System.Collections;

public class ScoreManagerLite : MonoBehaviour
{
    public static ScoreManagerLite Instance { get; private set; }

    [Header("Judge Animation Settings")]
    [SerializeField, Range(0f, 0.1f)] private float judgeDisplayDelay = 0f;
    [SerializeField, Range(0.05f, 0.2f)] private float judgeFullOpaqueDuration = 0.1f;
    [SerializeField, Range(0.15f, 0.3f)] private float perfectHoldDuration = 0.20f;
    [SerializeField, Range(0.1f, 0.3f)] private float perfectFadeDuration = 0.18f;
    [SerializeField, Range(0.7f, 1.0f)] private float perfectAlpha = 1.0f;
    [SerializeField, Range(0.1f, 0.25f)] private float goodHoldDuration = 0.16f;
    [SerializeField, Range(0.1f, 0.25f)] private float goodFadeDuration = 0.16f;
    [SerializeField, Range(0.6f, 1.0f)] private float goodAlpha = 0.85f;
    [SerializeField, Range(0.08f, 0.2f)] private float missHoldDuration = 0.12f;
    [SerializeField, Range(0.08f, 0.2f)] private float missFadeDuration = 0.12f;
    [SerializeField, Range(0.5f, 1.0f)] private float missAlpha = 0.7f;

    [Header("Hold Note Perfect Blink Settings")]
    [SerializeField, Range(0.05f, 0.15f)] private float holdPerfectBlinkInterval = 0.08f;
    [SerializeField, Range(1, 3)] private int holdPerfectBlinkCount = 1;

    [Header("UI（必ず割当て）")]
    public TextMeshProUGUI flourText;
    public TextMeshProUGUI milkText;
    public TextMeshProUGUI eggText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI judgeText;

    [Header("色テーマの参照（ResultCanvas 側のラベルをドラッグ）")]
    public TextMeshProUGUI refMaxComboLabel;
    public TextMeshProUGUI refPerfectLabel;
    public TextMeshProUGUI refGoodLabel;
    public TextMeshProUGUI refMissLabel;

    private int flour, milk, egg, combo;

    [SerializeField] private int perfectCount;
    [SerializeField] private int goodCount;
    [SerializeField] private int missCount;
    [SerializeField] private int maxChain;

    private bool isHoldNotePerfect = false;

    public int FlourCount => flour;
    public int MilkCount => milk;
    public int EggCount => egg;
    public int PerfectCount => perfectCount;
    public int GoodCount => goodCount;
    public int MissCount => missCount;
    public int MaxChain => maxChain;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (comboText && refMaxComboLabel)
            CopyTMPStyle(refMaxComboLabel, comboText);

        UpdateUI();
    }

public void OnPick(IngredientType type, string judge)
    {
        if (judgeText)
        {
            StopAllCoroutines();
            judgeText.alpha = 1.0f;
        }
        
        if (judge == "PERFECT")
        {
            perfectCount++;
            GainItemAndCombo(type);
            if (judgeText && refPerfectLabel) CopyTMPStyle(refPerfectLabel, judgeText);
            isHoldNotePerfect = false;
        }
        else if (judge == "GOOD")
        {
            goodCount++;
            GainItemAndCombo(type);
            if (judgeText && refGoodLabel) CopyTMPStyle(refGoodLabel, judgeText);
            isHoldNotePerfect = false;
        }
        else  // MISS
        {
            missCount++;
            combo = 0;
            
            if (judgeText)
            {
                if (refMissLabel)
                {
                    CopyTMPStyle(refMissLabel, judgeText);
                }
                else
                {
                    judgeText.color = new Color(1f, 0.2f, 0.2f, 1f);
                }
            }
            
            isHoldNotePerfect = false;
        }

        if (judgeText)
        {
            StartCoroutine(ShowJudgeText(judge, false));
        }

        UpdateUI();
    }

public void AddScore(int amount)
    {
        perfectCount++;
        combo++;
        if (combo > maxChain) maxChain = combo;
        
        if (judgeText && refPerfectLabel)
        {
            CopyTMPStyle(refPerfectLabel, judgeText);
            judgeText.text = "PERFECT";
            
            if (!isHoldNotePerfect)
            {
                isHoldNotePerfect = true;
                StopAllCoroutines();
                StartCoroutine(ShowJudgeText("PERFECT", false));
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(ShowJudgeText("PERFECT", true));
            }
        }
        
        UpdateUI();
    }
    
    public void StopHoldPerfect()
    {
        isHoldNotePerfect = false;
        StopAllCoroutines();
        if (judgeText != null)
        {
            judgeText.text = "";
            judgeText.alpha = 1f;
        }
    }

    private void GainItemAndCombo(IngredientType type)
    {
        switch (type)
        {
            case IngredientType.Flour: flour++; break;
            case IngredientType.Milk: milk++; break;
            case IngredientType.Egg: egg++; break;
        }
        combo++;
        if (combo > maxChain) maxChain = combo;
    }

private IEnumerator ShowJudgeText(string judge, bool isHoldContinuous)
    {
        if (judgeText == null) yield break;

        if (judgeDisplayDelay > 0f && !isHoldContinuous)
        {
            yield return new WaitForSeconds(judgeDisplayDelay);
        }

        float holdDuration;
        float fadeDuration;
        float targetAlpha;

        if (judge == "PERFECT")
        {
            holdDuration = perfectHoldDuration;
            fadeDuration = perfectFadeDuration;
            targetAlpha = perfectAlpha;
        }
        else if (judge == "GOOD")
        {
            holdDuration = goodHoldDuration;
            fadeDuration = goodFadeDuration;
            targetAlpha = goodAlpha;
        }
        else  // MISS
        {
            holdDuration = missHoldDuration;
            fadeDuration = missFadeDuration;
            targetAlpha = missAlpha;
            
            // MISSの場合、ここでも色を設定
            if (refMissLabel)
            {
                CopyTMPStyle(refMissLabel, judgeText);
            }
            else
            {
                judgeText.color = new Color(1f, 0.2f, 0.2f, 1f);
            }
        }

        judgeText.text = judge;

        // ホールドノート連続PERFECT時のみ明滅
        if (judge == "PERFECT" && isHoldContinuous)
        {
            for (int i = 0; i < holdPerfectBlinkCount; i++)
            {
                judgeText.alpha = 0f;
                yield return new WaitForSeconds(holdPerfectBlinkInterval);
                judgeText.alpha = 1.0f;
                yield return new WaitForSeconds(holdPerfectBlinkInterval);
            }
        }
        else
        {
            judgeText.alpha = 1.0f;
            yield return new WaitForSeconds(judgeFullOpaqueDuration);
        }

        judgeText.alpha = targetAlpha;

        yield return new WaitForSeconds(holdDuration);

        // フェードアウト
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            judgeText.alpha = Mathf.Lerp(targetAlpha, 0f, t);
            yield return null;
        }

        if (!isHoldNotePerfect)
        {
            judgeText.text = "";
        }
        judgeText.alpha = 1f;
    }

    private void UpdateUI()
    {
        if (flourText) flourText.text = flour.ToString("00");
        if (milkText) milkText.text = milk.ToString("00");
        if (eggText) eggText.text = egg.ToString("00");

        if (comboText)
            comboText.text = combo > 0 ? $"COMBO {combo:00}" : "";
    }

    private static void CopyTMPStyle(TextMeshProUGUI src, TextMeshProUGUI dst)
    {
        if (!src || !dst) return;

        dst.color = src.color;
        dst.alpha = src.alpha;

        dst.enableVertexGradient = src.enableVertexGradient;
        if (src.enableVertexGradient)
            dst.colorGradient = src.colorGradient;

        if (src.font == dst.font && src.fontSharedMaterial != null)
            dst.fontSharedMaterial = src.fontSharedMaterial;
        else if (dst.font != null)
            dst.fontSharedMaterial = dst.font.material;
    }


public int CalculateTotalScore()
    {
        // スコア計算式:
        // PERFECT: 100点
        // GOOD: 50点
        // MISS: 0点
        // コンボボーナス: 最大コンボ × 10点
        
        int score = (perfectCount * 100) + (goodCount * 50);
        
        // ComboProbeから最大コンボを取得
        var comboProbe = FindObjectOfType<ComboProbe>();
        int maxCombo = comboProbe != null ? comboProbe.GetMaxCombo() : 0;
        
        int comboBonus = maxCombo * 10;
        return score + comboBonus;
    }
}
