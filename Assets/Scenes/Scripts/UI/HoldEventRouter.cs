using UnityEngine;

public class HoldEventRouter : MonoBehaviour
{
    [Header("参照")]
    public Transform StartNote;     // 始点ノーツ
    public Transform EndNote;       // 終点ノーツ
    public Transform Player;        // クマ（進行基準）
    public LineRenderer Ribbon;     // シーン上の LineRenderer（WorldRibbon(Clone) など）

    [Header("動作")]
    [Tooltip("プレイヤーが終点を通過してどれだけ後ろに抜けたら非表示にするか（Z距離を正規化後の余剰）")]
    public float ExitBehind = 0.3f;

    [Tooltip("起動時に常時表示へ（イベントに依存せず表示を維持）")]
    public bool AlwaysOn = true;

    void Awake()
    {
        if (!Ribbon) Ribbon = GetComponent<LineRenderer>();
        if (Ribbon)
        {
            Ribbon.useWorldSpace = true;   // ローカル空間だと伸縮が見えないことがある
            Ribbon.positionCount = 2;
            Ribbon.enabled = true;         // 最初から見える
        }
    }

    void Update()
    {
        if (!Ribbon || !StartNote || !EndNote || !Player) return;

        // 毎フレーム、現在のノーツ位置を読む（ノーツが動くケースに対応）
        Vector3 a = StartNote.position;
        Vector3 b = EndNote.position;

        // Zが小さい方を「後ろ(a)」、大きい方を「前(b)」に統一（進行方向Z想定）
        if (a.z > b.z) { var tmp = a; a = b; b = tmp; }

        // プレイヤーZが a→b のどの位置かを 0..1 で算出
        float t = Mathf.InverseLerp(a.z, b.z, Player.position.z);
        t = Mathf.Clamp01(t);

        // プレイヤーが通過したぶんだけ「終点側」を手前に寄せる
        Vector3 newEnd = Vector3.Lerp(b, a, t);

        Ribbon.SetPosition(0, a);
        Ribbon.SetPosition(1, newEnd);
        Ribbon.enabled = true;

        // 完全通過後に少し後ろへ抜けたら消す（任意）
        if (!AlwaysOn && (Player.position.z > b.z + (b.z - a.z) * ExitBehind))
        {
            Ribbon.enabled = false;
        }
    }

    // —— 既存のイベントフックが残っていても動くようにダミーを置く（呼ばれても Update がやる）——
    public void BeginHold(Object _) { if (Ribbon) Ribbon.enabled = true; }
    public void HoldTick(Object _)  { /* Updateで処理する */ }
    public void EndHold(Object _)   { if (!AlwaysOn && Ribbon) Ribbon.enabled = false; }
}
