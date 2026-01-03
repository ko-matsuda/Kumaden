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
    private bool initialized;

    public float songPositionSec { get; private set; }
    public float songPositionBeats { get; private set; }
    public float secPerBeat => 60f / Mathf.Max(1f, bpm);

private void Awake()
    {
        // シングルトン：既存のインスタンスがあれば古い方を破棄
        if (Instance != null && Instance != this)
        {
            Debug.Log("[Conductor] Destroying old Conductor instance");
            Destroy(Instance.gameObject);
        }
        Instance = this;
        
        musicSource = GetComponent<AudioSource>();
        
        // 初期状態では再生しない
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.Stop();
        }
        
        Debug.Log($"[Conductor] Awake completed - Instance set, musicSource={musicSource}");
    }

    private void OnEnable()
    {
        // 既に初期化済みなら再開処理
        if (initialized && !scheduled)
        {
            StartMusic();
        }
    }

    private void Start()
    {
        if (!musicSource || musicSource.clip == null)
        {
            Debug.LogError("[Conductor] AudioSource.clip 未設定");
            enabled = false;
            return;
        }

        initialized = true;
        StartMusic();
    }

public void StartMusic()
    {
        Debug.Log("[Conductor] StartMusic called");
        Debug.Log($"[Conductor] scheduled={scheduled}, musicSource={musicSource}, clip={musicSource?.clip}");
        Debug.Log($"[Conductor] enabled={enabled}, gameObject.activeInHierarchy={gameObject.activeInHierarchy}");
        
        enabled = true;
        
        if (!musicSource || musicSource.clip == null)
        {
            Debug.LogError("[Conductor] musicSource or clip is null!");
            return;
        }

        var chartSpawner = FindObjectOfType<ChartSpawner>();
        if (chartSpawner != null)
        {
            Debug.Log("[Conductor] ChartSpawner found");
        }

        scheduled = false;
        
        _dspSongStartTime = AudioSettings.dspTime + startDelaySec;
        musicSource.Stop();
        musicSource.time = 0f;
        musicSource.PlayScheduled(_dspSongStartTime);
        scheduled = true;
        
        Debug.Log($"[Conductor] Music scheduled at DSP time: {_dspSongStartTime}, scheduled={scheduled}, enabled={enabled}");
    }

public void ResetTiming()
    {
        Debug.Log("[Conductor] ResetTiming called");
        
        // 前の曲の長さ分だけDSP時間を進める
        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            float previousSongLength = audioSource.clip.length;
            _dspSongStartTime += previousSongLength;
            
            Debug.Log($"[Conductor] DSP time adjusted by {previousSongLength}s, new startTime={_dspSongStartTime}");
        }
        
        scheduled = true;
    }


    private void OnDisable()
    {
        // 無効化されたら音楽を停止
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        scheduled = false;
        songPositionSec = 0f;
        songPositionBeats = -preRollBeats;
    }

private void Update()
    {
        if (!scheduled)
        {
            if (Time.frameCount % 60 == 0)
            {
                Debug.LogWarning($"[Conductor] Update - scheduled=false, not updating beats");
            }
            return;
        }

        songPositionSec = (float)(AudioSettings.dspTime - _dspSongStartTime);
        songPositionBeats = songPositionSec / secPerBeat - preRollBeats;
        
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[Conductor] Update - songPositionBeats={songPositionBeats:F2}, isPlaying={musicSource.isPlaying}");
        }
    }

    public AudioSource GetMusicSource() => musicSource;

    /// <summary>この曲の「beat」に相当する正確な DSP 時刻を返す</summary>
    public double GetDspTimeForBeat(double beat)
    {
        return _dspSongStartTime + (preRollBeats + beat) * secPerBeat;
    }

    /// <summary>現在ビートに最も近い整数拍</summary>
    public double GetNearestBeat(double beatStep = 1.0)
    {
        double grid = Mathf.Max(0.0001f, (float)beatStep);
        return System.Math.Round(songPositionBeats / grid) * grid;
    }

    /// <summary>「次のグリッド拍」を返す</summary>
    public double GetNextBeat(double beatStep = 1.0)
    {
        double grid = Mathf.Max(0.0001f, (float)beatStep);
        return System.Math.Ceiling(songPositionBeats / grid) * grid;
    }
}
