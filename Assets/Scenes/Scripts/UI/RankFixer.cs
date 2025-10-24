using UnityEngine;
using TMPro;

public class RankFixer : MonoBehaviour
{
    public TMP_Text rankText;

    void Start()
    {
        if (!rankText) rankText = GetComponent<TMP_Text>();
        string t = rankText.text.Trim();

        // 文章が混ざっていたら最初の文字だけ残す
        if (t.Length > 1)
        {
            rankText.text = t.Substring(0, 1);
        }
    }
}
