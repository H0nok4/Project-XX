using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UiTransitionPlayer : UiWidgetBase
{
    private struct TransitionPose
    {
        public Vector2 anchoredPosition;
        public Vector3 scale;
        public float alpha;

        public static TransitionPose Lerp(TransitionPose from, TransitionPose to, float t)
        {
            return new TransitionPose
            {
                anchoredPosition = Vector2.LerpUnclamped(from.anchoredPosition, to.anchoredPosition, t),
                scale = Vector3.LerpUnclamped(from.scale, to.scale, t),
                alpha = Mathf.LerpUnclamped(from.alpha, to.alpha, t)
            };
        }
    }

    [Header("Transition")]
    [SerializeField] private RectTransform motionTarget;
    [SerializeField] private CanvasGroup targetCanvasGroup;
    [SerializeField] private bool autoCreateTargetCanvasGroup = true;
    [SerializeField] private bool captureShownPoseOnInitialize = true;
    [SerializeField] private bool captureShownPoseEachShow;
    [SerializeField] private bool applyAnchoredPosition = true;
    [SerializeField] private bool applyScale = true;
    [SerializeField] private bool applyAlpha = true;
    [SerializeField] private bool interactableWhenShown = true;
    [SerializeField] private bool blockRaycastsWhenShown = true;
    [SerializeField] private float showDuration = 0.18f;
    [SerializeField] private float hideDuration = 0.12f;
    [SerializeField] private AnimationCurve showCurve = null;
    [SerializeField] private AnimationCurve hideCurve = null;
    [SerializeField] private Vector2 hiddenOffset = new Vector2(0f, -24f);
    [SerializeField] private Vector3 hiddenScale = new Vector3(0.98f, 0.98f, 1f);
    [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;

    private TransitionPose shownPose;
    private TransitionPose hiddenPose;
    private TransitionPose fromPose;
    private TransitionPose toPose;
    private TransitionPose currentPose;
    private Action pendingCompletion;
    private AnimationCurve activeCurve;
    private float activeDuration;
    private float transitionElapsed;
    private bool isPlaying;
    private bool isShown = true;
    private bool hasCapturedShownPose;

    public RectTransform MotionTarget => motionTarget != null ? motionTarget : motionTarget = RectTransform;
    public CanvasGroup TargetCanvasGroup => targetCanvasGroup != null ? targetCanvasGroup : targetCanvasGroup = ResolveCanvasGroup(false);
    public bool IsPlaying => isPlaying;
    public bool IsShown => isShown;

    protected override bool AutoCreateCanvasGroup => autoCreateTargetCanvasGroup;

    protected override void CacheReferences()
    {
        base.CacheReferences();
        motionTarget ??= RectTransform;
        targetCanvasGroup ??= GetComponent<CanvasGroup>();
    }

    protected override void OnInitialize()
    {
        CacheReferences();
        showCurve ??= AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        hideCurve ??= AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        if (autoCreateTargetCanvasGroup)
        {
            targetCanvasGroup = ResolveCanvasGroup(true);
        }

        if (captureShownPoseOnInitialize)
        {
            CaptureShownPose();
            currentPose = ReadCurrentPose();
            isShown = gameObject.activeSelf && (!applyAlpha || targetCanvasGroup == null || targetCanvasGroup.alpha > hiddenAlpha + 0.001f);
        }
        else
        {
            currentPose = ReadCurrentPose();
        }
    }

    protected override void OnWidgetHidden()
    {
        isPlaying = false;
        pendingCompletion = null;
    }

    protected override void OnWidgetDestroyed()
    {
        isPlaying = false;
        pendingCompletion = null;
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        transitionElapsed += DeltaTime;
        float normalizedTime = activeDuration <= 0f ? 1f : Mathf.Clamp01(transitionElapsed / activeDuration);
        float curveValue = activeCurve != null ? activeCurve.Evaluate(normalizedTime) : normalizedTime;
        currentPose = TransitionPose.Lerp(fromPose, toPose, curveValue);
        ApplyPose(currentPose);

        if (normalizedTime < 1f)
        {
            return;
        }

        isPlaying = false;
        currentPose = toPose;
        ApplyPose(currentPose);
        ApplyInteractionState(isShown);

        Action completion = pendingCompletion;
        pendingCompletion = null;
        completion?.Invoke();
    }

    public void CaptureShownPose()
    {
        CacheReferences();
        if (autoCreateTargetCanvasGroup && targetCanvasGroup == null)
        {
            targetCanvasGroup = ResolveCanvasGroup(true);
        }

        shownPose = ReadCurrentPose();
        hiddenPose = new TransitionPose
        {
            anchoredPosition = shownPose.anchoredPosition + hiddenOffset,
            scale = Vector3.Scale(shownPose.scale, ResolveHiddenScale()),
            alpha = hiddenAlpha
        };
        hasCapturedShownPose = true;
    }

    public void PlayShow(bool instant = false, Action onComplete = null)
    {
        EnsureInitialized();
        if (captureShownPoseEachShow || !hasCapturedShownPose)
        {
            CaptureShownPose();
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        isShown = true;
        pendingCompletion = onComplete;
        ApplyInteractionState(false);
        BeginTransition(ReadCurrentPose(), shownPose, showDuration, showCurve, instant);
    }

    public void PlayHide(bool instant = false, Action onComplete = null)
    {
        EnsureInitialized();
        if (!hasCapturedShownPose)
        {
            CaptureShownPose();
        }

        isShown = false;
        pendingCompletion = onComplete;
        ApplyInteractionState(false);
        BeginTransition(ReadCurrentPose(), hiddenPose, hideDuration, hideCurve, instant);
    }

    public void ShowAndActivate(bool instant = false, Action onComplete = null)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        PlayShow(instant, onComplete);
    }

    public void HideAndDeactivate(bool instant = false, Action onComplete = null)
    {
        PlayHide(
            instant,
            () =>
            {
                if (this != null && gameObject != null)
                {
                    gameObject.SetActive(false);
                }

                onComplete?.Invoke();
            });
    }

    public void SnapToShown()
    {
        EnsureInitialized();
        if (!hasCapturedShownPose)
        {
            CaptureShownPose();
        }

        isShown = true;
        pendingCompletion = null;
        isPlaying = false;
        ApplyPose(shownPose);
        ApplyInteractionState(true);
    }

    public void SnapToHidden()
    {
        EnsureInitialized();
        if (!hasCapturedShownPose)
        {
            CaptureShownPose();
        }

        isShown = false;
        pendingCompletion = null;
        isPlaying = false;
        ApplyPose(hiddenPose);
        ApplyInteractionState(false);
    }

    private void BeginTransition(TransitionPose from, TransitionPose to, float duration, AnimationCurve curve, bool instant)
    {
        fromPose = from;
        toPose = to;
        activeDuration = Mathf.Max(0f, duration);
        activeCurve = curve;
        transitionElapsed = 0f;

        if (instant || activeDuration <= 0f)
        {
            isPlaying = false;
            currentPose = toPose;
            ApplyPose(currentPose);
            ApplyInteractionState(isShown);

            Action completion = pendingCompletion;
            pendingCompletion = null;
            completion?.Invoke();
            return;
        }

        isPlaying = true;
        currentPose = fromPose;
        ApplyPose(currentPose);
    }

    private TransitionPose ReadCurrentPose()
    {
        return new TransitionPose
        {
            anchoredPosition = MotionTarget != null ? MotionTarget.anchoredPosition : Vector2.zero,
            scale = MotionTarget != null ? MotionTarget.localScale : Vector3.one,
            alpha = targetCanvasGroup != null ? targetCanvasGroup.alpha : 1f
        };
    }

    private void ApplyPose(TransitionPose pose)
    {
        if (MotionTarget != null)
        {
            if (applyAnchoredPosition)
            {
                MotionTarget.anchoredPosition = pose.anchoredPosition;
            }

            if (applyScale)
            {
                MotionTarget.localScale = pose.scale;
            }
        }

        if (applyAlpha && targetCanvasGroup != null)
        {
            targetCanvasGroup.alpha = pose.alpha;
        }
    }

    private void ApplyInteractionState(bool visible)
    {
        if (targetCanvasGroup == null)
        {
            return;
        }

        targetCanvasGroup.interactable = visible && interactableWhenShown;
        targetCanvasGroup.blocksRaycasts = visible && blockRaycastsWhenShown;
    }

    private Vector3 ResolveHiddenScale()
    {
        return hiddenScale == Vector3.zero ? Vector3.one : hiddenScale;
    }
}
