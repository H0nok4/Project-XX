using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BaseHubOverlayView : ViewBase
{
    private const string ViewPrefabResourcePath = "UI/Base/BaseHubOverlay";

    private static BaseHubOverlayView instance;

    private BaseHubOverlayTemplate viewTemplate;
    private TMP_Text titleText;
    private TMP_Text bodyText;

    public static BaseHubOverlayView GetOrCreate()
    {
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        instance = FindFirstObjectByType<BaseHubOverlayView>();
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        GameObject root = new GameObject("BaseHubOverlayView");
        instance = root.AddComponent<BaseHubOverlayView>();
        instance.Prepare();
        return instance;
    }

    protected override PrototypeUiLayer Layer => PrototypeUiLayer.Overlay;
    protected override bool AddTransparentRootGraphic => false;
    protected override bool VisibleOnAwake => false;
    protected override bool RootGraphicRaycastTarget => false;
    protected override bool ApplyRuntimeFontToChildren => true;
    protected override string ViewName => "BaseHubOverlay";

    protected override RectTransform CreateViewRoot()
    {
        RectTransform parent = UiManager.GetLayerRoot(Layer);
        GameObject prefabAsset = Resources.Load<GameObject>(ViewPrefabResourcePath);
        if (prefabAsset == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing view prefab at Resources/{ViewPrefabResourcePath}.", this);
            return null;
        }

        GameObject instanceObject = Instantiate(prefabAsset, parent, false);
        instanceObject.name = prefabAsset.name;

        viewTemplate = instanceObject.GetComponent<BaseHubOverlayTemplate>();
        if (viewTemplate == null || viewTemplate.Root == null)
        {
            Destroy(instanceObject);
            viewTemplate = null;
            Debug.LogWarning($"[{GetType().Name}] Base hub overlay prefab is missing {nameof(BaseHubOverlayTemplate)}.", this);
            return null;
        }

        return viewTemplate.Root;
    }

    public void SetContent(string title, string body, Vector2 size, bool visible)
    {
        Prepare();
        if (titleText != null)
        {
            titleText.text = title ?? string.Empty;
        }

        if (bodyText != null)
        {
            bodyText.text = body ?? string.Empty;
        }

        if (viewTemplate != null && viewTemplate.Root != null)
        {
            viewTemplate.Root.sizeDelta = size;
        }

        SetViewVisible(visible);
    }

    protected override void BuildView(RectTransform root)
    {
        if (root == null)
        {
            return;
        }

        if (viewTemplate == null || viewTemplate.Root != root)
        {
            viewTemplate = root.GetComponent<BaseHubOverlayTemplate>();
        }

        if (viewTemplate == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing {nameof(BaseHubOverlayTemplate)} on instantiated view root.", this);
            return;
        }

        titleText = viewTemplate.TitleText;
        bodyText = viewTemplate.BodyText;
        ApplyNonBlockingCanvasGroup();
    }

    protected override void OnViewVisibilityChanged(bool visible)
    {
        base.OnViewVisibilityChanged(visible);
        ApplyNonBlockingCanvasGroup();
    }

    protected override void OnViewRootDestroyed()
    {
        viewTemplate = null;
        titleText = null;
        bodyText = null;
        base.OnViewRootDestroyed();
    }

    protected override void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        base.OnDestroy();
    }

    private void Prepare()
    {
        EnsureView();
    }

    private void ApplyNonBlockingCanvasGroup()
    {
        if (RootCanvasGroup == null)
        {
            return;
        }

        RootCanvasGroup.interactable = false;
        RootCanvasGroup.blocksRaycasts = false;
    }
}
