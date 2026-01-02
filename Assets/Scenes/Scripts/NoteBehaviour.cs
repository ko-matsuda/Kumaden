using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// リズムノーツ本体（既存コード互換）
/// </summary>
public class NoteBehaviour : MonoBehaviour
{
    // ===== 旧タイプとの互換 =====
    public enum LegacyNoteType { Milk, Flour, Egg, Unknown }

    [Header("Type / Compat")]
    [Tooltip("外部コード（Pickup/Spawner 等）が参照する実タイプ")]
    public IngredientType Type = IngredientType.Milk;

    [Tooltip("旧コードから参照される場合の互換用")]
    public LegacyNoteType LegacyType = LegacyNoteType.Milk;

    // ===== レーン / 移動 / 判定 =====
    [Header("Lane / Move / Judge")]
    [Tooltip("0=Left, 1=Middle, 2=Right")]
    public int laneIndex = 0;

    [Tooltip("Zマイナス(手前)へ進むスクロール速度")]
    public float scrollSpeed = 12f;

    [Tooltip("判定ラインのZ")]
    public float judgeZ = 0f;

    [Tooltip("判定ライン通過後に残す距離（視覚的余韻）")]
    public float lingerDistance = 3f;

    // ===== 状態管理 =====
    public enum State { Idle, Picked, Holding, Finished }
    public State state { get; private set; } = State.Idle;

    // ===== イベント =====
    [Header("Events")]
    public UnityEvent onSpawn;     // 生成時
    public UnityEvent onPickup;    // 最初に当たった瞬間
    public UnityEvent onHoldTick;  // ホールド中フレーム毎
    public UnityEvent onRelease;   // 終端で離れた瞬間
    public UnityEvent onMiss;      // ミス時

    // ===== オプション =====
    [Header("Visual & Options")]
    public Transform visual;
    public float holdScorePerSecond = 0f;
    public float autoDisableDelay = 0.1f;
    public float hitRadius = 0.45f;

    // 内部
    float _holdAccum;
    bool  _judged;    // TryMarkJudged 済み
    float _zWhenPassedJudge = float.NaN;

    void Awake()
    {
        if (!visual) visual = transform;
        Type       = MapToIngredient(LegacyType);
        LegacyType = MapFromIngredient(Type);
    }

    void OnValidate()
    {
        LegacyType = MapFromIngredient(Type);
    }

    void OnEnable()
    {
        state = State.Idle;
        _holdAccum = 0f;
        _judged = false;
        _zWhenPassedJudge = float.NaN;
        onSpawn?.Invoke();
    }

    // ====== 旧↔新タイプ相互変換 ======
    static IngredientType MapToIngredient(LegacyNoteType t)
    {
        switch (t)
        {
            case LegacyNoteType.Milk:  return IngredientType.Milk;
            case LegacyNoteType.Flour: return IngredientType.Flour;
            case LegacyNoteType.Egg:   return IngredientType.Egg;
            default:                   return IngredientType.Milk;
        }
    }
    static LegacyNoteType MapFromIngredient(IngredientType it)
    {
        switch (it)
        {
            case IngredientType.Milk:  return LegacyNoteType.Milk;
            case IngredientType.Flour: return LegacyNoteType.Flour;
            case IngredientType.Egg:   return LegacyNoteType.Egg;
            default:                   return LegacyNoteType.Unknown;
        }
    }

    // ===== Spawner 互換 Init =====
    public void Init(float scrollSpeed, float judgeZ, float lingerDistance, int lane, IngredientType type)
    {
        this.scrollSpeed    = scrollSpeed;
        this.judgeZ         = judgeZ;
        this.lingerDistance = lingerDistance;
        this.laneIndex      = lane;
        this.Type           = type;
        this.LegacyType     = MapFromIngredient(type);
    }
    public void Init(float scrollSpeed, float judgeZ, float lingerDistance, int lane, LegacyNoteType legacyType)
    {
        this.scrollSpeed    = scrollSpeed;
        this.judgeZ         = judgeZ;
        this.lingerDistance = lingerDistance;
        this.laneIndex      = lane;
        this.LegacyType     = legacyType;
        this.Type           = MapToIngredient(legacyType);
    }

    // ===== 判定済みマーク =====
    public bool TryMarkJudged()
    {
        if (_judged) return false;
        _judged = true;
        return true;
    }

    // ===== 連続ヒット =====
    public void OnHoldTick(float dt)
    {
        if (state == State.Finished) return;

        if (state == State.Idle)
        {
            state = State.Picked;
            onPickup?.Invoke();
        }

        state = State.Holding;
        _holdAccum += dt;
        onHoldTick?.Invoke();

        if (holdScorePerSecond > 0f)
        {
            // 例: GameScore.Add(holdScorePerSecond * dt);
        }
    }

    // ===== 単発ヒット =====
    public void Pickup()
    {
        if (state == State.Finished) return;
        if (state == State.Idle)
        {
            state = State.Picked;
            onPickup?.Invoke();
        }
    }
    public void Hit()      => Pickup();
    public void OnHit()    => Pickup();
    public void OnPickup() => Pickup();
    public void HoldStay() => onHoldTick?.Invoke();

    // ===== 終了処理 =====
    public void Release()
    {
        if (state == State.Finished) return;
        state = State.Finished;
        onRelease?.Invoke();

        if (autoDisableDelay >= 0f)
            StartCoroutine(CoDisableAfter(autoDisableDelay));
    }

    IEnumerator CoDisableAfter(float t)
    {
        if (t > 0f) yield return new WaitForSeconds(t);
        gameObject.SetActive(false);
    }

public void Miss()
    {
        if (state == State.Finished) return;
        
        Debug.LogWarning($"[NoteBehaviour] MISS TRIGGERED! Lane={laneIndex}, Type={Type}");
        
        // Miss表示を強制的に呼び出す
        if (ScoreManagerLite.Instance != null)
        {
            Debug.LogWarning("[NoteBehaviour] Calling ScoreManagerLite.OnPick with MISS");
        }
        else
        {
            Debug.LogError("[NoteBehaviour] ScoreManagerLite.Instance is NULL!");
        }
        
        onMiss?.Invoke();
        Release();
    }

    // ===== 移動と自動クリーンアップ =====
    void Update()
    {
        if (state != State.Finished)
        {
            transform.position += Vector3.back * scrollSpeed * Time.deltaTime;
        }

        if (float.IsNaN(_zWhenPassedJudge) && transform.position.z <= judgeZ)
        {
            _zWhenPassedJudge = transform.position.z;
        }
        if (!float.IsNaN(_zWhenPassedJudge))
        {
            if (Mathf.Abs(_zWhenPassedJudge - transform.position.z) >= lingerDistance)
            {
                // 判定されずに通過した場合は MISS
                if (!_judged)
                {
                    // ScoreManagerLite に MISS を通知
                    ScoreManagerLite.Instance?.OnPick(Type, "MISS");
                    
                    // ComboProbe をリセット
                    var comboProbe = FindObjectOfType<ComboProbe>();
                    if (comboProbe != null)
                    {
                        comboProbe.ResetCombo();
                    }
                    
                    Debug.Log($"[NoteBehaviour] MISS - Note passed without being judged");
                    Miss();
                }
                else
                {
                    Release();
                }
            }
        }
    }

    // ===== デバッグ表示 =====
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}