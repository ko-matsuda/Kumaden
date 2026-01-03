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
        Debug.Log("[PrologueGate] Awake called");
        
        // Retry時はプロローグをスキップ（Startより早く実行）
        bool hasSeenPrologue = PlayerPrefs.GetInt("HasSeenPrologue", 0) == 1;
        
        if (hasSeenPrologue || GameFlags.SkipPrologueOnce)
        {
            Debug.Log($"[PrologueGate] Awake - Skipping prologue (hasSeenPrologue={hasSeenPrologue}, SkipPrologueOnce={GameFlags.SkipPrologueOnce})");
            
            // プロローグCanvasを完全に非アクティブ化
            if (prologueCanvasRoot != null)
            {
                prologueCanvasRoot.SetActive(false);
                Debug.Log("[PrologueGate] PrologueCanvas deactivated");
            }
            
            // プロローグ動画も停止
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

            // フラグはリセットしない（Startでリセット）
        }
        
        // 従来の初期化
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
        Debug.Log("[PrologueGate] Start called");
        
        bool hasSeenPrologue = PlayerPrefs.GetInt("HasSeenPrologue", 0) == 1;
        bool skipFlag = GameFlags.SkipPrologueOnce;
        
        Debug.Log($"[PrologueGate] Start - hasSeenPrologue={hasSeenPrologue}, SkipPrologueOnce={skipFlag}");
        
        // フラグをリセット（Awakeではリセットしない）
        if (GameFlags.SkipPrologueOnce)
        {
            GameFlags.SkipPrologueOnce = false;
            Debug.Log("[PrologueGate] SkipPrologueOnce flag reset");
        }
        
        if (hasSeenPrologue || skipFlag)
        {
            Debug.Log("[PrologueGate] Calling BootGameNow()");
            BootGameNow();
        }
        else
        {
            Debug.Log("[PrologueGate] Waiting for prologue to finish");
        }
    }

void BootGameNow()
    {
        
        
        // ResultCanvasを非表示（ゲーム開始時）
        var resultCanvas = GameObject.Find("ResultCanvas");
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(false);
            Debug.Log("[PrologueGate] ResultCanvas hidden");
        }
Debug.Log("[PrologueGate] BootGameNow called");
        
        // Animatorや自前のステートが止まっていると嫌なので、最低限の解除
        foreach (var anim in FindObjectsOfType<Animator>())
        {
            anim.updateMode = AnimatorUpdateMode.Normal;
            anim.speed = 1f;
        }
        
        // AudioSourceを全て有効化
        foreach (var audioSource in FindObjectsOfType<AudioSource>())
        {
            audioSource.enabled = true;
        }
        
        // WorldScrollerを後でリセット（1フレーム待機）
        StartCoroutine(ResetWorldScrollerDelayed());
        
        Debug.Log($"[PrologueGate] gameStartTarget={gameStartTarget}, startMethodName={startMethodName}");

        // 代表的な"開始メソッド"を順に叩く（存在するものだけ）
        if (gameStartTarget)
        {
            Debug.Log($"[PrologueGate] Sending message '{startMethodName}' to {gameStartTarget.name}");
            gameStartTarget.SendMessage(startMethodName, SendMessageOptions.DontRequireReceiver);
            gameStartTarget.SendMessage("OnRetryBoot", SendMessageOptions.DontRequireReceiver);
            Debug.Log($"[PrologueGate] Messages sent to {gameStartTarget.name}");
        }
        else
        {
            Debug.LogWarning("[PrologueGate] gameStartTarget is null!");
        }

        // ChartSpawnerはリセットしない（SongLoopControllerが管理）
        var chartSpawner = FindObjectOfType<ChartSpawner>();
        if (chartSpawner != null)
        {
            Debug.Log("[PrologueGate] ChartSpawner found - not resetting (managed by SongLoopController)");
        }

        // "Conductor""GameFlow""LaneManager"などの初期化メソッドがあれば、名前で併せて叩く
        Debug.Log("[PrologueGate] Broadcasting ResetStateForRetry");
        BroadcastMessage("ResetStateForRetry", SendMessageOptions.DontRequireReceiver);
        
        Debug.Log("[PrologueGate] BootGameNow completed");
    }

System.Collections.IEnumerator ResetWorldScrollerDelayed()
    {
        yield return null; // 1フレーム待機
        
        var worldScroller = FindObjectOfType<WorldScroller>();
        if (worldScroller != null)
        {
            Debug.Log($"[PrologueGate] Resetting WorldScroller position from {worldScroller.transform.position} to (0,0,0)");
            worldScroller.transform.position = Vector3.zero;
            worldScroller.enabled = true;
        }
        else
        {
            Debug.LogWarning("[PrologueGate] WorldScroller not found!");
        }
    }

}
