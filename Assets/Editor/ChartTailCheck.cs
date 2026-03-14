
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class ChartTailCheck
{
    [MenuItem("KumaDen/Check Chart Tails")]
    static void Run()
    {
        // Normal Song A: kuma_odyssey_chart_kumaden_normal
        // Normal Song B: bears_adventure_chart_kumaden_normal
        string root = Application.dataPath + "/Resources/Charts";
        string[] files = {
            root + "/Evening/kuma_odyssey_chart_kumaden_normal.json",
            root + "/Evening/bears_adventure_chart_kumaden_normal.json",
            root + "/Day/kuma_odyssey_chart_kumaden_easy.json",
        };
        float preRollBeats = 4f;

        foreach (var path in files)
        {
            if (!File.Exists(path)) { Debug.LogWarning("[Tail] not found: " + path); continue; }
            var json = File.ReadAllText(path);
            var chart = JsonUtility.FromJson<ChartData>(json);
            if (chart == null || chart.notes == null) { Debug.LogWarning("[Tail] parse fail: " + path); continue; }

            float secPerBeat = 60f / chart.bpm;
            string name = Path.GetFileNameWithoutExtension(path);

            // last 10 notes
            int start = Mathf.Max(0, chart.notes.Length - 10);
            Debug.Log($"[Tail] === {name}  BPM={chart.bpm}  notes={chart.notes.Length} ===");
            for (int i = start; i < chart.notes.Length; i++)
            {
                var n = chart.notes[i];
                // actual hit time in seconds (with preRoll)
                float hitSec = (n.beat + preRollBeats) * secPerBeat;
                Debug.Log($"[Tail]  [{i}] beat={n.beat:F2}  hitTime={hitSec:F2}s  lane={n.lane}");
            }
        }

        // Also log spawnAheadBeats travel time per difficulty
        float[] speeds = {1.0f, 2.2f, 3.5f};
        string[] labels = {"Easy", "Normal", "Hard"};
        float spawnAheadBeats = 8f;
        float bpm = 121f;
        float sPerBeat = 60f / bpm;
        Debug.Log("[Tail] === Travel times per difficulty (spawnAheadBeats=8) ===");
        foreach (var (s, l) in speeds.Zip(labels, (a,b) => (a,b)))
        {
            float effectiveBeats = spawnAheadBeats / s;
            float travelSec = effectiveBeats * sPerBeat;
            Debug.Log($"[Tail]  {l}: effectiveBeats={effectiveBeats:F2}  travelTime={travelSec:F2}s");
        }
    }
}
