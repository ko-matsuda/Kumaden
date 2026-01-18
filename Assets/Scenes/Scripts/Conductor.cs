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
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
        
        musicSource = GetComponent<AudioSource>();
        
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.Stop();
        }
    }

    private void OnEnable()
    {
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
        enabled = true;
        
        if (!musicSource || musicSource.clip == null)
        {
            Debug.LogError("[Conductor] musicSource or clip is null!");
            return;
        }

        scheduled = false;
        
        _dspSongStartTime = AudioSettings.dspTime + startDelaySec;
        musicSource.Stop();
        musicSource.time = 0f;
        musicSource.PlayScheduled(_dspSongStartTime);
        scheduled = true;
    }

    public void ResetTiming()
    {
        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            float previousSongLength = audioSource.clip.length;
            _dspSongStartTime += previousSongLength;
        }
        
        scheduled = true;
    }

    private void OnDisable()
    {
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
            return;
        }

        songPositionSec = (float)(AudioSettings.dspTime - _dspSongStartTime);
        songPositionBeats = songPositionSec / secPerBeat - preRollBeats;
    }

    public AudioSource GetMusicSource() => musicSource;

    public double GetDspTimeForBeat(double beat)
    {
        return _dspSongStartTime + (preRollBeats + beat) * secPerBeat;
    }

    public double GetNearestBeat(double beatStep = 1.0)
    {
        double grid = Mathf.Max(0.0001f, (float)beatStep);
        return System.Math.Round(songPositionBeats / grid) * grid;
    }

    public double GetNextBeat(double beatStep = 1.0)
    {
        double grid = Mathf.Max(0.0001f, (float)beatStep);
        return System.Math.Ceiling(songPositionBeats / grid) * grid;
    }
}
