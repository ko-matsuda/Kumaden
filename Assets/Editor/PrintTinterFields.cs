using UnityEditor;
using UnityEngine;
using System.Reflection;

public class PrintTinterFields
{
    [MenuItem("Tools/PrintTinterFields")]
    static void Print()
    {
        var go = GameObject.Find("GameFlow");
        if (go == null) { Debug.Log("GameFlow not found"); return; }
        var comps = go.GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            if (c == null) continue;
            var t = c.GetType();
            if (!t.Name.Contains("Background")) continue;
            Debug.Log("=== " + t.Name + " ===");
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                Debug.Log(f.Name + " = " + f.GetValue(c));
        }
    }
}
