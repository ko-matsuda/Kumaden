
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class ChartJsonPeek
{
    [MenuItem("KumaDen/Peek Chart JSON Structure")]
    static void Peek()
    {
        string path = "Assets/Resources/Charts/Day/kuma_odyssey_chart_kumaden_easy.json";
        if (!File.Exists(path)) { Debug.LogError("[Peek] not found: " + path); return; }
        string json = File.ReadAllText(path);
        // Show first 800 chars
        Debug.Log("[Peek] First 800 chars:\n" + json.Substring(0, Mathf.Min(800, json.Length)));
        // Show last 400 chars
        int len = json.Length;
        Debug.Log("[Peek] Last 400 chars:\n" + json.Substring(Mathf.Max(0, len - 400)));
    }
}
#endif
