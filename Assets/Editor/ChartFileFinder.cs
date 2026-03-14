
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class ChartFileFinder
{
    [MenuItem("KumaDen/Find All Chart JSON Files")]
    static void FindAll()
    {
        // Search entire Assets for json files with "chart" in name
        var allJson = Directory.GetFiles(Application.dataPath, "*.json", SearchOption.AllDirectories);
        Debug.Log($"[CFinder] Total json files in Assets: {allJson.Length}");
        foreach (var f in allJson.OrderBy(x => x))
        {
            string rel = f.Replace(Application.dataPath, "Assets").Replace("\\", "/");
            Debug.Log($"[CFinder] {rel}");
        }
    }
}
#endif
