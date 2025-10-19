// Unity 2021+ / Unity 6
// メニュー: Tools > Create Result Canvas (KumaDen)
// ランクは画像ではなくテキスト＋色で表示

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class ResultCanvasAutoBuilder
{
    [MenuItem("Tools/Create Result Canvas (KumaDen)")]
    public static void CreateResultCanvas()
    {
        // Canvas --------------------------------------------------------------
        var goCanvas = new GameObject("ResultCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(goCanvas, "Create ResultCanvas");

        var canvas = goCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = goCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        // EventSystem（無ければ作成）
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        // SafeArea ------------------------------------------------------------
        var safe = CreateUI("SafeArea", goCanvas.transform);
        StretchFull(safe);
        SetOffsets(safe, left:32, right:32, top:64, bottom:64);

        // Backdrop ------------------------------------------------------------
        var backdrop = CreateImage("Backdrop", safe, null, new Color(1,1,1,0.85f));
        StretchFull(backdrop);

        // Header --------------------------------------------------------------
        var header = CreateUI("Header", safe);
        AnchorTopStretch(header, height:160);

        var title = CreateTMP("Title", header, "RESULT", 72, new Color32(0xD8,0x43,0x2E,255));
        AnchorTopCenter(title, size:new Vector2(800,90), posY:-40);

        var sub = CreateTMP("Sub", header, "♪ 1st Verse Clear – 00:37.50", 36, new Color32(0x2E,0x6F,0xBF,255));
        AnchorTopCenter(sub, size:new Vector2(900,70), posY:-110);

        // Left: Rank & Combo --------------------------------------------------
        var left = CreateCard("RankAndCombo", safe, new Vector2(480,640));
        AnchorTopLeft(left, pos:new Vector2(0,-200));

        // ← 画像ではなくテキストでランクを表示
        var rankText = CreateTMP("RankText", left, "A", 200, new Color32(0xFF,0xD1,0x00,255)); // デフォは金っぽい
        AnchorTopCenter(rankText, size:new Vector2(440,250), posY:-40);
        rankText.alignment = TextAlignmentOptions.Center;
        rankText.fontStyle = FontStyles.Bold;

        var comboGroup = CreateUI("MaxCombo", left);
        AnchorBottomCenter(comboGroup, height:180, posY:-20);

        var comboLabel = CreateTMP("Label", comboGroup, "MAX COMBO", 36, Color.white * 0.9f);
        AnchorTopCenter(comboLabel, size:new Vector2(440,60),  posY:-10);

        var comboValue = CreateTMP("Value", comboGroup, "86", 72, Color.white);
        AnchorTopCenter(comboValue, size:new Vector2(440,100), posY:-80);
        comboLabel.alignment = TextAlignmentOptions.Center;
        comboValue.alignment = TextAlignmentOptions.Center;

        // Right: JudgeBox -----------------------------------------------------
        var right = CreateCard("JudgeBox", safe, new Vector2(480,640));
        AnchorTopRight(right, pos:new Vector2(0,-200));

        CreateJudgeRow(right, "Row-Perfect", "PERFECT", "54", new Color32(0x2E,0x6F,0xBF,255), 0);
        CreateJudgeRow(right, "Row-Good",    "GOOD",    "22", new Color32(0x6B,0xB7,0x6E,255), 1);
        CreateJudgeRow(right, "Row-Miss",    "MISS",    "3",  new Color32(0xD8,0x43,0x2E,255), 2);

        // Items: Grid ---------------------------------------------------------
        var items = CreateCard("ItemsBox", safe, new Vector2(1016,360));
        AnchorMidStretch(items, height:360, posY:-60);

        var grid = items.gameObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.cellSize = new Vector2(280,280);
        grid.spacing = new Vector2(48,0);
        grid.childAlignment = TextAnchor.MiddleCenter;

        CreateItemCell(items, "Milk",  "×3");
        CreateItemCell(items, "Flour", "×2");
        CreateItemCell(items, "Egg",   "×4");

        // Footer Buttons ------------------------------------------------------
        var footer = CreateUI("FooterButtons", safe);
        AnchorBottomStretch(footer, height:160);
        var h = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 32; h.padding = new RectOffset(32,32,16,16);
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childForceExpandHeight = false; h.childForceExpandWidth = false;

        CreateButton(footer, "Btn-Retry", "RETRY", new Color32(0xD8,0x43,0x2E,255));
        CreateButton(footer, "Btn-Next",  "NEXT",  new Color32(0x9A,0xA3,0xAF,255));
        CreateButton(footer, "Btn-Title", "TITLE", new Color32(0x2E,0x6F,0xBF,255));

        // ResultUI binding ----------------------------------------------------
        var ui = goCanvas.AddComponent<ResultUI>();
        ui.titleText        = title;
        ui.subText          = sub;
        ui.rankText         = rankText;      // ← テキスト参照
        ui.comboValueText   = comboValue;
        ui.perfectValueText = right.Find("Row-Perfect/Value").GetComponent<TMP_Text>();
        ui.goodValueText    = right.Find("Row-Good/Value").GetComponent<TMP_Text>();
        ui.missValueText    = right.Find("Row-Miss/Value").GetComponent<TMP_Text>();
        ui.milkCountText    = items.Find("Milk/Count").GetComponent<TMP_Text>();
        ui.flourCountText   = items.Find("Flour/Count").GetComponent<TMP_Text>();
        ui.eggCountText     = items.Find("Egg/Count").GetComponent<TMP_Text>();

        // デフォルトのランク色（お好みで変更可）
        ui.rankColorS = new Color32(0xFF,0xD1,0x00,255); // 金
        ui.rankColorA = new Color32(0x2E,0x6F,0xBF,255); // 青
        ui.rankColorB = new Color32(0x4C,0xAF,0x50,255); // 緑
        ui.rankColorC = new Color32(0xB0,0x86,0x5A,255); // ブラウン

        Selection.activeObject = goCanvas;
    }

    // ---------- helpers ----------
    static RectTransform CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 200);
        return rt;
    }

    static Image CreateImage(string name, Transform parent, Sprite sp = null, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sp;
        img.color  = color ?? Color.white;
        return img;
    }

    static TMP_Text CreateTMP(string name, Transform parent, string text, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    static RectTransform CreateCard(string name, Transform parent, Vector2 size)
    {
        var card = CreateImage(name, parent, null, new Color(1,1,1,0.95f)).rectTransform;
        card.sizeDelta = size;
        return card;
    }

    static void CreateJudgeRow(Transform parent, string name, string label, string value, Color labelColor, int index)
    {
        var row = CreateUI(name, parent);
        var rt = row;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, 180);
        rt.anchoredPosition = new Vector2(0, -20 - index * 200);

        var bg = CreateImage("BG", row, null, new Color(1,1,1,0));
        StretchFull(bg);

        var labelTMP = CreateTMP("Label", row, label, 40, labelColor);
        labelTMP.alignment = TextAlignmentOptions.Left;
        var lrt = labelTMP.rectTransform;
        lrt.anchorMin = new Vector2(0, 0.5f);
        lrt.anchorMax = new Vector2(0, 0.5f);
        lrt.pivot = new Vector2(0, 0.5f);
        lrt.anchoredPosition = new Vector2(32, 0);
        lrt.sizeDelta = new Vector2(260, 90);

        var valueTMP = CreateTMP("Value", row, value, 64, Color.white);
        valueTMP.alignment = TextAlignmentOptions.Right;
        var vrt = valueTMP.rectTransform;
        vrt.anchorMin = new Vector2(1, 0.5f);
        vrt.anchorMax = new Vector2(1, 0.5f);
        vrt.pivot = new Vector2(1, 0.5f);
        vrt.anchoredPosition = new Vector2(-32, 0);
        vrt.sizeDelta = new Vector2(180, 100);
    }

    static void CreateItemCell(Transform parent, string name, string countText)
    {
        var cell = CreateUI(name, parent);

        var icon = CreateImage("Icon", cell, null, Color.white);
        var irt = icon.rectTransform;
        irt.anchorMin = new Vector2(0.5f, 1);
        irt.anchorMax = new Vector2(0.5f, 1);
        irt.pivot     = new Vector2(0.5f, 1);
        irt.sizeDelta = new Vector2(200, 200);
        irt.anchoredPosition = new Vector2(0, -10);

        var cnt = CreateTMP("Count", cell, countText, 56, Color.white);
        var crt = cnt.rectTransform;
        crt.anchorMin = new Vector2(0.5f, 0);
        crt.anchorMax = new Vector2(0.5f, 0);
        crt.pivot     = new Vector2(0.5f, 0);
        crt.sizeDelta = new Vector2(180, 70);
        crt.anchoredPosition = new Vector2(0, 0);
    }

    static Button CreateButton(Transform parent, string name, string label, Color color)
    {
        var rt = CreateUI(name, parent);
        rt.sizeDelta = new Vector2(300, 120);

        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;

        var btn = rt.gameObject.AddComponent<Button>();

        var txt = CreateTMP("Text", rt, label, 44, Color.white);
        StretchFull(txt);

        return btn;
    }

    // ----- Anchor helpers (Component対応) -----
    static RectTransform RT(Component c) => (c as RectTransform) ?? c.GetComponent<RectTransform>();

    static void StretchFull(Component c)
    { var rt = RT(c); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }

    static void SetOffsets(Component c, float left=0,float right=0,float top=0,float bottom=0)
    { var rt = RT(c); rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top); }

    static void AnchorTopStretch(Component c, float height)
    { var rt = RT(c); rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(1,1); rt.pivot = new Vector2(0.5f,1); rt.sizeDelta = new Vector2(0,height); rt.anchoredPosition = Vector2.zero; }

    static void AnchorBottomStretch(Component c, float height)
    { var rt = RT(c); rt.anchorMin = new Vector2(0,0); rt.anchorMax = new Vector2(1,0); rt.pivot = new Vector2(0.5f,0); rt.sizeDelta = new Vector2(0,height); rt.anchoredPosition = Vector2.zero; }

    static void AnchorTopLeft(Component c, Vector2? size=null, Vector2? pos=null)
    { var rt = RT(c); rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0,1); if(size!=null) rt.sizeDelta=(Vector2)size; rt.anchoredPosition = pos ?? Vector2.zero; }

    static void AnchorTopRight(Component c, Vector2? size=null, Vector2? pos=null)
    { var rt = RT(c); rt.anchorMin = new Vector2(1,1); rt.anchorMax = new Vector2(1,1); rt.pivot = new Vector2(1,1); if(size!=null) rt.sizeDelta=(Vector2)size; rt.anchoredPosition = pos ?? Vector2.zero; }

    static void AnchorTopCenter(Component c, Vector2? size=null, float posY=0)
    { var rt = RT(c); rt.anchorMin = new Vector2(0.5f,1); rt.anchorMax = new Vector2(0.5f,1); rt.pivot = new Vector2(0.5f,1); if(size!=null) rt.sizeDelta=(Vector2)size; rt.anchoredPosition = new Vector2(0,posY); }

    static void AnchorBottomCenter(Component c, float height, float posY=0)
    { var rt = RT(c); rt.anchorMin = new Vector2(0.5f,0); rt.anchorMax = new Vector2(0.5f,0); rt.pivot = new Vector2(0.5f,0); rt.sizeDelta = new Vector2(rt.sizeDelta.x, height); rt.anchoredPosition = new Vector2(0,posY); }

    static void AnchorMidStretch(Component c, float height, float posY=0)
    { var rt = RT(c); rt.anchorMin = new Vector2(0,0.5f); rt.anchorMax = new Vector2(1,0.5f); rt.pivot = new Vector2(0.5f,0.5f); rt.sizeDelta = new Vector2(0,height); rt.anchoredPosition = new Vector2(0,posY); }
}
