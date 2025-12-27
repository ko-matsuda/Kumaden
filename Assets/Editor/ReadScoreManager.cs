using UnityEngine;
using UnityEditor;
using System.IO;

public class ReadScoreManager
{
    [MenuItem("Tools/Read ScoreManagerLite Script")]
    static void Read()
    {
        string path = "Assets/Scenes/Scripts/ScoreManagerLite.cs";
        if (File.Exists(path))
        {
            string content = File.ReadAllText(path);
            Debug.Log("=== ScoreManagerLite.cs START ===");
            Debug.Log(content);
            Debug.Log("=== ScoreManagerLite.cs END ===");
        }
        else
        {
            Debug.LogError("ScoreManagerLite.cs not found!");
        }
    }
}
