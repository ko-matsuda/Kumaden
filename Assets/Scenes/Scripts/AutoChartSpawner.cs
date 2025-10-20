using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AutoChartSpawner（停止を“秒”で固定／Inspectorから微調整）
/// - preRoll中は生成しない（Conductor.songPositionBeats < 0）
/// - 停止は clip.length を基準に「秒」でハード決定（BPMやpreRollの影響を受けない）
/// - 左=Milk / 中=Flour / 右=Egg 固定
/// - 終盤の“余計な1〜2個”は extraEarlyMarginSec を Inspector で微調整
/// </summary>
public class AutoChartSpawner : MonoBehaviour
{
    [Header("参照")]
    public Conductor conductor;
    public Transform spawnRoot;
    public AudioSource musicSource;

    [Header("ノーツプレハブ（Milk/Flour/Egg の順）")]
    public NoteBehaviour[] notePrefabs;

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
    [Tooltip("曲の末尾からこの秒数は生成禁止（travelTimeと余白を足した上で適用）")]
    public float endGuardSeconds = 1.0f;

    [Header("終端停止の微調整")]
    [Tooltip("さらに早めに止めたいときの“上乗せ秒”。0.5〜1.8あたりで調整。")]
    [SerializeField, Range(0f, 3f)] private float extraEarlyMarginSec = 1.3f;

    private System.Random rng;
    private float nextSpawnBeat;
    private float[] nextLaneBeat;

    // —— 実行時計算（Inspectorは増やさない）——
    private bool  spawningStopped;
    private bool  hardStopReady;
    private float hardStopTimeSec;          // これ以降は一切生成しない（秒）

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
        TryComputeHardStopTime(); // クリップが入っていればここで確定
    }

    private void Update()
    {
        if (conductor == null || spawningStopped) return;

        float songBeat = conductor.songPositionBeats;        // preRoll 済み（曲頭=0拍）
        float step     = 1f / Mathf.Max(1, subdivision);

        // preRoll中は生成しない
        if (songBeat < 0f) return;

        // まだ clip が未設定の場合は遅延計算
        if (!hardStopReady) TryComputeHardStopTime();

        // ★ “秒”でのハード停止：ズレなし・安定
        if (hardStopReady && musicSource != null && musicSource.time >= hardStopTimeSec && musicSource.time > 0.1f)
        {
            spawningStopped = true;
            return;
        }

        // Catch-up（停止中に拍が進んでいたらスケジュール追従）
        if (songBeat - nextSpawnBeat > catchupClampBeats)
        {
            nextSpawnBeat = songBeat;
            for (int i = 0; i < nextLaneBeat.Length; i++) nextLaneBeat[i] = songBeat;
        }

        // 生成ループ
        int spawned = 0;
        while (songBeat >= nextSpawnBeat && spawned < Mathf.Max(1, maxSpawnsPerFrame))
        {
            TrySpawnAtBeat(nextSpawnBeat);
            nextSpawnBeat += step;
            spawned++;
        }
    }

    private void TryComputeHardStopTime()
    {
        if (hardStopReady) return;
        if (musicSource == null || musicSource.clip == null) return;

        // 生成→判定ラインまでの“移動時間”(秒)
        float travelTimeSec = Mathf.Abs(spawnZ - judgeZ) / Mathf.Max(0.01f, scrollSpeed);

        // 停止マージン（秒）＝ 終端ガード + 移動時間 + 早め余白
        float guardSeconds = Mathf.Max(0f, endGuardSeconds) + travelTimeSec + Mathf.Max(0f, extraEarlyMarginSec);

        // clipの総秒からマージンを引いた地点を“絶対停止秒”として固定
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

    private void SpawnOne(int lane)
    {
        if (notePrefabs == null || notePrefabs.Length == 0) return;

        // 左=Milk / 中=Flour / 右=Egg 固定
        int index = Mathf.Clamp(lane, 0, notePrefabs.Length - 1);
        var prefab = notePrefabs[index];
        if (prefab == null) return;

        Vector3 pos = new Vector3(laneX[lane], laneY, spawnZ);
        var note = Instantiate(prefab, pos, Quaternion.identity, spawnRoot);
        note.Init(scrollSpeed, judgeZ, lingerDistance, lane, prefab.Type);
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
    }
}
