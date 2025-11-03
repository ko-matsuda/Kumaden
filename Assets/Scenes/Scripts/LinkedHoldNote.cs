// Assets/Scenes/Scripts/LinkedHoldNote.cs
// KumaDen ホールド帯：Startノーツ→EndノーツをLineRendererで結ぶ。
// Start接触で帯の始端がプレイヤーZに追従して短縮、End通過で消灯。
// 参照がランタイムで破棄されても最後にキャッシュした座標で継続。

using System.Collections.Generic;
using UnityEngine;

public class LinkedHoldNote : MonoBehaviour
{
    [Header("参照（必ず実ノーツを割当）")]
    [SerializeField] private Transform startNote;      // 直前のノーツ
    [SerializeField] private Transform endNote;        // 次ノーツ（※LaneLなど親を入れない）
    [SerializeField] private Transform player;         // kuma_model など
    [SerializeField] private LineRenderer ribbonLine;  // WorldRibbon_Runtime の LineRenderer

    [Header("自動検出（必要な時だけ）")]
    [SerializeField] private bool autoFindNotes = false;

    [Header("見た目")]
    [Range(0.02f, 1.0f)] public float lineWidth = 0.30f;

    [Header("判定バッファ")]
    public float enterAhead = 2.0f;    // Start の少し手前から判定開始
    public float exitBehind = 0.5f;    // End を少し過ぎて終了

    [Header("プレイヤー前端オフセットZ")]
    public float playerFrontOffsetZ = 0.15f;

    [Header("連続カウント（Tick）")]
    public float tickInterval = 0.08f;
    public bool  debugLog = true;

    // 内部
    private bool  isInside = false;
    private float lastTickTime = -999f;

    // 参照が死んでも動かすためのキャッシュ
    private Vector3 startPos;
    private Vector3 endPos;

    // レーン判定閾値（Xの許容差）
    private const float LaneEps = 1.20f;

    void Awake()
    {
        TryAutoDetect();

        if (ribbonLine != null)
        {
            ribbonLine.useWorldSpace = true;
            ribbonLine.positionCount = 2;
            ribbonLine.startWidth = lineWidth;
            ribbonLine.endWidth   = lineWidth;

            // 他の制御を無効化（保険）
            var wr = ribbonLine.GetComponent("WorldRibbon") as MonoBehaviour;
            if (wr != null && wr.enabled) wr.enabled = false;
        }
    }

    void OnEnable()
    {
        CacheEndsFromRefs();
        ShowFull();
    }

    void Update()
    {
        if (!Ready()) return;

        // ノーツ参照が破棄されても座標で継続
        RefreshCachedEnds();

        // レーンが大きくズレていたら縮まずフル表示（誤反応防止）
        if (Mathf.Abs(PlayerPos().x - startPos.x) > LaneEps)
        {
            isInside = false;
            ShowFull();
            return;
        }

        float pz = PlayerPos().z;
        bool shouldEnter = (pz >= (startPos.z - enterAhead)) && (pz <= (endPos.z + exitBehind));

        if (!isInside && shouldEnter)
        {
            isInside = true;
            if (debugLog) Debug.Log("[Hold] ENTER");
        }
        else if (isInside && !shouldEnter)
        {
            isInside = false;
            Hide();
            if (debugLog) Debug.Log("[Hold] EXIT");
            return;
        }

        if (isInside)
        {
            // 始端をプレイヤー手前にクランプ
            float headZ = Mathf.Clamp(pz, startPos.z, endPos.z);
            Vector3 head = startPos; head.z = headZ;
            ribbonLine.SetPosition(0, head);
            ribbonLine.SetPosition(1, endPos);
            ribbonLine.enabled = true;

            if (tickInterval <= 0f || Time.unscaledTime - lastTickTime >= tickInterval)
            {
                lastTickTime = Time.unscaledTime;
                if (debugLog) Debug.Log("[Hold] TICK");
            }
        }
        else
        {
            ShowFull();
        }
    }

    // --------- 内部ユーティリティ ---------
    private bool Ready()
    {
        return (player != null && ribbonLine != null);
    }

    private Vector3 PlayerPos()
    {
        Vector3 p = player.position;
        p.z += playerFrontOffsetZ;
        return p;
    }

    private void CacheEndsFromRefs()
    {
        startPos = (startNote != null) ? startNote.position : startPos;
        endPos   = (endNote   != null) ? endNote.position   : endPos;

        // Start/End が逆なら入替
        if (endPos.z < startPos.z)
        {
            Vector3 t = startPos; startPos = endPos; endPos = t;
        }
    }

    private void RefreshCachedEnds()
    {
        if (startNote != null) startPos = startNote.position; // まだ生きていれば更新
        if (endNote   != null) endPos   = endNote.position;
    }

    private void ShowFull()
    {
        ribbonLine.startWidth = lineWidth;
        ribbonLine.endWidth   = lineWidth;
        ribbonLine.SetPosition(0, startPos);
        ribbonLine.SetPosition(1, endPos);
        ribbonLine.enabled = true;
    }

    private void Hide()
    {
        ribbonLine.enabled = false;
    }

    private void TryAutoDetect()
    {
        if (player == null)
        {
            var p = GameObject.Find("kuma_model");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) player = p.transform;
        }

        if (ribbonLine == null)
        {
            var wr = GameObject.Find("WorldRibbon_Runtime");
            if (wr == null) wr = GameObject.Find("WorldRibbon");
            if (wr != null)
            {
                var lr = wr.GetComponentInChildren<LineRenderer>();
                if (lr != null) ribbonLine = lr;
            }
        }

        if (!autoFindNotes) return;

        // NoteRoot 直下から NoteBehaviour を持つ“実ノーツ”のみを抜く
        var rootGO = GameObject.Find("NoteRoot");
        if (rootGO == null || player == null) return;

        var list = new List<Transform>();
        var rt = rootGO.transform;
        for (int i = 0; i < rt.childCount; i++)
        {
            var c = rt.GetChild(i);
            if (c.GetComponent("NoteBehaviour") != null) list.Add(c);
        }
        if (list.Count < 2) return;

        list.Sort((a, b) => a.position.z.CompareTo(b.position.z));

        float px = player.position.x;
        var sameLane = new List<Transform>();
        for (int i = 0; i < list.Count; i++)
            if (Mathf.Abs(list[i].position.x - px) <= LaneEps) sameLane.Add(list[i]);

        if (sameLane.Count < 2) return;

        float pz = player.position.z;
        int idx = -1;
        for (int i = 0; i < sameLane.Count; i++)
        {
            if (sameLane[i].position.z >= pz - 0.05f) { idx = i; break; }
        }
        if (idx >= 0 && idx + 1 < sameLane.Count)
        {
            startNote = sameLane[idx];
            endNote   = sameLane[idx + 1];
            if (debugLog) Debug.Log("[Hold] Auto: " + startNote.name + " -> " + endNote.name);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(startNote ? startNote.position : startPos, 0.1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(endNote ? endNote.position : endPos, 0.1f);
    }
#endif
}
