using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public abstract class ViewBase : MonoBehaviour, IUiManagedElement
{
    [Header("View Base")]
    [SerializeField] private PrototypeUiLayer viewLayer = PrototypeUiLayer.Hud;
    [SerializeField] private bool stretchToLayer = true;
    [SerializeField] private bool buildOnAwake = true;
    [SerializeField] private bool visibleOnAwake = true;
    [SerializeField] private bool addTransparentRootGraphic = true;
    [SerializeField] private bool rootGraphicRaycastTarget;
    [SerializeField] private bool applyRuntimeFontToChildren;
    [SerializeField] private string viewNameOverride = string.Empty;

    private PrototypeRuntimeUiManager uiManager;
    private RectTransform viewRoot;
    private CanvasGroup viewCanvasGroup;
    private Image rootGraphic;

    protected PrototypeRuntimeUiManager UiManager => uiManager != null ? uiManager : uiManager = PrototypeRuntimeUiManager.GetOrCreate();
    protected Font RuntimeFont => UiManager.RuntimeFont;
    protected RectTransform Root => viewRoot;
    protected CanvasGroup RootCanvasGroup => viewCanvasGroup;
    protected Image RootGraphic => rootGraphic;

    public bool IsViewBuilt => viewRoot != null;
    public bool IsViewVisible => viewRoot != null && viewRoot.gameObject.activeSelf;
    public virtual bool RegistersWithUiWindowService => false;
    public bool IsManagedElementVisible => IsViewVisible;
    public PrototypeUiLayer ManagedLayer => Layer;
    public virtual int ManagedInputPriority => 0;
    public string ManagedElementName => ViewName;
    public Object ManagedOwner => this;

    protected virtual PrototypeUiLayer Layer => viewLayer;
    protected virtual bool StretchToLayer => stretchToLayer;
    protected virtual bool BuildOnAwake => buildOnAwake;
    protected virtual bool VisibleOnAwake => visibleOnAwake;
    protected virtual bool AddTransparentRootGraphic => addTransparentRootGraphic;
    protected virtual bool RootGraphicRaycastTarget => rootGraphicRaycastTarget;
    protected virtual bool ApplyRuntimeFontToChildren => applyRuntimeFontToChildren;
    protected virtual Color RootGraphicColor => new Color(0f, 0f, 0f, 0f);
    protected virtual string ViewName => string.IsNullOrWhiteSpace(viewNameOverride) ? GetType().Name : viewNameOverride.Trim();

    protected virtual void Awake()
    {
        if (RegistersWithUiWindowService)
        {
            UiWindowService.GetOrCreate().Register(this);
            UiInputService.GetOrCreate();
        }

        if (!BuildOnAwake)
        {
            return;
        }

        EnsureView();
        SetViewVisible(VisibleOnAwake);
    }

    protected virtual void OnDestroy()
    {
        if (RegistersWithUiWindowService && UiWindowService.TryGetExisting(out UiWindowService windowService))
        {
            windowService.Unregister(this);
        }

        DestroyViewRoot();
    }

    protected void EnsureView()
    {
        if (viewRoot != null)
        {
            return;
        }

        RectTransform createdRoot = CreateViewRoot();
        if (createdRoot == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Failed to create view root.", this);
            return;
        }

        viewRoot = createdRoot;
        viewCanvasGroup = PrototypeUiToolkit.EnsureCanvasGroup(viewRoot);
        ConfigureRootGraphic();
        OnViewRootCreated(viewRoot);
        BuildView(viewRoot);
        if (ApplyRuntimeFontToChildren)
        {
            PrototypeUiToolkit.ApplyFontRecursively(viewRoot, RuntimeFont);
        }
    }

    public virtual void ShowView()
    {
        SetViewVisible(true);
    }

    public virtual void HideView()
    {
        SetViewVisible(false);
    }

    public virtual void SetViewVisible(bool visible)
    {
        EnsureView();
        if (viewRoot == null)
        {
            return;
        }

        PrototypeUiToolkit.SetVisible(viewRoot, visible);
        if (viewCanvasGroup != null)
        {
            viewCanvasGroup.alpha = visible ? 1f : 0f;
            viewCanvasGroup.interactable = visible;
            viewCanvasGroup.blocksRaycasts = visible;
        }

        OnViewVisibilityChanged(visible);
        if (RegistersWithUiWindowService)
        {
            UiWindowService.GetOrCreate().NotifyVisibilityChanged(this, visible);
        }
    }

    public virtual void DestroyViewRoot()
    {
        if (viewRoot != null)
        {
            if (uiManager != null)
            {
                uiManager.DestroyViewRoot(ref viewRoot);
            }
            else
            {
                Destroy(viewRoot.gameObject);
                viewRoot = null;
            }
        }

        viewCanvasGroup = null;
        rootGraphic = null;
        OnViewRootDestroyed();
    }

    protected virtual RectTransform CreateViewRoot()
    {
        return UiManager.CreateViewRoot(ViewName, Layer, StretchToLayer);
    }

    protected virtual void OnViewRootCreated(RectTransform root)
    {
    }

    protected virtual void OnViewVisibilityChanged(bool visible)
    {
    }

    protected virtual void OnViewRootDestroyed()
    {
    }

    public virtual bool TryHandleUiSubmit()
    {
        return false;
    }

    public virtual bool TryHandleUiCancel()
    {
        return false;
    }

    // New runtime UI should bind or initialize an authored UGUI prefab here,
    // rather than constructing a new gameplay hierarchy in code.
    protected abstract void BuildView(RectTransform root);

    private void ConfigureRootGraphic()
    {
        if (viewRoot == null)
        {
            return;
        }

        rootGraphic = viewRoot.GetComponent<Image>();
        if (AddTransparentRootGraphic && rootGraphic == null)
        {
            rootGraphic = viewRoot.gameObject.AddComponent<Image>();
        }

        if (rootGraphic == null)
        {
            return;
        }

        rootGraphic.color = RootGraphicColor;
        rootGraphic.raycastTarget = RootGraphicRaycastTarget;
    }
}
