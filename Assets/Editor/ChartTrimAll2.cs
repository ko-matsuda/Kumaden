
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

[System.Serializable] class TN2 { public float beat; public int lane; public string type; public bool linkedHold; public float duration; public float holdDuration; }
[System.Serializable] class TC2 { public string songName; public int bpm; public float offset; public TN2[] notes; }

public class ChartTrimAll2
{
    // 全曲: クリップ終了の6秒前以前にラストノーツが来るよう調整
    // Evening B  BPM=122 clip=75.91s → target≤69.91s → beat<=136 → hit=68.85s (7.1s余裕)
    // Night A    BPM=121 clip=117.02s → 既に104.00s (13s余裕) → 変更不要
    // Night B    BPM=122 clip=85.75s  → target≤79.75s → beat<=153.5 → hit=77.46s (8.3s余裕)
    static readonly (string path, float trimBeat, float clipLen)[] Targets = new[]
    {
        (Application.dataPath + "/Resources/Charts/Evening/bears_adventure_chart_kumaden_normal.json", 136f,   75.91f),
        (Application.dataPath + "/Resources/Charts/Night/bears_adventure_chart_kumaden_hard.json",     153.5f, 85.75f),
    };

    [MenuItem("KumaDen/Trim All Chart Tails v2")]
    static void Trim()
    {
        float preRoll = 4f;
        foreach (var (path, trimBeat, clipLen) in Targets)
        {
            if (!File.Exists(path)) { Debug.LogError("[Trim2] NOT FOUND: " + path); continue; }
            var chart = JsonUtility.FromJson<TC2>(File.ReadAllText(path));
            if (chart?.notes == null) { Debug.LogError("[Trim2] parse fail: " + path); continue; }

            int before = chart.notes.Length;
            chart.notes = chart.notes.Where(n => n.beat <= trimBeat).ToArray();
            int after = chart.notes.Length;

            File.WriteAllText(path, JsonUtility.ToJson(chart, true));

            float sPerBeat = 60f / chart.bpm;
            var last = chart.notes[after - 1];
            float hitSec = (last.beat + preRoll) * sPerBeat;
            float margin = clipLen - hitSec;
            string name = Path.GetFileNameWithoutExtension(path);
            Debug.Log($"[Trim2] {name}: {before}->{after} notes | lastHit={hitSec:F2}s | margin={margin:F2}s | clip={clipLen}s");
        }
        AssetDatabase.Refresh();
        Debug.Log("[Trim2] Done.");
    }
}
