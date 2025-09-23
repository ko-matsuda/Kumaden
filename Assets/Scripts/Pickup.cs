using UnityEngine;

/// <summary>
/// Player 当たり判定：ノーツ接触時に即SE→スコア通知。
/// 「ライン到達前はPERFECTのみ判定」して、早期GOODを防ぐ。
/// </summary>
public class Pickup : MonoBehaviour
{
    [Header("SE（Player の AudioSource を割り当て）")]
    public AudioSource seSource;
    public AudioClip sePerfect;
    public AudioClip seGood;

    [Header("判定幅（Z距離）")]
    [Tooltip("PERFECT の許容距離（judgeZ からの絶対距離）")]
    public float perfectRangeZ = 0.35f;
    [Tooltip("GOOD の許容距離（judgeZ からの絶対距離）")]
    public float goodRangeZ    = 0.80f;

    private void Awake()
    {
        if (seSource != null)
        {
            seSource.playOnAwake  = false;
            seSource.loop         = false;
            seSource.spatialBlend = 0f; // 2D
            seSource.dopplerLevel = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var note = other.GetComponent<NoteBehaviour>();
        if (note == null) return;
        TryJudge(note);
    }

    private void OnTriggerStay(Collider other)
    {
        var note = other.GetComponent<NoteBehaviour>();
        if (note == null) return;
        TryJudge(note);
    }

    private void TryJudge(NoteBehaviour note)
    {
        // すでに他で判定済みなら無視
        if (!note.TryMarkJudged())
            return;

        float z = note.transform.position.z;
        float dz = Mathf.Abs(z - note.judgeZ);

        // —— 早期GOOD防止ルール ——
        // 1) ラインより手前（z > judgeZ）のときは PERFECT 圏内のみ即判定。
        //    PERFECT 圏外なら今回は保留（判定済みフラグを戻して次フレームで再挑戦）。
        if (z > note.judgeZ)
        {
            if (dz > perfectRangeZ)
            {
                note.UnmarkJudgedForRetry(); // 早いのでまだ待つ
                return;
            }
            // PERFECT 圏内なら先取りでOK（体感向上）
            FinishJudgement(note, "PERFECT");
            return;
        }

        // 2) ライン到達以降（z <= judgeZ）は通常どおり距離で判定
        string judge;
        if (dz <= perfectRangeZ)      judge = "PERFECT";
        else if (dz <= goodRangeZ)    judge = "GOOD";
        else                          judge = "MISS";

        FinishJudgement(note, judge);
    }

    private void FinishJudgement(NoteBehaviour note, string judge)
    {
        // SE 先行
        if (seSource != null)
        {
            if (judge == "PERFECT" && sePerfect != null) seSource.PlayOneShot(sePerfect);
            else if (judge == "GOOD" && seGood != null)  seSource.PlayOneShot(seGood);
        }

        // スコア通知
        var sm = ScoreManagerLite.Instance;
        if (sm != null) sm.OnPick(note.Type, judge);

        // 簡易演出（任意）
        if (judge == "PERFECT" || judge == "GOOD")
        {
            note.HighlightNote(0.2f);
            if (LaneController.Instance != null)
                LaneController.Instance.HighlightLane(note.laneIndex, 0.2f);
        }

        Destroy(note.gameObject);
    }
}
