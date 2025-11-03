using UnityEngine;
using UnityEngine.Events;

public class HoldTickRepeater : MonoBehaviour
{
    [Tooltip("Tick間隔(秒)")]
    public float tickInterval = 0.08f;

    [Tooltip("各Tickで呼ぶイベント(ComboProbe.OnHoldTick等)")]
    public UnityEvent onTick;

    private bool running = false;
    private float t = 0f;

    // 帯に入ったとき呼ぶ
    public void StartHold()
    {
        running = true;
        t = 0f;
    }

    // 帯を出たとき呼ぶ
    public void StopHold()
    {
        running = false;
    }

    void Update()
    {
        if (!running) return;
        t += Time.deltaTime;
        if (t >= tickInterval)
        {
            t -= tickInterval;
            if (onTick != null) onTick.Invoke();
        }
    }
}
