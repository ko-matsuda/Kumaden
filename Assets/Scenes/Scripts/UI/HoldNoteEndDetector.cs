using UnityEngine;

/// <summary>
/// StartNote にアタッチして、EndNote が判定ラインを通過したら HoldTickPulse を停止する
/// </summary>
public class HoldNoteEndDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform endNote;
    [SerializeField] private HoldTickPulse holdTickPulse;
    
    [Header("Settings")]
    [SerializeField] private float judgeZ = 0f;
    [SerializeField] private bool debugLog = true;
    
    private bool hasEnded = false;
    
    void Update()
    {
        if (hasEnded) return;
        if (endNote == null || holdTickPulse == null)
        {
            if (debugLog && Time.frameCount % 60 == 0)
            {
                Debug.LogWarning($"[HoldNoteEndDetector] Missing references - endNote: {endNote != null}, holdTickPulse: {holdTickPulse != null}");
            }
            return;
        }
        
        // デバッグ：1秒に1回 EndNote の位置を表示
        if (debugLog && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[HoldNoteEndDetector] Monitoring - EndNote Z: {endNote.position.z:F2}, Judge Z: {judgeZ:F2}");
        }
        
        // EndNote が判定ラインを通過したか
        if (endNote.position.z <= judgeZ)
        {
            hasEnded = true;
            holdTickPulse.OnHoldExit();
            
            if (debugLog)
            {
                Debug.Log($"[HoldNoteEndDetector] EndNote passed judge line (Z={endNote.position.z:F2}) - stopping HoldTickPulse");
            }
        }
    }
}