using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Conductor : MonoBehaviour
{
    public static Conductor Instance { get; private set; }

    [Header("基本設定")]
    public float bpm = 163f;
    public float preRollBeats = 4f;
    public double startDelaySec = 0.27;

    private AudioSource musicSource;
    private double _dspSongStartTime;
    private bool scheduled;

    public float songPositionSec { get; private set; }
    public float songPositionBeats { get; private set; }
    public float secPerBeat => 60f / Mathf.Max(1f, bpm);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        musicSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (!musicSource || musicSource.clip == null)
        {
            Debug.LogError("[Conductor] AudioSource.clip 未設定");
            enabled = false; return;
        }

        _dspSongStartTime = AudioSettings.dspTime + startDelaySec;
        musicSource.playOnAwake = false;
        musicSource.Stop();
        musicSource.time = 0f;
        musicSource.PlayScheduled(_dspSongStartTime);
        scheduled = true;
    }

    private void Update()
    {
        if (!scheduled) return;

        songPositionSec = (float)(AudioSettings.dspTime - _dspSongStartTime);
        songPositionBeats = songPositionSec / secPerBeat - preRollBeats;
    }

    public AudioSource GetMusicSource() => musicSource;

    // ===== ここからSEスナップ用ユーティリティ =====

    /// <summary>この曲の「beat」に相当する正確な DSP 時刻を返す（beat=0 はカウントイン直後）</summary>
    public double GetDspTimeForBeat(double beat)
    {
        return _dspSongStartTime + (preRollBeats + beat) * secPerBeat;
    }

    /// <summary>現在ビートに最も近い整数拍（または指定の量で切り方変更）</summary>
    public double GetNearestBeat(double beatStep = 1.0)
    {
        // beatStep=1 なら四分、0.5なら8分、0.25なら16分にスナップ
        double grid = Mathf.Max(0.0001f, (float)beatStep);
        return System.Math.Round(songPositionBeats / grid) * grid;
    }

    /// <summary>「次のグリッド拍」を返す（今より先の方へスナップしたい時）</summary>
    public double GetNextBeat(double beatStep = 1.0)
    {
        double grid = Mathf.Max(0.0001f, (float)beatStep);
        return System.Math.Ceiling(songPositionBeats / grid) * grid;
    }
}
