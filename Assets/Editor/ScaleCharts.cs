using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ScaleCharts
{
    const float MARGIN = 2.0f;
    const float MIN_GAP_BEATS = 0.25f; // ノーツ最小間隔（beat）

    struct ScaleJob
    {
        public string sourceChartPath;
        public string outputChartPath;
        public float songLength;
    }

    [MenuItem("Tools/ScaleChartsToSongs")]
    static void Scale()
    {
        var jobs = new ScaleJob[]
        {
            // Normal
            new ScaleJob {
                sourceChartPath = "Assets/Resources/Charts/Day/kuma_odyssey_chart_kumaden_easy.json",
                outputChartPath = "Assets/Resources/Charts/Evening/kuma_odyssey_chart_kumaden.json",
                songLength = 119.90f },
            new ScaleJob {
                sourceChartPath = "Assets/Resources/Charts/Day/bears_adventure_chart_kumaden_easy.json",
                outputChartPath = "Assets/Resources/Charts/Evening/bears_adventure_chart_kumaden.json",
                songLength = 60.05f },
            // Hard
            new ScaleJob {
                sourceChartPath = "Assets/Resources/Charts/Day/kuma_odyssey_chart_kumaden_easy.json",
                outputChartPath = "Assets/Resources/Charts/Night/kuma_odyssey_chart_kumaden_hard.json",
                songLength = 117.02f },
            new ScaleJob {
                sourceChartPath = "Assets/Resources/Charts/Day/bears_adventure_chart_kumaden_easy.json",
                outputChartPath = "Assets/Resources/Charts/Night/bears_adventure_chart_kumaden_hard.json",
                songLength = 85.75f },
        };

        foreach (var job in jobs)
        {
            TextAsset src = AssetDatabase.LoadAssetAtPath<TextAsset>(job.sourceChartPath);
            if (src == null) { Debug.LogWarning($"[ScaleCharts] Source not found: {job.sourceChartPath}"); continue; }

            ChartData data = JsonUtility.FromJson<ChartData>(src.text);
            if (data == null || data.notes == null || data.notes.Length == 0) { Debug.LogWarning($"[ScaleCharts] Parse fail: {src.name}"); continue; }

            float secPerBeat = 60f / data.bpm;

            // ★ easyLengthはチャートの実際の最後のノーツのsecから計算（曲の長さではない）
            float srcLastBeat = 0;
            foreach (var n in data.notes) if (n.beat > srcLastBeat) srcLastBeat = n.beat;
            float easyLastSec = srcLastBeat * secPerBeat;

            float targetSec = job.songLength - MARGIN;
            float ratio = targetSec / easyLastSec;

            int srcCount = data.notes.Length;
            int targetCount = Mathf.Max(Mathf.RoundToInt(srcCount * ratio), 10);

            // ── Step1: 全ノーツをスケーリング ──
            var scaled = new List<NoteData>();
            foreach (var n in data.notes)
            {
                var nn = new NoteData();
                nn.beat       = n.beat * ratio;
                nn.lane       = n.lane;
                nn.type       = n.type;
                nn.linkedHold = n.linkedHold;
                nn.duration     = n.duration     > 0 ? n.duration     * ratio : 0;
                nn.holdDuration = n.holdDuration  > 0 ? n.holdDuration * ratio : 0;
                scaled.Add(nn);
            }
            scaled.Sort((a, b) => a.beat.CompareTo(b.beat));

            List<NoteData> working;

            if (ratio >= 1.0f)
            {
                // ── Step2a: 曲が長い → 補間ノーツ追加 ──
                working = new List<NoteData>(scaled);
                int maxInsert = targetCount - srcCount;

                for (int iter = 0; iter < maxInsert; iter++)
                {
                    working.Sort((a, b) => a.beat.CompareTo(b.beat));

                    // 最大ギャップを探す（ホールド帯を除く）
                    float maxGap = 0;
                    int   maxIdx = -1;
                    for (int i = 0; i < working.Count - 1; i++)
                    {
                        NoteData cur = working[i];
                        float holdEnd = cur.beat + Mathf.Max(cur.duration, cur.holdDuration);
                        float gapStart = Mathf.Max(cur.beat + 0.01f, holdEnd);
                        float gap = working[i + 1].beat - gapStart;
                        if (gap > maxGap && gap >= MIN_GAP_BEATS * 2)
                        { maxGap = gap; maxIdx = i; }
                    }
                    if (maxIdx < 0) break;

                    NoteData prev = working[maxIdx];
                    NoteData next = working[maxIdx + 1];
                    float insertBeat = (Mathf.Max(prev.beat + Mathf.Max(prev.duration, prev.holdDuration), prev.beat) + next.beat) * 0.5f;

                    var extra = new NoteData();
                    extra.beat = insertBeat;
                    extra.lane = (prev.lane + 1) % 3;
                    working.Add(extra);
                }
                working.Sort((a, b) => a.beat.CompareTo(b.beat));
            }
            else
            {
                // ── Step2b: 曲が短い → 均等間引き ──
                working = new List<NoteData>();
                float step = (float)srcCount / targetCount;
                var seen = new HashSet<int>();
                for (int i = 0; i < targetCount; i++)
                {
                    int idx = Mathf.Min(Mathf.FloorToInt(i * step), srcCount - 1);
                    if (seen.Add(idx))
                        working.Add(scaled[idx]);
                }
                working.Sort((a, b) => a.beat.CompareTo(b.beat));
            }

            // ── Step3: 同beatノーツを除去（同タイミング1つのみ）──
            var deduplicated = new List<NoteData>();
            float lastBeat = -999f;
            foreach (var n in working)
            {
                if (n.beat - lastBeat < 0.05f) continue; // 近すぎる = 同タイミング扱い
                deduplicated.Add(n);
                lastBeat = n.beat;
            }

            // ── Step4: ホールド帯被りを除去 ──
            var cleaned = new List<NoteData>();
            float holdEndBeat = -999f;
            foreach (var n in deduplicated)
            {
                if (n.beat < holdEndBeat - 0.01f)
                    continue; // ホールド帯の途中 → スキップ

                cleaned.Add(n);

                float dur = Mathf.Max(n.duration, n.holdDuration);
                if (dur > 0)
                    holdEndBeat = n.beat + dur;
                else
                    holdEndBeat = -999f;
            }

            data.notes = cleaned.ToArray();

            // ログ
            float newLastBeat = 0;
            foreach (var n in data.notes) if (n.beat > newLastBeat) newLastBeat = n.beat;
            float newLastSec = newLastBeat * secPerBeat;

            Debug.Log($"[ScaleCharts] {src.name} -> {Path.GetFileName(job.outputChartPath)}: " +
                      $"notes {srcCount}->{data.notes.Length}, ratio={ratio:F3}, " +
                      $"easyLast={easyLastSec:F1}s, lastNote={newLastSec:F1}s / song={job.songLength}s");

            File.WriteAllText(job.outputChartPath, JsonUtility.ToJson(data, true));
            AssetDatabase.ImportAsset(job.outputChartPath);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[ScaleCharts] Done!");
    }
}
