using UnityEngine;
using System;

/// <summary>
/// テスト用モック広告プロバイダー
/// 実際の広告SDKなしで動作確認可能
/// </summary>
public class MockAdProvider : IAdProvider
{
    public bool IsInitialized { get; private set; }
    public bool IsAdReady { get; private set; }
    
    private float mockAdDuration = 3f;
    
    public void Initialize(Action onComplete)
    {
        Debug.Log("[MockAdProvider] Initializing...");
        IsInitialized = true;
        IsAdReady = false;
        onComplete?.Invoke();
        Debug.Log("[MockAdProvider] Initialized");
    }
    
    public void LoadAd(Action onSuccess, Action<string> onFailure)
    {
        Debug.Log("[MockAdProvider] Loading ad...");
        
        // モック：即座にロード成功
        IsAdReady = true;
        onSuccess?.Invoke();
        Debug.Log("[MockAdProvider] Ad loaded successfully");
    }
    
    public void ShowAd(Action onClosed, Action<string> onFailed)
    {
        if (!IsAdReady)
        {
            Debug.LogWarning("[MockAdProvider] Ad not ready");
            onFailed?.Invoke("Ad not ready");
            return;
        }
        
        Debug.Log($"[MockAdProvider] Showing ad for {mockAdDuration} seconds...");
        
        // モック：Time.unscaledDeltaTimeを使用して時間経過をシミュレート
        var adManager = AdPhaseManager.Instance;
        if (adManager != null)
        {
            adManager.StartCoroutine(MockAdCoroutine(onClosed));
        }
    }
    
private System.Collections.IEnumerator MockAdCoroutine(Action onClosed)
    {
        float elapsed = 0f;
        var adManager = AdPhaseManager.Instance;
        
        while (elapsed < mockAdDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            
            // プログレスバー更新
            if (adManager != null)
            {
                float progress = Mathf.Clamp01(elapsed / mockAdDuration);
                adManager.UpdateAdProgress(progress);
            }
            
            yield return null;
        }
        
        Debug.Log("[MockAdProvider] Ad completed");
        IsAdReady = false;
        onClosed?.Invoke();
    }
}
