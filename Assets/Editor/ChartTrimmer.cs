
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
class TrimNoteData
{
    public float beat;
    public int lane;
    public string type;
    public bool linkedHold;
    public float duration;
    public float holdDuration;
}

[System.Serializable]
class TrimChartData
{
    public string songName;
    public int bpm;
    public float offset;
    public TrimNoteData[] notes;
}

public class ChartTrimmer
{
    // Normal Song A: クリップ長 119.90s、遷移 -0.5s = 119.40s でカット
    // BPM=121, preRoll=4, lastNote がヒットする秒 = (beat + 4) * (60/121)
    // beat=218 → hitTime=110.08s  ← ここで止める (余裕 ~9秒)
    const float TRIM_BEAT = 218f;  // これ以降のノーツを削除

    [MenuItem("KumaDen/Trim Normal Song A Tail")]
    static void Trim()
    {
        string path = Application.dataPath + "/Resources/Charts/Evening/kuma_odyssey_chart_kumaden_normal.json";
        if (!File.Exists(path)) { Debug.LogError("[Trim] File not found: " + path); return; }

        string json = File.ReadAllText(path);
        TrimChartData chart = JsonUtility.FromJson<TrimChartData>(json);
        if (chart == null || chart.notes == null) { Debug.LogError("[Trim] Parse failed"); return; }

        int before = chart.notes.Length;
        chart.notes = chart.notes.Where(n => n.beat <= TRIM_BEAT).ToArray();
        int after = chart.notes.Length;

        string outJson = JsonUtility.ToJson(chart, true);
        File.WriteAllText(path, outJson);
        AssetDatabase.Refresh();

        float secPerBeat = 60f / chart.bpm;
        float lastHit = (chart.notes[after - 1].beat + 4f) * secPerBeat;
        Debug.Log($"[Trim] Done: {before} -> {after} notes. Last beat={chart.notes[after-1].beat}, hitTime={lastHit:F2}s (clip=119.90s)");
    }
}
