// Assets/Scripts/ResultCaller.cs
// インゲーム終了時に Result を直接 ON せず、CookingResultSequence に任せて挟み込む。
// ★ ResultCanvas を SetActive(true) しないことがポイント。

using UnityEngine;

public class ResultCaller : MonoBehaviour
{
    [Header("参照")]
    public GameObject interstitialCanvas;        // InterstitialCanvas をドラッグ
    public GameObject resultCanvas;              // ResultCanvas をドラッグ
    public AudioSource resultBGM;                // （任意）Result の BGM AudioSource

    CookingResultSequence seq;

    void Awake()
    {
        if (interstitialCanvas)
            seq = interstitialCanvas.GetComponent<CookingResultSequence>();
    }

    // これをインゲーム終了時に呼ぶ
    public void OnGameEnd_ShowResultWithCookingMovie()
    {
        if (seq == null)
        {
            Debug.LogError("[ResultCaller] CookingResultSequence が見つかりません");
            return;
        }

        // ここでResultCanvasをONにしない！ → 早鳴り＆早表示の原因
        // resultCanvas.SetActive(true); ←禁止

        // 料理動画 → フェード → Resultフェードイン＆BGMフェードイン
        seq.StartSequence(resultCanvas, resultBGM);
    }
}
