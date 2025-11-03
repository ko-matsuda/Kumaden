// Assets/Editor/CleanMissingScripts.cs
// 旧C#でも通る安全実装（target-typed new 等は不使用）
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class CleanMissingScripts
{
    [MenuItem("Tools/Missing Scripts/Remove In Open Scenes")]
    public static void RemoveInOpenScenes()
    {
        int removed = 0;
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            removed += RemoveInHierarchy(roots[r]);
        }
        Debug.Log("[CleanMissingScripts] Removed: " + removed + " missing scripts in open scene(s).");
    }

    [MenuItem("Tools/Missing Scripts/Scan All Assets")]
    public static void ScanAllAssets()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            count += CountMissingInHierarchy(prefab);
        }
        Debug.Log("[CleanMissingScripts] Prefabs with missing scripts (count of missing components): " + count);
    }

    [MenuItem("Tools/Missing Scripts/Remove In All Prefabs")]
    public static void RemoveInAllPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int removed = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            Undo.RecordObject(prefab, "Remove Missing Scripts");
            removed += RemoveInHierarchy(prefab);
            EditorUtility.SetDirty(prefab);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[CleanMissingScripts] Removed: " + removed + " missing scripts in prefabs.");
    }

    private static int RemoveInHierarchy(GameObject go)
    {
        int removed = 0;
        Transform[] trs = go.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < trs.Length; i++)
        {
            GameObject g = trs[i].gameObject;
            // Missingなコンポーネントを削除
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);
        }
        return removed;
    }

    private static int CountMissingInHierarchy(GameObject go)
    {
        int count = 0;
        Transform[] trs = go.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < trs.Length; i++)
        {
            Component[] comps = trs[i].GetComponents<Component>();
            for (int c = 0; c < comps.Length; c++)
            {
                if (comps[c] == null) count++;
            }
        }
        return count;
    }
}
