using UnityEngine;
using UnityEngine.Video;

/// 次のロードで一度だけプロローグをスキップするゲート。
/// シーンに置いて、プロローグ用のオブジェクトを割り当てるだけ。
[DisallowMultipleComponent]
public class PrologueGate : MonoBehaviour
{
    [Header("Prologue Objects")]
    [Tooltip("プロローグ一式の親（PrologueCanvas など）")]
    public GameObject prologueRoot;

    [Tooltip("プロローグ動画の GameObject（VideoPlayer付き）")]
    public GameObject prologueVideoObject;

    [Tooltip("プロローグで鳴らしている AudioSource（任意）")]
    public AudioSource prologueAudio;

    [Tooltip("FadeCanvas など入力をブロックする CanvasGroup（任意）")]
    public CanvasGroup fadeGroup;

    [Header("After Skip")]
    [Tooltip("スキップ後も確実に入力を通す（必要に応じてON）")]
    public bool unblockFadeAfterSkip = true;

    const string kSkipKey = "SkipPrologueOnce";

    void Awake()
    {
        if (PlayerPrefs.GetInt(kSkipKey, 0) == 1)
        {
            // 1回だけ消費
            PlayerPrefs.SetInt(kSkipKey, 0);
            PlayerPrefs.Save();

            // 映像・音を停止
            TryStopVideo(prologueVideoObject);
            if (prologueAudio && prologueAudio.isPlaying) prologueAudio.Stop();

            // ルート無効化（UI/黒板などまとめて）
            if (prologueRoot) prologueRoot.SetActive(false);
            if (prologueVideoObject) prologueVideoObject.SetActive(false);

            // Fade がクリックを邪魔しないように
            if (unblockFadeAfterSkip && fadeGroup)
            {
                fadeGroup.alpha = 0f;
                fadeGroup.blocksRaycasts = false;
                fadeGroup.interactable   = false;
            }

            // ここでゲーム開始は既存ロジックに委ねる（インゲーム初期化が自動で走る前提）
            // 特定のメソッド呼び出しが必要ならここで呼ぶ：
            // FindObjectOfType<GameFlow>()?.StartGame();
        }
    }

    void TryStopVideo(GameObject go)
    {
        if (!go) return;
        var vp = go.GetComponent<VideoPlayer>();
        if (vp && vp.isPlaying) vp.Stop();
    }
}
