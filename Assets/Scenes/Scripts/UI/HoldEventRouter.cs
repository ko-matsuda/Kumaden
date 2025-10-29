using UnityEngine;

public class HoldEventRouter : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public WorldRibbon ribbonL;
    public WorldRibbon ribbonM;
    public WorldRibbon ribbonR;

    [Header("Tune")]
    public float startZOffset = -0.1f;
    public float minLength   = 0.2f;

    WorldRibbon PickRibbonByLaneIndex(int laneIndex)
    {
        switch (laneIndex)
        {
            case 0: return ribbonL;
            case 1: return ribbonM;
            case 2: return ribbonR;
            default: return ribbonM; // フォールバック
        }
    }

    // ==== Events (NoteBehaviour を Dynamic 引数で受け取る) ====
    public void BeginHold(NoteBehaviour note)
    {
        if (note == null || player == null) return;
        var r = PickRibbonByLaneIndex(note.laneIndex);
        if (r == null) return;

        Vector3 a = player.position + new Vector3(0f, 0f, startZOffset);
        Vector3 b = note.transform.position;
        if (Vector3.Distance(a, b) < minLength) b = a + Vector3.forward * minLength;

        r.SetEndpoints(a, b);
        r.SetExpose(1f); // ← 互換ラッパでOK
    }

    public void HoldTick(NoteBehaviour note)
    {
        if (note == null || player == null) return;
        var r = PickRibbonByLaneIndex(note.laneIndex);
        if (r == null) return;

        Vector3 a = player.position + new Vector3(0f, 0f, startZOffset);
        Vector3 b = note.transform.position;
        if (Vector3.Distance(a, b) < minLength) b = a + Vector3.forward * minLength;

        r.SetEndpoints(a, b);
    }

    public void EndHold(NoteBehaviour note)
    {
        if (note == null) return;
        var r = PickRibbonByLaneIndex(note.laneIndex);
        if (r == null) return;

        r.SetExpose(0f);
        r.Clear();
    }
}
