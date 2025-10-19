using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultCaller : MonoBehaviour
{
    [Header("参照")]
    public ResultUI resultUI;
    public GameObject resultCanvas;   // ResultCanvas（開始時は非表示推奨）
    public GameObject gameplayCanvas;

    [Header("サウンド（BGM/SE 停止対象）")]
    public AudioSource bgm;                 // BGMのAudioSource
    public AudioSource[] extraAudios;       // 他に止めたいSEのAudioSource

    [Header("BGMの止め方（速くジングルを鳴らすための2段階）")]
    [Tooltip("すぐにBGM音量をここまで下げる（0=無音, 1=元音量）")]
    [Range(0f,1f)] public float duckVolume = 0.25f; // 25%まで一瞬で下げる
    [Tooltip("上のダックにかける時間（秒）")]
    public float duckSeconds = 0.15f;               // 0.15秒で素早く下げる
    [Tooltip("ダック後、最終的に0まで消えるまでの追加フェード時間（秒）")]
    public float fadeOutSeconds = 1.50f;            // その後ゆっくり消す

    [Header("リザルト用ジングル")]
    public bool  playJingle = true;
    public AudioClip resultJingle;
    [Range(0f,1f)] public float jingleVolume = 1f;
    [Tooltip("リザルト表示から何秒後にジングル開始するか（小さめにすると速く聞こえる）")]
    public float jingleDelaySeconds = 0.12f;        // ほぼ即時
    public bool  jingleIgnoresListenerPause = true; // AudioListener.pause中でも鳴らす

    [Header("ゲーム停止のやり方")]
    public bool useTimeScalePause = true;           // 物理/Update停止
    public bool pauseAllAudioByListener = false;    // 全音一括停止（ジングルは無視設定で鳴らす）

    [Header("止めたいスクリプト/オブジェクト")]
    public MonoBehaviour[] systemsToDisable;
    public GameObject[] objectsToDisable;

    [Header("テスト用ダミー値（あとで実データに差し替え）")]
    public string rank = "A";
    public int maxCombo = 86;
    public int perfect = 54, good = 22, miss = 3;
    public int milk = 3, flour = 2, egg = 4;
    public float timeSec = 37.5f;

    [Header("TITLEシーン名")]
    public string titleSceneName = "Title";

    bool shown;
    AudioSource jingleSource; // ジングル専用

    // ===== リザルト表示 =====
    public void ShowResult()
    {
        if (shown) return;
        shown = true;

        // --- 1) BGM：素早くダック → そのままフェードアウト ---
        if (bgm) StartCoroutine(DuckThenFade(bgm, duckVolume, duckSeconds, fadeOutSeconds));

        // --- 2) 他SEは短くフェードアウト ---
        if (extraAudios != null)
            foreach (var a in extraAudios.Where(a => a))
                StartCoroutine(FadeOutAudio(a, Mathf.Max(duckSeconds, 0.3f)));

        // --- 3) 早めにジングル開始（BGMダックと重ねる） ---
        if (playJingle && resultJingle)
            StartCoroutine(PlayJingleAfter(jingleDelaySeconds));

        // --- 4) ゲーム進行停止 ---
        if (useTimeScalePause) Time.timeScale = 0f;
        if (pauseAllAudioByListener) AudioListener.pause = true; // ジングルはignoreで鳴らす

        if (systemsToDisable != null)
            foreach (var m in systemsToDisable.Where(m => m)) m.enabled = false;
        if (objectsToDisable != null)
            foreach (var o in objectsToDisable.Where(o => o)) o.SetActive(false);

        // --- 5) UI切替 ---
        if (gameplayCanvas) gameplayCanvas.SetActive(false);
        if (resultCanvas)   resultCanvas.SetActive(true);

        // --- 6) 表示データ反映 ---
        var d = new ResultData {
            rank = rank,
            maxCombo = maxCombo,
            perfect = perfect, good = good, miss = miss,
            items = new ResultItems { milk = milk, flour = flour, egg = egg },
            timeSec = timeSec
        };
        if (resultUI) resultUI.Bind(d);
    }

    // ===== RETRY =====
    public void OnRetry()
    {
        if (useTimeScalePause) Time.timeScale = 1f;
        AudioListener.pause = false;

        if (bgm) { bgm.Stop(); bgm.volume = 1f; }
        if (extraAudios != null)
            foreach (var a in extraAudios.Where(a => a)) { a.Stop(); a.volume = 1f; }
        if (jingleSource) jingleSource.Stop();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ===== TITLE =====
    public void OnTitle()
    {
        if (useTimeScalePause) Time.timeScale = 1f;
        AudioListener.pause = false;

        if (bgm) { bgm.Stop(); bgm.volume = 1f; }
        if (extraAudios != null)
            foreach (var a in extraAudios.Where(a => a)) { a.Stop(); a.volume = 1f; }
        if (jingleSource) jingleSource.Stop();

        SceneManager.LoadScene(titleSceneName);
    }

    // ====== 2段階：素早くダック→そのままフェード ======
    private System.Collections.IEnumerator DuckThenFade(AudioSource src, float toVol, float duckDur, float fadeDur)
    {
        if (!src) yield break;
        float startVol = src.volume;

        // ① 素早くダック（TimeScale=0でも進む）
        float t = 0f;
        while (t < duckDur)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(startVol, toVol, Mathf.Clamp01(t / duckDur));
            yield return null;
        }
        src.volume = toVol;

        // ② ゆっくり0までフェード
        t = 0f;
        float from = src.volume;
        while (t < fadeDur)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(from, 0f, Mathf.Clamp01(t / fadeDur));
            yield return null;
        }
        src.volume = 0f;
        src.Pause();
    }

    // ====== SEなどのシンプルフェード ======
    private System.Collections.IEnumerator FadeOutAudio(AudioSource src, float duration)
    {
        if (!src) yield break;
        float startVol = src.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(t / duration));
            yield return null;
        }
        src.volume = 0f;
        src.Pause();
    }

    // ====== 早めにジングルを鳴らす（専用AudioSource, 1回だけ） ======
    private System.Collections.IEnumerator PlayJingleAfter(float delay)
    {
        float t = 0f;
        while (t < delay) { t += Time.unscaledDeltaTime; yield return null; }

        if (!resultJingle) yield break;

        if (!jingleSource)
        {
            jingleSource = gameObject.AddComponent<AudioSource>();
            jingleSource.playOnAwake = false;
            jingleSource.loop = false; // 1回だけ
            if (bgm) jingleSource.outputAudioMixerGroup = bgm.outputAudioMixerGroup;
        }
        jingleSource.ignoreListenerPause = jingleIgnoresListenerPause;
        jingleSource.clip = resultJingle;
        jingleSource.volume = jingleVolume;
        jingleSource.time = 0f;
        jingleSource.Play();
    }
}
