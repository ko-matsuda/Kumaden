using UnityEngine;

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
    private bool hasEnded = false;
    private Vector3 cachedEndPos;

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
            if (wr != null) wr.enabled = false;
        }
    }

    void OnEnable()
    {
        hasStarted = false;
        hasEnded = false;
        Show();
    }

    void Update()
    {
        if (player == null || ribbonLine == null) return;
        
        if (hasEnded) return;

        Vector3 start = startNote != null && startNote.gameObject.activeInHierarchy 
                        ? startNote.position 
                        : Vector3.zero;
        Vector3 end = endNote != null && endNote.gameObject.activeInHierarchy 
                      ? endNote.position 
                      : cachedEndPos;
        
        if (endNote != null && endNote.gameObject.activeInHierarchy)
        {
            cachedEndPos = endNote.position;
        }

        float playerZ = player.position.z;

        if (start.z <= playerZ && !hasStarted)
        {
            hasStarted = true;
            if (debugLog) Debug.Log("[Ribbon] StartNote passed player");
        }

        if (end.z <= playerZ)
        {
            HideRibbon();
            return;
        }

        if (!hasStarted)
        {
            ribbonLine.SetPosition(0, start);
            ribbonLine.SetPosition(1, end);
            return;
        }

        Vector3 head = new Vector3(end.x, start.y, playerZ);
        ribbonLine.SetPosition(0, head);
        ribbonLine.SetPosition(1, end);
    }

    // 外部から呼び出し可能（Pickup.cs から）
    public void HideRibbon()
    {
        if (hasEnded) return;
        
        hasEnded = true;
        if (ribbonLine != null)
        {
            ribbonLine.gameObject.SetActive(false);
            if (debugLog) Debug.Log("[Ribbon] HideRibbon called - GameObject hidden");
        }
    }

    private void Show()
    {
        if (ribbonLine != null)
        {
            ribbonLine.gameObject.SetActive(true);
        }
    }

    void OnDisable()
    {
        if (ribbonLine != null)
        {
            ribbonLine.gameObject.SetActive(false);
        }
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