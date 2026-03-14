using UnityEditor;
using UnityEngine;

public class GenerateSkyTextures
{
    [MenuItem("Tools/GenerateSkyTextures")]
    static void Generate()
    {
        GenerateAndAssign("SkyGrad_Dusk",  BuildDuskGradient());
        GenerateAndAssign("SkyGrad_Night", BuildNightGradient());
        AssetDatabase.SaveAssets();
        Debug.Log("Sky textures generated!");
    }

    static Texture2D BuildDuskGradient()
    {
        // オレンジ系の夕焼け（ピンクなし）
        var stops = new (float t, Color c)[]
        {
            (0.00f, new Color(1.00f, 0.80f, 0.40f)), // 地平線: 明るいオレンジ
            (0.30f, new Color(1.00f, 0.55f, 0.10f)), // 深いオレンジ
            (0.60f, new Color(0.85f, 0.38f, 0.05f)), // 赤オレンジ
            (0.80f, new Color(0.55f, 0.28f, 0.40f)), // 赤みの暗紫（移行）
            (1.00f, new Color(0.30f, 0.25f, 0.65f)), // 天頂: 青紫
        };
        return BuildTex("SkyGrad_Dusk", stops);
    }

    static Texture2D BuildNightGradient()
    {
        var stops = new (float t, Color c)[]
        {
            (0.00f, new Color(0.10f, 0.12f, 0.28f)),
            (0.50f, new Color(0.07f, 0.09f, 0.22f)),
            (1.00f, new Color(0.03f, 0.04f, 0.15f)),
        };
        return BuildTex("SkyGrad_Night", stops);
    }

    static Texture2D BuildTex(string texName, (float t, Color c)[] stops)
    {
        int h = 128;
        var tex = new Texture2D(1, h, TextureFormat.RGB24, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            tex.SetPixel(0, y, EvalGradient(stops, t));
        }
        tex.Apply();

        string path = $"Assets/Textures/{texName}.png";
        System.IO.Directory.CreateDirectory("Assets/Textures");
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        Object.DestroyImmediate(tex);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Color EvalGradient((float t, Color c)[] stops, float t)
    {
        if (t <= stops[0].t) return stops[0].c;
        if (t >= stops[stops.Length - 1].t) return stops[stops.Length - 1].c;
        for (int i = 0; i < stops.Length - 1; i++)
        {
            if (t <= stops[i + 1].t)
            {
                float s = Mathf.InverseLerp(stops[i].t, stops[i + 1].t, t);
                return Color.Lerp(stops[i].c, stops[i + 1].c, s);
            }
        }
        return Color.black;
    }

    static void GenerateAndAssign(string texName, Texture2D tex)
    {
        if (tex == null) return;
        string matPath = $"Assets/Material/Mat_{texName}.mat";
        var srcMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Mat_SkyGrad.mat");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(srcMat);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);
        Debug.Log($"Saved {matPath}");
    }
}
