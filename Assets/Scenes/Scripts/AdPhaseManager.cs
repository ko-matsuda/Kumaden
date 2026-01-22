using UnityEngine;
using System;

public class AdPhaseManager : MonoBehaviour
{
    public static AdPhaseManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    private IAdProvider adProvider;
    private CanvasGroup adPanelCanvasGroup;
    private Action onAdCompleteCallback;
    
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
        StartCoroutine(FadeInPanel());
        adProvider.ShowAd(() => OnAdClosed(), (error) => OnAdFailed(error));
    }
    
    private void OnAdClosed()
    {
        StartCoroutine(FadeOutPanelAndCallback());
    }
    
    private void OnAdFailed(string error)
    {
        Debug.LogWarning($"[AdPhaseManager] Ad failed: {error}");
        StartCoroutine(FadeOutPanelAndCallback());
    }
    
    private System.Collections.IEnumerator FadeInPanel()
    {
        adPanelCanvasGroup.gameObject.SetActive(true);
        
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
        
        onAdCompleteCallback?.Invoke();
        onAdCompleteCallback = null;
    }
}
