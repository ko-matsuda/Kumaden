using UnityEngine;
using UnityEditor;

public class CheckChart
{
    [MenuItem("Tools/Check Chart File")]
    static void Check()
    {
        TextAsset chart = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Charts/kumaden_chart.json");
        if (chart != null)
        {
            Debug.Log("=== CHART CONTENT START ===");
            Debug.Log(chart.text);
            Debug.Log("=== CHART CONTENT END ===");
        }
        else
        {
            Debug.LogError("Chart file not found!");
        }
    }
}
