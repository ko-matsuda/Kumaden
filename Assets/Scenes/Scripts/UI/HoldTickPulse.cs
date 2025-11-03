using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 帯がアクティブの間だけ連続で Tick を発火させるドライバ。
/// ・OnHoldEnter()で開始、OnHoldExit()で停止
/// ・毎フレーム or 一定Hz で UnityEvent OnTick を呼ぶ
/// ここに ComboProbe.OnHoldTick / HudCounterBinder.OnHoldTick を配線するだけで連続カウントが成立。
/// </summary>
public sealed class HoldTickPulse : MonoBehaviour
{
    [Header("Tick Mode")]
    [SerializeField] private bool perFrame = true;   // 毎フレーム発火
    [SerializeField] private float tickRateHz = 30f; // perFrame=false のとき使用（1秒間に何回）

    [Header("Gating")]
    [SerializeField] private bool _active = false;   // 帯アクティブ中のみ発火

    [Header("Events")]
    public UnityEvent OnTick;                        // ここに OnHoldTick を複数ぶら下げる

    private float _accum;

    // 帯開始/終了フック（LinkedHoldNote/HoldEventRouter から配線）
    public void OnHoldEnter()
    {
        _active = true;
        _accum = 0f;
    }

    public void OnHoldExit()
    {
        _active = false;
    }

    // 終端ゲート等からの強制停止も受けられるように
    public void ForceStopFromGate()
    {
        _active = false;
    }

    private void Update()
    {
        if (!_active) return;
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
        if (OnTick != null) OnTick.Invoke();
    }
}
