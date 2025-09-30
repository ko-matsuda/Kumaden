using UnityEngine;

public class ChunkLooper : MonoBehaviour
{
    public Transform[] chunks;        // 並び順は画面手前→奥
    public float chunkLength = 25f;   // PrefabのStart→End距離
    public Transform player;          // 使わないなら未設定でOK
    public Transform recycleLine;     // 推奨：PlayerRoot/RecycleLine
    public float recycleMargin = 1f;  // 少し余裕

    Transform FindMarker(Transform root, string name)
    {
        var t = root.Find(name);
        if (t) return t;
        foreach (Transform c in root) { var r = c.Find(name); if (r) return r; }
        return null;
    }

    void Update()
    {
        if (chunks == null || chunks.Length < 2) return;

        // リサイクル基準のZ
        float lineZ = 0f;
        if (recycleLine)      lineZ = recycleLine.position.z;
        else if (player)      lineZ = player.position.z;

        // 先頭チャンクの End.z
        var first = chunks[0];
        var end = FindMarker(first, "End");
        if (!end) return;

        float endZ = end.position.z;

        // 先頭が基準を超えたら後ろへ回す
        if (endZ < lineZ - recycleMargin)
        {
            // 末尾の End 位置
            var last = chunks[chunks.Length - 1];
            var lastEnd = FindMarker(last, "End");
            if (!lastEnd) return;

            float newStartZ = lastEnd.position.z; // 末尾の終端
            // 先頭のStartを newStartZ へ揃えて移動
            var start = FindMarker(first, "Start");
            if (!start) return;

            float dz = newStartZ - start.position.z;
            first.position += new Vector3(0, 0, dz);

            // 配列を回転：先頭を末尾へ
            for (int i = 0; i < chunks.Length - 1; i++)
                chunks[i] = chunks[i + 1];
            chunks[chunks.Length - 1] = first;
        }
    }

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (recycleLine)
        {
            Gizmos.color = Color.red;
            var p = recycleLine.position;
            Gizmos.DrawLine(p + Vector3.left * 100, p + Vector3.right * 100);
        }
    }
    #endif
}
