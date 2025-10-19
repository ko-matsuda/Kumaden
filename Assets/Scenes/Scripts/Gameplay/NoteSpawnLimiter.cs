using System.Linq;
using UnityEngine;

public class NoteSpawnLimiter : MonoBehaviour
{
    public enum StopMode
    {
        SecondsBeforeEnd, // 1番終了「秒」で指定
        BarsBeforeEnd     // 1番終了「小節」で指定
    }

    [Header("参照")]
    public AudioSource bgm;

    [Header("止め方のモード")]
    public StopMode mode = StopMode.SecondsBeforeEnd;

    [Header("秒ベース（SecondsBeforeEnd のとき使用）")]
    [Tooltip("1番が終わる絶対時刻（秒） 例: 37.5")]
    public float verse1EndSec = 37.5f;
    [Tooltip("ノーツが当たり位置に到達するまでの移動時間 + 安全マージン（秒） 例: 1.5")]
    public float leadSeconds = 1.5f;

    [Header("小節ベース（BarsBeforeEnd のとき使用）")]
    [Tooltip("BPM (拍/分)")]
    public float bpm = 120f;
    [Tooltip("1小節あたりの拍数（4/4なら4）")]
    public int beatsPerBar = 4;
    [Tooltip("1番は何小節？ 例: 16")]
    public int verse1Bars = 16;
    [Tooltip("曲頭の無音/オフセット（秒） 例: 0.0 ～ 0.2")]
    public float songStartOffsetSec = 0f;
    [Tooltip("終わりの何小節前から止めるか 例: 2 (＝2小節手前で停止)")]
    public int stopBarsBeforeEnd = 2;

    [Header("停止対象（ここにSpawner等を入れる）")]
    public MonoBehaviour[] targetsToDisable;

    [Header("ログ/可視化")]
    public bool debugLog = false;

    bool done;

    void Update()
    {
        if (done || !bgm) return;

        float stopTime = CalcStopTimeSec();

        // BGMの現在時間が停止閾値を越えたら、以後の生成を止める
        if (bgm.isPlaying && bgm.time >= stopTime)
        {
            if (targetsToDisable != null)
            {
                foreach (var t in targetsToDisable.Where(t => t))
                {
                    t.enabled = false; // 生成スクリプトを停止
                    if (debugLog) Debug.Log($"[NoteSpawnLimiter] Disabled: {t.name}");
                }
            }
            done = true;
            if (debugLog) Debug.Log($"[NoteSpawnLimiter] Stopped further spawns at t={bgm.time:F2}s (threshold {stopTime:F2}s)");
        }
    }

    /// <summary>
    /// 生成を止めるべき絶対秒を計算
    /// </summary>
    float CalcStopTimeSec()
    {
        switch (mode)
        {
            case StopMode.SecondsBeforeEnd:
            default:
                // 1番終了秒 - リード秒（移動時間＋マージン）
                return Mathf.Max(0f, verse1EndSec - Mathf.Max(0f, leadSeconds));

            case StopMode.BarsBeforeEnd:
                // 1番の総拍数→総秒、そこから stopBarsBeforeEnd 分を差し引き
                float secPerBeat = 60f / Mathf.Max(1f, bpm);
                float totalBeats = beatsPerBar * Mathf.Max(1, verse1Bars);
                float verse1End = songStartOffsetSec + totalBeats * secPerBeat;
                float stopBeatsBefore = beatsPerBar * Mathf.Max(0, stopBarsBeforeEnd);
                float stopTime = verse1End - stopBeatsBefore * secPerBeat;

                // さらに、移動時間相当のリード秒も引くとより自然（任意）
                stopTime -= Mathf.Max(0f, leadSeconds * 0.5f); // 半分だけ引くのが程よい
                return Mathf.Max(0f, stopTime);
        }
    }

#if UNITY_EDITOR
    // シーン上で閾値を視覚的に把握しやすくするための補助（任意）
    void OnDrawGizmosSelected()
    {
        if (!bgm) return;
        float stopT = CalcStopTimeSec();
        UnityEditor.Handles.Label(transform.position, $"Spawn Stop @ {stopT:F2}s");
    }
#endif
}
