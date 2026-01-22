using UnityEngine;

public enum Difficulty
{
    Easy,
    Normal,
    Hard
}

[System.Serializable]
public class DifficultySettings
{
    [Header("Judge Timing (seconds)")]
    public float perfectWindow = 0.10f;
    public float goodWindow = 0.20f;
    
    [Header("Speed")]
    [Range(0.5f, 2.0f)]
    public float speedMultiplier = 1.0f;
    
    [Header("Note Density")]
    [Range(0.5f, 2.0f)]
    public float noteDensity = 1.0f;
    
    [Header("Display Name")]
    public string displayName = "Normal";
}

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }
    
    [Header("Round System")]
    private const int MAX_ROUNDS = 3;
    [SerializeField] private int currentRound = 1;
    
    [Header("Current Difficulty")]
    [SerializeField] private Difficulty currentDifficulty = Difficulty.Normal;
    
    [Header("Difficulty Settings")]
    [SerializeField] private DifficultySettings easySettings = new DifficultySettings
    {
        perfectWindow = 0.375f,
        goodWindow = 0.75f,
        speedMultiplier = 1.2f,
        noteDensity = 1.0f,
        displayName = "Easy"
    };
    
    [SerializeField] private DifficultySettings normalSettings = new DifficultySettings
    {
        perfectWindow = 0.375f,
        goodWindow = 0.75f,
        speedMultiplier = 2.0f,
        noteDensity = 1.0f,
        displayName = "Normal"
    };
    
    [SerializeField] private DifficultySettings hardSettings = new DifficultySettings
    {
        perfectWindow = 0.375f,
        goodWindow = 0.75f,
        speedMultiplier = 2.5f,
        noteDensity = 1.0f,
        displayName = "Hard"
    };
    
private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 初回起動時のみ Round 1 から開始
        if (currentRound == 0)
        {
            SetRound(1);
        }
        
        Debug.Log($"[DifficultyManager] Initialized - Round {currentRound}, Difficulty: {currentDifficulty}");
    }
    
    public DifficultySettings GetCurrentSettings()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy:
                return easySettings;
            case Difficulty.Normal:
                return normalSettings;
            case Difficulty.Hard:
                return hardSettings;
            default:
                return normalSettings;
        }
    }
    
    public void SetDifficulty(Difficulty difficulty)
    {
        currentDifficulty = difficulty;
        Debug.Log($"[DifficultyManager] Difficulty changed to: {difficulty}");
        
        // 難易度変更を通知（他のシステムに適用）
        ApplyDifficultyToAll();
    }
    
    public Difficulty GetCurrentDifficulty()
    {
        return currentDifficulty;
    }
    
    /// <summary>
    /// ラウンド番号に応じて難易度を設定
    /// Round 1 = Easy, Round 2 = Normal, Round 3 = Hard
    /// </summary>
    public void SetRound(int round)
    {
        currentRound = Mathf.Clamp(round, 1, MAX_ROUNDS);
        
        Difficulty newDifficulty;
        switch (currentRound)
        {
            case 1:
                newDifficulty = Difficulty.Easy;
                break;
            case 2:
                newDifficulty = Difficulty.Normal;
                break;
            case 3:
                newDifficulty = Difficulty.Hard;
                break;
            default:
                newDifficulty = Difficulty.Easy;
                break;
        }
        
        Debug.Log($"[DifficultyManager] Round {currentRound}/3 → Difficulty: {newDifficulty}");
        SetDifficulty(newDifficulty);
    }
    
    /// <summary>
    /// 次のラウンドに進む
    /// Round 3の後は広告表示して Round 1 に戻る
    /// </summary>
    public void NextRound()
    {
        // Round 3 の次は広告を表示
        if (currentRound >= MAX_ROUNDS)
        {
            Debug.Log($"[DifficultyManager] Round {currentRound} 終了！広告表示後、Round 1 へ");
            ShowAdAndRestart();
        }
        else
        {
            // Round 1 → 2, Round 2 → 3
            SetRound(currentRound + 1);
        }
    }
    
    /// <summary>
    /// 現在のラウンド番号を取得
    /// </summary>
    public int GetCurrentRound()
    {
        return currentRound;
    }
    
    /// <summary>
    /// Round 3 が終了したかチェック（広告タイミング）
    /// </summary>
    public bool IsRound3Complete()
    {
        return currentRound >= MAX_ROUNDS;
    }
    
    /// <summary>
    /// 広告表示して Round 1 に戻る
    /// </summary>
    private void ShowAdAndRestart()
    {
        Debug.Log("[DifficultyManager] 📺 広告表示中...");
        
        // AdPhaseManager を使って広告表示
        if (AdPhaseManager.Instance != null)
        {
            AdPhaseManager.Instance.ShowAd(() => 
            {
                // 広告終了後に Round 1 へ
                RestartFromRound1();
            });
        }
        else
        {
            Debug.LogWarning("[DifficultyManager] AdPhaseManager not found. Skipping ad.");
            RestartFromRound1();
        }
    }
    
    /// <summary>
    /// Round 1 に戻る
    /// </summary>
    private void RestartFromRound1()
    {
        Debug.Log("[DifficultyManager] 🔄 Round 1 から再スタート！");
        SetRound(1);
    }
    
    private void ApplyDifficultyToAll()
    {
        // ChartSpawner に通知
        var chartSpawner = FindObjectOfType<ChartSpawner>();
        if (chartSpawner != null)
        {
            // ChartSpawner に難易度適用メソッドがあれば呼ぶ
            Debug.Log("[DifficultyManager] Notified ChartSpawner");
        }
        
        // 判定システムに通知
        Debug.Log($"[DifficultyManager] Applied settings - Perfect: {GetCurrentSettings().perfectWindow}s, Speed: {GetCurrentSettings().speedMultiplier}x, Density: {GetCurrentSettings().noteDensity}x");
    }
    
    // Inspector でテスト用
    [ContextMenu("Set Easy")]
    private void SetEasy() => SetDifficulty(Difficulty.Easy);
    
    [ContextMenu("Set Normal")]
    private void SetNormal() => SetDifficulty(Difficulty.Normal);
    
    [ContextMenu("Set Hard")]
    private void SetHard() => SetDifficulty(Difficulty.Hard);
    
    [ContextMenu("Start Round 1")]
    private void StartRound1() => SetRound(1);
    
    [ContextMenu("Start Round 2")]
    private void StartRound2() => SetRound(2);
    
    [ContextMenu("Start Round 3")]
    private void StartRound3() => SetRound(3);
    
    [ContextMenu("Next Round")]
    private void TestNextRound() => NextRound();
}
