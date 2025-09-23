using System.Collections;
using UnityEngine;

/// <summary>
/// ノーツの基本挙動（移動・寿命・見た目）
/// ・非接触の取り逃し時に MISS を一度だけ通知
/// </summary>
public class NoteBehaviour : MonoBehaviour
{
    [Header("移動・判定")]
    public float scrollSpeed = 12f;
    public float judgeZ = 0f;          // 判定ラインZ
    public float lingerDistance = 3f;  // 判定ラインを過ぎて何mで自壊するか
    public int laneIndex = 0;          // どのレーンで生成

    [Header("ノーツ種別（必須）")]
    [SerializeField] private IngredientType ingredient = IngredientType.Milk;
    public IngredientType Type => ingredient;

    [Header("見た目（任意）")]
    [SerializeField] private Renderer noteRenderer;
    [SerializeField] private Material highlightMat;

    // 判定済みガード
    private bool _judged = false;

    /// <summary>Spawner からの初期化</summary>
    public void Init(float speed, float judgeZ, float linger, int lane, IngredientType type)
    {
        this.scrollSpeed    = speed;
        this.judgeZ         = judgeZ;
        this.lingerDistance = linger;
        this.laneIndex      = lane;
        this.ingredient     = type;
    }

    private void Update()
    {
        // 手前（-Z）へ移動
        transform.position += Vector3.back * scrollSpeed * Time.deltaTime;

        // 判定線を十分越えたら「非接触MISS」
        if (transform.position.z <= judgeZ - lingerDistance)
        {
            if (!_judged)
            {
                _judged = true;
                var sm = ScoreManagerLite.Instance;
                if (sm != null) sm.OnPick(ingredient, "MISS");
            }
            Destroy(gameObject);
        }
    }

    /// <summary>未判定なら判定済みにして true（多重発火防止）</summary>
    public bool TryMarkJudged()
    {
        if (_judged) return false;
        _judged = true;
        return true;
    }

    /// <summary>
    /// 早すぎた入接触のときだけ判定済みを取り消して再挑戦させるためのフック。
    /// （Pickup からのみ使用想定）
    /// </summary>
    public void UnmarkJudgedForRetry()
    {
        _judged = false;
    }

    /// <summary>見た目ハイライト（任意）</summary>
    public void HighlightNote(float sec = 0.15f)
    {
        if (noteRenderer == null || highlightMat == null) return;
        StopAllCoroutines();
        StartCoroutine(CoHighlight(sec));
    }

    private IEnumerator CoHighlight(float sec)
    {
        var prev = noteRenderer.material;
        noteRenderer.material = highlightMat;
        yield return new WaitForSeconds(sec);
        noteRenderer.material = prev;
    }
}
