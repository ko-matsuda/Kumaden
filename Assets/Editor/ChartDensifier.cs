
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

[System.Serializable] class DN { public float beat; public int lane; public string type; public bool linkedHold; public float duration; public float holdDuration; }
[System.Serializable] class DC { public string songName; public int bpm; public float offset; public DN[] notes; }

public class ChartDensifier
{
    // Evening Normal: NPS目標 ~1.2、ギャップ閾値8拍
    // Night Hard:     NPS目標 ~1.5、ギャップ閾値6拍
    static readonly (string path, float gapThreshold, float introTarget)[] Targets = new[]
    {
        (Application.dataPath + "/Resources/Charts/Evening/kuma_odyssey_chart_kumaden_normal.json",   8f, 4f),
        (Application.dataPath + "/Resources/Charts/Evening/bears_adventure_chart_kumaden_normal.json", 8f, 4f),
        (Application.dataPath + "/Resources/Charts/Night/kuma_odyssey_chart_kumaden_hard.json",        6f, 4f),
        (Application.dataPath + "/Resources/Charts/Night/bears_adventure_chart_kumaden_hard.json",     6f, 4f),
    };

    static readonly string[] TYPES = { "Flour", "Milk", "Egg" };

    [MenuItem("KumaDen/Densify Charts")]
    static void Densify()
    {
        foreach (var (path, gapThreshold, introTarget) in Targets)
        {
            if (!File.Exists(path)) { Debug.LogError("[Dense] NOT FOUND: " + path); continue; }
            var chart = JsonUtility.FromJson<DC>(File.ReadAllText(path));
            if (chart?.notes == null) continue;

            var notes = chart.notes.ToList();
            int addedIntro = 0, addedGap = 0;

            // --- 1. イントロ補完: firstBeat > introTarget なら 4拍ごとに追加 ---
            float firstBeat = notes[0].beat;
            int laneCounter = notes[0].lane; // 既存最初のレーンから逆算してローテ

            // introTarget から 4拍ステップで firstBeat の手前まで
            for (float b = introTarget; b < firstBeat - 1f; b += 4f)
            {
                laneCounter = (laneCounter + 2) % 3; // 逆ローテで被りにくく
                notes.Add(new DN { beat = b, lane = laneCounter, type = TYPES[laneCounter] });
                addedIntro++;
            }

            // --- 2. ギャップ補完: 連続する2ノーツ間がgapThreshold拍を超えたら中間に追加 ---
            // ソート後に再スキャン
            notes = notes.OrderBy(n => n.beat).ToList();

            var toAdd = new List<DN>();
            for (int i = 0; i < notes.Count - 1; i++)
            {
                float gap = notes[i + 1].beat - notes[i].beat;
                if (gap > gapThreshold)
                {
                    // ギャップを gapThreshold 以下になるよう等分割
                    int divisions = Mathf.CeilToInt(gap / gapThreshold);
                    float step = gap / divisions;
                    int lc = (notes[i].lane + 1) % 3;
                    for (int d = 1; d < divisions; d++)
                    {
                        float newBeat = notes[i].beat + step * d;
                        // 既存ノーツとの重複チェック (0.2拍以内は追加しない)
                        bool tooClose = notes.Any(n => Mathf.Abs(n.beat - newBeat) < 0.2f)
                                     || toAdd.Any(n => Mathf.Abs(n.beat - newBeat) < 0.2f);
                        if (!tooClose)
                        {
                            toAdd.Add(new DN { beat = newBeat, lane = lc, type = TYPES[lc] });
                            lc = (lc + 1) % 3;
                            addedGap++;
                        }
                    }
                }
            }

            notes.AddRange(toAdd);
            notes = notes.OrderBy(n => n.beat).ToList();
            chart.notes = notes.ToArray();

            File.WriteAllText(path, JsonUtility.ToJson(chart, true));

            float sPerBeat = 60f / chart.bpm;
            float fh = (chart.notes[0].beat + 4f) * sPerBeat;
            float lh = (chart.notes[chart.notes.Length - 1].beat + 4f) * sPerBeat;
            float nps = (lh - fh) > 0 ? chart.notes.Length / (lh - fh) : 0;
            string name = Path.GetFileNameWithoutExtension(path);
            Debug.Log($"[Dense] {name}: +{addedIntro} intro +{addedGap} gap = {chart.notes.Length} total | firstHit={fh:F2}s | NPS={nps:F2}");
        }
        AssetDatabase.Refresh();
        Debug.Log("[Dense] Done.");
    }
}
