using UnityEditor;
using System.IO;

public class FindScripts
{
    [MenuItem("Tools/FindScriptPaths")]
    static void Find()
    {
        foreach (var g in AssetDatabase.FindAssets("t:Script ChartSpawner"))
            UnityEngine.Debug.Log("ChartSpawner: " + AssetDatabase.GUIDToAssetPath(g));
        foreach (var g in AssetDatabase.FindAssets("t:Script DifficultyManager"))
            UnityEngine.Debug.Log("DifficultyManager: " + AssetDatabase.GUIDToAssetPath(g));
        foreach (var g in AssetDatabase.FindAssets("t:Script SongLoopController"))
            UnityEngine.Debug.Log("SongLoopController: " + AssetDatabase.GUIDToAssetPath(g));
    }
}
