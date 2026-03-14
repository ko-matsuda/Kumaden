using UnityEditor;
using UnityEngine;
using System.IO;

public class RedistributeNotes
{
    const float MARGIN = 2.0f;      // 曲末尾の余裕（秒）
    const float INTRO_SEC = 3.0f;   // 最初のノーツまでの余裕（秒）

    [MenuItem("Tools/RedistributeNormalB")]
    static void Run()
    {
        string assetPath = "Assets/Resources/Charts/Evening/bears_adventure_chart_kumaden.json";
        float songLength = 60.05f;

        var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        if (ta == null) { Debug.LogError("NOT FOUND: " + assetPath); return; }

        var data = JsonUtility.FromJson<ChartData>(ta.text);
        int noteCount = data.notes.Length;
        float bpm = data.bpm;

        // 利用可能な秒数
        float usableSec = songLength - MARGIN - INTRO_SEC;
        // ノーツ間隔（秒）
        float intervalSec = usableSec / (noteCount - 1);

        // ビート単位に変換
        float secPerBeat = 60f / bpm;

        for (int i = 0; i < noteCount; i++)
        {
            float targetSec = INTRO_SEC + i * intervalSec;
            data.notes[i].beat = targetSec / secPerBeat;
            // holdDuration/durationもスケール（元々あれば）
            // ノート間隔の1/3程度に収める
            if (data.notes[i].duration > 0)
                data.notes[i].duration = Mathf.Min(data.notes[i].duration, intervalSec * 0.5f / secPerBeat);
            if (data.notes[i].holdDuration > 0)
                data.notes[i].holdDuration = Mathf.Min(data.notes[i].holdDuration, intervalSec * 0.5f / secPerBeat);
        }

        float lastSec = data.notes[noteCount - 1].beat * secPerBeat;
        Debug.Log($"[Redistribute] {noteCount} notes spread over {INTRO_SEC:F1}s - {lastSec:F1}s (interval={intervalSec:F2}s, song={songLength}s)");

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(AssetDatabase.GetAssetPath(ta), json);
        AssetDatabase.ImportAsset(assetPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Redistribute] Done!");
    }
}
