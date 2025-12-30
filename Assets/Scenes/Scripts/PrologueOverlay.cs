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
    private bool _bgmStarted = false;

    void Awake()
    {
        _conductor = FindObjectOfType<Conductor>();
        _autoChartSpawner = FindObjectOfType<AutoChartSpawner>();
        
        if (_conductor != null) _conductor.enabled = false;
        if (_autoChartSpawner != null) _autoChartSpawner.enabled = false;
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

    public void StartBgmFadeIn(float duration)
    {
        if (_bgmStarted) return;
        _bgmStarted = true;
        
        if (_conductor != null)
        {
            _conductor.enabled = true;
        }
        
        Debug.Log("[PrologueOverlay] BGM started");
    }

    public void OnVideoEnded()
    {
        StartCoroutine(FadeOutAndStartGame());
    }

    public void Skip()
    {
        StopAllCoroutines();
        
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

        if (_autoChartSpawner != null) _autoChartSpawner.enabled = true;

        var prologueCanvas = GameObject.Find("PrologueCanvas");
        if (prologueCanvas != null)
        {
            prologueCanvas.SetActive(false);
            Debug.Log("[PrologueOverlay] PrologueCanvas hidden");
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
