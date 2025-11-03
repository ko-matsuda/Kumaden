using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[DisallowMultipleComponent]
public class WorldRibbon : MonoBehaviour
{
    [Header("必須")]
    public LineRenderer line;

    [Header("デバッグ")]
    public bool debugLog = true;

    void Reset()
    {
        line = GetComponent<LineRenderer>();
        if (line == null) line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
    }

    void Awake()
    {
        if (!line) line = GetComponent<LineRenderer>();
        if (debugLog) Debug.Log("[WorldRibbon] Awake");
    }

    public void Show()
    {
        if (!line) return;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (debugLog) Debug.Log("[WorldRibbon] Show");
    }

    public void Hide()
    {
        if (!line) return;
        if (gameObject.activeSelf) gameObject.SetActive(false);
        if (debugLog) Debug.Log("[WorldRibbon] Hide");
    }

    /// <summary>
    /// 2点 A→B の区間のうち、[a01,b01] (0..1) だけを描く
    /// a01>=b01 のときは非表示
    /// </summary>
    public void ShowSpan(Vector3 aWorld, Vector3 bWorld, float a01, float b01)
    {
        if (!line) return;

        // clamp
        if (a01 < 0f) a01 = 0f;
        if (b01 > 1f) b01 = 1f;

        if (a01 >= b01)
        {
            Hide();
            return;
        }

        Vector3 dir = (bWorld - aWorld);
        float len = dir.magnitude;
        if (len < 1e-4f)
        {
            Hide();
            return;
        }

        Vector3 p0 = aWorld + dir * a01;
        Vector3 p1 = aWorld + dir * b01;

        line.positionCount = 2;
        line.SetPosition(0, p0);
        line.SetPosition(1, p1);

        Show();
    }

    /// <summary>区間全部を描く（0..1）</summary>
    public void ShowFull(Vector3 aWorld, Vector3 bWorld)
    {
        if (!line) return;
        line.positionCount = 2;
        line.SetPosition(0, aWorld);
        line.SetPosition(1, bWorld);
        Show();
    }
}
