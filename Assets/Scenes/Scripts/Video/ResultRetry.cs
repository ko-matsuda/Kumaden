using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// RETRYボタン用：黒→（動画/音 停止）→ シーン再ロードで完全リセット
[DisallowMultipleComponent]
public class ResultRetry : MonoBehaviour
{
    [Header("UI / Fade")]
    [Tooltip("黒フェード用 CanvasGroup（FadeCanvas を割り当て）")]
    public CanvasGroup fadeGroup;         // 省略可：未指定でも動く
    [Tooltip("フェードにかける秒数")]
    public float fadeSeconds = 0.25f;

    [Header("Optional")]
    [Tooltip("押下SEがあればここに")]
    public AudioSource clickSE;           // 省略可
    [Tooltip("必要なら一緒に止めたいAudioSource（BGMなど）")]
    public AudioSource[] extraAudiosToStop; // 省略可

    bool running;

    /// Button の OnClick にこれを割り当て
    public void OnRetryButton()
    {
        if (!running && gameObject.activeInHierarchy)
            StartCoroutine(RetryFlow());
    }

    IEnumerator RetryFlow()
    {
        running = true;

        // クリック音（任意）
        if (clickSE) { clickSE.ignoreListenerPause = true; clickSE.spatialBlend = 0f; clickSE.Play(); }

        // フェードイン（黒化）
        if (fadeGroup)
        {
            if (!fadeGroup.gameObject.activeSelf) fadeGroup.gameObject.SetActive(true);
            fadeGroup.blocksRaycasts = true;
            fadeGroup.interactable   = false;

            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / fadeSeconds));
                yield return null;
            }
            fadeGroup.alpha = 1f;
        }

        // 動画/音を確実に停止
        StopAllVideos();
        StopAudios();

        // 少しだけ待機（停止の反映）
        yield return new WaitForSecondsRealtime(0.05f);

        // 現在シーンを再ロード（完全リセット）
        var active = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(active, LoadSceneMode.Single);
    }

    void StopAllVideos()
    {
        var videos = FindVideos();
        foreach (var vp in videos)
        {
            if (!vp) continue;
            // Video の AudioSource も止める
            try
            {
                if (vp.audioOutputMode == VideoAudioOutputMode.AudioSource)
                {
                    // 0番だけでも止める（複数あっても最小実装）
                    var getTarget = vp.GetType().GetMethod("GetTargetAudioSource",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (getTarget != null)
                    {
                        var src = getTarget.Invoke(vp, new object[] { 0 }) as AudioSource;
                        if (src && src.isPlaying) src.Stop();
                    }
                }
            }
            catch { /* 失敗しても続行 */ }

            if (vp.isPlaying) vp.Stop();
            vp.targetTexture = vp.targetTexture; // RTの解放を遅延しないための小細工（安全）
        }
    }

    void StopAudios()
    {
        // 追加で止めたい音
        if (extraAudiosToStop != null)
        {
            foreach (var a in extraAudiosToStop)
                if (a && a.isPlaying) a.Stop();
        }

        // ResultCanvas 直下などのAudioSourceも止める（任意）
        var locals = GetComponentsInChildren<AudioSource>(true);
        foreach (var a in locals)
            if (a && a.isPlaying) a.Stop();
    }

    // Unity 6/2023 以降は FindObjectsByType、古い版は FindObjectsOfType
    static VideoPlayer[] FindVideos()
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        return Object.FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<VideoPlayer>();
#endif
    }
}
