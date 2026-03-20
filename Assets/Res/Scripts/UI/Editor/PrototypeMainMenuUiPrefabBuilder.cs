using UnityEditor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PrototypeMainMenuUiPrefabBuilder
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string UiFolder = "Assets/Resources/UI";
    private const string MainMenuFolder = "Assets/Resources/UI/MainMenu";
    private const string RootPrefabPath = "Assets/Resources/UI/PrototypeMainMenuUgui.prefab";
    private const string ButtonPrefabPath = "Assets/Resources/UI/MainMenu/PrototypeMainMenuButton.prefab";
    private const string CardPrefabPath = "Assets/Resources/UI/MainMenu/PrototypeMainMenuCard.prefab";
    private const string PanelPrefabPath = "Assets/Resources/UI/MainMenu/PrototypeMainMenuPanel.prefab";

    [MenuItem("Tools/Prototype/Build Main Menu UI Prefab")]
    public static void BuildPrefab()
    {
        string result = BuildPrefabAndReport();
        if (!string.IsNullOrWhiteSpace(result))
        {
            Debug.Log(result);
        }
    }

    public static string BuildPrefabAndReport()
    {
        EnsureFolder(ResourcesFolder);
        EnsureFolder(UiFolder);
        EnsureFolder(MainMenuFolder);

        PrototypeMainMenuButtonTemplate buttonTemplate = BuildButtonTemplatePrefab();
        PrototypeMainMenuCardTemplate cardTemplate = BuildCardTemplatePrefab();
        PrototypeMainMenuPanelTemplate panelTemplate = BuildPanelTemplatePrefab();

        GameObject root = new GameObject("PrototypeMainMenuUgui");
        try
        {
            PrototypeMainMenuUguiView view = root.AddComponent<PrototypeMainMenuUguiView>();
            view.ConfigureTemplates(cardTemplate, buttonTemplate, panelTemplate);
            view.BuildPrefabShellForEditor();
            PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
        if (rootPrefab != null)
        {
            EditorGUIUtility.PingObject(rootPrefab);
            return $"Built {RootPrefabPath}, {ButtonPrefabPath}, {CardPrefabPath}, {PanelPrefabPath}";
        }

        return $"Failed to create prefab at {RootPrefabPath}";
    }

    public static string TryBuildPrefab()
    {
        try
        {
            return BuildPrefabAndReport();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            return exception.ToString();
        }
    }

    private static PrototypeMainMenuButtonTemplate BuildButtonTemplatePrefab()
    {
        GameObject root = new GameObject("PrototypeMainMenuButton", typeof(RectTransform));
        try
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            Image background = root.AddComponent<Image>();
            background.color = new Color(0.22f, 0.28f, 0.36f, 1f);

            Button button = root.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;

            LayoutElement layoutElement = root.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 36f;
            layoutElement.flexibleWidth = 1f;

            RectTransform labelRoot = CreateRectTransform("Label", rect);
            SetStretch(labelRoot, 12f, 12f, 6f, 6f);
            TextMeshProUGUI labelText = labelRoot.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(labelText, 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

            PrototypeMainMenuButtonTemplate template = root.AddComponent<PrototypeMainMenuButtonTemplate>();
            template.ConfigureReferences(rect, background, button, labelText, layoutElement);

            PrefabUtility.SaveAsPrefabAsset(root, ButtonPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        return prefab != null ? prefab.GetComponent<PrototypeMainMenuButtonTemplate>() : null;
    }

    private static PrototypeMainMenuCardTemplate BuildCardTemplatePrefab()
    {
        GameObject root = new GameObject("PrototypeMainMenuCard", typeof(RectTransform));
        try
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            Image background = root.AddComponent<Image>();
            background.color = new Color(0.16f, 0.2f, 0.27f, 0.96f);

            VerticalLayoutGroup layoutGroup = root.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(14, 14, 14, 14);
            layoutGroup.spacing = 8f;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            LayoutElement layoutElement = root.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;

            PrototypeMainMenuCardTemplate template = root.AddComponent<PrototypeMainMenuCardTemplate>();
            template.ConfigureReferences(rect, background, layoutGroup, layoutElement);

            PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
        return prefab != null ? prefab.GetComponent<PrototypeMainMenuCardTemplate>() : null;
    }

    private static PrototypeMainMenuPanelTemplate BuildPanelTemplatePrefab()
    {
        GameObject root = new GameObject("PrototypeMainMenuPanel", typeof(RectTransform));
        try
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            Image background = root.AddComponent<Image>();
            background.color = new Color(0.12f, 0.16f, 0.22f, 0.98f);

            VerticalLayoutGroup layoutGroup = root.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(14, 14, 14, 14);
            layoutGroup.spacing = 10f;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            LayoutElement layoutElement = root.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;
            layoutElement.flexibleHeight = 1f;
            layoutElement.minWidth = 260f;

            RectTransform accentRoot = CreateRectTransform("AccentBar", rect);
            Image accentImage = accentRoot.gameObject.AddComponent<Image>();
            accentImage.color = new Color(0.95f, 0.55f, 0.18f, 1f);
            LayoutElement accentLayout = accentRoot.gameObject.AddComponent<LayoutElement>();
            accentLayout.preferredHeight = 4f;
            accentLayout.flexibleWidth = 1f;

            RectTransform titleRoot = CreateRectTransform("Title", rect);
            TextMeshProUGUI titleText = titleRoot.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(titleText, 22, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);

            RectTransform subtitleRoot = CreateRectTransform("Subtitle", rect);
            TextMeshProUGUI subtitleText = subtitleRoot.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(subtitleText, 13, FontStyle.Normal, new Color(0.75f, 0.81f, 0.88f), TextAnchor.UpperLeft);

            RectTransform scrollRoot = CreateRectTransform("ScrollView", rect);
            LayoutElement scrollLayout = scrollRoot.gameObject.AddComponent<LayoutElement>();
            scrollLayout.flexibleWidth = 1f;
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 0f;

            ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;

            RectTransform viewport = CreateRectTransform("Viewport", scrollRoot);
            SetStretch(viewport, 0f, 0f, 0f, 0f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.015f);
            Mask viewportMask = viewport.gameObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            RectTransform content = CreateRectTransform("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 12f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            RectTransform footer = CreateRectTransform("Footer", rect);
            VerticalLayoutGroup footerLayout = footer.gameObject.AddComponent<VerticalLayoutGroup>();
            footerLayout.padding = new RectOffset(0, 0, 0, 0);
            footerLayout.spacing = 8f;
            footerLayout.childAlignment = TextAnchor.UpperLeft;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = true;
            footerLayout.childForceExpandHeight = false;

            PrototypeMainMenuPanelTemplate template = root.AddComponent<PrototypeMainMenuPanelTemplate>();
            template.ConfigureReferences(rect, background, layoutElement, accentImage, titleText, subtitleText, scrollRect, content, footer);

            PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
        return prefab != null ? prefab.GetComponent<PrototypeMainMenuPanelTemplate>() : null;
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

    private static void ConfigureText(TMP_Text text, int fontSize, FontStyle fontStyle, Color color, TextAnchor anchor)
    {
        TMP_FontAsset fontAsset = ResolveFontAsset();
        if (fontAsset != null)
        {
            text.font = fontAsset;
        }

        text.fontSize = fontSize;
        text.fontStyle = PrototypeUiToolkit.ConvertFontStyle(fontStyle);
        text.color = color;
        text.alignment = PrototypeUiToolkit.ConvertTextAlignment(anchor);
        text.richText = true;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.text = string.Empty;
        text.margin = Vector4.zero;
    }

    private static TMP_FontAsset ResolveFontAsset()
    {
        TMP_Settings settings = TMP_Settings.instance;
        TMP_FontAsset defaultFontAsset = settings != null ? TMP_Settings.defaultFontAsset : null;
        return defaultFontAsset != null ? defaultFontAsset : PrototypeUiToolkit.ResolveTmpFontAsset(PrototypeUiToolkit.ResolveDefaultFont());
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
