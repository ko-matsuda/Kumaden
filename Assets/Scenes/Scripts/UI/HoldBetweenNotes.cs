using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// StartNote と EndNote の間を通過している間だけ「ホールド中」とみなし、
/// Enter / Tick / Exit のイベントを発火するシンプルなゾーン検出。
/// 衝突判定を使わず、プレイヤー位置の軸投影だけで判断します。
/// </summary>
[AddComponentMenu("KumaDen/Hold Between Notes")]
public class HoldBetweenNotes : MonoBehaviour
{
    // ---- 検出対象 ----
    public Transform startNote;     // 空なら子の NoteBehaviour から自動取得
    public Transform endNote;       // 空なら子の NoteBehaviour から自動取得
    public Transform player;        // 空なら Tag=Player を自動取得

    // ---- パラメータ ----
    [Tooltip("帯の太さの目安。ロジックには直接影響しませんが、将来の可視化で使用します。")]
    [Range(0f, 2f)] public float width = 0.3f;

    public enum Axis { X, Y, Z }
    [Tooltip("距離判定で使う軸（通常はZ）。")]
    public Axis lengthAxis = Axis.Z;

    [Header("判定バッファ（少し余裕を持たせる）")]
    [Tooltip("入口側にどれだけ手前から Enter するか（正の値で早めに入る）。")]
    public float enterAhead = 0.2f;
    [Tooltip("出口側をどれだけ奥まで Exit を遅らせるか（正の値で遅めに出る）。")]
    public float exitBehind = 0.3f;

    [Header("連続カウント（Tick）")]
    [Tooltip("ホールド中に Tick を打つ間隔（秒）。")]
    public float tickInterval = 0.08f;

    // ---- イベント（Inspector に出ます） ----
    [System.Serializable] public class NoteBehaviourEvent : UnityEvent<NoteBehaviour> { }
    public NoteBehaviourEvent onHoldEnter;
    public UnityEvent onHoldTick;
    public NoteBehaviourEvent onHoldExit;

    // ---- 内部状態 ----
    private bool _holding;
    private float _tickAccum;
    private NoteBehaviour _startNB;
    private NoteBehaviour _endNB;

    void Reset()
    {
        // 自動で子から拾う
        AutoAssignNotesIfEmpty();
    }

    void Awake()
    {
        AutoAssignNotesIfEmpty();
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (startNote == null || endNote == null || player == null) return;

        float p = GetAxis(player.position);
        float a = GetAxis(startNote.position);
        float b = GetAxis(endNote.position);

        // a <= b になるよう並び替え
        float min = a < b ? a : b;
        float max = a < b ? b : a;

        // 入口/出口にバッファを適用
        float enterEdge = min - enterAhead;
        float exitEdge  = max + exitBehind;

        bool inside = (p >= enterEdge) && (p <= exitEdge);

        if (!_holding && inside)
        {
            _holding = true;
            _tickAccum = 0f;
            // Start 側 NoteBehaviour を引数に渡す（無ければ null でも可）
            if (onHoldEnter != null) onHoldEnter.Invoke(_startNB);
        }

        if (_holding)
        {
            // Tick
            _tickAccum += Time.deltaTime;
            while (_tickAccum >= tickInterval)
            {
                _tickAccum -= tickInterval;
                if (onHoldTick != null) onHoldTick.Invoke();
            }

            // 外に出たら終了
            if (!inside)
            {
                _holding = false;
                if (onHoldExit != null) onHoldExit.Invoke(_endNB);
            }
        }
    }

    private float GetAxis(Vector3 v)
    {
        if (lengthAxis == Axis.X) return v.x;
        if (lengthAxis == Axis.Y) return v.y;
        return v.z; // Axis.Z
    }

    private void AutoAssignNotesIfEmpty()
    {
        if (startNote == null || endNote == null)
        {
            // 子孫から最初に見つかった NoteBehaviour を Start と End に割り当て
            NoteBehaviour[] nbs = GetComponentsInChildren<NoteBehaviour>(true);
            if (nbs != null && nbs.Length > 0)
            {
                // 軸位置が小さい方を Start、大きい方を End とみなす
                int si = 0, ei = 0;
                if (nbs.Length == 1)
                {
                    si = 0; ei = 0;
                }
                else
                {
                    float sPos = GetAxis(nbs[0].transform.position);
                    float bestMin = sPos;
                    float bestMax = sPos;
                    si = 0; ei = 0;
                    for (int i = 1; i < nbs.Length; i++)
                    {
                        float pos = GetAxis(nbs[i].transform.position);
                        if (pos < bestMin) { bestMin = pos; si = i; }
                        if (pos > bestMax) { bestMax = pos; ei = i; }
                    }
                }
                startNote = nbs[si].transform;
                endNote   = nbs[ei].transform;
                _startNB  = nbs[si];
                _endNB    = nbs[ei];
            }
        }
        else
        {
            _startNB = startNote.GetComponent<NoteBehaviour>();
            _endNB   = endNote   .GetComponent<NoteBehaviour>();
        }
    }
}
