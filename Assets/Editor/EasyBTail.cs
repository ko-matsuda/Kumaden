using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

[System.Serializable] class EN { public float beat; public int lane; public string type=""; public bool linkedHold=false; public float duration=0; public float holdDuration=0; }
[System.Serializable] class EC { public string songName; public int bpm; public float offset; public EN[] notes; }

public class EasyBTail
{
    // Easy B: bears_adventure_chart_kumaden_easy  BPM=122 clip=86.47s
    const float BPM   = 122f;
    const float CLIP  = 86.47f;
    const float PREROLL = 4f;

    [MenuItem("KumaDen/Fix Easy B Tail")]
static void Fix()
    {
        string path = Application.dataPath + "/Resources/Charts/Day/bears_adventure_chart_kumaden_easy.json";
        if (!File.Exists(path)) { Debug.LogError("[EasyB] NOT FOUND: " + path); return; }

        var chart = JsonUtility.FromJson<EC>(File.ReadAllText(path));
        var notes = new List<EN>(chart.notes.Select(n => new EN { beat=n.beat, lane=n.lane, type=n.type, linkedHold=n.linkedHold, duration=n.duration, holdDuration=n.holdDuration }));

        float spb = 60f / BPM;
        float maxBeat = (CLIP - 4f) * BPM / 60f - PREROLL; // 163.7

        // 末尾のレーン流れを引き継ぐ (beat=142 lane=1から)
        // 隣レーン優先で自然な流れを作る
        int[] laneSeq = { 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0 }; // lane=1の隣から開始
        int li = 0;
        float step = 2f; // Easyは2beat間隔

        for (float b = 144f; b <= maxBeat; b += step)
        {
            if (!notes.Any(n => Mathf.Abs(n.beat - b) < 0.5f))
            {
                int lane = laneSeq[li++ % laneSeq.Length];
                notes.Add(new EN { beat = b, lane = lane });
            }
        }

        notes.Sort((a, b2) => a.beat.CompareTo(b2.beat));
        chart.notes = notes.ToArray();
        File.WriteAllText(path, JsonUtility.ToJson(chart, true));
        AssetDatabase.Refresh();

        var last = notes[notes.Count - 1];
        float lastHit = (last.beat + PREROLL) * spb;
        Debug.Log($"[EasyB] Done: {notes.Count} notes | lastBeat={last.beat:F1} lastHit={lastHit:F2}s margin={(CLIP-lastHit):F2}s");
    }
}
