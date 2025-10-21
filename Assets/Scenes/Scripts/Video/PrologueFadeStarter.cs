using System.Collections;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 画面を黒→透明にフェードインしてからプロローグ動画を再生する。
/// ・VideoPlayer 直制御 もしくは 既存のコントローラ(例: VideoSmoothPlayer)の Play() を呼ぶ
/// ・Time.timeScale=0 でも動くように WaitForSecondsRealtime を使用
/// </summary>
public class PrologueFadeStarter : MonoBehaviour
{
    [Header("参照（どちらかだけでOK）")]
    [Tooltip("Unity標準の VideoPlayer を直接制御する場合はこちらに割り当て")]
    public VideoPlayer videoPlayer;

    [Tooltip("独自の動画再生スクリプト(例: VideoSmoothPlayer)がある場合は、そのコンポーネントを割り当て")]
    public Behaviour videoController; // Play() メソッドがあれば呼び出します

    [Header("演出")]
    [Tooltip("フェードインにかける秒数")]
    public float fadeInSeconds = 0.8f;

    [Tooltip("フェードイン完了後、再生を始めるまでの待機(秒)")]
    public float delayAfterFade = 0.0f;

    private IEnumerator Start()
    {
        // 安全対策：フェード板が無ければ何もしない
        if (ScreenFader.Instance == null)
        {
            StartVideoNow();
            yield break;
        }

        // ① いったん黒にしてから…
        ScreenFader.Instance.gameObject.SetActive(true);
        ScreenFader.Instance.Alpha = 1f;

        // ② 黒→透明にフェードイン
        yield return ScreenFader.Instance.FadeIn(Mathf.Max(0.01f, fadeInSeconds));

        // ③ 少し待ってから動画再生（必要なら）
        if (delayAfterFade > 0f)
            yield return new WaitForSecondsRealtime(delayAfterFade);

        // ④ 再生開始
        StartVideoNow();
    }

    private void StartVideoNow()
    {
        // 優先度：videoController.Play() → videoPlayer.Play()
        bool played = false;

        if (videoController != null)
        {
            var t = videoController.GetType();
            var m = t.GetMethod("Play", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (m != null)
            {
                videoController.enabled = true;
                m.Invoke(videoController, null);
                played = true;
            }
        }

        if (!played && videoPlayer != null)
        {
            videoPlayer.Play();
        }
    }
}
