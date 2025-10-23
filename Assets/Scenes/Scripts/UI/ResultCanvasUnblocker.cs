using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// ボタンが効かない原因（フェードのブロック/配線ミス/ES不足）を全部解決するブートストラップ。
/// ResultCanvas に付けるだけで OK。
[DisallowMultipleComponent]
public class ResultRetryBootstrap : MonoBehaviour
{
    [Header("Assign (空でも自動検出)")]
    public Button retryButton;                  // 「RETRY」ボタン
    public CanvasGroup fadeGroup;               // FadeCanvas の CanvasGroup
    public ResultRetry retryImpl;               // 既存の ResultRetry（なければ内部で簡易版を使う）

    [Header("Options")]
    public bool enforceEveryFrame = true;       // 毎フレーム、ブロック解除を維持
    public string retryButtonNameHint = "retry";// 検出用の名前ヒント（大文字小文字無視）

    void Reset()
    {
        AutoFindAll();
    }

    void Awake()
    {
        if (!retryImpl) retryImpl = GetComponent<ResultRetry>(); // あれば利用
        AutoFindAll();
        EnsureEventSystem();
        EnsureGraphicRaycaster();
        UnblockFade(); // 最初からクリック通す
        WireRetry();
    }

    void OnEnable()
    {
        UnblockFade();
        WireRetry();
    }

    void LateUpdate()
    {
        if (enforceEveryFrame) UnblockFade();
    }

    // ---- 配線 ----
    void WireRetry()
    {
        if (!retryButton)
        {
            Debug.LogWarning("[RetryBootstrap] Retry Button not found.");
            return;
        }

        // 二重登録を防ぐ
        retryButton.onClick.RemoveListener(OnRetryClicked);
        retryButton.onClick.AddListener(OnRetryClicked);

        // 念のため有効化
        retryButton.interactable = true;
        var cg = retryButton.GetComponentInParent<CanvasGroup>();
        if (cg) { cg.interactable = true; }

        Debug.Log("[RetryBootstrap] Retry wired.");
    }

    void OnRetryClicked()
    {
        // 既存の ResultRetry があればそれを使用
        if (retryImpl)
        {
            retryImpl.OnRetryButton();
            return;
        }

        // 簡易版：黒フェード解除 → 動画と音を止めて現シーン再ロード
        UnblockFade();
        StopAllVideosAndAudios();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    // ---- 検出 ----
    void AutoFindAll()
    {
        if (!retryButton)
        {
            retryButton = GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b =>
                {
                    // 名前かテキストに "retry" を含むものを優先
                    var nameHit = b.name.ToLower().Contains(retryButtonNameHint.ToLower());
                    var txt = b.GetComponentInChildren<TMPro.TMP_Text>(true);
                    var txtHit = txt && txt.text.ToLower().Contains(retryButtonNameHint.ToLower());
                    return nameHit || txtHit;
                })
                ?? GetComponentsInChildren<Button>(true).FirstOrDefault();
        }

        if (!fadeGroup)
        {
            // 名前に "fade" を含むものを最優先、sortingOrder が高い Canvas を優先
            fadeGroup = FindObjectsOfType<CanvasGroup>(true)
                .OrderByDescending(g =>
                {
                    int s = 0;
                    if (g.name.ToLower().Contains("fade")) s += 100;
                    var cv = g.GetComponent<Canvas>();
                    if (cv && cv.overrideSorting) s += Mathf.Clamp(cv.sortingOrder, 0, 10000);
                    return s;
                })
                .FirstOrDefault();
        }

        if (!retryImpl) retryImpl = GetComponent<ResultRetry>();
    }

    // ---- 入力ブロック解除 ----
    void UnblockFade()
    {
        if (!fadeGroup) return;
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable   = false;
        if (!fadeGroup.gameObject.activeSelf) fadeGroup.gameObject.SetActive(true);
    }

    // ---- 必須要素の補完 ----
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
            Debug.Log("[RetryBootstrap] EventSystem created.");
        }
    }

    void EnsureGraphicRaycaster()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas && !canvas.GetComponent<GraphicRaycaster>())
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log("[RetryBootstrap] GraphicRaycaster added to ResultCanvas.");
        }
    }

    // ---- 停止処理（簡易） ----
    void StopAllVideosAndAudios()
    {
        // Video 停止
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var videos = FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None);
        var audios = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
#else
        var videos = FindObjectsOfType<VideoPlayer>();
        var audios = FindObjectsOfType<AudioSource>();
#endif
        foreach (var vp in videos)
        {
            if (!vp) continue;
            try
            {
                if (vp.audioOutputMode == VideoAudioOutputMode.AudioSource)
                {
                    var getTarget = vp.GetType().GetMethod("GetTargetAudioSource",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (getTarget != null)
                    {
                        var src = getTarget.Invoke(vp, new object[] { 0 }) as AudioSource;
                        if (src && src.isPlaying) src.Stop();
                    }
                }
            }
            catch {}
            if (vp.isPlaying) vp.Stop();
        }
        foreach (var a in audios)
            if (a && a.isPlaying) a.Stop();
    }
}
