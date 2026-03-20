using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public abstract class UiWidgetBase : MonoBehaviour
{
    [Header("UI Widget")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private bool refreshOnEnable;
    [SerializeField] private bool rebuildLayoutOnEnable;
    [SerializeField] private bool autoCreateCanvasGroup;
    [SerializeField] private bool useUnscaledTime = true;

    private bool isInitialized;
    private bool hasBeenShown;
    private bool isInteractable = true;

    public RectTransform RectTransform => rectTransform != null ? rectTransform : rectTransform = transform as RectTransform;
    public CanvasGroup CanvasGroup => ResolveCanvasGroup(false);
    public bool IsInitialized => isInitialized;
    public bool HasBeenShown => hasBeenShown;
    public bool IsWidgetVisible => gameObject.activeInHierarchy;
    public bool IsWidgetInteractable => isInteractable;

    protected float DeltaTime => UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    protected float TimeNow => UseUnscaledTime ? Time.unscaledTime : Time.time;

    protected virtual bool InitializeOnAwake => initializeOnAwake;
    protected virtual bool RefreshOnEnable => refreshOnEnable;
    protected virtual bool RebuildLayoutOnEnable => rebuildLayoutOnEnable;
    protected virtual bool AutoCreateCanvasGroup => autoCreateCanvasGroup;
    protected virtual bool UseUnscaledTime => useUnscaledTime;

    protected virtual void Reset()
    {
        CacheReferences();
    }

    protected virtual void OnValidate()
    {
        CacheReferences();
    }

    protected virtual void Awake()
    {
        CacheReferences();
        if (InitializeOnAwake)
        {
            EnsureInitialized();
        }
    }

    protected virtual void OnEnable()
    {
        EnsureInitialized();
        if (RefreshOnEnable)
        {
            RefreshWidget();
        }

        if (RebuildLayoutOnEnable)
        {
            RebuildLayout();
        }

        hasBeenShown = true;
        OnWidgetShown();
    }

    protected virtual void OnDisable()
    {
        if (hasBeenShown)
        {
            OnWidgetHidden();
        }
    }

    protected virtual void OnDestroy()
    {
        if (!isInitialized)
        {
            return;
        }

        OnWidgetDestroyed();
        isInitialized = false;
    }

    public void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        CacheReferences();
        if (AutoCreateCanvasGroup)
        {
            ResolveCanvasGroup(true);
        }

        OnInitialize();
        isInitialized = true;
    }

    public virtual void RefreshWidget()
    {
        EnsureInitialized();
        OnRefresh();
    }

    public virtual void Show()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        else
        {
            RefreshWidget();
        }
    }

    public virtual void Hide()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    public virtual void SetWidgetVisible(bool visible)
    {
        if (visible)
        {
            Show();
            return;
        }

        Hide();
    }

    public virtual void SetWidgetInteractable(bool interactable, bool updateCanvasGroup = true)
    {
        isInteractable = interactable;
        CanvasGroup group = ResolveCanvasGroup(updateCanvasGroup && AutoCreateCanvasGroup);
        if (updateCanvasGroup && group != null)
        {
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }

        OnInteractableChanged(interactable);
    }

    public void RebuildLayout()
    {
        if (RectTransform != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
        }
    }

    protected virtual void CacheReferences()
    {
        rectTransform ??= transform as RectTransform;
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    protected CanvasGroup ResolveCanvasGroup(bool createIfMissing)
    {
        CacheReferences();
        if (canvasGroup == null && createIfMissing && RectTransform != null)
        {
            canvasGroup = PrototypeUiToolkit.EnsureCanvasGroup(RectTransform);
        }

        return canvasGroup;
    }

    protected virtual void OnInitialize()
    {
    }

    protected virtual void OnRefresh()
    {
    }

    protected virtual void OnWidgetShown()
    {
    }

    protected virtual void OnWidgetHidden()
    {
    }

    protected virtual void OnWidgetDestroyed()
    {
    }

    protected virtual void OnInteractableChanged(bool interactable)
    {
    }
}
