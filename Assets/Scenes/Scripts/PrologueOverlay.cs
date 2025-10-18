using System.Collections;
using UnityEngine;

public class PrologueOverlay : MonoBehaviour
{
    public CanvasGroup overlay;     // ← BlackPanel の CanvasGroup を入れる
    public float autoCloseAfter = 23f; // 23秒
    public float fadeTime = 0.25f;     // 0.25秒でふわっと

    IEnumerator Start()
    {
        // ゲームを一時停止（キャラが動かない）
        Time.timeScale = 0f;

        // 黒いふたを見えるように
        overlay.alpha = 1f;
        overlay.blocksRaycasts = true;
        overlay.interactable = true;

        // 動画の長さだけ待つ（時間は止めても数える）
        yield return new WaitForSecondsRealtime(autoCloseAfter);

        // ふわっと消す → ゲーム再開
        yield return FadeOutAndStartGame();
    }

    public void Skip()  // スキップボタン用（使わなくてもOK）
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutAndStartGame());
    }

    IEnumerator FadeOutAndStartGame()
    {
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime; // 時間停止中でも進む
            overlay.alpha = 1f - (t / fadeTime);
            yield return null;
        }
        overlay.alpha = 0f;
        overlay.blocksRaycasts = false;
        overlay.interactable = false;

        // ゲームを動かす
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        yield return null;
    }
}
