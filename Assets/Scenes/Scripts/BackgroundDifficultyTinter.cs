using UnityEngine;

/// <summary>
/// 難易度に応じて背景・世界全体の色調を変える
/// 
/// 【方法】
/// 1. ディレクショナルライトの色を変える
///    → 建物・道路・クマ（Lit）に色が乗る。UIは自己発光なので影響なし。
/// 2. Sky/Hills/Clouds は Unlit なので MaterialPropertyBlock で個別tint
///
/// Easy=昼（現状）, Normal=夕方オレンジ, Hard=夜ブルー
/// </summary>
public class BackgroundDifficultyTinter : MonoBehaviour
{
    [Header("ライト（自動検索）")]
    [SerializeField] private Light directionalLight;

    [Header("背景Renderer（自動検索）")]
    [SerializeField] private Renderer skyRenderer;
    [SerializeField] private Renderer hillsRenderer;
    [SerializeField] private Renderer[] cloudRenderers;

    [Header("昼（Easy）")]
    [SerializeField] private Color lightColorDay  = new Color(1.00f, 0.96f, 0.84f);
    [SerializeField] private Color skyColorDay    = new Color(1.00f, 1.00f, 1.00f);

    [Header("夕方（Normal）")]
    [SerializeField] private Color lightColorDusk = new Color(1.00f, 0.60f, 0.25f);
    [SerializeField] private Color skyColorDusk   = new Color(1.00f, 0.50f, 0.15f);

    [Header("夜（Hard）")]
    [SerializeField] private Color lightColorNight = new Color(0.20f, 0.25f, 0.60f);
    [SerializeField] private Color skyColorNight   = new Color(0.08f, 0.10f, 0.25f);

    private MaterialPropertyBlock _block;
    private Color _originalLightColor;

    private void Start()
    {
        _block = new MaterialPropertyBlock();
        AutoFind();
        ApplyCurrentDifficulty();
    }

    private void OnDestroy()
    {
        // Playモード終了時にライト色を元に戻す
        if (directionalLight != null)
            directionalLight.color = _originalLightColor;
    }

    private void AutoFind()
    {
        if (directionalLight == null)
            directionalLight = FindObjectOfType<Light>();

        if (directionalLight != null)
            _originalLightColor = directionalLight.color;

        // BackgroundRoot は WorldCam の子なので相対パスで探す
        var bgRoot = GameObject.Find("BackgroundRoot");
        Transform bgTransform = bgRoot != null ? bgRoot.transform : null;

        if (bgTransform != null)
        {
            if (skyRenderer == null)
            {
                var t = bgTransform.Find("SkyQuad");
                if (t) skyRenderer = t.GetComponent<Renderer>();
            }
            if (hillsRenderer == null)
            {
                var t = bgTransform.Find("Hills_Far");
                if (t) hillsRenderer = t.GetComponent<Renderer>();
            }
            if (cloudRenderers == null || cloudRenderers.Length == 0)
            {
                var t = bgTransform.Find("Clouds");
                if (t) cloudRenderers = t.GetComponentsInChildren<Renderer>();
            }
        }

        Debug.Log($"[BgTinter] light={directionalLight?.name}, sky={skyRenderer?.name}, hills={hillsRenderer?.name}, clouds={cloudRenderers?.Length}");
    }

    private void ApplyCurrentDifficulty()
    {
        if (DifficultyManager.Instance == null) return;

        Difficulty diff = DifficultyManager.Instance.GetCurrentDifficulty();
        Debug.Log($"[BgTinter] Applying: {diff}");

        Color lightColor, skyColor;
        switch (diff)
        {
            case Difficulty.Normal:
                lightColor = lightColorDusk;  skyColor = skyColorDusk;
                break;
            case Difficulty.Hard:
                lightColor = lightColorNight; skyColor = skyColorNight;
                break;
            default: // Easy
                lightColor = lightColorDay;   skyColor = skyColorDay;
                break;
        }

        // ライト色変更（建物・道路・クマに反映）
        if (directionalLight != null)
            directionalLight.color = lightColor;

        // Unlit背景の個別tint
        ApplyColor(skyRenderer, skyColor);
        ApplyColor(hillsRenderer, skyColor);
        if (cloudRenderers != null)
            foreach (var cr in cloudRenderers)
                ApplyColor(cr, skyColor);
    }

    private void ApplyColor(Renderer r, Color color)
    {
        if (r == null) return;
        float originalAlpha = r.sharedMaterial != null ? r.sharedMaterial.color.a : 1f;
        color.a = originalAlpha;
        r.GetPropertyBlock(_block);
        _block.SetColor("_BaseColor", color);
        r.SetPropertyBlock(_block);
    }
}
