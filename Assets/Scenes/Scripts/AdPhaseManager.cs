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
        Debug.Log($"[AdPhaseManager] Initializing ad provider: {adProviderType}");
        
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
                Debug.LogWarning("[AdPhaseManager] Unknown ad provider type, using Mock");
                adProvider = new MockAdProvider();
                break;
        }
        
        // 広告SDK初期化
        adProvider.Initialize(() => {
            Debug.Log("[AdPhaseManager] Ad provider initialized");
            
            // 初回ロード
            adProvider.LoadAd(
                onSuccess: () => Debug.Log("[AdPhaseManager] Initial ad loaded"),
                onFailure: (error) => Debug.LogWarning($"[AdPhaseManager] Initial ad load failed: {error}")
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
        
        // 広告プロバイダーの初期化
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
            Debug.LogWarning("[AdPhaseManager] Ad is already playing");
            return;
        }
        
        if (adProvider == null)
        {
            Debug.LogError("[AdPhaseManager] Ad provider not initialized");
            onCompleted?.Invoke();
            return;
        }
        
        isAdPlaying = true;
        onAdCompleted = onCompleted;
        
        Debug.Log("[AdPhaseManager] ===== AD PHASE START =====");
        
        // 完全停止
        FreezeGame();
        
        // 広告UI表示
        ShowAdPanel();
        
        // 広告表示
        adProvider.ShowAd(
            onClosed: () => {
                Debug.Log("[AdPhaseManager] Ad closed by user/completed");
                OnAdFinished();
                
                // 次の広告をプリロード
                adProvider.LoadAd(
                    onSuccess: () => Debug.Log("[AdPhaseManager] Next ad loaded"),
                    onFailure: (error) => Debug.LogWarning($"[AdPhaseManager] Next ad load failed: {error}")
                );
            },
            onFailed: (error) => {
                Debug.LogError($"[AdPhaseManager] Ad failed to show: {error}");
                // 失敗しても続行
                OnAdFinished();
                
                // 次の広告をロード
                adProvider.LoadAd(null, null);
            }
        );
    }
    
    /// <summary>
    /// ゲームを完全停止
    /// </summary>
    private void FreezeGame()
    {
        // Time.timeScale を 0 に
        Time.timeScale = 0f;
        
        // インゲームHUD非表示
        if (hudContainer != null)
        {
            hudContainer.SetActive(false);
        }
        
        // WorldScrollerなどの動きを停止
        var worldScroller = FindObjectOfType<WorldScroller>();
        if (worldScroller != null)
        {
            worldScroller.enabled = false;
        }
        
        Debug.Log("[AdPhaseManager] Game frozen: Time.timeScale=0, HUD hidden");
    }
    
    /// <summary>
    /// ゲームを再開
    /// </summary>
    private void UnfreezeGame()
    {
        // Time.timeScale を 1 に
        Time.timeScale = 1f;
        
        // WorldScrollerを再開
        var worldScroller = FindObjectOfType<WorldScroller>();
        if (worldScroller != null)
        {
            worldScroller.enabled = true;
        }
        
        Debug.Log("[AdPhaseManager] Game unfrozen: Time.timeScale=1");
    }
    
    /// <summary>
    /// 広告パネル表示
    /// </summary>
    private void ShowAdPanel()
    {
        if (adPanelCanvasGroup != null)
        {
            adPanelCanvasGroup.gameObject.SetActive(true);
            adPanelCanvasGroup.alpha = 1f;
            adPanelCanvasGroup.interactable = true;
            adPanelCanvasGroup.blocksRaycasts = true;
            
            // プログレスバーをリセット
            if (showProgressBar && progressBarImage != null)
            {
                progressBarImage.fillAmount = 0f;
            }
        }
    }
    
    /// <summary>
    /// プログレスバー更新
    /// </summary>
    public void UpdateAdProgress(float progress)
    {
        if (showProgressBar && progressBarImage != null)
        {
            progressBarImage.fillAmount = progress;
        }
    }
    
    /// <summary>
    /// 広告パネル非表示
    /// </summary>
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
    
    /// <summary>
    /// 広告終了コールバック
    /// </summary>
    private void OnAdFinished()
    {
        Debug.Log("[AdPhaseManager] ===== AD PHASE END =====");
        
        adTimerCoroutine = null;
        
        HideAdPanel();
        UnfreezeGame();
        
        // セッションカウントをリセット（3サイクル完了）
        ScoreManagerLite.ResetSessionCount();
        
        isAdPlaying = false;
        
        // コールバック実行（次のサイクルへ）
        onAdCompleted?.Invoke();
    }
    
    /// <summary>
    /// スキップボタン用（デバッグ用）
    /// </summary>
    public void SkipAd()
    {
        if (!isAdPlaying) return;
        
        if (adTimerCoroutine != null)
        {
            StopCoroutine(adTimerCoroutine);
            adTimerCoroutine = null;
        }
        
        OnAdFinished();
    }
}
