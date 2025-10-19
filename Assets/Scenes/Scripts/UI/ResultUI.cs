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

    [Header("Left")]
    public Image    rankBadge;
    public TMP_Text comboValueText;

    [Header("Judge")]
    public TMP_Text perfectValueText;
    public TMP_Text goodValueText;
    public TMP_Text missValueText;

    [Header("Items")]
    public TMP_Text milkCountText;
    public TMP_Text flourCountText;
    public TMP_Text eggCountText;

    // ランク→バッジ差し替え（必要ならSpriteを割当）
    public Sprite rankS, rankA, rankB, rankC;

    public void Bind(ResultData d)
    {
        if (comboValueText)   comboValueText.text   = d.maxCombo.ToString();
        if (perfectValueText) perfectValueText.text = d.perfect.ToString();
        if (goodValueText)    goodValueText.text    = d.good.ToString();
        if (missValueText)    missValueText.text    = d.miss.ToString();

        if (milkCountText)  milkCountText.text  = $"×{d.items.milk}";
        if (flourCountText) flourCountText.text = $"×{d.items.flour}";
        if (eggCountText)   eggCountText.text   = $"×{d.items.egg}";

        if (subText) subText.text = $"♪ 1st Verse Clear – {FormatTime(d.timeSec)}";

        if (rankBadge)
        {
            rankBadge.sprite = RankToSprite(d.rank);
            // rankBadge.SetNativeSize(); // 必要なら
        }
    }

    Sprite RankToSprite(string r)
    {
        switch ((r ?? "A").ToUpper())
        {
            case "S": return rankS ?? rankA;
            case "A": return rankA;
            case "B": return rankB ?? rankA;
            case "C": return rankC ?? rankA;
            default:  return rankA;
        }
    }

    static string FormatTime(float t)
    {
        var m = Mathf.FloorToInt(t / 60f);
        var s = t - m * 60f;
        return $"{m:00}:{s:00.00}";
    }
}
