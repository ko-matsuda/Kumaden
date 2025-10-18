using UnityEngine;
using UnityEngine.Video;
using System.IO;

// VideoPlayer と AudioSource を自動で用意してくれる
[RequireComponent(typeof(VideoPlayer), typeof(AudioSource))]
public class PrologueVideo : MonoBehaviour
{
    void Start()
    {
        // 動画プレイヤーと音のスピーカーを取得
        var vp  = GetComponent<VideoPlayer>();
        var src = GetComponent<AudioSource>();

        // 音を AudioSource から出すように設定
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.SetTargetAudioSource(0, src);

        // 動画の場所を指定（Assets/StreamingAssets/Prologue/intro.mp4）
        var path = Path.Combine(Application.streamingAssetsPath, "Prologue", "intro.mp4");

        // Android 以外では "file://" をつける
        #if !UNITY_ANDROID
        path = "file://" + path;
        #endif

        vp.source = VideoSource.Url;
        vp.url    = path;

        // 事前に準備してから再生（カクつき防止）
        vp.prepareCompleted += _ => vp.Play();
        vp.Prepare();

        // 👇★追加ポイント★
        // 動画が終わった瞬間に呼ばれるイベント
        vp.loopPointReached += _ =>
        {
            // PrologueOverlay（黒いふた）を見つけて「Skip()」を呼ぶ
            var overlay = FindObjectOfType<PrologueOverlay>();
            if (overlay != null)
            {
                overlay.Skip();
            }
        };
    }
}
