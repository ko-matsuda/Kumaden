using UnityEngine;
using UnityEngine.Video;

public class ResultCaller : MonoBehaviour
{
    [Header("動画プレイヤー(料理動画)")]
    public VideoPlayer videoPlayer;          // 料理シーンの VideoPlayer を入れる
    [Header("結果UI")]
    public ResultHUD resultHUD;              // ResultCanvas に付けた ResultHUD を入れる
    [Header("表示までの遅延(秒)")]
    public float delayAfterVideo = 0.35f;    // 黒フェード等の見栄え用
    [Header("バックアップ(動画が無い/失敗時)")]
    public float fallbackSeconds = 8f;       // 動画が無い時の保険タイマー

    bool fired;

    void Reset()
    {
        // 楽する自動参照（見つかれば勝手に入ります）
        if (!videoPlayer) videoPlayer = FindObjectOfType<VideoPlayer>(true);
        if (!resultHUD)   resultHUD   = FindObjectOfType<ResultHUD>(true);
    }

    void OnEnable()
    {
        Hook();
    }

    void OnDisable()
    {
        Unhook();
    }

    void Hook()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        else
        {
            // 動画が無い場合の保険：一定時間後に結果を出す
            Invoke(nameof(FallbackShow), fallbackSeconds);
        }
    }

    void Unhook()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
        CancelInvoke(nameof(FallbackShow));
    }

    void OnVideoFinished(VideoPlayer _)
    {
        if (fired) return;
        fired = true;
        Invoke(nameof(ShowResult), delayAfterVideo);
    }

    void FallbackShow()
    {
        if (fired) return;
        fired = true;
        ShowResult();
    }

    void ShowResult()
    {
        // 念のため停止やタイムスケールを正常化
        Time.timeScale = 1f;

        if (resultHUD == null)
        {
            resultHUD = FindObjectOfType<ResultHUD>(true);
        }

        if (resultHUD != null)
        {
            // 非表示でも ShowResult が内部で CanvasGroup を有効化します
            resultHUD.gameObject.SetActive(true);
            resultHUD.ShowResult();
        }
        else
        {
            Debug.LogError("[ResultCaller] ResultHUD が見つかりません。ResultCanvas に ResultHUD を付けてください。");
        }
    }
}
