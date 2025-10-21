using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultCaller : MonoBehaviour
{
    [Header("参照")]
    public ResultUI resultUI;
    public GameObject resultCanvas;
    public GameObject gameplayCanvas;

    [Header("サウンド（BGM/SE 停止対象）")]
    public AudioSource bgm;
    public AudioSource[] extraAudios;

    [Header("BGMの止め方")]
    [Range(0f,1f)] public float duckVolume = 0.25f;
    public float duckSeconds = 0.15f;
    public float fadeOutSeconds = 1.50f;

    [Header("リザルト用ジングル")]
    public bool  playJingle = true;
    public AudioClip resultJingle;
    [Range(0f,1f)] public float jingleVolume = 1f;
    public float jingleDelaySeconds = 0.12f;
    public bool  jingleIgnoresListenerPause = true;

    [Header("ゲーム停止のやり方")]
    public bool useTimeScalePause = true;
    public bool pauseAllAudioByListener = false;

    [Header("止めたいスクリプト/オブジェクト")]
    public MonoBehaviour[] systemsToDisable;
    public GameObject[] objectsToDisable;

    [Header("リザルト表示演出")]
    public float resultFadeSeconds = 0.5f;

    [Header("テスト用ダミー値（最終手段）")]
    public string rank = "A";
    public int maxCombo = 86;
    public int perfect = 54, good = 22, miss = 3;
    public int milk = 3, flour = 2, egg = 4;
    public float timeSec = 85f;

    [Header("ダミー使用ポリシー")]
    public bool fallbackToDummyIfNoData = false;

    [Header("TITLEシーン名")]
    public string titleSceneName = "Title";

    [Serializable]
    public class CustomBinding
    {
        [Tooltip("rank/maxCombo/perfect/good/miss/milk/flour/egg/timeSec")]
        public string key;
        public Component source;
        public string memberName;
    }

    [Header("Custom Bindings（見つからない値だけ手でマップ）")]
    public CustomBinding[] customBindings;

    // 画面フェード
    [Header("画面フェード")]
    public bool fadeBeforeResult = true;               // リザルト前に暗転する
    public float fadeOutSecondsToResult = 0.8f;        // 暗転時間
    public bool fadeInAfterResult = true;              // リザルトを出した直後に黒板を開く
    public float fadeInSecondsAfterResult = 0.5f;      // 開く時間

    bool shown;
    AudioSource jingleSource;

    // ————————— 公開API —————————
    public void ShowResult(ResultData data) => StartCoroutine(ShowResultSequence(data));

    public void ShowResult()
    {
        ResultData d = new ResultData();
        bool gotAny = ApplyCustomBindings(ref d);
        gotAny |= TryAutoCollect(ref d);

        if (!gotAny && !fallbackToDummyIfNoData)
        {
            d = new ResultData { rank = "C" };
        }
        else if (!gotAny && fallbackToDummyIfNoData)
        {
            d = new ResultData {
                rank = rank, maxCombo = maxCombo,
                perfect = perfect, good = good, miss = miss,
                items = new ResultItems { milk = milk, flour = flour, egg = egg },
                timeSec = timeSec
            };
        }
        StartCoroutine(ShowResultSequence(d));
    }

    // ————————— 本体シーケンス —————————
    System.Collections.IEnumerator ShowResultSequence(ResultData d)
    {
        if (shown) yield break;
        shown = true;

        // 先にBGMを下げ→フェード
        if (bgm) StartCoroutine(DuckThenFade(bgm, duckVolume, duckSeconds, fadeOutSeconds));
        if (extraAudios != null)
            foreach (var a in extraAudios.Where(x => x)) StartCoroutine(FadeOutAudio(a, Mathf.Max(duckSeconds,0.3f)));

        // ① リザルト直前の暗転（黒くする）
        if (fadeBeforeResult && ScreenFader.Instance)
            yield return ScreenFader.Instance.FadeOut(fadeOutSecondsToResult);

        // ② リザルトUIへ切替
        InternalShowResult(d);

        // ③ 黒板を開いて（透明にして）リザルトを見せる ← これが重要！
        if (fadeInAfterResult && ScreenFader.Instance)
            yield return ScreenFader.Instance.FadeIn(fadeInSecondsAfterResult);
        else
            // 保険：フェーダーが残らないように完全透明＆非アクティブにする
            if (ScreenFader.Instance)
            {
                ScreenFader.Instance.Alpha = 0f;
                if (ScreenFader.Instance.autoDeactivateWhenClear)
                    ScreenFader.Instance.gameObject.SetActive(false);
            }
    }

    void InternalShowResult(ResultData d)
    {
        if (useTimeScalePause) Time.timeScale = 0f;
        if (pauseAllAudioByListener) AudioListener.pause = true;

        if (playJingle && resultJingle) StartCoroutine(PlayJingleAfter(jingleDelaySeconds));

        if (systemsToDisable != null) foreach (var m in systemsToDisable.Where(x => x)) m.enabled = false;
        if (objectsToDisable  != null) foreach (var o in objectsToDisable .Where(x => x)) o.SetActive(false);

        if (gameplayCanvas) gameplayCanvas.SetActive(false);
        if (resultCanvas)
        {
            resultCanvas.SetActive(true);
            StartCoroutine(FadeInResultCanvas(resultCanvas, resultFadeSeconds));
        }

        if (resultUI) resultUI.Bind(d);
    }

    // ————————— ボタン —————————
    public void OnRetry()
    {
        if (useTimeScalePause) Time.timeScale = 1f;
        AudioListener.pause = false;
        if (bgm) { bgm.Stop(); bgm.volume = 1f; }
        if (extraAudios != null) foreach (var a in extraAudios.Where(x => x)) { a.Stop(); a.volume = 1f; }
        if (jingleSource) jingleSource.Stop();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnTitle()
    {
        if (useTimeScalePause) Time.timeScale = 1f;
        AudioListener.pause = false;
        if (bgm) { bgm.Stop(); bgm.volume = 1f; }
        if (extraAudios != null) foreach (var a in extraAudios.Where(x => x)) { a.Stop(); a.volume = 1f; }
        if (jingleSource) jingleSource.Stop();
        SceneManager.LoadScene(titleSceneName);
    }

    // ————————— Custom Bindings / 自動収集 —————————
    private bool ApplyCustomBindings(ref ResultData d)
    {
        bool any = false;
        if (customBindings == null) return false;

        foreach (var b in customBindings)
        {
            if (b == null || b.source == null || string.IsNullOrEmpty(b.key) || string.IsNullOrEmpty(b.memberName))
                continue;

            object value = GetMemberValue(b.source, b.memberName);
            if (value == null) continue;

            any = true;
            switch (b.key)
            {
                case "rank":     d.rank = value.ToString(); break;
                case "maxCombo": d.maxCombo = SafeInt(value, d.maxCombo); break;
                case "perfect":  d.perfect = SafeInt(value, d.perfect); break;
                case "good":     d.good = SafeInt(value, d.good); break;
                case "miss":     d.miss = SafeInt(value, d.miss); break;
                case "milk":     d.items.milk = SafeInt(value, d.items.milk); break;
                case "flour":    d.items.flour = SafeInt(value, d.items.flour); break;
                case "egg":      d.items.egg = SafeInt(value, d.items.egg); break;
                case "timeSec":  d.timeSec = SafeFloat(value, d.timeSec); break;
            }
        }
        return any;
    }

    private static readonly string[] candidateTypes =
        { "Score", "GameFlow", "Result", "Judge", "Counter", "Stats", "State", "Manager", "Combo" };

    private static readonly (string key,string[] aliases)[] fields =
    {
        ("rank"    , new[]{"rank","resultRank","grade","finalRank","Rank"}),
        ("maxCombo", new[]{"maxcombo","bestcombo","max_combo","comboMax","MaxChain","BestChain","HighestCombo","MaxCombo"}),
        ("perfect" , new[]{"perfect","just","p","Perfect","Just","PerfectCount","JustCount","NumPerfect","CountPerfect"}),
        ("good"    , new[]{"good","great","g","Good","Great","GoodCount","GreatCount","NumGreat","CountGood"}),
        ("miss"    , new[]{"miss","bad","m","Miss","Bad","MissCount","NumMiss","CountMiss"}),
        ("milk"    , new[]{"milk","Milk","MilkCount"}),
        ("flour"   , new[]{"flour","Flour","Powder","PowderCount"}),
        ("egg"     , new[]{"egg","Egg","EggCount"}),
        ("timeSec" , new[]{"time","elapsed","timeSec","playtime","PlayTime","ElapsedTime","ClearTimeSec"})
    };

    private bool TryAutoCollect(ref ResultData data)
    {
        bool gotAny = false;

        var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mb in all)
        {
            if (!candidateTypes.Any(k => mb.GetType().Name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                continue;

            var t = mb.GetType();
            foreach (var (key, aliases) in fields)
            {
                if (HasValueFor(ref data, key)) continue;

                var member = t.GetMembers(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                              .FirstOrDefault(mi =>
                                  (mi.MemberType == MemberTypes.Field || mi.MemberType == MemberTypes.Property) &&
                                  aliases.Any(a => mi.Name.Equals(a, StringComparison.OrdinalIgnoreCase)));

                if (member == null) continue;

                object value = null;
                try
                {
                    if (member is FieldInfo fi) value = fi.GetValue(mb);
                    else if (member is PropertyInfo pi && pi.CanRead) value = pi.GetValue(mb, null);
                }
                catch {}

                if (value == null) continue;
                gotAny = true;

                switch (key)
                {
                    case "rank":     data.rank = value.ToString(); break;
                    case "maxCombo": data.maxCombo = SafeInt(value, data.maxCombo); break;
                    case "perfect":  data.perfect = SafeInt(value, data.perfect); break;
                    case "good":     data.good = SafeInt(value, data.good); break;
                    case "miss":     data.miss = SafeInt(value, data.miss); break;
                    case "milk":     data.items.milk = SafeInt(value, data.items.milk); break;
                    case "flour":    data.items.flour = SafeInt(value, data.items.flour); break;
                    case "egg":      data.items.egg = SafeInt(value, data.items.egg); break;
                    case "timeSec":  data.timeSec = SafeFloat(value, data.timeSec); break;
                }
            }
        }

        if (string.IsNullOrEmpty(data.rank) && (data.perfect + data.good + data.miss) > 0)
            data.rank = EstimateRank(data);

        return gotAny;
    }

    private static object GetMemberValue(Component src, string name)
    {
        var t = src.GetType();
        var flags = BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        var f = t.GetField(name, flags);
        if (f != null) return f.GetValue(src);
        var p = t.GetProperty(name, flags);
        if (p != null && p.CanRead) return p.GetValue(src, null);
        return null;
    }

    private static int SafeInt(object v, int def){ try{ return Convert.ToInt32(v);}catch{ return def; } }
    private static float SafeFloat(object v, float def){ try{ return Convert.ToSingle(v);}catch{ return def; } }

    private static bool HasValueFor(ref ResultData d, string key)
    {
        return key switch {
            "rank"     => !string.IsNullOrEmpty(d.rank),
            "maxCombo" => d.maxCombo != 0,
            "perfect"  => d.perfect  != 0,
            "good"     => d.good     != 0,
            "miss"     => d.miss     != 0,
            "milk"     => d.items.milk  != 0,
            "flour"    => d.items.flour != 0,
            "egg"      => d.items.egg   != 0,
            "timeSec"  => d.timeSec  != 0,
            _ => false
        };
    }

    private static string EstimateRank(ResultData d)
    {
        var total = Mathf.Max(1, d.perfect + d.good + d.miss);
        var acc = (d.perfect * 1f + d.good * 0.7f) / total;
        if (acc >= 0.95f) return "S";
        if (acc >= 0.85f) return "A";
        if (acc >= 0.70f) return "B";
        return "C";
    }

    // ————————— サウンド/演出ユーティリティ —————————
    private System.Collections.IEnumerator DuckThenFade(AudioSource src, float toVol, float duckDur, float fadeDur)
    {
        if (!src) yield break;
        float startVol = src.volume;
        float t = 0f;
        while (t < duckDur) { t += Time.unscaledDeltaTime; src.volume = Mathf.Lerp(startVol, toVol, t/Mathf.Max(0.0001f,duckDur)); yield return null; }
        src.volume = toVol;
        t = 0f; float from = src.volume;
        while (t < fadeDur) { t += Time.unscaledDeltaTime; src.volume = Mathf.Lerp(from, 0f, t/Mathf.Max(0.0001f,fadeDur)); yield return null; }
        src.volume = 0f; src.Pause();
    }

    private System.Collections.IEnumerator FadeOutAudio(AudioSource src, float duration)
    {
        if (!src) yield break;
        float start = src.volume, t = 0f;
        while (t < duration) { t += Time.unscaledDeltaTime; src.volume = Mathf.Lerp(start, 0f, t/Mathf.Max(0.0001f,duration)); yield return null; }
        src.volume = 0f; src.Pause();
    }

    private System.Collections.IEnumerator PlayJingleAfter(float delay)
    {
        float t=0f; while (t < delay) { t += Time.unscaledDeltaTime; yield return null; }
        if (!resultJingle) yield break;
        if (!jingleSource) { jingleSource = gameObject.AddComponent<AudioSource>(); jingleSource.playOnAwake=false; jingleSource.loop=false; if (bgm) jingleSource.outputAudioMixerGroup = bgm.outputAudioMixerGroup; }
        jingleSource.ignoreListenerPause = jingleIgnoresListenerPause;
        jingleSource.clip = resultJingle; jingleSource.volume = jingleVolume; jingleSource.time = 0f; jingleSource.Play();
    }

    private System.Collections.IEnumerator FadeInResultCanvas(GameObject go, float duration)
    {
        if (!go) yield break;
        var cg = go.GetComponent<CanvasGroup>(); if (!cg) cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
        float t=0f; duration = Mathf.Max(0.01f, duration);
        while (t < duration) { t += Time.unscaledDeltaTime; cg.alpha = Mathf.Clamp01(t/duration); yield return null; }
        cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;
    }
}
