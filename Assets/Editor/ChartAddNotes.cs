
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

[System.Serializable] class AN { public float beat; public int lane; public string type = ""; public bool linkedHold = false; public float duration = 0; public float holdDuration = 0; }
[System.Serializable] class AC { public string songName; public int bpm; public float offset; public AN[] notes; }

public class ChartAddNotes
{
    [MenuItem("KumaDen/Add Notes to Charts")]
    static void Run()
    {
        AddNightAIntro();
        AddEveningBDensity();
    }

    // Night A: beat 4〜15 に 2.76beat間隔で5ノーツ追加（既存パターンの疎な間隔に合わせる）
    static void AddNightAIntro()
    {
        string path = Application.dataPath + "/Resources/Charts/Night/kuma_odyssey_chart_kumaden_hard.json";
        var chart = JsonUtility.FromJson<AC>(File.ReadAllText(path));
        var notes = new List<AN>(chart.notes.Select(n => new AN { beat=n.beat, lane=n.lane, type=n.type, linkedHold=n.linkedHold, duration=n.duration, holdDuration=n.holdDuration }));

        // 2.76beat間隔、lane 1→2→0→1→2
        float[] newBeats = { 4.00f, 6.76f, 9.52f, 12.28f, 15.04f };
        int[] newLanes  = {    1,      2,     0,      1,      2   };

        for (int i = 0; i < newBeats.Length; i++)
            notes.Add(new AN { beat = newBeats[i], lane = newLanes[i] });

        // beat順にソート
        notes.Sort((a, b) => a.beat.CompareTo(b.beat));
        chart.notes = notes.ToArray();

        File.WriteAllText(path, JsonUtility.ToJson(chart, true));

        float sPerBeat = 60f / chart.bpm;
        Debug.Log($"[AddNotes] Night A: {chart.notes.Length} notes. First note beat={chart.notes[0].beat} hitTime={(chart.notes[0].beat+4f)*sPerBeat:F2}s");
    }

    // Evening B: 既存ノーツの中間（2beat後）にノーツを追加。ギャップ部分（36,60,84,108beat付近）は除く
    static void AddEveningBDensity()
    {
        string path = Application.dataPath + "/Resources/Charts/Evening/bears_adventure_chart_kumaden_normal.json";
        var chart = JsonUtility.FromJson<AC>(File.ReadAllText(path));
        var notes = new List<AN>(chart.notes.Select(n => new AN { beat=n.beat, lane=n.lane, type=n.type, linkedHold=n.linkedHold, duration=n.duration, holdDuration=n.holdDuration }));

        // 既存ノーツのbeat一覧
        var existingBeats = new HashSet<float>(chart.notes.Select(n => n.beat));

        // ギャップ領域（音楽的な休符区間）
        float[] gapBeats = { 34f, 36f, 38f, 58f, 60f, 62f, 82f, 84f, 86f, 106f, 108f, 110f };

        int[] laneSeq = { 0, 2, 1, 0, 2, 1, 0, 2, 1, 0, 2, 1, 0, 2, 1 };
        int laneIdx = 0;

        // 各連続する4beatペアの中間に1ノーツ挿入
        var sorted = chart.notes.OrderBy(n => n.beat).ToArray();
        for (int i = 0; i < sorted.Length - 1; i++)
        {
            float a = sorted[i].beat;
            float b = sorted[i+1].beat;
            float diff = b - a;

            // 4beat間隔のペアのみ（ギャップ=8beat以上はスキップ）
            if (diff < 3.5f || diff > 5f) continue;

            float midBeat = a + 2f;

            // ギャップ付近はスキップ
            if (gapBeats.Any(g => Mathf.Abs(midBeat - g) < 1.5f)) continue;

            // 既存と重複しない
            if (existingBeats.Contains(midBeat)) continue;

            // 隣と同じlaneにならないよう調整
            int newLane = laneSeq[laneIdx % laneSeq.Length];
            if (newLane == sorted[i].lane) newLane = (newLane + 1) % 3;
            if (newLane == sorted[i+1].lane) newLane = (newLane + 1) % 3;

            notes.Add(new AN { beat = midBeat, lane = newLane });
            existingBeats.Add(midBeat);
            laneIdx++;
        }

        notes.Sort((a, b) => a.beat.CompareTo(b.beat));
        chart.notes = notes.ToArray();

        File.WriteAllText(path, JsonUtility.ToJson(chart, true));

        Debug.Log($"[AddNotes] Evening B: {chart.notes.Length} notes total.");
    }

    [MenuItem("KumaDen/Add Notes to Charts", validate=true)]
    static bool Validate()
    {
        return !Application.isPlaying;
    }
}
