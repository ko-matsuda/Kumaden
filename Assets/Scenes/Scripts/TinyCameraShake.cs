using UnityEngine;

public class TinyCameraShake : MonoBehaviour
{
    Vector3 baseLocalPos;
    float ampNow, timeLeft;

    void Awake()
    {
        baseLocalPos = transform.localPosition;
    }

    public void Kick(float amp = 0.03f, float time = 0.07f)
    {
        // 連続呼び出しでも強い方を優先
        ampNow = Mathf.Max(ampNow, amp);
        timeLeft = Mathf.Max(timeLeft, time);
    }

    void Update()
    {
        if (timeLeft > 0f) timeLeft -= Time.unscaledDeltaTime;
        else
        {
            transform.localPosition = baseLocalPos;
            ampNow = 0f;
        }
    }

    // 描画直前でオフセット適用（上書きされにくい）
    void OnPreCull()
    {
        if (timeLeft > 0f)
        {
            Vector2 r = Random.insideUnitCircle * ampNow;
            transform.localPosition = baseLocalPos + new Vector3(r.x, r.y, 0f);
        }
    }
}
