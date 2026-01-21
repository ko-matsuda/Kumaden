using UnityEngine;

public class Pickup : MonoBehaviour
{
    [Header("SE（Player の AudioSource を割り当て）")]
    public AudioSource seSource;
    public AudioClip sePerfect;
    public AudioClip seGood;

    
    [Header("Difficulty (Optional)")]
    [SerializeField] private bool useDifficultyManager = true;
    
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

    [Header("VFX")]
    public HitVFXPool hitVFXPool;
    public Vector3 vfxOffset = new Vector3(0f, 0.5f, 0f); // VFXの表示位置オフセット

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
            seSource.enabled = true;
            seSource.playOnAwake  = false;
            seSource.loop         = false;
            seSource.spatialBlend = 0f;
            seSource.dopplerLevel = 0f;
        }
        
        // 難易度設定を適用
        if (useDifficultyManager && DifficultyManager.Instance != null)
        {
            var settings = DifficultyManager.Instance.GetCurrentSettings();
            perfectRangeZ = settings.perfectWindow * 4.0f;
            goodRangeZ = settings.goodWindow * 4.0f;
            Debug.Log($"[Pickup] Difficulty applied - Perfect: {perfectRangeZ:F2}z, Good: {goodRangeZ:F2}z");
        }
    }

private void OnTriggerEnter(Collider other)
    {
        if (seSource != null && !seSource.enabled)
        {
            seSource.enabled = true;
            Debug.Log("[Pickup] seSource force enabled");
        }
        
        if (other.CompareTag("LinkedHoldStart"))
        {
            if (holdTickPulse != null)
            {
                holdTickPulse.StartTick();
                if (printDebug) Debug.Log("[Pickup] StartTick called");
            }
            string holdStartJudge = PlaySEAndGetJudge(other);
            PlayVFXAtPosition(transform.position, holdStartJudge);
            Destroy(other.gameObject);
            return;
        }

        if (other.CompareTag("LinkedHoldEnd"))
        {
            if (playerBox == null) return;
            
            Vector3 playerCenter = transform.TransformPoint(playerBox.center);
            float playerCenterZ = playerCenter.z;
            float endNoteCenterZ = other.bounds.center.z;
            
            if (endNoteCenterZ > playerCenterZ)
            {
                if (printDebug) Debug.Log($"[Pickup] EndNote too early: endZ={endNoteCenterZ:F2}, playerZ={playerCenterZ:F2}");
                return;
            }
            
            if (holdTickPulse != null)
            {
                holdTickPulse.StopTick();
                if (printDebug) Debug.Log("[Pickup] StopTick called");
            }
            if (linkedHoldNote != null)
            {
                linkedHoldNote.HideRibbon();
                if (printDebug) Debug.Log("[Pickup] HideRibbon called");
            }
            string holdEndJudge = PlaySEAndGetJudge(other);
            PlayVFXAtPosition(other.transform.position, holdEndJudge);
            Destroy(other.gameObject);
            return;
        }

        var note = other.GetComponent<NoteBehaviour>();
        if (note == null || playerBox == null) return;

        if (!note.TryMarkJudged()) return;

        Vector3 localFront = playerBox.center + new Vector3(0f, 0f, playerBox.size.z * 0.5f);
        float playerFrontZ = transform.TransformPoint(localFront).z + judgeOffsetFromFront;
        float noteCenterZ = other.bounds.center.z;
        float dz = Mathf.Abs(noteCenterZ - playerFrontZ);

        string normalJudge = (dz <= perfectRangeZ) ? "PERFECT"
                           : (dz <= goodRangeZ)    ? "GOOD"
                           :                         "MISS";

        Debug.Log($"[Pickup] lane={note.laneIndex}, dz={dz:F2}, perfectRange={perfectRangeZ:F2}, goodRange={goodRangeZ:F2} → {normalJudge}");

        if (seSource != null)
        {
            if (normalJudge == "PERFECT" && sePerfect != null) seSource.PlayOneShot(sePerfect);
            else if (normalJudge == "GOOD" && seGood != null)  seSource.PlayOneShot(seGood);
        }

        ScoreManagerLite.Instance?.OnPick(note.Type, normalJudge);

        if (normalJudge == "PERFECT" || normalJudge == "GOOD")
        {
            if (comboProbe != null) comboProbe.OnNormalNote();
            LaneController.Instance?.HighlightLane(note.laneIndex, 0.2f);
            PlayVFXAtPosition(transform.position, normalJudge);
        }
        else if (normalJudge == "MISS")
        {
            if (comboProbe != null) comboProbe.ResetCombo();
            if (printDebug) Debug.Log("[Pickup] MISS - Combo reset");
        }

        Destroy(note.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("[Pickup] OnTriggerExit called - Tag=" + other.tag);
        
        // ホールド中に外れた場合
        if (other.CompareTag("LinkedHoldStart") || other.CompareTag("LinkedHoldEnd"))
        {
            Debug.Log("[Pickup] OnTriggerExit - Hold tag detected!");
            
            if (holdTickPulse != null)
            {
                Debug.LogError("[Pickup] holdTickPulse found, IsActive=" + holdTickPulse.IsActive);
                
                if (holdTickPulse.IsActive)
                {
                    Debug.Log("[Pickup] OnTriggerExit - Hold interrupted! Calling StopTick");
                    holdTickPulse.StopTick();
                }
            }
            else
            {
                Debug.LogWarning("[Pickup] holdTickPulse is NULL - skipping hold end processing");
                return;
            }
        }
    }

    private string PlaySEAndGetJudge(Collider other)
    {
        if (playerBox == null || seSource == null) return "MISS";

        Vector3 localFront = playerBox.center + new Vector3(0f, 0f, playerBox.size.z * 0.5f);
        float playerFrontZ = transform.TransformPoint(localFront).z + judgeOffsetFromFront;
        float noteCenterZ = other.bounds.center.z;
        float dz = Mathf.Abs(noteCenterZ - playerFrontZ);

        string holdJudge = (dz <= perfectRangeZ) ? "PERFECT"
                         : (dz <= goodRangeZ)    ? "GOOD"
                         :                         "MISS";

        if (printDebug)
        {
            Debug.Log($"[Pickup] HoldNote SE: dz={dz:F2} → {holdJudge}");
        }

        if (holdJudge == "PERFECT" && sePerfect != null) seSource.PlayOneShot(sePerfect);
        else if (holdJudge == "GOOD" && seGood != null)  seSource.PlayOneShot(seGood);
        
        return holdJudge;
    }

    /// <summary>
    /// 指定位置でVFXを再生
    /// </summary>
    private void PlayVFXAtPosition(Vector3 position, string judgement)
    {
        if (hitVFXPool != null)
        {
            hitVFXPool.PlayVFX(position + vfxOffset, judgement);
        }
    }
}
