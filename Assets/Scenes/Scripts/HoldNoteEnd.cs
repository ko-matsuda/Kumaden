using UnityEngine;

public class HoldNoteEnd : MonoBehaviour
{
    [Header("デバッグ")]
    public bool debugLog = true;

    private LinkedHoldNote parentHoldNote;
    private bool hasTriggered = false;

    void Awake()
    {
        parentHoldNote = GetComponentInParent<LinkedHoldNote>();
        if (parentHoldNote == null && debugLog)
        {
            Debug.LogWarning("[HoldNoteEnd] LinkedHoldNote parent not found!");
        }
    }

void OnTriggerEnter(Collider other) { if (hasTriggered) return; if (debugLog) { Debug.Log($"[HoldNoteEnd] OnTriggerEnter - other={other.name}, tag={other.tag}"); } if (other.CompareTag("Player")) { hasTriggered = true; bool wasHolding = parentHoldNote != null && parentHoldNote.IsHolding(); if (debugLog) { Debug.Log($"[HoldNoteEnd] Player hit - wasHolding={wasHolding}"); } if (!wasHolding) { if (debugLog) Debug.Log("[HoldNoteEnd] MISS - not holding"); var comboProbe = FindObjectOfType<ComboProbe>(); if (comboProbe != null) { comboProbe.OnNoteMiss(); if (debugLog) Debug.Log("[HoldNoteEnd] Called ComboProbe.OnNoteMiss()"); } var scoreMgr = ScoreManagerLite.Instance; if (scoreMgr != null) { scoreMgr.OnPick(IngredientType.Milk, "MISS"); } } else { if (debugLog) Debug.Log("[HoldNoteEnd] SUCCESS - hold complete"); } if (parentHoldNote != null) { parentHoldNote.HideRibbon(); } Destroy(gameObject); } }

    void OnEnable()
    {
        hasTriggered = false;
    }
}
