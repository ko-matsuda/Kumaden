using UnityEditor;
using UnityEngine;

public class InspectNoteDistribution
{
    [MenuItem("Tools/InspectNoteDistribution")]
    static void Inspect()
    {
        var ta = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Charts/Evening/bears_adventure_chart_kumaden.json");
        if (ta == null) { Debug.Log("NOT FOUND"); return; }
        var data = JsonUtility.FromJson<ChartData>(ta.text);
        float lastBeat = 0;
        foreach (var n in data.notes) if (n.beat > lastBeat) lastBeat = n.beat;
        for (int i = 0; i < 10; i++)
        {
            float s = (lastBeat * i / 10) * (60f / data.bpm);
            float e = (lastBeat * (i+1) / 10) * (60f / data.bpm);
            int c = 0;
            foreach (var n in data.notes)
                if (n.beat >= lastBeat * i / 10 && n.beat < lastBeat * (i+1) / 10) c++;
            Debug.Log($"[{i*10}-{(i+1)*10}%] {s:F1}s-{e:F1}s : {c}notes");
        }
    }
}
