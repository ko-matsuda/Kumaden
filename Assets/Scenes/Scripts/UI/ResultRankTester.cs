using UnityEngine;
using TMPro;

public class ResultRankTester : MonoBehaviour
{
    public TMP_Text rankText;
    public Color colorA = new Color(1f, 0.95f, 0.2f);

    void OnEnable()
    {
        if (!rankText) return;
        rankText.text = "A";      // 仮で「A」を表示
        rankText.color = colorA;  // 色も仮で変更
    }
}
