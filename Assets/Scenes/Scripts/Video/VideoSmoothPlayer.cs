using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoSmoothPlayer : MonoBehaviour
{
    [Header("参照")]
    public VideoPlayer video;   // VideoPlayer をドラッグ
    public RawImage   target;   // 画面に出す RawImage（UI）

    [Header("再生設定")]
    public string fileNameInStreamingAssets = "Prologue/intro.mp4"; // ← サブフォルダ込みに
    public bool   playOnStart = true;
    public bool   useVideoAudio = false;    // 動画の音を使う？（OFF推奨）
    public AudioSource audioOut;            // useVideoAudio=true のとき必須

    [Header("安定化オプション")]
    public bool   waitForPrepare = true;    // 再生前にデコード準備を待つ
    public bool   waitForFirstFrame = true; // 最初のフレーム到着を待つ
    public bool   skipOnDrop = true;        // 重い時はフレームを捨てて追従
    public bool   matchAppFrameRate = true; // アプリFPSを動画FPSに合わせる
    public int    fallbackTargetFPS = 60;   // 取得できない時のターゲットFPS

    [Header("縦動画サポート")]
    public bool autoRotatePortraitIfNeeded = true; // 横向き報告でも縦に見せたい時に自動回転

    RenderTexture rt;

    void Reset()
    {
        video  = GetComponent<VideoPlayer>();
        target = FindObjectOfType<RawImage>();
    }

    IEnumerator Start()
    {
        if (!playOnStart) yield break;
        yield return Play();
    }

    public IEnumerator Play()
    {
        if (!video || !target)
        {
            Debug.LogError("[VideoSmoothPlayer] VideoPlayer/RawImage が未設定です。");
            yield break;
        }

        // 1) 動画ソース（StreamingAssetsのURLを使う）
        video.source = VideoSource.Url;
        video.url    = System.IO.Path.Combine(Application.streamingAssetsPath, fileNameInStreamingAssets);

        // 2) 音声
        if (useVideoAudio)
        {
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            if (!audioOut) audioOut = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            video.EnableAudioTrack(0, true);
            video.SetTargetAudioSource(0, audioOut);
            audioOut.loop = false;
        }
        else
        {
            video.audioOutputMode = VideoAudioOutputMode.None;
        }

        // 3) 安定化設定
        video.waitForFirstFrame = waitForFirstFrame;
        video.skipOnDrop        = skipOnDrop;
        video.isLooping         = false;

        // 4) まず Prepare（ここで正しい width/height を得る）
        if (waitForPrepare)
        {
            video.Prepare();
            while (!video.isPrepared)
                yield return null;
        }

        // 5) 正しいサイズで RenderTexture 作成（←ここが重要）
        int w = (int)video.width;
        int h = (int)video.height;
        if (rt == null || rt.width != w || rt.height != h)
        {
            if (rt) { rt.Release(); Destroy(rt); }
            rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            rt.Create();
        }
        video.renderMode    = VideoRenderMode.RenderTexture;
        video.targetTexture = rt;
        target.texture      = rt;

        // 6) 縦横の自動補正（回転タグの不一致を吸収）
        target.rectTransform.localEulerAngles = Vector3.zero;
        if (autoRotatePortraitIfNeeded)
        {
            // 例：ファイルは横(1920x1080)だが実際は縦で見せたいパターン
            if (h < w) // 横長報告
            {
                target.rectTransform.localEulerAngles = new Vector3(0, 0, 90);
                // 見た目の比率を維持する
                AddOrUpdateAspectFitter(target, (float)h / (float)w);
            }
            else
            {
                AddOrUpdateAspectFitter(target, (float)w / (float)h);
            }
        }

        // 7) アプリFPSを動画FPSに合わせる（可能なら）
        if (matchAppFrameRate)
        {
            int targetFps = fallbackTargetFPS;
            if (video.frameRate > 1.0f && video.frameRate < 145.0f)
                targetFps = Mathf.Clamp(Mathf.RoundToInt((float)video.frameRate), 24, 144);

            Application.targetFrameRate = targetFps;
            QualitySettings.vSyncCount  = 1;
        }

        // 8) 再生
        video.Play();
        if (useVideoAudio && audioOut && !audioOut.isPlaying) audioOut.Play();

        // 9) 最初のフレーム待ち（チラ見え防止）
        if (waitForFirstFrame)
        {
            while (video.isPlaying && video.frame <= 0)
                yield return null;
        }
    }

    void AddOrUpdateAspectFitter(RawImage img, float aspect)
    {
        var fitter = img.GetComponent<AspectRatioFitter>();
        if (!fitter) fitter = img.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode  = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = Mathf.Max(0.01f, aspect);
    }

    void OnDestroy()
    {
        if (rt)
        {
            rt.Release();
            Destroy(rt);
        }
    }
}
