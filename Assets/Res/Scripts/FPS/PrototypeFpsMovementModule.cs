using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController), typeof(PrototypeFpsInput))]
public class PrototypeFpsMovementModule : MonoBehaviour
{
    private const float StaminaEpsilon = 0.001f;

    [Header("References")]
    [SerializeField] private Camera viewCamera;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.4f;
    [SerializeField] private float sprintSpeedMultiplier = 1.65f;
    [SerializeField] private float sprintStaminaPerSecond = 21f;
    [SerializeField] private float jumpStaminaCost = 15f;
    [SerializeField] private float jumpHeight = 1.25f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundAcceleration = 30f;
    [SerializeField] private float groundFriction = 8f;
    [SerializeField] private float directionChangeBrakeAcceleration = 64f;
    [SerializeField, Range(-1f, 1f)] private float directionChangeBrakeDotThreshold = 0.2f;
    [SerializeField, Range(0.1f, 1f)] private float movementStartAccelerationMultiplier = 0.65f;
    [SerializeField, Range(0.1f, 1f)] private float movementTurnAccelerationMultiplier = 0.3f;
    [SerializeField, Range(0.1f, 1f)] private float backwardMoveSpeedMultiplier = 0.72f;
    [SerializeField, Range(0.1f, 1f)] private float strafeMoveSpeedMultiplier = 0.8f;
    [SerializeField] private float stopSpeed = 2.2f;
    [SerializeField] private float airPlanarDrag = 1.8f;
    [SerializeField, Range(0.1f, 1f)] private float sprintStrafeMultiplier = 0.35f;
    [SerializeField] private float landingRecoveryTime = 0.14f;
    [SerializeField, Range(0.1f, 1f)] private float landingRecoveryMoveMultiplier = 0.72f;
    [SerializeField] private float sprintStopBrakeAcceleration = 48f;
    [SerializeField] private float groundedSnapForce = 2f;
    [SerializeField, HideInInspector] private float walkSpeedMultiplier = 0.48f;
    [SerializeField, Range(0.1f, 1f)] private float movementSpeedRatio = 1f;
    [SerializeField, Range(0.1f, 1f)] private float minMovementSpeedRatio = 0.1f;
    [SerializeField, Range(0.01f, 0.5f)] private float movementSpeedRatioStep = 0.1f;
    [SerializeField] private float crouchSpeedMultiplier = 0.58f;
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float crouchCameraDrop = 0.32f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float crouchStepOffsetMultiplier = 0.45f;
    [SerializeField] private float sprintAccelerationTime = 0.32f;
    [SerializeField] private float sprintDecelerationTime = 0.08f;
    [SerializeField] private LayerMask stanceObstructionMask = Physics.DefaultRaycastLayers;
    [SerializeField] private float stanceClearancePadding = 0.04f;

    [Header("AI Awareness")]
    [SerializeField] private float walkNoiseRadius = 4.8f;
    [SerializeField] private float sprintNoiseRadius = 11.5f;
    [SerializeField] private float jumpNoiseRadius = 7.5f;
    [SerializeField] private float landingNoiseRadius = 12f;
    [SerializeField] private float movementNoiseInterval = 0.42f;
    [SerializeField, Range(0.1f, 1f)] private float crouchNoiseMultiplier = 0.45f;

    private CharacterController characterController;
    private PrototypeFpsInput fpsInput;
    private PlayerInteractionState interactionState;
    private PrototypeUnitVitals playerVitals;
    private Vector3 planarVelocity;
    private float verticalVelocity;
    private float landingRecoveryTimer;
    private float standingHeight;
    private float standingCameraLocalY;
    private float standingStepOffset;
    private Vector3 standingCenter;
    private float sprintSpeedBlend;
    private bool crouchToggleRequested;
    private bool isCrouching;
    private bool isSprinting;
    private bool wasGroundedLastFrame;
    private float nextMovementNoiseTime;

    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;
    public Vector3 PlanarVelocity => planarVelocity;
    public float PlanarSpeed => planarVelocity.magnitude;
    public float SelectedMovementSpeedRatio => GetSelectedMovementSpeedRatio();
    public float SelectedStandingMoveSpeed => GetSelectedStandingMoveSpeed();

    private void Awake()
    {
        EnsureReferences();
        EnsureMovementSettings();
        CacheStanceDefaults();
        wasGroundedLastFrame = characterController != null && characterController.isGrounded;
    }

    public void SetViewCamera(Camera camera)
    {
        if (camera == null)
        {
            return;
        }

        viewCamera = camera;
        CacheStanceDefaults();
    }

    public void HandleUiFocus()
    {
        isSprinting = false;
        UpdateSprintSpeedBlend(Time.unscaledDeltaTime);
    }

    public void TickMovement()
    {
        if (characterController == null || fpsInput == null || !fpsInput.IsReady)
        {
            return;
        }

        if (interactionState != null && interactionState.IsUiFocused)
        {
            HandleUiFocus();
            return;
        }

        float deltaTime = Time.deltaTime;
        bool grounded = characterController.isGrounded;
        Vector2 moveInput = Vector2.ClampMagnitude(fpsInput.Move, 1f);

        HandleMovementModeInput();
        UpdateStance(deltaTime, grounded);

        if (landingRecoveryTimer > 0f)
        {
            landingRecoveryTimer = Mathf.Max(landingRecoveryTimer - deltaTime, 0f);
        }

        bool wasSprinting = isSprinting;
        isSprinting = CanSprint(moveInput, grounded, wasSprinting);
        if (isSprinting && playerVitals != null && sprintStaminaPerSecond > 0f)
        {
            float drained = playerVitals.DrainStamina(sprintStaminaPerSecond * deltaTime);
            isSprinting = drained > StaminaEpsilon;
        }

        UpdateSprintSpeedBlend(deltaTime);
        bool jumpedThisFrame = TryStartJump(grounded);

        if (grounded && !jumpedThisFrame)
        {
            Vector3 desiredPlanarVelocity = GetDesiredGroundPlanarVelocity(moveInput);
            planarVelocity = UpdateGroundPlanarVelocity(desiredPlanarVelocity, deltaTime);

            if (verticalVelocity < 0f)
            {
                verticalVelocity = -groundedSnapForce;
            }
        }
        else
        {
            ApplyAirPlanarDrag(deltaTime);
        }

        if (!grounded || jumpedThisFrame)
        {
            verticalVelocity += gravity * deltaTime;
        }

        Vector3 velocity = planarVelocity + Vector3.up * verticalVelocity;
        CollisionFlags collisionFlags = characterController.Move(velocity * deltaTime);
        bool groundedAfterMove = (collisionFlags & CollisionFlags.Below) != 0 || characterController.isGrounded;

        if (groundedAfterMove && verticalVelocity < 0f)
        {
            verticalVelocity = -groundedSnapForce;
        }

        bool landedThisFrame = !wasGroundedLastFrame && groundedAfterMove;
        if (landedThisFrame)
        {
            landingRecoveryTimer = landingRecoveryTime;
            float landingRadius = landingNoiseRadius + Mathf.Min(planarVelocity.magnitude * 0.45f, 4f);
            if (isCrouching)
            {
                landingRadius *= crouchNoiseMultiplier;
            }

            ReportNoise(landingRadius);
            nextMovementNoiseTime = Time.time + movementNoiseInterval * 0.75f;
        }

        if (groundedAfterMove && !landedThisFrame)
        {
            ReportMovementNoise();
        }

        wasGroundedLastFrame = groundedAfterMove;
    }

    private void HandleMovementModeInput()
    {
        if (fpsInput == null)
        {
            return;
        }

        if (fpsInput.ToggleCrouchPressedThisFrame)
        {
            crouchToggleRequested = !crouchToggleRequested;
        }

        if (fpsInput.SpeedAdjustModifierHeld)
        {
            float scrollDelta = fpsInput.MoveSpeedScrollDelta;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                AdjustMovementSpeedRatio(scrollDelta > 0f ? 1 : -1);
            }
        }

        if (fpsInput.SprintHeld)
        {
            movementSpeedRatio = 1f;
            crouchToggleRequested = false;
        }
    }

    private void UpdateStance(float deltaTime, bool grounded)
    {
        bool canStand = CanOccupyHeight(standingHeight);
        isCrouching = crouchToggleRequested || !canStand;

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float targetCameraY = isCrouching
            ? standingCameraLocalY - crouchCameraDrop
            : standingCameraLocalY;
        float targetStepOffset = isCrouching
            ? standingStepOffset * crouchStepOffsetMultiplier
            : standingStepOffset;
        float nextHeight = Mathf.MoveTowards(characterController.height, targetHeight, crouchTransitionSpeed * deltaTime);
        characterController.height = nextHeight;
        characterController.center = GetControllerCenterForHeight(nextHeight);
        characterController.stepOffset = grounded ? targetStepOffset : 0f;

        if (viewCamera != null)
        {
            Vector3 cameraLocalPosition = viewCamera.transform.localPosition;
            cameraLocalPosition.y = Mathf.MoveTowards(cameraLocalPosition.y, targetCameraY, crouchTransitionSpeed * deltaTime);
            viewCamera.transform.localPosition = cameraLocalPosition;
        }
    }

    private float GetTargetMoveSpeed()
    {
        float selectedStandingSpeed = moveSpeed * GetSelectedMovementSpeedRatio();
        float baseStanceSpeed = isCrouching
            ? selectedStandingSpeed * crouchSpeedMultiplier
            : selectedStandingSpeed;
        float sprintTargetSpeed = moveSpeed * sprintSpeedMultiplier;
        float targetSpeed = Mathf.Lerp(baseStanceSpeed, sprintTargetSpeed, sprintSpeedBlend);

        if (playerVitals != null)
        {
            targetSpeed *= playerVitals.MovementPenaltyMultiplier;
        }

        return targetSpeed;
    }

    private bool CanOccupyHeight(float targetHeight)
    {
        if (characterController == null)
        {
            return true;
        }

        float radius = Mathf.Max(0.01f, characterController.radius - stanceClearancePadding);
        float halfHeight = Mathf.Max(targetHeight * 0.5f, radius + 0.01f);
        Vector3 targetCenter = GetControllerCenterForHeight(targetHeight);
        Vector3 worldCenter = transform.TransformPoint(targetCenter);
        Vector3 capsuleTop = worldCenter + Vector3.up * (halfHeight - radius);
        Vector3 capsuleBottom = worldCenter - Vector3.up * (halfHeight - radius);

        return !Physics.CheckCapsule(
            capsuleBottom,
            capsuleTop,
            radius,
            stanceObstructionMask,
            QueryTriggerInteraction.Ignore);
    }

    private Vector3 GetControllerCenterForHeight(float targetHeight)
    {
        float heightOffset = standingHeight - targetHeight;
        Vector3 targetCenter = standingCenter;
        targetCenter.y = standingCenter.y - heightOffset * 0.5f;
        return targetCenter;
    }

    private Vector3 GetDesiredGroundPlanarVelocity(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        float targetMoveSpeed = GetTargetMoveSpeed();
        float directionalMultiplier = GetDirectionalSpeedMultiplier(moveInput, Mathf.Abs(moveInput.x));
        float scaledMoveSpeed = targetMoveSpeed * directionalMultiplier;
        if (landingRecoveryTimer > 0f)
        {
            scaledMoveSpeed *= landingRecoveryMoveMultiplier;
        }

        Vector2 adjustedInput = moveInput;
        if (isSprinting)
        {
            adjustedInput.x *= sprintStrafeMultiplier;
            adjustedInput.y = Mathf.Max(adjustedInput.y, 0f);
        }

        Vector3 localVelocity = new Vector3(
            adjustedInput.x * scaledMoveSpeed,
            0f,
            adjustedInput.y * scaledMoveSpeed);
        return transform.TransformDirection(localVelocity);
    }

    private Vector3 UpdateGroundPlanarVelocity(Vector3 desiredPlanarVelocity, float deltaTime)
    {
        if (desiredPlanarVelocity.sqrMagnitude <= 0.0001f)
        {
            return Vector3.MoveTowards(planarVelocity, Vector3.zero, groundFriction * deltaTime);
        }

        Vector3 currentLocalVelocity = transform.InverseTransformDirection(planarVelocity);
        currentLocalVelocity.y = 0f;
        Vector3 desiredLocalVelocity = transform.InverseTransformDirection(desiredPlanarVelocity);
        desiredLocalVelocity.y = 0f;

        float baseAcceleration = groundAcceleration * GetGroundAccelerationMultiplier(desiredPlanarVelocity);
        float forwardAcceleration = baseAcceleration;
        float lateralAcceleration = baseAcceleration * 0.9f;
        if (isSprinting)
        {
            forwardAcceleration *= 0.8f;
            lateralAcceleration *= sprintStrafeMultiplier;
        }

        float trackingDeceleration = Mathf.Max(groundFriction, baseAcceleration * 0.85f);
        float forwardDeceleration = trackingDeceleration;
        if (!isSprinting && currentLocalVelocity.z > desiredLocalVelocity.z + 0.05f)
        {
            forwardDeceleration = Mathf.Max(forwardDeceleration, sprintStopBrakeAcceleration);
        }

        currentLocalVelocity.z = MovePlanarVelocityAxis(
            currentLocalVelocity.z,
            desiredLocalVelocity.z,
            forwardAcceleration,
            forwardDeceleration,
            deltaTime);
        currentLocalVelocity.x = MovePlanarVelocityAxis(
            currentLocalVelocity.x,
            desiredLocalVelocity.x,
            lateralAcceleration,
            trackingDeceleration,
            deltaTime);

        Vector3 nextPlanarVelocity = transform.TransformDirection(currentLocalVelocity);
        nextPlanarVelocity.y = 0f;
        return nextPlanarVelocity;
    }

    private float MovePlanarVelocityAxis(
        float current,
        float desired,
        float acceleration,
        float deceleration,
        float deltaTime)
    {
        if (Mathf.Abs(desired) <= 0.01f)
        {
            return Mathf.MoveTowards(current, 0f, deceleration * deltaTime);
        }

        if (Mathf.Abs(current) > 0.01f && Mathf.Sign(current) != Mathf.Sign(desired))
        {
            return Mathf.MoveTowards(current, 0f, directionChangeBrakeAcceleration * deltaTime);
        }

        float responseRate = Mathf.Abs(desired) > Mathf.Abs(current)
            ? acceleration
            : deceleration;
        return Mathf.MoveTowards(current, desired, responseRate * deltaTime);
    }

    private bool TryStartJump(bool grounded)
    {
        if (!grounded || fpsInput == null || !fpsInput.JumpPressedThisFrame)
        {
            return false;
        }

        float effectiveJumpHeight = jumpHeight;
        if (playerVitals != null)
        {
            effectiveJumpHeight *= playerVitals.JumpPenaltyMultiplier;
            if (jumpStaminaCost > 0f && (!playerVitals.CanStartStaminaAction(jumpStaminaCost) || !playerVitals.TryConsumeStamina(jumpStaminaCost)))
            {
                return false;
            }
        }

        verticalVelocity = Mathf.Sqrt(effectiveJumpHeight * -2f * gravity);
        ReportNoise(isCrouching ? jumpNoiseRadius * crouchNoiseMultiplier : jumpNoiseRadius);
        return true;
    }

    private void ApplyAirPlanarDrag(float deltaTime)
    {
        if (planarVelocity.sqrMagnitude <= 0.0001f || airPlanarDrag <= 0f)
        {
            return;
        }

        float dragFactor = Mathf.Clamp01(airPlanarDrag * deltaTime);
        planarVelocity *= 1f - dragFactor;
    }

    private float GetDirectionalSpeedMultiplier(Vector2 moveInput, float allowedSideInput)
    {
        float directionalMultiplier = 1f;

        if (moveInput.y < -0.01f)
        {
            directionalMultiplier *= Mathf.Lerp(1f, backwardMoveSpeedMultiplier, Mathf.Clamp01(Mathf.Abs(moveInput.y)));
        }

        directionalMultiplier *= Mathf.Lerp(1f, strafeMoveSpeedMultiplier, Mathf.Clamp01(allowedSideInput));
        return directionalMultiplier;
    }

    private float GetGroundAccelerationMultiplier(Vector3 wishDirection)
    {
        if (wishDirection.sqrMagnitude <= 0f)
        {
            return 1f;
        }

        if (planarVelocity.sqrMagnitude <= 1f)
        {
            return movementStartAccelerationMultiplier;
        }

        float directionDot = Vector3.Dot(planarVelocity.normalized, wishDirection);
        if (directionDot >= 0.98f)
        {
            return 1f;
        }

        float turnBlend = Mathf.InverseLerp(directionChangeBrakeDotThreshold, 0.98f, directionDot);
        return Mathf.Lerp(movementTurnAccelerationMultiplier, 1f, turnBlend);
    }

    private void UpdateSprintSpeedBlend(float deltaTime)
    {
        float targetBlend = isSprinting ? 1f : 0f;
        float blendDuration = targetBlend > sprintSpeedBlend
            ? sprintAccelerationTime
            : sprintDecelerationTime;
        float blendSpeed = blendDuration > 0.001f ? 1f / blendDuration : float.PositiveInfinity;
        sprintSpeedBlend = Mathf.MoveTowards(sprintSpeedBlend, targetBlend, blendSpeed * deltaTime);
    }

    private bool CanSprint(Vector2 moveInput, bool grounded, bool wasSprinting)
    {
        if (!grounded
            || isCrouching
            || fpsInput == null
            || !fpsInput.SprintHeld
            || moveInput.y < 0.35f
            || moveInput.sqrMagnitude <= 0.01f
            || landingRecoveryTimer > 0f)
        {
            return false;
        }

        if (playerVitals == null)
        {
            return true;
        }

        if (playerVitals.CurrentStamina <= StaminaEpsilon)
        {
            return false;
        }

        return wasSprinting || playerVitals.CanStartStaminaAction();
    }

    private void AdjustMovementSpeedRatio(int stepDirection)
    {
        if (stepDirection == 0)
        {
            return;
        }

        float stepSize = Mathf.Max(0.01f, movementSpeedRatioStep);
        float minRatio = Mathf.Clamp(minMovementSpeedRatio, 0.1f, 1f);
        int minStep = Mathf.Max(1, Mathf.RoundToInt(minRatio / stepSize));
        int maxStep = Mathf.Max(minStep, Mathf.RoundToInt(1f / stepSize));
        int currentStep = Mathf.Clamp(Mathf.RoundToInt(GetSelectedMovementSpeedRatio() / stepSize), minStep, maxStep);
        currentStep = Mathf.Clamp(currentStep + stepDirection, minStep, maxStep);
        movementSpeedRatio = Mathf.Clamp(currentStep * stepSize, minRatio, 1f);
    }

    private float GetSelectedMovementSpeedRatio()
    {
        return Mathf.Clamp(movementSpeedRatio, Mathf.Clamp(minMovementSpeedRatio, 0.1f, 1f), 1f);
    }

    private float GetSelectedStandingMoveSpeed()
    {
        return moveSpeed * GetSelectedMovementSpeedRatio();
    }

    private void ReportMovementNoise()
    {
        if (Time.time < nextMovementNoiseTime)
        {
            return;
        }

        float movementSpeed = planarVelocity.magnitude;
        if (movementSpeed <= 0.65f)
        {
            return;
        }

        float selectedTopSpeed = isSprinting
            ? moveSpeed * sprintSpeedMultiplier
            : Mathf.Max(0.65f, isCrouching ? GetSelectedStandingMoveSpeed() * crouchSpeedMultiplier : GetSelectedStandingMoveSpeed());
        float selectedSpeedFactor = isSprinting
            ? 1f
            : Mathf.InverseLerp(moveSpeed * minMovementSpeedRatio, moveSpeed, GetSelectedStandingMoveSpeed());
        float speedFactor = Mathf.InverseLerp(0.65f, selectedTopSpeed, movementSpeed);
        float noiseRadius = isSprinting
            ? sprintNoiseRadius
            : Mathf.Lerp(walkNoiseRadius * 0.2f, walkNoiseRadius, selectedSpeedFactor);
        noiseRadius = isSprinting
            ? noiseRadius
            : Mathf.Lerp(noiseRadius * 0.68f, noiseRadius, speedFactor);
        if (isCrouching)
        {
            noiseRadius *= crouchNoiseMultiplier;
        }

        ReportNoise(noiseRadius);
        float intervalScale = isSprinting
            ? 0.72f
            : Mathf.Lerp(1.35f, 1f, selectedSpeedFactor);
        nextMovementNoiseTime = Time.time + movementNoiseInterval * intervalScale;
    }

    private void ReportNoise(float radius)
    {
        if (radius <= 0f)
        {
            return;
        }

        PrototypeCombatNoiseSystem.ReportNoise(transform.position + Vector3.up * 0.9f, radius, gameObject);
    }

    private void OnValidate()
    {
        EnsureReferences();
        EnsureMovementSettings();

        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }

#if UNITY_EDITOR
        if (!Application.isPlaying && fpsInput == null)
        {
            fpsInput = gameObject.AddComponent<PrototypeFpsInput>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        if (!Application.isPlaying && interactionState == null)
        {
            interactionState = gameObject.AddComponent<PlayerInteractionState>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif

        CacheStanceDefaults();
    }

    private void EnsureReferences()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (fpsInput == null)
        {
            fpsInput = GetComponent<PrototypeFpsInput>();
        }

        if (interactionState == null)
        {
            interactionState = GetComponent<PlayerInteractionState>();
        }

        if (playerVitals == null)
        {
            playerVitals = GetComponent<PrototypeUnitVitals>();
        }
    }

    private void EnsureMovementSettings()
    {
        moveSpeed = Mathf.Max(moveSpeed, 0.1f);
        sprintSpeedMultiplier = Mathf.Max(1f, sprintSpeedMultiplier);
        sprintStaminaPerSecond = Mathf.Max(0f, sprintStaminaPerSecond);
        jumpStaminaCost = Mathf.Max(0f, jumpStaminaCost);
        jumpHeight = Mathf.Max(jumpHeight, 0.1f);
        gravity = gravity < -0.1f ? gravity : -20f;
        groundAcceleration = groundAcceleration > 0f ? groundAcceleration : 30f;
        groundFriction = groundFriction > 0f ? groundFriction : 8f;
        directionChangeBrakeAcceleration = Mathf.Max(0f, directionChangeBrakeAcceleration);
        directionChangeBrakeDotThreshold = Mathf.Clamp(directionChangeBrakeDotThreshold, -1f, 1f);
        movementStartAccelerationMultiplier = Mathf.Clamp(movementStartAccelerationMultiplier, 0.1f, 1f);
        movementTurnAccelerationMultiplier = Mathf.Clamp(movementTurnAccelerationMultiplier, 0.1f, 1f);
        backwardMoveSpeedMultiplier = Mathf.Clamp(backwardMoveSpeedMultiplier, 0.1f, 1f);
        strafeMoveSpeedMultiplier = Mathf.Clamp(strafeMoveSpeedMultiplier, 0.1f, 1f);
        stopSpeed = stopSpeed > 0f ? stopSpeed : 2.2f;
        airPlanarDrag = Mathf.Max(0f, airPlanarDrag);
        sprintStrafeMultiplier = Mathf.Clamp(sprintStrafeMultiplier, 0.1f, 1f);
        landingRecoveryTime = Mathf.Max(0f, landingRecoveryTime);
        landingRecoveryMoveMultiplier = Mathf.Clamp(landingRecoveryMoveMultiplier, 0.1f, 1f);
        sprintStopBrakeAcceleration = Mathf.Max(groundFriction, sprintStopBrakeAcceleration);
        groundedSnapForce = groundedSnapForce > 0f ? groundedSnapForce : 2f;
        walkSpeedMultiplier = Mathf.Clamp(walkSpeedMultiplier, 0.1f, 1f);
        minMovementSpeedRatio = Mathf.Clamp(minMovementSpeedRatio, 0.1f, 1f);
        movementSpeedRatioStep = Mathf.Clamp(movementSpeedRatioStep, 0.01f, 0.5f);
        movementSpeedRatio = Mathf.Clamp(movementSpeedRatio, minMovementSpeedRatio, 1f);
        crouchSpeedMultiplier = Mathf.Clamp(crouchSpeedMultiplier, 0.1f, 1f);
        crouchHeight = Mathf.Max(crouchHeight, 0.8f);
        crouchCameraDrop = Mathf.Max(crouchCameraDrop, 0.05f);
        crouchTransitionSpeed = Mathf.Max(crouchTransitionSpeed, 0.1f);
        crouchStepOffsetMultiplier = Mathf.Clamp(crouchStepOffsetMultiplier, 0f, 1f);
        sprintAccelerationTime = Mathf.Max(0.01f, sprintAccelerationTime);
        sprintDecelerationTime = Mathf.Max(0.01f, sprintDecelerationTime);
        stanceClearancePadding = Mathf.Clamp(stanceClearancePadding, 0f, 0.2f);
        walkNoiseRadius = Mathf.Max(0f, walkNoiseRadius);
        sprintNoiseRadius = Mathf.Max(walkNoiseRadius, sprintNoiseRadius);
        jumpNoiseRadius = Mathf.Max(0f, jumpNoiseRadius);
        landingNoiseRadius = Mathf.Max(0f, landingNoiseRadius);
        movementNoiseInterval = Mathf.Max(0.05f, movementNoiseInterval);
        crouchNoiseMultiplier = Mathf.Clamp(crouchNoiseMultiplier, 0.1f, 1f);

        if (stanceObstructionMask.value == 0 || stanceObstructionMask.value == ~0)
        {
            stanceObstructionMask = Physics.DefaultRaycastLayers;
        }
    }

    private void CacheStanceDefaults()
    {
        if (characterController != null)
        {
            standingHeight = characterController.height > 0f ? characterController.height : 1.8f;
            standingStepOffset = characterController.stepOffset > 0f ? characterController.stepOffset : 0.3f;
            standingCenter = characterController.center;
        }
        else
        {
            standingHeight = 1.8f;
            standingStepOffset = 0.3f;
            standingCenter = new Vector3(0f, standingHeight * 0.5f, 0f);
        }

        if (viewCamera != null)
        {
            standingCameraLocalY = viewCamera.transform.localPosition.y;
        }
        else if (standingCameraLocalY <= 0f)
        {
            standingCameraLocalY = 0.72f;
        }
    }
}
