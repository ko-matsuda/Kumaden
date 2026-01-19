using UnityEngine;
using UnityEngine.Advertisements;
using System;

public class UnityAdsProvider : MonoBehaviour, IAdProvider, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [Header("Unity Ads Settings")]
    [SerializeField] private string androidGameId = "6028482";
    [SerializeField] private string iOSGameId = "6028483";
    [SerializeField] private bool testMode = true;
    
    [Header("Ad Unit IDs")]
    [SerializeField] private string androidAdUnitId = "Interstitial_Android";
    [SerializeField] private string iOSAdUnitId = "Interstitial_iOS";
    
    private string gameId;
    private string adUnitId;
    
    public bool IsInitialized { get; private set; }
    public bool IsAdReady { get; private set; }
    
    private Action currentOnClosed;
    private Action<string> currentOnFailed;
    
    private void Awake()
    {
        InitializeAds();
    }
    
    public void Initialize(Action onComplete)
    {
        if (IsInitialized)
        {
            onComplete?.Invoke();
            return;
        }
        
        InitializeAds();
        onComplete?.Invoke();
    }
    
    private void InitializeAds()
    {
#if UNITY_IOS
        gameId = iOSGameId;
        adUnitId = iOSAdUnitId;
#else
        gameId = androidGameId;
        adUnitId = androidAdUnitId;
#endif

        if (!Advertisement.isInitialized)
        {
            Debug.Log($"[UnityAds] Initializing with Game ID: {gameId}, Test Mode: {testMode}");
            Advertisement.Initialize(gameId, testMode, this);
        }
        else
        {
            IsInitialized = true;
            Debug.Log("[UnityAds] Already initialized");
        }
    }
    
    public void OnInitializationComplete()
    {
        IsInitialized = true;
        Debug.Log("[UnityAds] Initialization Complete");
        LoadAd(null, null);
    }
    
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        IsInitialized = false;
        Debug.LogError($"[UnityAds] Initialization Failed: {error} - {message}");
    }
    
    public void LoadAd(Action onSuccess, Action<string> onFailure)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[UnityAds] Not initialized yet");
            onFailure?.Invoke("Not initialized");
            return;
        }
        
        Debug.Log($"[UnityAds] Loading ad: {adUnitId}");
        Advertisement.Load(adUnitId, this);
    }
    
    public void ShowAd(Action onClosed, Action<string> onFailed)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[UnityAds] Cannot show ad - not initialized");
            onFailed?.Invoke("Not initialized");
            return;
        }
        
        if (!IsAdReady)
        {
            Debug.LogWarning("[UnityAds] Ad not loaded yet, loading now...");
            currentOnClosed = onClosed;
            currentOnFailed = onFailed;
            LoadAd(() => ShowAdInternal(), onFailed);
            return;
        }
        
        currentOnClosed = onClosed;
        currentOnFailed = onFailed;
        ShowAdInternal();
    }
    
    private void ShowAdInternal()
    {
        Debug.Log($"[UnityAds] Showing ad: {adUnitId}");
        Advertisement.Show(adUnitId, this);
    }
    
    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log($"[UnityAds] Ad Loaded: {placementId}");
        IsAdReady = true;
    }
    
    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"[UnityAds] Ad Failed to Load: {placementId} - {error} - {message}");
        IsAdReady = false;
        currentOnFailed?.Invoke($"Load failed: {message}");
        
        Invoke(nameof(RetryLoad), 5f);
    }
    
    private void RetryLoad()
    {
        Debug.Log("[UnityAds] Retrying ad load...");
        LoadAd(null, null);
    }
    
    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log($"[UnityAds] Ad Show Complete: {placementId} - {showCompletionState}");
        
        IsAdReady = false;
        
        var callback = currentOnClosed;
        currentOnClosed = null;
        currentOnFailed = null;
        
        callback?.Invoke();
        
        LoadAd(null, null);
    }
    
    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"[UnityAds] Ad Show Failed: {placementId} - {error} - {message}");
        
        IsAdReady = false;
        
        var failCallback = currentOnFailed;
        currentOnClosed = null;
        currentOnFailed = null;
        
        failCallback?.Invoke($"Show failed: {message}");
        
        LoadAd(null, null);
    }
    
    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.Log($"[UnityAds] Ad Show Start: {placementId}");
    }
    
    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.Log($"[UnityAds] Ad Clicked: {placementId}");
    }
}
