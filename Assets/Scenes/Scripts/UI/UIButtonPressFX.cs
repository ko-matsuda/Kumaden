using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// ボタンの「へこみ＆バウンド」アニメ。Button と同じオブジェクトに付けるだけ。
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonPressFX : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Scale")]
    [Range(0.7f, 1f)] public float pressedScale = 0.92f;
    [Range(0.01f, 0.3f)] public float pressDuration = 0.08f;
    [Tooltip("指を離した時の軽いバウンド量")]
    [Range(0f, 0.2f)] public float bounce = 0.06f;
    [Range(0.01f, 0.3f)] public float releaseDuration = 0.10f;

    [Header("Optional")]
    public AudioSource clickSE; // クリック音(任意)

    RectTransform rt;
    Vector3 baseScale;
    Button btn;
    Coroutine animCo;
    bool isPressed;

    void Awake()
    {
        rt = transform as RectTransform;
        baseScale = rt.localScale;
        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClickPunch); // キーボード決定でもバウンドさせる
    }

    void OnEnable()
    {
        // 途中で無効化された場合のリセット
        if (rt) rt.localScale = baseScale;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!btn || !btn.interactable) return;
        isPressed = true;
        PlayScale(baseScale * pressedScale, pressDuration, easeOut: false);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!btn || !btn.interactable) return;
        isPressed = false;
        // 指を離した時に少し弾ませて戻す
        PlayRelease();
        if (clickSE) clickSE.Play();
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (isPressed) // 押しっぱなしで外に出たら元に戻す
        {
            isPressed = false;
            PlayScale(baseScale, pressDuration, easeOut: true);
        }
    }

    void OnClickPunch() // ボタンの onClick からも呼ばれる
    {
        if (!isActiveAndEnabled) return;
        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(PunchCo());
    }

    void PlayRelease()
    {
        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(ReleaseCo());
    }

    void PlayScale(Vector3 target, float dur, bool easeOut)
    {
        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(ScaleCo(target, dur, easeOut));
    }

    IEnumerator ScaleCo(Vector3 target, float dur, bool easeOut)
    {
        Vector3 from = rt.localScale;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / dur);
            // イーズ（簡易）：easeOut ならスムーズに減速、そうでなければ加速寄り
            float k = easeOut ? 1f - Mathf.Pow(1f - u, 3f) : u * u;
            rt.localScale = Vector3.LerpUnclamped(from, target, k);
            yield return null;
        }
        rt.localScale = target;
        animCo = null;
    }

    IEnumerator ReleaseCo()
    {
        // 1) 押し縮み→ちょいオーバー(バウンド)  2) 基本スケールへ
        Vector3 over = baseScale * (1f + bounce);
        yield return ScaleCo(over, releaseDuration * 0.6f, easeOut: false);
        yield return ScaleCo(baseScale, releaseDuration * 0.4f, easeOut: true);
    }

    IEnumerator PunchCo()
    {
        // onClick時にも軽いパンチ
        Vector3 over = baseScale * (1f + bounce * 0.5f);
        yield return ScaleCo(over, 0.06f, easeOut: false);
        yield return ScaleCo(baseScale, 0.06f, easeOut: true);
    }
}
