using System;
using UnityEngine;

/// <summary>
/// テスト用モック広告プロバイダー
/// 実際の広告SDKなしで動作確認可能
/// </summary>
public class MockAdProvider : MonoBehaviour, IAdProvider
{
    [SerializeField] private float mockAdDuration = 3f;
    
    public bool IsInitialized { get; private set; }
    public bool IsAdReady { get; private set; }
    
    public void Initialize(Action onComplete)
    {
        GameLogger.Log("[MockAdProvider] Initializing...");
        IsInitialized = true;
        IsAdReady = false;
        onComplete?.Invoke();
    }
    
    public void LoadAd(Action onSuccess, Action<string> onFailure)
    {
        GameLogger.Log("[MockAdProvider] Loading ad...");
        IsAdReady = true;
        onSuccess?.Invoke();
    }
    
    public void ShowAd(Action onClosed, Action<string> onFailed)
    {
        if (!IsAdReady)
        {
            GameLogger.Warning("[MockAdProvider] Ad not ready");
            onFailed?.Invoke("Ad not ready");
            return;
        }
        
        GameLogger.Log($"[MockAdProvider] Showing mock ad ({mockAdDuration}s)");
        
        var adManager = AdPhaseManager.Instance;
        if (adManager != null)
        {
            adManager.StartCoroutine(MockAdCoroutine(onClosed));
        }
        else
        {
            GameLogger.Error("[MockAdProvider] AdPhaseManager not found");
            onFailed?.Invoke("AdPhaseManager not found");
        }
    }
    
    private System.Collections.IEnumerator MockAdCoroutine(Action onClosed)
    {
        float elapsed = 0f;
        var adManager = AdPhaseManager.Instance;
        
        while (elapsed < mockAdDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            
            if (adManager != null)
            {
                float progress = Mathf.Clamp01(elapsed / mockAdDuration);
                adManager.UpdateAdProgress(progress);
            }
            
            yield return null;
        }
        
        GameLogger.Log("[MockAdProvider] Ad completed");
        IsAdReady = false;
        onClosed?.Invoke();
    }
}
