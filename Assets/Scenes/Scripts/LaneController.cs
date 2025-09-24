using System.Collections;
using UnityEngine;

public class LaneController : MonoBehaviour
{
    public static LaneController Instance;

    [Header("LaneL, LaneM, LaneR の MeshRenderer を登録（配列サイズ3）")]
    public MeshRenderer[] laneRenderers;

    void Awake()
    {
        Instance = this;
        // 起動時に全レーンを非表示
        foreach (var r in laneRenderers)
        {
            if (r == null) continue;
            r.enabled = false;
        }
    }

    // Pickup から呼ばれる
    public void HighlightLane(int laneIndex, float sec)
    {
        if (laneRenderers == null || laneIndex < 0 || laneIndex >= laneRenderers.Length) return;
        var r = laneRenderers[laneIndex];
        if (r == null) return;
        StartCoroutine(CoShow(r, sec));
    }

    private IEnumerator CoShow(MeshRenderer r, float sec)
    {
        r.enabled = true;                  // ★ 接触時にON
        yield return new WaitForSeconds(sec);
        r.enabled = false;                 // ★ 時間が経ったらOFF
    }
}
