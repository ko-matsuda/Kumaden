
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class ChartTailAll
{
    [MenuItem("KumaDen/Check All Chart Tails")]
    static void Run()
    {
        float preRoll = 4f;
        var targets = new (string path, float clipLen)[]
        {
            (Application.dataPath + "/Resources/Charts/Evening/bears_adventure_chart_kumaden_normal.json", 75.91f),
            (Application.dataPath + "/Resources/Charts/Night/kuma_odyssey_chart_kumaden_hard.json",       117.02f),
            (Application.dataPath + "/Resources/Charts/Night/bears_adventure_chart_kumaden_hard.json",    85.75f),
        };

        foreach (var (path, clipLen) in targets)
        {
            if (!File.Exists(path)) { Debug.LogWarning("[TailAll] NOT FOUND: " + path); continue; }
            var chart = JsonUtility.FromJson<ChartData>(File.ReadAllText(path));
            if (chart?.notes == null) { Debug.LogWarning("[TailAll] parse fail: " + path); continue; }

            float sPerBeat = 60f / chart.bpm;
            string name = Path.GetFileNameWithoutExtension(path);
            int n = chart.notes.Length;
            Debug.Log($"[TailAll] === {name}  BPM={chart.bpm}  notes={n}  clip={clipLen}s ===");

            // last 10
            for (int i = Mathf.Max(0, n - 10); i < n; i++)
            {
                var note = chart.notes[i];
                float hitSec = (note.beat + preRoll) * sPerBeat;
                string over = hitSec > clipLen - 0.5f ? "  ⚠ OVER" : "";
                Debug.Log($"[TailAll]  [{i}] beat={note.beat:F1}  hit={hitSec:F2}s  (clip ends {clipLen}s){over}");
            }
        }
    }
}
