using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

/// ====== Result データの受け口 ======
public static class ResultStore
{
    public struct Snapshot
    {
        public int maxCombo, perfect, good, miss;
        public int egg, flour, milk;
        public string rank;
        public float playTimeSec;
        public bool valid;
    }

    private static Snapshot _last;
    public static void Save(
        int maxCombo, int perfect, int good, int miss,
        int egg, int flour, int milk,
        string rank, float playTimeSec)
    {
        _last = new Snapshot
        {
            maxCombo = maxCombo, perfect = perfect, good = good, miss = miss,
            egg = egg, flour = flour, milk = milk,
            rank = rank, playTimeSec = playTimeSec, valid = true
        };
    }

    public static Snapshot TakeOnce()
    {
        var s = _last;
        _last.valid = false;
        return s;
    }
}

/// ====== UI 本体（旧フィールド名との互換ブリッジ付き） ======
[DisallowMultipleComponent]
public class ResultUI : MonoBehaviour
{
    // --- 新フィールド（推奨） ---
    [Header("Header")]
    public TMP_Text Title;
    public TMP_Text Sub;

    [Header("Rank (Text Color)")]
    public TMP_Text Rank;
    public Color RankColorS = new Color(1f, 1f, 1f);
    public Color RankColorA = new Color(0.6f, 0.85f, 1f);
    public Color RankColorB = new Color(0.8f, 1f, 0.6f);
    public Color RankColorC = new Color(1f, 0.9f, 0.6f);

    [Header("Others")]
    
    public TMP_Text TotalScoreValue;
public TMP_Text ComboValue;
    public TMP_Text PerfectValue;
    public TMP_Text GoodValue;
    public TMP_Text MissValue;
    public TMP_Text MilkCount;
    public TMP_Text FlourCount;
    public TMP_Text EggCount;

    [Header("Audio (Optional)")]
    public AudioSource jingle;

    [Header("自動収集（配線無しでも表示）")]
    public bool enableReflectionFallback = true;
    public string[] perfectKeys = new[] { "perfect", "nPerfect", "countPerfect" };
    public string[] goodKeys    = new[] { "good", "nGood", "countGood" };
    public string[] missKeys    = new[] { "miss", "nMiss", "countMiss" };
    public string[] comboKeys   = new[] { "maxcombo", "max_combo", "bestcombo" };
    public string[] eggKeys     = new[] { "egg", "countEgg" };
    public string[] flourKeys   = new[] { "flour", "countFlour" };
    public string[] milkKeys    = new[] { "milk", "countMilk" };
    public string[] rankKeys    = new[] { "rank", "grade" };
    public string[] timeKeys    = new[] { "time", "playtime", "clearTime", "songTimeSec" };

    // ========= ここから互換フィールド（Editor拡張が参照する古い名前） =========
    // テキスト
    [Header("Compatibility (Old Field Names)")]
    public TMP_Text titleText;
    public TMP_Text subText;
    public TMP_Text rankText;
    public TMP_Text comboValueText;
    public TMP_Text perfectValueText;
    public TMP_Text goodValueText;
    public TMP_Text missValueText;
    public TMP_Text milkCountText;   // ← Editorが要求
    public TMP_Text flourCountText;  // ← Editorが要求
    public TMP_Text eggCountText;    // ← Editorが要求

    // ランク色（Editorが小文字の rankColorX を要求）
    public Color rankColorS; // ← Editorが要求
    public Color rankColorA; // ← Editorが要求
    public Color rankColorB; // ← Editorが要求
    public Color rankColorC; // ← Editorが要求
    // ========= 互換ここまで ==============================================

    void OnValidate()
    {
        // 互換→新 へ自動ミラー（インスペクタに古い方だけ刺さっていても動く）
        if (!Title && titleText) Title = titleText;
        if (!Sub && subText) Sub = subText;
        if (!Rank && rankText) Rank = rankText;

        if (!ComboValue && comboValueText) ComboValue = comboValueText;
        if (!PerfectValue && perfectValueText) PerfectValue = perfectValueText;
        if (!GoodValue && goodValueText) GoodValue = goodValueText;
        if (!MissValue && missValueText) MissValue = missValueText;

        if (!MilkCount && milkCountText) MilkCount = milkCountText;
        if (!FlourCount && flourCountText) FlourCount = flourCountText;
        if (!EggCount && eggCountText) EggCount = eggCountText;

        // 色も同期（古→新 / 新→古 どちらも）
        if (rankColorS == default) rankColorS = RankColorS;
        if (rankColorA == default) rankColorA = RankColorA;
        if (rankColorB == default) rankColorB = RankColorB;
        if (rankColorC == default) rankColorC = RankColorC;

        RankColorS = rankColorS;
        RankColorA = rankColorA;
        RankColorB = rankColorB;
        RankColorC = rankColorC;
    }

    void OnEnable()
    {
        // 1) ResultStore を優先
        var snap = ResultStore.TakeOnce();

        // 2) 無ければ自動収集
        if (!snap.valid && enableReflectionFallback)
            snap = CollectByReflection();

        // 3) 反映
        ApplyToUI(snap);

        // 4) ジングル再生
        if (jingle && jingle.clip && !jingle.isPlaying) jingle.Play();
    }

    // ----------------- UI 反映 -----------------
    void ApplyToUI(ResultStore.Snapshot s)
    {
        if (Title) Title.text = "RESULT";
        if (Sub)   Sub.text   = s.playTimeSec > 0f ? FormatTime(s.playTimeSec) : "—";

        
        
        // 総合スコア表示
        if (TotalScoreValue)
        {
            var scoreMgr = ScoreManagerLite.Instance;
            if (scoreMgr != null)
            {
                int totalScore = scoreMgr.CalculateTotalScore();
                TotalScoreValue.text = totalScore.ToString("N0");
            }
        }
if (ComboValue)   ComboValue.text   = s.maxCombo.ToString();
        if (PerfectValue) PerfectValue.text = s.perfect.ToString();
        if (GoodValue)    GoodValue.text    = s.good.ToString();
        if (MissValue)    MissValue.text    = s.miss.ToString();

        if (MilkCount)  MilkCount.text  = "×" + s.milk;
        if (FlourCount) FlourCount.text = "×" + s.flour;
        if (EggCount)   EggCount.text   = "×" + s.egg;

        string r = string.IsNullOrEmpty(s.rank) ? GuessRank(s) : s.rank.ToUpperInvariant();
        if (Rank)
        {
            Rank.text = r;
            var col = r switch
            {
                "S" => RankColorS,
                "A" => RankColorA,
                "B" => RankColorB,
                _   => RankColorC
            };
            Rank.color = col;

            // 互換色も更新（インスペクタで参照されている可能性があるため）
            rankColorS = RankColorS;
            rankColorA = RankColorA;
            rankColorB = RankColorB;
            rankColorC = RankColorC;
        }

        // 互換テキスト群にもミラー（Editor拡張が参照する場合に備えて）
        MirrorToCompatTexts();
    }

    void MirrorToCompatTexts()
    {
        if (!titleText && Title) titleText = Title;
        if (!subText && Sub) subText = Sub;
        if (!rankText && Rank) rankText = Rank;
        if (!comboValueText && ComboValue) comboValueText = ComboValue;
        if (!perfectValueText && PerfectValue) perfectValueText = PerfectValue;
        if (!goodValueText && GoodValue) goodValueText = GoodValue;
        if (!missValueText && MissValue) missValueText = MissValue;
        if (!milkCountText && MilkCount) milkCountText = MilkCount;
        if (!flourCountText && FlourCount) flourCountText = FlourCount;
        if (!eggCountText && EggCount) eggCountText = EggCount;
    }

    string FormatTime(float sec)
    {
        if (sec < 0.0001f) return "—";
        int m = Mathf.FloorToInt(sec / 60f);
        float s = sec - m * 60f;
        return $"{m}:{s:00.00}";
    }

    string GuessRank(ResultStore.Snapshot s)
    {
        if (s.miss == 0 && s.good <= 5) return "S";
        if (s.miss <= 3) return "A";
        if (s.miss <= 10) return "B";
        return "C";
    }

    // ----------------- 自動収集（配線が無くても動く） -----------------
    ResultStore.Snapshot CollectByReflection()
    {
        var snap = new ResultStore.Snapshot();

#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
#else
        var behaviours = FindObjectsOfType<MonoBehaviour>();
#endif
        TryPickInt(behaviours, perfectKeys,  out snap.perfect);
        TryPickInt(behaviours, goodKeys,     out snap.good);
        TryPickInt(behaviours, missKeys,     out snap.miss);
        TryPickInt(behaviours, comboKeys,    out snap.maxCombo);
        TryPickInt(behaviours, eggKeys,      out snap.egg);
        TryPickInt(behaviours, flourKeys,    out snap.flour);
        TryPickInt(behaviours, milkKeys,     out snap.milk);
        TryPickRank(behaviours, rankKeys,    out snap.rank);
        TryPickTime(behaviours, timeKeys,    out snap.playTimeSec);

        snap.valid = true;
        return snap;
    }

    void TryPickInt(IEnumerable<MonoBehaviour> src, string[] keys, out int value)
    {
        value = 0;
        foreach (var b in src)
        {
            var t = b.GetType();
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (NameLike(f.Name, keys) && TryToInt(f.GetValue(b), out var v)) value = Math.Max(value, v);

            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (p.CanRead && NameLike(p.Name, keys) && TryToInt(SafeGet(() => p.GetValue(b, null)), out var v)) value = Math.Max(value, v);
        }
    }

    void TryPickRank(IEnumerable<MonoBehaviour> src, string[] keys, out string rank)
    {
        rank = null;
        foreach (var b in src)
        {
            var t = b.GetType();
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (NameLike(f.Name, keys))
                { var v = f.GetValue(b)?.ToString(); if (!string.IsNullOrEmpty(v)) { rank = v; return; } }

            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (p.CanRead && NameLike(p.Name, keys))
                { var v = SafeGet(() => p.GetValue(b, null))?.ToString(); if (!string.IsNullOrEmpty(v)) { rank = v; return; } }
        }
    }

    void TryPickTime(IEnumerable<MonoBehaviour> src, string[] keys, out float sec)
    {
        sec = 0f;
        foreach (var b in src)
        {
            var t = b.GetType();
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (NameLike(f.Name, keys) && TryToFloat(f.GetValue(b), out var v)) sec = Mathf.Max(sec, v);

            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (p.CanRead && NameLike(p.Name, keys) && TryToFloat(SafeGet(() => p.GetValue(b, null)), out var v)) sec = Mathf.Max(sec, v);
        }
        if (sec <= 0f) sec = Time.timeSinceLevelLoad;
    }

    bool NameLike(string name, string[] keys)
    {
        var n = name.ToLowerInvariant();
        return keys.Any(k => n.Contains(k.ToLowerInvariant()));
    }

    bool TryToInt(object o, out int v)
    {
        v = 0;
        if (o == null) return false;
        if (o is int i) { v = i; return true; }
        if (o is float f) { v = Mathf.RoundToInt(f); return true; }
        if (o is double d) { v = (int)Math.Round(d); return true; }
        if (int.TryParse(o.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var p)) { v = p; return true; }
        return false;
    }

    bool TryToFloat(object o, out float v)
    {
        v = 0f;
        if (o == null) return false;
        if (o is float f) { v = f; return true; }
        if (o is double d) { v = (float)d; return true; }
        if (o is int i) { v = i; return true; }
        if (float.TryParse(o.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var p)) { v = p; return true; }
        return false;
    }

    object SafeGet(Func<object> get)
    {
        try { return get(); }
        catch { return null; }
    }
}
