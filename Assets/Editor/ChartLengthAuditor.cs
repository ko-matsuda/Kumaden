
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class ChartLengthAuditor
{
    static readonly string[][] charts = new string[][] {
        new[] { "Day",     "kuma_odyssey_chart_kumaden_easy",   "87.55" },
        new[] { "Day",     "bears_adventure_chart_kumaden_easy","86.47" },
        new[] { "Evening", "kuma_odyssey_chart_kumaden_normal", "119.90" },
        new[] { "Evening", "bears_adventure_chart_kumaden_normal","75.91" },
        new[] { "Night",   "kuma_odyssey_chart_kumaden_hard",   "117.02" },
        new[] { "Night",   "bears_adventure_chart_kumaden_hard","85.75" },
    };

    [MenuItem("KumaDen/Audit Chart Last Note Times")]
    static void Audit()
    {
        foreach (var c in charts)
        {
            string folder = c[0]; string name = c[1]; string audioLen = c[2];
            string path = $"Assets/Resources/Charts/{folder}/{name}.json";
            if (!File.Exists(path)) { Debug.LogWarning($"[CLA] NOT FOUND: {path}"); continue; }
            string json = File.ReadAllText(path);
            float lastTime = 0f;
            int idx = 0;
            while (true)
            {
                int ti = json.IndexOf("\"time\"", idx);
                if (ti < 0) break;
                int colon = json.IndexOf(':', ti);
                int start = colon + 1;
                while (start < json.Length && (json[start] == ' ' || json[start] == '\n' || json[start] == '\r')) start++;
                int end = start;
                while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.' || json[end] == '-')) end++;
                if (end > start && float.TryParse(json.Substring(start, end - start), out float t))
                    if (t > lastTime) lastTime = t;
                idx = ti + 1;
            }
            float audioLenF = float.Parse(audioLen);
            float diff = audioLenF - lastTime;
            string status = diff >= 0 && diff < 10f ? "✅" : (diff < 0 ? "❌ OVER" : "⚠ GAP");
            Debug.Log($"[CLA] {folder}/{name}: lastNote={lastTime:F2}s  audio={audioLen}s  diff={diff:+0.00;-0.00}s  {status}");
        }
    }
}
#endif
