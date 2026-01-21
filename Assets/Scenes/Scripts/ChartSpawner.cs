using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class NoteData
{
    public float beat;
    public int lane;
    public string type;
    public bool linkedHold;
    
    public float duration;
public float holdDuration;
}

[System.Serializable]
public class ChartData
{
    public string songName;
    public int bpm;
    public float offset;
    public NoteData[] notes;
}

public class ChartSpawner : MonoBehaviour
{
    [Header("Chart")]
    [SerializeField] private TextAsset chartJson;
    
    [Header("Note Prefabs")]
    [SerializeField] private GameObject flourNotePrefab;
    [SerializeField] private GameObject milkNotePrefab;
    
    [SerializeField] private GameObject linkedHoldFlourPrefab;
    [SerializeField] private GameObject linkedHoldMilkPrefab;
    [SerializeField] private GameObject linkedHoldEggPrefab;
[SerializeField] private GameObject eggNotePrefab;
    
    [Header("Lane Positions")]
    [SerializeField] private Transform[] laneTransforms = new Transform[3];
    
    
    [Header("Difficulty (Optional)")]
    [SerializeField] private bool useDifficultyManager = true;
    
[Header("Spawn Settings")]
    [SerializeField] private float spawnAheadBeats = 8f;
    
    [Header("References")]
    [SerializeField] private Conductor conductor;
    
    public ChartData currentChart;
    private int nextNoteIndex = 0;
    private List<GameObject> spawnedNotes = new List<GameObject>();
    
void Start()
    {
        TextAsset chartToLoad = null;
        
        if (useDifficultyManager && DifficultyManager.Instance != null)
        {
            string difficultySuffix = "";
            Difficulty currentDiff = DifficultyManager.Instance.GetCurrentDifficulty();
            
            switch (currentDiff)
            {
                case Difficulty.Easy:
                    difficultySuffix = "_easy";
                    break;
                case Difficulty.Normal:
                    difficultySuffix = "";
                    break;
                case Difficulty.Hard:
                    difficultySuffix = "_hard";
                    break;
            }
            
            string baseChartName = "kuma_odyssey_chart_kumaden";
            string chartPath = "Charts/" + baseChartName + difficultySuffix;
            
            chartToLoad = Resources.Load<TextAsset>(chartPath);
            
            if (chartToLoad != null)
            {
                Debug.Log($"[ChartSpawner] Loading difficulty chart: {chartPath}");
            }
            else
            {
                Debug.LogWarning($"[ChartSpawner] Difficulty chart not found: {chartPath}, falling back to default");
            }
        }
        
        if (chartToLoad == null && chartJson != null)
        {
            chartToLoad = chartJson;
            Debug.Log($"[ChartSpawner] Loading chart from chartJson: {chartJson.name}");
        }
        
        if (chartToLoad == null)
        {
            Debug.LogWarning("[ChartSpawner] chartJson is null! Attempting to load default chart...");
            TextAsset defaultChart = Resources.Load<TextAsset>("Charts/kuma_odyssey_chart_kumaden");
            
            if (defaultChart != null)
            {
                chartToLoad = defaultChart;
            }
        }
        
        if (chartToLoad != null)
        {
            LoadChartFromJson(chartToLoad.text);
        }
        else
        {
            Debug.LogError("[ChartSpawner] No chart available! Please assign chartJson in Inspector.");
        }
    }

public void ResetForNewSong()
    {
        Debug.Log("[ChartSpawner] ResetForNewSong called");
        
        // 次のノーツインデックスをリセット
        nextNoteIndex = 0;
        
        // 生成済みノーツを削除（Destroyで次のフレームで削除）
        foreach (var note in spawnedNotes)
        {
            if (note != null)
                Destroy(note);
        }
        spawnedNotes.Clear();
        
        Debug.Log($"[ChartSpawner] Reset complete - nextNoteIndex={nextNoteIndex}, spawnedNotes cleared");
    }

    
void Update()
    {
        if (currentChart == null || conductor == null) 
        {
            return;
        }
        
        float currentBeat = conductor.songPositionBeats;
        
        if (Time.frameCount % 120 == 0 && nextNoteIndex < currentChart.notes.Length)
        {
            var nextNote = currentChart.notes[nextNoteIndex];
            float threshold = currentBeat + spawnAheadBeats;
        }
        
        while (nextNoteIndex < currentChart.notes.Length)
        {
            var noteData = currentChart.notes[nextNoteIndex];
            
            if (noteData.beat <= currentBeat + spawnAheadBeats)
            {
                bool shouldSpawn = ShouldSpawnNote(nextNoteIndex);
                
                if (shouldSpawn)
                {
                    SpawnNote(noteData);
                }
                nextNoteIndex++;
            }
            else
            {
                break;
            }
        }
    }

private bool ShouldSpawnNote(int noteIndex)
    {
        if (!useDifficultyManager || DifficultyManager.Instance == null)
            return true;
        
        float density = DifficultyManager.Instance.GetCurrentSettings().noteDensity;
        
        if (density >= 1.0f)
        {
            return true;
        }
        else
        {
            int skipPattern = Mathf.RoundToInt(1.0f / density);
            return (noteIndex % skipPattern) == 0;
        }
    }

    
public void LoadChartFromJson(string jsonText)
    {
        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogError("[ChartSpawner] JSON text is empty!");
            return;
        }
        
        ChartData chartData = JsonUtility.FromJson<ChartData>(jsonText);
        
        if (chartData == null || chartData.notes == null)
        {
            Debug.LogError("[ChartSpawner] Failed to parse chart JSON!");
            return;
        }
        
        ClearAllNotes();
        
        currentChart = chartData;
        nextNoteIndex = 0;
        
        if (conductor != null)
        {
            conductor.bpm = chartData.bpm;
        }
        
        Debug.Log($"[ChartSpawner] Loaded chart: {chartData.songName}, BPM: {chartData.bpm}, Notes: {chartData.notes.Length}");
        
        if (chartData.notes.Length > 0)
        {
            Debug.Log($"[ChartSpawner] First 15 notes:");
            for (int i = 0; i < Mathf.Min(15, chartData.notes.Length); i++)
            {
                var note = chartData.notes[i];
                Debug.Log($"  Note {i}: beat={note.beat}, lane={note.lane}, type={note.type}");
            }
        }
    }
    
    private void ClearAllNotes()
    {
        foreach (var note in spawnedNotes)
        {
            if (note != null)
            {
                Destroy(note);
            }
        }
        spawnedNotes.Clear();
        
        var holdNotes = FindObjectsOfType<LinkedHoldNote>();
        foreach (var note in holdNotes)
        {
            Destroy(note.gameObject);
        }
    }
    
private void SpawnNote(NoteData noteData)
    {
        if (noteData.lane < 0 || noteData.lane >= laneTransforms.Length)
        {
            Debug.LogWarning($"[ChartSpawner] Invalid lane: {noteData.lane}");
            return;
        }
        
        bool isHoldNote = noteData.duration > 0 || (noteData.linkedHold && noteData.holdDuration > 0);
        
        GameObject notePrefab = GetNotePrefabByLane(noteData.lane, noteData.type, isHoldNote);
        
        if (notePrefab == null)
        {
            Debug.LogWarning($"[ChartSpawner] Prefab is NULL");
            return;
        }
        
        Transform laneTransform = laneTransforms[noteData.lane];
        
        float currentBeat = conductor.songPositionBeats;
        float beatDifference = noteData.beat - currentBeat;
        
        float scrollSpeed = 4.0f;
        if (useDifficultyManager && DifficultyManager.Instance != null)
        {
            scrollSpeed *= DifficultyManager.Instance.GetCurrentSettings().speedMultiplier;
        }
        
        float spawnZ = beatDifference * conductor.secPerBeat * scrollSpeed;
        
        Vector3 spawnPos = laneTransform.position;
        spawnPos.z = spawnZ;
        
        GameObject noteObj = Instantiate(notePrefab, spawnPos, Quaternion.identity, laneTransform);
        
        if (isHoldNote)
        {
            var linkedHold = noteObj.GetComponent<LinkedHoldNote>();
            if (linkedHold != null)
            {
                float holdDuration = noteData.duration > 0 ? noteData.duration : noteData.holdDuration;
                float distanceZ = holdDuration * conductor.secPerBeat * linkedHold.scrollSpeed;
                linkedHold.SetEndNoteDistance(distanceZ);
            }
        }
        
        spawnedNotes.Add(noteObj);
    }
    
    private GameObject GetNotePrefab(string noteType)
    {
        switch (noteType)
        {
            case "Flour":
                return flourNotePrefab;
            case "Milk":
                return milkNotePrefab;
            case "Egg":
                return eggNotePrefab;
            default:
                return null;
        }
    }

private GameObject GetNotePrefabByLane(int lane, string type, bool isHoldNote = false)
    {
        if (!string.IsNullOrEmpty(type))
        {
            return GetNotePrefab(type);
        }
        
        if (isHoldNote)
        {
            switch (lane)
            {
                case 0:
                    return linkedHoldMilkPrefab;
                case 1:
                    return linkedHoldFlourPrefab;
                case 2:
                    return linkedHoldEggPrefab;
                default:
                    return null;
            }
        }
        else
        {
            switch (lane)
            {
                case 0:
                    return milkNotePrefab;
                case 1:
                    return flourNotePrefab;
                case 2:
                    return eggNotePrefab;
                default:
                    return null;
            }
        }
    }













}