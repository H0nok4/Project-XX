using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UiAnimatedButton : UiWidgetBase,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler
{
    private enum VisualState
    {
        Normal = 0,
        Highlighted = 1,
        Selected = 2,
        Pressed = 3,
        Disabled = 4
    }

    [Serializable]
    private struct VisualStyle
    {
        public Vector2 anchoredOffset;
        public Vector3 scale;
        public Color color;
        [Range(0f, 1f)] public float alpha;

        public static VisualStyle Create(Vector2 offset, Vector3 targetScale, Color targetColor, float targetAlpha)
        {
            return new VisualStyle
            {
                anchoredOffset = offset,
                scale = targetScale,
                color = targetColor,
                alpha = targetAlpha
            };
        }
    }

    [Serializable]
    private struct VisualSnapshot
    {
        public Vector2 anchoredOffset;
        public Vector3 scale;
        public Color color;
        public float alpha;

        public static VisualSnapshot Lerp(VisualSnapshot from, VisualSnapshot to, float t)
        {
            return new VisualSnapshot
            {
                anchoredOffset = Vector2.LerpUnclamped(from.anchoredOffset, to.anchoredOffset, t),
                scale = Vector3.LerpUnclamped(from.scale, to.scale, t),
                color = Color.LerpUnclamped(from.color, to.color, t),
                alpha = Mathf.LerpUnclamped(from.alpha, to.alpha, t)
            };
        }
    }

    [Header("Animated Button")]
    [SerializeField] private Button button;
    [SerializeField] private RectTransform motionTarget;
    [SerializeField] private Graphic tintGraphic;
    [SerializeField] private CanvasGroup fadeTarget;
    [SerializeField] private bool disableSelectableTransition = true;
    [SerializeField] private bool trackSelectionAsHover = true;
    [SerializeField] private bool useSelectedStyle;
    [SerializeField] private float transitionDuration = 0.12f;
    [SerializeField] private AnimationCurve transitionCurve = null;
    [SerializeField] private float clickPunchScale = 0.08f;
    [SerializeField] private float clickPunchDuration = 0.16f;
    [SerializeField] private AnimationCurve clickPunchCurve = null;
    [SerializeField] private VisualStyle normalStyle = default;
    [SerializeField] private VisualStyle highlightedStyle = default;
    [SerializeField] private VisualStyle selectedStyle = default;
    [SerializeField] private VisualStyle pressedStyle = default;
    [SerializeField] private VisualStyle disabledStyle = default;

    private Vector2 baseAnchoredPosition;
    private Vector3 baseScale;
    private Color baseColor;
    private float baseAlpha = 1f;
    private VisualSnapshot currentSnapshot;
    private VisualSnapshot fromSnapshot;
    private VisualSnapshot toSnapshot;
    private VisualState currentVisualState;
    private VisualState targetVisualState;
    private float transitionStartTime;
    private bool transitionRunning;
    private bool pointerInside;
    private bool pointerPressed;
    private bool selectedFromEventSystem;
    private bool hasManualSelectionState;
    private bool manualSelectionState;
    private float clickPunchStartTime = -1f;

    protected override void Reset()
    {
        base.Reset();
        CacheReferences();
        ApplyDefaultStyles();
    }

    protected override void CacheReferences()
    {
        base.CacheReferences();
        button ??= GetComponent<Button>();
        motionTarget ??= transform as RectTransform;
        if (button != null)
        {
            tintGraphic ??= button.targetGraphic;
        }

        if (fadeTarget == null)
        {
            fadeTarget = GetComponent<CanvasGroup>();
        }
    }

    protected override void OnInitialize()
    {
        CacheReferences();
        ApplyDefaultStyles();

        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
            button.onClick.AddListener(HandleButtonClicked);
            if (disableSelectableTransition)
            {
                button.transition = Selectable.Transition.None;
            }
        }

        baseAnchoredPosition = motionTarget != null ? motionTarget.anchoredPosition : Vector2.zero;
        baseScale = motionTarget != null ? motionTarget.localScale : Vector3.one;
        baseColor = tintGraphic != null ? tintGraphic.color : Color.white;
        baseAlpha = fadeTarget != null ? fadeTarget.alpha : 1f;

        currentVisualState = ResolveVisualState();
        targetVisualState = currentVisualState;
        currentSnapshot = GetSnapshot(currentVisualState);
        fromSnapshot = currentSnapshot;
        toSnapshot = currentSnapshot;
        ApplySnapshot(currentSnapshot, true);
    }

    protected override void OnWidgetShown()
    {
        transitionRunning = false;
        currentVisualState = ResolveVisualState();
        targetVisualState = currentVisualState;
        currentSnapshot = GetSnapshot(currentVisualState);
        ApplySnapshot(currentSnapshot, true);
    }

    protected override void OnWidgetHidden()
    {
        pointerInside = false;
        pointerPressed = false;
        selectedFromEventSystem = false;
        hasManualSelectionState = false;
        manualSelectionState = false;
        transitionRunning = false;
        clickPunchStartTime = -1f;
    }

    protected override void OnWidgetDestroyed()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    public override void SetWidgetInteractable(bool interactable, bool updateCanvasGroup = true)
    {
        base.SetWidgetInteractable(interactable, updateCanvasGroup);
        if (button != null)
        {
            button.interactable = interactable;
        }

        BeginTransition(ResolveVisualState(), false);
    }

    private void Update()
    {
        if (!IsInitialized)
        {
            return;
        }

        VisualState resolvedState = ResolveVisualState();
        if (resolvedState != targetVisualState)
        {
            BeginTransition(resolvedState, false);
        }

        bool shouldAnimate = transitionRunning || IsClickPunchActive();
        if (!shouldAnimate)
        {
            return;
        }

        UpdateVisualAnimation();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        BeginTransition(ResolveVisualState(), false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        pointerPressed = false;
        BeginTransition(ResolveVisualState(), false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        pointerPressed = true;
        BeginTransition(ResolveVisualState(), false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        pointerPressed = false;
        BeginTransition(ResolveVisualState(), false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        selectedFromEventSystem = true;
        BeginTransition(ResolveVisualState(), false);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selectedFromEventSystem = false;
        pointerPressed = false;
        BeginTransition(ResolveVisualState(), false);
    }

    public void SetSelectedState(bool selected, bool useManualSelection = true)
    {
        if (useManualSelection)
        {
            hasManualSelectionState = true;
            manualSelectionState = selected;
        }
        else
        {
            selectedFromEventSystem = selected;
        }

        BeginTransition(ResolveVisualState(), false);
    }

    public void ClearSelectedStateOverride()
    {
        hasManualSelectionState = false;
        manualSelectionState = false;
        BeginTransition(ResolveVisualState(), false);
    }

    private void ApplyDefaultStyles()
    {
        if (normalStyle.scale == Vector3.zero)
        {
            normalStyle = VisualStyle.Create(Vector2.zero, Vector3.one, Color.white, 1f);
        }

        if (highlightedStyle.scale == Vector3.zero)
        {
            highlightedStyle = VisualStyle.Create(new Vector2(0f, 6f), new Vector3(1.02f, 1.02f, 1f), Color.white, 1f);
        }

        if (selectedStyle.scale == Vector3.zero)
        {
            selectedStyle = VisualStyle.Create(new Vector2(0f, 4f), new Vector3(1.01f, 1.01f, 1f), new Color(1f, 0.98f, 0.92f, 1f), 1f);
        }

        if (pressedStyle.scale == Vector3.zero)
        {
            pressedStyle = VisualStyle.Create(new Vector2(0f, 2f), new Vector3(0.98f, 0.98f, 1f), new Color(0.95f, 0.95f, 0.95f, 1f), 1f);
        }

        if (disabledStyle.scale == Vector3.zero)
        {
            disabledStyle = VisualStyle.Create(Vector2.zero, Vector3.one, new Color(0.8f, 0.8f, 0.8f, 1f), 0.6f);
        }

        transitionCurve ??= AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        clickPunchCurve ??= new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 6f),
            new Keyframe(0.35f, 1f),
            new Keyframe(1f, 0f, -6f, 0f));
    }

    private VisualState ResolveVisualState()
    {
        if (button == null || !button.IsInteractable())
        {
            return VisualState.Disabled;
        }

        bool selected = hasManualSelectionState ? manualSelectionState : selectedFromEventSystem;
        if (pointerPressed && pointerInside)
        {
            return VisualState.Pressed;
        }

        if (selected && useSelectedStyle)
        {
            return VisualState.Selected;
        }

        if (pointerInside || (trackSelectionAsHover && selected))
        {
            return VisualState.Highlighted;
        }

        return VisualState.Normal;
    }

    private void BeginTransition(VisualState nextState, bool instant)
    {
        targetVisualState = nextState;
        currentVisualState = nextState;

        VisualSnapshot snapshot = GetSnapshot(nextState);
        if (instant || transitionDuration <= 0f)
        {
            transitionRunning = false;
            currentSnapshot = snapshot;
            fromSnapshot = snapshot;
            toSnapshot = snapshot;
            ApplySnapshot(snapshot, true);
            return;
        }

        fromSnapshot = currentSnapshot;
        toSnapshot = snapshot;
        transitionStartTime = TimeNow;
        transitionRunning = true;
    }

    private void UpdateVisualAnimation()
    {
        if (transitionRunning)
        {
            float progress = Mathf.Clamp01((TimeNow - transitionStartTime) / Mathf.Max(0.0001f, transitionDuration));
            float curveValue = transitionCurve != null ? transitionCurve.Evaluate(progress) : progress;
            currentSnapshot = VisualSnapshot.Lerp(fromSnapshot, toSnapshot, curveValue);
            if (progress >= 1f)
            {
                transitionRunning = false;
                currentSnapshot = toSnapshot;
            }
        }
        else
        {
            currentSnapshot = GetSnapshot(targetVisualState);
        }

        ApplySnapshot(currentSnapshot, false);
    }

    private void ApplySnapshot(VisualSnapshot snapshot, bool resetPunch)
    {
        float punchScale = resetPunch ? 0f : EvaluateClickPunch();

        if (motionTarget != null)
        {
            motionTarget.anchoredPosition = baseAnchoredPosition + snapshot.anchoredOffset;
            motionTarget.localScale = Vector3.Scale(baseScale, snapshot.scale) * (1f + punchScale);
        }

        if (tintGraphic != null)
        {
            tintGraphic.color = MultiplyColor(baseColor, snapshot.color);
        }

        if (fadeTarget != null)
        {
            fadeTarget.alpha = baseAlpha * snapshot.alpha;
        }
    }

    private VisualSnapshot GetSnapshot(VisualState state)
    {
        VisualStyle style = state switch
        {
            VisualState.Highlighted => highlightedStyle,
            VisualState.Selected => selectedStyle,
            VisualState.Pressed => pressedStyle,
            VisualState.Disabled => disabledStyle,
            _ => normalStyle
        };

        return new VisualSnapshot
        {
            anchoredOffset = style.anchoredOffset,
            scale = style.scale == Vector3.zero ? Vector3.one : style.scale,
            color = style.color,
            alpha = Mathf.Clamp01(style.alpha)
        };
    }

    private void HandleButtonClicked()
    {
        if (clickPunchScale <= 0f || clickPunchDuration <= 0f)
        {
            return;
        }

        clickPunchStartTime = TimeNow;
    }

    private bool IsClickPunchActive()
    {
        if (clickPunchStartTime < 0f || clickPunchDuration <= 0f)
        {
            return false;
        }

        return TimeNow - clickPunchStartTime < clickPunchDuration;
    }

    private float EvaluateClickPunch()
    {
        if (!IsClickPunchActive())
        {
            return 0f;
        }

        float progress = Mathf.Clamp01((TimeNow - clickPunchStartTime) / clickPunchDuration);
        if (progress >= 1f)
        {
            clickPunchStartTime = -1f;
            return 0f;
        }

        return clickPunchCurve != null ? clickPunchCurve.Evaluate(progress) * clickPunchScale : 0f;
    }

    private static Color MultiplyColor(Color left, Color right)
    {
        return new Color(
            left.r * right.r,
            left.g * right.g,
            left.b * right.b,
            left.a * right.a);
    }
}

