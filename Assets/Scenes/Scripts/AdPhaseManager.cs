using UnityEngine;
using System;

public class AdPhaseManager : MonoBehaviour
{
    public static AdPhaseManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float adTimeout = 10f; // 広告タイムアウト（秒）
    
    private IAdProvider adProvider;
    private CanvasGroup adPanelCanvasGroup;
    private Action onAdCompleteCallback;
    private Coroutine timeoutCoroutine;
    private bool adInProgress = false;
    private bool panelVisible = false;
    
    private void Awake()
    {
        Instance = this;
        
        // Editor では MockAdProvider を優先的に使用
#if UNITY_EDITOR
        var mockProvider = GetComponent<MockAdProvider>();
        if (mockProvider != null)
        {
            adProvider = mockProvider;
            Debug.Log("[AdPhaseManager] Using MockAdProvider for Editor");
        }
        else
        {
            adProvider = GetComponent<IAdProvider>();
        }
#else
        // 実機では IAdProvider を取得（UnityAdsProvider または MockAdProvider）
        adProvider = GetComponent<IAdProvider>();
#endif
        
        if (adProvider == null)
        {
            Debug.LogError("[AdPhaseManager] IAdProvider component not found!");
        }
        
        // AdPanel を探す（非アクティブでも見つかる方法）
        var allCanvasGroups = Resources.FindObjectsOfTypeAll<CanvasGroup>();
        foreach (var cg in allCanvasGroups)
        {
            if (cg.gameObject.name == "AdPanel")
            {
                adPanelCanvasGroup = cg;
                break;
            }
        }
        
        if (adPanelCanvasGroup == null)
        {
            Debug.LogError("[AdPhaseManager] AdPanel CanvasGroup not found!");
        }
    }
    
    public void ShowAd(Action onComplete)
    {
        if (adProvider == null)
        {
            Debug.LogError("[AdPhaseManager] No ad provider");
            onComplete?.Invoke();
            return;
        }
        
        if (adPanelCanvasGroup == null)
        {
            Debug.LogError("[AdPhaseManager] No ad panel");
            onComplete?.Invoke();
            return;
        }
        
        onAdCompleteCallback = onComplete;
        adInProgress = true;
        
        // タイムアウトコルーチンを開始
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
        }
        timeoutCoroutine = StartCoroutine(AdTimeoutCoroutine());
        
        // 広告がロード済みならパネルをスキップ
        var adsProvider = adProvider as UnityAdsProvider;
        if (adsProvider != null && adsProvider.IsAdReady)
        {
            Debug.Log("[AdPhaseManager] Ad already loaded, showing immediately");
            adProvider.ShowAd(() => OnAdClosed(), (error) => OnAdFailed(error));
        }
        else
        {
            Debug.Log("[AdPhaseManager] Ad not ready, showing loading panel");
            StartCoroutine(FadeInPanel());
            adProvider.ShowAd(() => OnAdClosed(), (error) => OnAdFailed(error));
        }
        
        Debug.Log($"[AdPhaseManager] ShowAd called, timeout set to {adTimeout} seconds");
    }
    
    private System.Collections.IEnumerator AdTimeoutCoroutine()
    {
        yield return new WaitForSeconds(adTimeout);
        
        if (adInProgress)
        {
            Debug.LogWarning($"[AdPhaseManager] Ad timed out after {adTimeout} seconds, skipping...");
            OnAdTimeout();
        }
    }
    
    private void OnAdTimeout()
    {
        if (!adInProgress) return;
        
        adInProgress = false;
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }
        
        if (panelVisible)
        {
            StartCoroutine(FadeOutPanelAndCallback());
        }
        else
        {
            onAdCompleteCallback?.Invoke();
            onAdCompleteCallback = null;
        }
    }
    
    private void OnAdClosed()
    {
        if (!adInProgress) return;
        
        adInProgress = false;
        
        // タイムアウトコルーチンをキャンセル
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }
        
        Debug.Log("[AdPhaseManager] Ad closed successfully");
        
        if (panelVisible)
        {
            StartCoroutine(FadeOutPanelAndCallback());
        }
        else
        {
            onAdCompleteCallback?.Invoke();
            onAdCompleteCallback = null;
        }
    }
    
    private void OnAdFailed(string error)
    {
        if (!adInProgress) return;
        
        adInProgress = false;
        
        // タイムアウトコルーチンをキャンセル
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }
        
        Debug.LogWarning($"[AdPhaseManager] Ad failed: {error}");
        
        if (panelVisible)
        {
            StartCoroutine(FadeOutPanelAndCallback());
        }
        else
        {
            onAdCompleteCallback?.Invoke();
            onAdCompleteCallback = null;
        }
    }
    
    private System.Collections.IEnumerator FadeInPanel()
    {
        adPanelCanvasGroup.gameObject.SetActive(true);
        panelVisible = true;
        
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            adPanelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        
        adPanelCanvasGroup.alpha = 1f;
        adPanelCanvasGroup.interactable = false;
        adPanelCanvasGroup.blocksRaycasts = true;
    }
    
    private System.Collections.IEnumerator FadeOutPanelAndCallback()
    {
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            adPanelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        
        adPanelCanvasGroup.alpha = 0f;
        adPanelCanvasGroup.interactable = false;
        adPanelCanvasGroup.blocksRaycasts = false;
        adPanelCanvasGroup.gameObject.SetActive(false);
        panelVisible = false;
        
        onAdCompleteCallback?.Invoke();
        onAdCompleteCallback = null;
    }
}