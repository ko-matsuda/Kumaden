using UnityEngine;

// シンプル & 互換重視。既存呼び出し(SetExpose/SetExpose01/SetEndpoints/Clear)を吸収。
[RequireComponent(typeof(LineRenderer))]
public class WorldRibbon : MonoBehaviour
{
    public LineRenderer line;
    public float expose01 = 0f; // 0=非表示, 1=表示（必要ならマテリアル側へ反映）

    void Awake()
    {
        if (line == null) line = GetComponent<LineRenderer>();
    }

    // === 表示ON/OFF系 ===
    public void SetExpose(float value) => SetExpose01(value);
    public void SetExpose01(float value)
    {
        expose01 = value;
        // 見た目反映を入れたい場合はここで mpb などに書き込み。
        // 今回はビルド互換優先で値保持のみにしています。
    }

    // ※ 以前の SetExpose(UnityEngine.Object) は削除
    //   （UnityEngine.Object を float として扱うパターンマッチが C# 設定によっては通らないため）

    // === 端点設定系 ===
    public void SetEndpoints(Vector3 a, Vector3 b)
    {
        if (!line) return;
        line.positionCount = 2;
        line.SetPosition(0, a);
        line.SetPosition(1, b);
    }

    public void SetEndpoints(Transform a, Transform b)
    {
        if (!a || !b) { Clear(); return; }
        SetEndpoints(a.position, b.position);
    }

    public void Clear()
    {
        if (!line) return;
        line.positionCount = 0;
    }
}
