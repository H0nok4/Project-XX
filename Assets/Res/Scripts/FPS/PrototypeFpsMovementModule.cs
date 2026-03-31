using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController), typeof(PrototypeFpsInput))]
public class PrototypeFpsMovementModule : MonoBehaviour
{
    private const float StaminaEpsilon = 0.001f;

    [Header("References")]
    [Tooltip("第一人称视角摄像机，用于同步蹲起视角高度。")]
    [SerializeField] private Camera viewCamera;

    [Header("Movement")]
    [Tooltip("站立状态下、100%速度档位时的基础移动速度。")]
    [SerializeField] private float moveSpeed = 4f;
    [Tooltip("冲刺时相对基础移动速度的倍率。")]
    [SerializeField] private float sprintSpeedMultiplier = 1.33f;
    [Tooltip("冲刺每秒消耗的体力值。")]
    [SerializeField] private float sprintStaminaPerSecond = 21f;
    [Tooltip("每次起跳消耗的体力值。")]
    [SerializeField] private float jumpStaminaCost = 20f;
    [Tooltip("跳跃高度，数值越大跳得越高。")]
    [SerializeField] private float jumpHeight = 1.25f;
    [Tooltip("重力强度，绝对值越大下落越快。")]
    [SerializeField] private float gravity = -20f;
    [Tooltip("地面移动的基础加速度，决定起步和提速速度。")]
    [SerializeField] private float groundAcceleration = 16f;
    [Tooltip("没有输入时的地面减速强度，越大越容易停下。")]
    [SerializeField] private float groundFriction = 24f;
    [Tooltip("反向移动时的额外刹车强度，越大掉头越干脆。")]
    [SerializeField] private float directionChangeBrakeAcceleration = 32f;
    [Tooltip("开始判定为明显反向输入的方向点积阈值。")]
    [SerializeField, Range(-1f, 1f)] private float directionChangeBrakeDotThreshold = 0.2f;
    [Tooltip("静止起步时的加速倍率，越小越有沉重感。")]
    [SerializeField, Range(0.1f, 1f)] private float movementStartAccelerationMultiplier = 0.65f;
    [Tooltip("转向过程中的加速倍率，越小越不灵活。")]
    [SerializeField, Range(0.1f, 1f)] private float movementTurnAccelerationMultiplier = 0.3f;
    [Tooltip("后退移动时的速度倍率。")]
    [SerializeField, Range(0.1f, 1f)] private float backwardMoveSpeedMultiplier = 0.72f;
    [Tooltip("横移移动时的速度倍率。")]
    [SerializeField, Range(0.1f, 1f)] private float strafeMoveSpeedMultiplier = 0.8f;
    [Tooltip("松开移动输入后，向零速度收敛时的最小减速基线。")]
    [SerializeField] private float stopSpeed = 2.2f;
    [Tooltip("空中时对水平速度施加的阻尼，越大越快失去空中余速。")]
    [SerializeField] private float airPlanarDrag = 1.8f;
    [Tooltip("Scales air drag while carrying sprint jump momentum. Lower values preserve more speed.")]
    [SerializeField, Range(0.05f, 1f)] private float sprintJumpAirDragMultiplier = 0.35f;
    [Tooltip("冲刺状态下允许保留的横移倍率，用来限制横向飘移。")]
    [SerializeField, Range(0.1f, 1f)] private float sprintStrafeMultiplier = 0.35f;
    [Tooltip("落地后的短暂恢复时间，期间无法立刻满效率移动和冲刺。")]
    [SerializeField] private float landingRecoveryTime = 0.14f;
    [Tooltip("落地恢复期内的移动速度倍率。")]
    [SerializeField, Range(0.1f, 1f)] private float landingRecoveryMoveMultiplier = 0.72f;
    [Tooltip("冲刺停止时的额外前向刹车强度，用来压掉滑步感。")]
    [SerializeField] private float sprintStopBrakeAcceleration = 48f;
    [Tooltip("地面贴附力，防止轻微离地和下坡抖动。")]
    [SerializeField] private float groundedSnapForce = 2f;
    [SerializeField, HideInInspector] private float walkSpeedMultiplier = 0.48f;
    [Tooltip("当前选择的移动速度比例，LCtrl+滚轮调整的就是这个值。")]
    [SerializeField, Range(0.1f, 1f)] private float movementSpeedRatio = 1f;
    [Tooltip("允许调整到的最低移动速度比例。")]
    [SerializeField, Range(0.1f, 1f)] private float minMovementSpeedRatio = 0.1f;
    [Tooltip("每次滚轮调整移动速度比例时的步进。")]
    [SerializeField, Range(0.01f, 0.5f)] private float movementSpeedRatioStep = 0.1f;
    [Tooltip("蹲下状态相对站立基础速度的倍率。")]
    [SerializeField] private float crouchSpeedMultiplier = 0.58f;
    [Tooltip("蹲下时 CharacterController 的目标高度。")]
    [SerializeField] private float crouchHeight = 1.1f;
    [Tooltip("蹲下时摄像机向下偏移的距离。")]
    [SerializeField] private float crouchCameraDrop = 0.32f;
    [Tooltip("站立和蹲下之间切换的过渡速度。")]
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [Tooltip("蹲下时台阶跨越能力相对站立时的倍率。")]
    [SerializeField] private float crouchStepOffsetMultiplier = 0.45f;
    [Tooltip("冲刺起步加速到满冲刺速度所需的时间。")]
    [SerializeField] private float sprintAccelerationTime = 0.32f;
    [Tooltip("停止冲刺后从冲刺速度回落的时间。")]
    [SerializeField] private float sprintDecelerationTime = 0.08f;
    [Tooltip("检测头顶是否能站起时使用的碰撞层。")]
    [SerializeField] private LayerMask stanceObstructionMask = Physics.DefaultRaycastLayers;
    [Tooltip("站立空间检测时从控制器半径里扣掉的安全边距。")]
    [SerializeField] private float stanceClearancePadding = 0.04f;

    [Header("View Motion")]
    [Tooltip("是否启用移动时的轻微视角摇晃。")]
    [SerializeField] private bool headBobEnabled = true;
    [Tooltip("基础摇晃频率，数值越大步伐感越明显。")]
    [SerializeField] private float headBobFrequency = 7.5f;
    [Tooltip("垂直方向的轻微摇晃幅度。")]
    [SerializeField] private float headBobVerticalAmplitude = 0.014f;
    [Tooltip("水平方向的轻微摇晃幅度。")]
    [SerializeField] private float headBobHorizontalAmplitude = 0.008f;
    [Tooltip("开始出现摇晃所需的最小移动速度。")]
    [SerializeField] private float headBobActivationSpeed = 0.18f;
    [Tooltip("冲刺时对摇晃幅度施加的额外倍率。")]
    [SerializeField, Range(1f, 2f)] private float sprintHeadBobAmplitudeMultiplier = 1.22f;
    [Tooltip("冲刺时对摇晃频率施加的额外倍率。")]
    [SerializeField, Range(1f, 2f)] private float sprintHeadBobFrequencyMultiplier = 1.12f;
    [Tooltip("蹲姿时对摇晃幅度施加的倍率。")]
    [SerializeField, Range(0.1f, 1f)] private float crouchHeadBobAmplitudeMultiplier = 0.72f;
    [Tooltip("摇晃偏移追踪目标时的平滑速度。")]
    [SerializeField] private float headBobSmoothing = 14f;
    [Tooltip("停止移动后回正的平滑速度。")]
    [SerializeField] private float headBobResetSpeed = 10f;

    [Header("AI Awareness")]
    [Tooltip("普通步行时制造的基础噪声半径。")]
    [SerializeField] private float walkNoiseRadius = 4.8f;
    [Tooltip("冲刺时制造的基础噪声半径。")]
    [SerializeField] private float sprintNoiseRadius = 11.5f;
    [Tooltip("起跳瞬间制造的噪声半径。")]
    [SerializeField] private float jumpNoiseRadius = 7.5f;
    [Tooltip("落地瞬间制造的噪声半径。")]
    [SerializeField] private float landingNoiseRadius = 12f;
    [Tooltip("持续移动时汇报噪声的时间间隔。")]
    [SerializeField] private float movementNoiseInterval = 0.42f;
    [Tooltip("蹲下移动时对噪声半径施加的倍率。")]
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
    private Vector3 standingCameraLocalPosition;
    private float standingStepOffset;
    private Vector3 standingCenter;
    private float sprintSpeedBlend;
    private float headBobCycle;
    private bool crouchToggleRequested;
    private bool isCrouching;
    private bool isSprinting;
    private bool sprintMomentumCarryActive;
    private bool wasGroundedLastFrame;
    private bool isGrounded;
    private bool jumpTriggeredThisFrame;
    private bool landTriggeredThisFrame;
    private float nextMovementNoiseTime;
    private Vector2 headBobOffset;
    private Vector2 currentMoveInput;

    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;
    public bool IsGrounded => isGrounded;
    public bool JumpTriggeredThisFrame => jumpTriggeredThisFrame;
    public bool LandTriggeredThisFrame => landTriggeredThisFrame;
    public Vector3 PlanarVelocity => planarVelocity;
    public float PlanarSpeed => planarVelocity.magnitude;
    public float VerticalVelocity => verticalVelocity;
    public float SelectedMovementSpeedRatio => GetSelectedMovementSpeedRatio();
    public float SelectedStandingMoveSpeed => GetSelectedStandingMoveSpeed();
    public Vector2 CurrentMoveInput => currentMoveInput;

    private void Awake()
    {
        EnsureReferences();
        EnsureMovementSettings();
        CacheStanceDefaults();
        wasGroundedLastFrame = characterController != null && characterController.isGrounded;
        isGrounded = wasGroundedLastFrame;
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
        currentMoveInput = Vector2.zero;
        UpdateSprintSpeedBlend(Time.unscaledDeltaTime);
    }

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        EnsureReferences();

        planarVelocity = Vector3.zero;
        verticalVelocity = 0f;
        landingRecoveryTimer = 0f;
        sprintSpeedBlend = 0f;
        isSprinting = false;
        sprintMomentumCarryActive = false;
        headBobCycle = 0f;
        headBobOffset = Vector2.zero;
        currentMoveInput = Vector2.zero;
        jumpTriggeredThisFrame = false;
        landTriggeredThisFrame = false;

        if (characterController != null)
        {
            bool previousEnabled = characterController.enabled;
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = previousEnabled;
        }
        else
        {
            transform.SetPositionAndRotation(position, rotation);
        }

        wasGroundedLastFrame = characterController != null && characterController.isGrounded;
        isGrounded = wasGroundedLastFrame;
        UpdateViewCamera(0f, wasGroundedLastFrame, false);
    }

    public void TickMovement()
    {
        if (characterController == null || fpsInput == null || !fpsInput.IsReady)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        bool uiFocused = interactionState != null && interactionState.IsUiFocused;
        bool grounded = characterController.isGrounded;
        Vector2 moveInput = uiFocused ? Vector2.zero : Vector2.ClampMagnitude(fpsInput.Move, 1f);
        currentMoveInput = moveInput;
        jumpTriggeredThisFrame = false;
        landTriggeredThisFrame = false;
        isGrounded = grounded;

        if (uiFocused)
        {
            HandleUiFocus();
        }
        else
        {
            HandleMovementModeInput();
        }

        UpdateStance(deltaTime, grounded);

        if (landingRecoveryTimer > 0f)
        {
            landingRecoveryTimer = Mathf.Max(landingRecoveryTimer - deltaTime, 0f);
        }

        bool wasSprinting = isSprinting;
        isSprinting = CanSprint(moveInput, grounded, wasSprinting);
        if (isSprinting && grounded && playerVitals != null && sprintStaminaPerSecond > 0f)
        {
            float drained = playerVitals.DrainStamina(sprintStaminaPerSecond * deltaTime);
            isSprinting = drained > StaminaEpsilon;
        }

        UpdateSprintSpeedBlend(deltaTime);
        bool jumpedThisFrame = !uiFocused && TryStartJump(grounded, moveInput);
        jumpTriggeredThisFrame = jumpedThisFrame;

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
        landTriggeredThisFrame = landedThisFrame;
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

        UpdateViewCamera(deltaTime, groundedAfterMove, uiFocused);
        wasGroundedLastFrame = groundedAfterMove;
        isGrounded = groundedAfterMove;
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
        float targetStepOffset = isCrouching
            ? standingStepOffset * crouchStepOffsetMultiplier
            : standingStepOffset;
        float nextHeight = Mathf.MoveTowards(characterController.height, targetHeight, crouchTransitionSpeed * deltaTime);
        characterController.height = nextHeight;
        characterController.center = GetControllerCenterForHeight(nextHeight);
        characterController.stepOffset = grounded ? targetStepOffset : 0f;
    }

    private void UpdateViewCamera(float deltaTime, bool grounded, bool uiFocused)
    {
        if (viewCamera == null)
        {
            return;
        }

        bool bobActive = UpdateHeadBob(deltaTime, grounded, uiFocused);
        Vector3 targetCameraPosition = standingCameraLocalPosition;
        targetCameraPosition.y = isCrouching
            ? standingCameraLocalY - crouchCameraDrop + headBobOffset.y
            : standingCameraLocalY + headBobOffset.y;
        targetCameraPosition.x += headBobOffset.x;

        if (deltaTime <= 0f)
        {
            viewCamera.transform.localPosition = targetCameraPosition;
            return;
        }

        Vector3 cameraLocalPosition = viewCamera.transform.localPosition;
        float lateralSmoothing = bobActive ? headBobSmoothing : headBobResetSpeed;
        float lateralBlend = 1f - Mathf.Exp(-lateralSmoothing * deltaTime);
        cameraLocalPosition.x = Mathf.Lerp(cameraLocalPosition.x, targetCameraPosition.x, lateralBlend);
        cameraLocalPosition.y = Mathf.MoveTowards(
            cameraLocalPosition.y,
            targetCameraPosition.y,
            Mathf.Max(crouchTransitionSpeed, headBobSmoothing) * deltaTime);
        cameraLocalPosition.z = Mathf.Lerp(cameraLocalPosition.z, targetCameraPosition.z, lateralBlend);
        viewCamera.transform.localPosition = cameraLocalPosition;
    }

    private bool UpdateHeadBob(float deltaTime, bool grounded, bool uiFocused)
    {
        if (!headBobEnabled || viewCamera == null)
        {
            headBobOffset = Vector2.zero;
            headBobCycle = 0f;
            return false;
        }

        float currentPlanarSpeed = planarVelocity.magnitude;
        float bobTopSpeed = isSprinting
            ? moveSpeed * sprintSpeedMultiplier
            : Mathf.Max(headBobActivationSpeed + 0.01f, GetTargetMoveSpeed());
        float bobStrength = grounded && !uiFocused
            ? Mathf.InverseLerp(headBobActivationSpeed, bobTopSpeed, currentPlanarSpeed)
            : 0f;
        bool bobActive = bobStrength > 0.001f;
        Vector2 targetBobOffset = Vector2.zero;

        if (bobActive)
        {
            float amplitudeMultiplier = bobStrength;
            if (isSprinting)
            {
                amplitudeMultiplier *= sprintHeadBobAmplitudeMultiplier;
            }
            else if (isCrouching)
            {
                amplitudeMultiplier *= crouchHeadBobAmplitudeMultiplier;
            }

            float frequencyMultiplier = Mathf.Lerp(0.7f, 1f, bobStrength);
            if (isSprinting)
            {
                frequencyMultiplier *= sprintHeadBobFrequencyMultiplier;
            }

            headBobCycle += deltaTime * headBobFrequency * frequencyMultiplier;
            float horizontalBob = Mathf.Sin(headBobCycle) * headBobHorizontalAmplitude * amplitudeMultiplier;
            float verticalBob = Mathf.Sin(headBobCycle * 2f) * headBobVerticalAmplitude * amplitudeMultiplier;
            targetBobOffset = new Vector2(horizontalBob, verticalBob);
        }

        float smoothing = bobActive ? headBobSmoothing : headBobResetSpeed;
        float blend = deltaTime > 0f ? 1f - Mathf.Exp(-smoothing * deltaTime) : 1f;
        headBobOffset = Vector2.Lerp(headBobOffset, targetBobOffset, blend);
        if (!bobActive && headBobOffset.sqrMagnitude <= 0.000001f)
        {
            headBobOffset = Vector2.zero;
            headBobCycle = 0f;
        }

        return bobActive;
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

    private bool TryStartJump(bool grounded, Vector2 moveInput)
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

        bool shouldCarrySprintMomentum = isSprinting
            && fpsInput.SprintHeld
            && moveInput.y >= 0.35f
            && moveInput.sqrMagnitude > 0.01f;
        sprintMomentumCarryActive = shouldCarrySprintMomentum;
        if (shouldCarrySprintMomentum)
        {
            PreserveSprintJumpTakeoffSpeed(moveInput);
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

        float effectiveAirPlanarDrag = airPlanarDrag;
        if (sprintMomentumCarryActive)
        {
            effectiveAirPlanarDrag *= sprintJumpAirDragMultiplier;
        }

        float dragFactor = Mathf.Clamp01(effectiveAirPlanarDrag * deltaTime);
        planarVelocity *= 1f - dragFactor;
    }

    private void PreserveSprintJumpTakeoffSpeed(Vector2 moveInput)
    {
        Vector3 desiredTakeoffVelocity = GetDesiredGroundPlanarVelocity(moveInput);
        if (desiredTakeoffVelocity.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3 desiredDirection = desiredTakeoffVelocity.normalized;
        float desiredSpeed = desiredTakeoffVelocity.magnitude;
        float currentSpeedInDesiredDirection = Vector3.Dot(planarVelocity, desiredDirection);
        if (currentSpeedInDesiredDirection >= desiredSpeed)
        {
            return;
        }

        planarVelocity += desiredDirection * (desiredSpeed - currentSpeedInDesiredDirection);
        planarVelocity.y = 0f;
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
        if (ShouldCarrySprintMomentum(moveInput, grounded))
        {
            return true;
        }

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

    private bool ShouldCarrySprintMomentum(Vector2 moveInput, bool grounded)
    {
        if (!sprintMomentumCarryActive)
        {
            return false;
        }

        if (fpsInput == null
            || isCrouching
            || !fpsInput.SprintHeld
            || moveInput.y < 0.35f
            || moveInput.sqrMagnitude <= 0.01f)
        {
            sprintMomentumCarryActive = false;
            return false;
        }

        if (grounded && landingRecoveryTimer <= 0f)
        {
            sprintMomentumCarryActive = false;
            return false;
        }

        return true;
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
            PlayerAnimationRigRefs rigRefs = GetComponent<PlayerAnimationRigRefs>();
            viewCamera = rigRefs != null ? rigRefs.ViewCamera : GetComponentInChildren<Camera>();
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
        if (viewCamera == null)
        {
            PlayerAnimationRigRefs rigRefs = GetComponent<PlayerAnimationRigRefs>();
            viewCamera = rigRefs != null ? rigRefs.ViewCamera : GetComponentInChildren<Camera>();
        }

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
        sprintJumpAirDragMultiplier = Mathf.Clamp(sprintJumpAirDragMultiplier, 0.05f, 1f);
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
        headBobFrequency = Mathf.Max(0f, headBobFrequency);
        headBobVerticalAmplitude = Mathf.Max(0f, headBobVerticalAmplitude);
        headBobHorizontalAmplitude = Mathf.Max(0f, headBobHorizontalAmplitude);
        headBobActivationSpeed = Mathf.Max(0.01f, headBobActivationSpeed);
        sprintHeadBobAmplitudeMultiplier = Mathf.Clamp(sprintHeadBobAmplitudeMultiplier, 1f, 2f);
        sprintHeadBobFrequencyMultiplier = Mathf.Clamp(sprintHeadBobFrequencyMultiplier, 1f, 2f);
        crouchHeadBobAmplitudeMultiplier = Mathf.Clamp(crouchHeadBobAmplitudeMultiplier, 0.1f, 1f);
        headBobSmoothing = Mathf.Max(0.1f, headBobSmoothing);
        headBobResetSpeed = Mathf.Max(0.1f, headBobResetSpeed);
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
            standingCameraLocalPosition = viewCamera.transform.localPosition;
            standingCameraLocalY = viewCamera.transform.localPosition.y;
        }
        else if (standingCameraLocalY <= 0f)
        {
            standingCameraLocalPosition = new Vector3(0f, 0.72f, 0f);
            standingCameraLocalY = 0.72f;
        }
    }
}
