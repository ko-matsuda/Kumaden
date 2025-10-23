using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// RETRY が反応しない根本（レイキャスト遮り）を無視して発火させる保険.
/// ResultCanvas にアタッチするだけで、ブロック解除＆強制クリック＆自動配線。
[DisallowMultipleComponent]
public class ResultRetryForce : MonoBehaviour
{
    [Header("Assign (空でも自動検出)")]
    public Button retryButton;              // Btn-Retry
    public CanvasGroup fadeGroup;           // FadeCanvas の CanvasGroup（任意）
    public ResultRetry retryImpl;           // 既存の実装があれば使う（任意）

    Canvas rootCanvas;
    GraphicRaycaster raycaster;

    void Reset()        { AutoFind(); }
    void Awake()        { AutoFind(); EnsureEventSystem(); UnblockFade(); }
    void OnEnable()     { UnblockFade(); WireRetry(); }
    void LateUpdate()   { UnblockFade(); }

    // —— 強制クリック（UIがブロックされていても発火） ——
    void Update()
    {
        if (!retryButton) return;

        bool released =
            Input.GetMouseButtonUp(0) ||
            (Input.touchCount > 0 && Input.touches.Any(t => t.phase == TouchPhase.Ended));

        if (!released) return;

        Vector2 pos = Input.touchCount > 0
            ? (Vector2)Input.touches.First(t => t.phase == TouchPhase.Ended).position
            : (Vector2)Input.mousePosition;

        var rt = retryButton.transform as RectTransform;
        if (rt && RectTransformUtility.RectangleContainsScreenPoint(rt, pos, rootCanvas ? rootCanvas.worldCamera : null))
        {
            // クリックがブロックされていても自前で実行
            OnRetryClicked();
        }
    }

    void OnRetryClicked()
    {
        // 既存の実装があればそれを使う
        if (retryImpl)
        {
            retryImpl.OnRetryButton();
            return;
        }

        // 簡易版：全動画/音停止 → 現シーン再ロード
        UnblockFade();
        StopAllVideosAndAudios();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    // —— 配線 & 解除 —— //
    void WireRetry()
    {
        if (!retryButton) return;
        retryButton.onClick.RemoveListener(OnRetryClicked);
        retryButton.onClick.AddListener(OnRetryClicked);

        // クリックが通るよう最低限の解除
        var cg = retryButton.GetComponentInParent<CanvasGroup>();
        if (cg) cg.interactable = true;
        retryButton.interactable = true;
    }

    void UnblockFade()
    {
        // FadeCanvasが生きていても入力は通す
        if (fadeGroup)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable   = false;
        }

        // ResultCanvas 内の全画面画像(Backdrop/Blackなど)の RaycastTarget をOFF
        if (!raycaster) return;
        var graphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
        {
            if (!g) continue;
            var name = g.name.ToLower();
            bool likelyOverlay = name.Contains("fade") || name.Contains("backdrop") || name.Contains("black");
            if (likelyOverlay && (g is Image || g is RawImage))
                g.raycastTarget = false;
        }
    }

    void AutoFind()
    {
        if (!retryButton)
        {
            retryButton = GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b =>
                {
                    var n = b.name.ToLower();
                    var t = b.GetComponentInChildren<TMPro.TMP_Text>(true);
                    return n.Contains("retry") || (t && t.text.ToLower().Contains("retry"));
                })
                ?? GetComponentsInChildren<Button>(true).FirstOrDefault();
        }

        if (!fadeGroup)
        {
            fadeGroup = FindObjectsOfType<CanvasGroup>(true)
                .OrderByDescending(g =>
                {
                    int score = 0;
                    if (g.name.ToLower().Contains("fade")) score += 100;
                    var cv = g.GetComponent<Canvas>();
                    if (cv && cv.overrideSorting) score += Mathf.Clamp(cv.sortingOrder, 0, 10000);
                    return score;
                })
                .FirstOrDefault();
        }

        if (!retryImpl) retryImpl = GetComponent<ResultRetry>();
        if (!raycaster) raycaster = GetComponentInParent<GraphicRaycaster>();
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
    }

    void EnsureEventSystem()
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var es = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (es == null || es.Length == 0)
#else
        var es = FindObjectsOfType<EventSystem>();
        if (es == null || es.Length == 0)
#endif
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
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
        foreach (var vp in videos) { if (vp && vp.isPlaying) vp.Stop(); }
        foreach (var a in audios)  { if (a && a.isPlaying)  a.Stop(); }
    }
}
