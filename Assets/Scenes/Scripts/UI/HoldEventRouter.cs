using UnityEngine;
using System.Reflection;

/// <summary>
/// ノートのイベント(開始/維持/終了)を、シーン上の RibbonBinder_L/M/R に届けるだけの最小ルーター。
/// 依存型は一切作らず、対象オブジェクト上の「BeginHold/ HoldTick/ EndHold(NoteBehaviour)」
/// を持つコンポーネントを反射で探して呼び出します。
/// （RibbonHoldController でも WorldRibbonNoteConnector でも動く）
/// </summary>
public class HoldEventRouter : MonoBehaviour
{
    const string NAME_L = "RibbonBinder_L";
    const string NAME_M = "RibbonBinder_M";
    const string NAME_R = "RibbonBinder_R";

    Component handlerL, handlerM, handlerR;

    void EnsureBinders()
    {
        if (handlerL && handlerM && handlerR) return;

        handlerL = FindHandlerByName(NAME_L);
        handlerM = FindHandlerByName(NAME_M);
        handlerR = FindHandlerByName(NAME_R);
#if UNITY_EDITOR
        if (!handlerL) Debug.LogWarning($"[Router] {NAME_L} の受け口が見つかりません");
        if (!handlerM) Debug.LogWarning($"[Router] {NAME_M} の受け口が見つかりません");
        if (!handlerR) Debug.LogWarning($"[Router] {NAME_R} の受け口が見つかりません");
#endif
    }

    Component FindHandlerByName(string goName)
    {
        var go = GameObject.Find(goName);
        if (!go) return null;

        // そのオブジェクト上の任意のコンポーネントから
        // public void BeginHold(NoteBehaviour) などのメソッドを持つものを選ぶ
        foreach (var c in go.GetComponents<Component>())
        {
            if (HasMethod(c, "BeginHold") || HasMethod(c, "HoldTick") || HasMethod(c, "EndHold"))
                return c;
        }
        return null;
    }

    bool HasMethod(Component c, string method)
    {
        if (!c) return false;
        var t = c.GetType();
        return t.GetMethod(method, BindingFlags.Instance | BindingFlags.Public,
                           null, new[] { typeof(NoteBehaviour) }, null) != null;
    }

    // ===== UnityEvent<NoteBehaviour> と同じシグネチャ =====
    public void BeginHold(NoteBehaviour note) => Route(note, "BeginHold");
    public void HoldTick (NoteBehaviour note) => Route(note, "HoldTick");
    public void EndHold  (NoteBehaviour note) => Route(note, "EndHold");

    void Route(NoteBehaviour note, string method)
    {
        if (!note) return;
        EnsureBinders();

        var lane = GetLaneIndex(note);       // 0=L,1=M,2=R （取り方は反射で安全に）
        var target = lane == 0 ? handlerL : (lane == 2 ? handlerR : handlerM);
        if (!target) return;

        var mi = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public,
                                            null, new[] { typeof(NoteBehaviour) }, null);
        if (mi != null) mi.Invoke(target, new object[] { note });
    }

    // NoteBehaviour の lane をプロパティ/フィールド名の違いに関係なく取得
    int GetLaneIndex(NoteBehaviour note)
    {
        var t = note.GetType();
        foreach (var n in new[] { "LaneIndex", "laneIndex", "lane", "Lane" })
        {
            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(int)) return Clamp012((int)p.GetValue(note));

            var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(int)) return Clamp012((int)f.GetValue(note));
        }
        return 1; // 取れなければ中央
    }

    int Clamp012(int v) => v < 0 ? 0 : (v > 2 ? 2 : v);
}
