using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractionPromptView : ViewBase
{
    private const string ViewPrefabResourcePath = "UI/Interaction/InteractionPrompt";

    private static InteractionPromptView instance;

    private InteractionPromptTemplate viewTemplate;
    private TMP_Text promptText;
    private CanvasGroup promptCanvasGroup;
    private Vector2 promptSize = new Vector2(320f, 42f);

    public static InteractionPromptView GetOrCreate()
    {
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        instance = FindFirstObjectByType<InteractionPromptView>();
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        GameObject root = new GameObject("InteractionPromptView");
        instance = root.AddComponent<InteractionPromptView>();
        instance.Prepare();
        return instance;
    }

    protected override PrototypeUiLayer Layer => PrototypeUiLayer.Overlay;
    protected override bool AddTransparentRootGraphic => false;
    protected override bool VisibleOnAwake => false;
    protected override bool RootGraphicRaycastTarget => false;
    protected override bool ApplyRuntimeFontToChildren => true;
    protected override string ViewName => "InteractionPrompt";

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

        viewTemplate = instanceObject.GetComponent<InteractionPromptTemplate>();
        if (viewTemplate == null || viewTemplate.Root == null)
        {
            Destroy(instanceObject);
            viewTemplate = null;
            Debug.LogWarning($"[{GetType().Name}] Interaction prompt prefab is missing {nameof(InteractionPromptTemplate)}.", this);
            return null;
        }

        return viewTemplate.Root;
    }

    public void SetPrompt(string prompt, Vector2 size, bool visible)
    {
        Prepare();
        promptSize = new Vector2(Mathf.Max(160f, size.x), Mathf.Max(24f, size.y));
        ApplyPromptSize();

        if (promptText != null)
        {
            promptText.text = prompt ?? string.Empty;
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
            viewTemplate = root.GetComponent<InteractionPromptTemplate>();
        }

        if (viewTemplate == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing {nameof(InteractionPromptTemplate)} on instantiated view root.", this);
            return;
        }

        promptText = viewTemplate.PromptText;
        promptCanvasGroup = viewTemplate.CanvasGroup;
        ApplyPromptSize();
        ApplyNonBlockingCanvasGroup();
    }

    protected override void OnViewVisibilityChanged(bool visible)
    {
        base.OnViewVisibilityChanged(visible);
        ApplyNonBlockingCanvasGroup();
        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = visible ? 1f : 0f;
        }
    }

    protected override void OnViewRootDestroyed()
    {
        viewTemplate = null;
        promptText = null;
        promptCanvasGroup = null;
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

    private void ApplyPromptSize()
    {
        if (viewTemplate != null && viewTemplate.Root != null)
        {
            viewTemplate.Root.sizeDelta = promptSize;
        }
    }

    private void ApplyNonBlockingCanvasGroup()
    {
        if (promptCanvasGroup == null)
        {
            return;
        }

        promptCanvasGroup.interactable = false;
        promptCanvasGroup.blocksRaycasts = false;
    }
}
