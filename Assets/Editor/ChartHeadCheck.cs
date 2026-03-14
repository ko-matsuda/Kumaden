
using UnityEngine;
using UnityEditor;
using System.IO;

public class ChartHeadCheck
{
    [MenuItem("KumaDen/Check Chart Heads")]
    static void Run()
    {
        float preRoll = 4f;
        var files = new (string path, float clipLen)[]
        {
            (Application.dataPath + "/Resources/Charts/Day/kuma_odyssey_chart_kumaden_easy.json",         87.55f),
            (Application.dataPath + "/Resources/Charts/Evening/kuma_odyssey_chart_kumaden_normal.json",  119.90f),
            (Application.dataPath + "/Resources/Charts/Evening/bears_adventure_chart_kumaden_normal.json", 75.91f),
            (Application.dataPath + "/Resources/Charts/Night/kuma_odyssey_chart_kumaden_hard.json",      117.02f),
            (Application.dataPath + "/Resources/Charts/Night/bears_adventure_chart_kumaden_hard.json",    85.75f),
        };

        foreach (var (path, clipLen) in files)
        {
            if (!File.Exists(path)) { Debug.LogWarning("[Head] NOT FOUND: " + path); continue; }
            var chart = JsonUtility.FromJson<ChartData>(File.ReadAllText(path));
            if (chart?.notes == null) continue;

            float sPerBeat = 60f / chart.bpm;
            string name = Path.GetFileNameWithoutExtension(path);
            int n = chart.notes.Length;
            Debug.Log($"[Head] === {name}  BPM={chart.bpm}  notes={n} ===");

            // first 5 notes
            for (int i = 0; i < Mathf.Min(5, n); i++)
            {
                var note = chart.notes[i];
                float hitSec = (note.beat + preRoll) * sPerBeat;
                // spawn happens 3s before hit → spawnSec = hitSec - 3
                float spawnSec = hitSec - 3f;
                Debug.Log($"[Head]  [{i}] beat={note.beat:F2}  hitTime={hitSec:F2}s  spawnTime={spawnSec:F2}s");
            }
        }
    }
}
