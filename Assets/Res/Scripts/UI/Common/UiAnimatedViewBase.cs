using UnityEngine;

public abstract class UiAnimatedViewBase : ViewBase
{
    [Header("Animated View")]
    [SerializeField] private bool useRootTransitionIfAvailable = true;
    [SerializeField] private bool instantFirstShow;
    [SerializeField] private bool instantFirstHide;

    private UiTransitionPlayer rootTransition;
    private bool hasShownOnce;
    private bool hasHiddenOnce;

    protected UiTransitionPlayer RootTransition => rootTransition != null ? rootTransition : rootTransition = ResolveRootTransition();

    public override void SetViewVisible(bool visible)
    {
        EnsureView();
        if (Root == null)
        {
            return;
        }

        UiTransitionPlayer transition = useRootTransitionIfAvailable ? RootTransition : null;
        if (transition == null)
        {
            base.SetViewVisible(visible);
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

        OnViewVisibilityChanged(visible);
    }

    protected override void OnViewRootCreated(RectTransform root)
    {
        base.OnViewRootCreated(root);
        rootTransition = ResolveRootTransition();
        if (rootTransition != null)
        {
            rootTransition.EnsureInitialized();
        }
    }

    protected override void OnViewRootDestroyed()
    {
        rootTransition = null;
        base.OnViewRootDestroyed();
    }

    private UiTransitionPlayer ResolveRootTransition()
    {
        return Root != null ? Root.GetComponent<UiTransitionPlayer>() : null;
    }
}
