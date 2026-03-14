
using UnityEngine;
using UnityEditor;
using System.IO;

public class ChartDensityCheck
{
    [MenuItem("KumaDen/Check Chart Density")]
    static void Run()
    {
        float preRoll = 4f;
        var charts = new (string path, float clipLen)[]
        {
            (Application.dataPath + "/Resources/Charts/Evening/kuma_odyssey_chart_kumaden_normal.json",   119.90f),
            (Application.dataPath + "/Resources/Charts/Evening/bears_adventure_chart_kumaden_normal.json", 75.91f),
            (Application.dataPath + "/Resources/Charts/Night/kuma_odyssey_chart_kumaden_hard.json",       117.02f),
            (Application.dataPath + "/Resources/Charts/Night/bears_adventure_chart_kumaden_hard.json",    85.75f),
            (Application.dataPath + "/Resources/Charts/Day/kuma_odyssey_chart_kumaden_easy.json",         87.55f),
            (Application.dataPath + "/Resources/Charts/Day/bears_adventure_chart_kumaden_easy.json",      86.47f),
        };

        foreach (var (path, clipLen) in charts)
        {
            if (!File.Exists(path)) { Debug.LogWarning("[Density] NOT FOUND: " + path); continue; }
            var chart = JsonUtility.FromJson<ChartData>(File.ReadAllText(path));
            if (chart?.notes == null) continue;

            float sPerBeat = 60f / chart.bpm;
            int n = chart.notes.Length;
            var first = chart.notes[0];
            var last  = chart.notes[n - 1];
            float firstHit = (first.beat + preRoll) * sPerBeat;
            float lastHit  = (last.beat  + preRoll) * sPerBeat;
            float playDur  = lastHit - firstHit;
            float nps      = playDur > 0 ? n / playDur : 0;
            string name    = Path.GetFileNameWithoutExtension(path);

            Debug.Log($"[Density] {name}");
            Debug.Log($"  notes={n}  firstBeat={first.beat:F1} firstHit={firstHit:F2}s  lastHit={lastHit:F2}s  NPS={nps:F2}");
            // first 5 notes
            for (int i = 0; i < Mathf.Min(5, n); i++)
            {
                var nd = chart.notes[i];
                float hit = (nd.beat + preRoll) * sPerBeat;
                Debug.Log($"  first[{i}] beat={nd.beat:F2} hit={hit:F2}s lane={nd.lane}");
            }
        }
    }
}
