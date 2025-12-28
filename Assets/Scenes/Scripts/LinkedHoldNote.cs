using UnityEngine;

public class LinkedHoldNote : MonoBehaviour
{
    [Header("参照（必ず実ノーツを割当）")]
    [SerializeField] private Transform startNote;
    [SerializeField] private Transform endNote;
    [SerializeField] private Transform player;

    [Header("移動")]
    public float scrollSpeed = 12f;

    [Header("見た目 - 基本")]
    [Range(0.02f, 2.0f)] public float lineWidth = 0.30f;
    [Tooltip("帯のマテリアル（Glow系など）を直接設定")]
    public Material ribbonMaterial;
    [Tooltip("WorldRibbon_Runtime等の既存LineRendererからマテリアルをコピーする場合に指定")]
    public LineRenderer sourceLineRenderer;
    public Color ribbonColor = new Color(1f, 0.9f, 0.2f, 1f);

    [Header("見た目 - 演出調整（描画表現のみ）")]
    [SerializeField, Range(1.0f, 2.0f), Tooltip("基本太さ倍率（推奨1.4）")]
    private float baseWidthMultiplier = 1.4f;
    
    [SerializeField, Range(1.0f, 1.5f), Tooltip("Hold中ピーク太さ倍率（推奨1.2）")]
    private float holdPeakMultiplier = 1.2f;
    
    [SerializeField, Range(0.05f, 0.2f), Tooltip("Start接触時の明度上昇時間（秒）")]
    private float startFlashDuration = 0.1f;
    
    [SerializeField, Range(0.1f, 0.3f), Tooltip("Start接触時の明度上昇量")]
    private float startFlashBrightness = 0.18f;
    
    [SerializeField, Range(0.0f, 0.1f), Tooltip("Hold中の明度揺れ振幅（背景揺れ）")]
    private float holdFlickerAmount = 0.05f;
    
    [SerializeField, Range(0.3f, 0.8f), Tooltip("Hold中の揺らぎ周期（秒）")]
    private float holdFlickerCycle = 0.5f;
    
    [SerializeField, Range(0.05f, 0.1f), Tooltip("入力成立時の明度上昇量（+5〜8%）")]
    private float inputActiveBrightness = 0.07f;
    
    [SerializeField, Range(1.0f, 1.08f), Tooltip("入力成立時の太さ倍率（+5%前後）")]
    private float inputActiveWidthMultiplier = 1.05f;
    
    [SerializeField, Range(0.0005f, 0.002f), Tooltip("太さ揺らぎ振幅（px相当）")]
    private float widthFlickerAmount = 0.001f;
    
    [SerializeField, Range(0.8f, 1.0f), Tooltip("触れる前の太さ倍率（推奨0.85 = Hold中より細い）")]
    private float beforeHoldWidthMultiplier = 0.85f;

    [Header("拍頭微強調（Hold中のみ）")]
    [SerializeField, Range(0.02f, 0.04f), Tooltip("拍頭強調の明度上乗せ（+2〜3%推奨）")]
    private float beatAccentBrightness = 0.025f;
    
    [SerializeField, Range(0.03f, 0.08f), Tooltip("拍頭強調の持続時間（秒）")]
    private float beatAccentDuration = 0.04f;
    
    [SerializeField, Range(2, 8), Tooltip("何拍に1回強調するか（4=小節頭のみ）")]
    private int beatAccentInterval = 4;

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
    
    private float holdTimer = 0f;
    private float startFlashTimer = 0f;
    private bool startFlashActive = false;
    private Color baseColor;
    private float baseWidth;
    private bool inputActiveThisFrame = false;
    
    // ★ 拍頭微強調用（新規追加）
    private float beatAccentTimer = 0f;     // 拍頭強調の持続タイマー
    private bool beatAccentActive = false;  // 拍頭強調中フラグ
    private int lastBeatIndex = -1;         // 前回処理した拍インデックス

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
        
        baseColor = ribbonColor;
        baseWidth = lineWidth * baseWidthMultiplier;
    }

    void OnEnable()
    {
        hasStarted = false;
        hasEnded = false;
        startNoteDestroyed = false;
        holdTimer = 0f;
        startFlashTimer = 0f;
        startFlashActive = false;
        inputActiveThisFrame = false;
        beatAccentTimer = 0f;
        beatAccentActive = false;
        lastBeatIndex = -1;
        
        if (ribbonLine != null)
        {
            ribbonLine.enabled = true;
        }
    }

    void Update()
    {
        inputActiveThisFrame = false;

        if (!hasEnded)
        {
            transform.position += Vector3.back * scrollSpeed * Time.deltaTime;
        }

        if (player == null || ribbonLine == null) return;

        if (isHolding)
        {
            holdTimer += Time.deltaTime;
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer = 0f;
                AddTickScore();
            }
            
            // ★ 拍頭検出（Hold中のみ）
            CheckBeatAccent();
        }

        if (hasEnded)
        {
            ribbonLine.enabled = false;
            return;
        }

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

        Vector3 end;
        if (endNote != null && endNote.gameObject.activeInHierarchy)
        {
            end = endNote.position;
        }
        else
        {
            HideRibbon();
            return;
        }

        float playerZ = player.position.z;

        if (startNoteDestroyed && !isHolding)
        {
            isHolding = true;
            holdTimer = 0f;
            startFlashActive = true;
            startFlashTimer = 0f;
            tickTimer = 0f;
            if (debugLog) Debug.Log("[Ribbon] Hold started");
        }

        if (startNoteDestroyed && !hasStarted)
        {
            hasStarted = true;
            if (debugLog) Debug.Log("[Ribbon] StartNote destroyed - shrinking started");
        }
        else if (!startNoteDestroyed && start.z <= playerZ && !hasStarted)
        {
            hasStarted = true;
            if (!isHolding)
            {
                if (debugLog) Debug.Log("[LinkedHoldNote] MISS - StartNote passed without holding");
                var comboProbe = FindObjectOfType<ComboProbe>();
                if (comboProbe != null)
                {
                    comboProbe.OnNoteMiss();
                }
                var scoreMgr = ScoreManagerLite.Instance;
                if (scoreMgr != null)
                {
                    scoreMgr.OnPick(IngredientType.Milk, "MISS");
                }
                if (startNote != null) Destroy(startNote.gameObject);
                if (endNote != null) Destroy(endNote.gameObject);
                hasEnded = true;
                HideRibbon();
                Destroy(gameObject, 0.1f);
                return;
            }
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
        }
        else
        {
            Vector3 head = new Vector3(cachedStartPos.x, cachedStartPos.y, playerZ);
            ribbonLine.SetPosition(0, head);
            ribbonLine.SetPosition(1, end);
        }

        UpdateVisualEffects();
    }

    // ★ 拍頭検出メソッド（新規追加・Hold中のみ呼ばれる）
    private void CheckBeatAccent()
    {
        // Conductorから現在の拍位置を取得
        var conductor = FindObjectOfType<Conductor>();
        if (conductor == null) return;

        // 現在の拍インデックス（整数部分）
        int currentBeatIndex = Mathf.FloorToInt(conductor.songPositionBeats);

        // 拍頭判定：指定間隔の倍数 かつ 前回と違う拍
        bool isBeatHead = (currentBeatIndex % beatAccentInterval == 0) && (currentBeatIndex != lastBeatIndex);

        if (isBeatHead)
        {
            // 拍頭強調開始
            beatAccentActive = true;
            beatAccentTimer = 0f;
            lastBeatIndex = currentBeatIndex;
        }

        // 拍頭強調の持続時間管理
        if (beatAccentActive)
        {
            beatAccentTimer += Time.deltaTime;
            if (beatAccentTimer >= beatAccentDuration)
            {
                beatAccentActive = false;
            }
        }
    }

    private void UpdateVisualEffects()
    {
        float currentWidth = baseWidth;
        Color currentColor = baseColor;

        if (startFlashActive)
        {
            startFlashTimer += Time.deltaTime;
            if (startFlashTimer < startFlashDuration)
            {
                float t = startFlashTimer / startFlashDuration;
                float brightness = Mathf.Lerp(startFlashBrightness, 0f, t);
                currentColor = BrightenColor(baseColor, brightness);
            }
            else
            {
                startFlashActive = false;
            }
        }

        // Hold中の演出
        if (isHolding && !hasEnded)
        {
            float holdWidthMult = holdPeakMultiplier;
            float baseFlicker = Mathf.Sin(holdTimer / holdFlickerCycle * Mathf.PI * 2f) * widthFlickerAmount;
            currentWidth = baseWidth * holdWidthMult + baseFlicker;
            
            float brightFlicker = Mathf.Sin(holdTimer / holdFlickerCycle * Mathf.PI * 2f) * holdFlickerAmount;
            currentColor = BrightenColor(baseColor, brightFlicker);
            
            // 入力成立時の強調（既存・維持）
            if (inputActiveThisFrame)
            {
                currentColor = BrightenColor(currentColor, inputActiveBrightness);
                currentWidth *= inputActiveWidthMultiplier;
            }
            
            // ★ 拍頭微強調（新規追加・入力成立中のみ有効）
            // inputActiveThisFrame が true = 判定OK の瞬間のみ
            // 判定が途切れたら即座に無効化される
            if (beatAccentActive)
            {
                // 明度のみ +2〜3% 上乗せ（太さは変えない）
                currentColor = BrightenColor(currentColor, beatAccentBrightness);
            }
        }
        else
        {
            // 触れる前は細くする
            currentWidth = baseWidth * holdPeakMultiplier * beforeHoldWidthMultiplier;
        }

        ribbonLine.startWidth = currentWidth;
        ribbonLine.endWidth = currentWidth;
        ribbonLine.startColor = currentColor;
        ribbonLine.endColor = currentColor;
    }

    private Color BrightenColor(Color original, float amount)
    {
        Color.RGBToHSV(original, out float h, out float s, out float v);
        v = Mathf.Clamp01(v + amount);
        Color result = Color.HSVToRGB(h, s, v);
        result.a = original.a;
        return result;
    }

    public void HideRibbon()
    {
        if (debugLog) Debug.Log($"[Ribbon] HideRibbon called - isHolding was: {isHolding}");
        hasEnded = true;
        isHolding = false;
        beatAccentActive = false; // 拍頭強調も停止
        if (ribbonLine != null)
        {
            ribbonLine.enabled = false;
        }
    }

    public bool IsHolding()
    {
        return isHolding;
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
        GameObject lineObj = new GameObject("RibbonLine");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;

        ribbonLine = lineObj.AddComponent<LineRenderer>();
        ribbonLine.useWorldSpace = true;
        ribbonLine.positionCount = 2;
        
        float adjustedWidth = lineWidth * baseWidthMultiplier;
        ribbonLine.startWidth = adjustedWidth;
        ribbonLine.endWidth = adjustedWidth;

        if (debugLog) Debug.Log($"[LinkedHoldNote] Creating LineRenderer - width={adjustedWidth}, color={ribbonColor}");

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
        inputActiveThisFrame = true;
        
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
            float colliderSize = 1.0f;
            float scale = transform.lossyScale.z;
            float adjustedFullSize = colliderSize * scale;
            float targetWorldZ = transform.position.z + distanceZ - adjustedFullSize;
            
            if (debugLog)
            {
                Debug.Log($"[LinkedHoldNote] Scale={scale:F2}, BoxCollider size={colliderSize:F2}, Adjusted={adjustedFullSize:F2}");
                Debug.Log($"[LinkedHoldNote] Target world.z = parent({transform.position.z:F2}) + distance({distanceZ:F2}) - size({adjustedFullSize:F2}) = {targetWorldZ:F2}");
            }
            
            Vector3 worldPos = endNote.position;
            worldPos.z = targetWorldZ;
            endNote.position = worldPos;
            Physics.SyncTransforms();
            
            if (debugLog)
            {
                Debug.Log($"[LinkedHoldNote] EndNote set - local.z={endNote.localPosition.z:F2}, world.z={endNote.position.z:F2}");
                Debug.Log($"[LinkedHoldNote] Expected={targetWorldZ:F2}, Actual={endNote.position.z:F2}, Diff={Mathf.Abs(targetWorldZ - endNote.position.z):F2}");
            }
        }
    }
}
