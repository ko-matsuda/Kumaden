using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// リザルト動画を Rank に応じて出し分けるコントローラー
/// S なら Result.mp4、それ以外なら Result_Fail.mp4
/// </summary>
public class ResultVideoController : MonoBehaviour
{
    [Header("Video Player")]
    public VideoPlayer videoPlayer;
    
    [Header("Video Clips")]
    public VideoClip resultSuccess;  // Result.mp4
    public VideoClip resultFail;     // Result_Fail.mp4
    
    [Header("Result UI")]
    public GameObject resultCanvas;
    
    [Header("Rank (外部から設定される)")]
    public string rank = "";
    
    private bool videoPlayed = false;

void Start()
    {
        // 自動検索と設定
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }
        
        if (resultCanvas == null)
        {
            resultCanvas = GameObject.Find("ResultCanvas");
        }
        
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(false);
        }
        
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        
        Debug.Log($"[ResultVideoController] Initialized - videoPlayer={videoPlayer}, resultCanvas={resultCanvas}, resultSuccess={resultSuccess}, resultFail={resultFail}");
    }

    /// <summary>
    /// 外部から呼ばれる想定のメソッド
    /// </summary>
    public void PlayResultVideo()
    {
        if (videoPlayed) return;
        if (videoPlayer == null) return;
        
        // Rank が S なら Result.mp4、それ以外なら Result_Fail.mp4
        if (rank == "S")
        {
            videoPlayer.clip = resultSuccess;
        }
        else
        {
            videoPlayer.clip = resultFail;
        }
        
        videoPlayer.Play();
        videoPlayed = true;
    }

    /// <summary>
    /// 動画再生完了時のコールバック
    /// </summary>
    private void OnVideoFinished(VideoPlayer vp)
    {
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(true);
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
