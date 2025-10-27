// 一時テスター：線が2点で出るかだけ確認
using UnityEngine;

public class RibbonLineTester : MonoBehaviour
{
    public LineRenderer lr;
    public float x = -1.6f;     // 左レーン中心
    public float z0 = -1f, z1 = 1f;
    public float y = 0.05f;

    void Reset() { lr = GetComponent<LineRenderer>(); }

    void OnEnable()
    {
        if (!lr) return;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.SetPosition(0, new Vector3(x, y, z0));
        lr.SetPosition(1, new Vector3(x, y, z1));
        if (lr.widthMultiplier <= 0f) lr.widthMultiplier = 0.2f;
    }
}
