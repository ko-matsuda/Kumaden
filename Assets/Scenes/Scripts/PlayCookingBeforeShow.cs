// Assets/Scripts/PlayCookingBeforeShow.cs
// ResultCanvas にアタッチ。Result が出る直前に CRS を起動するだけ（本体制御は CRS）。

using UnityEngine;

public class PlayCookingBeforeShow : MonoBehaviour
{
    public bool oncePerRun = true;
    public CookingResultSequence seq;   // InterstitialCanvas の CookingResultSequence をドラッグ

    bool played;

    void OnEnable()
    {
        if (oncePerRun && played) return;
        played = true;

        if (!seq) { Debug.LogError("[PCS] CookingResultSequence 未参照"); return; }

        // ResultCanvas を CRS に渡して起動（OFF→動画→ON まで CRS が管理）
        seq.resultCanvas = gameObject;
        seq.StartSequence();
    }

    public void ResetForNextRun() => played = false;
}
