using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class PrologueVideo : MonoBehaviour
{
    [Header("動画クリップ")]
    public VideoClip videoClip;
    
    [Header("BGMフェード設定")]
    public float bgmLeadTime = 3.9f;
    
    private VideoPlayer vp;
    private PrologueOverlay overlay;
    private bool hasEnded = false;
    private bool bgmStarted = false;

    void Start()
    {
        vp = GetComponent<VideoPlayer>();
        
        if (videoClip == null)
        {
            Debug.LogError("[PrologueVideo] VideoClip が設定されていません");
            return;
        }
        
        // VideoClip を使用
        vp.source = VideoSource.VideoClip;
        vp.clip = videoClip;
        
        // Direct Audio を使用（バッファオーバーフロー対策）
        vp.audioOutputMode = VideoAudioOutputMode.Direct;
        vp.playbackSpeed = 1f;
        vp.isLooping = false;
        vp.skipOnDrop = false;

        overlay = FindObjectOfType<PrologueOverlay>();

        vp.prepareCompleted += OnPrepared;
        vp.loopPointReached += OnVideoEnd;
        
        vp.Prepare();
    }

    void OnPrepared(VideoPlayer source)
    {
        Debug.Log($"[PrologueVideo] Prepared. Duration: {vp.length} sec");
        vp.Play();
    }

    void Update()
    {
        if (vp == null || !vp.isPlaying || hasEnded) return;
        
        double fadeStartTime = vp.length - bgmLeadTime;
        if (!bgmStarted && vp.time >= fadeStartTime)
        {
            bgmStarted = true;
            Debug.Log($"[PrologueVideo] Starting BGM at {vp.time:F1} sec");
            if (overlay != null)
            {
                overlay.StartBgmFadeIn(bgmLeadTime);
            }
        }
    }

    void OnVideoEnd(VideoPlayer source)
    {
        if (hasEnded) return;
        hasEnded = true;
        
        Debug.Log("[PrologueVideo] Video ended");
        
        if (overlay != null)
        {
            overlay.OnVideoEnded();
        }
    }
}
