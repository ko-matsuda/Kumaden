// BlobShadowFollower.cs
using UnityEngine;

public class BlobShadowFollower : MonoBehaviour
{
    public Transform player;          // クマ
    public LayerMask groundMask;      // Road/Grass など地面レイヤー
    public float heightOffset = 0.01f;
    public float sizeAtGround = 1.2f; // 接地時の直径
    public float shrinkPerMeter = 0.5f; // 高さ1mでどれだけ縮むか

    void LateUpdate()
    {
        Vector3 from = player.position + Vector3.up * 5f;
        if (Physics.Raycast(from, Vector3.down, out var hit, 20f, groundMask))
        {
            transform.position = hit.point + Vector3.up * heightOffset;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal); // 斜面対応
            float h = Mathf.Max(0f, player.position.y - hit.point.y);
            float s = Mathf.Max(0.1f, sizeAtGround - h * shrinkPerMeter);
            transform.localScale = new Vector3(s, 1f, s);
        }
        else
        {
            // 地面が無ければ非表示（スケール0）
            transform.localScale = Vector3.zero;
        }
    }
}
