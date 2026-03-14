using UnityEditor;
using UnityEngine;

public class InspectCharts
{
    [MenuItem("Tools/InspectCharts")]
    static void Inspect()
    {
        string[] charts = {
            "Assets/Resources/Charts/kuma_odyssey_chart_kumaden_easy.json",
            "Assets/Resources/Charts/bears_adventure_chart_kumaden_easy.json",
            "Assets/Resources/Charts/kuma_odyssey_chart_kumaden.json",
            "Assets/Resources/Charts/bears_adventure_chart_kumaden.json",
            "Assets/Resources/Charts/kuma_odyssey_chart_kumaden_hard.json",
            "Assets/Resources/Charts/bears_adventure_chart_kumaden_hard.json",
        };
        foreach (var path in charts)
        {
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (ta == null) { Debug.Log($"NOT FOUND: {path}"); continue; }
            var data = JsonUtility.FromJson<ChartData>(ta.text);
            if (data == null) { Debug.Log($"PARSE FAIL: {path}"); continue; }
            float lastBeat = 0;
            if (data.notes != null)
                foreach (var n in data.notes)
                    if (n.beat > lastBeat) lastBeat = n.beat;
            float lastSec = lastBeat * (60f / data.bpm);
            Debug.Log($"{ta.name}: BPM={data.bpm}, notes={data.notes?.Length}, lastBeat={lastBeat:F1}, lastSec={lastSec:F1}s");
        }
    }
}
