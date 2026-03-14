
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

public class SongLoopFixer4
{
    [MenuItem("KumaDen/Check Chart Durations v4")]
    static void CheckDurations()
    {
        var go = GameObject.Find("SongLoopController");
        if (go == null) return;
        var slc = go.GetComponent<SongLoopController>();
        var so = new SerializedObject(slc);

        string[] arrays = { "songs", "songsDusk", "songsNight" };
        string[] labels = { "Day(Easy)", "Dusk(Normal)", "Night(Hard)" };

        for (int a = 0; a < arrays.Length; a++)
        {
            var arr = so.FindProperty(arrays[a]);
            for (int i = 0; i < arr.arraySize; i++)
            {
                var elem = arr.GetArrayElementAtIndex(i);
                var clip = elem.FindPropertyRelative("audioClip").objectReferenceValue as AudioClip;
                var chart = elem.FindPropertyRelative("chartJson").objectReferenceValue as TextAsset;

                float audioLen = clip != null ? clip.length : 0;
                string chartName = chart != null ? chart.name : "NULL";

                float lastBeat = 0;
                float bpm = 120;

                if (chart != null)
                {
                    // BPM取得
                    var bpmMatch = Regex.Match(chart.text, @"""bpm""\s*:\s*([\d.]+)");
                    if (bpmMatch.Success) float.TryParse(bpmMatch.Groups[1].Value, out bpm);

                    // beat の最大値を取得
                    var beatMatches = Regex.Matches(chart.text, @"""beat""\s*:\s*([\d.]+)");
                    foreach (Match m in beatMatches)
                    {
                        if (float.TryParse(m.Groups[1].Value, out float b) && b > lastBeat)
                            lastBeat = b;
                    }
                }

                // beat → 秒換算 (BPMは四分音符/分, beat = 四分音符単位)
                float lastNoteSec = (lastBeat / bpm) * 60f;

                string status;
                if (lastNoteSec > audioLen + 0.5f)
                    status = $"⚠️ OVER by {lastNoteSec - audioLen:F2}s";
                else if (audioLen - lastNoteSec > 10f)
                    status = $"⚠️ CHART SHORT by {audioLen - lastNoteSec:F2}s";
                else
                    status = "✅ OK";

                Debug.Log($"[ChkDur4] {labels[a]}[{i}] {chartName}");
                Debug.Log($"  BPM={bpm}  LastBeat={lastBeat:F1}  LastNoteSec={lastNoteSec:F2}s  AudioLen={audioLen:F2}s  {status}");
            }
        }
    }
}
#endif
