
using UnityEngine;
using UnityEditor;

public class ConductorChartInspect
{
    [MenuItem("KumaDen/Inspect Conductor and ChartSpawner")]
    static void Run()
    {
        // Conductor
        var conductorGO = GameObject.FindObjectOfType<Conductor>();
        if (conductorGO != null)
        {
            var so = new SerializedObject(conductorGO);
            var preRoll = so.FindProperty("preRollBeats");
            var bpm     = so.FindProperty("bpm");
            var delay   = so.FindProperty("startDelaySec");
            Debug.Log($"[Inspect] Conductor: bpm={bpm?.floatValue}  preRollBeats={preRoll?.floatValue}  startDelaySec={delay?.floatValue}");
        }
        else Debug.LogWarning("[Inspect] Conductor not found");

        // ChartSpawner
        var spawnerGO = GameObject.FindObjectOfType<ChartSpawner>();
        if (spawnerGO != null)
        {
            var so = new SerializedObject(spawnerGO);
            var ahead = so.FindProperty("spawnAheadBeats");
            Debug.Log($"[Inspect] ChartSpawner: spawnAheadBeats={ahead?.floatValue}");
        }
        else Debug.LogWarning("[Inspect] ChartSpawner not found");

        // SongLoopController transition trigger summary
        // Also compute last note hitTime using real preRollBeats from Inspector
        var cond = GameObject.FindObjectOfType<Conductor>();
        if (cond == null) return;
        var cso = new SerializedObject(cond);
        float realPreRoll = cso.FindProperty("preRollBeats")?.floatValue ?? 4f;

        string[] chartPaths = {
            Application.dataPath + "/Resources/Charts/Evening/kuma_odyssey_chart_kumaden_normal.json",
        };
        foreach (var p in chartPaths)
        {
            if (!System.IO.File.Exists(p)) continue;
            var chart = JsonUtility.FromJson<ChartData>(System.IO.File.ReadAllText(p));
            if (chart?.notes == null) continue;
            float sPerBeat = 60f / chart.bpm;
            var last = chart.notes[chart.notes.Length - 1];
            float hitSec = (last.beat + realPreRoll) * sPerBeat;
            Debug.Log($"[Inspect] {System.IO.Path.GetFileNameWithoutExtension(p)}: lastBeat={last.beat}  hitTime(preRoll={realPreRoll})={hitSec:F2}s");
        }
    }
}
