using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WorldRibbon : MonoBehaviour
{
    LineRenderer line;
    MaterialPropertyBlock mpb;

    [Range(0f, 1f)]
    public float expose01 = 0f;
    static readonly int ExposeID = Shader.PropertyToID("_Expose01");

    void Awake()
    {
        if (!line) line = GetComponent<LineRenderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();
        line.useWorldSpace = true;
        ApplyExpose();
    }

    void OnValidate()
    {
        if (!line) line = GetComponent<LineRenderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();
        ApplyExpose();
    }

    // === 正式API ===
    public void SetExpose01(float value)
    {
        expose01 = Mathf.Clamp01(value);
        ApplyExpose();
    }

    public void SetEndpoints(Vector3 a, Vector3 b)
    {
        if (!line) line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, a);
        line.SetPosition(1, b);
    }

    public void Clear()
    {
        if (!line) line = GetComponent<LineRenderer>();
        line.positionCount = 0;
    }

    void ApplyExpose()
    {
        if (!line) return;
        line.GetPropertyBlock(mpb);
        mpb.SetFloat(ExposeID, expose01);
        line.SetPropertyBlock(mpb);
    }

    // === 互換API（既存コード対策） ===
    // 旧名: SetExpose(float)
    public void SetExpose(float value) => SetExpose01(value);
    // 旧名: TurnOn/TurnOff を呼ぶコードがある場合に備えて
    public void TurnOn()  => SetExpose01(1f);
    public void TurnOff() => SetExpose01(0f);

    [ContextMenu("TestDraw (force)")]
    void TestDraw()
    {
        var a = transform.position + new Vector3(-0.5f, 0f, 0f);
        var b = transform.position + new Vector3( 0.5f, 0f, 0f);
        SetEndpoints(a, b);
        SetExpose01(1f);
    }
}
