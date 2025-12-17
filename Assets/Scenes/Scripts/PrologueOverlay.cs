using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrologueOverlay : MonoBehaviour
{
    public CanvasGroup overlay;
    public float fadeTime = 0.25f;

    public List<Behaviour> componentsToDisable = new List<Behaviour>();

    private Conductor _conductor;
    private AutoChartSpawner _autoChartSpawner;
    private CookingResultSequence _cookingResult;
    private bool _bgmStarted = false;

    void Awake()
    {
        // プロローグ中に動いてほしくないスクリプトを自動検出して無効化
        _conductor = FindObjectOfType<Conductor>();
        _autoChartSpawner = FindObjectOfType<AutoChartSpawner>();
        _cookingResult = FindObjectOfType<CookingResultSequence>();
        
        if (_conductor != null) _conductor.enabled = false;
        if (_autoChartSpawner != null) _autoChartSpawner.enabled = false;
        if (_cookingResult != null) _cookingResult.enabled = false;
    }

    void OnEnable()
    {
        if (overlay != null)
        {
            overlay.alpha = 1f;
            overlay.blocksRaycasts = true;
            overlay.interactable = true;
        }
    }

    void Start()
    {
        foreach (var b in componentsToDisable)
            if (b != null) b.enabled = false;
    }

    // 動画終了の bgmLeadTime 秒前に呼ばれる
    public void StartBgmFadeIn(float duration)
    {
        if (_bgmStarted) return;
        _bgmStarted = true;
        
        // Conductor を有効化して BGM 開始
        if (_conductor != null)
        {
            _conductor.enabled = true;
        }
        
        Debug.Log("[PrologueOverlay] BGM started");
    }

    // 動画終了時に呼ばれる
    public void OnVideoEnded()
    {
        StartCoroutine(FadeOutAndStartGame());
    }

    // スキップボタン用
    public void Skip()
    {
        StopAllCoroutines();
        
        // BGMがまだなら開始
        if (!_bgmStarted && _conductor != null)
        {
            _conductor.enabled = true;
            _bgmStarted = true;
        }
        
        StartCoroutine(FadeOutAndStartGame());
    }

    IEnumerator FadeOutAndStartGame()
    {
        foreach (var helper in FindObjectsOfType<PrologueSpawnerResumeHelper>())
            helper.ResetScheduleBeats();

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

        foreach (var b in componentsToDisable)
            if (b != null) b.enabled = true;

        // ゲーム開始に必要なスクリプトを有効化
        if (_autoChartSpawner != null) _autoChartSpawner.enabled = true;
        if (_cookingResult != null) _cookingResult.enabled = true;

        gameObject.SetActive(false);
    }
}
