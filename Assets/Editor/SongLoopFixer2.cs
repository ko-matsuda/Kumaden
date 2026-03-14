
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class SongLoopFixer2
{
    [MenuItem("KumaDen/Fix SongLoop References")]
    static void FixReferences()
    {
        var go = GameObject.Find("SongLoopController");
        if (go == null) { Debug.LogError("[Fix2] SongLoopController not found"); return; }
        var slc = go.GetComponent<SongLoopController>();
        var so = new SerializedObject(slc);

        // Evening フォルダ内のリネーム済みアセットを GUID で直接取得
        string[] names = { "kuma_odyssey_chart_kumaden_normal", "bears_adventure_chart_kumaden_normal" };
        var dusk = so.FindProperty("songsDusk");

        for (int i = 0; i < names.Length && i < dusk.arraySize; i++)
        {
            string[] guids = AssetDatabase.FindAssets(names[i], new[] { "Assets/Resources/Charts/Evening" });
            if (guids.Length == 0) { Debug.LogWarning($"[Fix2] Not found: {names[i]}"); continue; }

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (asset == null) { Debug.LogWarning($"[Fix2] Load failed: {assetPath}"); continue; }

            var elem = dusk.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("chartJson").objectReferenceValue = asset;
            Debug.Log($"[Fix2] songsDusk[{i}].chartJson -> {asset.name} ({assetPath})");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(slc);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(slc.gameObject.scene);
        Debug.Log("[Fix2] References updated.");
    }

    [MenuItem("KumaDen/Check Chart Durations v2")]
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

                // JSON の構造を確認（最初の100文字）
                string preview = chart != null ? chart.text.Substring(0, Mathf.Min(200, chart.text.Length)) : "N/A";
                Debug.Log($"[ChkDur2] {labels[a]}[{i}] Audio={audioLen:F2}s  Chart={chartName}");
                Debug.Log($"  JSON preview: {preview}");
            }
        }
    }
}
#endif
