using UnityEngine;

/// <summary>
/// ConductorのBGMが最後まで到達したら確実に Stop() する補助。
/// これで AutoTriggerResultAtSongEnd が「停止」を検知できるようになる。
/// </summary>
public class ConductorAutoStop : MonoBehaviour
{
    public float margin = 0.05f;  // 終端検知の余裕（秒）
    public bool debugLog = false;

    AudioSource src;
    AudioClip clip;
    bool stopped;

    void Start()
    {
        // Conductor の AudioSource を取得
        var c = FindObjectOfType<Conductor>();
        if (c) src = c.GetMusicSource();
        if (src) clip = src.clip;
        if (!src || !clip)
        {
            Debug.LogWarning("[ConductorAutoStop] ConductorのAudioSource/Clipが見つかりません。");
            enabled = false;
        }
    }

    void Update()
    {
        if (stopped || !src || !clip) return;

        // 再生中かつ、残り時間が margin 未満になったら Stop
        if (src.isPlaying && src.time >= Mathf.Max(0f, clip.length - margin))
        {
            src.Stop();          // ← これで AutoTrigger が isPlaying=false を検知できる
            stopped = true;
            if (debugLog) Debug.Log("[ConductorAutoStop] Stop at clip end.");
        }
    }
}
