using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeFpsInput))]
public class PlayerInteractor : MonoBehaviour
{
    private enum InteractionQuerySource
    {
        DirectCameraRay = 0,
        CameraSphereCast = 1,
        PlayerReachSphereCast = 2
    }

    private readonly struct InteractionCandidate
    {
        public readonly IInteractable Interactable;
        public readonly Collider Collider;
        public readonly RaycastHit Hit;
        public readonly float Score;
        public readonly InteractionQuerySource Source;

        public InteractionCandidate(
            IInteractable interactable,
            Collider collider,
            RaycastHit hit,
            float score,
            InteractionQuerySource source)
        {
            Interactable = interactable;
            Collider = collider;
            Hit = hit;
            Score = score;
            Source = source;
        }
    }

    [Header("References")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private PrototypeFpsInput fpsInput;
    [SerializeField] private InventoryContainer primaryInventory;
    [SerializeField] private InventoryContainer secureInventory;
    [SerializeField] private InventoryContainer specialInventory;
    [SerializeField] private PlayerInteractionState interactionState;
    [SerializeField] private PlayerAimPointResolver aimPointResolver;
    [SerializeField] private CharacterController characterController;

    [Header("Query")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("TPS Assist")]
    [SerializeField] private bool enableThirdPersonAimAssist = true;
    [SerializeField] private float assistSphereRadius = 0.4f;
    [SerializeField] private float assistReachSlack = 0.55f;
    [SerializeField] private float assistOriginHeight = 1.15f;
    [SerializeField] private float assistViewportRadius = 0.18f;

    [Header("Prompt")]
    [SerializeField] private bool showPrompt = true;
    [SerializeField] private Vector2 promptSize = new Vector2(320f, 42f);
    [SerializeField] private bool autoAddInventoryWindowController = true;

    private InteractionQueryResult currentQuery;
    private InteractionPromptView promptView;
    private readonly List<InteractionCandidate> candidateBuffer = new List<InteractionCandidate>(8);

    public InventoryContainer PrimaryInventory => primaryInventory;
    public InventoryContainer SecureInventory => secureInventory;
    public InventoryContainer SpecialInventory => specialInventory;
    public Camera InteractionCamera => aimPointResolver != null && aimPointResolver.ActiveAimCamera != null
        ? aimPointResolver.ActiveAimCamera
        : interactionCamera;
    public IInteractable CurrentInteractable => currentQuery.Interactable;
    public bool HasInteractionTarget => currentQuery.HasValue;

    private void Awake()
    {
        ResolveReferences();
        EnsureInventoryWindowController();
        EnsurePromptUi();
    }

    private void Update()
    {
        if (interactionState != null && interactionState.IsUiFocused)
        {
            currentQuery = default;
            UpdatePromptUi();
            return;
        }

        RefreshInteractionQuery();

        if (fpsInput != null && fpsInput.InteractPressedThisFrame && currentQuery.HasValue)
        {
            IInteractable interactable = currentQuery.Interactable;
            if (interactable != null && interactable.CanInteract(this))
            {
                interactable.Interact(this);
                RefreshInteractionQuery();
            }
        }

        UpdatePromptUi();
    }

    private void OnValidate()
    {
        ResolveReferences();
        interactDistance = Mathf.Max(0.5f, interactDistance);
        assistSphereRadius = Mathf.Clamp(assistSphereRadius, 0f, 1.5f);
        assistReachSlack = Mathf.Clamp(assistReachSlack, 0f, 2f);
        assistOriginHeight = Mathf.Clamp(assistOriginHeight, 0.25f, 2.5f);
        assistViewportRadius = Mathf.Clamp(assistViewportRadius, 0.05f, 0.45f);
        promptSize.x = Mathf.Max(160f, promptSize.x);
        promptSize.y = Mathf.Max(24f, promptSize.y);
    }

    public void Configure(
        Camera targetCamera,
        InventoryContainer inventory = null,
        InventoryContainer secureContainer = null,
        InventoryContainer specialContainer = null)
    {
        interactionCamera = targetCamera;
        if (inventory != null)
        {
            primaryInventory = inventory;
        }

        if (secureContainer != null)
        {
            secureInventory = secureContainer;
        }

        if (specialContainer != null)
        {
            specialInventory = specialContainer;
        }

        ResolveReferences();
    }

    private void OnDestroy()
    {
        promptView = null;
    }

    private void OnDisable()
    {
        promptView?.SetPrompt(string.Empty, promptSize, false);
    }

    private void RefreshInteractionQuery()
    {
        currentQuery = default;

        Camera activeCamera = InteractionCamera;
        if (activeCamera == null && aimPointResolver == null)
        {
            return;
        }

        Ray ray = aimPointResolver != null
            ? aimPointResolver.CurrentAimRay
            : new Ray(activeCamera.transform.position, activeCamera.transform.forward);

        if (!TryResolveInteractionQuery(ray, activeCamera, out InteractionQueryResult query))
        {
            return;
        }

        currentQuery = query;
    }

    private void ResolveReferences()
    {
        if (fpsInput == null)
        {
            fpsInput = GetComponent<PrototypeFpsInput>();
        }

        if (aimPointResolver == null)
        {
            aimPointResolver = GetComponent<PlayerAimPointResolver>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (interactionCamera == null)
        {
            interactionCamera = aimPointResolver != null && aimPointResolver.ActiveAimCamera != null
                ? aimPointResolver.ActiveAimCamera
                : GetComponentInChildren<Camera>();
        }

        if (primaryInventory == null)
        {
            primaryInventory = GetComponent<InventoryContainer>();
        }

        if (secureInventory == null)
        {
            Transform secureTransform = transform.Find("SecureContainer_Runtime");
            if (secureTransform != null)
            {
                secureInventory = secureTransform.GetComponent<InventoryContainer>();
            }
        }

        if (specialInventory == null)
        {
            Transform specialTransform = transform.Find("SpecialEquipment_Runtime");
            if (specialTransform != null)
            {
                specialInventory = specialTransform.GetComponent<InventoryContainer>();
            }
        }

        if (interactionState == null)
        {
            interactionState = GetComponent<PlayerInteractionState>();
        }
    }

    private bool TryResolveInteractionQuery(Ray cameraRay, Camera activeCamera, out InteractionQueryResult query)
    {
        candidateBuffer.Clear();
        float cameraQueryDistance = GetCameraQueryDistance(activeCamera);

        CollectRayCandidates(cameraRay, activeCamera, 0f, InteractionQuerySource.DirectCameraRay, cameraQueryDistance);

        if (enableThirdPersonAimAssist && assistSphereRadius > 0.001f)
        {
            CollectRayCandidates(
                cameraRay,
                activeCamera,
                assistSphereRadius,
                InteractionQuerySource.CameraSphereCast,
                cameraQueryDistance);

            if (TryBuildPlayerReachRay(cameraRay, activeCamera, out Ray reachRay, out float reachDistance))
            {
                CollectRayCandidates(
                    reachRay,
                    activeCamera,
                    assistSphereRadius,
                    InteractionQuerySource.PlayerReachSphereCast,
                    reachDistance);
            }
        }

        if (candidateBuffer.Count == 0)
        {
            query = default;
            return false;
        }

        InteractionCandidate bestCandidate = candidateBuffer[0];
        for (int i = 1; i < candidateBuffer.Count; i++)
        {
            if (candidateBuffer[i].Score < bestCandidate.Score)
            {
                bestCandidate = candidateBuffer[i];
            }
        }

        query = new InteractionQueryResult(bestCandidate.Interactable, bestCandidate.Collider, bestCandidate.Hit);
        return true;
    }

    private void CollectRayCandidates(
        Ray ray,
        Camera activeCamera,
        float sphereRadius,
        InteractionQuerySource source,
        float maxDistance)
    {
        RaycastHit[] hits = sphereRadius > 0.001f
            ? Physics.SphereCastAll(ray, sphereRadius, maxDistance, interactionMask, triggerInteraction)
            : Physics.RaycastAll(ray, maxDistance, interactionMask, triggerInteraction);

        if (hits == null || hits.Length == 0)
        {
            return;
        }

        Array.Sort(hits, CompareByDistance);
        foreach (RaycastHit hit in hits)
        {
            if (!IsCandidateColliderValid(hit.collider))
            {
                continue;
            }

            IInteractable interactable = ResolveInteractable(hit.collider);
            if (interactable != null)
            {
                if (TryBuildCandidate(interactable, hit.collider, hit, activeCamera, source, out InteractionCandidate candidate))
                {
                    candidateBuffer.Add(candidate);
                }

                if (!hit.collider.isTrigger)
                {
                    break;
                }

                continue;
            }

            if (BlocksInteraction(hit.collider))
            {
                break;
            }
        }
    }

    private bool TryBuildPlayerReachRay(Ray cameraRay, Camera activeCamera, out Ray reachRay, out float reachDistance)
    {
        Vector3 origin = GetInteractionOrigin();
        Vector3 targetPoint = aimPointResolver != null
            ? aimPointResolver.CurrentAimWorldPoint
            : cameraRay.origin + cameraRay.direction * GetCameraQueryDistance(activeCamera);
        Vector3 toTarget = targetPoint - origin;

        float maxReach = interactDistance + assistReachSlack;
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            Vector3 fallbackDirection = new Vector3(cameraRay.direction.x, 0f, cameraRay.direction.z);
            if (fallbackDirection.sqrMagnitude <= 0.0001f)
            {
                fallbackDirection = transform.forward;
            }

            fallbackDirection.Normalize();
            reachDistance = maxReach;
            reachRay = new Ray(origin, fallbackDirection);
            return true;
        }

        float unclampedDistance = toTarget.magnitude;
        reachDistance = Mathf.Min(maxReach, unclampedDistance);
        if (reachDistance <= 0.05f)
        {
            reachRay = default;
            return false;
        }

        reachRay = new Ray(origin, toTarget / unclampedDistance);
        return true;
    }

    private float GetCameraQueryDistance(Camera activeCamera)
    {
        Vector3 interactionOrigin = GetInteractionOrigin();
        float cameraBackOffset = activeCamera != null
            ? Vector3.Distance(activeCamera.transform.position, interactionOrigin)
            : 0f;
        return interactDistance + assistReachSlack + cameraBackOffset;
    }

    private bool TryBuildCandidate(
        IInteractable interactable,
        Collider hitCollider,
        RaycastHit hit,
        Camera activeCamera,
        InteractionQuerySource source,
        out InteractionCandidate candidate)
    {
        candidate = default;
        if (interactable == null || hitCollider == null)
        {
            return false;
        }

        Vector3 evaluationPoint = ResolveEvaluationPoint(interactable, hitCollider, hit.point, activeCamera);
        if (!TryGetViewportOffset(activeCamera, evaluationPoint, out float viewportOffset))
        {
            return false;
        }

        if (source != InteractionQuerySource.DirectCameraRay && viewportOffset > assistViewportRadius)
        {
            return false;
        }

        Vector3 interactionOrigin = GetInteractionOrigin();
        float reachDistance = Vector3.Distance(interactionOrigin, evaluationPoint);
        float maxReach = interactDistance + assistReachSlack;
        if (reachDistance > maxReach)
        {
            return false;
        }

        float score = BuildCandidateScore(hit.distance, reachDistance, viewportOffset, source);
        candidate = new InteractionCandidate(interactable, hitCollider, hit, score, source);
        return true;
    }

    private Vector3 GetInteractionOrigin()
    {
        if (characterController != null)
        {
            Vector3 center = characterController.bounds.center;
            center.x = transform.position.x;
            center.z = transform.position.z;
            return center;
        }

        return transform.position + Vector3.up * assistOriginHeight;
    }

    private Vector3 ResolveEvaluationPoint(
        IInteractable interactable,
        Collider hitCollider,
        Vector3 fallbackPoint,
        Camera activeCamera)
    {
        Vector3 bestPoint = fallbackPoint;
        float bestOffset = float.MaxValue;

        ConsiderEvaluationPoint(fallbackPoint);

        if (hitCollider != null)
        {
            ConsiderEvaluationPoint(hitCollider.bounds.center);
            ConsiderEvaluationPoint(hitCollider.ClosestPoint(activeCamera != null ? activeCamera.transform.position : fallbackPoint));
        }

        Transform interactionTransform = interactable.GetInteractionTransform();
        if (interactionTransform != null)
        {
            ConsiderEvaluationPoint(interactionTransform.position);
        }

        return bestPoint;

        void ConsiderEvaluationPoint(Vector3 point)
        {
            if (!TryGetViewportOffset(activeCamera, point, out float offset))
            {
                return;
            }

            if (offset < bestOffset)
            {
                bestOffset = offset;
                bestPoint = point;
            }
        }
    }

    private float BuildCandidateScore(
        float rayDistance,
        float reachDistance,
        float viewportOffset,
        InteractionQuerySource source)
    {
        float normalizedViewport = assistViewportRadius > 0.0001f
            ? Mathf.Clamp01(viewportOffset / assistViewportRadius)
            : 0f;
        float normalizedRayDistance = interactDistance > 0.0001f
            ? Mathf.Clamp01(rayDistance / interactDistance)
            : 0f;
        float normalizedReachDistance = interactDistance + assistReachSlack > 0.0001f
            ? Mathf.Clamp01(reachDistance / (interactDistance + assistReachSlack))
            : 0f;

        float sourceBias = source == InteractionQuerySource.DirectCameraRay
            ? -1.25f
            : source == InteractionQuerySource.CameraSphereCast
                ? -0.25f
                : 0.15f;

        return normalizedViewport * normalizedViewport * 4f
             + normalizedRayDistance * 0.45f
             + normalizedReachDistance * 0.7f
             + sourceBias;
    }

    private bool TryGetViewportOffset(Camera activeCamera, Vector3 worldPoint, out float offset)
    {
        Camera camera = activeCamera != null ? activeCamera : InteractionCamera;
        if (camera == null)
        {
            offset = 0f;
            return true;
        }

        Vector3 viewportPoint = camera.WorldToViewportPoint(worldPoint);
        if (viewportPoint.z <= 0f)
        {
            offset = float.MaxValue;
            return false;
        }

        Vector2 delta = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);
        offset = delta.magnitude;
        return true;
    }

    private bool IsCandidateColliderValid(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        Transform hitTransform = hitCollider.transform;
        return hitTransform != transform && !hitTransform.IsChildOf(transform);
    }

    private static bool BlocksInteraction(Collider hitCollider)
    {
        return hitCollider != null && !hitCollider.isTrigger;
    }

    private void EnsureInventoryWindowController()
    {
        if (!autoAddInventoryWindowController)
        {
            return;
        }

        if (GetComponent<PlayerInventoryWindowController>() != null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            gameObject.AddComponent<PlayerInventoryWindowController>();
        }
    }

    private void EnsurePromptUi()
    {
        promptView ??= InteractionPromptView.GetOrCreate();
    }

    private void UpdatePromptUi()
    {
        if (!showPrompt)
        {
            SetPromptVisible(false);
            return;
        }

        EnsurePromptUi();
        if (!currentQuery.HasValue || (interactionState != null && interactionState.IsUiFocused))
        {
            SetPromptVisible(false);
            return;
        }

        IInteractable interactable = currentQuery.Interactable;
        if (interactable == null || !interactable.CanInteract(this))
        {
            SetPromptVisible(false);
            return;
        }

        string prompt = interactable.GetInteractionLabel(this);
        if (string.IsNullOrWhiteSpace(prompt))
        {
            SetPromptVisible(false);
            return;
        }

        string bindingDisplay = fpsInput != null ? fpsInput.GetBindingDisplayString("Interact") : string.Empty;
        string promptPrefix = string.IsNullOrWhiteSpace(bindingDisplay) ? "[E]" : $"[{bindingDisplay}]";
        promptView?.SetPrompt($"{promptPrefix} {prompt}", promptSize, true);
    }

    private void SetPromptVisible(bool visible)
    {
        promptView?.SetPrompt(string.Empty, promptSize, visible);
    }

    private static IInteractable ResolveInteractable(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        MonoBehaviour[] localComponents = hitCollider.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in localComponents)
        {
            if (component is IInteractable interactable)
            {
                return interactable;
            }
        }

        MonoBehaviour[] parentComponents = hitCollider.GetComponentsInParent<MonoBehaviour>(true);
        foreach (MonoBehaviour component in parentComponents)
        {
            if (component is IInteractable interactable)
            {
                return interactable;
            }
        }

        return null;
    }

    private static int CompareByDistance(RaycastHit left, RaycastHit right)
    {
        return left.distance.CompareTo(right.distance);
    }
}
