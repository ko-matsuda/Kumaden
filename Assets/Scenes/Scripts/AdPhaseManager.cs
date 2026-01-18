using UnityEngine;
using System;
using UnityEngine.UI;

public enum AdProviderType
{
    Mock,      // テスト用モック
    AdMob,     // Google AdMob
    UnityAds   // Unity Ads
}

/// <summary>
/// 広告表示と完全停止を管理するマネージャー
/// 3サイクル完了後に広告フェーズを挿入
/// </summary>
public class AdPhaseManager : MonoBehaviour
{
    [Header("Ad Provider")]
    [SerializeField] private AdProviderType adProviderType = AdProviderType.Mock;
    
    public static AdPhaseManager Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private GameObject hudContainer;
    [SerializeField] private bool showProgressBar = true;
    [SerializeField] private Image progressBarImage;
    [SerializeField] private CanvasGroup adPanelCanvasGroup;
    
    [Header("Settings")]
    [SerializeField] private float mockAdDuration = 3f;
    
    private IAdProvider adProvider;
    private bool isAdPlaying = false;
    private Action onAdCompleted;
    private Coroutine adTimerCoroutine;
    
    private void InitializeAdProvider()
    {
        GameLogger.Info($"[AdPhaseManager] Initializing: {adProviderType}");
        
        switch (adProviderType)
        {
            case AdProviderType.Mock:
                adProvider = new MockAdProvider();
                break;
            
            case AdProviderType.AdMob:
                adProvider = new AdMobProvider();
                break;
            
            case AdProviderType.UnityAds:
                adProvider = new UnityAdsProvider();
                break;
            
            default:
                GameLogger.Warning("[AdPhaseManager] Unknown provider, using Mock");
                adProvider = new MockAdProvider();
                break;
        }
        
        adProvider.Initialize(() => {
            GameLogger.Log("[AdPhaseManager] Provider initialized");
            adProvider.LoadAd(
                onSuccess: () => GameLogger.Log("[AdPhaseManager] Initial ad loaded"),
                onFailure: (error) => GameLogger.Warning($"[AdPhaseManager] Load failed: {error}")
            );
        });
    }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        InitializeAdProvider();
        
        if (adPanelCanvasGroup != null)
        {
            adPanelCanvasGroup.alpha = 0f;
            adPanelCanvasGroup.interactable = false;
            adPanelCanvasGroup.blocksRaycasts = false;
        }
    }
    
    /// <summary>
    /// 広告フェーズを開始（完全停止）
    /// </summary>
    public void ShowAd(Action onCompleted)
    {
        if (isAdPlaying)
        {
            GameLogger.Warning("[AdPhaseManager] Ad already playing");
            return;
        }
        
        if (adProvider == null)
        {
            GameLogger.Error("[AdPhaseManager] Provider not initialized");
            onCompleted?.Invoke();
            return;
        }
        
        isAdPlaying = true;
        onAdCompleted = onCompleted;
        
        GameLogger.Important("[AdPhaseManager] Ad phase started");
        
        FreezeGame();
        ShowAdPanel();
        
        adProvider.ShowAd(
            onClosed: () => {
                GameLogger.Log("[AdPhaseManager] Ad closed");
                OnAdFinished();
                adProvider.LoadAd(
                    onSuccess: () => GameLogger.Log("[AdPhaseManager] Next ad loaded"),
                    onFailure: (error) => GameLogger.Warning($"[AdPhaseManager] Reload failed: {error}")
                );
            },
            onFailed: (error) => {
                GameLogger.Error($"[AdPhaseManager] Ad failed: {error}");
                OnAdFinished();
                adProvider.LoadAd(null, null);
            }
        );
    }
    
    private void FreezeGame()
    {
        Time.timeScale = 0f;
        
        if (hudContainer != null)
        {
            hudContainer.SetActive(false);
        }
        
        var worldScroller = FindObjectOfType<WorldScroller>();
        if (worldScroller != null)
        {
            worldScroller.enabled = false;
        }
        
        GameLogger.Log("[AdPhaseManager] Game frozen");
    }
    
    private void UnfreezeGame()
    {
        Time.timeScale = 1f;
        
        var worldScroller = FindObjectOfType<WorldScroller>();
        if (worldScroller != null)
        {
            worldScroller.enabled = true;
        }
        
        GameLogger.Log("[AdPhaseManager] Game resumed");
    }
    
    private void ShowAdPanel()
    {
        if (adPanelCanvasGroup != null)
        {
            adPanelCanvasGroup.gameObject.SetActive(true);
            adPanelCanvasGroup.alpha = 1f;
            adPanelCanvasGroup.interactable = true;
            adPanelCanvasGroup.blocksRaycasts = true;
            
            if (showProgressBar && progressBarImage != null)
            {
                progressBarImage.fillAmount = 0f;
            }
        }
    }
    
    public void UpdateAdProgress(float progress)
    {
        if (showProgressBar && progressBarImage != null)
        {
            progressBarImage.fillAmount = progress;
        }
    }
    
    private void HideAdPanel()
    {
        if (adPanelCanvasGroup != null)
        {
            adPanelCanvasGroup.alpha = 0f;
            adPanelCanvasGroup.interactable = false;
            adPanelCanvasGroup.blocksRaycasts = false;
            adPanelCanvasGroup.gameObject.SetActive(false);
        }
    }
    
    private void OnAdFinished()
    {
        GameLogger.Important("[AdPhaseManager] Ad phase ended");
        
        adTimerCoroutine = null;
        
        HideAdPanel();
        UnfreezeGame();
        ScoreManagerLite.ResetSessionCount();
        
        isAdPlaying = false;
        onAdCompleted?.Invoke();
    }
    
    public void SkipAd()
    {
        if (!isAdPlaying) return;
        
        if (adTimerCoroutine != null)
        {
            StopCoroutine(adTimerCoroutine);
            adTimerCoroutine = null;
        }
        
        GameLogger.Log("[AdPhaseManager] Ad skipped");
        OnAdFinished();
    }
}
