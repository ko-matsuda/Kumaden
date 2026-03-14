
using UnityEngine;
using UnityEditor;

public class DifficultyAudit
{
    [MenuItem("KumaDen/Dump Difficulty Settings")]
    static void Dump()
    {
        var go = GameObject.Find("DifficultyManager");
        if (go == null) { Debug.LogError("[Diff] DifficultyManager not found"); return; }
        var dm = go.GetComponent<DifficultyManager>();
        if (dm == null) { Debug.LogError("[Diff] component not found"); return; }
        var so = new SerializedObject(dm);
        string[] diffs = { "easySettings", "normalSettings", "hardSettings" };
        foreach (var d in diffs)
        {
            var prop = so.FindProperty(d);
            if (prop == null) { Debug.Log($"[Diff] {d}: not found"); continue; }
            var speed   = prop.FindPropertyRelative("speedMultiplier");
            var density = prop.FindPropertyRelative("noteDensity");
            var perfect = prop.FindPropertyRelative("perfectWindow");
            var good    = prop.FindPropertyRelative("goodWindow");
            Debug.Log($"[Diff] {d}: speed={speed?.floatValue} density={density?.floatValue} perfect={perfect?.floatValue} good={good?.floatValue}");
        }
    }

    [MenuItem("KumaDen/Set Difficulty Values")]
static void SetValues()
    {
        var go = GameObject.Find("DifficultyManager");
        if (go == null) { Debug.LogError("[Diff] not found"); return; }
        var dm = go.GetComponent<DifficultyManager>();
        var so = new SerializedObject(dm);

        // Easy: 現行維持
        SetDiff(so, "easySettings",   speedMultiplier: 1.0f, noteDensity: 1.0f);
        // Normal: Easy の 2.2倍速
        SetDiff(so, "normalSettings", speedMultiplier: 2.2f, noteDensity: 1.0f);
        // Hard: Easy の 3.5倍速
        SetDiff(so, "hardSettings",   speedMultiplier: 3.5f, noteDensity: 1.5f);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(dm);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("[Diff] Applied!  Easy=1.0x  Normal=2.2x  Hard=3.5x(density1.5)");
    }

    static void SetDiff(SerializedObject so, string field, float speedMultiplier, float noteDensity)
    {
        var prop = so.FindProperty(field);
        if (prop == null) return;
        prop.FindPropertyRelative("speedMultiplier").floatValue = speedMultiplier;
        prop.FindPropertyRelative("noteDensity").floatValue     = noteDensity;
    }
}
