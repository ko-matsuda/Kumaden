
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class ChartJsonPeek2
{
    static void PeekFile(string path)
    {
        if (!File.Exists(path)) { Debug.LogError("[Peek2] NOT FOUND: " + path); return; }
        string json = File.ReadAllText(path);
        if (string.IsNullOrEmpty(json)) { Debug.LogWarning("[Peek2] EMPTY: " + path); return; }
        int previewLen = Mathf.Min(400, json.Length);
        Debug.Log($"[Peek2] {System.IO.Path.GetFileName(path)} (len={json.Length}):\n{json.Substring(0, previewLen)}");
    }

    [MenuItem("KumaDen/Peek All Charts")]
    static void PeekAll()
    {
        PeekFile("Assets/Resources/Charts/Day/kuma_odyssey_chart_kumaden_easy.json");
        PeekFile("Assets/Resources/Charts/Evening/kuma_odyssey_chart_kumaden_normal.json");
        PeekFile("Assets/Resources/Charts/Night/kuma_odyssey_chart_kumaden_hard.json");
    }
}
#endif
