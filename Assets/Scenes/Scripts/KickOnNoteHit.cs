using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KickOnNoteHit : MonoBehaviour
{
    [Header("揺れ")]
    public float power = 0.06f;
    public float duration = 0.10f;

    [Header("プレイヤータグ")]
    public string playerTag = "Player";

    [Header("VFX オフセット (少し上に出したい時)")]
    public Vector3 vfxOffset = new Vector3(0f, 0.5f, 0f);

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // Trigger 推奨
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // 1) カメラ揺らす
        GlobalCameraShake.Kick(power, duration);

        // 2) VFX 再生（Z が地形に隠れないよう少し手前に）
        if (HitVFXPool.Instance != null)
        {
            Vector3 pos = other.bounds.center + vfxOffset;

            // 判定ラインの少し手前に固定したい場合（必要なら有効化）
            pos.z = other.bounds.center.z - 0.2f;

            HitVFXPool.Instance.PlayAt(pos, Quaternion.identity, null);
        }

        // （ノーツ自身の破棄等はゲーム仕様に合わせて）
        // Destroy(gameObject);
    }
}
