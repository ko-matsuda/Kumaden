using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct ResultItems { public int milk, flour, egg; }

[System.Serializable]
public struct ResultData
{
    public string rank;          // "S","A","B","C"
    public int maxCombo;
    public int perfect, good, miss;
    public ResultItems items;
    public float timeSec;        // 37.5f など
}

public class ResultUI : MonoBehaviour
{
    [Header("Header")]
    public TMP_Text titleText;
    public TMP_Text subText;

    [Header("Rank (Text Color)")]
    public TMP_Text rankText;     // ← テキストでランク表示
    public Color rankColorS = new Color32(0xFF,0xD1,0x00,255);
    public Color rankColorA = new Color32(0x2E,0x6F,0xBF,255);
    public Color rankColorB = new Color32(0x4C,0xAF,0x50,255);
    public Color rankColorC = new Color32(0xB0,0x86,0x5A,255);

    [Header("Others")]
    public TMP_Text comboValueText;
    public TMP_Text perfectValueText;
    public TMP_Text goodValueText;
    public TMP_Text missValueText;
    public TMP_Text milkCountText;
    public TMP_Text flourCountText;
    public TMP_Text eggCountText;

    public void Bind(ResultData d)
    {
        // Rank（文字と色）
        string r = (d.rank ?? "A").ToUpper();
        if (rankText)
        {
            rankText.text  = r;
            rankText.color = RankToColor(r);
        }

        if (comboValueText)   comboValueText.text   = d.maxCombo.ToString();
        if (perfectValueText) perfectValueText.text = d.perfect.ToString();
        if (goodValueText)    goodValueText.text    = d.good.ToString();
        if (missValueText)    missValueText.text    = d.miss.ToString();

        if (milkCountText)  milkCountText.text  = $"×{d.items.milk}";
        if (flourCountText) flourCountText.text = $"×{d.items.flour}";
        if (eggCountText)   eggCountText.text   = $"×{d.items.egg}";

        if (subText) subText.text = $"♪ 1st Verse Clear – {FormatTime(d.timeSec)}";
    }

    Color RankToColor(string r)
    {
        switch (r)
        {
            case "S": return rankColorS;
            case "A": return rankColorA;
            case "B": return rankColorB;
            case "C": return rankColorC;
            default:  return rankColorA;
        }
    }

    static string FormatTime(float t)
    {
        var m = Mathf.FloorToInt(t / 60f);
        var s = t - m * 60f;
        return $"{m:00}:{s:00.00}";
    }
}
