using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public abstract class WindowBase : MonoBehaviour, IUiManagedElement
{
    [Header("Window Base")]
    [SerializeField] private PrototypeUiLayer windowLayer = PrototypeUiLayer.Window;
    [SerializeField] private bool buildOnAwake = true;
    [SerializeField] private bool visibleOnAwake;
    [SerializeField] private bool applyRuntimeFontToChildren;
    [SerializeField] private string windowNameOverride = string.Empty;
    [SerializeField] private string windowTitleOverride = string.Empty;
    [SerializeField] private string windowSubtitleOverride = string.Empty;
    [SerializeField] private Vector2 defaultWindowSize = new Vector2(960f, 640f);

    private PrototypeRuntimeUiManager uiManager;
    private PrototypeUiToolkit.WindowChrome windowChrome;

    protected PrototypeRuntimeUiManager UiManager => uiManager != null ? uiManager : uiManager = PrototypeRuntimeUiManager.GetOrCreate();
    protected Font RuntimeFont => UiManager.RuntimeFont;
    protected PrototypeUiToolkit.WindowChrome Chrome => windowChrome;
    protected RectTransform Root => windowChrome != null ? windowChrome.Root : null;
    protected RectTransform Panel => windowChrome != null ? windowChrome.Panel : null;
    protected RectTransform BodyRoot => windowChrome != null ? windowChrome.BodyRoot : null;
    protected RectTransform FooterRoot => windowChrome != null ? windowChrome.FooterRoot : null;
    protected TMP_Text TitleText => windowChrome != null ? windowChrome.TitleText : null;
    protected TMP_Text SubtitleText => windowChrome != null ? windowChrome.SubtitleText : null;

    public bool IsWindowBuilt => windowChrome != null && windowChrome.Root != null;
    public bool IsWindowVisible => windowChrome != null && windowChrome.Root != null && windowChrome.Root.gameObject.activeSelf;
    public virtual bool RegistersWithUiWindowService => true;
    public bool IsManagedElementVisible => IsWindowVisible;
    public PrototypeUiLayer ManagedLayer => WindowLayer;
    public virtual int ManagedInputPriority => 0;
    public string ManagedElementName => WindowName;
    public Object ManagedOwner => this;

    protected virtual PrototypeUiLayer WindowLayer => windowLayer;
    protected virtual bool BuildOnAwake => buildOnAwake;
    protected virtual bool VisibleOnAwake => visibleOnAwake;
    protected virtual bool ApplyRuntimeFontToChildren => applyRuntimeFontToChildren;
    protected virtual string WindowName => string.IsNullOrWhiteSpace(windowNameOverride) ? GetType().Name : windowNameOverride.Trim();
    protected virtual string WindowTitle => string.IsNullOrWhiteSpace(windowTitleOverride) ? WindowName : windowTitleOverride.Trim();
    protected virtual string WindowSubtitle => string.IsNullOrWhiteSpace(windowSubtitleOverride) ? string.Empty : windowSubtitleOverride.Trim();
    protected virtual Vector2 WindowSize => defaultWindowSize;

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

        EnsureWindow();
        SetWindowVisible(VisibleOnAwake);
    }

    protected virtual void OnDestroy()
    {
        if (RegistersWithUiWindowService && UiWindowService.TryGetExisting(out UiWindowService windowService))
        {
            windowService.Unregister(this);
        }

        DestroyWindowRoot();
    }

    protected void EnsureWindow()
    {
        if (windowChrome != null && windowChrome.Root != null)
        {
            return;
        }

        PrototypeUiToolkit.WindowChrome createdWindow = CreateWindowChrome();
        if (createdWindow == null || createdWindow.Root == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Failed to create window chrome.", this);
            return;
        }

        windowChrome = createdWindow;
        if (ApplyRuntimeFontToChildren)
        {
            PrototypeUiToolkit.ApplyFontRecursively(windowChrome.Root, RuntimeFont);
        }

        OnWindowRootCreated(windowChrome);
        BuildWindow(windowChrome);
    }

    public virtual void ShowWindow()
    {
        SetWindowVisible(true);
    }

    public virtual void HideWindow()
    {
        SetWindowVisible(false);
    }

    public virtual void SetWindowVisible(bool visible)
    {
        EnsureWindow();
        if (windowChrome == null || windowChrome.Root == null)
        {
            return;
        }

        PrototypeUiToolkit.SetVisible(windowChrome.Root, visible);
        OnWindowVisibilityChanged(visible);
        if (RegistersWithUiWindowService)
        {
            UiWindowService.GetOrCreate().NotifyVisibilityChanged(this, visible);
        }
    }

    public virtual void DestroyWindowRoot()
    {
        if (windowChrome != null && windowChrome.Root != null)
        {
            Destroy(windowChrome.Root.gameObject);
        }

        windowChrome = null;
        OnWindowRootDestroyed();
    }

    protected virtual PrototypeUiToolkit.WindowChrome CreateWindowChrome()
    {
        return PrototypeUiToolkit.CreateWindowChrome(
            UiManager.GetLayerRoot(WindowLayer),
            RuntimeFont,
            WindowName,
            WindowTitle,
            WindowSubtitle,
            WindowSize);
    }

    protected virtual void OnWindowRootCreated(PrototypeUiToolkit.WindowChrome chrome)
    {
    }

    protected virtual void OnWindowVisibilityChanged(bool visible)
    {
    }

    protected virtual void OnWindowRootDestroyed()
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
    protected abstract void BuildWindow(PrototypeUiToolkit.WindowChrome chrome);
}
