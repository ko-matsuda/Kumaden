using UnityEngine;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// VideoPlayerの古いフレームをクリアして新しい動画を再生
/// 前の動画のフレームが一瞬表示される問題を防ぐ
/// </summary>
public class VideoPlayerCleaner : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    /// <summary>
    /// VideoPlayerをクリアしてから新しい動画を再生
    /// </summary>
    public void PlayClean(VideoClip newClip)
    {
        if (videoPlayer == null) return;

        StartCoroutine(PlayCleanCoroutine(newClip));
    }

    IEnumerator PlayCleanCoroutine(VideoClip newClip)
    {
        // 1. 現在の再生を完全停止
        videoPlayer.Stop();
        
        // 2. VideoClipをnullにして古いフレームをクリア
        videoPlayer.clip = null;
        
        // 3. RenderTextureもクリア（もし使っている場合）
        if (videoPlayer.targetTexture != null)
        {
            RenderTexture rt = videoPlayer.targetTexture;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
        }

        // 4. 1フレーム待機してクリア完了を確認
        yield return null;

        // 5. 新しい動画を設定
        videoPlayer.clip = newClip;

        // 6. Prepare
        videoPlayer.Prepare();

        // 7. Prepare完了を待機
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        // 8. 再生開始
        videoPlayer.Play();

        Debug.Log($"[VideoPlayerCleaner] Playing new video: {newClip.name}");
    }

    /// <summary>
    /// URLから動画を再生する場合
    /// </summary>
    public void PlayCleanFromURL(string url)
    {
        if (videoPlayer == null) return;

        StartCoroutine(PlayCleanURLCoroutine(url));
    }

    IEnumerator PlayCleanURLCoroutine(string url)
    {
        videoPlayer.Stop();
        videoPlayer.url = null;
        
        if (videoPlayer.targetTexture != null)
        {
            RenderTexture rt = videoPlayer.targetTexture;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
        }

        yield return null;

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        videoPlayer.Play();

        Debug.Log($"[VideoPlayerCleaner] Playing URL: {url}");
    }
}
