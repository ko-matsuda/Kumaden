using UnityEngine;
using System;

/// <summary>
/// 広告表示の中継役（後方互換用）
/// 実体は AdMobRewardedManager に委譲する
/// </summary>
public class AdPhaseManager : MonoBehaviour
{
    public static AdPhaseManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        Debug.Log("[AdPhaseManager] Initialized - delegates to AdMobRewardedManager");
    }

    public void ShowAd(Action onComplete)
    {
        if (AdMobRewardedManager.Instance != null)
        {
            Debug.Log("[AdPhaseManager] Delegating to AdMobRewardedManager");
            AdMobRewardedManager.Instance.ShowAd(onComplete);
            return;
        }

        Debug.LogWarning("[AdPhaseManager] AdMobRewardedManager not found, skipping ad");
        onComplete?.Invoke();
    }
}