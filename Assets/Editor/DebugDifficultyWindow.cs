#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// デバッグ用：任意の難易度からテストプレイを開始するEditorウィンドウ
/// メニュー: KumaDen > Debug Play
/// </summary>
public class DebugDifficultyWindow : EditorWindow
{
    public const string PREF_KEY = "DebugStartRound";

    [MenuItem("KumaDen/Debug Play")]
    public static void Open()
    {
        var window = GetWindow<DebugDifficultyWindow>("Debug Play");
        window.minSize = new Vector2(220, 130);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("難易度を選んでPlayを開始", EditorStyles.boldLabel);
        GUILayout.Space(8);

        GUI.backgroundColor = new Color(0.5f, 1.0f, 0.5f);
        if (GUILayout.Button("▶  Easy（昼）", GUILayout.Height(30)))
            StartDebugPlay(1);

        GUI.backgroundColor = new Color(1.0f, 0.7f, 0.3f);
        if (GUILayout.Button("▶  Normal（夕方）", GUILayout.Height(30)))
            StartDebugPlay(2);

        GUI.backgroundColor = new Color(0.4f, 0.5f, 1.0f);
        if (GUILayout.Button("▶  Hard（夜）", GUILayout.Height(30)))
            StartDebugPlay(3);

        GUI.backgroundColor = Color.white;
        GUILayout.Space(6);
        EditorGUILayout.HelpBox("Playを停止するとデバッグラウンドは自動解除されます", MessageType.None);
    }

    private static void StartDebugPlay(int round)
    {
        PlayerPrefs.SetInt(PREF_KEY, round);
        PlayerPrefs.Save();
        EditorApplication.isPlaying = true;
    }
}
#endif
