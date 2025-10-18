using System;
using System.Reflection;
using UnityEngine;

public class PrologueSpawnerResumeHelper : MonoBehaviour
{
    [Tooltip("AutoChartSpawner をドラッグ（空なら同じGameObjectから自動検索）")]
    public MonoBehaviour spawner; // AutoChartSpawner を割り当て

    [Tooltip("プロローグ明け、何拍後に最初のノーツを出すか（例：0.75拍）")]
    public float extraDelayBeats = 0.75f;

    // ---- ここから下は自動（触らなくてOK） ----
    FieldInfo fiConductor;
    FieldInfo fiNextSpawnBeat;
    FieldInfo fiNextLaneBeat;
    FieldInfo fiMinGlobalGapBeats;
    PropertyInfo piSongPositionBeats; // conductor.songPositionBeats がプロパティの場合
    FieldInfo    fiSongPositionBeats; // あるいはフィールドの場合

    void Awake()
    {
        if (spawner == null)
        {
            // 同じGameObjectからそれっぽいコンポーネント名を自動検索
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                var n = mb.GetType().Name;
                if (n.Contains("AutoChartSpawner"))
                {
                    spawner = mb;
                    break;
                }
            }
        }

        if (spawner != null)
        {
            var t = spawner.GetType();
            fiConductor        = t.GetField("conductor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fiNextSpawnBeat    = t.GetField("nextSpawnBeat", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fiNextLaneBeat     = t.GetField("nextLaneBeat", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fiMinGlobalGapBeats= t.GetField("minGlobalGapBeats", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // conductor.songPositionBeats（プロパティ or フィールド どちらでも対応）
            if (fiConductor != null)
            {
                var ct = fiConductor.FieldType;
                piSongPositionBeats = ct.GetProperty("songPositionBeats", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                fiSongPositionBeats = ct.GetField   ("songPositionBeats", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            }
        }
    }

    /// <summary>
    /// プロローグ終了直前に呼んでください（PrologueOverlayから自動で呼びます）
    /// </summary>
    public void ResetScheduleBeats(float? overrideDelayBeats = null)
    {
        if (spawner == null || fiNextSpawnBeat == null) return;

        float delay = overrideDelayBeats.HasValue ? overrideDelayBeats.Value : Mathf.Max(0f, extraDelayBeats);

        // いまの楽曲の「現在拍」を取得
        float nowBeat = 0f;
        if (fiConductor != null)
        {
            var conductor = fiConductor.GetValue(spawner);
            if (conductor != null)
            {
                if (piSongPositionBeats != null)
                    nowBeat = Convert.ToSingle(piSongPositionBeats.GetValue(conductor, null));
                else if (fiSongPositionBeats != null)
                    nowBeat = Convert.ToSingle(fiSongPositionBeats.GetValue(conductor));
            }
        }

        // 次回スポーン拍を「今 + 余裕拍」へ押し出し
        float nextBeat = nowBeat + Mathf.Max(0f, delay);

        // ついでに最小グローバル間隔も考慮
        if (fiMinGlobalGapBeats != null)
        {
            float gap = Convert.ToSingle(fiMinGlobalGapBeats.GetValue(spawner));
            nextBeat += Mathf.Max(0f, gap);
        }

        fiNextSpawnBeat.SetValue(spawner, nextBeat);

        // レーンごとの次回拍も押し出し（あれば）
        if (fiNextLaneBeat != null)
        {
            var arr = fiNextLaneBeat.GetValue(spawner) as Array;
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; i++)
                    arr.SetValue(nextBeat, i);
                fiNextLaneBeat.SetValue(spawner, arr);
            }
        }
    }
}
