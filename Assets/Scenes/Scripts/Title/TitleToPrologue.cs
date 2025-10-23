using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class TitleToPrologue : MonoBehaviour
{
    [Header("Title")]
    public CanvasGroup titleCanvas;   // タイトルUI一式（CanvasGroup）
    public TMP_Text tapText;          // 「タップでスタート」(点滅させる)
    public float tapBlinkSpeed = 1.6f;

    [Header("Prologue")]
    public CanvasGroup prologueCanvas;    // プロローグ用Canvas(最初はAlpha=0 / 非表示)
    public VideoPlayer prologuePlayer;    // VideoPlayer (PlayOnAwake = false)
    public AudioSource prologueAudio;     // 動画の音(任意)

    [Header("Fade (黒板)")]
    public CanvasGroup fade;          // 黒パネル（CanvasGroup）/ 無ければ空でOK
    public float fadeInTime = 0.6f;   // 起動時：黒→表示
    public float fadeOutTime = 0.5f;  // 遷移時：表示→黒

    [Header("Next")]
    public string nextSceneName = "Main"; // プロローグの後に開くシーン

    float blinkT;
    bool starting;

    void Awake()
    {
        if (prologueCanvas) { prologueCanvas.alpha = 0f; prologueCanvas.gameObject.SetActive(false); }
        if (fade) { fade.alpha = 1f; fade.blocksRaycasts = true; }
    }

    void Start()
    {
        // フェードインしてタイトルを見せる
        if (titleCanvas && fade) StartCoroutine(FadeTo(fade, 0f, fadeInTime));
    }

    void Update()
    {
        if (!starting && tapText)
        {
            blinkT += Time.unscaledDeltaTime * tapBlinkSpeed;
            float a = Mathf.Lerp(0.25f, 1f, (Mathf.Sin(blinkT) * 0.5f + 0.5f));
            var c = tapText.color; c.a = a; tapText.color = c;
        }

        if (starting) return;

        // クリック / タップ / 任意キー
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.anyKeyDown)
            StartPrologue();
    }

    void StartPrologue()
    {
        if (starting) return;
        starting = true;
        StartCoroutine(CoStartPrologue());
    }

    System.Collections.IEnumerator CoStartPrologue()
    {
        // タイトルを黒で隠す
        if (fade) yield return FadeTo(fade, 1f, fadeOutTime);

        // タイトルを消す、プロローグCanvasを出す
        if (titleCanvas) titleCanvas.gameObject.SetActive(false);
        if (prologueCanvas)
        {
            prologueCanvas.gameObject.SetActive(true);
            prologueCanvas.alpha = 1f;
        }

        // 黒を外す（プロローグを見せる）
        if (fade) yield return FadeTo(fade, 0f, 0.3f);

        // 動画再生
        if (prologuePlayer)
        {
            // 念のためオーディオの配線
            if (prologuePlayer.audioOutputMode == VideoAudioOutputMode.AudioSource && prologueAudio)
            {
                if (!prologuePlayer.GetTargetAudioSource(0))
                    prologuePlayer.SetTargetAudioSource(0, prologueAudio);
            }

            bool ended = false;
            prologuePlayer.loopPointReached += _ => ended = true;
            prologuePlayer.Play();
            if (prologueAudio) prologueAudio.Play();

            // 終わるまで待つ（安全バックアップ：最大10分）
            float timeout = Time.time + 600f;
            while (!ended && Time.time < timeout) yield return null;
        }

        // 終了：黒で隠してから次のシーンへ
        if (fade) yield return FadeTo(fade, 1f, 0.4f);
        SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
    }

    System.Collections.IEnumerator FadeTo(CanvasGroup cg, float target, float dur)
    {
        if (!cg) yield break;
        float from = cg.alpha;
        float t = 0f;
        cg.blocksRaycasts = true;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
            cg.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        cg.alpha = target;
        cg.blocksRaycasts = target > 0.5f;
        cg.interactable   = target > 0.5f;
    }
}
