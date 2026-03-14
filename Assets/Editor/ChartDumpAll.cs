using UnityEngine;
using UnityEditor;
using System.IO;

public class ChartDumpAll
{
    [MenuItem("KumaDen/Dump All Charts JSON")]
    static void Run()
    {
        string[] paths = {
            Application.dataPath + "/Resources/Charts/Day/kuma_odyssey_chart_kumaden_easy.json",
            Application.dataPath + "/Resources/Charts/Day/bears_adventure_chart_kumaden_easy.json",
            Application.dataPath + "/Resources/Charts/Evening/kuma_odyssey_chart_kumaden_normal.json",
            Application.dataPath + "/Resources/Charts/Evening/bears_adventure_chart_kumaden_normal.json",
            Application.dataPath + "/Resources/Charts/Night/kuma_odyssey_chart_kumaden_hard.json",
            Application.dataPath + "/Resources/Charts/Night/bears_adventure_chart_kumaden_hard.json",
        };
        foreach (var p in paths)
        {
            string name = Path.GetFileNameWithoutExtension(p);
            if (!File.Exists(p)) { Debug.LogWarning("[Dump] NOT FOUND: " + name); continue; }
            var chart = JsonUtility.FromJson<ChartData>(File.ReadAllText(p));
            float spb = 60f / chart.bpm;
            Debug.Log($"[Dump] === {name}  BPM={chart.bpm}  notes={chart.notes.Length} ===");
            for (int i = 0; i < chart.notes.Length; i++)
            {
                var n = chart.notes[i];
                string hold = n.linkedHold ? $" HOLD dur={n.holdDuration}" : (n.duration > 0 ? $" DUR={n.duration}" : "");
                Debug.Log($"[Dump][{name}] [{i:D3}] beat={n.beat:F2} lane={n.lane}{hold}");
            }
        }
    }
}
