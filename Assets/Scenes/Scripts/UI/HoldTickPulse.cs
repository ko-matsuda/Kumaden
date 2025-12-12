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
    [SerializeField] private bool perFrame = true;   // 毎フレーム発火
    [SerializeField] private float tickRateHz = 30f; // perFrame=false のとき使用（1秒間に何回）

    [Header("Gating")]
    [SerializeField] private bool useGating = true;  // Gating 機能を使うかどうか
    [SerializeField] private bool isGated = true;    // Inspector で状態確認用（読み取り専用的に使用）

    [Header("Events")]
    public UnityEvent OnTick;                        // 連続カウント用
    public UnityEvent OnEnter;                       // ホールド開始時のイベント
    public UnityEvent OnExit;                        // ホールド終了時のイベント

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool _active = false;                    // 内部状態（常に false で初期化）
    private float _accum;

    // ───────────────────────────────────────
    // Pickup.cs から呼ばれるメソッド
    // ───────────────────────────────────────
    
    /// <summary>
    /// Tick 発火を開始する（Pickup.cs から呼ばれる）
    /// </summary>
    public void StartTick()
    {
        _active = true;
        _accum = 0f;
        isGated = false;  // Inspector 表示用
        OnEnter?.Invoke();
        if (debugLog) Debug.Log("[HoldTickPulse] StartTick - Started (_active = true)");
    }

    /// <summary>
    /// Tick 発火を停止する（Pickup.cs から呼ばれる）
    /// </summary>
    public void StopTick()
    {
        _active = false;
        isGated = true;  // Inspector 表示用
        OnExit?.Invoke();
        if (debugLog) Debug.Log("[HoldTickPulse] StopTick - Stopped (_active = false)");
    }

    // ───────────────────────────────────────
    // Inspector イベント用エイリアス（互換性のため残す）
    // ───────────────────────────────────────
    
    public void OnHoldEnter()
    {
        StartTick();
    }

    public void OnHoldExit()
    {
        StopTick();
    }

    // ───────────────────────────────────────
    // 強制停止（ゲートからの呼び出し用）
    // ───────────────────────────────────────
    
    public void ForceStopFromGate()
    {
        _active = false;
        isGated = true;
        if (debugLog) Debug.Log("[HoldTickPulse] ForceStopFromGate");
    }

    // ───────────────────────────────────────
    // Update ループ
    // ───────────────────────────────────────
    
    private void Update()
    {
        // Gating が有効な場合は _active をチェック
        if (useGating && !_active) return;
        if (Time.timeScale <= 0f) return;

        if (perFrame)
        {
            FireTick();
            return;
        }

        // 固定レート発火
        if (tickRateHz <= 0f) tickRateHz = 30f;
        _accum += Time.deltaTime;
        float interval = 1f / tickRateHz;

        // フレーム落ち吸収：溜まった分だけ複数回呼ぶ
        while (_accum >= interval)
        {
            FireTick();
            _accum -= interval;
        }
    }

    private void FireTick()
    {
        OnTick?.Invoke();
        // Tick ログは大量に出るのでコメントアウト
        // if (debugLog) Debug.Log("[HoldTickPulse] TICK");
    }

    // ───────────────────────────────────────
    // 状態確認用
    // ───────────────────────────────────────
    
    public bool IsActive => _active;
}