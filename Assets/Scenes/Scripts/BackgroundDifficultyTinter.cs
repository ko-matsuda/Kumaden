using UnityEngine;
using System.Collections.Generic;

public class BackgroundDifficultyTinter : MonoBehaviour
{
    [Header("夕方（Normal）")]
    [SerializeField] private Color tintDusk  = new Color(1.00f, 0.75f, 0.45f, 1f);

    [Header("夜（Hard）")]
    [SerializeField] private Color tintNight = new Color(0.75f, 0.80f, 1.00f, 1f);

    [Header("夕方の空の色")]
    [SerializeField] private Color skyColorDusk  = new Color(1.00f, 0.60f, 0.10f, 1f);

    [Header("夜空の色")]
    [SerializeField] private Color skyColorNight = new Color(0.10f, 0.13f, 0.35f, 1f);

    private struct RendererInfo
    {
        public Renderer renderer;
        public Color[] originalColors;
    }

    private struct WindowInfo
    {
        public Renderer renderer;
        public int slot; // Glass_Windowマテリアルのスロット番号
    }

    private List<RendererInfo> _worldRenderers = new List<RendererInfo>();
    private List<WindowInfo>   _windowRenderers = new List<WindowInfo>();
    private MaterialPropertyBlock _block;
    private Difficulty _lastDiff = (Difficulty)(-1);

    private Renderer   _skyRenderer;
    private Material   _skyMatEasy;
    private Material   _skyMatDusk;
    private Material   _skyMatNight;
    private GameObject _cloudsObject;
    private GameObject _nightStarsObject;

    private void Start()
    {
        _block = new MaterialPropertyBlock();
        CollectRenderers();
        ApplyIfChanged();
    }

    private void Update()
    {
        ApplyIfChanged();
    }

private void CollectRenderers()
    {
        _worldRenderers.Clear();
        _windowRenderers.Clear();

        var skyQuad = GameObject.Find("SkyQuad");
        if (skyQuad != null)
            _skyRenderer = skyQuad.GetComponent<Renderer>();

        // 空のマテリアルをロード
        _skyMatEasy  = _skyRenderer != null ? _skyRenderer.sharedMaterial : null;
        _skyMatDusk  = Resources.Load<Material>("../Material/Mat_SkyGrad_Dusk");
        _skyMatNight = Resources.Load<Material>("../Material/Mat_SkyGrad_Night");
        // Resourcesフォルダ外なのでAssetDatabaseで
#if UNITY_EDITOR
        if (_skyMatDusk  == null) _skyMatDusk  = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Mat_SkyGrad_Dusk.mat");
        if (_skyMatNight == null) _skyMatNight = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Mat_SkyGrad_Night.mat");
#endif

        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (!t.gameObject.scene.isLoaded) continue;
            if (t.name == "Clouds")      { _cloudsObject = t.gameObject; }
            if (t.name == "NightStars")  { _nightStarsObject = t.gameObject; }
        }

        GameObject player = GameObject.Find("Player");
        Transform playerRoot = player != null ? player.transform : null;

        var all = FindObjectsOfType<Renderer>();
        foreach (var r in all)
        {
            if (r.GetComponentInParent<Canvas>() != null) continue;
            if (r is ParticleSystemRenderer) continue;
            if (playerRoot != null && r.transform.IsChildOf(playerRoot)) continue;
            if (r == _skyRenderer) continue;

            var mats = r.sharedMaterials;

            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && mats[i].name.Contains("Glass_Window"))
                    _windowRenderers.Add(new WindowInfo { renderer = r, slot = i });

            var colors = new Color[mats.Length];
            for (int i = 0; i < mats.Length; i++)
                colors[i] = (mats[i] != null && mats[i].HasProperty("_BaseColor"))
                    ? mats[i].GetColor("_BaseColor") : Color.white;
            _worldRenderers.Add(new RendererInfo { renderer = r, originalColors = colors });
        }
        Debug.Log($"[BgTinter] sky={_skyRenderer?.name} duskMat={_skyMatDusk?.name} renderers={_worldRenderers.Count}");
    }

private void ApplyIfChanged()
    {
        if (DifficultyManager.Instance == null) return;
        Difficulty diff = DifficultyManager.Instance.GetCurrentDifficulty();
        if (diff == _lastDiff) return;
        _lastDiff = diff;

        bool isNight = diff == Difficulty.Hard;
        bool isEasy  = diff == Difficulty.Easy;

        Color tint = diff == Difficulty.Normal ? tintDusk
                   : diff == Difficulty.Hard   ? tintNight
                   : Color.white;

        foreach (var info in _worldRenderers)
        {
            if (info.renderer == null) continue;
            if (isEasy)
            {
                info.renderer.SetPropertyBlock(null);
            }
            else
            {
                var mats = info.renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Color blended = info.originalColors[i] * tint;
                    blended.a = info.originalColors[i].a;
                    info.renderer.GetPropertyBlock(_block, i);
                    _block.SetColor("_BaseColor", blended);
                    info.renderer.SetPropertyBlock(_block, i);
                }
            }
        }

        // SkyQuad: マテリアルごと差し替え（PropertyBlockではなく実体を切り替え）
        if (_skyRenderer != null)
        {
            Material skyMat = isNight ? _skyMatNight
                            : diff == Difficulty.Normal ? _skyMatDusk
                            : _skyMatEasy;
            if (skyMat != null)
                _skyRenderer.sharedMaterial = skyMat;
        }

        if (_cloudsObject != null)     _cloudsObject.SetActive(!isNight);
        if (_nightStarsObject != null) _nightStarsObject.SetActive(isNight);

        foreach (var w in _windowRenderers)
        {
            if (w.renderer == null) continue;
            w.renderer.GetPropertyBlock(_block, w.slot);
            if (isNight)
                _block.SetColor("_BaseColor", new Color(1.0f, 0.85f, 0.2f, 1f));
            else if (isEasy)
                _block.SetColor("_BaseColor", Color.white);
            else
                _block.SetColor("_BaseColor", new Color(1.0f, 0.80f, 0.3f, 1f));
            w.renderer.SetPropertyBlock(_block, w.slot);
        }

        Debug.Log($"[BgTinter] Applied diff={diff}");
    }
}
