using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 帯がアクティブの間だけ連続で Tick を発火させるドライバ。
/// ・StartTick() または OnHoldEnter() で開始
/// ・StopTick() または OnHoldExit() で停止
/// ・毎フレーム or 一定Hz で UnityEvent OnTick を呼ぶ
/// 
/// 【配線方法】
/// - Pickup.cs から StartTick()/StopTick() を呼ぶ
/// - OnTick に ComboProbe.OnHoldTick / HudCounterBinder.OnHoldTick を設定
/// - OnEnter に ComboProbe.OnHoldEnter / HudCounterBinder.OnHoldEnter を設定
/// - OnExit に ComboProbe.OnHoldExit / HudCounterBinder.OnHoldExit を設定
/// </summary>
public sealed class HoldTickPulse : MonoBehaviour
{
    [Header("Tick Mode")]
    [SerializeField] private bool perFrame = true;
    [SerializeField] private float tickRateHz = 30f;

    [Header("Gating")]
    [SerializeField] private bool useGating = true;
    [SerializeField] private bool isGated = true;

    [Header("レーン情報")]
    public int laneIndex = 0;

    [Header("VFX")]
    public HitVFXPool hitVFXPool;
    public Transform playerTransform;
    public Vector3 vfxOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private float vfxInterval = 0.1f;
    private float vfxTimer = 0f;

    [Header("Events")]
    public UnityEvent OnTick;
    public UnityEvent OnEnter;
    public UnityEvent OnExit;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private ComboProbe comboProbe;
    private JudgeTextBlinker judgeTextBlinker;
    private bool _active = false;
    private float _accum;

    private void Awake()
    {
        // Playerを自動検索
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                if (debugLog) Debug.Log("[HoldTickPulse] Player found and assigned");
            }
        }

        // HitVFXPoolを自動検索
        if (hitVFXPool == null)
        {
            hitVFXPool = FindObjectOfType<HitVFXPool>();
            if (hitVFXPool != null && debugLog)
            {
                Debug.Log("[HoldTickPulse] HitVFXPool found and assigned");
            }
        }
    }

    public void StartTick()
    {
        if (judgeTextBlinker == null)
        {
            judgeTextBlinker = FindObjectOfType<JudgeTextBlinker>();
        }
        if (comboProbe == null)
        {
            comboProbe = FindObjectOfType<ComboProbe>();
        }
        
        _active = true;
        _accum = 0f;
        vfxTimer = 0f;
        isGated = false;
        OnEnter?.Invoke();
        if (debugLog) Debug.Log("[HoldTickPulse] StartTick - Started (_active = true)");
    }

    public void StopTick()
    {
        Debug.Log("[HoldTickPulse] ========== StopTick START ==========");
        Debug.Log("[HoldTickPulse] _active before = " + _active);
        
        _active = false;
        isGated = true;
        
        var linkedHoldNote = GetComponentInParent<LinkedHoldNote>();
        if (linkedHoldNote != null)
        {
            linkedHoldNote.StopHold();
            Debug.Log("[HoldTickPulse] StopTick - Notified LinkedHoldNote");
        }
        else
        {
            Debug.Log("[HoldTickPulse] StopTick - LinkedHoldNote NOT FOUND");
        }
        
        OnExit?.Invoke();
        Debug.Log("[HoldTickPulse] StopTick - OnExit invoked");
        Debug.Log("[HoldTickPulse] ========== StopTick END ==========");
    }

    public void OnHoldEnter()
    {
        StartTick();
    }

    public void OnHoldExit()
    {
        StopTick();
    }

    public void ForceStopFromGate()
    {
        _active = false;
        isGated = true;
        if (debugLog) Debug.Log("[HoldTickPulse] ForceStopFromGate");
    }

    private void Update()
    {
        if (useGating && !_active) return;
        if (Time.timeScale <= 0f) return;

        if (perFrame)
        {
            FireTick();
            return;
        }

        if (tickRateHz <= 0f) tickRateHz = 30f;
        _accum += Time.deltaTime;
        float interval = 1f / tickRateHz;

        while (_accum >= interval)
        {
            FireTick();
            _accum -= interval;
        }
    }

    private void FireTick()
    {
        OnTick?.Invoke();
        
        if (comboProbe != null)
        {
            comboProbe.OnHoldTick();
        }
        
        if (judgeTextBlinker != null)
        {
            judgeTextBlinker.OnHoldTick();
        }

        // VFX表示（インターバル制御）
        vfxTimer += Time.deltaTime;
        if (vfxTimer >= vfxInterval)
        {
            PlayHoldVFX();
            vfxTimer = 0f;
        }
    }

    private void PlayHoldVFX()
    {
        if (hitVFXPool == null || playerTransform == null) return;

        Vector3 vfxPosition = playerTransform.position + vfxOffset;
        hitVFXPool.PlayVFX(vfxPosition, "GOOD");
        
        if (debugLog)
        {
            Debug.Log($"[HoldTickPulse] VFX played at {vfxPosition}");
        }
    }

    public bool IsActive => _active;
}
