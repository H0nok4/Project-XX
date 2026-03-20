using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PrototypeGameplayUiPrefabBuilder
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string UiFolder = "Assets/Resources/UI";
    private const string InteractionFolder = "Assets/Resources/UI/Interaction";
    private const string RaidFolder = "Assets/Resources/UI/Raid";
    private const string FpsFolder = "Assets/Resources/UI/FPS";
    private const string BaseFolder = "Assets/Resources/UI/Base";

    private const string InteractionPromptPrefabPath = "Assets/Resources/UI/Interaction/InteractionPrompt.prefab";
    private const string RaidHudPrefabPath = "Assets/Resources/UI/Raid/RaidHud.prefab";
    private const string RaidResultViewPrefabPath = "Assets/Resources/UI/Raid/RaidResultView.prefab";
    private const string RaidReturnButtonPrefabPath = "Assets/Resources/UI/Raid/RaidReturnButton.prefab";
    private const string FpsHudPrefabPath = "Assets/Resources/UI/FPS/PrototypeFpsHud.prefab";
    private const string TargetHealthBarPrefabPath = "Assets/Resources/UI/FPS/PrototypeTargetHealthBar.prefab";
    private const string CombatTextEntryPrefabPath = "Assets/Resources/UI/FPS/PrototypeCombatTextEntry.prefab";
    private const string BaseHubOverlayPrefabPath = "Assets/Resources/UI/Base/BaseHubOverlay.prefab";

    [MenuItem("Tools/Prototype/Build Gameplay UI Prefabs")]
    public static void BuildPrefabs()
    {
        string result = BuildPrefabsAndReport();
        if (!string.IsNullOrWhiteSpace(result))
        {
            Debug.Log(result);
        }
    }

    public static string BuildPrefabsAndReport()
    {
        EnsureFolder(ResourcesFolder);
        EnsureFolder(UiFolder);
        EnsureFolder(InteractionFolder);
        EnsureFolder(RaidFolder);
        EnsureFolder(FpsFolder);
        EnsureFolder(BaseFolder);

        BuildInteractionPromptPrefab();
        BuildRaidHudPrefab();
        BuildRaidResultViewPrefab();
        BuildRaidReturnButtonPrefab();
        BuildFpsHudPrefab();
        BuildTargetHealthBarPrefab();
        BuildCombatTextEntryPrefab();
        BuildBaseHubOverlayPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return $"Built {InteractionPromptPrefabPath}, {RaidHudPrefabPath}, {RaidResultViewPrefabPath}, {RaidReturnButtonPrefabPath}, {FpsHudPrefabPath}, {TargetHealthBarPrefabPath}, {CombatTextEntryPrefabPath}, {BaseHubOverlayPrefabPath}";
    }

    public static string TryBuildPrefabs()
    {
        try
        {
            return BuildPrefabsAndReport();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return exception.ToString();
        }
    }

    private static void BuildInteractionPromptPrefab()
    {
        GameObject root = new GameObject("InteractionPrompt", typeof(RectTransform));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetAnchor(
                rootRect,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 36f),
                new Vector2(320f, 42f));

            Image background = root.AddComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);
            background.raycastTarget = false;

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

            RectTransform labelRoot = CreateRectTransform("Label", rootRect);
            SetStretch(labelRoot, 12f, 12f, 8f, 8f);
            TMP_Text promptText = CreateText(labelRoot, "PromptText", string.Empty, 14, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

            InteractionPromptTemplate template = root.AddComponent<InteractionPromptTemplate>();
            template.ConfigureReferences(rootRect, background, canvasGroup, promptText);

            PrefabUtility.SaveAsPrefabAsset(root, InteractionPromptPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildRaidHudPrefab()
    {
        GameObject root = new GameObject("RaidHud", typeof(RectTransform));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetStretch(rootRect, 0f, 0f, 0f, 0f);

            RectTransform summaryPanel = CreateRectTransform("SummaryPanel", rootRect);
            SetAnchor(
                summaryPanel,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -18f),
                new Vector2(320f, 72f));
            Image summaryBackground = summaryPanel.gameObject.AddComponent<Image>();
            summaryBackground.color = new Color(0.08f, 0.1f, 0.14f, 0.88f);
            summaryBackground.raycastTarget = false;
            VerticalLayoutGroup summaryLayout = summaryPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            summaryLayout.padding = new RectOffset(12, 12, 10, 10);
            summaryLayout.spacing = 0f;
            summaryLayout.childAlignment = TextAnchor.UpperLeft;
            summaryLayout.childControlWidth = true;
            summaryLayout.childControlHeight = true;
            summaryLayout.childForceExpandWidth = true;
            summaryLayout.childForceExpandHeight = true;

            TMP_Text summaryText = CreateText(summaryPanel, "SummaryText", string.Empty, 14, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
            LayoutElement summaryTextLayout = summaryText.gameObject.AddComponent<LayoutElement>();
            summaryTextLayout.flexibleWidth = 1f;
            summaryTextLayout.flexibleHeight = 1f;

            RectTransform extractionPanel = CreateRectTransform("ExtractionPanel", rootRect);
            SetAnchor(
                extractionPanel,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -96f),
                new Vector2(320f, 44f));
            Image extractionBackground = extractionPanel.gameObject.AddComponent<Image>();
            extractionBackground.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
            extractionBackground.raycastTarget = false;
            VerticalLayoutGroup extractionLayout = extractionPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            extractionLayout.padding = new RectOffset(10, 10, 8, 8);
            extractionLayout.spacing = 8f;
            extractionLayout.childAlignment = TextAnchor.UpperLeft;
            extractionLayout.childControlWidth = true;
            extractionLayout.childControlHeight = true;
            extractionLayout.childForceExpandWidth = true;
            extractionLayout.childForceExpandHeight = false;

            TMP_Text extractionText = CreateText(extractionPanel, "ExtractionText", string.Empty, 14, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
            RectTransform barTrack = CreateRectTransform("BarTrack", extractionPanel);
            LayoutElement barLayout = barTrack.gameObject.AddComponent<LayoutElement>();
            barLayout.preferredHeight = 14f;
            Image barBackground = barTrack.gameObject.AddComponent<Image>();
            barBackground.color = new Color(0f, 0f, 0f, 0.45f);
            barBackground.raycastTarget = false;

            RectTransform fillRoot = CreateRectTransform("Fill", barTrack);
            fillRoot.anchorMin = new Vector2(0f, 0f);
            fillRoot.anchorMax = new Vector2(0f, 1f);
            fillRoot.pivot = new Vector2(0f, 0.5f);
            fillRoot.offsetMin = Vector2.zero;
            fillRoot.offsetMax = Vector2.zero;
            Image fillImage = fillRoot.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.22f, 0.86f, 0.48f, 0.95f);
            fillImage.raycastTarget = false;

            RaidHudTemplate template = root.AddComponent<RaidHudTemplate>();
            template.ConfigureReferences(rootRect, summaryText, extractionPanel, extractionText, barTrack, fillImage);

            PrefabUtility.SaveAsPrefabAsset(root, RaidHudPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildRaidResultViewPrefab()
    {
        GameObject root = new GameObject("RaidResultView", typeof(RectTransform));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetAnchor(
                rootRect,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -24f),
                new Vector2(500f, 220f));

            Image background = root.AddComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);
            background.raycastTarget = false;

            VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 16);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = root.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text titleText = CreateText(rootRect, "TitleText", string.Empty, 22, FontStyle.Bold, Color.white, TextAnchor.UpperCenter);
            titleText.lineSpacing = 0.95f;

            TMP_Text bodyText = CreateText(rootRect, "BodyText", string.Empty, 16, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
            bodyText.lineSpacing = 0.92f;

            RaidResultViewTemplate template = root.AddComponent<RaidResultViewTemplate>();
            template.ConfigureReferences(rootRect, titleText, bodyText);

            PrefabUtility.SaveAsPrefabAsset(root, RaidResultViewPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildRaidReturnButtonPrefab()
    {
        GameObject root = new GameObject("RaidReturnButton", typeof(RectTransform));
        try
        {
            RectTransform rectTransform = root.GetComponent<RectTransform>();
            SetAnchor(
                rectTransform,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-40f, 34f),
                new Vector2(180f, 42f));

            Button button = CreateButton(rectTransform, "ReturnButton", "Return To Menu", 42f, out TMP_Text labelText);
            SetStretch(button.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

            RaidReturnButtonTemplate template = root.AddComponent<RaidReturnButtonTemplate>();
            template.ConfigureReferences(rectTransform, button, labelText);

            PrefabUtility.SaveAsPrefabAsset(root, RaidReturnButtonPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildFpsHudPrefab()
    {
        GameObject root = new GameObject("PrototypeFpsHud", typeof(RectTransform));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetStretch(rootRect, 0f, 0f, 0f, 0f);

            RectTransform crosshairRoot = CreateRectTransform("Crosshair", rootRect);
            SetAnchor(
                crosshairRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(40f, 40f));
            TMP_Text crosshairText = CreateText(crosshairRoot, "CrosshairText", "+", 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            SetStretch(crosshairText.rectTransform, 0f, 0f, 0f, 0f);

            RectTransform controlsPanel = CreateRectTransform("ControlsPanel", rootRect);
            SetAnchor(
                controlsPanel,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -100f),
                new Vector2(380f, 0f));
            Image controlsBackground = controlsPanel.gameObject.AddComponent<Image>();
            controlsBackground.color = new Color(0.08f, 0.1f, 0.14f, 0.88f);
            controlsBackground.raycastTarget = false;
            VerticalLayoutGroup controlsLayout = controlsPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            controlsLayout.padding = new RectOffset(12, 12, 10, 10);
            controlsLayout.spacing = 0f;
            controlsLayout.childAlignment = TextAnchor.UpperLeft;
            controlsLayout.childControlWidth = true;
            controlsLayout.childControlHeight = true;
            controlsLayout.childForceExpandWidth = true;
            controlsLayout.childForceExpandHeight = false;
            ContentSizeFitter controlsFitter = controlsPanel.gameObject.AddComponent<ContentSizeFitter>();
            controlsFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            controlsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TMP_Text controlsText = CreateText(
                controlsPanel,
                "ControlsText",
                "移动 WASD\n观察 鼠标\n攻击 左键\n投掷 G\n交互 E\n背包 Tab\n切枪 1 / 2 / 3\n换弹 R\n切换射击模式 B\n快速治疗 4\n止血 5\n夹板 6\n止痛 7\n跳跃 Space\n冲刺 Shift\n切换蹲伏 C\n步速 LeftCtrl + 滚轮\n切换鼠标 Esc",
                14,
                FontStyle.Normal,
                Color.white,
                TextAnchor.UpperLeft);
            controlsText.lineSpacing = 0.9f;

            RectTransform staminaPanel = CreateRectTransform("StaminaPanel", rootRect);
            SetAnchor(
                staminaPanel,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(18f, 18f),
                new Vector2(280f, 24f));
            Image staminaBackground = staminaPanel.gameObject.AddComponent<Image>();
            staminaBackground.color = new Color(0.08f, 0.1f, 0.14f, 0.88f);
            staminaBackground.raycastTarget = false;

            RectTransform staminaTrack = CreateRectTransform("StaminaTrack", staminaPanel);
            SetStretch(staminaTrack, 3f, 3f, 3f, 3f);
            Image staminaTrackBackground = staminaTrack.gameObject.AddComponent<Image>();
            staminaTrackBackground.color = new Color(0f, 0f, 0f, 0.08f);
            staminaTrackBackground.raycastTarget = false;

            RectTransform staminaFillRoot = CreateRectTransform("StaminaFill", staminaTrack);
            staminaFillRoot.anchorMin = new Vector2(0f, 0f);
            staminaFillRoot.anchorMax = new Vector2(0f, 1f);
            staminaFillRoot.pivot = new Vector2(0f, 0.5f);
            staminaFillRoot.offsetMin = Vector2.zero;
            staminaFillRoot.offsetMax = Vector2.zero;
            Image staminaFillImage = staminaFillRoot.gameObject.AddComponent<Image>();
            staminaFillImage.color = new Color(0.27f, 0.82f, 0.38f, 0.95f);
            staminaFillImage.raycastTarget = false;

            TMP_Text staminaLabelText = CreateText(staminaPanel, "StaminaLabelText", string.Empty, 13, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
            SetStretch(staminaLabelText.rectTransform, 0f, 0f, 0f, 0f);

            RectTransform weaponPanel = CreateRectTransform("WeaponPanel", rootRect);
            SetAnchor(
                weaponPanel,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-30f, -18f),
                new Vector2(364f, 0f));
            Image weaponBackground = weaponPanel.gameObject.AddComponent<Image>();
            weaponBackground.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);
            weaponBackground.raycastTarget = false;
            VerticalLayoutGroup weaponLayout = weaponPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            weaponLayout.padding = new RectOffset(12, 12, 10, 10);
            weaponLayout.spacing = 0f;
            weaponLayout.childAlignment = TextAnchor.UpperLeft;
            weaponLayout.childControlWidth = true;
            weaponLayout.childControlHeight = true;
            weaponLayout.childForceExpandWidth = true;
            weaponLayout.childForceExpandHeight = false;
            ContentSizeFitter weaponFitter = weaponPanel.gameObject.AddComponent<ContentSizeFitter>();
            weaponFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            weaponFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TMP_Text weaponInfoText = CreateText(weaponPanel, "WeaponInfoText", string.Empty, 14, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
            weaponInfoText.lineSpacing = 0.92f;

            PrototypeFpsHudTemplate template = root.AddComponent<PrototypeFpsHudTemplate>();
            template.ConfigureReferences(rootRect, crosshairText, controlsText, staminaTrack, staminaFillImage, staminaLabelText, weaponInfoText);

            PrefabUtility.SaveAsPrefabAsset(root, FpsHudPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildTargetHealthBarPrefab()
    {
        GameObject root = new GameObject("PrototypeTargetHealthBar", typeof(RectTransform));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetAnchor(
                rootRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(108f, 34f));

            TMP_Text levelLabelText = CreateText(rootRect, "LevelLabelText", string.Empty, 11, FontStyle.Bold, new Color(1f, 1f, 1f, 0.96f), TextAnchor.MiddleCenter);
            SetAnchor(
                levelLabelText.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(120f, 16f));

            RectTransform borderRect = CreateRectTransform("Border", rootRect);
            SetAnchor(
                borderRect,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                Vector2.zero,
                new Vector2(84f, 10f));
            Image borderImage = borderRect.gameObject.AddComponent<Image>();
            borderImage.color = new Color(0f, 0f, 0f, 0.95f);
            borderImage.raycastTarget = false;

            RectTransform backgroundRect = CreateRectTransform("Background", borderRect);
            SetStretch(backgroundRect, 1f, 1f, 1f, 1f);
            Image backgroundImage = backgroundRect.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            backgroundImage.raycastTarget = false;

            RectTransform fillRect = CreateRectTransform("Fill", backgroundRect);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fillRect.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.18f, 0.85f, 0.31f, 1f);
            fillImage.raycastTarget = false;

            PrototypeTargetHealthBarTemplate template = root.AddComponent<PrototypeTargetHealthBarTemplate>();
            template.ConfigureReferences(rootRect, levelLabelText, borderRect, backgroundImage, fillImage);

            PrefabUtility.SaveAsPrefabAsset(root, TargetHealthBarPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildCombatTextEntryPrefab()
    {
        GameObject root = new GameObject("PrototypeCombatTextEntry", typeof(RectTransform));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetAnchor(
                rootRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(132f, 40f));

            TMP_Text labelText = CreateText(rootRect, "LabelText", string.Empty, 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            SetStretch(labelText.rectTransform, 0f, 0f, 0f, 0f);
            Shadow shadow = labelText.gameObject.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;

            PrototypeCombatTextEntryTemplate template = root.AddComponent<PrototypeCombatTextEntryTemplate>();
            template.ConfigureReferences(rootRect, labelText, shadow);

            PrefabUtility.SaveAsPrefabAsset(root, CombatTextEntryPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildBaseHubOverlayPrefab()
    {
        GameObject root = new GameObject("BaseHubOverlay", typeof(RectTransform));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetAnchor(
                rootRect,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(28f, -28f),
                new Vector2(520f, 120f));

            Image background = root.AddComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
            background.raycastTarget = false;

            VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TMP_Text titleText = CreateText(rootRect, "TitleText", string.Empty, 20, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
            TMP_Text bodyText = CreateText(rootRect, "BodyText", string.Empty, 13, FontStyle.Normal, new Color(0.92f, 0.94f, 0.98f, 1f), TextAnchor.UpperLeft);

            BaseHubOverlayTemplate template = root.AddComponent<BaseHubOverlayTemplate>();
            template.ConfigureReferences(rootRect, background, titleText, bodyText);

            PrefabUtility.SaveAsPrefabAsset(root, BaseHubOverlayPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        float preferredHeight,
        out TMP_Text labelText)
    {
        RectTransform rectTransform = CreateRectTransform(name, parent);
        Image background = rectTransform.gameObject.AddComponent<Image>();
        Color normalColor = new Color(0.2f, 0.27f, 0.36f, 0.98f);
        Color highlightedColor = new Color(0.29f, 0.38f, 0.49f, 1f);
        Color pressedColor = new Color(0.16f, 0.22f, 0.3f, 1f);
        background.color = normalColor;

        Button button = rectTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.selectedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        colors.disabledColor = new Color(normalColor.r * 0.65f, normalColor.g * 0.65f, normalColor.b * 0.65f, 0.7f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.flexibleWidth = 1f;

        RectTransform labelRoot = CreateRectTransform("Label", rectTransform);
        SetStretch(labelRoot, 12f, 12f, 8f, 8f);
        labelText = CreateText(labelRoot, "Text", label, 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        return button;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string text,
        int fontSize,
        FontStyle fontStyle,
        Color color,
        TextAnchor anchor)
    {
        RectTransform rectTransform = CreateRectTransform(name, parent);
        TextMeshProUGUI label = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset fontAsset = ResolveFontAsset();
        if (fontAsset != null)
        {
            label.font = fontAsset;
        }

        label.fontSize = fontSize;
        label.fontStyle = PrototypeUiToolkit.ConvertFontStyle(fontStyle);
        label.color = color;
        label.alignment = PrototypeUiToolkit.ConvertTextAlignment(anchor);
        label.richText = true;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        label.text = text ?? string.Empty;
        label.margin = Vector4.zero;
        return label;
    }

    private static TMP_FontAsset ResolveFontAsset()
    {
        TMP_Settings settings = TMP_Settings.instance;
        TMP_FontAsset defaultFontAsset = settings != null ? TMP_Settings.defaultFontAsset : null;
        return defaultFontAsset != null ? defaultFontAsset : PrototypeUiToolkit.ResolveTmpFontAsset(PrototypeUiToolkit.ResolveDefaultFont());
    }

    private static RectTransform CreateRectTransform(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.anchoredPosition3D = Vector3.zero;
        return rectTransform;
    }

    private static void SetStretch(RectTransform rectTransform, float left, float right, float bottom, float top)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    private static void SetAnchor(
        RectTransform rectTransform,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parentPath = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(parentPath) && !AssetDatabase.IsValidFolder(parentPath))
        {
            EnsureFolder(parentPath);
        }

        AssetDatabase.CreateFolder(parentPath, folderName);
    }
}
