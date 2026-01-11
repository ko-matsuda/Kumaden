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
            seSource.enabled = true; // 有効化
            seSource.playOnAwake  = false;
            seSource.loop         = false;
            seSource.spatialBlend = 0f;
            seSource.dopplerLevel = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // AudioSourceを強制的に有効化（Retry対応）
        if (seSource != null && !seSource.enabled)
        {
            seSource.enabled = true;
            Debug.Log("[Pickup] seSource force enabled");
        }
        
        // ホールド開始
        if (other.CompareTag("LinkedHoldStart"))
        {
            if (holdTickPulse != null)
            {
                holdTickPulse.StartTick();
                if (printDebug) Debug.Log("[Pickup] StartTick called");
            }
            string holdStartJudge = PlaySEAndGetJudge(other);
            PlayVFXAtPosition(transform.position, holdStartJudge);
            Destroy(other.gameObject);  // StartNote を破棄
            return;
        }

        // ホールド終了 - Colliderの中心がPlayerの中心を通過したら判定
        if (other.CompareTag("LinkedHoldEnd"))
        {
            if (playerBox == null) return;
            
            // PlayerのCollider中心位置を取得
            Vector3 playerCenter = transform.TransformPoint(playerBox.center);
            float playerCenterZ = playerCenter.z;
            
            // EndNoteの中心位置を取得
            float endNoteCenterZ = other.bounds.center.z;
            
            // EndNoteがまだPlayerより前にある場合は判定しない
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
            // リボンを非表示
            if (linkedHoldNote != null)
            {
                linkedHoldNote.HideRibbon();
                if (printDebug) Debug.Log("[Pickup] HideRibbon called");
            }
            string holdEndJudge = PlaySEAndGetJudge(other);
            PlayVFXAtPosition(other.transform.position, holdEndJudge);
            Destroy(other.gameObject);  // EndNote を破棄
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

        string normalJudge = (dz <= perfectRangeZ) ? "PERFECT"
                           : (dz <= goodRangeZ)    ? "GOOD"
                           :                         "MISS";

        if (printDebug)
        {
            Debug.Log($"[Pickup] lane={note.laneIndex}, dz={dz:F2} → {normalJudge}");
        }

        if (seSource != null)
        {
            if (normalJudge == "PERFECT" && sePerfect != null) seSource.PlayOneShot(sePerfect);
            else if (normalJudge == "GOOD" && seGood != null)  seSource.PlayOneShot(seGood);
        }

        ScoreManagerLite.Instance?.OnPick(note.Type, normalJudge);

        if (normalJudge == "PERFECT" || normalJudge == "GOOD")
        {
            // HudCounterBinder削除: ScoreManagerLiteが食材を管理
            if (comboProbe != null) comboProbe.OnNormalNote();
            LaneController.Instance?.HighlightLane(note.laneIndex, 0.2f);
            
            // VFXを再生
            PlayVFXAtPosition(transform.position, normalJudge);
        }
        else if (normalJudge == "MISS")
        {
            // MISS でコンボリセット
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
