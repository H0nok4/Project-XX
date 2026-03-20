using UnityEngine;

public abstract class UiAnimatedWindowBase : WindowBase
{
    [Header("Animated Window")]
    [SerializeField] private bool useRootTransitionIfAvailable = true;
    [SerializeField] private bool instantFirstShow;
    [SerializeField] private bool instantFirstHide;

    private UiTransitionPlayer rootTransition;
    private bool hasShownOnce;
    private bool hasHiddenOnce;

    protected UiTransitionPlayer RootTransition => rootTransition != null ? rootTransition : rootTransition = ResolveRootTransition();

    public override void SetWindowVisible(bool visible)
    {
        EnsureWindow();
        if (Root == null)
        {
            return;
        }

        UiTransitionPlayer transition = useRootTransitionIfAvailable ? RootTransition : null;
        if (transition == null)
        {
            base.SetWindowVisible(visible);
            return;
        }

        if (visible)
        {
            bool instant = instantFirstShow && !hasShownOnce;
            hasShownOnce = true;
            transition.ShowAndActivate(instant);
        }
        else
        {
            bool instant = instantFirstHide && !hasHiddenOnce;
            hasHiddenOnce = true;
            transition.HideAndDeactivate(instant);
        }

        OnWindowVisibilityChanged(visible);
    }

    protected override void OnWindowRootCreated(PrototypeUiToolkit.WindowChrome chrome)
    {
        base.OnWindowRootCreated(chrome);
        rootTransition = ResolveRootTransition();
        if (rootTransition != null)
        {
            rootTransition.EnsureInitialized();
        }
    }

    protected override void OnWindowRootDestroyed()
    {
        rootTransition = null;
        base.OnWindowRootDestroyed();
    }

    private UiTransitionPlayer ResolveRootTransition()
    {
        return Root != null ? Root.GetComponent<UiTransitionPlayer>() : null;
    }
}
