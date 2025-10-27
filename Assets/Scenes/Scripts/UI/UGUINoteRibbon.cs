using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class UGUINoteRibbon : MaskableGraphic
{
    public List<RectTransform> anchors = new List<RectTransform>(); // つなぐ点
    [Range(1, 100)] public float width = 20f;                       // 太さ
    public Color colorStart = Color.yellow;                         // はじの色
    public Color colorEnd = Color.white;                            // おわりの色
    [Range(0f, 1f)] public float expose01 = 1f;                     // どこまで出すか(0=非表示,1=全部)
    [Range(2, 16)] public int roundJoinSegments = 4;                // カーブのなめらかさ

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (anchors == null || anchors.Count < 2) return;

        int maxCount = Mathf.FloorToInt((anchors.Count - 1) * expose01) + 1;
        if (maxCount < 2) return;

        for (int i = 0; i < maxCount - 1; i++)
        {
            RectTransform a = anchors[i];
            RectTransform b = anchors[i + 1];
            if (a == null || b == null) continue;

            Vector2 p0 = WorldToLocal(a.position);
            Vector2 p1 = WorldToLocal(b.position);
            Vector2 dir = (p1 - p0).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * width * 0.5f;

            Color c = Color.Lerp(colorStart, colorEnd, (float)i / (anchors.Count - 1));

            // 四角形(帯の1区間)
            vh.AddVert(p0 - normal, c, Vector2.zero);
            vh.AddVert(p0 + normal, c, Vector2.zero);
            vh.AddVert(p1 + normal, c, Vector2.zero);
            vh.AddVert(p1 - normal, c, Vector2.zero);

            int idx = i * 4;
            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }
    }

    Vector2 WorldToLocal(Vector3 worldPos)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, RectTransformUtility.WorldToScreenPoint(null, worldPos), null, out localPos);
        return localPos;
    }

    // 外から 0〜1 で見せる量を変える関数
    public void SetExpose01(float value)
    {
        expose01 = Mathf.Clamp01(value);
        SetVerticesDirty();
    }
}
