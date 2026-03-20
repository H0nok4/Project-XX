using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PrototypeQuestUiPrefabBuilder
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string UiFolder = "Assets/Resources/UI";
    private const string QuestFolder = "Assets/Resources/UI/Quest";
    private const string TrackerPrefabPath = "Assets/Resources/UI/Quest/QuestTrackerHud.prefab";
    private const string ToastPrefabPath = "Assets/Resources/UI/Quest/QuestToast.prefab";
    private const string JournalPrefabPath = "Assets/Resources/UI/Quest/QuestJournal.prefab";
    private const string DialoguePrefabPath = "Assets/Resources/UI/Quest/DialogueWindow.prefab";
    private const string DialogueOptionButtonPrefabPath = "Assets/Resources/UI/Quest/DialogueOptionButton.prefab";

    [MenuItem("Tools/Prototype/Build Quest UI Prefabs")]
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
        EnsureFolder(QuestFolder);

        BuildTrackerPrefab();
        BuildToastPrefab();
        BuildJournalPrefab();
        BuildDialogueOptionButtonPrefab();
        BuildDialoguePrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return $"Built {TrackerPrefabPath}, {ToastPrefabPath}, {JournalPrefabPath}, {DialogueOptionButtonPrefabPath}, {DialoguePrefabPath}";
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

    private static void BuildTrackerPrefab()
    {
        GameObject root = new GameObject("QuestTrackerHud", typeof(RectTransform));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetAnchor(
                rootRect,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-18f, -18f),
                new Vector2(420f, 180f));

            Image background = root.AddComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.14f, 0.88f);

            VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TMP_Text trackerText = CreateText(
                rootRect,
                "TrackerText",
                "\u4efb\u52a1\u8ffd\u8e2a",
                14,
                FontStyle.Normal,
                Color.white,
                TextAnchor.UpperLeft);
            LayoutElement textLayout = trackerText.gameObject.AddComponent<LayoutElement>();
            textLayout.flexibleHeight = 1f;
            textLayout.minHeight = 0f;

            Button journalButton = CreateButton(
                rootRect,
                "JournalButton",
                "\u4efb\u52a1\u65e5\u5fd7",
                34f,
                new Color(0.21f, 0.34f, 0.48f, 0.98f),
                new Color(0.29f, 0.46f, 0.64f, 1f),
                new Color(0.16f, 0.26f, 0.38f, 1f));

            QuestTrackerViewTemplate template = root.AddComponent<QuestTrackerViewTemplate>();
            template.ConfigureReferences(rootRect, background, layout, trackerText, journalButton);
            PrefabUtility.SaveAsPrefabAsset(root, TrackerPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildToastPrefab()
    {
        GameObject root = new GameObject("QuestToast", typeof(RectTransform));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetAnchor(
                rootRect,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -28f),
                new Vector2(520f, 52f));

            Image background = root.AddComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.14f, 0.94f);

            VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            TMP_Text messageText = CreateText(
                rootRect,
                "MessageText",
                string.Empty,
                15,
                FontStyle.Bold,
                Color.white,
                TextAnchor.MiddleCenter);

            QuestToastViewTemplate template = root.AddComponent<QuestToastViewTemplate>();
            template.ConfigureReferences(rootRect, background, layout, canvasGroup, messageText);
            PrefabUtility.SaveAsPrefabAsset(root, ToastPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildJournalPrefab()
    {
        GameObject root = new GameObject("QuestJournal", typeof(RectTransform));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetStretch(rootRect, 0f, 0f, 0f, 0f);

            Image overlay = root.AddComponent<Image>();
            overlay.color = new Color(0.02f, 0.03f, 0.05f, 0.58f);
            overlay.raycastTarget = true;

            RectTransform panel = CreatePanel(
                rootRect,
                "Panel",
                new Color(0.08f, 0.1f, 0.14f, 0.97f),
                new RectOffset(18, 18, 18, 18),
                12f);
            SetAnchor(
                panel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(980f, 680f));

            TMP_Text titleText = CreateText(
                panel,
                "Title",
                "\u4efb\u52a1\u65e5\u5fd7",
                24,
                FontStyle.Bold,
                Color.white,
                TextAnchor.UpperLeft);
            LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;

            TMP_Text subtitleText = CreateText(
                panel,
                "Subtitle",
                "\u67e5\u770b\u53ef\u63a5\u4efb\u52a1\u3001\u8fdb\u884c\u4e2d\u4efb\u52a1\u4e0e\u5956\u52b1\u3002",
                14,
                FontStyle.Normal,
                new Color(0.83f, 0.88f, 0.93f, 1f),
                TextAnchor.UpperLeft);
            LayoutElement subtitleLayout = subtitleText.gameObject.AddComponent<LayoutElement>();
            subtitleLayout.flexibleWidth = 1f;

            RectTransform bodyRoot = CreateRectTransform("Body", panel);
            HorizontalLayoutGroup bodyLayout = bodyRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 16f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;

            LayoutElement bodyElement = bodyRoot.gameObject.AddComponent<LayoutElement>();
            bodyElement.flexibleWidth = 1f;
            bodyElement.flexibleHeight = 1f;
            bodyElement.minHeight = 0f;

            RectTransform footerRoot = CreateRectTransform("Footer", panel);
            HorizontalLayoutGroup footerLayout = footerRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 10f;
            footerLayout.padding = new RectOffset(0, 0, 4, 0);
            footerLayout.childAlignment = TextAnchor.MiddleRight;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = false;
            footerLayout.childForceExpandHeight = false;

            LayoutElement footerElement = footerRoot.gameObject.AddComponent<LayoutElement>();
            footerElement.flexibleWidth = 1f;
            footerElement.preferredHeight = 44f;

            RectTransform listPanel = CreatePanel(
                bodyRoot,
                "QuestListPanel",
                new Color(0.12f, 0.15f, 0.2f, 0.92f),
                new RectOffset(12, 12, 12, 12),
                10f);
            LayoutElement listLayout = listPanel.gameObject.AddComponent<LayoutElement>();
            listLayout.preferredWidth = 300f;
            listLayout.flexibleHeight = 1f;
            CreateText(
                listPanel,
                "ListTitle",
                "\u4efb\u52a1\u5217\u8868",
                18,
                FontStyle.Bold,
                Color.white,
                TextAnchor.UpperLeft);
            ScrollRect listScrollRect = CreateScrollView(listPanel, "ListScrollView", out RectTransform listContentRoot);

            RectTransform detailPanel = CreatePanel(
                bodyRoot,
                "QuestDetailPanel",
                new Color(0.11f, 0.14f, 0.18f, 0.92f),
                new RectOffset(14, 14, 14, 14),
                10f);
            LayoutElement detailLayout = detailPanel.gameObject.AddComponent<LayoutElement>();
            detailLayout.flexibleWidth = 1f;
            detailLayout.flexibleHeight = 1f;
            CreateText(
                detailPanel,
                "DetailTitle",
                "\u4efb\u52a1\u8be6\u60c5",
                18,
                FontStyle.Bold,
                Color.white,
                TextAnchor.UpperLeft);
            ScrollRect detailScrollRect = CreateScrollView(detailPanel, "DetailScrollView", out RectTransform detailContentRoot);

            Button closeButton = CreateButton(
                footerRoot,
                "CloseButton",
                "\u5173\u95ed",
                38f,
                new Color(0.2f, 0.27f, 0.36f, 0.98f),
                new Color(0.29f, 0.38f, 0.49f, 1f),
                new Color(0.16f, 0.22f, 0.3f, 1f));

            QuestJournalViewTemplate template = root.AddComponent<QuestJournalViewTemplate>();
            template.ConfigureReferences(
                rootRect,
                panel,
                titleText,
                subtitleText,
                bodyRoot,
                footerRoot,
                listScrollRect,
                listContentRoot,
                detailScrollRect,
                detailContentRoot,
                closeButton);

            PrefabUtility.SaveAsPrefabAsset(root, JournalPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildDialoguePrefab()
    {
        GameObject root = new GameObject("DialogueWindow", typeof(RectTransform));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetStretch(rootRect, 0f, 0f, 0f, 0f);

            Image overlay = root.AddComponent<Image>();
            overlay.color = new Color(0.02f, 0.03f, 0.05f, 0.58f);
            overlay.raycastTarget = true;

            RectTransform panel = CreatePanel(
                rootRect,
                "Panel",
                new Color(0.08f, 0.1f, 0.14f, 0.97f),
                new RectOffset(18, 18, 18, 18),
                10f);
            SetAnchor(
                panel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(760f, 560f));

            TMP_Text titleText = CreateText(
                panel,
                "Title",
                "\u5bf9\u8bdd",
                24,
                FontStyle.Bold,
                Color.white,
                TextAnchor.UpperLeft);
            LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;

            TMP_Text subtitleText = CreateText(
                panel,
                "Subtitle",
                string.Empty,
                14,
                FontStyle.Normal,
                new Color(0.83f, 0.88f, 0.93f, 1f),
                TextAnchor.UpperLeft);
            LayoutElement subtitleLayout = subtitleText.gameObject.AddComponent<LayoutElement>();
            subtitleLayout.flexibleWidth = 1f;
            subtitleText.gameObject.SetActive(false);

            RectTransform bodyRoot = CreateRectTransform("Body", panel);
            VerticalLayoutGroup bodyLayout = bodyRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyLayout.spacing = 10f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            LayoutElement bodyElement = bodyRoot.gameObject.AddComponent<LayoutElement>();
            bodyElement.flexibleWidth = 1f;
            bodyElement.flexibleHeight = 1f;
            bodyElement.minHeight = 0f;

            TMP_Text speakerText = CreateText(
                bodyRoot,
                "SpeakerText",
                string.Empty,
                18,
                FontStyle.Bold,
                new Color(0.96f, 0.8f, 0.44f, 1f),
                TextAnchor.UpperLeft);

            TMP_Text dialogueText = CreateText(
                bodyRoot,
                "DialogueText",
                string.Empty,
                16,
                FontStyle.Normal,
                Color.white,
                TextAnchor.UpperLeft);

            RectTransform optionsRoot = CreateRectTransform("Options", bodyRoot);
            VerticalLayoutGroup optionsLayout = optionsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            optionsLayout.spacing = 8f;
            optionsLayout.childAlignment = TextAnchor.UpperLeft;
            optionsLayout.childControlWidth = true;
            optionsLayout.childControlHeight = true;
            optionsLayout.childForceExpandWidth = true;
            optionsLayout.childForceExpandHeight = false;

            TMP_Text emptyOptionsText = CreateText(
                bodyRoot,
                "EmptyOptionsText",
                "没有可执行的对话选项。",
                14,
                FontStyle.Normal,
                new Color(0.82f, 0.87f, 0.92f, 1f),
                TextAnchor.UpperLeft);
            emptyOptionsText.gameObject.SetActive(false);

            RectTransform footerRoot = CreateRectTransform("Footer", panel);
            HorizontalLayoutGroup footerLayout = footerRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 10f;
            footerLayout.padding = new RectOffset(0, 0, 4, 0);
            footerLayout.childAlignment = TextAnchor.MiddleRight;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = false;
            footerLayout.childForceExpandHeight = false;

            LayoutElement footerElement = footerRoot.gameObject.AddComponent<LayoutElement>();
            footerElement.flexibleWidth = 1f;
            footerElement.preferredHeight = 44f;

            Button closeButton = CreateButton(
                footerRoot,
                "CloseButton",
                "\u7ed3\u675f\u5bf9\u8bdd",
                38f,
                new Color(0.22f, 0.27f, 0.34f, 0.98f),
                new Color(0.31f, 0.38f, 0.48f, 1f),
                new Color(0.17f, 0.21f, 0.29f, 1f));

            DialogueWindowViewTemplate template = root.AddComponent<DialogueWindowViewTemplate>();
            template.ConfigureReferences(
                rootRect,
                panel,
                titleText,
                subtitleText,
                bodyRoot,
                footerRoot,
                speakerText,
                dialogueText,
                optionsRoot,
                emptyOptionsText,
                closeButton);

            PrefabUtility.SaveAsPrefabAsset(root, DialoguePrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildDialogueOptionButtonPrefab()
    {
        GameObject root = new GameObject("DialogueOptionButton", typeof(RectTransform));
        try
        {
            RectTransform rectTransform = root.GetComponent<RectTransform>();
            Image background = root.AddComponent<Image>();
            Color normalColor = new Color(0.18f, 0.24f, 0.33f, 0.98f);
            Color highlightedColor = new Color(0.26f, 0.35f, 0.47f, 1f);
            Color pressedColor = new Color(0.14f, 0.19f, 0.27f, 1f);
            background.color = normalColor;

            Button button = root.AddComponent<Button>();
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

            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredHeight = 42f;
            layout.flexibleWidth = 1f;

            RectTransform labelRoot = CreateRectTransform("Label", rectTransform);
            SetStretch(labelRoot, 12f, 12f, 8f, 8f);
            TMP_Text label = CreateText(labelRoot, "Text", "对话选项", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

            DialogueOptionButtonTemplate template = root.AddComponent<DialogueOptionButtonTemplate>();
            template.ConfigureReferences(rectTransform, button, label);

            PrefabUtility.SaveAsPrefabAsset(root, DialogueOptionButtonPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static RectTransform CreatePanel(
        Transform parent,
        string name,
        Color backgroundColor,
        RectOffset padding,
        float spacing)
    {
        RectTransform root = CreateRectTransform(name, parent);
        Image background = root.gameObject.AddComponent<Image>();
        background.color = backgroundColor;

        VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = padding ?? new RectOffset(0, 0, 0, 0);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return root;
    }

    private static ScrollRect CreateScrollView(Transform parent, string name, out RectTransform content)
    {
        RectTransform scrollRoot = CreateRectTransform(name, parent);
        LayoutElement rootLayout = scrollRoot.gameObject.AddComponent<LayoutElement>();
        rootLayout.flexibleWidth = 1f;
        rootLayout.flexibleHeight = 1f;
        rootLayout.minHeight = 0f;

        Image background = scrollRoot.gameObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.08f);
        background.raycastTarget = true;

        ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 32f;

        RectTransform viewport = CreateRectTransform("Viewport", scrollRoot);
        SetStretch(viewport, 0f, 0f, 0f, 0f);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask viewportMask = viewport.gameObject.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        content = CreateRectTransform("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 8f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        return scrollRect;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        float preferredHeight,
        Color normalColor,
        Color highlightedColor,
        Color pressedColor)
    {
        RectTransform rectTransform = CreateRectTransform(name, parent);
        Image background = rectTransform.gameObject.AddComponent<Image>();
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
        CreateText(labelRoot, "Text", label, 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
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
