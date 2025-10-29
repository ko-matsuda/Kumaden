using UnityEngine;

// 既存のイベント(BeginHold/HoldTick/EndHold)はそのまま。
// BridgeTo の受け口を拡張して、NoteBehaviour や GameObject/Component からでも WorldRibbon を解決できるようにする。
public class HoldEventRouter : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public WorldRibbon ribbonL;
    public WorldRibbon ribbonM;
    public WorldRibbon ribbonR;

    // === UnityEvent などから呼ばれる想定のイベント ===
    public void BeginHold(UnityEngine.Object noteObj) { InternalBeginHold(ResolveGO(noteObj)); }
    public void HoldTick(UnityEngine.Object noteObj)  { InternalHoldTick(ResolveGO(noteObj)); }
    public void EndHold(UnityEngine.Object noteObj)   { InternalEndHold(ResolveGO(noteObj)); }

    public void BeginHold(GameObject go) { InternalBeginHold(go); }
    public void HoldTick(GameObject go)  { InternalHoldTick(go); }
    public void EndHold(GameObject go)   { InternalEndHold(go); }

    // ※ NoteBehaviour 型で直接飛んで来ても受けられるようオブジェクト版を用意（上の Object 版が拾います）
    // public void BeginHold(NoteBehaviour nb) { InternalBeginHold(ResolveGO(nb as UnityEngine.Object)); } // 必要なら有効化

    void InternalBeginHold(GameObject note)
    {
        if (ribbonM) ribbonM.SetExpose01(1f);
        // ここにノーツ開始処理を追加
    }

    void InternalHoldTick(GameObject note)
    {
        if (ribbonM) ribbonM.SetExpose01(1f);
        // 継続カウント処理など
    }

    void InternalEndHold(GameObject note)
    {
        if (ribbonM) ribbonM.SetExpose01(0f);
        // 終了処理
    }

    // === BridgeTo: さまざまな型から WorldRibbon へ橋渡しできる受け口を追加 ===

    // 既存：from(note), target(WorldRibbon)
    public void BridgeTo(UnityEngine.Object fromNote, WorldRibbon target)
    {
        BridgeTo(ResolveGO(fromNote), target);
    }

    // 新規：from(note), target(なんでも) -> 内部で WorldRibbon を解決
    public void BridgeTo(UnityEngine.Object fromNote, UnityEngine.Object ribbonTarget)
    {
        BridgeTo(ResolveGO(fromNote), ResolveRibbon(ribbonTarget));
    }

    // 実体
    public void BridgeTo(GameObject noteObj, WorldRibbon target)
    {
        if (!noteObj || !target) return;
        var a = noteObj.transform.position;
        var b = (player ? player.position : a + Vector3.forward);
        target.SetEndpoints(a, b);
        target.SetExpose01(1f);
    }

    // 直接座標版
    public void BridgeTo(Vector3 from, Vector3 to, WorldRibbon target)
    {
        if (!target) return;
        target.SetEndpoints(from, to);
        target.SetExpose01(1f);
    }

    // === 解決ユーティリティ ===
    GameObject ResolveGO(UnityEngine.Object obj)
    {
        if (!obj) return null;
        if (obj is GameObject go) return go;
        if (obj is Component c) return c.gameObject;
        return null;
    }

    WorldRibbon ResolveRibbon(UnityEngine.Object obj)
    {
        if (!obj) return null;
        if (obj is WorldRibbon wr) return wr;

        if (obj is Component c)
        {
            return c.GetComponent<WorldRibbon>() ?? c.GetComponentInChildren<WorldRibbon>(true);
        }

        if (obj is GameObject g)
        {
            return g.GetComponent<WorldRibbon>() ?? g.GetComponentInChildren<WorldRibbon>(true);
        }

        // NoteBehaviour など未知の型でも、同じ GameObject から探せるように
        return null;
    }
}
