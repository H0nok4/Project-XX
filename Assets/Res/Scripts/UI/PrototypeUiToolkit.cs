using System;
using UnityEngine;
using UnityEngine.UI;

public static class PrototypeUiToolkit
{
    public sealed class WindowChrome
    {
        public RectTransform Root { get; internal set; }
        public RectTransform Panel { get; internal set; }
        public Text TitleText { get; internal set; }
        public Text SubtitleText { get; internal set; }
        public RectTransform BodyRoot { get; internal set; }
        public RectTransform FooterRoot { get; internal set; }
    }

    public static Font ResolveDefaultFont()
    {
        Font dynamicFont = Font.CreateDynamicFontFromOSFont(
            new[]
            {
                "Microsoft YaHei UI",
                "Microsoft YaHei",
                "Microsoft JhengHei UI",
                "SimHei",
                "Arial Unicode MS",
                "Arial"
            },
            16);
        if (dynamicFont != null)
        {
            return dynamicFont;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    public static RectTransform CreateRectTransform(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            gameObject.layer = parent.gameObject.layer;
        }

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.anchoredPosition3D = Vector3.zero;
        return rectTransform;
    }

    public static void SetStretch(RectTransform rectTransform, float left, float right, float bottom, float top)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    public static void SetAnchor(
        RectTransform rectTransform,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }

    public static Image CreateImage(Transform parent, string name, Color color, bool raycastTarget = false)
    {
        RectTransform rectTransform = CreateRectTransform(name, parent);
        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    public static Text CreateText(
        Transform parent,
        Font font,
        string text,
        int fontSize,
        FontStyle fontStyle,
        Color color,
        TextAnchor alignment,
        bool raycastTarget = false,
        bool richText = true)
    {
        RectTransform rectTransform = CreateRectTransform("Text", parent);
        Text label = rectTransform.gameObject.AddComponent<Text>();
        label.font = font != null ? font : ResolveDefaultFont();
        label.text = text ?? string.Empty;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = raycastTarget;
        label.supportRichText = richText;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    public static Button CreateButton(
        Transform parent,
        Font font,
        string label,
        Action onClick,
        Color normalColor,
        Color highlightedColor,
        Color pressedColor,
        float preferredHeight)
    {
        RectTransform rectTransform = CreateRectTransform("Button", parent);
        Image background = rectTransform.gameObject.AddComponent<Image>();
        background.color = normalColor;

        Button button = rectTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        ApplyButtonColors(button, normalColor, highlightedColor, pressedColor);

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.flexibleWidth = 1f;

        RectTransform labelRoot = CreateRectTransform("Label", rectTransform);
        SetStretch(labelRoot, 12f, 12f, 8f, 8f);
        Text textComponent = labelRoot.gameObject.AddComponent<Text>();
        textComponent.font = font != null ? font : ResolveDefaultFont();
        textComponent.text = label ?? string.Empty;
        textComponent.fontSize = 15;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.raycastTarget = false;
        textComponent.supportRichText = false;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;

        button.onClick.RemoveAllListeners();
        if (onClick != null)
        {
            button.onClick.AddListener(() => onClick());
        }

        return button;
    }

    public static void ApplyButtonColors(Button button, Color normalColor, Color highlightedColor, Color pressedColor)
    {
        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.selectedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        colors.disabledColor = new Color(normalColor.r * 0.65f, normalColor.g * 0.65f, normalColor.b * 0.65f, 0.7f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    public static RectTransform CreatePanel(
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

    public static WindowChrome CreateWindowChrome(
        Transform parent,
        Font font,
        string name,
        string title,
        string subtitle,
        Vector2 panelSize)
    {
        RectTransform root = CreateRectTransform(string.IsNullOrWhiteSpace(name) ? "Window" : name.Trim(), parent);
        SetStretch(root, 0f, 0f, 0f, 0f);
        Image overlay = root.gameObject.AddComponent<Image>();
        overlay.color = new Color(0.02f, 0.03f, 0.05f, 0.58f);
        overlay.raycastTarget = true;

        RectTransform panel = CreatePanel(
            root,
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
            panelSize);

        Text titleText = CreateText(panel, font, title, 24, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;

        Text subtitleText = CreateText(
            panel,
            font,
            subtitle,
            14,
            FontStyle.Normal,
            new Color(0.83f, 0.88f, 0.93f, 1f),
            TextAnchor.UpperLeft);
        LayoutElement subtitleLayout = subtitleText.gameObject.AddComponent<LayoutElement>();
        subtitleLayout.flexibleWidth = 1f;
        subtitleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(subtitle));

        RectTransform bodyRoot = CreateRectTransform("Body", panel);
        LayoutElement bodyLayout = bodyRoot.gameObject.AddComponent<LayoutElement>();
        bodyLayout.flexibleWidth = 1f;
        bodyLayout.flexibleHeight = 1f;
        bodyLayout.minHeight = 0f;

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

        return new WindowChrome
        {
            Root = root,
            Panel = panel,
            TitleText = titleText,
            SubtitleText = subtitleText,
            BodyRoot = bodyRoot,
            FooterRoot = footerRoot
        };
    }

    public static ScrollRect CreateScrollView(
        Transform parent,
        out RectTransform viewport,
        out RectTransform content,
        bool flexibleHeight)
    {
        RectTransform scrollRoot = CreateRectTransform("ScrollView", parent);
        LayoutElement rootLayout = scrollRoot.gameObject.AddComponent<LayoutElement>();
        rootLayout.flexibleWidth = 1f;
        rootLayout.flexibleHeight = flexibleHeight ? 1f : 0f;
        if (flexibleHeight)
        {
            rootLayout.minHeight = 0f;
        }

        Image background = scrollRoot.gameObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.08f);
        background.raycastTarget = true;

        ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 32f;

        viewport = CreateRectTransform("Viewport", scrollRoot);
        SetStretch(viewport, 0f, 0f, 0f, 0f);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        content = CreateRectTransform("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 10f;
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

    public static CanvasGroup EnsureCanvasGroup(RectTransform root)
    {
        if (root == null)
        {
            return null;
        }

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    public static void ApplyFontRecursively(Transform root, Font font)
    {
        if (root == null || font == null)
        {
            return;
        }

        Text[] textComponents = root.GetComponentsInChildren<Text>(true);
        for (int index = 0; index < textComponents.Length; index++)
        {
            if (textComponents[index] != null)
            {
                textComponents[index].font = font;
            }
        }
    }

    public static void SetVisible(RectTransform root, bool visible)
    {
        if (root == null)
        {
            return;
        }

        if (root.gameObject.activeSelf != visible)
        {
            root.gameObject.SetActive(visible);
        }
    }

    public static void ClearChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        for (int index = root.childCount - 1; index >= 0; index--)
        {
            UnityEngine.Object.Destroy(root.GetChild(index).gameObject);
        }
    }

    public static void SetScreenPosition(RectTransform canvasRoot, RectTransform target, Vector2 screenPoint)
    {
        if (canvasRoot == null || target == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screenPoint, null, out Vector2 localPoint))
        {
            target.anchoredPosition = localPoint;
        }
    }
}
