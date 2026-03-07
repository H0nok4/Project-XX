using System;
using System.Text;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PrototypeFpsInput))]
public class PrototypeFpsController : MonoBehaviour
{
    private const float StaminaEpsilon = 0.001f;

    private enum WeaponSlot
    {
        Primary = 0,
        Secondary = 1,
        Melee = 2
    }

    private sealed class WeaponRuntime
    {
        public WeaponSlot Slot;
        public PrototypeWeaponDefinition Definition;
        public int MagazineAmmo;
        public int FireModeIndex;
        public int PendingBurstShots;
        public float NextAttackTime;
        public float ReloadEndTime;
        public bool IsReloading;

        public bool IsConfigured => Definition != null;
        public PrototypeWeaponFireMode CurrentFireMode => Definition != null
            ? Definition.GetFireMode(FireModeIndex)
            : PrototypeWeaponFireMode.Semi;
    }

    [Header("References")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform primaryViewModel;
    [SerializeField] private Transform secondaryViewModel;
    [SerializeField] private Transform meleeViewModel;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.4f;
    [SerializeField] private float sprintSpeedMultiplier = 1.65f;
    [SerializeField] private float sprintStaminaPerSecond = 21f;
    [SerializeField] private float jumpStaminaCost = 15f;
    [SerializeField] private float jumpHeight = 1.25f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundAcceleration = 52f;
    [SerializeField] private float groundFriction = 8f;
    [SerializeField] private float stopSpeed = 2.2f;
    [SerializeField] private float airAcceleration = 18f;
    [SerializeField] private float airSpeedCap = 4.2f;
    [SerializeField] private float airStrafeMouseThreshold = 0.08f;
    [SerializeField] private float airStrafeBuildRange = 0.14f;
    [SerializeField] private float landingSpeedRetention = 0.5f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float coyoteTime = 0.08f;
    [SerializeField] private float groundedSnapForce = 2f;
    [SerializeField, HideInInspector] private float walkSpeedMultiplier = 0.48f;
    [SerializeField] private float crouchSpeedMultiplier = 0.58f;
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float crouchCameraDrop = 0.32f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float crouchStepOffsetMultiplier = 0.45f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.14f;
    [SerializeField] private float maxLookAngle = 85f;

    [Header("Combat")]
    [SerializeField] private PrototypeWeaponDefinition primaryWeapon;
    [SerializeField] private PrototypeWeaponDefinition secondaryWeapon;
    [SerializeField] private PrototypeWeaponDefinition meleeWeapon;
    [SerializeField] private float shootDistance = 60f;
    [SerializeField] private float baseDamage = 42f;
    [SerializeField] private float shootForce = 18f;
    [SerializeField] private float meleeImpactForce = 9f;
    [SerializeField] private float meleeStaminaCost = 22f;
    [SerializeField] private float impactMarkerLifetime = 0.2f;
    [SerializeField] private LayerMask shootMask = Physics.DefaultRaycastLayers;

    [Header("Medical")]
    [SerializeField] private float medicalUseCooldown = 0.28f;
    [SerializeField] private float medicalFeedbackLifetime = 1.4f;

    [Header("AI Awareness")]
    [SerializeField] private float firearmNoiseRadius = 26f;
    [SerializeField] private float meleeNoiseRadius = 8f;
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
    private InventoryContainer inventory;
    private readonly WeaponRuntime primaryRuntime = new WeaponRuntime { Slot = WeaponSlot.Primary };
    private readonly WeaponRuntime secondaryRuntime = new WeaponRuntime { Slot = WeaponSlot.Secondary };
    private readonly WeaponRuntime meleeRuntime = new WeaponRuntime { Slot = WeaponSlot.Melee };
    private WeaponSlot activeWeaponSlot = WeaponSlot.Primary;
    private Vector3 planarVelocity;
    private float verticalVelocity;
    private float pitch;
    private float lookYawDelta;
    private float hitMarkerTimer;
    private float jumpBufferTimer;
    private float groundedTimer;
    private float standingHeight;
    private float standingCameraLocalY;
    private float standingStepOffset;
    private bool isCrouching;
    private bool isSprinting;
    private bool wasGroundedLastFrame;
    private bool pendingLandingSpeedLoss;
    private float nextMedicalUseTime;
    private float medicalFeedbackTimer;
    private float nextMovementNoiseTime;
    private string medicalFeedbackMessage = string.Empty;
    private GUIStyle hudStyle;
    private GUIStyle centerStyle;
    private GUIStyle barLabelStyle;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        fpsInput = GetOrCreateInput();
        interactionState = GetOrCreateInteractionState();
        playerVitals = GetComponent<PrototypeUnitVitals>();
        inventory = GetComponent<InventoryContainer>();
        EnsureMovementSettings();
        EnsureCombatSettings();

        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }

        if (muzzle == null && viewCamera != null)
        {
            Transform muzzleTransform = viewCamera.transform.Find("Muzzle");
            if (muzzleTransform != null)
            {
                muzzle = muzzleTransform;
            }
        }

        ResolveViewModels();
        ConfigureRuntimeWeapons();
        CacheStanceDefaults();
        wasGroundedLastFrame = characterController != null && characterController.isGrounded;
    }

    private void OnEnable()
    {
        LockCursor(true);
        RefreshWeaponViewModels();
    }

    private void OnDisable()
    {
        LockCursor(false);
    }

    public void ConfigureWeaponLoadout(
        PrototypeWeaponDefinition primaryDefinition,
        PrototypeWeaponDefinition secondaryDefinition,
        PrototypeWeaponDefinition meleeDefinition)
    {
        primaryWeapon = primaryDefinition;
        secondaryWeapon = secondaryDefinition;
        meleeWeapon = meleeDefinition;

        if (Application.isPlaying)
        {
            ConfigureRuntimeWeapons();
        }
    }

    private void Update()
    {
        if (fpsInput == null || !fpsInput.IsReady || viewCamera == null)
        {
            return;
        }

        if (interactionState != null && interactionState.IsUiFocused)
        {
            isSprinting = false;
            if (Cursor.lockState != CursorLockMode.None)
            {
                LockCursor(false);
            }

            if (hitMarkerTimer > 0f)
            {
                hitMarkerTimer -= Time.deltaTime;
            }

            return;
        }

        if (fpsInput.ToggleCursorPressedThisFrame)
        {
            LockCursor(false);
        }
        else if (fpsInput.ShootPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor(true);
        }

        HandleLook();
        HandleMovement();
        HandleWeaponInput();
        bool usedMedical = HandleMedicalInput();
        if (!usedMedical)
        {
            HandleCombat();
        }

        if (hitMarkerTimer > 0f)
        {
            hitMarkerTimer -= Time.deltaTime;
        }

        if (medicalFeedbackTimer > 0f)
        {
            medicalFeedbackTimer -= Time.deltaTime;
            if (medicalFeedbackTimer <= 0f)
            {
                medicalFeedbackMessage = string.Empty;
            }
        }
    }

    private void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            lookYawDelta = 0f;
            return;
        }

        Vector2 mouseDelta = fpsInput.LookDelta;
        lookYawDelta = mouseDelta.x * mouseSensitivity;

        transform.Rotate(Vector3.up * lookYawDelta);

        pitch -= mouseDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        viewCamera.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        float deltaTime = Time.deltaTime;
        bool grounded = characterController.isGrounded;

        UpdateStance(deltaTime, grounded);
        UpdateJumpTimers(grounded, deltaTime);

        Vector2 moveInput = Vector2.ClampMagnitude(fpsInput.Move, 1f);
        bool wasSprinting = isSprinting;
        isSprinting = CanSprint(moveInput, grounded, wasSprinting);
        if (isSprinting && playerVitals != null && sprintStaminaPerSecond > 0f)
        {
            float drained = playerVitals.DrainStamina(sprintStaminaPerSecond * deltaTime);
            isSprinting = drained > StaminaEpsilon;
        }

        Vector3 wishDirection = GetWishDirection(moveInput, grounded);
        bool jumpedThisFrame = TryConsumeJump(ref grounded);
        bool appliedLandingSpeedLoss = false;

        if (pendingLandingSpeedLoss)
        {
            if (jumpedThisFrame)
            {
                pendingLandingSpeedLoss = false;
            }
            else if (grounded)
            {
                ApplyLandingSpeedLoss();
                pendingLandingSpeedLoss = false;
                appliedLandingSpeedLoss = true;
            }
        }

        if (grounded)
        {
            if (!appliedLandingSpeedLoss)
            {
                ApplyGroundFriction(deltaTime);
            }

            if (verticalVelocity < 0f)
            {
                verticalVelocity = -groundedSnapForce;
            }
        }

        float targetMoveSpeed = GetTargetMoveSpeed();
        float wishSpeed = GetWishSpeed(moveInput, grounded, targetMoveSpeed);
        float acceleration = grounded ? groundAcceleration : airAcceleration;
        Accelerate(wishDirection, wishSpeed, acceleration, deltaTime);

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
            pendingLandingSpeedLoss = true;
            float landingRadius = landingNoiseRadius + Mathf.Min(planarVelocity.magnitude * 0.45f, 4f);
            if (isCrouching)
            {
                landingRadius *= crouchNoiseMultiplier;
            }

            ReportCombatNoise(landingRadius);
            nextMovementNoiseTime = Time.time + movementNoiseInterval * 0.75f;
        }
        else if (!groundedAfterMove)
        {
            pendingLandingSpeedLoss = false;
        }

        if (groundedAfterMove && !landedThisFrame)
        {
            ReportMovementNoise();
        }

        wasGroundedLastFrame = groundedAfterMove;
    }

    private void UpdateStance(float deltaTime, bool grounded)
    {
        isCrouching = fpsInput.CrouchHeld;

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float targetCameraY = isCrouching
            ? standingCameraLocalY - crouchCameraDrop
            : standingCameraLocalY;
        float targetStepOffset = isCrouching
            ? standingStepOffset * crouchStepOffsetMultiplier
            : standingStepOffset;

        float nextHeight = Mathf.MoveTowards(characterController.height, targetHeight, crouchTransitionSpeed * deltaTime);
        characterController.height = nextHeight;
        characterController.center = new Vector3(0f, nextHeight * 0.5f, 0f);
        characterController.stepOffset = grounded ? targetStepOffset : 0f;

        Vector3 cameraLocalPosition = viewCamera.transform.localPosition;
        cameraLocalPosition.y = Mathf.MoveTowards(cameraLocalPosition.y, targetCameraY, crouchTransitionSpeed * deltaTime);
        viewCamera.transform.localPosition = cameraLocalPosition;
    }

    private float GetTargetMoveSpeed()
    {
        float targetSpeed = moveSpeed;

        if (isSprinting)
        {
            targetSpeed = Mathf.Max(targetSpeed, moveSpeed * sprintSpeedMultiplier);
        }

        if (isCrouching)
        {
            targetSpeed = Mathf.Min(targetSpeed, moveSpeed * crouchSpeedMultiplier);
        }

        if (playerVitals != null)
        {
            targetSpeed *= playerVitals.MovementPenaltyMultiplier;
        }

        return targetSpeed;
    }

    private Vector3 GetWishDirection(Vector2 moveInput, bool grounded)
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0f ? forward.normalized : Vector3.forward;

        Vector3 right = transform.right;
        right.y = 0f;
        right = right.sqrMagnitude > 0f ? right.normalized : Vector3.right;

        float airStrafeFactor = grounded ? 1f : GetAirStrafeFactor(moveInput.x);
        Vector3 wishDirection = forward * moveInput.y;
        if (grounded || airStrafeFactor > 0f)
        {
            wishDirection += right * (moveInput.x * airStrafeFactor);
        }

        return wishDirection.sqrMagnitude > 0f ? wishDirection.normalized : Vector3.zero;
    }

    private float GetWishSpeed(Vector2 moveInput, bool grounded, float targetMoveSpeed)
    {
        float allowedSideInput = grounded ? Mathf.Abs(moveInput.x) : Mathf.Abs(moveInput.x) * GetAirStrafeFactor(moveInput.x);
        float inputMagnitude = Mathf.Clamp01(new Vector2(allowedSideInput, moveInput.y).magnitude);
        float maxWishSpeed = grounded ? targetMoveSpeed : Mathf.Min(targetMoveSpeed, airSpeedCap);
        return maxWishSpeed * inputMagnitude;
    }

    private void UpdateJumpTimers(bool grounded, float deltaTime)
    {
        if (fpsInput.JumpPressedThisFrame)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer = Mathf.Max(jumpBufferTimer - deltaTime, 0f);
        }

        groundedTimer = grounded ? coyoteTime : Mathf.Max(groundedTimer - deltaTime, 0f);
    }

    private bool TryConsumeJump(ref bool grounded)
    {
        if (jumpBufferTimer <= 0f || groundedTimer <= 0f)
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
        jumpBufferTimer = 0f;
        groundedTimer = 0f;
        grounded = false;
        ReportCombatNoise(isCrouching ? jumpNoiseRadius * crouchNoiseMultiplier : jumpNoiseRadius);
        return true;
    }

    private void ApplyGroundFriction(float deltaTime)
    {
        float speed = planarVelocity.magnitude;
        if (speed <= 0f)
        {
            return;
        }

        float control = Mathf.Max(speed, stopSpeed);
        float drop = control * groundFriction * deltaTime;
        float newSpeed = Mathf.Max(speed - drop, 0f);

        if (newSpeed == speed)
        {
            return;
        }

        planarVelocity *= newSpeed / speed;
    }

    private void ApplyLandingSpeedLoss()
    {
        if (planarVelocity.sqrMagnitude <= 0f)
        {
            return;
        }

        planarVelocity *= landingSpeedRetention;
    }

    private void Accelerate(Vector3 wishDirection, float wishSpeed, float acceleration, float deltaTime)
    {
        if (wishDirection.sqrMagnitude <= 0f || wishSpeed <= 0f || acceleration <= 0f)
        {
            return;
        }

        float currentSpeed = Vector3.Dot(planarVelocity, wishDirection);
        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0f)
        {
            return;
        }

        float accelSpeed = acceleration * deltaTime * wishSpeed;
        if (accelSpeed > addSpeed)
        {
            accelSpeed = addSpeed;
        }

        planarVelocity += wishDirection * accelSpeed;
    }

    private float GetAirStrafeFactor(float sideInput)
    {
        if (Mathf.Abs(sideInput) <= 0.01f)
        {
            return 0f;
        }

        if (Mathf.Sign(sideInput) != Mathf.Sign(lookYawDelta))
        {
            return 0f;
        }

        float absYawDelta = Mathf.Abs(lookYawDelta);
        if (absYawDelta <= airStrafeMouseThreshold)
        {
            return 0f;
        }

        return Mathf.Clamp01((absYawDelta - airStrafeMouseThreshold) / airStrafeBuildRange);
    }

    private bool CanSprint(Vector2 moveInput, bool grounded, bool wasSprinting)
    {
        if (!grounded || isCrouching || fpsInput == null || !fpsInput.SprintHeld || moveInput.sqrMagnitude <= 0.01f)
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

    private void HandleWeaponInput()
    {
        if (fpsInput.EquipPrimaryPressedThisFrame)
        {
            EquipWeapon(WeaponSlot.Primary);
        }
        else if (fpsInput.EquipSecondaryPressedThisFrame)
        {
            EquipWeapon(WeaponSlot.Secondary);
        }
        else if (fpsInput.EquipMeleePressedThisFrame)
        {
            EquipWeapon(WeaponSlot.Melee);
        }

        WeaponRuntime activeWeapon = GetActiveWeapon();
        if (activeWeapon == null || !activeWeapon.IsConfigured)
        {
            return;
        }

        if (fpsInput.ReloadPressedThisFrame)
        {
            TryStartReload(activeWeapon);
        }

        if (fpsInput.ToggleFireModePressedThisFrame)
        {
            CycleFireMode(activeWeapon);
        }
    }

    private bool HandleMedicalInput()
    {
        if (fpsInput == null || playerVitals == null || inventory == null || Time.time < nextMedicalUseTime)
        {
            return false;
        }

        if (fpsInput.StopBleedPressedThisFrame)
        {
            return TryUseBleedTreatment();
        }

        if (fpsInput.SplintPressedThisFrame)
        {
            return TryUseSplint();
        }

        if (fpsInput.PainkillerPressedThisFrame)
        {
            return TryUsePainkiller();
        }

        if (fpsInput.QuickHealPressedThisFrame)
        {
            return TryUseQuickHeal();
        }

        return false;
    }

    private void HandleCombat()
    {
        WeaponRuntime activeWeapon = GetActiveWeapon();
        if (activeWeapon == null || !activeWeapon.IsConfigured)
        {
            return;
        }

        UpdateReload(activeWeapon);

        if (activeWeapon.IsReloading)
        {
            return;
        }

        if (activeWeapon.Definition.IsMeleeWeapon)
        {
            if (fpsInput.ShootPressedThisFrame && Time.time >= activeWeapon.NextAttackTime)
            {
                PerformMeleeAttack(activeWeapon);
            }

            return;
        }

        if (activeWeapon.PendingBurstShots > 0)
        {
            ProcessBurst(activeWeapon);
            return;
        }

        switch (activeWeapon.CurrentFireMode)
        {
            case PrototypeWeaponFireMode.Auto:
                if (fpsInput.ShootHeld)
                {
                    FireFirearmRound(activeWeapon);
                }
                break;

            case PrototypeWeaponFireMode.Burst:
                if (fpsInput.ShootPressedThisFrame && Time.time >= activeWeapon.NextAttackTime)
                {
                    activeWeapon.PendingBurstShots = activeWeapon.Definition.BurstCount;
                    ProcessBurst(activeWeapon);
                }
                break;

            default:
                if (fpsInput.ShootPressedThisFrame)
                {
                    FireFirearmRound(activeWeapon);
                }
                break;
        }
    }

    private bool TryUseQuickHeal()
    {
        bool needsHealing = playerVitals.TotalCurrentHealth < playerVitals.TotalMaxHealth - 0.5f;
        if (!needsHealing && !playerVitals.HasAnyBleed && !playerVitals.HasFracture)
        {
            SetMedicalFeedback("No treatment needed");
            return false;
        }

        return TryUseMedicalItem(
            medical => medical.HealAmount > 0f
                || (playerVitals.HasHeavyBleed && medical.RemovesHeavyBleeds > 0)
                || (playerVitals.HasLightBleed && medical.RemovesLightBleeds > 0)
                || (playerVitals.HasFracture && medical.CuresFractures > 0),
            medical =>
            {
                float score = 0f;
                if (needsHealing)
                {
                    score += medical.HealAmount;
                }

                if (playerVitals.HasHeavyBleed)
                {
                    score += medical.RemovesHeavyBleeds * 150f;
                }

                if (playerVitals.HasLightBleed)
                {
                    score += medical.RemovesLightBleeds * 60f;
                }

                if (playerVitals.HasFracture)
                {
                    score += medical.CuresFractures * 90f;
                }

                return score;
            },
            "No medical item ready");
    }

    private bool TryUseBleedTreatment()
    {
        if (!playerVitals.HasAnyBleed)
        {
            SetMedicalFeedback("No bleeding");
            return false;
        }

        bool hasHeavyBleed = playerVitals.HasHeavyBleed;
        return TryUseMedicalItem(
            medical => hasHeavyBleed ? medical.RemovesHeavyBleeds > 0 : medical.RemovesLightBleeds > 0 || medical.RemovesHeavyBleeds > 0,
            medical =>
            {
                float score = 0f;
                if (hasHeavyBleed)
                {
                    score += medical.RemovesHeavyBleeds * 200f;
                    score += medical.RemovesLightBleeds * 25f;
                }
                else
                {
                    score += medical.RemovesLightBleeds * 120f;
                    score += medical.RemovesHeavyBleeds * 80f;
                }

                score += medical.HealAmount * 0.1f;
                return score;
            },
            "No bleed treatment");
    }

    private bool TryUseSplint()
    {
        if (!playerVitals.HasFracture)
        {
            SetMedicalFeedback("No fracture");
            return false;
        }

        return TryUseMedicalItem(
            medical => medical.CuresFractures > 0,
            medical => medical.CuresFractures * 100f + medical.PainkillerDuration * 0.1f,
            "No splint available");
    }

    private bool TryUsePainkiller()
    {
        if (playerVitals.IsPainkillerActive && playerVitals.PainkillerRemaining > 8f)
        {
            SetMedicalFeedback("Painkiller active");
            return false;
        }

        return TryUseMedicalItem(
            medical => medical.PainkillerDuration > 0f,
            medical => medical.PainkillerDuration + medical.HealAmount * 0.1f,
            "No painkillers available");
    }

    private bool TryUseMedicalItem(
        Func<MedicalItemDefinition, bool> predicate,
        Func<MedicalItemDefinition, float> scoreSelector,
        string missingItemMessage)
    {
        MedicalItemDefinition bestMedicalItem = FindBestMedicalItem(predicate, scoreSelector);
        if (bestMedicalItem == null)
        {
            SetMedicalFeedback(missingItemMessage);
            return false;
        }

        if (!playerVitals.TryUseMedicalItem(bestMedicalItem))
        {
            SetMedicalFeedback("Treatment not needed");
            return false;
        }

        if (!inventory.TryRemoveItem(bestMedicalItem, 1, out int removedQuantity) || removedQuantity <= 0)
        {
            SetMedicalFeedback("Treatment sync failed");
            return false;
        }

        nextMedicalUseTime = Time.time + medicalUseCooldown;
        SetMedicalFeedback($"Used {bestMedicalItem.DisplayName}");
        return true;
    }

    private MedicalItemDefinition FindBestMedicalItem(
        Func<MedicalItemDefinition, bool> predicate,
        Func<MedicalItemDefinition, float> scoreSelector)
    {
        if (inventory == null || predicate == null || scoreSelector == null)
        {
            return null;
        }

        MedicalItemDefinition bestMedicalItem = null;
        float bestScore = float.NegativeInfinity;

        foreach (ItemInstance item in inventory.Items)
        {
            if (item == null || !(item.Definition is MedicalItemDefinition medicalItem) || item.Quantity <= 0)
            {
                continue;
            }

            if (!predicate(medicalItem))
            {
                continue;
            }

            float score = scoreSelector(medicalItem);
            if (score > bestScore)
            {
                bestScore = score;
                bestMedicalItem = medicalItem;
            }
        }

        return bestMedicalItem;
    }

    private void SetMedicalFeedback(string message)
    {
        medicalFeedbackMessage = message ?? string.Empty;
        medicalFeedbackTimer = medicalFeedbackLifetime;
    }

    private void ProcessBurst(WeaponRuntime runtime)
    {
        while (runtime.PendingBurstShots > 0 && Time.time >= runtime.NextAttackTime)
        {
            if (!FireFirearmRound(runtime))
            {
                runtime.PendingBurstShots = 0;
                return;
            }

            runtime.PendingBurstShots--;
        }
    }

    private bool FireFirearmRound(WeaponRuntime runtime)
    {
        if (runtime == null || !runtime.IsConfigured || runtime.Definition.IsMeleeWeapon)
        {
            return false;
        }

        if (runtime.IsReloading || Time.time < runtime.NextAttackTime)
        {
            return false;
        }

        if (runtime.MagazineAmmo <= 0)
        {
            runtime.PendingBurstShots = 0;
            return false;
        }

        runtime.MagazineAmmo--;
        runtime.NextAttackTime = Time.time + runtime.Definition.SecondsPerShot;
        ReportCombatNoise(muzzle != null ? muzzle.position : viewCamera.transform.position, firearmNoiseRadius);

        AmmoDefinition ammo = runtime.Definition.AmmoDefinition;
        float shotDamage = ammo != null ? ammo.DirectDamage : baseDamage;
        float shotForce = (ammo != null ? ammo.ImpactForce : shootForce) + runtime.Definition.AddedImpactForce;
        float shotRange = runtime.Definition.EffectiveRange > 0f ? runtime.Definition.EffectiveRange : shootDistance;
        Vector3 direction = GetSpreadDirection(runtime.Definition.SpreadAngle);
        PrototypeUnitVitals.DamageInfo damageInfo = BuildFirearmDamageInfo(runtime, shotDamage, ammo);

        if (TryGetCombatHit(viewCamera.transform.position, direction, shotRange, out RaycastHit hit))
        {
            ResolveCombatHit(hit, damageInfo, shotForce);
        }

        return true;
    }

    private void PerformMeleeAttack(WeaponRuntime runtime)
    {
        if (runtime == null || !runtime.IsConfigured || !runtime.Definition.IsMeleeWeapon)
        {
            return;
        }

        if (playerVitals != null && meleeStaminaCost > 0f && (!playerVitals.CanStartStaminaAction(meleeStaminaCost) || !playerVitals.TryConsumeStamina(meleeStaminaCost)))
        {
            return;
        }

        runtime.NextAttackTime = Time.time + runtime.Definition.MeleeCooldown;
        float meleeRadius = Mathf.Max(meleeNoiseRadius, runtime.Definition.MeleeRange * 3.8f);
        ReportCombatNoise(muzzle != null ? muzzle.position : viewCamera.transform.position, meleeRadius);

        if (TryGetMeleeHit(runtime.Definition.MeleeRange, runtime.Definition.MeleeRadius, out RaycastHit hit))
        {
            ResolveCombatHit(hit, BuildMeleeDamageInfo(runtime.Definition), meleeImpactForce);
        }
    }

    private bool TryGetCombatHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit hit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, shootMask, QueryTriggerInteraction.Collide);
        return TrySelectCombatHit(hits, out hit);
    }

    private bool TryGetMeleeHit(float range, float radius, out RaycastHit hit)
    {
        Vector3 origin = viewCamera.transform.position;
        Vector3 direction = viewCamera.transform.forward;
        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, range, shootMask, QueryTriggerInteraction.Collide);
        return TrySelectCombatHit(hits, out hit);
    }

    private bool TrySelectCombatHit(RaycastHit[] hits, out RaycastHit hit)
    {
        if (hits == null || hits.Length == 0)
        {
            hit = default;
            return false;
        }

        Array.Sort(hits, CompareHitDistance);

        foreach (RaycastHit candidate in hits)
        {
            if (candidate.collider == null)
            {
                continue;
            }

            if (candidate.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            PrototypeUnitHitbox unitHitbox = candidate.collider.GetComponent<PrototypeUnitHitbox>();
            if (unitHitbox == null)
            {
                unitHitbox = candidate.collider.GetComponentInParent<PrototypeUnitHitbox>();
            }

            if (candidate.collider.isTrigger && unitHitbox == null)
            {
                continue;
            }

            hit = candidate;
            return true;
        }

        hit = default;
        return false;
    }

    private PrototypeUnitVitals.DamageInfo BuildFirearmDamageInfo(WeaponRuntime runtime, float defaultDamage, AmmoDefinition ammo)
    {
        return new PrototypeUnitVitals.DamageInfo
        {
            damage = Mathf.Max(1f, defaultDamage),
            penetrationPower = ammo != null ? ammo.PenetrationPower : runtime.Definition.PenetrationPower,
            armorDamage = ammo != null ? ammo.ArmorDamage : Mathf.Max(8f, defaultDamage * 0.5f),
            lightBleedChance = ammo != null ? ammo.LightBleedChance : runtime.Definition.LightBleedChance,
            heavyBleedChance = ammo != null ? ammo.HeavyBleedChance : runtime.Definition.HeavyBleedChance,
            fractureChance = ammo != null ? ammo.FractureChance : runtime.Definition.FractureChance,
            bypassArmor = false,
            canApplyAfflictions = true
        };
    }

    private PrototypeUnitVitals.DamageInfo BuildMeleeDamageInfo(PrototypeWeaponDefinition weaponDefinition)
    {
        return new PrototypeUnitVitals.DamageInfo
        {
            damage = weaponDefinition != null ? weaponDefinition.MeleeDamage : baseDamage,
            penetrationPower = weaponDefinition != null ? weaponDefinition.PenetrationPower : 6f,
            armorDamage = weaponDefinition != null ? Mathf.Max(6f, weaponDefinition.MeleeDamage * 0.28f) : 6f,
            lightBleedChance = weaponDefinition != null ? weaponDefinition.LightBleedChance : 0.4f,
            heavyBleedChance = weaponDefinition != null ? weaponDefinition.HeavyBleedChance : 0.1f,
            fractureChance = weaponDefinition != null ? weaponDefinition.FractureChance : 0.12f,
            bypassArmor = false,
            canApplyAfflictions = true
        };
    }

    private void ResolveCombatHit(RaycastHit hit, PrototypeUnitVitals.DamageInfo damageInfo, float force)
    {
        hitMarkerTimer = 0.08f;

        PrototypeUnitHitbox unitHitbox = hit.collider.GetComponent<PrototypeUnitHitbox>();
        if (unitHitbox == null)
        {
            unitHitbox = hit.collider.GetComponentInParent<PrototypeUnitHitbox>();
        }

        bool shouldApplyImpactForce = true;
        if (unitHitbox != null)
        {
            unitHitbox.ApplyDamage(damageInfo);
            if (unitHitbox.Owner != null)
            {
                shouldApplyImpactForce = unitHitbox.Owner.ShouldReceiveImpactForce;
            }
        }

        if (shouldApplyImpactForce && hit.rigidbody != null && force > 0f)
        {
            hit.rigidbody.AddForce(viewCamera.transform.forward * force, ForceMode.Impulse);
        }

        SpawnImpactMarker(hit);
    }

    private void TryStartReload(WeaponRuntime runtime)
    {
        if (runtime == null || !runtime.IsConfigured || runtime.Definition.IsMeleeWeapon)
        {
            return;
        }

        if (runtime.IsReloading || runtime.MagazineAmmo >= runtime.Definition.MagazineSize)
        {
            return;
        }

        AmmoDefinition ammo = runtime.Definition.AmmoDefinition;
        if (inventory == null || ammo == null || inventory.CountItem(ammo) <= 0)
        {
            return;
        }

        runtime.IsReloading = true;
        runtime.ReloadEndTime = Time.time + runtime.Definition.ReloadDuration;
        runtime.PendingBurstShots = 0;
    }

    private void UpdateReload(WeaponRuntime runtime)
    {
        if (runtime == null || !runtime.IsReloading)
        {
            return;
        }

        if (Time.time < runtime.ReloadEndTime)
        {
            return;
        }

        runtime.IsReloading = false;

        if (inventory == null || runtime.Definition == null || runtime.Definition.AmmoDefinition == null)
        {
            return;
        }

        int roundsNeeded = runtime.Definition.MagazineSize - runtime.MagazineAmmo;
        if (roundsNeeded <= 0)
        {
            return;
        }

        if (inventory.TryRemoveItem(runtime.Definition.AmmoDefinition, roundsNeeded, out int removedQuantity) && removedQuantity > 0)
        {
            runtime.MagazineAmmo = Mathf.Clamp(runtime.MagazineAmmo + removedQuantity, 0, runtime.Definition.MagazineSize);
        }
    }

    private void CycleFireMode(WeaponRuntime runtime)
    {
        if (runtime == null || !runtime.IsConfigured || runtime.Definition.IsMeleeWeapon)
        {
            return;
        }

        PrototypeWeaponFireMode[] fireModes = runtime.Definition.FireModes;
        if (fireModes == null || fireModes.Length <= 1)
        {
            return;
        }

        runtime.FireModeIndex = (runtime.FireModeIndex + 1) % fireModes.Length;
        runtime.PendingBurstShots = 0;
    }

    private void EquipWeapon(WeaponSlot slot)
    {
        WeaponRuntime runtime = GetWeaponRuntime(slot);
        if (runtime == null || !runtime.IsConfigured || activeWeaponSlot == slot)
        {
            return;
        }

        WeaponRuntime currentWeapon = GetActiveWeapon();
        if (currentWeapon != null)
        {
            currentWeapon.IsReloading = false;
            currentWeapon.PendingBurstShots = 0;
        }

        activeWeaponSlot = slot;
        RefreshWeaponViewModels();
    }

    private void ConfigureRuntimeWeapons()
    {
        SetupWeaponRuntime(primaryRuntime, primaryWeapon);
        SetupWeaponRuntime(secondaryRuntime, secondaryWeapon);
        SetupWeaponRuntime(meleeRuntime, meleeWeapon);
        SelectInitialWeapon();
        RefreshWeaponViewModels();
    }

    private void SetupWeaponRuntime(WeaponRuntime runtime, PrototypeWeaponDefinition definition)
    {
        runtime.Definition = definition;
        runtime.PendingBurstShots = 0;
        runtime.IsReloading = false;
        runtime.NextAttackTime = 0f;
        runtime.ReloadEndTime = 0f;
        runtime.FireModeIndex = 0;
        runtime.MagazineAmmo = definition != null && !definition.IsMeleeWeapon ? definition.MagazineSize : 0;
    }

    private void SelectInitialWeapon()
    {
        if (primaryRuntime.IsConfigured)
        {
            activeWeaponSlot = WeaponSlot.Primary;
        }
        else if (secondaryRuntime.IsConfigured)
        {
            activeWeaponSlot = WeaponSlot.Secondary;
        }
        else if (meleeRuntime.IsConfigured)
        {
            activeWeaponSlot = WeaponSlot.Melee;
        }
    }

    private WeaponRuntime GetActiveWeapon()
    {
        return GetWeaponRuntime(activeWeaponSlot);
    }

    private WeaponRuntime GetWeaponRuntime(WeaponSlot slot)
    {
        switch (slot)
        {
            case WeaponSlot.Primary:
                return primaryRuntime;

            case WeaponSlot.Secondary:
                return secondaryRuntime;

            default:
                return meleeRuntime;
        }
    }

    private int GetReserveAmmoCount(WeaponRuntime runtime)
    {
        if (inventory == null || runtime == null || runtime.Definition == null || runtime.Definition.AmmoDefinition == null)
        {
            return 0;
        }

        return inventory.CountItem(runtime.Definition.AmmoDefinition);
    }

    private void ResolveViewModels()
    {
        if (viewCamera == null)
        {
            return;
        }

        if (primaryViewModel == null)
        {
            Transform found = viewCamera.transform.Find("WeaponView_Primary");
            if (found != null)
            {
                primaryViewModel = found;
            }
        }

        if (secondaryViewModel == null)
        {
            Transform found = viewCamera.transform.Find("WeaponView_Secondary");
            if (found != null)
            {
                secondaryViewModel = found;
            }
        }

        if (meleeViewModel == null)
        {
            Transform found = viewCamera.transform.Find("WeaponView_Melee");
            if (found != null)
            {
                meleeViewModel = found;
            }
        }
    }

    private void RefreshWeaponViewModels()
    {
        if (primaryViewModel != null)
        {
            primaryViewModel.gameObject.SetActive(activeWeaponSlot == WeaponSlot.Primary);
        }

        if (secondaryViewModel != null)
        {
            secondaryViewModel.gameObject.SetActive(activeWeaponSlot == WeaponSlot.Secondary);
        }

        if (meleeViewModel != null)
        {
            meleeViewModel.gameObject.SetActive(activeWeaponSlot == WeaponSlot.Melee);
        }
    }

    private Vector3 GetSpreadDirection(float spreadAngle)
    {
        Vector3 direction = viewCamera.transform.forward;
        if (spreadAngle <= 0.001f)
        {
            return direction;
        }

        Vector2 spread = UnityEngine.Random.insideUnitCircle * spreadAngle;
        direction = Quaternion.AngleAxis(spread.x, viewCamera.transform.up)
            * Quaternion.AngleAxis(spread.y, viewCamera.transform.right)
            * direction;
        return direction.normalized;
    }

    private static int CompareHitDistance(RaycastHit left, RaycastHit right)
    {
        return left.distance.CompareTo(right.distance);
    }

    private void SpawnImpactMarker(RaycastHit hit)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "ImpactMarker";
        marker.transform.position = hit.point + hit.normal * 0.03f;
        marker.transform.localScale = Vector3.one * 0.12f;

        Destroy(marker.GetComponent<Collider>());

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        markerRenderer.material = new Material(shader);
        markerRenderer.material.color = new Color(1f, 0.4f, 0.1f, 1f);

        Destroy(marker, impactMarkerLifetime);
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

        float targetTopSpeed = Mathf.Max(moveSpeed * sprintSpeedMultiplier, moveSpeed);
        float speedFactor = Mathf.InverseLerp(0.65f, targetTopSpeed, movementSpeed);
        float noiseRadius = isSprinting
            ? sprintNoiseRadius
            : Mathf.Lerp(walkNoiseRadius * 0.72f, walkNoiseRadius, speedFactor);
        if (isCrouching)
        {
            noiseRadius *= crouchNoiseMultiplier;
        }

        ReportCombatNoise(noiseRadius);
        nextMovementNoiseTime = Time.time + movementNoiseInterval * (isSprinting ? 0.72f : 1f);
    }

    private void ReportCombatNoise(float radius)
    {
        ReportCombatNoise(transform.position + Vector3.up * 0.9f, radius);
    }

    private void ReportCombatNoise(Vector3 position, float radius)
    {
        if (radius <= 0f)
        {
            return;
        }

        PrototypeCombatNoiseSystem.ReportNoise(position, radius, gameObject);
    }

    private void LockCursor(bool shouldLock)
    {
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    private void OnGUI()
    {
        EnsureHudStyles();

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        WeaponRuntime activeWeapon = GetActiveWeapon();

        GUI.Label(new Rect(centerX - 10f, centerY - 20f, 20f, 40f), hitMarkerTimer > 0f ? "X" : "+", centerStyle);

        GUI.Label(
            new Rect(18f, 100f, 380f, 280f),
            "Move\nLook\nAttack\nInteract\nInventory\nEquip 1 / 2 / 3\nReload\nToggle Fire Mode\nQuick Heal 4\nStop Bleed 5\nSplint 6\nPainkiller 7\nJump\nSprint\nCrouch\nToggle Cursor",
            hudStyle);

        if (activeWeapon == null || !activeWeapon.IsConfigured)
        {
            return;
        }

        string weaponLine = activeWeapon.Definition.DisplayName;
        string stateLine;
        if (activeWeapon.Definition.IsMeleeWeapon)
        {
            float cooldownRemaining = Mathf.Max(0f, activeWeapon.NextAttackTime - Time.time);
            stateLine = cooldownRemaining > 0f
                ? $"Cooldown {cooldownRemaining:0.00}s"
                : "Ready";
        }
        else if (activeWeapon.IsReloading)
        {
            stateLine = $"Reloading {Mathf.Max(0f, activeWeapon.ReloadEndTime - Time.time):0.0}s";
        }
        else
        {
            stateLine = $"{activeWeapon.CurrentFireMode}  {activeWeapon.MagazineAmmo}/{activeWeapon.Definition.MagazineSize}  Reserve {GetReserveAmmoCount(activeWeapon)}";
        }

        DrawStaminaBar(new Rect(18f, 394f, 280f, 24f));
        GUI.Box(new Rect(Screen.width - 370f, 18f, 340f, 172f), $"{weaponLine}\n{stateLine}\n{BuildCombatStatusText()}", hudStyle);
    }

    private string BuildCombatStatusText()
    {
        if (playerVitals == null)
        {
            return "No vitals";
        }

        var builder = new StringBuilder(192);
        builder.Append("HP ");
        builder.Append(Mathf.RoundToInt(playerVitals.TotalCurrentHealth));
        builder.Append('/');
        builder.Append(Mathf.RoundToInt(playerVitals.TotalMaxHealth));
        builder.Append("\nSTA ");
        builder.Append(Mathf.RoundToInt(playerVitals.CurrentStamina));
        builder.Append('/');
        builder.Append(Mathf.RoundToInt(playerVitals.MaxStamina));
        if (playerVitals.StaminaRecoveryBlockedRemaining > 0f)
        {
            builder.Append(playerVitals.IsExhausted ? "  Exhausted " : "  Recover ");
            builder.Append(playerVitals.StaminaRecoveryBlockedRemaining.ToString("0.0"));
            builder.Append('s');
        }

        float headArmor = playerVitals.GetArmorDurabilityNormalized("head");
        float torsoArmor = playerVitals.GetArmorDurabilityNormalized("torso");
        if (headArmor > 0f || torsoArmor > 0f)
        {
            builder.Append("\nArmor H ");
            builder.Append(Mathf.RoundToInt(headArmor * 100f));
            builder.Append("%  T ");
            builder.Append(Mathf.RoundToInt(torsoArmor * 100f));
            builder.Append('%');
        }

        builder.Append("\nStatus ");
        if (playerVitals.HasHeavyBleed)
        {
            builder.Append("HeavyBleed ");
        }

        if (playerVitals.HasLightBleed)
        {
            builder.Append("LightBleed ");
        }

        if (playerVitals.HasFracture)
        {
            builder.Append("Fracture ");
        }

        if (playerVitals.IsPainkillerActive)
        {
            builder.Append("PK ");
            builder.Append(playerVitals.PainkillerRemaining.ToString("0"));
            builder.Append('s');
        }

        if (!playerVitals.HasAnyBleed && !playerVitals.HasFracture && !playerVitals.IsPainkillerActive)
        {
            builder.Append("Clean");
        }

        if (!string.IsNullOrWhiteSpace(medicalFeedbackMessage))
        {
            builder.Append("\n");
            builder.Append(medicalFeedbackMessage);
        }

        return builder.ToString();
    }

    private void DrawStaminaBar(Rect rect)
    {
        if (playerVitals == null)
        {
            return;
        }

        GUI.Box(rect, GUIContent.none);

        float normalized = playerVitals.StaminaNormalized;
        Rect fillRect = new Rect(rect.x + 3f, rect.y + 3f, Mathf.Max(0f, (rect.width - 6f) * normalized), rect.height - 6f);
        bool recoveryBlocked = playerVitals.IsStaminaRecoveryBlocked;
        bool lowStamina = playerVitals.IsBelowStaminaActionThreshold;
        Color fillColor = lowStamina
            ? new Color(0.8f, 0.2f, 0.18f, 0.95f)
            : recoveryBlocked
                ? (playerVitals.IsExhausted ? new Color(0.78f, 0.28f, 0.2f, 0.95f) : new Color(0.88f, 0.58f, 0.14f, 0.95f))
            : Color.Lerp(new Color(0.94f, 0.68f, 0.16f, 0.95f), new Color(0.27f, 0.82f, 0.38f, 0.95f), normalized);

        Color previousColor = GUI.color;
        GUI.color = fillColor;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = previousColor;

        string label = recoveryBlocked
            ? playerVitals.IsExhausted
                ? $"Stamina {Mathf.RoundToInt(playerVitals.CurrentStamina)}/{Mathf.RoundToInt(playerVitals.MaxStamina)}  Exhausted {playerVitals.StaminaRecoveryBlockedRemaining:0.0}s"
                : $"Stamina {Mathf.RoundToInt(playerVitals.CurrentStamina)}/{Mathf.RoundToInt(playerVitals.MaxStamina)}  Recover {playerVitals.StaminaRecoveryBlockedRemaining:0.0}s"
            : $"Stamina {Mathf.RoundToInt(playerVitals.CurrentStamina)}/{Mathf.RoundToInt(playerVitals.MaxStamina)}";
        GUI.Label(rect, label, barLabelStyle);
    }

    private void EnsureHudStyles()
    {
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };
            hudStyle.padding = new RectOffset(12, 12, 10, 10);
        }

        if (centerStyle == null)
        {
            centerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }

        if (barLabelStyle == null)
        {
            barLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }
    }

    private void OnValidate()
    {
        EnsureMovementSettings();
        EnsureCombatSettings();

        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }

        if (muzzle == null && viewCamera != null)
        {
            Transform muzzleTransform = viewCamera.transform.Find("Muzzle");
            if (muzzleTransform != null)
            {
                muzzle = muzzleTransform;
            }
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

        ResolveViewModels();
        CacheStanceDefaults();
    }

    private void EnsureMovementSettings()
    {
        moveSpeed = Mathf.Max(moveSpeed, 0.1f);
        sprintSpeedMultiplier = Mathf.Max(1f, sprintSpeedMultiplier);
        sprintStaminaPerSecond = Mathf.Max(0f, sprintStaminaPerSecond);
        jumpStaminaCost = Mathf.Max(0f, jumpStaminaCost);
        jumpHeight = Mathf.Max(jumpHeight, 0.1f);
        gravity = gravity < -0.1f ? gravity : -20f;
        groundAcceleration = groundAcceleration > 0f ? groundAcceleration : 52f;
        groundFriction = groundFriction > 0f ? groundFriction : 8f;
        stopSpeed = stopSpeed > 0f ? stopSpeed : 2.2f;
        airAcceleration = airAcceleration > 0f ? airAcceleration : 18f;
        airSpeedCap = airSpeedCap > 0f ? airSpeedCap : 4.2f;
        airStrafeMouseThreshold = Mathf.Max(airStrafeMouseThreshold, 0.001f);
        airStrafeBuildRange = Mathf.Max(airStrafeBuildRange, 0.001f);
        landingSpeedRetention = Mathf.Clamp01(landingSpeedRetention);
        jumpBufferTime = jumpBufferTime > 0f ? jumpBufferTime : 0.12f;
        coyoteTime = coyoteTime > 0f ? coyoteTime : 0.08f;
        groundedSnapForce = groundedSnapForce > 0f ? groundedSnapForce : 2f;
        walkSpeedMultiplier = Mathf.Clamp(walkSpeedMultiplier, 0.1f, 1f);
        crouchSpeedMultiplier = Mathf.Clamp(crouchSpeedMultiplier, 0.1f, 1f);
        crouchHeight = Mathf.Max(crouchHeight, 0.8f);
        crouchCameraDrop = Mathf.Max(crouchCameraDrop, 0.05f);
        crouchTransitionSpeed = Mathf.Max(crouchTransitionSpeed, 0.1f);
        crouchStepOffsetMultiplier = Mathf.Clamp(crouchStepOffsetMultiplier, 0f, 1f);
    }

    private void EnsureCombatSettings()
    {
        shootDistance = Mathf.Max(shootDistance, 1f);
        baseDamage = Mathf.Max(baseDamage, 1f);
        shootForce = Mathf.Max(shootForce, 0f);
        meleeImpactForce = Mathf.Max(meleeImpactForce, 0f);
        meleeStaminaCost = Mathf.Max(0f, meleeStaminaCost);
        impactMarkerLifetime = Mathf.Max(impactMarkerLifetime, 0.05f);
        medicalUseCooldown = Mathf.Max(0.05f, medicalUseCooldown);
        medicalFeedbackLifetime = Mathf.Max(0.25f, medicalFeedbackLifetime);
        firearmNoiseRadius = Mathf.Max(0f, firearmNoiseRadius);
        meleeNoiseRadius = Mathf.Max(0f, meleeNoiseRadius);
        walkNoiseRadius = Mathf.Max(0f, walkNoiseRadius);
        sprintNoiseRadius = Mathf.Max(walkNoiseRadius, sprintNoiseRadius);
        jumpNoiseRadius = Mathf.Max(0f, jumpNoiseRadius);
        landingNoiseRadius = Mathf.Max(0f, landingNoiseRadius);
        movementNoiseInterval = Mathf.Max(0.05f, movementNoiseInterval);
        crouchNoiseMultiplier = Mathf.Clamp(crouchNoiseMultiplier, 0.1f, 1f);

        if (shootMask.value == 0 || shootMask.value == ~0)
        {
            shootMask = Physics.DefaultRaycastLayers;
        }
    }

    private void CacheStanceDefaults()
    {
        if (characterController != null)
        {
            standingHeight = characterController.height > 0f ? characterController.height : 1.8f;
            standingStepOffset = characterController.stepOffset > 0f ? characterController.stepOffset : 0.3f;
        }
        else
        {
            standingHeight = 1.8f;
            standingStepOffset = 0.3f;
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

    private PrototypeFpsInput GetOrCreateInput()
    {
        PrototypeFpsInput input = GetComponent<PrototypeFpsInput>();
        if (input == null)
        {
            input = gameObject.AddComponent<PrototypeFpsInput>();
        }

        return input;
    }

    private PlayerInteractionState GetOrCreateInteractionState()
    {
        PlayerInteractionState state = GetComponent<PlayerInteractionState>();
        if (state == null)
        {
            state = gameObject.AddComponent<PlayerInteractionState>();
        }

        return state;
    }
}
