using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// 広告パネルUIを自動生成するエディタツール
/// Tools → Create Ad Panel UI から実行
/// </summary>
public class AdPanelCreator : EditorWindow
{
    [MenuItem("Tools/Create Ad Panel UI")]
    public static void CreateAdPanel()
    {
        // ResultCanvasを検索
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Canvas resultCanvas = null;
        
        foreach (var canvas in canvases)
        {
            if (canvas.gameObject.name == "ResultCanvas")
            {
                resultCanvas = canvas;
                break;
            }
        }
        
        if (resultCanvas == null)
        {
            Debug.LogError("[AdPanelCreator] ResultCanvas not found in scene!");
            EditorUtility.DisplayDialog("Error", "ResultCanvas not found in scene!", "OK");
            return;
        }
        
        // 既存のAdPanelを削除
        Transform existingPanel = resultCanvas.transform.Find("AdPanel");
        if (existingPanel != null)
        {
            Debug.Log("[AdPanelCreator] Removing existing AdPanel");
            DestroyImmediate(existingPanel.gameObject);
        }
        
        // AdPanel作成
        GameObject adPanel = new GameObject("AdPanel");
        adPanel.transform.SetParent(resultCanvas.transform, false);
        
        // RectTransform設定（全画面）
        RectTransform rectTransform = adPanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        // 背景Image（半透明黒）
        Image backgroundImage = adPanel.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.85f);
        
        // CanvasGroup（フェード制御用）
        CanvasGroup canvasGroup = adPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // コンテナ作成
        GameObject container = new GameObject("Container");
        container.transform.SetParent(adPanel.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(400f, 300f);
        containerRect.anchoredPosition = Vector2.zero;
        
        // アイコン背景
        GameObject iconBg = new GameObject("IconBackground");
        iconBg.transform.SetParent(container.transform, false);
        RectTransform iconBgRect = iconBg.AddComponent<RectTransform>();
        iconBgRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconBgRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconBgRect.sizeDelta = new Vector2(100f, 100f);
        iconBgRect.anchoredPosition = new Vector2(0f, 60f);
        Image iconBgImage = iconBg.AddComponent<Image>();
        iconBgImage.color = new Color(1f, 1f, 1f, 0.1f);
        
        // "AD" テキスト
        GameObject adText = new GameObject("AdText");
        adText.transform.SetParent(container.transform, false);
        RectTransform adTextRect = adText.AddComponent<RectTransform>();
        adTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        adTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        adTextRect.sizeDelta = new Vector2(400f, 80f);
        adTextRect.anchoredPosition = new Vector2(0f, 0f);
        
        TextMeshProUGUI adTMP = adText.AddComponent<TextMeshProUGUI>();
        adTMP.text = "AD";
        adTMP.fontSize = 72;
        adTMP.color = Color.white;
        adTMP.alignment = TextAlignmentOptions.Center;
        adTMP.fontStyle = FontStyles.Bold;
        
        // サブテキスト "Please wait..."
        GameObject subText = new GameObject("SubText");
        subText.transform.SetParent(container.transform, false);
        RectTransform subTextRect = subText.AddComponent<RectTransform>();
        subTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        subTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        subTextRect.sizeDelta = new Vector2(400f, 40f);
        subTextRect.anchoredPosition = new Vector2(0f, -60f);
        
        TextMeshProUGUI subTMP = subText.AddComponent<TextMeshProUGUI>();
        subTMP.text = "Please wait...";
        subTMP.fontSize = 24;
        subTMP.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        subTMP.alignment = TextAlignmentOptions.Center;
        
        // プログレスバー背景
        GameObject progressBg = new GameObject("ProgressBarBackground");
        progressBg.transform.SetParent(container.transform, false);
        RectTransform progressBgRect = progressBg.AddComponent<RectTransform>();
        progressBgRect.anchorMin = new Vector2(0.5f, 0f);
        progressBgRect.anchorMax = new Vector2(0.5f, 0f);
        progressBgRect.sizeDelta = new Vector2(300f, 4f);
        progressBgRect.anchoredPosition = new Vector2(0f, 30f);
        Image progressBgImage = progressBg.AddComponent<Image>();
        progressBgImage.color = new Color(1f, 1f, 1f, 0.2f);
        
        // プログレスバー
        GameObject progressBar = new GameObject("ProgressBar");
        progressBar.transform.SetParent(progressBg.transform, false);
        RectTransform progressBarRect = progressBar.AddComponent<RectTransform>();
        progressBarRect.anchorMin = new Vector2(0f, 0.5f);
        progressBarRect.anchorMax = new Vector2(0f, 0.5f);
        progressBarRect.pivot = new Vector2(0f, 0.5f);
        progressBarRect.sizeDelta = new Vector2(300f, 4f);
        progressBarRect.anchoredPosition = Vector2.zero;
        Image progressBarImage = progressBar.AddComponent<Image>();
        progressBarImage.color = new Color(1f, 0.8f, 0f, 1f);
        progressBarImage.type = Image.Type.Filled;
        progressBarImage.fillMethod = Image.FillMethod.Horizontal;
        progressBarImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressBarImage.fillAmount = 0f;
        
        // AdPhaseManagerにバインド
        AdPhaseManager adManager = FindObjectOfType<AdPhaseManager>();
        if (adManager != null)
        {
            SerializedObject so = new SerializedObject(adManager);
            so.FindProperty("adPanelCanvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("progressBarImage").objectReferenceValue = progressBarImage;
            so.ApplyModifiedProperties();
            Debug.Log("[AdPanelCreator] AdPanel bound to AdPhaseManager");
        }
        else
        {
            Debug.LogWarning("[AdPanelCreator] AdPhaseManager not found - please bind manually");
        }
        
        // 初期状態は非表示
        adPanel.SetActive(false);
        
        // 選択状態にする
        Selection.activeGameObject = adPanel;
        
        Debug.Log("[AdPanelCreator] ✅ Ad Panel UI created successfully!");
        EditorUtility.DisplayDialog("Success", "Ad Panel UI created successfully!\n\nCheck ResultCanvas → AdPanel", "OK");
    }
}
