using UnityEditor;
using UnityEngine;

public class ListChartFiles
{
    [MenuItem("Tools/ListChartFiles")]
    static void List()
    {
        var guids = AssetDatabase.FindAssets("", new[]{"Assets/Resources/Charts"});
        foreach (var g in guids)
            Debug.Log(AssetDatabase.GUIDToAssetPath(g));
    }
}
