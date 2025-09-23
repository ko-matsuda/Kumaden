using System.Collections;
using UnityEngine;

/// <summary>
/// レーンの発光演出をまとめて管理
/// </summary>
public class LaneController : MonoBehaviour
{
    public static LaneController Instance;

    [Header("レーンの Renderer を左から順に登録")]
    public Renderer[] laneRenderers;

    [Header("発光用マテリアル")]
    public Material laneHighlightMat;

    private Material[] _orig;

    private void Awake()
    {
        Instance = this;
        if (laneRenderers == null) laneRenderers = new Renderer[0];
        _orig = new Material[laneRenderers.Length];
        for (int i = 0; i < laneRenderers.Length; i++)
            if (laneRenderers[i] != null) _orig[i] = laneRenderers[i].material;
    }

    public void HighlightLane(int laneIndex, float sec)
    {
        if (laneRenderers == null || laneIndex < 0 || laneIndex >= laneRenderers.Length) return;
        if (laneRenderers[laneIndex] == null || laneHighlightMat == null) return;
        StartCoroutine(CoHighlight(laneIndex, sec));
    }

    private IEnumerator CoHighlight(int idx, float sec)
    {
        var r = laneRenderers[idx];
        var prev = r.material;
        r.material = laneHighlightMat;
        yield return new WaitForSeconds(sec);
        r.material = prev != null ? prev : laneHighlightMat;
    }
}
