using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

[System.Serializable] class RN { public float beat; public int lane; public string type=""; public bool linkedHold=false; public float duration=0; public float holdDuration=0; }
[System.Serializable] class RC { public string songName; public int bpm; public float offset; public RN[] notes; }

public class ChartRebuild
{
    // =====================================================================
    // データ型
    // =====================================================================
    struct ME { public float b; public bool h; public float hd; }   // beat, isHold, holdDur
    struct Mot { public float len; public ME[] ev; }

    static ME N(float b)          => new ME { b=b };
    static ME H(float b, float d) => new ME { b=b, h=true, hd=d };
    static Mot M(float len, params ME[] ev) => new Mot { len=len, ev=ev };

    // =====================================================================
    // レーンローテーション (単調にならないよう非規則パターン)
    // =====================================================================
    static readonly int[] LROT = {1,2,0,2,1,0,1,0,2,0,2,1,2,0,1,1,2,0,2,1,0,0,1,2,1,0,2,0,2,1,
                                   2,1,0,0,2,1,1,0,2,2,0,1,0,1,2,1,2,0,0,2,1,2,1,0,1,0,2,2,0,1};

static int NextLane(int prev, ref int ri)
    {
        // 隣レーン優先: 前のレーンから「遠く飛びすぎない」移動パターン
        // 70%の確率で隣レーン(±1)、30%で隣でないレーン
        int r = LROT[ri % LROT.Length]; ri++;
        if (r % 10 < 7)  // 隣レーン優先
        {
            int dir = (LROT[ri % LROT.Length] % 2 == 0) ? 1 : -1; ri++;
            int next = prev + dir;
            if (next < 0) next = 1;
            if (next > 2) next = 1;
            return next;
        }
        else
        {
            // 前と違うレーンならOK
            int next = r % 3;
            if (next == prev) next = (next + 1) % 3;
            return next;
        }
    }

    // =====================================================================
    // モチーフバンク
    // totalLen = 全イベントの(beat+holdDur)の最大値 + 0.5f バッファ
    // =====================================================================

    // --- Normal Verse : やや軽め、ノーツ主体・短ホールド混じり ---
    static readonly Mot[] NV = {
        M(4.5f, N(0), N(1), N(2), N(3), N(4)),             // 5連ノーツ
        M(4.5f, N(0), N(1.3f), N(2.6f), N(4f)),            // 4ノーツ(不規則間隔)
        M(4f,   N(0), N(1), N(2), N(3)),                    // 4連等間隔
        M(4f,   N(0), N(2), N(3)),                          // 3ノーツ広め
        M(3.5f, N(0), N(1), N(2)),                          // 3連コンパクト
        M(3.5f, H(0,1.5f), N(2f), N(3f)),                  // 短ホールド→2ノーツ
        M(4.5f, N(0), N(1.5f), H(2.5f,1.3f)),              // 2ノーツ→短ホールド  [max=3.8+0.5]
        M(5f,   N(0), N(1), H(2f,2f), N(4.5f)),            // 2ノーツ→ホールド→ノーツ [hold:2-4, N4.5 ✓]
    };

    // --- Normal Chorus : ホールド多め、ドラマチック ---
    static readonly Mot[] NC = {
        M(5.5f, N(0), N(1), H(2f,3f)),                               // 2ノーツ→長ホールド(3拍)
        M(5.5f, H(0,3f), N(3.5f), N(4.5f), N(5f)),                  // 長ホールド→3ノーツ
        M(6.5f, N(0), H(1f,4f), N(5.5f), N(6f)),                    // 1ノーツ→超長ホールド(4)→2ノーツ
        M(6f,   N(0), N(1), N(2), H(3f,2.5f)),                      // 3ノーツ→ホールド(2.5) [max=5.5+0.5=6]
        M(4.5f, H(0,3f), N(3.5f), N(4f)),                           // 長ホールド→2ノーツ
        M(7.5f, H(0,2f), N(2.5f), N(3.5f), H(4.5f,2.5f)),          // ホールド→2ノーツ→ホールド [max=7+0.5=7.5]
        M(7.5f, N(0), N(1), N(2), N(3), H(4f,3f)),                  // 4ノーツ→長ホールド(3) [max=7+0.5]
        M(5.5f, N(0), H(1f,2f), N(3.5f), N(4.5f), N(5f)),          // 1ノーツ→ホールド→3ノーツ
        M(7f,   H(0,4f), N(4.5f), N(5.5f), N(6.5f)),               // 超長ホールド(4)→3ノーツ
        M(6f,   N(0), N(1), N(2), H(3f,2f), N(5.5f)),              // 3ノーツ→ホールド→ノーツ [hold:3-5, N5.5 ✓, max=5.5+0.5=6]
    };

    // --- Hard Verse : 速め、高密度ノーツ、短ホールド散在 ---
    static readonly Mot[] HV = {
        M(3.5f, N(0), N(0.7f), N(1.4f), N(2.1f), N(2.8f)),        // 5連速攻
        M(3.5f, N(0), N(1), N(2), N(3)),                            // 4連等間隔
        M(3f,   N(0), N(0.7f), N(1.4f), N(2.1f)),                  // 4連速め
        M(3f,   N(0), N(1), N(2)),                                   // 3連
        M(4f,   H(0,1.5f), N(2f), N(2.7f), N(3.3f)),               // 短ホールド→3ノーツ [max=3.3+0.5=3.8→4f]
        M(3.5f, N(0), N(0.7f), H(1.4f,1.4f)),                      // 2連速→短ホールド [max=2.8+0.5=3.3→3.5f]
        M(3.5f, N(0), N(1.4f), N(2.8f)),                           // 3連広め
        M(3.5f, N(0), N(0.7f), N(1.4f), H(2f,1f)),                 // 3連→短ホールド [max=3+0.5=3.5 ✓]
    };

    // --- Hard Chorus : 高密度、ホールド多彩、複数ホールド混在 ---
    static readonly Mot[] HC = {
        M(4f,   N(0), N(0.7f), H(1.4f,2f)),                                      // 2連速→ホールド(2) [max=3.4+0.5=3.9→4f]
        M(5f,   H(0,2f), N(2.4f), N(3f), N(3.6f), N(4.2f)),                    // ホールド→4ノーツ [max=4.2+0.5=4.7→5f]
        M(5.5f, N(0), N(0.7f), N(1.4f), H(2f,3f)),                              // 3連速→長ホールド(3) [max=5+0.5=5.5 ✓]
        M(6f,   H(0,3f), N(3.4f), N(4f), N(4.6f), N(5.2f)),                    // 長ホールド→4ノーツ [max=5.2+0.5=5.7→6f]
        M(4f,   N(0), H(0.7f,2f), N(2.8f), N(3.5f)),                           // 1ノーツ→ホールド→2ノーツ [max=3.5+0.5=4 ✓]
        M(6.5f, H(0,2f), N(2.4f), H(3f,2.5f), N(5.6f)),                        // ホールド→ノーツ→ホールド→ノーツ [max=5.6+0.5=6.1→6.5f]
        M(6.5f, N(0), N(0.7f), N(1.4f), N(2.1f), H(3f,2.5f), N(5.6f)),       // 4連速→ホールド→ノーツ [hold:3-5.5, N5.6 ✓]
        M(5f,   N(0), H(0.7f,3f), N(3.8f), N(4.3f)),                           // 1ノーツ→超長ホールド(3)→2ノーツ [hold:0.7-3.7, N3.8 ✓]
        M(3.5f, N(0), N(0.7f), H(1.4f,1.4f), N(3f)),                           // 2連→短ホールド→ノーツ [hold:1.4-2.8, N3 ✓]
        M(5.5f, H(0,1.5f), N(2f), N(2.7f), H(3.3f,1.5f)),                     // ホールド→2ノーツ→ホールド [hold1:0-1.5, hold2:3.3-4.8, max=4.8+0.5=5.3→5.5f]
    };

    // =====================================================================
    // モチーフ配置
    // =====================================================================
static void PlaceMotif(Mot mot, float startBeat, ref int prevLane, ref int lri, List<RN> notes)
    {
        int n = mot.ev.Length;
        var lanes = new int[n];

        // 各イベントのレーンを他レーン流れで仓決め
        int tmpPrev = prevLane;
        int tmpLri = lri;
        for (int i = 0; i < n; i++)
        {
            lanes[i] = NextLane(tmpPrev, ref tmpLri);
            tmpPrev = lanes[i];
        }

        // ホールドレーンを先に確定し、直前ノーツを隣レーンに調整
        for (int i = 0; i < n; i++)
        {
            if (!mot.ev[i].h) continue;
            int holdLane = lanes[i];

            // ホールド直前のノーツ: 必ず holdLaneの隣(±1)にする
            // 隣がなければ holdLane そのままでOK
            if (i - 1 >= 0 && !mot.ev[i-1].h)
            {
                int adj1 = (holdLane + 1) % 3;  // 隣レーンA
                int adj2 = (holdLane + 2) % 3;  // 隣レーンB
                // 直前ノーツは隣レーンを優先、前のノーツに少し近い方
                int prev2 = (i - 2 >= 0) ? lanes[i-2] : prevLane;
                // adj1とadj2のうちprev2に近い方を強制
                lanes[i-1] = (Mathf.Abs(adj1 - prev2) <= Mathf.Abs(adj2 - prev2)) ? adj1 : adj2;
            }

            // ホールド直前2つのノーツが全部同レーンにならないようする
            if (i - 2 >= 0 && !mot.ev[i-2].h && lanes[i-2] == lanes[i-1])
                lanes[i-2] = (lanes[i-2] == 0) ? 1 : lanes[i-2] - 1;

            // ホールド直後の最初のノーツは holdLane の隣レーン
            float holdEnd = mot.ev[i].b + mot.ev[i].hd;
            for (int j = i + 1; j < n; j++)
            {
                if (mot.ev[j].b <= holdEnd + 0.05f) continue;
                if (lanes[j] == holdLane)
                    lanes[j] = (holdLane + 1) % 3;
                break;
            }
        }

        // 確定したレーンで配置
        for (int i = 0; i < n; i++)
        {
            float beat = startBeat + mot.ev[i].b;
            if (mot.ev[i].h)
                notes.Add(new RN { beat=beat, lane=lanes[i], linkedHold=true, holdDuration=mot.ev[i].hd });
            else
                notes.Add(new RN { beat=beat, lane=lanes[i] });
        }

        prevLane = lanes[n-1];
        lri = tmpLri;
    }

    // セクション全体を埋める (toBeat を超えたら止まる)
    static void BuildSection(Mot[] bank, float from, float to, ref int idx, ref int pl, ref int lri, List<RN> notes)
    {
        float cur = from;
        while (cur < to)
        {
            Mot m = bank[idx % bank.Length]; idx++;
            PlaceMotif(m, cur, ref pl, ref lri, notes);
            cur += m.len;
        }
    }

    // イントロ/アウトロ用シンプルノーツ列
    static void BuildSimpleNotes(float from, float to, float step, int[] laneArr, ref int li, List<RN> notes)
    {
        for (float b = from; b <= to; b += step)
            if (!notes.Any(n => Mathf.Abs(n.beat - b) < 0.4f))
            {
                int lane = laneArr[li % laneArr.Length]; li++;
                notes.Add(new RN { beat=b, lane=lane });
            }
    }

    // =====================================================================
    // 保存＆検証
    // =====================================================================
    static RC Load(string path)
    {
        if (!File.Exists(path)) { Debug.LogError("[Rebuild] NOT FOUND: " + path); return null; }
        return JsonUtility.FromJson<RC>(File.ReadAllText(path));
    }

    static void SaveAndValidate(string path, List<RN> notes, float bpm, float clipLen, float preRoll=4f)
    {
        notes.Sort((a,b2) => a.beat.CompareTo(b2.beat));

        // 末尾トリム (clip - 4秒 以内に収める)
        float maxBeat = (clipLen - 4f) * bpm / 60f - preRoll;
        notes.RemoveAll(n => n.beat > maxBeat);
        notes.RemoveAll(n => n.linkedHold && n.beat + n.holdDuration > maxBeat + 1f);

        // ホールド中のノーツを全て除去 (全レーン)
        var holds = notes.Where(n => n.linkedHold && n.holdDuration > 0).ToList();
        notes.RemoveAll(n => {
            if (n.linkedHold) return false;
            return holds.Any(h => n.beat > h.beat + 0.05f && n.beat < h.beat + h.holdDuration - 0.05f);
        });

        // 重複除去
        notes.Sort((a,b2) => a.beat.CompareTo(b2.beat));
        var deduped = new List<RN>();
        for (int i=0; i<notes.Count; i++)
            if (i==0 || notes[i].beat - notes[i-1].beat >= 0.1f)
                deduped.Add(notes[i]);

        RC rc = Load(path);
        rc.notes = deduped.ToArray();
        File.WriteAllText(path, JsonUtility.ToJson(rc, true));

        holds = deduped.Where(n => n.linkedHold).ToList();
        int violations = deduped.Count(n => !n.linkedHold &&
            holds.Any(h => n.beat > h.beat+0.05f && n.beat < h.beat+h.holdDuration-0.05f));
        string name = Path.GetFileNameWithoutExtension(path);
        float spb = 60f / rc.bpm;
        var last = deduped[deduped.Count-1];
        float lastHit = (last.beat + preRoll) * spb;
        Debug.Log($"[Rebuild] {name}: {deduped.Count} notes ({holds.Count} holds) | " +
                  $"first={deduped[0].beat:F1} last={last.beat:F1} | " +
                  $"lastHit={lastHit:F1}s margin={(clipLen-lastHit):F1}s | violations={violations}");
    }

    // =====================================================================
    // Evening A  (Normal / BPM=121 / clip=119.9s)
    // 曲構成推定: Intro→Verse1→Chorus1→Verse2→Chorus2→Outro
    // =====================================================================
    [MenuItem("KumaDen/Rebuild Normal A Chart")]
    static void RebuildNormalA()
    {
        string path = Application.dataPath + "/Resources/Charts/Evening/kuma_odyssey_chart_kumaden_normal.json";
        RC src = Load(path); if (src==null) return;
        var notes = new List<RN>();
        int pl=1, lri=0, vIdx=0, cIdx=0;

        // Intro: beat 2〜16 (2拍間隔ノーツ)
        int[] introL = {1,0,2,1,0,2,1,0}; int ili=0;
        BuildSimpleNotes(2f, 16f, 2f, introL, ref ili, notes);
        pl = notes[notes.Count-1].lane;

        // Verse1: beat 16〜64
        BuildSection(NV, 16f, 64f, ref vIdx, ref pl, ref lri, notes);

        // Chorus1: beat 64〜128
        BuildSection(NC, 64f, 128f, ref cIdx, ref pl, ref lri, notes);

        // Verse2: beat 128〜160
        BuildSection(NV, 128f, 160f, ref vIdx, ref pl, ref lri, notes);

        // Chorus2: beat 160〜212
        BuildSection(NC, 160f, 212f, ref cIdx, ref pl, ref lri, notes);

        // Outro: beat 212〜226 (2拍間隔)
        int[] tailL = {0,2,1,0,2,1}; int tli=0;
        BuildSimpleNotes(212f, 226f, 2f, tailL, ref tli, notes);

        SaveAndValidate(path, notes, src.bpm, 119.9f);
    }

    // =====================================================================
    // Evening B  (Normal / BPM=122 / clip=75.91s)
    // 曲構成推定: Intro→Verse→Chorus→Outro
    // =====================================================================
    [MenuItem("KumaDen/Rebuild Normal B Chart")]
    static void RebuildNormalB()
    {
        string path = Application.dataPath + "/Resources/Charts/Evening/bears_adventure_chart_kumaden_normal.json";
        RC src = Load(path); if (src==null) return;
        var notes = new List<RN>();
        int pl=2, lri=5, vIdx=0, cIdx=0;

        // Intro: beat 2〜10
        int[] introL = {2,0,1,2,0}; int ili=0;
        BuildSimpleNotes(2f, 10f, 2f, introL, ref ili, notes);
        pl = notes[notes.Count-1].lane;

        // Verse: beat 10〜48
        BuildSection(NV, 10f, 48f, ref vIdx, ref pl, ref lri, notes);

        // Chorus: beat 48〜112
        BuildSection(NC, 48f, 112f, ref cIdx, ref pl, ref lri, notes);

        // Outro: beat 112〜138 (2拍間隔)
        int[] tailL = {1,2,0,1,2}; int tli=0;
        BuildSimpleNotes(112f, 138f, 2f, tailL, ref tli, notes);

        SaveAndValidate(path, notes, src.bpm, 75.91f);
    }

    // =====================================================================
    // Night A  (Hard / BPM=121 / clip=117.02s)
    // 曲構成推定: Intro→Verse1→Chorus1→Verse2→Chorus2→Outro
    // =====================================================================
    [MenuItem("KumaDen/Rebuild Hard A Chart")]
    static void RebuildHardA()
    {
        string path = Application.dataPath + "/Resources/Charts/Night/kuma_odyssey_chart_kumaden_hard.json";
        RC src = Load(path); if (src==null) return;
        var notes = new List<RN>();
        int pl=1, lri=3, vIdx=0, cIdx=0;

        // Intro: beat 2〜14 (1.38拍間隔)
        int[] introL = {1,0,2,1,2,0,1,0,2,1}; int ili=0;
        BuildSimpleNotes(2f, 14f, 1.38f, introL, ref ili, notes);
        pl = notes[notes.Count-1].lane;

        // Verse1: beat 14〜60
        BuildSection(HV, 14f, 60f, ref vIdx, ref pl, ref lri, notes);

        // Chorus1: beat 60〜120
        BuildSection(HC, 60f, 120f, ref cIdx, ref pl, ref lri, notes);

        // Verse2: beat 120〜155
        BuildSection(HV, 120f, 155f, ref vIdx, ref pl, ref lri, notes);

        // Chorus2: beat 155〜212
        BuildSection(HC, 155f, 212f, ref cIdx, ref pl, ref lri, notes);

        // Outro: beat 212〜220 (1.38拍間隔)
        int[] tailL = {2,0,1,2,0,1,2}; int tli=0;
        BuildSimpleNotes(212f, 220f, 1.38f, tailL, ref tli, notes);

        SaveAndValidate(path, notes, src.bpm, 117.02f);
    }

    // =====================================================================
    // Night B  (Hard / BPM=122 / clip=85.75s)
    // 曲構成推定: Intro→Verse→Chorus→Outro
    // =====================================================================
    [MenuItem("KumaDen/Rebuild Hard B Chart")]
    static void RebuildHardB()
    {
        string path = Application.dataPath + "/Resources/Charts/Night/bears_adventure_chart_kumaden_hard.json";
        RC src = Load(path); if (src==null) return;
        var notes = new List<RN>();
        int pl=2, lri=7, vIdx=0, cIdx=0;

        // Intro: beat 2〜9 (1.2拍間隔)
        int[] introL = {2,0,1,2,0,1,2}; int ili=0;
        BuildSimpleNotes(2f, 9f, 1.2f, introL, ref ili, notes);
        pl = notes[notes.Count-1].lane;

        // Verse: beat 9〜50
        BuildSection(HV, 9f, 50f, ref vIdx, ref pl, ref lri, notes);

        // Chorus: beat 50〜145
        BuildSection(HC, 50f, 145f, ref cIdx, ref pl, ref lri, notes);

        // Outro: beat 145〜160 (1.2拍間隔)
        int[] tailL = {0,1,2,0,1,2}; int tli=0;
        BuildSimpleNotes(145f, 160f, 1.2f, tailL, ref tli, notes);

        SaveAndValidate(path, notes, src.bpm, 85.75f);
    }

    // =====================================================================
    // 一括実行
    // =====================================================================
    [MenuItem("KumaDen/Rebuild All Evening+Night Charts")]
    static void RebuildAll()
    {
        RebuildNormalA();
        RebuildNormalB();
        RebuildHardA();
        RebuildHardB();
        AssetDatabase.Refresh();
        Debug.Log("[Rebuild] ===== ALL DONE =====");
    }
}
