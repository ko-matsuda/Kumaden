using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class Pickup : MonoBehaviour
{
    [Header("Basic Pickup")]
    public UnityEvent onEnter;

    [Header("SE（Player の AudioSource を割り当て）")]
    public AudioSource seSource;
    public AudioClip sePerfect;
    public AudioClip seGood;

    [Header("判定幅（Z距離）")]
    public float perfectRangeZ = 1f;
    public float goodRangeZ = 2f;

    [Header("微調整（前面からのオフセット +前 / -後）")]
    public float judgeOffsetFromFront = 0.4f;

    [Header("Hold Note")]
    public HoldTickPulse holdTickPulse;

    [Header("カウンター")]
    public HudCounterBinder hudCounterBinder;
    public ComboProbe comboProbe;

    [Header("判定表示")]
    public TextMeshProUGUI judgeText;
    public float judgeDisplayTime = 0.5f;

    [Header("Debug")]
    public bool debugLog = true;

    private float judgeDisplayTimer = 0f;

    void Start()
    {
        // 自動的に JudgeText を探す
        if (judgeText == null)
        {
            var judgeTextObj = GameObject.Find("JudgeText");
            if (judgeTextObj != null)
            {
                judgeText = judgeTextObj.GetComponent<TextMeshProUGUI>();
                Debug.Log("[Pickup] Auto-found JudgeText");
            }
        }
    }

    void Update()
    {
        // 判定表示のタイマー
        if (judgeDisplayTimer > 0)
        {
            judgeDisplayTimer -= Time.deltaTime;
            if (judgeDisplayTimer <= 0 && judgeText != null)
            {
                judgeText.text = "";
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (debugLog) Debug.Log($"[Pickup] OnTriggerEnter: {other.name}, Tag={other.tag}");

        // ホールド開始
        if (other.CompareTag("LinkedHoldStart"))
        {
            if (holdTickPulse != null)
            {
                holdTickPulse.StartTick();
                if (debugLog) Debug.Log("[Pickup] StartTick called");
            }
            PlayJudgedSE(other.transform);
            return;
        }

        // ホールド終了
        if (other.CompareTag("LinkedHoldEnd"))
        {
            if (holdTickPulse != null)
            {
                holdTickPulse.StopTick();
                if (debugLog) Debug.Log("[Pickup] StopTick called");
            }
            PlayJudgedSE(other.transform);
            return;
        }

        // 通常ノーツ
        PlayJudgedSE(other.transform);
        CountUpNormal();
        onEnter?.Invoke();
    }

    void PlayJudgedSE(Transform noteTransform)
    {
        float noteZ = noteTransform.position.z;
        float playerZ = transform.position.z + judgeOffsetFromFront;
        float distance = Mathf.Abs(noteZ - playerZ);

        if (debugLog) Debug.Log($"[Pickup] Judge: noteZ={noteZ:F2}, playerZ={playerZ:F2}, distance={distance:F2}");

        if (distance <= perfectRangeZ)
        {
            // PERFECT
            if (seSource != null && sePerfect != null)
            {
                seSource.PlayOneShot(sePerfect);
            }
            ShowJudge("PERFECT");
            if (debugLog) Debug.Log("[Pickup] PERFECT!");
        }
        else if (distance <= goodRangeZ)
        {
            // GOOD
            if (seSource != null && seGood != null)
            {
                seSource.PlayOneShot(seGood);
            }
            ShowJudge("GOOD");
            if (debugLog) Debug.Log("[Pickup] GOOD!");
        }
        else
        {
            // MISS
            ShowJudge("MISS");
            if (debugLog) Debug.Log("[Pickup] MISS!");
        }
    }

    void ShowJudge(string text)
    {
        if (judgeText != null)
        {
            judgeText.text = text;
            judgeDisplayTimer = judgeDisplayTime;
        }
    }

    void CountUpNormal()
    {
        if (hudCounterBinder != null)
        {
            hudCounterBinder.OnNormalNote();
        }
        if (comboProbe != null)
        {
            comboProbe.OnNormalNote();
        }
    }
}