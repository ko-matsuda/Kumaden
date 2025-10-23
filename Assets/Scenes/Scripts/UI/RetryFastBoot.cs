using UnityEngine;

public class RetryFastBoot : MonoBehaviour
{
    // ResultButtons から渡される合図
    public static bool SkipToGameplay = false;

    [Header("ゲーム開始の呼び出し先（名前で探して SendMessage）")]
    [Tooltip("インゲームを開始させる管理オブジェクト名（例: GameFlow）")]
    public string startTargetName = "GameFlow";
    [Tooltip("開始メソッド名（例: StartGame / Begin / Play など）")]
    public string startMethodName = "StartGame";

    [Header("スキップ時に無効化したいUI（任意）")]
    public GameObject[] uiToDisableOnSkip; // 例：PrologueCanvas, TitleCanvas など

    void Start()
    {
        // 毎回、安全に復帰
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (!SkipToGameplay) return; // 通常起動

        // プロローグ/タイトル系を消す
        if (uiToDisableOnSkip != null)
            foreach (var go in uiToDisableOnSkip) if (go) go.SetActive(false);

        // すぐインゲーム開始
        var target = GameObject.Find(startTargetName);
        if (target)
        {
            // Start/Begin/Play… どれでも対応できるように試行
            string[] methods = { startMethodName, "Start", "Begin", "Play", "StartRun" };
            foreach (var m in methods)
            {
                try { target.SendMessage(m, SendMessageOptions.DontRequireReceiver); } catch {}
            }
        }

        // 一度使ったらオフ（次回タイトル復帰時に通常フローへ戻す）
        SkipToGameplay = false;
    }
}
