using UnityEngine;
using UnityEngine.Video;

public class PrologueGate : MonoBehaviour
{
    [Header("プロローグ関連（あれば入れる。未指定でもOK）")]
    public GameObject prologueCanvasRoot;   // PrologueCanvas のルート
    public VideoPlayer prologueVideo;       // プロローグ用 VideoPlayer（任意）
    public CanvasGroup fadeCanvas;          // FadeCanvas（任意）

    [Header("ゲーム開始ターゲット")]
    public GameObject gameStartTarget;      // GameFlow 等、開始メソッドを持つオブジェクト
    public string startMethodName = "StartGame"; // 公開メソッド名（SendMessage用）

    void Awake()
    {
        // 1) 念のため初期化（待ちが残っても動くように）
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (fadeCanvas)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
            fadeCanvas.interactable = false;
        }
    }

    void Start()
    {
        if (GameFlags.SkipPrologueOnce)
        {
            // 2) プロローグ要素は完全停止&非表示
            if (prologueVideo)
            {
                try
                {
                    prologueVideo.Stop();
                    prologueVideo.sendFrameReadyEvents = false;
                    prologueVideo.enabled = false;
                }
                catch { }
            }
            if (prologueCanvasRoot) prologueCanvasRoot.SetActive(false);

            // 3) すぐゲーム開始
            BootGameNow();

            // 4) このフラグは1回きり
            GameFlags.SkipPrologueOnce = false;
        }
        else
        {
            // 普通にプロローグから始めたい時は何もしない
        }
    }

    void BootGameNow()
    {
        // Animatorや自前のステートが止まっていると嫌なので、最低限の解除
        foreach (var anim in FindObjectsOfType<Animator>())
        {
            anim.updateMode = AnimatorUpdateMode.Normal;
            anim.speed = 1f;
        }

        // 代表的な“開始メソッド”を順に叩く（存在するものだけ）
        if (gameStartTarget)
        {
            // SendMessage は相手にメソッドが無くても落ちないのが利点
            gameStartTarget.SendMessage(startMethodName, SendMessageOptions.DontRequireReceiver);
            gameStartTarget.SendMessage("OnRetryBoot", SendMessageOptions.DontRequireReceiver); // 予備フック
        }

        // “Conductor”“GameFlow”“LaneManager”などの初期化メソッドがあれば、名前で併せて叩く
        BroadcastMessage("ResetStateForRetry", SendMessageOptions.DontRequireReceiver);
    }
}
