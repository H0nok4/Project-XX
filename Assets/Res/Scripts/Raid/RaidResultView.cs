using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RaidResultView : ViewBase
{
    private const string ViewPrefabResourcePath = "UI/Raid/RaidResultView";

    private static RaidResultView instance;

    private RaidResultViewTemplate viewTemplate;
    private TMP_Text titleText;
    private TMP_Text bodyText;

    public static RaidResultView GetOrCreate()
    {
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        instance = FindFirstObjectByType<RaidResultView>();
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        GameObject root = new GameObject("RaidResultView");
        instance = root.AddComponent<RaidResultView>();
        instance.Prepare();
        return instance;
    }

    protected override PrototypeUiLayer Layer => PrototypeUiLayer.Modal;
    protected override bool AddTransparentRootGraphic => false;
    protected override bool VisibleOnAwake => false;
    protected override bool RootGraphicRaycastTarget => false;
    protected override bool ApplyRuntimeFontToChildren => true;
    protected override string ViewName => "RaidResultView";

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

        viewTemplate = instanceObject.GetComponent<RaidResultViewTemplate>();
        if (viewTemplate == null || viewTemplate.Root == null)
        {
            Destroy(instanceObject);
            viewTemplate = null;
            Debug.LogWarning($"[{GetType().Name}] Raid result prefab is missing {nameof(RaidResultViewTemplate)}.", this);
            return null;
        }

        return viewTemplate.Root;
    }

    public void SetResult(bool visible, string title, string body)
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
            viewTemplate = root.GetComponent<RaidResultViewTemplate>();
        }

        if (viewTemplate == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing {nameof(RaidResultViewTemplate)} on instantiated view root.", this);
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
