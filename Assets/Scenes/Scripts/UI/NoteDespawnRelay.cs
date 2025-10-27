using UnityEngine;
using System.Reflection;

[DisallowMultipleComponent]
public class NoteDespawnRelay : MonoBehaviour
{
    // ← これだけ選べばOK。ドラッグ不要（Prefabがシーン参照を持てない問題を回避）
    public enum RibbonLane { Left = 0, Middle = 1, Right = 2 }
    [Header("どのレーンの帯を使うか（ドラッグ不要）")]
    public RibbonLane lane = RibbonLane.Left;

    [Header("自動デスポーン（判定ラインを過ぎたら消す保険）")]
    public float autoDespawnPastJudgeZ = 3f;
    public float judgeZ = 0f;
    public bool movesToMinusZ = true;

    Transform _tf;

    // ランタイムで自動解決（KumaWorldRibbon / 自作 WorldRibbon / DLL 版 どれでも対応）
    GameObject  _ribbonObj;
    Component   _ribbonComp;
    PropertyInfo _propExpose01;
    MethodInfo   _methStopAndClear;

    void Awake()
    {
        _tf = transform;
        ResolveRibbonRuntime();
    }

    void OnEnable()
    {
        if (_ribbonComp == null) ResolveRibbonRuntime();
    }

    void Update()
    {
        float dz = _tf.position.z - judgeZ;
        bool passed = movesToMinusZ ? (dz < -autoDespawnPastJudgeZ) : (dz > autoDespawnPastJudgeZ);
        if (passed) ReleaseAndDespawn();
    }

    void OnDisable()
    {
        TrySetExpose01(0f);
        TryStopAndClear();
    }

    // —— ホールド終了などから呼ぶ —— 
    public void ReleaseAndDespawn()
    {
        TrySetExpose01(0f);
        TryStopAndClear();
        gameObject.SetActive(false); // プール想定
    }

    //================ ここから内部処理 ================

    void ResolveRibbonRuntime()
    {
        // 1) レーン名から WorldRibbon_L/M/R を探す
        string targetName =
            lane == RibbonLane.Left  ? "WorldRibbon_L" :
            lane == RibbonLane.Middle? "WorldRibbon_M" :
                                       "WorldRibbon_R";

        _ribbonObj = GameObject.Find(targetName);

        // 2) 念のため、名前が少し違っても拾えるよう保険
        if (_ribbonObj == null)
        {
            foreach (var go in FindObjectsOfType<GameObject>())
            {
                if (go.name.Contains("WorldRibbon_"))
                {
                    _ribbonObj = go;
                    break;
                }
            }
        }

        // 3) コンポーネント特定（自作/ DLL どちらでも）
        if (_ribbonObj != null)
        {
            _ribbonComp = _ribbonObj.GetComponent("KumaWorldRibbon"); // 推奨名
            if (_ribbonComp == null)
            {
                var comp = _ribbonObj.GetComponent("WorldRibbon");
                if (comp != null && comp.GetType().GetProperty("expose01") != null) _ribbonComp = comp;
            }
            if (_ribbonComp == null)
            {
                foreach (var c in _ribbonObj.GetComponents<Component>())
                {
                    if (c == null) continue;
                    if (c.GetType().GetProperty("expose01") != null) { _ribbonComp = c; break; }
                }
            }

            if (_ribbonComp != null)
            {
                var t = _ribbonComp.GetType();
                _propExpose01     = t.GetProperty("expose01", BindingFlags.Public | BindingFlags.Instance);
                _methStopAndClear = t.GetMethod("StopAndClear", BindingFlags.Public | BindingFlags.Instance);
            }
        }
    }

    void TrySetExpose01(float v)
    {
        if (_propExpose01 != null && _ribbonComp != null) _propExpose01.SetValue(_ribbonComp, v, null);
    }

    void TryStopAndClear()
    {
        if (_methStopAndClear != null && _ribbonComp != null) _methStopAndClear.Invoke(_ribbonComp, null);
    }
}
