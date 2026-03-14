
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class SongLoopFixer
{
    [MenuItem("KumaDen/Fix SongLoop Settings")]
    static void Fix()
    {
        // 1. Chart ファイル一覧を確認
        string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/Resources/Charts" });
        Debug.Log($"[Fix] Found {guids.Length} chart files:");
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            Debug.Log($"  {Path.GetFileName(p)}  ({p})");
        }

        // 2. Normal chart を _normal suffix にリネーム
        string[] normalTargets = {
            "kuma_odyssey_chart_kumaden",
            "bears_adventure_chart_kumaden"
        };

        foreach (var target in normalTargets)
        {
            string[] found = AssetDatabase.FindAssets(target, new[] { "Assets/Resources/Charts" });
            foreach (var g in found)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                string filename = Path.GetFileNameWithoutExtension(path);
                // すでに _easy / _hard / _normal が付いているものはスキップ
                if (filename.EndsWith("_easy") || filename.EndsWith("_hard") || filename.EndsWith("_normal"))
                    continue;
                // 完全一致チェック
                if (filename != target) continue;

                string newName = target + "_normal";
                string result = AssetDatabase.RenameAsset(path, newName);
                if (string.IsNullOrEmpty(result))
                    Debug.Log($"[Fix] Renamed: {filename} -> {newName}");
                else
                    Debug.LogError($"[Fix] Rename failed: {result}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 3. SongLoopController の songsDusk 参照を更新
        var go = GameObject.Find("SongLoopController");
        if (go == null) { Debug.LogError("[Fix] SongLoopController not found"); return; }
        var slc = go.GetComponent<SongLoopController>();
        var so = new SerializedObject(slc);

        string[] newChartNames = {
            "kuma_odyssey_chart_kumaden_normal",
            "bears_adventure_chart_kumaden_normal"
        };

        var dusk = so.FindProperty("songsDusk");
        for (int i = 0; i < dusk.arraySize && i < newChartNames.Length; i++)
        {
            var elem = dusk.GetArrayElementAtIndex(i);
            var chartProp = elem.FindPropertyRelative("chartJson");
            var newAsset = Resources.Load<TextAsset>($"Charts/{newChartNames[i]}");
            if (newAsset != null)
            {
                chartProp.objectReferenceValue = newAsset;
                Debug.Log($"[Fix] songsDusk[{i}].chartJson -> {newChartNames[i]}");
            }
            else
            {
                Debug.LogWarning($"[Fix] Could not load: Charts/{newChartNames[i]}");
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(slc);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(slc.gameObject.scene);

        Debug.Log("[Fix] Done. Please save the scene (Ctrl+S).");
    }

    [MenuItem("KumaDen/Check Chart Durations")]
    static void CheckChartDurations()
    {
        // 各チャートの最終ノーツ時間を確認
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
                var clip = (elem.FindPropertyRelative("audioClip").objectReferenceValue as AudioClip);
                var chart = (elem.FindPropertyRelative("chartJson").objectReferenceValue as TextAsset);

                float audioLen = clip != null ? clip.length : 0;
                float lastNote = 0;

                if (chart != null)
                {
                    // JSON から最後の time 値を探す
                    string json = chart.text;
                    // "time": X.XX を全て探して最大値を取得
                    int idx = 0;
                    while (true)
                    {
                        idx = json.IndexOf("\"time\"", idx);
                        if (idx < 0) break;
                        int colon = json.IndexOf(':', idx);
                        int start = colon + 1;
                        while (start < json.Length && (json[start] == ' ' || json[start] == '\t')) start++;
                        int end = start;
                        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.')) end++;
                        if (end > start && float.TryParse(json.Substring(start, end - start), out float t))
                            if (t > lastNote) lastNote = t;
                        idx = end;
                    }
                }

                string status = (lastNote > audioLen) ? "⚠️ OVER" : "✅ OK";
                Debug.Log($"[ChkDur] {labels[a]}[{i}] Audio={audioLen:F2}s  LastNote={lastNote:F2}s  {status}");
            }
        }
    }
}
#endif
