using UnityEngine;
using System;
using System.Collections;

public class MockAdProvider : MonoBehaviour, IAdProvider
{
    [Header("Mock Settings")]
    [SerializeField, Range(1f, 10f)] private float adDuration = 3f;
    
    public bool IsInitialized { get; private set; }
    public bool IsAdReady { get; private set; }
    
    private Action currentOnClosed;
    private Action<string> currentOnFailed;
    
    private void Awake()
    {
        Initialize(null);
    }
    
    public void Initialize(Action onComplete)
    {
        IsInitialized = true;
        IsAdReady = true;
        onComplete?.Invoke();
    }
    
    public void LoadAd(Action onSuccess, Action<string> onFailure)
    {
        IsAdReady = true;
        onSuccess?.Invoke();
    }
    
    public void ShowAd(Action onClosed, Action<string> onFailed)
    {
        if (!IsAdReady)
        {
            onFailed?.Invoke("Ad not ready");
            return;
        }
        
        if (currentOnClosed != null)
        {
            Debug.LogWarning("[MockAdProvider] Ad already playing");
            return;
        }
        
        currentOnClosed = onClosed;
        currentOnFailed = onFailed;
        
        StartCoroutine(PlayAdSequence());
    }
    
    private IEnumerator PlayAdSequence()
    {
#if UNITY_EDITOR
        Debug.Log($"[MockAdProvider] Ad started (duration: {adDuration}s)");
#endif
        
        yield return new WaitForSeconds(adDuration);
        
#if UNITY_EDITOR
        Debug.Log("[MockAdProvider] Ad completed");
#endif
        
        IsAdReady = false;
        
        var callback = currentOnClosed;
        currentOnClosed = null;
        currentOnFailed = null;
        
        callback?.Invoke();
        
        LoadAd(null, null);
    }
}
