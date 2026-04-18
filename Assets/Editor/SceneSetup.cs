using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SceneSetup
{
    [MenuItem("TowerRush/Setup Scene")]
    static void SetupScene()
    {
        // ── Clean up any previous setup ──────────────────────────────────────
        foreach (string n in new[] { "GameManager", "TowerManager", "UIManager", "Canvas", "EventSystem", "Crane" })
        {
            GameObject old = GameObject.Find(n);
            if (old != null) Object.DestroyImmediate(old);
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var cf = mainCam.GetComponent<CameraFollow>();
            if (cf != null) Object.DestroyImmediate(cf);
        }

        // ── EventSystem ───────────────────────────────────────────────────────
        GameObject esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // ── GameManager ───────────────────────────────────────────────────────
        new GameObject("GameManager").AddComponent<GameManager>();

        // ── TowerManager ──────────────────────────────────────────────────────
        GameObject tmGO = new GameObject("TowerManager");
        TowerManager towerManager = tmGO.AddComponent<TowerManager>();

        SerializedObject tmSO = new SerializedObject(towerManager);
        SerializedProperty prefabsProp = tmSO.FindProperty("blockPrefabs");
        prefabsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Block_{i + 1}.prefab");
            prefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
        }
        tmSO.ApplyModifiedProperties();

        // ── Camera ────────────────────────────────────────────────────────────
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 8f;
            mainCam.backgroundColor = new Color(0.07f, 0.07f, 0.13f);
            mainCam.gameObject.AddComponent<CameraFollow>();
        }

        // ── Crane ─────────────────────────────────────────────────────────────
        GameObject craneGO = new GameObject("Crane");
        craneGO.AddComponent<LineRenderer>();
        CraneController craneCtrl = craneGO.AddComponent<CraneController>();

        // Gantry (horizontal bar)
        GameObject gantryGO = new GameObject("Gantry");
        gantryGO.transform.SetParent(craneGO.transform, false);
        gantryGO.transform.localPosition = Vector3.zero;
        gantryGO.transform.localScale    = new Vector3(1.4f, 0.18f, 1f);
        SpriteRenderer gantrySR = gantryGO.AddComponent<SpriteRenderer>();
        gantrySR.color        = new Color(0.35f, 0.38f, 0.45f);
        gantrySR.sortingOrder = 3;

        // Cab (small box hanging from centre)
        GameObject cabGO = new GameObject("Cab");
        cabGO.transform.SetParent(craneGO.transform, false);
        cabGO.transform.localPosition = new Vector3(0f, -0.20f, 0f);
        cabGO.transform.localScale    = new Vector3(0.22f, 0.30f, 1f);
        SpriteRenderer cabSR = cabGO.AddComponent<SpriteRenderer>();
        cabSR.color        = new Color(0.35f, 0.38f, 0.45f);
        cabSR.sortingOrder = 3;

        // Hook visual (world-space child — stays in hierarchy but moves in Update)
        GameObject hookGO = new GameObject("HookVisual");
        hookGO.transform.SetParent(craneGO.transform, false);
        hookGO.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
        SpriteRenderer hookSR = hookGO.AddComponent<SpriteRenderer>();
        hookSR.color        = new Color(0.5f, 0.52f, 0.60f);
        hookSR.sortingOrder = 5;

        // HookAttachPoint — child of HookVisual, user moves it to the hook tip
        GameObject attachGO = new GameObject("HookAttachPoint");
        attachGO.transform.SetParent(hookGO.transform, false);
        attachGO.transform.localPosition = Vector3.zero;

        // Wire serialised fields
        SerializedObject craneSO = new SerializedObject(craneCtrl);
        craneSO.FindProperty("hookAttachPoint").objectReferenceValue = attachGO.transform;
        craneSO.FindProperty("gantryRenderer").objectReferenceValue  = gantrySR;
        craneSO.FindProperty("cabRenderer").objectReferenceValue     = cabSR;
        craneSO.FindProperty("hookRenderer").objectReferenceValue    = hookSR;
        craneSO.FindProperty("hookVisual").objectReferenceValue      = hookGO.transform;
        craneSO.ApplyModifiedProperties();

        // ── Canvas ────────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Panels ────────────────────────────────────────────────────────────
        GameObject menuPanel   = BuildMenuPanel(canvasGO.transform);
        GameObject hudPanel    = BuildHudPanel(canvasGO.transform,
                                     out TMP_Text multText,
                                     out TMP_Text floorText,
                                     out TMP_Text betText,
                                     out Button   cashOutBtn);
        GameObject resultPanel = BuildResultPanel(canvasGO.transform,
                                     out TMP_Text resTitleText,
                                     out TMP_Text resAmountText,
                                     out Button   playAgainBtn,
                                     out Button   menuBtn);

        TMP_InputField betInput = menuPanel.GetComponentInChildren<TMP_InputField>();
        Button playBtn = menuPanel.transform.Find("PlayButton")?.GetComponent<Button>();

        // ── UIManager ─────────────────────────────────────────────────────────
        GameObject uiGO = new GameObject("UIManager");
        UIManager uiManager = uiGO.AddComponent<UIManager>();

        SerializedObject uiSO = new SerializedObject(uiManager);
        uiSO.FindProperty("menuPanel").objectReferenceValue        = menuPanel;
        uiSO.FindProperty("betInput").objectReferenceValue         = betInput;
        uiSO.FindProperty("playButton").objectReferenceValue       = playBtn;
        uiSO.FindProperty("hudPanel").objectReferenceValue         = hudPanel;
        uiSO.FindProperty("multiplierText").objectReferenceValue   = multText;
        uiSO.FindProperty("floorText").objectReferenceValue        = floorText;
        uiSO.FindProperty("betText").objectReferenceValue          = betText;
        uiSO.FindProperty("cashOutButton").objectReferenceValue    = cashOutBtn;
        uiSO.FindProperty("resultPanel").objectReferenceValue      = resultPanel;
        uiSO.FindProperty("resultTitleText").objectReferenceValue  = resTitleText;
        uiSO.FindProperty("resultAmountText").objectReferenceValue = resAmountText;
        uiSO.FindProperty("playAgainButton").objectReferenceValue  = playAgainBtn;
        uiSO.FindProperty("menuButton").objectReferenceValue       = menuBtn;
        uiSO.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("TowerRush scene setup complete! Press Ctrl+S to save the scene.");
    }

    // ── Panel Builders ────────────────────────────────────────────────────────

    static GameObject BuildMenuPanel(Transform parent)
    {
        GameObject panel = FullScreenPanel("MenuPanel", parent, new Color(0.07f, 0.07f, 0.13f));

        // Title
        var title = MakeText("TitleText", panel.transform, "TOWER RUSH", 90, FontStyles.Bold, Color.white);
        Anchor(title.rectTransform, 0.5f, 0.72f, 860, 130);
        title.alignment = TextAlignmentOptions.Center;

        // Subtitle
        var sub = MakeText("SubText", panel.transform, "Stack blocks to win", 36, FontStyles.Normal, new Color(0.6f, 0.6f, 0.7f));
        Anchor(sub.rectTransform, 0.5f, 0.63f, 700, 55);
        sub.alignment = TextAlignmentOptions.Center;

        // Bet label
        var betLbl = MakeText("BetLabel", panel.transform, "BET AMOUNT", 30, FontStyles.Normal, new Color(0.7f, 0.7f, 0.8f));
        Anchor(betLbl.rectTransform, 0.5f, 0.51f, 500, 50);
        betLbl.alignment = TextAlignmentOptions.Center;

        // Bet input
        GameObject input = MakeInputField("BetInput", panel.transform, "10");
        Anchor(input.GetComponent<RectTransform>(), 0.5f, 0.44f, 420, 85);

        // Play button
        Button playBtn = MakeButton("PlayButton", panel.transform, "PLAY", new Color(0.18f, 0.78f, 0.42f));
        Anchor(playBtn.GetComponent<RectTransform>(), 0.5f, 0.33f, 420, 105);

        return panel;
    }

    static GameObject BuildHudPanel(Transform parent,
        out TMP_Text multText, out TMP_Text floorText, out TMP_Text betText, out Button cashOutBtn)
    {
        GameObject panel = new GameObject("HudPanel");
        panel.transform.SetParent(parent, false);
        StretchFull(panel.AddComponent<RectTransform>());

        // Multiplier
        multText = MakeText("MultiplierText", panel.transform, "1.00x", 100, FontStyles.Bold, new Color(1f, 0.85f, 0.2f));
        Anchor(multText.rectTransform, 0.5f, 0.88f, 750, 135);
        multText.alignment = TextAlignmentOptions.Center;

        // Floor
        floorText = MakeText("FloorText", panel.transform, "Floor 0", 42, FontStyles.Normal, Color.white);
        Anchor(floorText.rectTransform, 0.5f, 0.80f, 500, 65);
        floorText.alignment = TextAlignmentOptions.Center;

        // Bet
        betText = MakeText("BetText", panel.transform, "Bet: 10.00", 32, FontStyles.Normal, new Color(0.6f, 0.6f, 0.7f));
        Anchor(betText.rectTransform, 0.5f, 0.74f, 400, 50);
        betText.alignment = TextAlignmentOptions.Center;

        // Cash Out
        cashOutBtn = MakeButton("CashOutButton", panel.transform, "CASH OUT", new Color(0.92f, 0.60f, 0.08f));
        Anchor(cashOutBtn.GetComponent<RectTransform>(), 0.5f, 0.07f, 520, 125);

        return panel;
    }

    static GameObject BuildResultPanel(Transform parent,
        out TMP_Text titleText, out TMP_Text amountText, out Button playAgainBtn, out Button menuBtn)
    {
        // Dark overlay
        GameObject overlay = FullScreenPanel("ResultPanel", parent, new Color(0f, 0f, 0f, 0.78f));

        // Card
        GameObject card = new GameObject("Card");
        card.transform.SetParent(overlay.transform, false);
        Image cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.11f, 0.11f, 0.20f);
        RectTransform crt = card.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(820, 720);
        crt.anchoredPosition = Vector2.zero;

        // Result title
        titleText = MakeText("ResultTitle", card.transform, "CASHED OUT!", 80, FontStyles.Bold, new Color(0.25f, 1f, 0.5f));
        Anchor(titleText.rectTransform, 0.5f, 0.76f, 740, 115);
        titleText.alignment = TextAlignmentOptions.Center;

        // Amount
        amountText = MakeText("ResultAmount", card.transform, "+0.00", 72, FontStyles.Bold, Color.white);
        Anchor(amountText.rectTransform, 0.5f, 0.56f, 740, 105);
        amountText.alignment = TextAlignmentOptions.Center;

        // Play Again
        playAgainBtn = MakeButton("PlayAgainButton", card.transform, "PLAY AGAIN", new Color(0.18f, 0.78f, 0.42f));
        Anchor(playAgainBtn.GetComponent<RectTransform>(), 0.5f, 0.33f, 520, 105);

        // Menu
        menuBtn = MakeButton("MenuButton", card.transform, "MENU", new Color(0.28f, 0.28f, 0.38f));
        Anchor(menuBtn.GetComponent<RectTransform>(), 0.5f, 0.16f, 520, 105);

        return overlay;
    }

    // ── UI Helpers ────────────────────────────────────────────────────────────

    static GameObject FullScreenPanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        StretchFull(go.GetComponent<RectTransform>());
        return go;
    }

    static TMP_Text MakeText(string name, Transform parent, string text, int size, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = color;
        return tmp;
    }

    static Button MakeButton(string name, Transform parent, string label, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgColor;
        Button btn = go.AddComponent<Button>();

        // Label inside button
        var lbl = MakeText("Label", go.transform, label, 50, FontStyles.Bold, Color.white);
        lbl.alignment = TextAlignmentOptions.Center;
        StretchFull(lbl.rectTransform);

        return btn;
    }

    static GameObject MakeInputField(string name, Transform parent, string placeholder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.28f);

        TMP_InputField field = go.AddComponent<TMP_InputField>();
        field.contentType = TMP_InputField.ContentType.DecimalNumber;

        // Text area with mask
        GameObject area = new GameObject("Text Area");
        area.transform.SetParent(go.transform, false);
        RectTransform areaRT = area.AddComponent<RectTransform>();
        areaRT.anchorMin = Vector2.zero;
        areaRT.anchorMax = Vector2.one;
        areaRT.offsetMin = new Vector2(12, 6);
        areaRT.offsetMax = new Vector2(-12, -6);
        area.AddComponent<RectMask2D>();

        // Placeholder text
        var ph = MakeText("Placeholder", area.transform, placeholder, 42, FontStyles.Normal, new Color(0.5f, 0.5f, 0.55f));
        ph.alignment = TextAlignmentOptions.Center;
        StretchFull(ph.rectTransform);

        // Input text
        var inputTxt = MakeText("Text", area.transform, "", 42, FontStyles.Normal, Color.white);
        inputTxt.alignment = TextAlignmentOptions.Center;
        StretchFull(inputTxt.rectTransform);

        field.textViewport  = areaRT;
        field.textComponent = inputTxt;
        field.placeholder   = ph;

        return go;
    }

    static void Anchor(RectTransform rt, float anchorX, float anchorY, float width, float height)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(anchorX, anchorY);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = Vector2.zero;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
