using UnityEditor;
using UnityEngine;

public class InspectSongLoop
{
    [MenuItem("Tools/InspectSongLoop")]
    static void Inspect()
    {
        var go = GameObject.Find("SongLoopController");
        if (go == null) { Debug.Log("SongLoopController not found"); return; }
        var comp = go.GetComponent<SongLoopController>();
        if (comp == null) { Debug.Log("No SongLoopController component"); return; }

        var so = new UnityEditor.SerializedObject(comp);
        void PrintArray(string propName)
        {
            var arr = so.FindProperty(propName);
            if (arr == null) { Debug.Log($"{propName}: NOT FOUND"); return; }
            Debug.Log($"=== {propName} (count={arr.arraySize}) ===");
            for (int i = 0; i < arr.arraySize; i++)
            {
                var elem = arr.GetArrayElementAtIndex(i);
                var clip = elem.FindPropertyRelative("audioClip");
                var chart = elem.FindPropertyRelative("chartJson");
                string clipName = clip?.objectReferenceValue?.name ?? "NULL";
                string chartName = chart?.objectReferenceValue?.name ?? "NULL";
                Debug.Log($"  [{i}] audioClip={clipName}, chartJson={chartName}");
            }
        }
        PrintArray("songs");
        PrintArray("songsDusk");
        PrintArray("songsNight");
    }
}
