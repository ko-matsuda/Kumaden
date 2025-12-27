using UnityEngine;
using UnityEditor;
using System.IO;

public class ReadHoldTickPulse
{
    [MenuItem("Tools/Read HoldTickPulse Script")]
    static void Read()
    {
        string path = "Assets/Scenes/Scripts/UI/HoldTickPulse.cs";
        if (File.Exists(path))
        {
            string content = File.ReadAllText(path);
            Debug.Log("=== HoldTickPulse.cs START ===");
            Debug.Log(content);
            Debug.Log("=== HoldTickPulse.cs END ===");
        }
        else
        {
            Debug.LogError("HoldTickPulse.cs not found!");
        }
    }
}
