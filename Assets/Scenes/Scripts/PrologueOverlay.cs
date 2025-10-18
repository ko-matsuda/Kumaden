using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrologueOverlay : MonoBehaviour
{
    public CanvasGroup overlay;       // BlackPanel の CanvasGroup
    public float totalDuration = 23f; // プロローグ動画の長さ（秒）
    public float bgmLeadTime = 3.9f;  // 終了何秒前からBGMを上げるか
    public float fadeTime = 0.25f;    // 画面フェード時間

    public List<AudioSource> bgmSources = new List<AudioSource>(); // インゲームBGM
    public List<Behaviour> componentsToDisable = new List<Behaviour>(); // 止めたいスクリプト

    public bool restartBgmFromBeginning = true; // BGMを先頭から再生する
    public bool hardStopAtStart = true;         // 開始時にBGMを完全停止

    private readonly List<float> _bgmOriginalVolumes = new List<float>();
    private readonly List<int>   _savedTimeSamples    = new List<int>();

    void OnEnable()
    {
        if (overlay != null)
        {
            overlay.alpha = 1f;
            overlay.blocksRaycasts = true;
            overlay.interactable = true;
        }
    }

    IEnumerator Start()
    {
        // ① ゲーム停止
        Time.timeScale = 0f;

        // ② BGMは“フェード開始まで再生させない”
        _bgmOriginalVolumes.Clear();
        _savedTimeSamples.Clear();
        foreach (var s in bgmSources)
        {
            if (s == null) { _bgmOriginalVolumes.Add(0f); _savedTimeSamples.Add(0); continue; }
            _bgmOriginalVolumes.Add(s.volume);
            _savedTimeSamples.Add(s.clip != null ? s.timeSamples : 0);

            s.volume = 0f;
            s.mute   = true;
            s.playOnAwake = false;

            if (hardStopAtStart) s.Stop(); else if (s.isPlaying) s.Pause();
        }

        // ③ 動き続ける処理は手動で停止
        foreach (var b in componentsToDisable)
            if (b != null) b.enabled = false;

        // ④ BGMフェード開始のタイミングまで“リアル時間”で待機
        float fadeStart = Mathf.Max(0f, totalDuration - bgmLeadTime);
        yield return new WaitForSecondsRealtime(fadeStart);

        // ⑤ BGMフェードイン（この瞬間にだけ Play）
        yield return StartCoroutine(FadeInBgmFromSilence(bgmLeadTime));

        // ⑥ 動画終了まで待つ（残りbgmLeadTimeはすでに消化済み）
        // → すぐに再開フェードへ
        yield return FadeOutAndStartGame();
    }

    IEnumerator FadeInBgmFromSilence(float duration)
    {
        for (int i = 0; i < bgmSources.Count; i++)
        {
            var s = bgmSources[i];
            if (s == null) continue;

            if (restartBgmFromBeginning && s.clip != null) s.timeSamples = 0;
            else if (!restartBgmFromBeginning && s.clip != null) s.timeSamples = _savedTimeSamples[i];

            s.mute = false;
            if (!s.isPlaying) s.Play();
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);

            for (int i = 0; i < bgmSources.Count; i++)
            {
                var s = bgmSources[i];
                if (s == null) continue;
                float target = _bgmOriginalVolumes.Count > i ? _bgmOriginalVolumes[i] : 1f;
                s.volume = Mathf.Lerp(0f, target, k);
            }
            yield return null;
        }

        for (int i = 0; i < bgmSources.Count; i++)
        {
            var s = bgmSources[i];
            if (s == null) continue;
            s.volume = _bgmOriginalVolumes[i];
        }
        yield break;
    }

    public void Skip()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutAndStartGame());
    }

    IEnumerator FadeOutAndStartGame()
    {
        // ★ スポーナーに「再開は今から！」を通知（まとめ湧き防止）
        foreach (var helper in FindObjectsOfType<PrologueSpawnerResumeHelper>())
            helper.ResetScheduleBeats(); // Inspectorの値（既定0.75拍）を使用

        // 黒フェード（Unscaled）
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            if (overlay != null)
            {
                float r = fadeTime <= 0f ? 1f : Mathf.Clamp01(t / fadeTime);
                overlay.alpha = 1f - r;
            }
            yield return null;
        }
        if (overlay != null)
        {
            overlay.alpha = 0f;
            overlay.blocksRaycasts = false;
            overlay.interactable = false;
        }

        // 停めていたコンポーネントを再開
        foreach (var b in componentsToDisable)
            if (b != null) b.enabled = true;

        // ゲーム再開
        Time.timeScale = 1f;

        gameObject.SetActive(false);
    }
}
