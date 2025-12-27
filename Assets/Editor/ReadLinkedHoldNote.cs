using UnityEngine;
using UnityEditor;
using System.IO;

public class ReadLinkedHoldNote
{
    [MenuItem("Tools/Read LinkedHoldNote Script")]
    static void Read()
    {
        string path = "Assets/Scenes/Scripts/LinkedHoldNote.cs";
        if (File.Exists(path))
        {
            string content = File.ReadAllText(path);
            Debug.Log("=== LinkedHoldNote.cs START ===");
            Debug.Log(content);
            Debug.Log("=== LinkedHoldNote.cs END ===");
        }
        else
        {
            Debug.LogError("LinkedHoldNote.cs not found!");
        }
    }
}
