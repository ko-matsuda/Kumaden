using UnityEditor;
using UnityEngine;

public class PrintSkyMat
{
    [MenuItem("Tools/PrintSkyMat")]
    static void Print()
    {
        var go = GameObject.Find("SkyQuad");
        if (go == null) { Debug.Log("SkyQuad not found"); return; }
        var r = go.GetComponent<Renderer>();
        if (r == null) { Debug.Log("No renderer"); return; }
        foreach (var m in r.sharedMaterials)
        {
            if (m == null) continue;
            Debug.Log($"Mat: {m.name} | Shader: {m.shader.name} | Path: {AssetDatabase.GetAssetPath(m)}");
            foreach (var prop in MaterialEditor.GetMaterialProperties(new Object[]{m}))
                Debug.Log($"  {prop.name} ({prop.propertyType}) = {prop.colorValue} / {prop.floatValue} / {prop.vectorValue}");
        }
    }
}
