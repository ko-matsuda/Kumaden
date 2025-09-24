using UnityEngine;

/// ノーツ接触 → 判定(PERFECT/GOOD/MISS) → SE → スコア更新 → レーン発光 → ノーツ破棄
/// 判定は「プレイヤー BoxCollider の“前面”のワールドZ」と「ノーツ中心Z」の距離で行う。
public class Pickup : MonoBehaviour
{
    [Header("SE（Player の AudioSource を割り当て）")]
    public AudioSource seSource;
    public AudioClip sePerfect;
    public AudioClip seGood;

    [Header("判定幅（Z距離）")]
    public float perfectRangeZ = 0.30f;
    public float goodRangeZ    = 0.80f;

    [Header("微調整（前面からのオフセット +前 / -後）")]
    public float judgeOffsetFromFront = 0f;

    [Header("デバッグ")]
    public bool printDebug = true;

    private BoxCollider playerBox;   // 前面を正確に求めるため BoxCollider を使用

    void Awake()
    {
        playerBox = GetComponent<BoxCollider>();
        if (playerBox == null)
        {
            Debug.LogError("[Pickup] Player に BoxCollider が必要です。");
        }
        if (seSource != null)
        {
            seSource.playOnAwake  = false;
            seSource.loop         = false;
            seSource.spatialBlend = 0f;
            seSource.dopplerLevel = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var note = other.GetComponent<NoteBehaviour>();
        if (note == null || playerBox == null) return;

        // 二重判定防止
        if (!note.TryMarkJudged()) return;

        // ★ プレイヤー前面のワールドZを求める（center + size/2 をローカル→ワールド変換）
        Vector3 localFront = playerBox.center + new Vector3(0f, 0f, playerBox.size.z * 0.5f);
        float playerFrontZ = transform.TransformPoint(localFront).z + judgeOffsetFromFront;

        // ノーツ中心のワールドZ
        float noteCenterZ = other.bounds.center.z;

        // 前後距離で判定
        float dz = Mathf.Abs(noteCenterZ - playerFrontZ);
        string judge = (dz <= perfectRangeZ) ? "PERFECT"
                     : (dz <= goodRangeZ)    ? "GOOD"
                     :                         "MISS";

        if (printDebug)
        {
            Debug.Log($"[Pickup] lane={note.laneIndex}, PlayerFrontZ={playerFrontZ:F2}, NoteZ={noteCenterZ:F2}, dz={dz:F2} → {judge}  (center.z={playerBox.center.z:F2}, size.z={playerBox.size.z:F2})");
        }

        // SE（MISSは無音）
        if (seSource != null)
        {
            if (judge == "PERFECT" && sePerfect != null) seSource.PlayOneShot(sePerfect);
            else if (judge == "GOOD" && seGood != null)  seSource.PlayOneShot(seGood);
        }

        // スコア
        ScoreManagerLite.Instance?.OnPick(note.Type, judge);

        // レーン発光（成功時のみ）
        if ((judge == "PERFECT" || judge == "GOOD"))
            LaneController.Instance?.HighlightLane(note.laneIndex, 0.2f);

        // ノーツ破棄
        Destroy(note.gameObject);
    }
}
