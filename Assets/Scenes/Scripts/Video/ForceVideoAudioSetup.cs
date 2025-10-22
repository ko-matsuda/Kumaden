using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class ForceVideoAudioSetup : MonoBehaviour
{
    public AudioSource audioSource; // Inspectorでセット

    void Awake()
    {
        var vp = GetComponent<VideoPlayer>();
        if (audioSource != null)
        {
            // 強制設定
            vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
            if (vp.canSetDirectAudioVolume)
            {
                vp.SetDirectAudioMute(0, false);
            }
            try {
                vp.SetTargetAudioSource(0, audioSource);
            } catch { /* Unityのバージョン差対策 */ }
        }
        vp.playOnAwake = false;
        vp.isLooping = false;
    }
}
