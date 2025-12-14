using UnityEngine;

public class LinkedHoldNote : MonoBehaviour
{
    [Header("参照（必ず実ノーツを割当）")]
    [SerializeField] private Transform startNote;
    [SerializeField] private Transform endNote;
    [SerializeField] private Transform player;

    [Header("移動")]
    public float scrollSpeed = 12f;

    [Header("見た目")]
    [Range(0.02f, 1.0f)] public float lineWidth = 0.30f;
    [Tooltip("帯のマテリアル（Glow系など）を直接設定")]
    public Material ribbonMaterial;
    [Tooltip("WorldRibbon_Runtime等の既存LineRendererからマテリアルをコピーする場合に指定")]
    public LineRenderer sourceLineRenderer;
    public Color ribbonColor = new Color(1f, 0.9f, 0.2f, 1f);

    [Header("デバッグ")]
    public bool debugLog = false;

    private LineRenderer ribbonLine;
    private bool hasStarted = false;
    private bool hasEnded = false;
    private Vector3 cachedStartPos;
    private bool startNoteDestroyed = false;

    void Awake()
    {
        TryAutoDetect();
        CreateOwnLineRenderer();
    }

    void OnEnable()
    {
        hasStarted = false;
        hasEnded = false;
        startNoteDestroyed = false;
        if (ribbonLine != null)
        {
            ribbonLine.enabled = true;
        }
    }

    void Update()
    {
        // 移動処理
        if (!hasEnded)
        {
            transform.position += Vector3.back * scrollSpeed * Time.deltaTime;
        }

        if (player == null || ribbonLine == null) return;
        
        if (hasEnded)
        {
            ribbonLine.enabled = false;
            return;
        }

        // StartNote の位置を取得（破棄されていたらキャッシュを使用）
        Vector3 start;
        if (startNote != null && startNote.gameObject.activeInHierarchy)
        {
            start = startNote.position;
            cachedStartPos = start;
        }
        else
        {
            startNoteDestroyed = true;
            start = cachedStartPos;
        }

        // EndNote の位置を取得
        Vector3 end;
        if (endNote != null && endNote.gameObject.activeInHierarchy)
        {
            end = endNote.position;
        }
        else
        {
            // EndNote も破棄されたら終了
            HideRibbon();
            return;
        }

        float playerZ = player.position.z;

        // StartNote が Player を通過したら縮小開始
        if (startNoteDestroyed && !hasStarted)
        {
            hasStarted = true;
            if (debugLog) Debug.Log("[Ribbon] StartNote destroyed - shrinking started");
        }
        else if (!startNoteDestroyed && start.z <= playerZ && !hasStarted)
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
            // フル表示
            ribbonLine.SetPosition(0, start);
            ribbonLine.SetPosition(1, end);
        }
        else
        {
            // StartNote 通過後、リボンを縮小（プレイヤー位置から）
            Vector3 head = new Vector3(cachedStartPos.x, cachedStartPos.y, playerZ);
            ribbonLine.SetPosition(0, head);
            ribbonLine.SetPosition(1, end);
        }
    }

    public void HideRibbon()
    {
        hasEnded = true;
        if (ribbonLine != null)
        {
            ribbonLine.enabled = false;
            if (debugLog) Debug.Log("[Ribbon] HideRibbon called");
        }
    }

    void OnDisable()
    {
        if (ribbonLine != null)
        {
            ribbonLine.enabled = false;
        }
    }

    void OnDestroy()
    {
        if (ribbonLine != null && ribbonLine.gameObject != null)
        {
            Destroy(ribbonLine.gameObject);
        }
    }

    private void CreateOwnLineRenderer()
    {
        // 専用の LineRenderer を子オブジェクトとして生成
        GameObject lineObj = new GameObject("RibbonLine");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;

        ribbonLine = lineObj.AddComponent<LineRenderer>();
        ribbonLine.useWorldSpace = true;
        ribbonLine.positionCount = 2;
        ribbonLine.startWidth = lineWidth;
        ribbonLine.endWidth = lineWidth;

        // マテリアル設定（優先順位: ribbonMaterial > sourceLineRenderer > 自動検出 > デフォルト）
        if (ribbonMaterial != null)
        {
            // 直接設定されたマテリアルを使用
            ribbonLine.material = ribbonMaterial;
            ribbonLine.startColor = ribbonColor;
            ribbonLine.endColor = ribbonColor;
        }
        else if (sourceLineRenderer != null && sourceLineRenderer.sharedMaterial != null)
        {
            // 既存のLineRendererからマテリアルをコピー
            ribbonLine.material = sourceLineRenderer.sharedMaterial;
            ribbonLine.startColor = sourceLineRenderer.startColor;
            ribbonLine.endColor = sourceLineRenderer.endColor;
        }
        else
        {
            // URPの場合はUnlit/Colorを使う
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            
            Material mat = new Material(shader);
            mat.color = ribbonColor;
            ribbonLine.material = mat;
            ribbonLine.startColor = ribbonColor;
            ribbonLine.endColor = ribbonColor;
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

        if (startNote == null)
        {
            startNote = transform.Find("StartNote");
        }

        if (endNote == null)
        {
            endNote = transform.Find("EndNote");
        }

        // WorldRibbon_Runtimeからマテリアルをコピーする
        if (sourceLineRenderer == null)
        {
            var wr = GameObject.Find("WorldRibbon_Runtime");
            if (wr != null)
            {
                sourceLineRenderer = wr.GetComponent<LineRenderer>();
            }
        }
    }
}