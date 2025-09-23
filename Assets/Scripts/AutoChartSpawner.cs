using UnityEngine;

/// <summary>
/// とても簡単な自動譜面生成（カジュアル向けプリセット）
/// </summary>
public class AutoChartSpawner : MonoBehaviour
{
    [Header("参照")]
    public Conductor conductor;       // Conductor をドラッグ
    public Transform spawnRoot;       // 生成親
    public AudioSource musicSource;   // BGM（ConductorのAudioSourceでもOK）

    [Header("ノーツプレハブ（Milk/Flour/Egg の順など）")]
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
    public int subdivision = 2;           // 1=四分, 2=八分, 4=16分相当
    public float firstSpawnBeat = 0f;

    [Header("難易度（重要）")]
    [Range(0f, 1f)] public float density = 0.30f; // 低いほど簡単
    public int   maxSimultaneousLanes = 1;        // 1 で同時は出さない
    public float minLaneGapBeats = 1.5f;          // 同じレーンでの最小間隔
    public float minGlobalGapBeats = 0.5f;        // 全体の最小間隔

    [Header("ランダム")]
    public int fixedRandomSeed = 12345;

    private System.Random rng;
    private float nextSpawnBeat;
    private float[] nextLaneBeat; // 各レーンの次に使える拍

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

        // judgeZ を Player のコライダー中心に合わせたい場合（任意）
        TryAutoMatchJudgeZToPlayer();
    }

    private void Update()
    {
        if (conductor == null) return;

        float songBeat = conductor.songPositionBeats;
        float step = 1f / Mathf.Max(1, subdivision);

        while (songBeat >= nextSpawnBeat)
        {
            TrySpawnAtBeat(nextSpawnBeat);
            nextSpawnBeat += step;
        }
    }

    private void TrySpawnAtBeat(float beat)
    {
        // 全体の密度（確率）
        if (rng.NextDouble() > density) return;

        // 使えるレーンを集める（最小間隔を満たす）
        var candidates = new System.Collections.Generic.List<int>();
        for (int lane = 0; lane < laneX.Length; lane++)
        {
            if (beat >= nextLaneBeat[lane]) candidates.Add(lane);
        }
        if (candidates.Count == 0) return;

        // 同時数を制限
        int spawnCount = Mathf.Min(maxSimultaneousLanes, candidates.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            int idx = rng.Next(candidates.Count);
            int lane = candidates[idx];
            candidates.RemoveAt(idx);

            SpawnOne(lane);
            nextLaneBeat[lane] = beat + minLaneGapBeats;
        }

        // 全体間隔
        nextSpawnBeat += minGlobalGapBeats;
    }

    private void SpawnOne(int lane)
    {
        if (notePrefabs == null || notePrefabs.Length == 0) return;

        // 種別をランダム
        var prefab = notePrefabs[rng.Next(notePrefabs.Length)];
        if (prefab == null) return;

        Vector3 pos = new Vector3(laneX[lane], laneY, spawnZ);
        var note = Instantiate(prefab, pos, Quaternion.identity, spawnRoot);

        // Init(速度, 判定Z, 残留距離, レーン, 種別)
        note.Init(scrollSpeed, judgeZ, lingerDistance, lane, prefab.Type);
    }

    private void TryAutoMatchJudgeZToPlayer()
    {
        // シーン内の Player を探し、BoxCollider の中心Z を judgeZ に合わせる（任意）
        var player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var box = player.GetComponent<BoxCollider>();
        if (box == null) return;

        // ワールドZに換算
        float playerZ = player.transform.TransformPoint(box.center).z;
        judgeZ = playerZ;
        // Debug.Log($"[Spawner] Auto match judgeZ = {judgeZ:F2}");
    }
}
