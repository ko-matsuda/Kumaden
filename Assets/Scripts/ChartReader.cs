using UnityEngine;
using UnityEditor;

public class ChartReader : MonoBehaviour
{
    [MenuItem("Tools/Read Chart Files")]
    public static void ReadChartFiles()
    {
        // Normal
        TextAsset normalChart = Resources.Load<TextAsset>("Charts/kuma_odyssey_chart_kumaden");
        if (normalChart != null)
        {
            Debug.Log("=== NORMAL CHART ===");
            Debug.Log(normalChart.text);
        }
        
        // Hard
        TextAsset hardChart = Resources.Load<TextAsset>("Charts/kuma_odyssey_chart_kumaden_hard");
        if (hardChart != null)
        {
            Debug.Log("=== HARD CHART ===");
            Debug.Log(hardChart.text);
        }
        
        // Easy
        TextAsset easyChart = Resources.Load<TextAsset>("Charts/kuma_odyssey_chart_kumaden_easy");
        if (easyChart != null)
        {
            Debug.Log("=== EASY CHART ===");
            Debug.Log(easyChart.text);
        }
    }
}
