
using UnityEngine;
using UnityEditor;
using System.IO;

public class ChartFullDump
{
    [MenuItem("KumaDen/Dump Chart Full")]
    static void Run()
    {
        var files = new string[]
        {
            Application.dataPath + "/Resources/Charts/Night/kuma_odyssey_chart_kumaden_hard.json",
            Application.dataPath + "/Resources/Charts/Evening/bears_adventure_chart_kumaden_normal.json",
        };
        foreach (var path in files)
        {
            if (!File.Exists(path)) { Debug.LogWarning("NOT FOUND: " + path); continue; }
            var chart = JsonUtility.FromJson<ChartData>(File.ReadAllText(path));
            string name = Path.GetFileNameWithoutExtension(path);
            float sPerBeat = 60f / chart.bpm;
            Debug.Log($"[Full] === {name} BPM={chart.bpm} notes={chart.notes.Length} ===");
            for (int i = 0; i < chart.notes.Length; i++)
            {
                var n = chart.notes[i];
                float hitSec = (n.beat + 4f) * sPerBeat;
                Debug.Log($"[Full] [{i:D2}] beat={n.beat:F2} lane={n.lane} hit={hitSec:F2}s");
            }
        }
    }
}
