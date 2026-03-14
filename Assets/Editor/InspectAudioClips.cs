using UnityEditor;
using UnityEngine;

public class InspectAudioClips
{
    [MenuItem("Tools/InspectAudioClips")]
    static void Inspect()
    {
        string[] names = {
            "BGM_Day_Odyssey_01", "BGM_Day_Pancakes_02",
            "BGM_Evening_Cafe_01", "BGM_Evening_Adventure_02",
            "BGM_Night_Dream_01", "BGM_Night_Dream_02"
        };
        foreach (var n in names)
        {
            var guids = AssetDatabase.FindAssets($"t:AudioClip {n}");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                    Debug.Log($"{clip.name}: length={clip.length:F2}s, freq={clip.frequency}, ch={clip.channels}");
            }
        }
    }
}
