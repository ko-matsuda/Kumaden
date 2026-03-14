
using UnityEngine;
using UnityEditor;

/// <summary>
/// KumaDen ステージデバッグウィンドウ
/// Window > KumaDen > Stage Debug Player から開く
/// </summary>
public class KumaDenDebugWindow : EditorWindow
{
    // ────────────────────────────────────────────────────────────────
    // 各ステージ情報
    // ────────────────────────────────────────────────────────────────
    private static readonly string[] DIFF_LABELS  = { "Easy (Day)",  "Normal (Evening)", "Hard (Night)" };
    private static readonly string[] SONG_LABELS  = { "Song A",      "Song B" };
    private static readonly Color[]  DIFF_COLORS  = {
        new Color(0.40f, 0.75f, 0.40f),   // green  – Easy
        new Color(0.40f, 0.60f, 0.90f),   // blue   – Normal
        new Color(0.80f, 0.40f, 0.40f),   // red    – Hard
    };

    // Chart / Audio 名一覧（監査結果）
    private static readonly string[,] SONG_NAMES = {
        // Easy
        { "Odyssey (87.5s → lastNote 83.3s)",  "Pancakes (86.5s → lastNote 69.8s ⚠)" },
        // Normal
        { "Evening Cafe (119.9s → lastNote 116.5s)", "Evening Adventure (75.9s → lastNote 74.8s)" },
        // Hard
        { "Night Dream 01 (117.0s → lastNote 115.0s)", "Night Dream 02 (85.8s → lastNote 83.8s)" },
    };

    private Vector2 scroll;
    private string statusMsg = "";

    // ────────────────────────────────────────────────────────────────
    [MenuItem("Window/KumaDen/Stage Debug Player")]
    public static void ShowWindow()
    {
        var win = GetWindow<KumaDenDebugWindow>("🐻 Stage Debug");
        win.minSize = new Vector2(320, 480);
    }

    // ────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        // ── タイトル ──
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("🐻  KumaDen Stage Debug Player", titleStyle, GUILayout.Height(24));
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox("プレイしたいステージのボタンを押すと\nそのステージから自動でプレイ開始します", MessageType.Info);
        EditorGUILayout.Space(8);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int diff = 0; diff < 3; diff++)
        {
            // ── 難易度ヘッダ ──
            GUI.backgroundColor = DIFF_COLORS[diff];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            EditorGUILayout.LabelField($"Round {diff + 1}  ──  {DIFF_LABELS[diff]}", headerStyle);
            EditorGUILayout.Space(2);

            for (int song = 0; song < 2; song++)
            {
                EditorGUILayout.BeginHorizontal();

                // 曲情報ラベル
                EditorGUILayout.LabelField($"  {SONG_LABELS[song]}:  {SONG_NAMES[diff, song]}",
                    EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

                // ▶ ボタン
                GUI.backgroundColor = new Color(0.9f, 0.95f, 1.0f);
                if (GUILayout.Button("▶ Play", GUILayout.Width(64), GUILayout.Height(20)))
                    StartDebugPlay(diff + 1, song);
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);
        }

        EditorGUILayout.EndScrollView();

        // ── ステータス表示 ──
        if (!string.IsNullOrEmpty(statusMsg))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(statusMsg, MessageType.None);
        }

        // ── 停止ボタン ──
        EditorGUILayout.Space(4);
        GUI.backgroundColor = new Color(1.0f, 0.7f, 0.7f);
        if (GUILayout.Button("■  Stop", GUILayout.Height(28)))
        {
            EditorApplication.isPlaying = false;
            statusMsg = "■ 停止しました";
            Repaint();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(6);
    }

    // ────────────────────────────────────────────────────────────────
    /// <summary>
    /// PlayerPrefs にデバッグ情報を書き込んでプレイ開始
    /// </summary>
    static void StartDebugPlay(int round, int songIndex)
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            // ドメインリロードを待ってから開始
            EditorApplication.playModeStateChanged += WaitAndPlay;

            void WaitAndPlay(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    EditorApplication.playModeStateChanged -= WaitAndPlay;
                    SetPrefsAndPlay(round, songIndex);
                }
            }
            return;
        }

        SetPrefsAndPlay(round, songIndex);
    }

    static void SetPrefsAndPlay(int round, int songIndex)
    {
        // DifficultyManager が読む
        PlayerPrefs.SetInt("DebugStartRound", round);
        // SongLoopController が読む
        PlayerPrefs.SetInt("DebugStartSongIndex", songIndex);
        PlayerPrefs.Save();

        string diff = DIFF_LABELS[round - 1];
        string song = SONG_LABELS[songIndex];
        Debug.Log($"[DebugPlay] Round {round} ({diff}) / {song} でプレイ開始");

        EditorApplication.isPlaying = true;

        // ウィンドウのステータス更新
        var win = GetWindow<KumaDenDebugWindow>();
        win.statusMsg = $"▶ Round {round} ({diff}) / {song} 開始中...";
        win.Repaint();
    }
}
