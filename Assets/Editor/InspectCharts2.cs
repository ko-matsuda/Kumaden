using UnityEditor;
using UnityEngine;

public class InspectCharts2
{
    [MenuItem("Tools/InspectCharts2")]
    static void Inspect()
    {
        var guids = AssetDatabase.FindAssets("t:TextAsset", new[]{"Assets/Resources/Charts"});
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (ta == null) continue;
            try
            {
                var data = JsonUtility.FromJson<ChartData>(ta.text);
                if (data == null || data.notes == null) { Debug.Log($"{ta.name}: parse fail"); continue; }
                float lastBeat = 0;
                foreach (var n in data.notes)
                    if (n.beat > lastBeat) lastBeat = n.beat;
                float lastSec = lastBeat * (60f / data.bpm);
                Debug.Log($"{ta.name}: BPM={data.bpm}, notes={data.notes.Length}, lastBeat={lastBeat:F1}, lastSec~{lastSec:F1}s");
            }
            catch { Debug.Log($"{ta.name}: exception"); }
        }
    }
}
