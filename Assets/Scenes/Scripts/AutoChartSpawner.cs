using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AutoChartSpawner（ホールドノート自動生成対応）
/// </summary>
public class AutoChartSpawner : MonoBehaviour
{
    [Header("参照")]
    public Conductor conductor;
    public Transform spawnRoot;
    public AudioSource musicSource;

    [Header("ノーツプレハブ（Milk/Flour/Egg の順）")]
    public NoteBehaviour[] notePrefabs;

    [Header("ホールドノート（Milk/Flour/Egg の順）")]
    public GameObject[] linkedHoldNotePrefabs;
    [Range(0f, 1f)] public float holdNoteProbability = 0.1f;
    [Tooltip("ホールドノートの長さ（拍数）")]
    public float holdNoteDurationBeats = 4f;

    [Header("レーン位置")]
    public float[] laneX = new float[] { -1.6f, 0f, +1.6f };
    public float laneY = 0.7f;

    [Header("生成/移動")]
    public float spawnZ = 30f;
    public float judgeZ = 0f;
    public float scrollSpeed = 12f;
    public float lingerDistance = 3f;

    [Header("リズム設定")]
    public int subdivision = 2;
    public float firstSpawnBeat = 0f;

    [Header("難易度（出現頻度）")]
    [Range(0f, 1f)] public float density = 0.30f;
    public int   maxSimultaneousLanes = 1;
    public float minLaneGapBeats = 1.5f;
    public float minGlobalGapBeats = 0.5f;

    [Header("ランダム")]
    public int fixedRandomSeed = 12345;

    [Header("まとめ湧き対策")]
    public bool  limitSpawnsPerFrame = true;
    public int   maxSpawnsPerFrame   = 1;
    public float catchupClampBeats   = 2f;

    [Header("終端ガード（秒ベースの安全マージン）")]
    [Tooltip("曲の末尾からこの秒数は生成禁止")]
    public float endGuardSeconds = 1.0f;

    [Header("終端停止の微調整")]
    [SerializeField, Range(0f, 3f)] private float extraEarlyMarginSec = 1.3f;

    private System.Random rng;
    private float nextSpawnBeat;
    private float[] nextLaneBeat;

    private bool  spawningStopped;
    private bool  hardStopReady;
    private float hardStopTimeSec;

    // ホールドノート制御
    private bool isHoldNoteActive = false;
    private float holdNoteEndBeat = 0f;

    private void Awake()
    {
        if (spawnRoot == null) spawnRoot = transform;
    }

    private void Start()
    {
        rng = new System.Random(fixedRandomSeed);
        nextSpawnBeat = firstSpawnBeat;

        nextLaneBeat = new float[laneX.Length];
        for (int i = 0; i < nextLaneBeat.Length; i++) nextLaneBeat[i] = firstSpawnBeat;

        TryAutoMatchJudgeZToPlayer();
        TryComputeHardStopTime();
    }

    private void Update()
    {
        if (conductor == null || spawningStopped) return;
        
        // 音楽が再生されていなければスポーンしない
        if (musicSource == null || !musicSource.isPlaying) return;

        float songBeat = conductor.songPositionBeats;
        float step     = 1f / Mathf.Max(1, subdivision);

        if (songBeat < 0f) return;

        if (!hardStopReady) TryComputeHardStopTime();

        if (hardStopReady && musicSource != null && musicSource.time >= hardStopTimeSec && musicSource.time > 0.1f)
        {
            spawningStopped = true;
            return;
        }

        // ホールドノート終了チェック
        if (isHoldNoteActive && songBeat >= holdNoteEndBeat)
        {
            isHoldNoteActive = false;
        }

        if (songBeat - nextSpawnBeat > catchupClampBeats)
        {
            nextSpawnBeat = songBeat;
            for (int i = 0; i < nextLaneBeat.Length; i++) nextLaneBeat[i] = songBeat;
        }

        int spawned = 0;
        while (songBeat >= nextSpawnBeat && spawned < Mathf.Max(1, maxSpawnsPerFrame))
        {
            // ホールドノート中は生成しない
            if (!isHoldNoteActive)
            {
                TrySpawnAtBeat(nextSpawnBeat);
            }
            nextSpawnBeat += step;
            spawned++;
        }
    }

    private void TryComputeHardStopTime()
    {
        if (hardStopReady) return;
        if (musicSource == null || musicSource.clip == null) return;

        float travelTimeSec = Mathf.Abs(spawnZ - judgeZ) / Mathf.Max(0.01f, scrollSpeed);
        float guardSeconds = Mathf.Max(0f, endGuardSeconds) + travelTimeSec + Mathf.Max(0f, extraEarlyMarginSec);
        hardStopTimeSec = Mathf.Max(0f, musicSource.clip.length - guardSeconds);
        hardStopReady = true;
    }

    private void TrySpawnAtBeat(float beat)
    {
        if (rng.NextDouble() > density) return;

        var candidates = new List<int>();
        for (int lane = 0; lane < laneX.Length; lane++)
            if (beat >= nextLaneBeat[lane]) candidates.Add(lane);
        if (candidates.Count == 0) return;

        // ホールドノートを生成するか判定
        bool spawnHold = linkedHoldNotePrefabs != null && linkedHoldNotePrefabs.Length > 0 && 
                         (float)rng.NextDouble() < holdNoteProbability;

        if (spawnHold)
        {
            // ランダムなレーンを選択
            int idx = rng.Next(candidates.Count);
            int lane = candidates[idx];

            SpawnHoldNote(lane, beat);
            
            // 全レーンをホールドノート終了まで封鎖
            for (int i = 0; i < nextLaneBeat.Length; i++)
            {
                nextLaneBeat[i] = holdNoteEndBeat + minLaneGapBeats;
            }
            nextSpawnBeat = holdNoteEndBeat + minGlobalGapBeats;
        }
        else
        {
            // 通常ノーツ生成
            int spawnCount = Mathf.Min(maxSimultaneousLanes, candidates.Count);
            for (int i = 0; i < spawnCount; i++)
            {
                int idx  = rng.Next(candidates.Count);
                int lane = candidates[idx];
                candidates.RemoveAt(idx);

                SpawnOne(lane);
                nextLaneBeat[lane] = beat + minLaneGapBeats;
            }
            nextSpawnBeat += minGlobalGapBeats;
        }
    }

    private void SpawnOne(int lane)
    {
        if (notePrefabs == null || notePrefabs.Length == 0) return;

        int index = Mathf.Clamp(lane, 0, notePrefabs.Length - 1);
        var prefab = notePrefabs[index];
        if (prefab == null) return;

        Vector3 pos = new Vector3(laneX[lane], laneY, spawnZ);
        var note = Instantiate(prefab, pos, Quaternion.identity, spawnRoot);
        note.Init(scrollSpeed, judgeZ, lingerDistance, lane, prefab.Type);
    }

private void SpawnHoldNote(int lane, float startBeat)
    {
        if (linkedHoldNotePrefabs == null || linkedHoldNotePrefabs.Length == 0) return;

        // レーンに対応したプレハブを選択
        int prefabIndex = Mathf.Clamp(lane, 0, linkedHoldNotePrefabs.Length - 1);
        var holdPrefab = linkedHoldNotePrefabs[prefabIndex];
        if (holdPrefab == null) return;

        // BPMから拍数を秒に変換
        float bps = conductor.bpm / 60f;
        float durationSec = holdNoteDurationBeats / bps;
        float distanceZ = durationSec * scrollSpeed;

        // StartNote の位置
        float startZ = spawnZ;

        Vector3 holdPos = new Vector3(laneX[lane], laneY, startZ);
        
        // LinkedHoldNote をインスタンス化
        var holdObj = Instantiate(holdPrefab, holdPos, Quaternion.identity, spawnRoot);

        // StartNote と EndNote の位置を設定
        Transform startNote = holdObj.transform.Find("StartNote");
        Transform endNote = holdObj.transform.Find("EndNote");

        if (startNote != null)
        {
            startNote.localPosition = new Vector3(0, 0, 0);
        }
        if (endNote != null)
        {
            endNote.localPosition = new Vector3(0, 0, distanceZ);
        }

        // LinkedHoldNote コンポーネントを取得して Player の Pickup に設定
        var linkedHoldNoteComponent = holdObj.GetComponent<LinkedHoldNote>();
        var holdTickPulseComponent = holdObj.GetComponent<HoldTickPulse>();
        
        // scrollSpeed を設定
        if (linkedHoldNoteComponent != null)
        {
            linkedHoldNoteComponent.scrollSpeed = scrollSpeed;
        }
        
        // レーン情報を設定
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

        // HoldTickPulse の Events を設定
        if (holdTickPulseComponent != null)
        {
            var hudCounterBinder = FindObjectOfType<HudCounterBinder>();
            if (hudCounterBinder != null)
            {
                holdTickPulseComponent.OnTick.AddListener(hudCounterBinder.OnHoldTick);
                holdTickPulseComponent.OnEnter.AddListener(hudCounterBinder.OnHoldEnter);
                holdTickPulseComponent.OnExit.AddListener(hudCounterBinder.OnHoldExit);
            }

            var comboProbe = FindObjectOfType<ComboProbe>();
            if (comboProbe != null)
            {
                holdTickPulseComponent.OnTick.AddListener(comboProbe.OnHoldTick);
                holdTickPulseComponent.OnEnter.AddListener(comboProbe.OnHoldEnter);
                holdTickPulseComponent.OnExit.AddListener(comboProbe.OnHoldExit);
            }

            Debug.Log($"[AutoChartSpawner] HoldTickPulse events configured");
        }

        // ホールドノート状態を設定
        isHoldNoteActive = true;
        holdNoteEndBeat = startBeat + holdNoteDurationBeats;

        Debug.Log($"[AutoChartSpawner] Spawned HoldNote at lane {lane}, duration {holdNoteDurationBeats} beats");
    }

    private void TryAutoMatchJudgeZToPlayer()
    {
        var player = GameObject.FindWithTag("Player");
        if (!player) return;
        var box = player.GetComponent<BoxCollider>();
        if (!box) return;

        float playerZ = player.transform.TransformPoint(box.center).z;
        judgeZ = playerZ;
    }

    public void ResetScheduleBeats(float extraDelayBeats = 0.75f)
    {
        float nowBeat = (conductor != null) ? conductor.songPositionBeats : 0f;
        nextSpawnBeat = nowBeat + Mathf.Max(0f, extraDelayBeats) + Mathf.Max(0f, minGlobalGapBeats);

        if (nextLaneBeat == null || nextLaneBeat.Length == 0) nextLaneBeat = new float[laneX.Length];
        for (int i = 0; i < nextLaneBeat.Length; i++) nextLaneBeat[i] = nextSpawnBeat;

        isHoldNoteActive = false;
    }
}
