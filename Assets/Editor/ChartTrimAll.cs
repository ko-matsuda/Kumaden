
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

[System.Serializable] class TrimND { public float beat; public int lane; public string type; public bool linkedHold; public float duration; public float holdDuration; }
[System.Serializable] class TrimCD { public string songName; public int bpm; public float offset; public TrimND[] notes; }

public class ChartTrimAll
{
    // Evening B  BPM=122 clip=75.91s → beat<=140 → hit=70.82s (5秒余裕)
    // Night A    BPM=121 clip=117.02s → beat<=214 → hit=108.10s (8.9秒余裕)
    // Night B    BPM=122 clip=85.75s  → beat<=158.3 → hit=79.82s (5.4秒余裕)
    static readonly (string path, float trimBeat, float clipLen)[] Targets = new[]
    {
        (Application.dataPath + "/Resources/Charts/Evening/bears_adventure_chart_kumaden_normal.json", 140f, 75.91f),
        (Application.dataPath + "/Resources/Charts/Night/kuma_odyssey_chart_kumaden_hard.json",        214f, 117.02f),
        (Application.dataPath + "/Resources/Charts/Night/bears_adventure_chart_kumaden_hard.json",     158.3f, 85.75f),
    };

    [MenuItem("KumaDen/Trim All Chart Tails")]
    static void Trim()
    {
        float preRoll = 4f;
        foreach (var (path, trimBeat, clipLen) in Targets)
        {
            if (!File.Exists(path)) { Debug.LogError("[TrimAll] NOT FOUND: " + path); continue; }
            var chart = JsonUtility.FromJson<TrimCD>(File.ReadAllText(path));
            if (chart?.notes == null) { Debug.LogError("[TrimAll] parse fail: " + path); continue; }

            int before = chart.notes.Length;
            chart.notes = chart.notes.Where(n => n.beat <= trimBeat).ToArray();
            int after = chart.notes.Length;

            File.WriteAllText(path, JsonUtility.ToJson(chart, true));

            float sPerBeat = 60f / chart.bpm;
            var last = chart.notes[after - 1];
            float hitSec = (last.beat + preRoll) * sPerBeat;
            string name = Path.GetFileNameWithoutExtension(path);
            Debug.Log($"[TrimAll] {name}: {before}->{after} notes. lastBeat={last.beat} hit={hitSec:F2}s clip={clipLen}s margin={(clipLen-hitSec):F2}s");
        }
        AssetDatabase.Refresh();
        Debug.Log("[TrimAll] Done.");
    }
}
