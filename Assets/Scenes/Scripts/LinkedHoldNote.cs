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

    [Header("レーン情報")]
    public int laneIndex = 0;

    [Header("ホールド中のスコア")]
    public float tickInterval = 0.2f;
    public int tickScore = 10;
    public AudioClip tickSound;
    private AudioSource audioSource;
    private float tickTimer = 0f;
    private bool isHolding = false;

    [Header("デバッグ")]
    public bool debugLog = true;

    private LineRenderer ribbonLine;
    private bool hasStarted = false;
    private bool hasEnded = false;
    private Vector3 cachedStartPos;
    private bool startNoteDestroyed = false;

    void Awake()
    {
        TryAutoDetect();
        CreateOwnLineRenderer();
        
        audioSource = gameObject.AddComponent<AudioSource>();
        if (tickSound != null)
        {
            audioSource.clip = tickSound;
            audioSource.playOnAwake = false;
        }
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

void Update() { if (!hasEnded) { transform.position += Vector3.back * scrollSpeed * Time.deltaTime; } if (player == null || ribbonLine == null) return; if (isHolding) { tickTimer += Time.deltaTime; if (tickTimer >= tickInterval) { tickTimer = 0f; AddTickScore(); } } if (hasEnded) { ribbonLine.enabled = false; return; } Vector3 start; if (startNote != null && startNote.gameObject.activeInHierarchy) { start = startNote.position; cachedStartPos = start; } else { startNoteDestroyed = true; start = cachedStartPos; } Vector3 end; if (endNote != null && endNote.gameObject.activeInHierarchy) { end = endNote.position; } else { HideRibbon(); return; } float playerZ = player.position.z; if (startNoteDestroyed && !isHolding) { isHolding = true; tickTimer = 0f; if (debugLog) Debug.Log("[Ribbon] Hold started"); } if (startNoteDestroyed && !hasStarted) { hasStarted = true; if (debugLog) Debug.Log("[Ribbon] StartNote destroyed - shrinking started"); } else if (!startNoteDestroyed && start.z <= playerZ && !hasStarted) { hasStarted = true; if (!isHolding) { if (debugLog) Debug.Log("[LinkedHoldNote] MISS - StartNote passed without holding"); var comboProbe = FindObjectOfType<ComboProbe>(); if (comboProbe != null) { comboProbe.OnNoteMiss(); } var scoreMgr = ScoreManagerLite.Instance; if (scoreMgr != null) { scoreMgr.OnPick(IngredientType.Milk, "MISS"); } if (startNote != null) Destroy(startNote.gameObject); if (endNote != null) Destroy(endNote.gameObject); hasEnded = true; HideRibbon(); Destroy(gameObject, 0.1f); return; } if (debugLog) Debug.Log("[Ribbon] StartNote passed player"); } if (end.z <= playerZ) { HideRibbon(); return; } if (!hasStarted) { ribbonLine.SetPosition(0, start); ribbonLine.SetPosition(1, end); } else { Vector3 head = new Vector3(cachedStartPos.x, cachedStartPos.y, playerZ); ribbonLine.SetPosition(0, head); ribbonLine.SetPosition(1, end); } }

        public void HideRibbon()
    {
        if (debugLog) Debug.Log($"[Ribbon] HideRibbon called - isHolding was: {isHolding}");
        hasEnded = true;
        isHolding = false;
        if (ribbonLine != null)
        {
            ribbonLine.enabled = false;
        }
    }

public bool IsHolding() { return isHolding; }


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
        GameObject lineObj = new GameObject("RibbonLine");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;

        ribbonLine = lineObj.AddComponent<LineRenderer>();
        ribbonLine.useWorldSpace = true;
        ribbonLine.positionCount = 2;
        ribbonLine.startWidth = lineWidth;
        ribbonLine.endWidth = lineWidth;

        if (debugLog) Debug.Log($"[LinkedHoldNote] Creating LineRenderer - width={lineWidth}, color={ribbonColor}");

        if (ribbonMaterial != null)
        {
            ribbonLine.material = ribbonMaterial;
            ribbonLine.startColor = ribbonColor;
            ribbonLine.endColor = ribbonColor;
            if (debugLog) Debug.Log($"[LinkedHoldNote] Using ribbonMaterial: {ribbonMaterial.name}");
        }
        else if (sourceLineRenderer != null && sourceLineRenderer.sharedMaterial != null)
        {
            ribbonLine.material = sourceLineRenderer.sharedMaterial;
            ribbonLine.startColor = sourceLineRenderer.startColor;
            ribbonLine.endColor = sourceLineRenderer.endColor;
            if (debugLog) Debug.Log($"[LinkedHoldNote] Copied material from sourceLineRenderer: {sourceLineRenderer.sharedMaterial.name}");
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            
            if (debugLog) Debug.Log($"[LinkedHoldNote] Creating new material with shader: {(shader != null ? shader.name : "NULL")}");
            
            Material mat = new Material(shader);
            mat.color = ribbonColor;
            ribbonLine.material = mat;
            ribbonLine.startColor = ribbonColor;
            ribbonLine.endColor = ribbonColor;
        }
        
        if (debugLog) Debug.Log($"[LinkedHoldNote] LineRenderer created - enabled={ribbonLine.enabled}, width={ribbonLine.startWidth}");
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

        if (ribbonMaterial == null && sourceLineRenderer == null)
        {
            var wr = GameObject.Find("WorldRibbon_Runtime");
            if (wr != null)
            {
                sourceLineRenderer = wr.GetComponent<LineRenderer>();
            }
        }
    }

    private void AddTickScore()
    {
        if (debugLog) Debug.Log($"[LinkedHoldNote] AddTickScore called - lane={laneIndex}");
        
        var hudCounter = FindObjectOfType<HudCounterBinder>();
        if (hudCounter != null)
        {
            hudCounter.OnHoldTick(laneIndex);
            if (debugLog) Debug.Log($"[LinkedHoldNote] Called HudCounterBinder.OnHoldTick({laneIndex})");
        }
        else
        {
            if (debugLog) Debug.LogWarning("[LinkedHoldNote] HudCounterBinder not found!");
        }
        
        var comboProbe = FindObjectOfType<ComboProbe>();
        if (comboProbe != null)
        {
            comboProbe.OnHoldTick();
            if (debugLog) Debug.Log("[LinkedHoldNote] Called ComboProbe.OnHoldTick()");
        }
        else
        {
            if (debugLog) Debug.LogWarning("[LinkedHoldNote] ComboProbe not found!");
        }
        
        if (audioSource != null && tickSound != null)
        {
            audioSource.PlayOneShot(tickSound);
        }

        if (debugLog) Debug.Log($"[LinkedHoldNote] Tick! lane={laneIndex}, interval={tickInterval}s");
    }

public void SetEndNoteDistance(float distanceZ)
    {
        if (endNote != null)
        {
            // CRITICAL: BoxCollider size=1.0, scale=0.34
            // 帯の終点をEndNoteのColliderの完全に手前に配置
            float colliderSize = 1.0f;  // BoxCollider full size
            float scale = transform.lossyScale.z;
            float adjustedFullSize = colliderSize * scale;  // 0.34
            
            // 帯の終点 = parent + distance - fullSize (Collider全体分引く)
            float targetWorldZ = transform.position.z + distanceZ - adjustedFullSize;
            
            if (debugLog)
            {
                Debug.Log($"[LinkedHoldNote] Scale={scale:F2}, BoxCollider size={colliderSize:F2}, Adjusted={adjustedFullSize:F2}");
                Debug.Log($"[LinkedHoldNote] Target world.z = parent({transform.position.z:F2}) + distance({distanceZ:F2}) - size({adjustedFullSize:F2}) = {targetWorldZ:F2}");
            }
            
            // world positionで直接設定
            Vector3 worldPos = endNote.position;
            worldPos.z = targetWorldZ;
            endNote.position = worldPos;
            
            // TransformとPhysicsを強制同期
            Physics.SyncTransforms();
            
            if (debugLog)
            {
                Debug.Log($"[LinkedHoldNote] EndNote set - local.z={endNote.localPosition.z:F2}, world.z={endNote.position.z:F2}");
                Debug.Log($"[LinkedHoldNote] Expected={targetWorldZ:F2}, Actual={endNote.position.z:F2}, Diff={Mathf.Abs(targetWorldZ - endNote.position.z):F2}");
            }
        }
    }
}
