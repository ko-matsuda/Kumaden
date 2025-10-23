using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// Main.Scene 内で、プロローグ動画→終わったらインゲーム(ingameRoot)を有効化する。
/// 何も配線できなくても「プロローグ非表示→ingameRoot表示」だけで動く最小実装。
[DisallowMultipleComponent]
public class PrologueInMain : MonoBehaviour
{
    [Header("Prologue UI (任意)")]
    public CanvasGroup prologueCanvas;   // プロローグのCanvasGroup（表示/非表示）
    public RawImage    prologueScreen;   // 画面に出すRawImage（任意）

    [Header("Video (任意)")]
    public VideoPlayer prologuePlayer;   // プロローグVideo（PlayOnAwake=OFF推奨）
    public AudioSource prologueAudio;    // その音（任意）

    [Header("In-Game Root")]
    public GameObject ingameRoot;        // インゲーム全体の親 (これをONにして開始)

    [Header("Fade (任意)")]
    public CanvasGroup fade;             // 黒フェードがあれば
    public float fadeTime = 0.4f;

    [Header("Options")]
    public bool allowSkip = true;        // タップでスキップ
    public float maxVideoWaitSec = 600f; // 安全上限(10分)

    void Start()
    {
        // 最初は ingame を隠し、プロローグを出す（Canvasが無いならスキップ）
        if (ingameRoot) ingameRoot.SetActive(false);
        if (prologueCanvas)
        {
            prologueCanvas.gameObject.SetActive(true);
            prologueCanvas.alpha = 1f;
        }

        StartCoroutine(RunPrologueThenStartGame());
    }

    System.Collections.IEnumerator RunPrologueThenStartGame()
    {
        // フェードアウトしてプロローグを見せる
        if (fade) yield return FadeTo(0f, fadeTime);

        bool ended = false;

        // Video 再生
        if (prologuePlayer)
        {
            prologuePlayer.loopPointReached += _ => ended = true;

            // Audio の接続（あれば）
            if (prologuePlayer.audioOutputMode == VideoAudioOutputMode.AudioSource && prologueAudio)
            {
                try { prologuePlayer.SetTargetAudioSource(0, prologueAudio); } catch {}
                prologueAudio.Play();
            }

            prologuePlayer.Play();
        }
        else
        {
            // Videoが無ければ即終了扱い
            ended = true;
        }

        // 再生待ち／スキップ
        float timeout = Time.time + maxVideoWaitSec;
        while (!ended && Time.time < timeout)
        {
            if (allowSkip && (Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.anyKeyDown))
                break;
            yield return null;
        }

        // プロローグを閉じる
        if (fade) yield return FadeTo(1f, fadeTime); // 黒で隠す

        if (prologuePlayer && prologuePlayer.isPlaying) prologuePlayer.Stop();
        if (prologueAudio && prologueAudio.isPlaying) prologueAudio.Stop();

        if (prologueCanvas) prologueCanvas.gameObject.SetActive(false);

        // インゲーム開始
        if (ingameRoot) ingameRoot.SetActive(true);

        if (fade) yield return FadeTo(0f, fadeTime); // 黒を外す
    }

    System.Collections.IEnumerator FadeTo(float target, float dur)
    {
        if (!fade) yield break;
        float from = fade.alpha, t = 0f;
        fade.blocksRaycasts = true;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
            fade.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        fade.alpha = target;
        fade.blocksRaycasts = target > 0.5f;
        fade.interactable   = target > 0.5f;
    }
}
