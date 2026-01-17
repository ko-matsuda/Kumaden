using UnityEngine;
using UnityEditor;

public class CumulativeScoreDebugTool : EditorWindow
{
    [MenuItem("Tools/Cumulative Score Debug")]
    static void ShowWindow()
    {
        GetWindow<CumulativeScoreDebugTool>("Cumulative Score");
    }

void OnGUI()
    {
        GUILayout.Label("クマ伝コアサイクル管理", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        // 累計スコア
        int currentScore = PlayerPrefs.GetInt("CumulativeScore", 0);
        GUILayout.Label($"通算スコア: {currentScore:N0}", EditorStyles.largeLabel);
        
        // サイクルカウント
        int sessionCount = PlayerPrefs.GetInt("SessionCount", 1);
        GUILayout.Label($"現在のサイクル: Round {sessionCount}/3", EditorStyles.largeLabel);
        
        GUILayout.Space(20);
        
        // 累計スコア操作
        GUILayout.Label("通算スコア操作:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("リセット (0に)", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("通算スコアリセット", 
                "通算スコアを0にリセットしますか？", 
                "Yes", "Cancel"))
            {
                ScoreManagerLite.ResetCumulativeScore();
                Debug.Log("Cumulative score reset to 0");
                Repaint();
            }
        }
        
        if (GUILayout.Button("+10,000点 (テスト)", GUILayout.Height(30)))
        {
            ScoreManagerLite.AddToCumulativeScore(10000);
            Debug.Log("Added 10,000 points");
            Repaint();
        }
        
        if (GUILayout.Button("+50,000点 (テスト)", GUILayout.Height(30)))
        {
            ScoreManagerLite.AddToCumulativeScore(50000);
            Debug.Log("Added 50,000 points");
            Repaint();
        }
        
        GUILayout.Space(20);
        
        // サイクルカウント操作
        GUILayout.Label("サイクルカウント操作:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("リセット (1に)", GUILayout.Height(30)))
        {
            ScoreManagerLite.ResetSessionCount();
            Debug.Log("Session count reset to 1");
            Repaint();
        }
        
        if (GUILayout.Button("+1サイクル (テスト)", GUILayout.Height(30)))
        {
            ScoreManagerLite.IncrementSessionCount();
            Debug.Log($"Session count incremented to {ScoreManagerLite.GetSessionCount()}");
            Repaint();
        }
        
        if (sessionCount >= 3)
        {
            GUILayout.Label("⚠️ 次のRetryで広告フョーズが表示されます", EditorStyles.helpBox);
        }
        
        GUILayout.Space(20);
        
        GUILayout.Label("ランク閾値 (通算スコア):", EditorStyles.boldLabel);
        GUILayout.Label("500,000+ = Rank 1-2");
        GUILayout.Label("300,000+ = Rank 2-4");
        GUILayout.Label("150,000+ = Rank 3-6");
        GUILayout.Label("50,000+ = Rank 5-9");
        GUILayout.Label("< 50,000 = Rank 8-19");
    }
}
