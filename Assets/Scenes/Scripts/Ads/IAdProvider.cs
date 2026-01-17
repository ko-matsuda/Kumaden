using System;

/// <summary>
/// 広告SDK統合用のインターフェース
/// AdMob、UnityAds、または他の広告SDKを実装可能
/// </summary>
public interface IAdProvider
{
    /// <summary>
    /// 広告SDKの初期化状態
    /// </summary>
    bool IsInitialized { get; }
    
    /// <summary>
    /// インタースティシャル広告がロード済みか
    /// </summary>
    bool IsAdReady { get; }
    
    /// <summary>
    /// 広告SDKを初期化
    /// </summary>
    void Initialize(Action onComplete);
    
    /// <summary>
    /// インタースティシャル広告をロード
    /// </summary>
    void LoadAd(Action onSuccess, Action<string> onFailure);
    
    /// <summary>
    /// インタースティシャル広告を表示
    /// </summary>
    /// <param name="onClosed">広告が閉じられた時のコールバック</param>
    /// <param name="onFailed">広告表示に失敗した時のコールバック</param>
    void ShowAd(Action onClosed, Action<string> onFailed);
}
