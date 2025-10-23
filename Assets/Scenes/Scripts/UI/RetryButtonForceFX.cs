using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class RetryButtonForceFX : MonoBehaviour
{
    [Header("Targets (未設定なら自分)")]
    public RectTransform target;
    public AudioSource clickSE;

    [Header("Press FX")]
    [Range(0.7f, 1f)] public float pressedScale = 0.92f;
    [Range(0.01f, 0.3f)] public float pressDuration = 0.08f;
    [Range(0f, 0.2f)] public float bounce = 0.06f;
    [Range(0.01f, 0.3f)] public float releaseDuration = 0.10f;

    [Header("Restart")]
    public string ingameSceneName = "Main";   // 現在シーンと同じならそのまま再ロードでもOK

    RectTransform rt;
    Vector3 baseScale;
    bool isPressed;
    float animT;

    enum AnimState { None, ToPressed, ReleaseOver, ReleaseBack }
    AnimState anim = AnimState.None;

    void Reset() { rt = transform as RectTransform; target = rt; }
    void Awake()
    {
        rt = transform as RectTransform;
        if (!target) target = rt;
        baseScale = target.localScale;
        if (clickSE) { clickSE.spatialBlend = 0f; clickSE.loop = false; }
    }
    void OnEnable() { isPressed = false; anim = AnimState.None; target.localScale = baseScale; }

    void Update()
    {
        // 入力（矩形判定で強制）
        Vector2 pos; bool down=false, up=false;
        if (Input.touchCount > 0) { var t = Input.touches[0]; pos=t.position; down=t.phase==TouchPhase.Began; up=t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled; }
        else { pos = Input.mousePosition; down = Input.GetMouseButtonDown(0); up = Input.GetMouseButtonUp(0); }

        bool inside = RectTransformUtility.RectangleContainsScreenPoint(rt, pos, null);

        if (down && inside) { isPressed = true; anim = AnimState.ToPressed; animT = 0f; }
        if (up)
        {
            bool clicked = isPressed && inside; isPressed = false;
            anim = AnimState.ReleaseOver; animT = 0f;
            if (clicked) { if (clickSE) clickSE.Play(); FireRetry(); }
        }

        UpdateAnim(Time.unscaledDeltaTime);
    }

    void UpdateAnim(float dt)
    {
        switch (anim)
        {
            case AnimState.ToPressed:
                animT += dt / Mathf.Max(pressDuration, 0.01f);
                target.localScale = Vector3.LerpUnclamped(baseScale, baseScale * pressedScale, Mathf.Clamp01(animT)*Mathf.Clamp01(animT));
                if (animT >= 1f) anim = AnimState.None;
                break;
            case AnimState.ReleaseOver:
                animT += dt / Mathf.Max(releaseDuration * 0.6f, 0.01f);
                {
                    float u = 1f - Mathf.Pow(1f - Mathf.Clamp01(animT), 3f);
                    Vector3 over = baseScale * (1f + bounce);
                    target.localScale = Vector3.LerpUnclamped(baseScale * pressedScale, over, u);
                    if (animT >= 1f) { anim = AnimState.ReleaseBack; animT = 0f; }
                }
                break;
            case AnimState.ReleaseBack:
                animT += dt / Mathf.Max(releaseDuration * 0.4f, 0.01f);
                {
                    float u = 1f - Mathf.Pow(1f - Mathf.Clamp01(animT), 3f);
                    target.localScale = Vector3.LerpUnclamped(baseScale * (1f + bounce), baseScale, u);
                    if (animT >= 1f) anim = AnimState.None;
                }
                break;
        }
    }

    void FireRetry()
    {
        // 既存の ResultRetry があればそちらへ
        var rr = GetComponentInParent<ResultRetry>();
        if (rr != null) { rr.OnRetryButton(); return; }

        RestartIngame();
    }

    void RestartIngame()
    {
        StopAllVideosAndAudios();

        // ★ 次回ロード時にプロローグを1回だけスキップするフラグ
        PlayerPrefs.SetInt("SkipPrologueOnce", 1);
        PlayerPrefs.Save();

        if (string.IsNullOrEmpty(ingameSceneName))
        {
            var current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.name, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(ingameSceneName, LoadSceneMode.Single);
        }
    }

    void StopAllVideosAndAudios()
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var videos = FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None);
        var audios = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
#else
        var videos = FindObjectsOfType<VideoPlayer>();
        var audios = FindObjectsOfType<AudioSource>();
#endif
        foreach (var vp in videos) if (vp && vp.isPlaying) vp.Stop();
        foreach (var a in audios) if (a && a.isPlaying) a.Stop();
    }
}
