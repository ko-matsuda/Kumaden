using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// JSON譜面ファイルからノーツを生成するスポナー
/// </summary>
public class ChartSpawner : MonoBehaviour
{
    [Header("参照")]
    public Conductor conductor;
    public Transform spawnRoot;
    public AudioSource musicSource;

    [Header("ノーツプレハブ（Milk/Flour/Egg の順）")]
    public NoteBehaviour[] notePrefabs;

    [Header("ホールドノート（Milk/Flour/Egg の順）")]
    public GameObject[] linkedHoldNotePrefabs;

    [Header("譜面データ")]
    public TextAsset chartFile;

    [Header("レーン位置")]
    public float[] laneX = new float[] { -1.6f, 0f, +1.6f };
    public float laneY = 1.0f;

    [Header("生成/移動")]
    public float spawnZ = 30f;
    public float judgeZ = 1.5f;
    public float scrollSpeed = 12f;
    public float lingerDistance = 3f;

    [Header("先行生成（拍数）")]
    public float spawnAheadBeats = 4f;

    // 譜面データ
    private ChartData chartData;
    private int nextNoteIndex = 0;
    private bool chartLoaded = false;

    [System.Serializable]
    public class NoteData
    {
        public float beat;      // 何拍目に出現するか
        public int lane;        // 0=Milk(左), 1=Flour(中), 2=Egg(右)
        public float duration;      // ホールドノートの長さ（拍数）、0なら通常ノート
    }

    [System.Serializable]
    public class ChartData
    {
        public float bpm;
        public NoteData[] notes;
    }

    void Awake()
    {
        if (spawnRoot == null) spawnRoot = transform;
    }

    void Start()
    {
        LoadChart();
        TryAutoMatchJudgeZToPlayer();
    }

    void Update()
    {
        if (!chartLoaded || conductor == null) return;
        if (musicSource == null || !musicSource.isPlaying) return;

        float currentBeat = conductor.songPositionBeats;
        float spawnBeat = currentBeat + spawnAheadBeats;

        // 次に生成すべきノーツをチェック
        while (nextNoteIndex < chartData.notes.Length)
        {
            NoteData note = chartData.notes[nextNoteIndex];
            
            if (note.beat <= spawnBeat)
            {
                SpawnNote(note);
                nextNoteIndex++;
            }
            else
            {
                break;
            }
        }
    }

    void LoadChart()
    {
        if (chartFile == null)
        {
            Debug.LogWarning("[ChartSpawner] 譜面ファイルが設定されていません");
            return;
        }

        try
        {
            chartData = JsonUtility.FromJson<ChartData>(chartFile.text);
            
            if (chartData != null && chartData.notes != null)
            {
                // 拍数でソート
                System.Array.Sort(chartData.notes, (a, b) => a.beat.CompareTo(b.beat));
                chartLoaded = true;
                Debug.Log($"[ChartSpawner] 譜面読み込み完了: {chartData.notes.Length}ノーツ, BPM={chartData.bpm}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ChartSpawner] 譜面の読み込みに失敗: {e.Message}");
        }
    }

        void SpawnNote(NoteData noteData)
    {
        int lane = Mathf.Clamp(noteData.lane, 0, laneX.Length - 1);

        if (noteData.duration > 0)
        {
            SpawnHoldNote(lane, noteData.duration);
        }
        else
        {
            SpawnNormalNote(lane);
        }
    }

    void SpawnNormalNote(int lane)
    {
        if (notePrefabs == null || notePrefabs.Length == 0) return;

        int index = Mathf.Clamp(lane, 0, notePrefabs.Length - 1);
        var prefab = notePrefabs[index];
        if (prefab == null) return;

        Vector3 pos = new Vector3(laneX[lane], laneY, spawnZ);
        var note = Instantiate(prefab, pos, Quaternion.identity, spawnRoot);
        note.Init(scrollSpeed, judgeZ, lingerDistance, lane, prefab.Type);
    }

void SpawnHoldNote(int lane, float durationBeats)
    {
        if (linkedHoldNotePrefabs == null || linkedHoldNotePrefabs.Length == 0) return;

        int prefabIndex = Mathf.Clamp(lane, 0, linkedHoldNotePrefabs.Length - 1);
        var holdPrefab = linkedHoldNotePrefabs[prefabIndex];
        if (holdPrefab == null) return;

        // BPMから拍数を秒に変換
        float bps = conductor.bpm / 60f;
        float durationSec = durationBeats / bps;
        float distanceZ = durationSec * scrollSpeed;

        Vector3 holdPos = new Vector3(laneX[lane], laneY, spawnZ);
        
        var holdObj = Instantiate(holdPrefab, holdPos, Quaternion.identity, spawnRoot);

        // StartNote の位置を設定
        Transform startNote = holdObj.transform.Find("StartNote");
        if (startNote != null)
        {
            startNote.localPosition = new Vector3(0, 0, 0);
        }

        // コンポーネント設定
        var linkedHoldNoteComponent = holdObj.GetComponent<LinkedHoldNote>();
        var holdTickPulseComponent = holdObj.GetComponent<HoldTickPulse>();
        
        if (linkedHoldNoteComponent != null)
        {
            linkedHoldNoteComponent.scrollSpeed = scrollSpeed;
            linkedHoldNoteComponent.laneIndex = lane;
            // 新しいメソッドでEndNoteの位置を設定
            linkedHoldNoteComponent.SetEndNoteDistance(distanceZ);
        }
        
        if (holdTickPulseComponent != null)
        {
            holdTickPulseComponent.laneIndex = lane;
        }
        
        var player = GameObject.Find("Player");
        
        if (player != null)
        {
            var pickup = player.GetComponent<Pickup>();
            if (pickup != null)
            {
                pickup.linkedHoldNote = linkedHoldNoteComponent;
                pickup.holdTickPulse = holdTickPulseComponent;
            }
        }

        Debug.Log($"[ChartSpawner] Spawned HoldNote at lane {lane}, duration {durationBeats} beats, distanceZ={distanceZ:F2}, durationSec={durationSec:F2}");
    }

    void TryAutoMatchJudgeZToPlayer()
    {
        var player = GameObject.FindWithTag("Player");
        if (!player) return;
        var box = player.GetComponent<BoxCollider>();
        if (!box) return;

        float playerZ = player.transform.TransformPoint(box.center).z;
        judgeZ = playerZ;
    }

    // 外部からリセット用
    public void ResetChart()
    {
        nextNoteIndex = 0;
    }

    /// <summary>
    /// ゲーム開始時点でのBGMオフセット（拍数）
    /// プロローグ動画中にBGMが先行開始するため
    /// </summary>
    public float GetBgmOffsetBeats()
    {
        // bgmLeadTime = 3.9秒、BPM = 163
        // 3.9秒 × (163/60) = 約10.6拍
        float bps = conductor != null ? conductor.bpm / 60f : 163f / 60f;
        return 3.9f * bps;
    }
}