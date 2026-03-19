using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.Cinemachine;

[DisallowMultipleComponent]
public sealed class StudentIdCardTutorialInteractable : MonoBehaviour, IInteractable
{
    [Serializable]
    public sealed class StringUnityEvent : UnityEvent<string>
    {
    }

    private sealed class CinemachineSession
    {
        public CinemachineBrain Brain;
        public bool CreatedBrain;
        public bool PreviousBrainEnabled;
        public bool PreviousIgnoreTimeScale;
        public CinemachineBlendDefinition PreviousDefaultBlend;
        public CinemachineBrain.UpdateMethods PreviousUpdateMethod;
        public CinemachineBrain.BrainUpdateMethods PreviousBlendUpdateMethod;
        public CinemachineCamera GameplayCamera;
        public CinemachineCamera InspectCamera;
    }

    [Header("References")]
    [SerializeField] private StudentIdCardRenderer cardRenderer;
    [SerializeField] private Collider interactionCollider;
    [SerializeField] private Transform focusCameraAnchor;

    [Header("Labels")]
    [SerializeField] private string firstInteractionLabel = "填写学生证";
    [SerializeField] private string repeatInteractionLabel = "查看或修改学生证";
    [SerializeField] private string fallbackName = "新入生";

    [Header("Camera")]
    [SerializeField] private bool preferCinemachine = true;
    [SerializeField] private float focusTransitionDuration = 0.4f;
    [SerializeField] private float returnTransitionDuration = 0.28f;
    [SerializeField] private float focusedFieldOfView = 26f;
    [SerializeField] private Vector3 fallbackFocusOffset = new Vector3(0.08f, 0.18f, -0.06f);

    [Header("Flow")]
    [SerializeField] private bool allowCancel = true;
    [SerializeField] private bool disableInteractionAfterCompletion;

    [Header("Events")]
    [SerializeField] private StringUnityEvent onNameConfirmed = new StringUnityEvent();

    [SerializeField, HideInInspector] private bool completed;

    private Coroutine interactionRoutine;

    public bool Completed => completed;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
        focusTransitionDuration = Mathf.Max(0.05f, focusTransitionDuration);
        returnTransitionDuration = Mathf.Max(0.05f, returnTransitionDuration);
        focusedFieldOfView = Mathf.Clamp(focusedFieldOfView, 15f, 70f);
    }

    public string GetInteractionLabel(PlayerInteractor interactor)
    {
        if (disableInteractionAfterCompletion && completed)
        {
            return string.Empty;
        }

        return completed
            ? TrimOrDefault(repeatInteractionLabel, "查看或修改学生证")
            : TrimOrDefault(firstInteractionLabel, "填写学生证");
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        ResolveReferences();
        return interactionRoutine == null
            && interactor != null
            && interactor.InteractionCamera != null
            && cardRenderer != null
            && (!disableInteractionAfterCompletion || !completed);
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        interactionRoutine = StartCoroutine(RunInteraction(interactor));
    }

    public Transform GetInteractionTransform()
    {
        return interactionCollider != null ? interactionCollider.transform : transform;
    }

    private IEnumerator RunInteraction(PlayerInteractor interactor)
    {
        Camera targetCamera = interactor != null ? interactor.InteractionCamera : null;
        if (targetCamera == null)
        {
            interactionRoutine = null;
            yield break;
        }

        ResolveReferences();

        PrototypeFpsController fpsController = interactor.GetComponent<PrototypeFpsController>();
        PlayerInteractionState interactionState = interactor.GetComponent<PlayerInteractionState>();

        bool previousInteractorEnabled = interactor.enabled;
        bool previousFpsControllerEnabled = fpsController != null && fpsController.enabled;

        Transform originalParent = targetCamera.transform.parent;
        Vector3 originalLocalPosition = targetCamera.transform.localPosition;
        Quaternion originalLocalRotation = targetCamera.transform.localRotation;
        Vector3 originalWorldPosition = targetCamera.transform.position;
        Quaternion originalWorldRotation = targetCamera.transform.rotation;
        float originalFieldOfView = targetCamera.fieldOfView;
        CinemachineSession cameraSession = preferCinemachine
            ? TryCreateCinemachineSession(targetCamera, originalWorldPosition, originalWorldRotation, originalFieldOfView)
            : null;

        bool finished = false;
        bool confirmed = false;
        string originalName = cardRenderer.Content != null ? cardRenderer.Content.FullName : string.Empty;
        string pendingName = SanitizeName(originalName);

        StudentIdCardNameEntryWindowController nameEntryWindow = null;

        try
        {
            if (interactionState != null)
            {
                interactionState.SetUiFocused(this, true);
            }

            interactor.enabled = false;
            if (fpsController != null)
            {
                fpsController.enabled = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (cameraSession != null)
            {
                yield return StartCinemachineFocus(cameraSession);
            }
            else
            {
                targetCamera.transform.SetParent(null, true);
                ResolveFocusPose(out Vector3 focusPosition, out Quaternion focusRotation);
                yield return AnimateCamera(
                    targetCamera,
                    originalWorldPosition,
                    originalWorldRotation,
                    originalFieldOfView,
                    focusPosition,
                    focusRotation,
                    focusedFieldOfView,
                    focusTransitionDuration);
            }

            nameEntryWindow = StudentIdCardNameEntryWindowController.GetOrCreate();
            nameEntryWindow.Open(
                pendingName,
                allowCancel,
                value =>
                {
                    pendingName = SanitizeName(value);
                    confirmed = !string.IsNullOrWhiteSpace(pendingName);
                    finished = true;
                },
                () =>
                {
                    RestorePreviewName(originalName);
                    finished = true;
                },
                PreviewNameInput);

            while (!finished)
            {
                yield return null;
            }

            if (confirmed)
            {
                ApplyConfirmedName(pendingName);
            }
            else
            {
                RestorePreviewName(originalName);
            }

            if (cameraSession != null)
            {
                yield return EndCinemachineFocus(cameraSession);
            }
            else
            {
                yield return AnimateCamera(
                    targetCamera,
                    targetCamera.transform.position,
                    targetCamera.transform.rotation,
                    targetCamera.fieldOfView,
                    originalWorldPosition,
                    originalWorldRotation,
                    originalFieldOfView,
                    returnTransitionDuration);
            }
        }
        finally
        {
            if (nameEntryWindow != null && nameEntryWindow.IsOpen)
            {
                nameEntryWindow.Close();
            }

            if (cameraSession != null)
            {
                CleanupCinemachineSession(cameraSession);
            }

            if (targetCamera != null && cameraSession == null)
            {
                targetCamera.transform.SetParent(originalParent, true);
                targetCamera.transform.localPosition = originalLocalPosition;
                targetCamera.transform.localRotation = originalLocalRotation;
                targetCamera.fieldOfView = originalFieldOfView;
            }
            else if (targetCamera != null)
            {
                targetCamera.fieldOfView = originalFieldOfView;
            }

            if (fpsController != null)
            {
                fpsController.enabled = previousFpsControllerEnabled;
            }

            if (interactor != null)
            {
                interactor.enabled = previousInteractorEnabled;
            }

            if (interactionState != null)
            {
                interactionState.SetUiFocused(this, false);
                bool keepCursorFree = interactionState.IsUiFocused;
                Cursor.lockState = keepCursorFree ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = keepCursorFree;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            interactionRoutine = null;
        }
    }

    private void PreviewNameInput(string previewName)
    {
        if (cardRenderer == null || cardRenderer.Content == null)
        {
            return;
        }

        cardRenderer.Content.FullName = previewName ?? string.Empty;
        cardRenderer.RefreshCard();
    }

    private void RestorePreviewName(string originalName)
    {
        if (cardRenderer == null || cardRenderer.Content == null)
        {
            return;
        }

        cardRenderer.Content.FullName = originalName ?? string.Empty;
        cardRenderer.RefreshCard();
    }

    private void ApplyConfirmedName(string newName)
    {
        string finalName = string.IsNullOrWhiteSpace(newName)
            ? TrimOrDefault(fallbackName, "新入生")
            : newName.Trim();

        if (cardRenderer.Content != null)
        {
            cardRenderer.Content.FullName = finalName;
        }

        cardRenderer.RefreshCard();
        completed = true;
        onNameConfirmed?.Invoke(finalName);
    }

    private CinemachineSession TryCreateCinemachineSession(
        Camera targetCamera,
        Vector3 originalWorldPosition,
        Quaternion originalWorldRotation,
        float originalFieldOfView)
    {
        if (targetCamera == null)
        {
            return null;
        }

        CinemachineBrain brain = targetCamera.GetComponent<CinemachineBrain>();
        bool createdBrain = false;
        bool previousBrainEnabled = false;
        bool previousIgnoreTimeScale = false;
        CinemachineBlendDefinition previousBlend = default;
        CinemachineBrain.UpdateMethods previousUpdateMethod = CinemachineBrain.UpdateMethods.SmartUpdate;
        CinemachineBrain.BrainUpdateMethods previousBlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;

        if (brain == null)
        {
            brain = targetCamera.gameObject.AddComponent<CinemachineBrain>();
            createdBrain = true;
        }
        else
        {
            previousBrainEnabled = brain.enabled;
            previousIgnoreTimeScale = brain.IgnoreTimeScale;
            previousBlend = brain.DefaultBlend;
            previousUpdateMethod = brain.UpdateMethod;
            previousBlendUpdateMethod = brain.BlendUpdateMethod;
        }

        brain.enabled = true;
        brain.IgnoreTimeScale = true;
        brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
        brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;

        CinemachineCamera gameplayCamera = CreateTemporaryCinemachineCamera(
            "StudentIdCardGameplayShot",
            originalWorldPosition,
            originalWorldRotation,
            targetCamera,
            originalFieldOfView,
            1000);

        ResolveFocusPose(out Vector3 focusPosition, out Quaternion focusRotation);
        CinemachineCamera inspectCamera = CreateTemporaryCinemachineCamera(
            "StudentIdCardInspectShot",
            focusPosition,
            focusRotation,
            targetCamera,
            focusedFieldOfView,
            1100);

        if (inspectCamera != null)
        {
            inspectCamera.gameObject.SetActive(false);
        }

        if (gameplayCamera == null || inspectCamera == null)
        {
            if (gameplayCamera != null)
            {
                Destroy(gameplayCamera.gameObject);
            }

            if (inspectCamera != null)
            {
                Destroy(inspectCamera.gameObject);
            }

            if (createdBrain)
            {
                Destroy(brain);
            }
            else
            {
                brain.enabled = previousBrainEnabled;
                brain.IgnoreTimeScale = previousIgnoreTimeScale;
                brain.DefaultBlend = previousBlend;
                brain.UpdateMethod = previousUpdateMethod;
                brain.BlendUpdateMethod = previousBlendUpdateMethod;
            }

            return null;
        }

        inspectCamera.gameObject.SetActive(false);
        gameplayCamera.Prioritize();

        return new CinemachineSession
        {
            Brain = brain,
            CreatedBrain = createdBrain,
            PreviousBrainEnabled = previousBrainEnabled,
            PreviousIgnoreTimeScale = previousIgnoreTimeScale,
            PreviousDefaultBlend = previousBlend,
            PreviousUpdateMethod = previousUpdateMethod,
            PreviousBlendUpdateMethod = previousBlendUpdateMethod,
            GameplayCamera = gameplayCamera,
            InspectCamera = inspectCamera
        };
    }

    private IEnumerator StartCinemachineFocus(CinemachineSession session)
    {
        if (session == null || session.Brain == null || session.GameplayCamera == null || session.InspectCamera == null)
        {
            yield break;
        }

        session.GameplayCamera.Priority = 1000;
        session.InspectCamera.Priority = 900;
        session.Brain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.Cut,
            0f);

        session.Brain.ManualUpdate();
        yield return null;
        session.Brain.ManualUpdate();

        session.Brain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut,
            focusTransitionDuration);

        session.InspectCamera.gameObject.SetActive(true);
        session.InspectCamera.Priority = 1100;
        yield return DriveCinemachineBrain(session, focusTransitionDuration);
    }

    private IEnumerator EndCinemachineFocus(CinemachineSession session)
    {
        if (session == null || session.Brain == null || session.GameplayCamera == null || session.InspectCamera == null)
        {
            yield break;
        }

        session.Brain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut,
            returnTransitionDuration);

        session.GameplayCamera.Priority = 1200;
        session.InspectCamera.Priority = 1100;
        yield return DriveCinemachineBrain(session, returnTransitionDuration);
        session.InspectCamera.gameObject.SetActive(false);
    }

    private static IEnumerator DriveCinemachineBrain(CinemachineSession session, float duration)
    {
        if (session == null || session.Brain == null)
        {
            yield break;
        }

        float totalDuration = Mathf.Max(0.05f, duration);
        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            session.Brain.ManualUpdate();
            yield return null;
            elapsed += Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : (1f / 60f);
        }

        session.Brain.ManualUpdate();
    }

    private void CleanupCinemachineSession(CinemachineSession session)
    {
        if (session == null)
        {
            return;
        }

        if (session.InspectCamera != null)
        {
            Destroy(session.InspectCamera.gameObject);
        }

        if (session.GameplayCamera != null)
        {
            Destroy(session.GameplayCamera.gameObject);
        }

        if (session.Brain == null)
        {
            return;
        }

        if (session.CreatedBrain)
        {
            Destroy(session.Brain);
            return;
        }

        session.Brain.enabled = session.PreviousBrainEnabled;
        session.Brain.IgnoreTimeScale = session.PreviousIgnoreTimeScale;
        session.Brain.DefaultBlend = session.PreviousDefaultBlend;
        session.Brain.UpdateMethod = session.PreviousUpdateMethod;
        session.Brain.BlendUpdateMethod = session.PreviousBlendUpdateMethod;
    }

    private static CinemachineCamera CreateTemporaryCinemachineCamera(
        string cameraName,
        Vector3 position,
        Quaternion rotation,
        Camera sourceCamera,
        float fieldOfView,
        int priority)
    {
        if (sourceCamera == null)
        {
            return null;
        }

        GameObject cameraObject = new GameObject(cameraName);
        cameraObject.transform.SetPositionAndRotation(position, rotation);

        CinemachineCamera virtualCamera = cameraObject.AddComponent<CinemachineCamera>();
        virtualCamera.Priority = priority;

        LensSettings lens = LensSettings.FromCamera(sourceCamera);
        lens.FieldOfView = fieldOfView;
        virtualCamera.Lens = lens;
        return virtualCamera;
    }

    private void ResolveFocusPose(out Vector3 position, out Quaternion rotation)
    {
        if (focusCameraAnchor != null)
        {
            position = focusCameraAnchor.position;
            rotation = focusCameraAnchor.rotation;
            return;
        }

        Transform focusTransform = cardRenderer != null ? cardRenderer.transform : transform;
        Renderer focusRenderer = FindFocusRenderer();
        Vector3 targetPoint = focusRenderer != null ? focusRenderer.bounds.center : focusTransform.position;
        Vector3 right = focusTransform.right.normalized;
        Vector3 normal = focusTransform.up.normalized;
        Vector3 top = focusTransform.forward.normalized;

        position = targetPoint
            + right * fallbackFocusOffset.x
            + normal * fallbackFocusOffset.y
            + top * fallbackFocusOffset.z;
        rotation = Quaternion.LookRotation((targetPoint - position).normalized, top);
    }

    private Renderer FindFocusRenderer()
    {
        if (cardRenderer == null)
        {
            return null;
        }

        MeshRenderer[] renderers = cardRenderer.GetComponentsInChildren<MeshRenderer>(true);
        for (int index = 0; index < renderers.Length; index++)
        {
            MeshRenderer renderer = renderers[index];
            if (renderer != null && renderer.name == "StudentID_Card")
            {
                return renderer;
            }
        }

        return cardRenderer.GetComponentInChildren<MeshRenderer>();
    }

    private void ResolveReferences()
    {
        if (cardRenderer == null)
        {
            cardRenderer = GetComponent<StudentIdCardRenderer>();
            if (cardRenderer == null)
            {
                cardRenderer = GetComponentInChildren<StudentIdCardRenderer>(true);
            }
        }

        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<Collider>();
            if (interactionCollider == null)
            {
                interactionCollider = GetComponentInChildren<Collider>(true);
            }
        }

        if (focusCameraAnchor == null)
        {
            Transform found = transform.Find("InspectCameraAnchor");
            if (found != null)
            {
                focusCameraAnchor = found;
            }
        }
    }

    private static IEnumerator AnimateCamera(
        Camera camera,
        Vector3 fromPosition,
        Quaternion fromRotation,
        float fromFieldOfView,
        Vector3 toPosition,
        Quaternion toRotation,
        float toFieldOfView,
        float duration)
    {
        if (camera == null)
        {
            yield break;
        }

        if (duration <= 0.001f)
        {
            camera.transform.SetPositionAndRotation(toPosition, toRotation);
            camera.fieldOfView = toFieldOfView;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            camera.transform.position = Vector3.LerpUnclamped(fromPosition, toPosition, eased);
            camera.transform.rotation = Quaternion.SlerpUnclamped(fromRotation, toRotation, eased);
            camera.fieldOfView = Mathf.LerpUnclamped(fromFieldOfView, toFieldOfView, eased);
            yield return null;
        }

        camera.transform.SetPositionAndRotation(toPosition, toRotation);
        camera.fieldOfView = toFieldOfView;
    }

    private static string SanitizeName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string TrimOrDefault(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
