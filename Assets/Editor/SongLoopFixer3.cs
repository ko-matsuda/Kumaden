
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

public class SongLoopFixer3
{
    [MenuItem("KumaDen/Check Chart Durations v3")]
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

                float lastNote = 0;
                if (chart != null)
                {
                    // time フィールドを全部抽出して最大値を取得
                    var matches = Regex.Matches(chart.text, @"""time""\s*:\s*([\d.]+)");
                    foreach (Match m in matches)
                    {
                        if (float.TryParse(m.Groups[1].Value, out float t) && t > lastNote)
                            lastNote = t;
                    }

                    // JSON構造確認用（先頭150文字）
                    Debug.Log($"  JSON keys preview: {chart.text.Substring(0, Mathf.Min(150, chart.text.Length)).Replace("\n","").Replace("  ","")}");
                }

                string status = (lastNote > audioLen + 1f) ? "⚠️ OVER" : (lastNote == 0 ? "❓ no_time_key" : "✅ OK");
                Debug.Log($"[ChkDur3] {labels[a]}[{i}] {chartName}  Audio={audioLen:F2}s  LastNote={lastNote:F2}s  {status}");
            }
        }
    }
}
#endif
