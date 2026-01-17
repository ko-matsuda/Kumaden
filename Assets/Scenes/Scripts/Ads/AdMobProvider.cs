using UnityEngine;
using System;

// ============================================================================
// AdMob SDK統合用テンプレート
// ============================================================================
// 使用前の準備:
// 1. Google Mobile Ads SDKをインストール
//    Package Manager → Add package from git URL
//    https://github.com/googleads/googleads-mobile-unity.git
// 
// 2. AdMobアカウントでアプリを作成し、App IDを取得
//    https://apps.admob.com/
//
// 3. Project Settings → Google Mobile Ads で App ID を設定
//
// 4. 下記のコメントアウトを解除してSDKをインポート
// ============================================================================

/*
using GoogleMobileAds;
using GoogleMobileAds.Api;
*/

/// <summary>
/// AdMob インタースティシャル広告プロバイダー
/// </summary>
public class AdMobProvider : IAdProvider
{
    public bool IsInitialized { get; private set; }
    public bool IsAdReady { get; private set; }
    
    // AdMob広告ユニットID（テスト用）
    // 本番環境では実際の広告ユニットIDに置き換える
#if UNITY_ANDROID
    private const string AD_UNIT_ID = "ca-app-pub-3940256099942544/1033173712"; // Android テスト用
#elif UNITY_IOS
    private const string AD_UNIT_ID = "ca-app-pub-3940256099942544/4411468910"; // iOS テスト用
#else
    private const string AD_UNIT_ID = "unused";
#endif
    
    // private InterstitialAd interstitialAd;
    private Action onAdClosedCallback;
    
    public void Initialize(Action onComplete)
    {
        Debug.Log("[AdMobProvider] Initializing AdMob SDK...");
        
        /*
        // AdMob SDK初期化
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[AdMobProvider] AdMob SDK initialized");
            IsInitialized = true;
            onComplete?.Invoke();
            
            // 初回ロード
            LoadAd(
                onSuccess: () => Debug.Log("[AdMobProvider] Initial ad loaded"),
                onFailure: (error) => Debug.LogWarning($"[AdMobProvider] Initial load failed: {error}")
            );
        });
        */
        
        // SDKなしの場合の代替処理
        Debug.LogWarning("[AdMobProvider] AdMob SDK not installed - using fallback");
        IsInitialized = false;
        onComplete?.Invoke();
    }
    
    public void LoadAd(Action onSuccess, Action<string> onFailure)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[AdMobProvider] Not initialized");
            onFailure?.Invoke("AdMob not initialized");
            return;
        }
        
        Debug.Log("[AdMobProvider] Loading interstitial ad...");
        
        /*
        // リクエスト作成
        AdRequest request = new AdRequest.Builder().Build();
        
        // インタースティシャル広告をロード
        InterstitialAd.Load(AD_UNIT_ID, request, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError($"[AdMobProvider] Failed to load ad: {error.GetMessage()}");
                IsAdReady = false;
                onFailure?.Invoke(error.GetMessage());
                return;
            }
            
            Debug.Log("[AdMobProvider] Interstitial ad loaded");
            interstitialAd = ad;
            IsAdReady = true;
            
            // イベント登録
            RegisterAdEvents(ad);
            
            onSuccess?.Invoke();
        });
        */
        
        // SDKなしの場合
        onFailure?.Invoke("AdMob SDK not installed");
    }
    
    public void ShowAd(Action onClosed, Action<string> onFailed)
    {
        /*
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            Debug.Log("[AdMobProvider] Showing interstitial ad");
            onAdClosedCallback = onClosed;
            interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("[AdMobProvider] Ad not ready to show");
            onFailed?.Invoke("Ad not ready");
            
            // 次の広告をロード
            LoadAd(null, null);
        }
        */
        
        // SDKなしの場合
        Debug.LogWarning("[AdMobProvider] AdMob SDK not installed");
        onFailed?.Invoke("AdMob SDK not installed");
    }
    
    /*
    private void RegisterAdEvents(InterstitialAd ad)
    {
        // 広告が閉じられた時
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdMobProvider] Ad closed");
            IsAdReady = false;
            onAdClosedCallback?.Invoke();
            
            // 次の広告をプリロード
            LoadAd(null, null);
        };
        
        // 広告表示に失敗した時
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"[AdMobProvider] Ad failed to show: {error.GetMessage()}");
            IsAdReady = false;
            
            // 次の広告をロード
            LoadAd(null, null);
        };
    }
    */
}
