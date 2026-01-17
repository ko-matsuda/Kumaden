using UnityEngine;
using System;

// ============================================================================
// Unity Ads SDK統合用テンプレート
// ============================================================================
// 使用前の準備:
// 1. Unity Ads SDKをインストール
//    Package Manager → Unity Registry → Advertisements
//    または
//    Services → Ads → Enable
//
// 2. Unity Dashboard でプロジェクトを作成
//    https://dashboard.unity3d.com/
//
// 3. Game ID を取得（Android/iOS別々）
//
// 4. 下記のコメントアウトを解除してSDKをインポート
// ============================================================================

/*
using UnityEngine.Advertisements;
*/

/// <summary>
/// Unity Ads インタースティシャル広告プロバイダー
/// </summary>
public class UnityAdsProvider : IAdProvider // , IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    public bool IsInitialized { get; private set; }
    public bool IsAdReady { get; private set; }
    
    // Unity Ads Game ID（テスト用）
#if UNITY_ANDROID
    private const string GAME_ID = "4374881"; // Android テスト用
#elif UNITY_IOS
    private const string GAME_ID = "4374880"; // iOS テスト用
#else
    private const string GAME_ID = "unused";
#endif
    
    private const string AD_UNIT_ID = "Interstitial_Android"; // または "Interstitial_iOS"
    private const bool TEST_MODE = true; // 本番環境では false に
    
    private Action onAdClosedCallback;
    private Action<string> onAdFailedCallback;
    
    public void Initialize(Action onComplete)
    {
        Debug.Log("[UnityAdsProvider] Initializing Unity Ads SDK...");
        
        /*
        if (Advertisement.isInitialized)
        {
            Debug.Log("[UnityAdsProvider] Already initialized");
            IsInitialized = true;
            onComplete?.Invoke();
            return;
        }
        
        // Unity Ads 初期化
        Advertisement.Initialize(GAME_ID, TEST_MODE, this);
        
        // コールバックは OnInitializationComplete で処理
        */
        
        // SDKなしの場合の代替処理
        Debug.LogWarning("[UnityAdsProvider] Unity Ads SDK not installed - using fallback");
        IsInitialized = false;
        onComplete?.Invoke();
    }
    
    public void LoadAd(Action onSuccess, Action<string> onFailure)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[UnityAdsProvider] Not initialized");
            onFailure?.Invoke("Unity Ads not initialized");
            return;
        }
        
        Debug.Log("[UnityAdsProvider] Loading interstitial ad...");
        
        /*
        // 広告をロード
        Advertisement.Load(AD_UNIT_ID, this);
        */
        
        // SDKなしの場合
        onFailure?.Invoke("Unity Ads SDK not installed");
    }
    
    public void ShowAd(Action onClosed, Action<string> onFailed)
    {
        if (!IsAdReady)
        {
            Debug.LogWarning("[UnityAdsProvider] Ad not ready");
            onFailed?.Invoke("Ad not ready");
            return;
        }
        
        Debug.Log("[UnityAdsProvider] Showing interstitial ad");
        
        onAdClosedCallback = onClosed;
        onAdFailedCallback = onFailed;
        
        /*
        Advertisement.Show(AD_UNIT_ID, this);
        */
        
        // SDKなしの場合
        Debug.LogWarning("[UnityAdsProvider] Unity Ads SDK not installed");
        onFailed?.Invoke("Unity Ads SDK not installed");
    }
    
    // ========== Unity Ads コールバック ==========
    
    /*
    // 初期化完了
    public void OnInitializationComplete()
    {
        Debug.Log("[UnityAdsProvider] Initialization complete");
        IsInitialized = true;
        
        // 初回ロード
        LoadAd(
            onSuccess: () => Debug.Log("[UnityAdsProvider] Initial ad loaded"),
            onFailure: (error) => Debug.LogWarning($"[UnityAdsProvider] Initial load failed: {error}")
        );
    }
    
    // 初期化失敗
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"[UnityAdsProvider] Initialization failed: {error} - {message}");
        IsInitialized = false;
    }
    
    // ロード完了
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log($"[UnityAdsProvider] Ad loaded: {adUnitId}");
        IsAdReady = true;
    }
    
    // ロード失敗
    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"[UnityAdsProvider] Failed to load {adUnitId}: {error} - {message}");
        IsAdReady = false;
    }
    
    // 広告表示開始
    public void OnUnityAdsShowStart(string adUnitId)
    {
        Debug.Log($"[UnityAdsProvider] Ad show start: {adUnitId}");
    }
    
    // 広告をクリック
    public void OnUnityAdsShowClick(string adUnitId)
    {
        Debug.Log($"[UnityAdsProvider] Ad clicked: {adUnitId}");
    }
    
    // 広告表示完了
    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log($"[UnityAdsProvider] Ad show complete: {adUnitId}, state: {showCompletionState}");
        IsAdReady = false;
        onAdClosedCallback?.Invoke();
        
        // 次の広告をプリロード
        LoadAd(null, null);
    }
    
    // 広告表示失敗
    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"[UnityAdsProvider] Failed to show {adUnitId}: {error} - {message}");
        IsAdReady = false;
        onAdFailedCallback?.Invoke($"{error}: {message}");
        
        // 次の広告をロード
        LoadAd(null, null);
    }
    */
}
