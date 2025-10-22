// AutoPlayCookingSequence.cs
// 目的：ResultCanvas が表示されそうになった瞬間に割り込み、
// 「料理動画 → 終わったら ResultCanvas を出す」を自動で実行します。
// 既存のGameFlowを一切いじらずに挟み込み可能。

using UnityEngine;

public class AutoPlayCookingSequence : MonoBehaviour
{
    [Header("オブジェクト名（必要なら変更）")]
    public string resultCanvasName = "ResultCanvas";
    public string interstitialCanvasName = "InterstitialCanvas";
    public string prologueCanvasName = "PrologueCanvas";

    [Header("一度だけ再生する")]
    public bool playOncePerRun = true;

    bool handledThisRun = false;
    bool searching = true;

    GameObject resultCanvas;
    CookingResultSequence crs;

    void Start()
    {
        // CookingResultSequence を探す（無ければアタッチして使う）
        var ic = GameObject.Find(interstitialCanvasName);
        if (ic == null)
        {
            Debug.LogError("[AutoCook] InterstitialCanvas が見つかりません");
            return;
        }
        crs = ic.GetComponent<CookingResultSequence>();
        if (crs == null)
        {
            // ユーザーが既に CookingResultSequence.cs を入れている前提
            // まだ付いていなければ付ける
            crs = ic.AddComponent<CookingResultSequence>();
        }

        // ResultCanvas を後で探す（シーン遷移の都合で遅れて出てくる場合がある）
        InvokeRepeating(nameof(TryFindResultCanvas), 0f, 0.2f);
    }

    void TryFindResultCanvas()
    {
        if (resultCanvas == null)
        {
            resultCanvas = GameObject.Find(resultCanvasName);
            if (resultCanvas != null)
            {
                searching = false;
                CancelInvoke(nameof(TryFindResultCanvas));
                // 以降、Updateで監視
            }
        }
    }

    void Update()
    {
        if (searching || resultCanvas == null || crs == null) return;

        // 既に一度処理したなら、次のラン開始まで待つ
        if (playOncePerRun && handledThisRun) return;

        // ResultCanvas が表示されそうになった瞬間に割り込む
        if (resultCanvas.activeSelf)
        {
            // いったん隠して動画へ
            resultCanvas.SetActive(false);
            handledThisRun = true;

            // プロローグ等を隠す（重複再生防止）
            var prologue = GameObject.Find(prologueCanvasName);
            if (prologue) prologue.SetActive(false);

            // 動画を開始（CookingResultSequence が動画 → 終了後に ResultCanvas を出します）
            crs.StartSequence();
        }
    }

    // ランが再スタートしたらフラグをリセットしたい場合、外部からこれを呼んでください
    public void ResetForNextRun()
    {
        handledThisRun = false;
    }
}
