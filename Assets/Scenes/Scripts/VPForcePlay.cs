using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VPForcePlay : MonoBehaviour
{
    public CanvasGroup cg;          // InterstitialCanvas の CanvasGroup
    public RawImage screen;         // VideoScreen (RawImage)
    public VideoPlayer vp;          // VideoPlayer
    public AudioSource videoAudio;  // 動画の音
    public RenderTexture rt;        // VideoRT_1080x1920
    public KeyCode hotkey = KeyCode.F5;

    void Awake()
    {
        if (cg == null) cg = GetComponent<CanvasGroup>();
        if (vp == null) vp = GetComponent<VideoPlayer>();

        if (rt != null)
        {
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = rt;
            if (screen != null) screen.texture = rt;
        }
        if (screen != null) screen.color = Color.white;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.sortingOrder = 999;

        LogState("[Awake]");
    }

    void OnEnable()
    {
        if (vp == null) return;
        vp.prepareCompleted += OnPrepared;
        vp.started += OnStarted;
        vp.loopPointReached += OnEnded;
        vp.errorReceived += OnError;
    }

    void OnDisable()
    {
        if (vp == null) return;
        vp.prepareCompleted -= OnPrepared;
        vp.started -= OnStarted;
        vp.loopPointReached -= OnEnded;
        vp.errorReceived -= OnError;
    }

    void Update()
    {
        if (Input.GetKeyDown(hotkey))
        {
            StartCoroutine(ForcePlay());
        }
    }

    IEnumerator ForcePlay()
    {
        // 他のVideoPlayerを停止（重複防止）
        foreach (var other in FindObjectsOfType<VideoPlayer>())
        {
            if (other != vp) other.Stop();
        }

        if (cg != null) cg.alpha = 1f;
        if (screen != null) screen.enabled = true;

        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        if (videoAudio != null) vp.SetTargetAudioSource(0, videoAudio);

        vp.Prepare();
        Debug.Log("[VP] prepare...");
        while (!vp.isPrepared) yield return null;

        vp.Play();
        if (videoAudio != null) videoAudio.Play();
        Debug.Log("[VP] started");

        while (vp.isPlaying) yield return null;

        vp.Stop();
        if (videoAudio != null) videoAudio.Stop();
        if (cg != null) cg.alpha = 0f;
        if (screen != null) screen.enabled = false;
        Debug.Log("[VP] stop");
    }

    void OnPrepared(VideoPlayer _)
    {
        Debug.Log("[VP] prepared, length=" + vp.length + "s");
        LogState("[Prepared]");
    }

    void OnStarted(VideoPlayer _)
    {
        Debug.Log("[VP] started event");
    }

    void OnEnded(VideoPlayer _)
    {
        Debug.Log("[VP] end");
    }

    void OnError(VideoPlayer _, string err)
    {
        Debug.LogError("[VP] ERROR: " + err);
    }

    void LogState(string tag)
    {
        string texName = (screen != null && screen.texture != null) ? screen.texture.name : "null";
        string rtName = (vp != null && vp.targetTexture != null) ? vp.targetTexture.name : "null";
        float alpha = (cg != null) ? cg.alpha : -1f;
        int sort = -1;
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) sort = canvas.sortingOrder;

        Debug.Log(tag + " [UI] alpha=" + alpha + ", sort=" + sort + ", screenTex=" + texName);
        Debug.Log(tag + " [VP] clip=" + (vp != null ? vp.clip : null) + ", mode=" + (vp != null ? vp.renderMode.ToString() : "null") + ", rt=" + rtName);
    }
}
