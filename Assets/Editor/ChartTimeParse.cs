
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class ChartTimeParse
{
    static float GetLastNoteTime(string path)
    {
        if (!File.Exists(path)) return -1f;
        string json = File.ReadAllText(path);
        // Try common key names: "time", "t", "beat", "sec"
        var matches = Regex.Matches(json, @"""(?:time|t|sec|beat)""\s*:\s*([\d.]+)");
        if (matches.Count == 0) return 0f;
        return matches.Cast<Match>().Max(m => float.Parse(m.Groups[1].Value));
    }

    static string GetKeys(string path)
    {
        if (!File.Exists(path)) return "N/A";
        string json = File.ReadAllText(path);
        var keys = Regex.Matches(json, @"""(\w+)""\s*:").Cast<Match>()
            .Select(m => m.Groups[1].Value).Distinct().Take(20);
        return string.Join(", ", keys);
    }

    [MenuItem("KumaDen/Parse Chart Times")]
    static void Parse()
    {
        // First show available keys in easy chart
        Debug.Log("[CTP] Keys in easy: " + GetKeys("Assets/Resources/Charts/Day/kuma_odyssey_chart_kumaden_easy.json"));

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
            float last = GetLastNoteTime("Assets/Resources/Charts/" + f[0]);
            float audio = float.Parse(f[1]);
            float diff = audio - last;
            string st = last <= 0 ? "?KEY" : diff >= 0 && diff < 10 ? "OK" : diff < 0 ? "OVER" : "GAP";
            Debug.Log($"[CTP] {f[0].Split('/')[1].Replace(".json","")}: lastNote={last:F2}s  audio={audio}s  diff={diff:+0.00;-0.00}s  [{st}]");
        }
    }
}
#endif
