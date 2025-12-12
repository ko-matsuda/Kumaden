using UnityEngine;

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

    [Header("Hold Note")]
    public HoldTickPulse holdTickPulse;
    public LinkedHoldNote linkedHoldNote;

    [Header("カウンター（通常ノーツ用）")]
    public HudCounterBinder hudCounterBinder;
    public ComboProbe comboProbe;

    [Header("デバッグ")]
    public bool printDebug = true;

    private BoxCollider playerBox;

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
        // ホールド開始
        if (other.CompareTag("LinkedHoldStart"))
        {
            if (holdTickPulse != null)
            {
                holdTickPulse.StartTick();
                if (printDebug) Debug.Log("[Pickup] StartTick called");
            }
            PlaySEOnly(other);
            return;
        }

        // ホールド終了
        if (other.CompareTag("LinkedHoldEnd"))
        {
            if (holdTickPulse != null)
            {
                holdTickPulse.StopTick();
                if (printDebug) Debug.Log("[Pickup] StopTick called");
            }
            // リボンを非表示
            if (linkedHoldNote != null)
            {
                linkedHoldNote.HideRibbon();
                if (printDebug) Debug.Log("[Pickup] HideRibbon called");
            }
            PlaySEOnly(other);
            return;
        }

        // 通常ノーツ
        var note = other.GetComponent<NoteBehaviour>();
        if (note == null || playerBox == null) return;

        if (!note.TryMarkJudged()) return;

        Vector3 localFront = playerBox.center + new Vector3(0f, 0f, playerBox.size.z * 0.5f);
        float playerFrontZ = transform.TransformPoint(localFront).z + judgeOffsetFromFront;
        float noteCenterZ = other.bounds.center.z;
        float dz = Mathf.Abs(noteCenterZ - playerFrontZ);

        string judge = (dz <= perfectRangeZ) ? "PERFECT"
                     : (dz <= goodRangeZ)    ? "GOOD"
                     :                         "MISS";

        if (printDebug)
        {
            Debug.Log($"[Pickup] lane={note.laneIndex}, dz={dz:F2} → {judge}");
        }

        if (seSource != null)
        {
            if (judge == "PERFECT" && sePerfect != null) seSource.PlayOneShot(sePerfect);
            else if (judge == "GOOD" && seGood != null)  seSource.PlayOneShot(seGood);
        }

        ScoreManagerLite.Instance?.OnPick(note.Type, judge);

        if (judge == "PERFECT" || judge == "GOOD")
        {
            if (hudCounterBinder != null) hudCounterBinder.OnNormalNote();
            if (comboProbe != null) comboProbe.OnNormalNote();
            LaneController.Instance?.HighlightLane(note.laneIndex, 0.2f);
        }
        else if (judge == "MISS")
        {
            // MISS でコンボリセット
            if (comboProbe != null) comboProbe.ResetCombo();
            if (printDebug) Debug.Log("[Pickup] MISS - Combo reset");
        }

        Destroy(note.gameObject);
    }

    private void PlaySEOnly(Collider other)
    {
        if (playerBox == null || seSource == null) return;

        Vector3 localFront = playerBox.center + new Vector3(0f, 0f, playerBox.size.z * 0.5f);
        float playerFrontZ = transform.TransformPoint(localFront).z + judgeOffsetFromFront;
        float noteCenterZ = other.bounds.center.z;
        float dz = Mathf.Abs(noteCenterZ - playerFrontZ);

        string judge = (dz <= perfectRangeZ) ? "PERFECT"
                     : (dz <= goodRangeZ)    ? "GOOD"
                     :                         "MISS";

        if (printDebug)
        {
            Debug.Log($"[Pickup] HoldNote SE: dz={dz:F2} → {judge}");
        }

        if (judge == "PERFECT" && sePerfect != null) seSource.PlayOneShot(sePerfect);
        else if (judge == "GOOD" && seGood != null)  seSource.PlayOneShot(seGood);
    }
}