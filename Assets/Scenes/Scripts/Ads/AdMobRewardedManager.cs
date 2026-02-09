using UnityEngine;
using System;
using System.Collections;

#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

/// <summary>
/// AdMob Rewarded 広告マネージャー（メディエーション対応）
/// 
/// SDK未導入時は Mock モードで動作する（3秒待機→コールバック）
/// 
/// 導入手順:
/// 1. Google Mobile Ads Unity Plugin をインストール
/// 2. Unity Ads Mediation Adapter をインストール  
/// 3. Player Settings → Scripting Define Symbols に "GOOGLE_MOBILE_ADS" を追加
/// 4. Assets → Google Mobile Ads → Settings で App ID を設定
/// </summary>
public class AdMobRewardedManager : MonoBehaviour
{
    public static AdMobRewardedManager Instance { get; private set; }

    [Header("Ad Unit IDs")]
    [SerializeField] private string androidTestAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    [SerializeField] private string androidProdAdUnitId = "ca-app-pub-6139222263584451/7909908747"; // ← 本番IDをInspectorで設定
    
    [Header("Settings")]
    [SerializeField] private bool useTestAds = true;
    [SerializeField] private float mockAdDuration = 3f; // Mock時の待機秒数
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
    private RewardedAd rewardedAd;
#endif

    private bool isInitialized = false;
    private bool isAdLoading = false;

    // コールバック
    private Action currentOnRewarded;
    private Action currentOnClosed;
    private Action<string> currentOnFailed;

    /// <summary>広告が表示可能か</summary>
    public bool IsAdReady
    {
        get
        {
#if GOOGLE_MOBILE_ADS
            return rewardedAd != null && rewardedAd.CanShowAd();
#else
            return isInitialized; // Mock: 常にready
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

    // ================================================================
    // 初期化
    // ================================================================
    private void Initialize()
    {
#if GOOGLE_MOBILE_ADS
        Debug.Log("[AdMobRewarded] Initializing Google Mobile Ads SDK...");
        MobileAds.Initialize(initStatus =>
        {
            isInitialized = true;
            Debug.Log("[AdMobRewarded] SDK Initialized");

            // アダプター状態ログ
            var statusMap = initStatus.getAdapterStatusMap();
            foreach (var kvp in statusMap)
            {
                Debug.Log($"[AdMobRewarded] Adapter: {kvp.Key} = {kvp.Value.InitializationState}");
            }

            LoadAd();
        });
#else
        Debug.Log("[AdMobRewarded] ⚠ Google Mobile Ads SDK未導入 - Mockモードで動作");
        Debug.Log("[AdMobRewarded] SDK導入後、Scripting Define Symbolsに GOOGLE_MOBILE_ADS を追加してください");
        isInitialized = true;
#endif
    }

    // ================================================================
    // 広告ロード
    // ================================================================
    public void LoadAd()
    {
#if GOOGLE_MOBILE_ADS
        if (!isInitialized || isAdLoading) return;

        // 既存の広告を破棄
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        isAdLoading = true;
        Debug.Log($"[AdMobRewarded] Loading ad: {AdUnitId}");

        var adRequest = new AdRequest();
        RewardedAd.Load(AdUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            isAdLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogError($"[AdMobRewarded] Load failed: {error}");
                Invoke(nameof(LoadAd), retryDelay);
                return;
            }

            Debug.Log("[AdMobRewarded] Ad loaded successfully");
            rewardedAd = ad;
            RegisterAdEvents(ad);
        });
#endif
    }

    // ================================================================
    // 広告表示
    // ================================================================

    /// <summary>
    /// リワード広告を表示する
    /// </summary>
    /// <param name="onRewarded">ユーザーが報酬を獲得した時</param>
    /// <param name="onClosed">広告が閉じられた時（報酬有無問わず）</param>
    /// <param name="onFailed">表示失敗時</param>
    public void ShowRewardedAd(Action onRewarded, Action onClosed = null, Action<string> onFailed = null)
    {
        currentOnRewarded = onRewarded;
        currentOnClosed = onClosed;
        currentOnFailed = onFailed;

#if GOOGLE_MOBILE_ADS
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            Debug.Log("[AdMobRewarded] Showing rewarded ad");
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log($"[AdMobRewarded] Reward earned: {reward.Amount} {reward.Type}");
                currentOnRewarded?.Invoke();
            });
        }
        else
        {
            Debug.LogWarning("[AdMobRewarded] Ad not ready");
            currentOnFailed?.Invoke("Ad not ready");
            ClearCallbacks();
            LoadAd();
        }
#else
        // Mock: コルーチンで模擬広告
        Debug.Log($"[AdMobRewarded] [MOCK] 模擬広告開始 ({mockAdDuration}秒)");
        StartCoroutine(MockAdCoroutine());
#endif
    }

    /// <summary>
    /// 簡易版: 広告を表示し、完了/失敗どちらでもコールバックを呼ぶ
    /// （既存の AdPhaseManager.ShowAd(Action) と互換）
    /// </summary>
    public void ShowAd(Action onComplete)
    {
        ShowRewardedAd(
            onRewarded: null,
            onClosed: onComplete,
            onFailed: (error) =>
            {
                Debug.LogWarning($"[AdMobRewarded] Ad failed, completing anyway: {error}");
                onComplete?.Invoke();
            }
        );
    }

    // ================================================================
    // Mock 広告（SDK未導入時）
    // ================================================================
#if !GOOGLE_MOBILE_ADS
    private IEnumerator MockAdCoroutine()
    {
        yield return new WaitForSeconds(mockAdDuration);

        Debug.Log("[AdMobRewarded] [MOCK] 模擬広告完了 - 報酬付与");
        currentOnRewarded?.Invoke();
        currentOnClosed?.Invoke();
        ClearCallbacks();
    }
#endif

    // ================================================================
    // AdMob イベントハンドラ
    // ================================================================
#if GOOGLE_MOBILE_ADS
    private void RegisterAdEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[AdMobRewarded] Ad opened fullscreen");
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdMobRewarded] Ad closed");
            currentOnClosed?.Invoke();
            ClearCallbacks();
            LoadAd(); // 次の広告をプリロード
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"[AdMobRewarded] Show failed: {error}");
            currentOnFailed?.Invoke(error.GetMessage());
            ClearCallbacks();
            LoadAd();
        };

        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("[AdMobRewarded] Impression recorded");
        };

        ad.OnAdPaid += (AdValue adValue) =>
        {
            float revenue = adValue.Value / 1_000_000f;
            Debug.Log($"[AdMobRewarded] Revenue: {revenue:F6} {adValue.CurrencyCode}");
        };
    }
#endif

    private void ClearCallbacks()
    {
        currentOnRewarded = null;
        currentOnClosed = null;
        currentOnFailed = null;
    }

    private void OnDestroy()
    {
#if GOOGLE_MOBILE_ADS
        rewardedAd?.Destroy();
#endif
    }
}