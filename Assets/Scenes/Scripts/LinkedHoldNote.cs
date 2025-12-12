using UnityEngine;

/// <summary>
/// LinkedHoldNote - リボン表示専用版
/// ノーツがプレイヤーに向かってくる（Z座標が減少する）前提
/// </summary>
public class LinkedHoldNote : MonoBehaviour
{
    [Header("参照（必ず実ノーツを割当）")]
    [SerializeField] private Transform startNote;
    [SerializeField] private Transform endNote;
    [SerializeField] private Transform player;
    [SerializeField] private LineRenderer ribbonLine;

    [Header("見た目")]
    [Range(0.02f, 1.0f)] public float lineWidth = 0.30f;

    [Header("デバッグ")]
    public bool debugLog = false;

    private bool hasStarted = false;

    void Awake()
    {
        TryAutoDetect();

        if (ribbonLine != null)
        {
            ribbonLine.useWorldSpace = true;
            ribbonLine.positionCount = 2;
            ribbonLine.startWidth = lineWidth;
            ribbonLine.endWidth = lineWidth;

            var wr = ribbonLine.GetComponent("WorldRibbon") as MonoBehaviour;
            if (wr != null && wr.enabled) wr.enabled = false;
        }
    }

    void OnEnable()
    {
        hasStarted = false;
        ShowFull();
    }

    void Update()
    {
        if (!Ready()) return;

        Vector3 start = startNote.position;
        Vector3 end = endNote.position;
        float playerZ = player.position.z;

        // StartNote がプレイヤーを通過したか
        if (start.z <= playerZ && !hasStarted)
        {
            hasStarted = true;
            if (debugLog) Debug.Log("[Ribbon] StartNote passed player");
        }

        // EndNote がプレイヤーを通過したら非表示
        if (end.z <= playerZ)
        {
            Hide();
            return;
        }

        // StartNote がまだプレイヤーより前にある場合はフル表示
        if (!hasStarted)
        {
            ShowFull();
            return;
        }

        // StartNote を通過後、リボンを縮小表示
        // 先頭をプレイヤー位置に固定、終端は EndNote
        Vector3 head = start;
        head.z = playerZ;
        
        ribbonLine.SetPosition(0, head);
        ribbonLine.SetPosition(1, end);
        ribbonLine.enabled = true;

        if (debugLog) Debug.Log($"[Ribbon] Shortened: head={playerZ:F2}, end={end.z:F2}");
    }

    private bool Ready()
    {
        return (player != null && ribbonLine != null && startNote != null && endNote != null);
    }

    private void ShowFull()
    {
        if (ribbonLine == null || startNote == null || endNote == null) return;
        ribbonLine.startWidth = lineWidth;
        ribbonLine.endWidth = lineWidth;
        ribbonLine.SetPosition(0, startNote.position);
        ribbonLine.SetPosition(1, endNote.position);
        ribbonLine.enabled = true;
    }

    private void Hide()
    {
        if (ribbonLine != null)
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
    }
}