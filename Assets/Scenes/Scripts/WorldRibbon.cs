using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WorldRibbon : MonoBehaviour
{
    public LineRenderer line;
    public float expose01 = 0f;
    private static readonly int ExposeID = Shader.PropertyToID("_Expose01");
    private MaterialPropertyBlock mpb;

    void Awake()
    {
        if (line == null) line = GetComponent<LineRenderer>();
        mpb = new MaterialPropertyBlock();
        line.useWorldSpace = true;
    }

    // 旧API互換
    public void SetExpose01(float value) => SetExpose(value);
    public void SetEndpoints(Vector3 a, Vector3 b) { line.SetPosition(0, a); line.SetPosition(1, b); }
    public void Clear() { line.positionCount = 2; line.SetPosition(0, Vector3.zero); line.SetPosition(1, Vector3.zero); }

    // 現行制御
    public void SetExpose(float value)
    {
        expose01 = Mathf.Clamp01(value);
        ApplyExpose();
    }

    public void TurnOn()
    {
        gameObject.SetActive(true);
        SetExpose(1f);
    }

    public void TurnOff()
    {
        SetExpose(0f);
        gameObject.SetActive(false);
    }

    private void ApplyExpose()
    {
        if (line == null) return;
        line.GetPropertyBlock(mpb);
        mpb.SetFloat(ExposeID, expose01);
        line.SetPropertyBlock(mpb);
    }
}
