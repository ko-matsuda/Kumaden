using UnityEngine;

/// <summary>
/// 条件付きログシステム
/// 開発環境では全ログ、本番環境ではエラー/警告のみ
/// </summary>
public static class GameLogger
{
    public enum LogLevel
    {
        None = 0,      // ログ無効
        Error = 1,     // エラーのみ
        Warning = 2,   // エラー + 警告
        Info = 3,      // エラー + 警告 + 情報
        Debug = 4      // 全ログ（デバッグ含む）
    }
    
    // ビルド設定に応じて自動切り替え
#if UNITY_EDITOR
    private static LogLevel currentLevel = LogLevel.Debug;  // エディタでは全ログ
#elif DEVELOPMENT_BUILD
    private static LogLevel currentLevel = LogLevel.Info;   // 開発ビルドでは情報まで
#else
    private static LogLevel currentLevel = LogLevel.Warning; // 本番では警告まで
#endif
    
    /// <summary>
    /// ログレベルを変更（Runtime調整用）
    /// </summary>
    public static void SetLogLevel(LogLevel level)
    {
        currentLevel = level;
        Log($"[GameLogger] Log level set to: {level}");
    }
    
    /// <summary>
    /// 現在のログレベルを取得
    /// </summary>
    public static LogLevel GetLogLevel()
    {
        return currentLevel;
    }
    
    /// <summary>
    /// デバッグログ（開発時のみ）
    /// </summary>
    public static void Log(string message)
    {
        if (currentLevel >= LogLevel.Debug)
        {
            Debug.Log(message);
        }
    }
    
    /// <summary>
    /// デバッグログ（オブジェクト指定）
    /// </summary>
    public static void Log(string message, Object context)
    {
        if (currentLevel >= LogLevel.Debug)
        {
            Debug.Log(message, context);
        }
    }
    
    /// <summary>
    /// 情報ログ（開発・デバッグビルドで表示）
    /// </summary>
    public static void Info(string message)
    {
        if (currentLevel >= LogLevel.Info)
        {
            Debug.Log($"<color=cyan>[INFO]</color> {message}");
        }
    }
    
    /// <summary>
    /// 警告ログ（常に表示推奨）
    /// </summary>
    public static void Warning(string message)
    {
        if (currentLevel >= LogLevel.Warning)
        {
            Debug.LogWarning(message);
        }
    }
    
    /// <summary>
    /// エラーログ（常に表示）
    /// </summary>
    public static void Error(string message)
    {
        if (currentLevel >= LogLevel.Error)
        {
            Debug.LogError(message);
        }
    }
    
    /// <summary>
    /// 重要な情報（本番でも表示）
    /// 例：セッション開始、重要な状態遷移
    /// </summary>
    public static void Important(string message)
    {
        if (currentLevel >= LogLevel.Warning)
        {
            Debug.Log($"<color=yellow>[IMPORTANT]</color> {message}");
        }
    }
    
    /// <summary>
    /// 条件付きログ（アサーション）
    /// </summary>
    public static void Assert(bool condition, string message)
    {
        if (!condition && currentLevel >= LogLevel.Error)
        {
            Debug.LogError($"[ASSERT FAILED] {message}");
        }
    }
}
