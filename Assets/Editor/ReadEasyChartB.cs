using UnityEngine;
using UnityEditor;

public class ReadEasyChartB
{
    [MenuItem("Tools/ReadEasyChartB")]
public static void Run()
    {
        string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[]{"Assets/Resources/Charts"});
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("easy")) continue;
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (ta == null) continue;
            string text = ta.text;
            int offsetIdx = text.IndexOf("\"offset\"");
            int notesIdx  = text.IndexOf("\"notes\"");
            string header = offsetIdx >= 0 ? text.Substring(offsetIdx, Mathf.Min(60, text.Length - offsetIdx)) : "?";
            string notesPreview = notesIdx >= 0 ? text.Substring(notesIdx, Mathf.Min(400, text.Length - notesIdx)) : "?";
            Debug.Log("[CI_PATH] " + path);
            Debug.Log("[CI_OFFSET] " + header);
            Debug.Log("[CI_NOTES] " + notesPreview);
        }
    }
}
