
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class ChartBpmParse
{
    static (float bpm, float lastBeat) GetChartInfo(string path)
    {
        if (!File.Exists(path)) return (-1, -1);
        string json = File.ReadAllText(path);
        var bpmMatch = Regex.Match(json, @"""bpm""\s*:\s*([\d.]+)");
        float bpm = bpmMatch.Success ? float.Parse(bpmMatch.Groups[1].Value) : 0f;
        var beats = Regex.Matches(json, @"""beat""\s*:\s*([\d.]+)").Cast<Match>()
            .Select(m => float.Parse(m.Groups[1].Value)).ToList();
        float lastBeat = beats.Count > 0 ? beats.Max() : 0f;
        return (bpm, lastBeat);
    }

    [MenuItem("KumaDen/Audit Chart BPM and Beat")]
    static void Audit()
    {
        string[][] files = {
            new[]{"Day/kuma_odyssey_chart_kumaden_easy.json",    "87.55"},
            new[]{"Day/bears_adventure_chart_kumaden_easy.json", "86.47"},
            new[]{"Evening/kuma_odyssey_chart_kumaden_normal.json",   "119.90"},
            new[]{"Evening/bears_adventure_chart_kumaden_normal.json","75.91"},
            new[]{"Night/kuma_odyssey_chart_kumaden_hard.json",  "117.02"},
            new[]{"Night/bears_adventure_chart_kumaden_hard.json","85.75"},
        };
        foreach (var f in files)
        {
            var (bpm, lastBeat) = GetChartInfo("Assets/Resources/Charts/" + f[0]);
            float audio = float.Parse(f[1]);
            float lastSec = bpm > 0 ? lastBeat / bpm * 60f : -1f;
            float diff = audio - lastSec;
            string st = lastSec < 0 ? "?BPM" : diff >= 0 && diff < 8 ? "OK" : diff < 0 ? "OVER" : "GAP";
            Debug.Log($"[CBP] {System.IO.Path.GetFileName(f[0]).Replace(".json","")}: BPM={bpm} lastBeat={lastBeat} => {lastSec:F2}s  audio={audio}s  diff={diff:+0.00;-0.00}s  [{st}]");
        }
    }
}
#endif
