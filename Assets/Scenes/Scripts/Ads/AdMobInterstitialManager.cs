using UnityEngine;
using System;
using System.Collections;

#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

/// <summary>
/// AdMob インタースティシャル広告マネージャー
///
/// SDK未導入時は Mock モードで動作する（即コールバック）
///
/// 使用方法:
/// AdMobInterstitialManager.Instance.ShowInterstitialAd(onClosed: () => { /* 広告後の処理 */ });
/// </summary>
public class AdMobInterstitialManager : MonoBehaviour
{
    public static AdMobInterstitialManager Instance { get; private set; }

    [Header("Ad Unit IDs")]
    [SerializeField] private string androidTestAdUnitId = "ca-app-pub-3940256099942544/1033173712";
    [SerializeField] private string androidProdAdUnitId = "ca-app-pub-6139222263584451/4950483154"; // 美食街_3回リザルト後リプレイ用

    [Header("Settings")]
    [SerializeField] private bool useTestAds = true;
    [SerializeField] private float retryDelay = 5f;

    private string AdUnitId
    {
        get
        {
            if (useTestAds || string.IsNullOrEmpty(androidProdAdUnitId))
                return androidTestAdUnitId;
            return androidProdAdUnitId;
        }
    }

#if GOOGLE_MOBILE_ADS
    private InterstitialAd interstitialAd;
#endif

    private bool isInitialized = false;
    private bool isAdLoading = false;

    private Action currentOnClosed;
    private Action<string> currentOnFailed;

    /// <summary>広告が表示可能か</summary>
    public bool IsAdReady
    {
        get
        {
#if GOOGLE_MOBILE_ADS
            return interstitialAd != null && interstitialAd.CanShowAd();
#else
            return isInitialized;
#endif
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
#if GOOGLE_MOBILE_ADS
        Debug.Log("[AdMobInterstitial] Initializing Google Mobile Ads SDK...");
        MobileAds.Initialize(initStatus =>
        {
            isInitialized = true;
            Debug.Log("[AdMobInterstitial] SDK Initialized");
            LoadAd();
        });
#else
        Debug.Log("[AdMobInterstitial] SDK未導入 - Mockモードで動作");
        isInitialized = true;
#endif
    }

    public void LoadAd()
    {
#if GOOGLE_MOBILE_ADS
        if (!isInitialized || isAdLoading) return;

        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        isAdLoading = true;
        Debug.Log($"[AdMobInterstitial] Loading ad: {AdUnitId}");

        var adRequest = new AdRequest();
        InterstitialAd.Load(AdUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            isAdLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogError($"[AdMobInterstitial] Load failed: {error}");
                Invoke(nameof(LoadAd), retryDelay);
                return;
            }

            Debug.Log("[AdMobInterstitial] Ad loaded successfully");
            interstitialAd = ad;
            RegisterAdEvents(ad);
        });
#endif
    }

    /// <summary>
    /// インタースティシャル広告を表示する
    /// </summary>
    /// <param name="onClosed">広告が閉じられた時（広告非表示の場合も呼ばれる）</param>
    /// <param name="onFailed">表示失敗時</param>
    public void ShowInterstitialAd(Action onClosed = null, Action<string> onFailed = null)
    {
        currentOnClosed = onClosed;
        currentOnFailed = onFailed;

#if GOOGLE_MOBILE_ADS
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            Debug.Log("[AdMobInterstitial] Showing interstitial ad");
            interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("[AdMobInterstitial] Ad not ready - skipping");
            currentOnFailed?.Invoke("Ad not ready");
            currentOnClosed?.Invoke();
            ClearCallbacks();
            LoadAd();
        }
#else
        Debug.Log("[AdMobInterstitial] [MOCK] 模擬インタースティシャル（即完了）");
        StartCoroutine(MockAdCoroutine());
#endif
    }

#if !GOOGLE_MOBILE_ADS
    private IEnumerator MockAdCoroutine()
    {
        yield return null;
        Debug.Log("[AdMobInterstitial] [MOCK] 模擬広告完了");
        currentOnClosed?.Invoke();
        ClearCallbacks();
    }
#endif

#if GOOGLE_MOBILE_ADS
    private void RegisterAdEvents(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[AdMobInterstitial] Ad opened fullscreen");
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdMobInterstitial] Ad closed");
            currentOnClosed?.Invoke();
            ClearCallbacks();
            LoadAd();
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"[AdMobInterstitial] Show failed: {error}");
            currentOnFailed?.Invoke(error.GetMessage());
            currentOnClosed?.Invoke();
            ClearCallbacks();
            LoadAd();
        };

        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("[AdMobInterstitial] Impression recorded");
        };

        ad.OnAdPaid += (AdValue adValue) =>
        {
            float revenue = adValue.Value / 1_000_000f;
            Debug.Log($"[AdMobInterstitial] Revenue: {revenue:F6} {adValue.CurrencyCode}");
        };
    }
#endif

    private void ClearCallbacks()
    {
        currentOnClosed = null;
        currentOnFailed = null;
    }

    private void OnDestroy()
    {
#if GOOGLE_MOBILE_ADS
        interstitialAd?.Destroy();
#endif
    }
}