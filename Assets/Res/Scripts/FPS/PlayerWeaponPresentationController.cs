using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerWeaponPresentationController : MonoBehaviour
{
    private static readonly string[] AimPoseNames = { "ScopePose", "AdsPose", "AimPose", "IronSightPose" };

    [Header("References")]
    [SerializeField] private PlayerAnimationRigRefs rigRefs;
    [SerializeField] private PlayerStateHub stateHub;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private Transform primaryViewAnchor;
    [SerializeField] private Transform secondaryViewAnchor;
    [SerializeField] private Transform meleeViewAnchor;
    [SerializeField] private Transform firstPersonArmsRigRoot;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private PlayerLookController lookController;

    [Header("ADS Fallback")]
    [SerializeField] private Vector3 primaryAdsLocalPosition = new Vector3(-0.25f, 0.18f, -0.58f);
    [SerializeField] private Vector3 primaryAdsLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 secondaryAdsLocalPosition = new Vector3(-0.18f, 0.18f, -0.48f);
    [SerializeField] private Vector3 secondaryAdsLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 meleeAdsLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 meleeAdsLocalEulerAngles = Vector3.zero;

    [Header("Recoil")]
    [SerializeField] private float fireKickBack = 0.045f;
    [SerializeField] private float fireKickPitch = 5.5f;
    [SerializeField] private float recoilReturnSpeed = 16f;

    [Header("Camera Recoil")]
    [SerializeField] private float screenKickPitchMultiplier = 0.3f;
    [SerializeField] private float minScreenKickPitch = 0.75f;
    [SerializeField] private float maxScreenKickPitch = 2.25f;
    [SerializeField] private float maxScreenKickYaw = 0.55f;
    [SerializeField] private float aimScreenKickMultiplier = 0.82f;

    [Header("Viewmodel Clipping")]
    [SerializeField] private float maxViewmodelPullback = 0.012f;
    [SerializeField] private float preferredNearClipPlane = 0.005f;

    [Header("Action Motion")]
    [SerializeField] private float equipDuration = 0.22f;
    [SerializeField] private float equipDipDistance = 0.08f;
    [SerializeField] private float equipPitch = 10f;
    [SerializeField] private float reloadDuration = 0.36f;
    [SerializeField] private float reloadDipDistance = 0.06f;
    [SerializeField] private float reloadPitch = 16f;
    [SerializeField] private float meleeDuration = 0.18f;
    [SerializeField] private float meleeSwingDistance = 0.08f;
    [SerializeField] private float meleeSwingYaw = 18f;

    [Header("Locomotion Sway")]
    [SerializeField] private float locomotionBobFrequency = 6.6f;
    [SerializeField] private float locomotionBobVerticalAmplitude = 0.018f;
    [SerializeField] private float locomotionBobHorizontalAmplitude = 0.012f;
    [SerializeField] private float locomotionRollAmplitude = 2.4f;
    [SerializeField] private float locomotionPitchAmplitude = 1.7f;
    [SerializeField] private float aimSwayMultiplier = 0.45f;
    [SerializeField] private float locomotionSwaySmoothing = 14f;

    private GameObject primaryInstance;
    private GameObject secondaryInstance;
    private GameObject meleeInstance;
    private PrototypeWeaponDefinition primarySource;
    private PrototypeWeaponDefinition secondarySource;
    private PrototypeWeaponDefinition meleeSource;
    private Vector3 recoilPositionOffset;
    private Vector3 recoilEulerOffset;
    private float equipTimer;
    private float reloadTimer;
    private float meleeTimer;
    private Vector3 firstPersonRigBaseLocalPosition;
    private Quaternion firstPersonRigBaseLocalRotation = Quaternion.identity;
    private Vector3 currentRigPositionOffset;
    private Vector3 currentRigEulerOffset;
    private float locomotionCycle;
    private float currentEquipDuration;
    private float currentReloadDuration;
    private float currentMeleeDuration;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        SyncAnchors();
        SyncViewModels();
        EnsureViewCameraSettings();

        if (stateHub == null || weaponController == null)
        {
            return;
        }

        PlayerRuntimeStateSnapshot snapshot = stateHub.Snapshot;
        HandleTriggers(snapshot);
        UpdateActionTimers(Time.deltaTime);
        UpdateRecoil(Time.deltaTime);
        UpdateViewVisibility(snapshot.ActiveWeaponSlot, ShouldHideWeaponForUtilityAction(snapshot));
        UpdateViewTransforms(snapshot);
        UpdateFirstPersonRigMotion(snapshot, Time.deltaTime);
    }

    public void ApplyHostSettings(
        PlayerAnimationRigRefs hostRigRefs,
        PlayerStateHub hostStateHub,
        PlayerWeaponController hostWeaponController,
        Transform hostPrimaryAnchor,
        Transform hostSecondaryAnchor,
        Transform hostMeleeAnchor)
    {
        if (hostRigRefs != null)
        {
            rigRefs = hostRigRefs;
        }

        if (hostStateHub != null)
        {
            stateHub = hostStateHub;
        }

        if (hostWeaponController != null)
        {
            weaponController = hostWeaponController;
        }

        if (hostPrimaryAnchor != null)
        {
            primaryViewAnchor = hostPrimaryAnchor;
        }

        if (hostSecondaryAnchor != null)
        {
            secondaryViewAnchor = hostSecondaryAnchor;
        }

        if (hostMeleeAnchor != null)
        {
            meleeViewAnchor = hostMeleeAnchor;
        }

        SyncAnchors();
        CacheFirstPersonRigBaseline();
    }

    public void RefreshPresentation()
    {
        ResolveReferences();
        SyncAnchors();
        SyncViewModels();
        recoilPositionOffset = Vector3.zero;
        recoilEulerOffset = Vector3.zero;
        currentEquipDuration = equipDuration;
        currentReloadDuration = reloadDuration;
        currentMeleeDuration = meleeDuration;
        currentRigPositionOffset = Vector3.zero;
        currentRigEulerOffset = Vector3.zero;
        locomotionCycle = 0f;
        PlayerRuntimeStateSnapshot snapshot = stateHub != null ? stateHub.Snapshot : default;
        UpdateViewVisibility(weaponController != null ? weaponController.ActiveWeaponSlotType : PlayerWeaponSlotType.None, ShouldHideWeaponForUtilityAction(snapshot));
        UpdateViewTransforms(snapshot);
        RestoreFirstPersonRigTransform();
    }

    public void ResetPresentationImmediate()
    {
        recoilPositionOffset = Vector3.zero;
        recoilEulerOffset = Vector3.zero;
        equipTimer = 0f;
        reloadTimer = 0f;
        meleeTimer = 0f;
        currentEquipDuration = equipDuration;
        currentReloadDuration = reloadDuration;
        currentMeleeDuration = meleeDuration;
        currentRigPositionOffset = Vector3.zero;
        currentRigEulerOffset = Vector3.zero;
        locomotionCycle = 0f;

        ResetInstanceTransform(primaryInstance);
        ResetInstanceTransform(secondaryInstance);
        ResetInstanceTransform(meleeInstance);
        RestoreFirstPersonRigTransform();
    }

    private void ResolveReferences()
    {
        if (rigRefs == null)
        {
            rigRefs = GetComponent<PlayerAnimationRigRefs>();
        }

        if (stateHub == null)
        {
            stateHub = GetComponent<PlayerStateHub>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<PlayerWeaponController>();
        }

        if (lookController == null)
        {
            lookController = GetComponent<PlayerLookController>();
        }

        if (firstPersonArmsRigRoot == null && rigRefs != null)
        {
            firstPersonArmsRigRoot = rigRefs.FirstPersonArmsRigRoot;
            CacheFirstPersonRigBaseline();
        }

        if (viewCamera == null && rigRefs != null)
        {
            viewCamera = rigRefs.ViewCamera;
        }
    }

    private void SyncAnchors()
    {
        if (rigRefs == null)
        {
            return;
        }

        primaryViewAnchor ??= rigRefs.PrimaryWeaponViewAnchor;
        secondaryViewAnchor ??= rigRefs.SecondaryWeaponViewAnchor;
        meleeViewAnchor ??= rigRefs.MeleeWeaponViewAnchor;
        firstPersonArmsRigRoot ??= rigRefs.FirstPersonArmsRigRoot;
        CacheFirstPersonRigBaseline();
    }

    private void SyncViewModels()
    {
        EnsureWeaponViewModel(weaponController != null ? weaponController.EquippedPrimaryWeapon : null, primaryViewAnchor, ref primaryInstance, ref primarySource);
        EnsureWeaponViewModel(weaponController != null ? weaponController.EquippedSecondaryWeapon : null, secondaryViewAnchor, ref secondaryInstance, ref secondarySource);
        EnsureWeaponViewModel(weaponController != null ? weaponController.EquippedMeleeWeapon : null, meleeViewAnchor, ref meleeInstance, ref meleeSource);
    }

    private void EnsureWeaponViewModel(
        PrototypeWeaponDefinition definition,
        Transform anchor,
        ref GameObject currentInstance,
        ref PrototypeWeaponDefinition currentSource)
    {
        if (anchor == null)
        {
            DestroyViewModelInstance(ref currentInstance);
            currentSource = null;
            return;
        }

        GameObject nextPrefab = definition != null ? definition.FirstPersonViewPrefab : null;
        bool needsRefresh = currentSource != definition || currentInstance == null;
        if (!needsRefresh)
        {
            return;
        }

        DestroyViewModelInstance(ref currentInstance);
        ClearViewModelAnchor(anchor);
        currentSource = definition;

        if (nextPrefab == null)
        {
            return;
        }

        currentInstance = Instantiate(nextPrefab, anchor, false);
        currentInstance.name = nextPrefab.name;
    }

    private void UpdateViewVisibility(PlayerWeaponSlotType activeSlot, bool hideAll)
    {
        if (hideAll)
        {
            SetInstanceActive(primaryInstance, false);
            SetInstanceActive(secondaryInstance, false);
            SetInstanceActive(meleeInstance, false);
            return;
        }

        SetInstanceActive(primaryInstance, activeSlot == PlayerWeaponSlotType.Primary);
        SetInstanceActive(secondaryInstance, activeSlot == PlayerWeaponSlotType.Secondary);
        SetInstanceActive(meleeInstance, activeSlot == PlayerWeaponSlotType.Melee);
    }

    private void UpdateViewTransforms(PlayerRuntimeStateSnapshot snapshot)
    {
        UpdateViewTransform(
            weaponController != null ? weaponController.EquippedPrimaryWeapon : null,
            primaryInstance,
            snapshot.ActiveWeaponSlot == PlayerWeaponSlotType.Primary ? snapshot.AimBlend : 0f,
            primaryAdsLocalPosition,
            primaryAdsLocalEulerAngles,
            snapshot.ActiveWeaponSlot == PlayerWeaponSlotType.Primary,
            snapshot);
        UpdateViewTransform(
            weaponController != null ? weaponController.EquippedSecondaryWeapon : null,
            secondaryInstance,
            snapshot.ActiveWeaponSlot == PlayerWeaponSlotType.Secondary ? snapshot.AimBlend : 0f,
            secondaryAdsLocalPosition,
            secondaryAdsLocalEulerAngles,
            snapshot.ActiveWeaponSlot == PlayerWeaponSlotType.Secondary,
            snapshot);
        UpdateViewTransform(
            weaponController != null ? weaponController.EquippedMeleeWeapon : null,
            meleeInstance,
            snapshot.ActiveWeaponSlot == PlayerWeaponSlotType.Melee ? snapshot.AimBlend : 0f,
            meleeAdsLocalPosition,
            meleeAdsLocalEulerAngles,
            snapshot.ActiveWeaponSlot == PlayerWeaponSlotType.Melee,
            snapshot);
    }

    private void UpdateViewTransform(
        PrototypeWeaponDefinition definition,
        GameObject instance,
        float aimBlend,
        Vector3 fallbackLocalPosition,
        Vector3 fallbackLocalEulerAngles,
        bool isActiveSlot,
        PlayerRuntimeStateSnapshot snapshot)
    {
        if (instance == null)
        {
            return;
        }

        if (!isActiveSlot)
        {
            ResetInstanceTransform(instance);
            return;
        }

        WeaponPresentationProfile profile = GetPresentationProfile(definition);

        bool supportsAds = definition != null && definition.IsFirearmWeapon;
        float clampedAimBlend = supportsAds ? Mathf.Clamp01(aimBlend) : 0f;
        Vector3 targetLocalPosition = Vector3.zero;
        Quaternion targetLocalRotation = Quaternion.identity;

        if (clampedAimBlend > 0f
            && TryGetViewModelAimPose(
                definition,
                instance.transform,
                profile != null ? profile.AdsLocalPosition : fallbackLocalPosition,
                profile != null ? profile.AdsLocalEulerAngles : fallbackLocalEulerAngles,
                out Vector3 adsLocalPosition,
                out Quaternion adsLocalRotation))
        {
            targetLocalPosition = adsLocalPosition;
            targetLocalRotation = adsLocalRotation;
        }

        Vector3 actionPositionOffset = recoilPositionOffset;
        Vector3 actionEulerOffset = recoilEulerOffset;
        actionPositionOffset += EvaluateEquipOffset();
        actionEulerOffset += EvaluateEquipEuler();
        actionPositionOffset += EvaluateReloadOffset();
        actionEulerOffset += EvaluateReloadEuler();
        actionPositionOffset += EvaluateMeleeOffset();
        actionEulerOffset += EvaluateMeleeEuler();
        actionPositionOffset.z = Mathf.Max(actionPositionOffset.z, -Mathf.Max(0f, maxViewmodelPullback));

        Transform instanceTransform = instance.transform;
        instanceTransform.localPosition = Vector3.Lerp(Vector3.zero, targetLocalPosition, clampedAimBlend) + actionPositionOffset;
        instanceTransform.localRotation = Quaternion.Slerp(Quaternion.identity, targetLocalRotation, clampedAimBlend)
            * Quaternion.Euler(actionEulerOffset);
    }

    private void HandleTriggers(PlayerRuntimeStateSnapshot snapshot)
    {
        WeaponPresentationProfile activeProfile = GetActivePresentationProfile();

        if (snapshot.EquipTriggered)
        {
            currentEquipDuration = GetEquipDuration(activeProfile);
            equipTimer = currentEquipDuration;
        }

        if (snapshot.ReloadTriggered)
        {
            currentReloadDuration = GetReloadDuration(activeProfile);
            reloadTimer = currentReloadDuration;
        }

        if (snapshot.FireTriggered)
        {
            if (snapshot.WeaponCategory == PlayerWeaponCategory.Melee)
            {
                currentMeleeDuration = GetMeleeDuration(activeProfile);
                meleeTimer = currentMeleeDuration;
            }
            else
            {
                recoilPositionOffset += new Vector3(0f, 0f, -GetFireKickBack(activeProfile));
                recoilEulerOffset += new Vector3(-GetFireKickPitch(activeProfile), 0f, 0f);
                ApplyScreenRecoil(snapshot, activeProfile);
            }
        }
    }

    private void UpdateActionTimers(float deltaTime)
    {
        equipTimer = Mathf.Max(0f, equipTimer - deltaTime);
        reloadTimer = Mathf.Max(0f, reloadTimer - deltaTime);
        meleeTimer = Mathf.Max(0f, meleeTimer - deltaTime);
    }

    private void UpdateRecoil(float deltaTime)
    {
        float returnBlend = 1f - Mathf.Exp(-GetRecoilReturnSpeed(GetActivePresentationProfile()) * Mathf.Max(0f, deltaTime));
        recoilPositionOffset = Vector3.Lerp(recoilPositionOffset, Vector3.zero, returnBlend);
        recoilEulerOffset = Vector3.Lerp(recoilEulerOffset, Vector3.zero, returnBlend);
    }

    private Vector3 EvaluateEquipOffset()
    {
        WeaponPresentationProfile profile = GetActivePresentationProfile();
        float normalized = EvaluatePulse(equipTimer, currentEquipDuration);
        float dipDistance = GetEquipDipDistance(profile);
        return new Vector3(0f, -normalized * dipDistance * 0.35f, -normalized * dipDistance);
    }

    private Vector3 EvaluateEquipEuler()
    {
        float normalized = EvaluatePulse(equipTimer, currentEquipDuration);
        return new Vector3(normalized * GetEquipPitch(GetActivePresentationProfile()), 0f, 0f);
    }

    private Vector3 EvaluateReloadOffset()
    {
        if (reloadTimer <= 0f)
        {
            return Vector3.zero;
        }

        WeaponPresentationProfile profile = GetActivePresentationProfile();
        float normalized = EvaluatePulse(reloadTimer, currentReloadDuration);
        float dipDistance = GetReloadDipDistance(profile);
        return new Vector3(0f, -normalized * dipDistance * 0.4f, -normalized * dipDistance);
    }

    private Vector3 EvaluateReloadEuler()
    {
        if (reloadTimer <= 0f)
        {
            return Vector3.zero;
        }

        float normalized = EvaluatePulse(reloadTimer, currentReloadDuration);
        return new Vector3(normalized * GetReloadPitch(GetActivePresentationProfile()), 0f, 0f);
    }

    private Vector3 EvaluateMeleeOffset()
    {
        if (meleeTimer <= 0f)
        {
            return Vector3.zero;
        }

        WeaponPresentationProfile profile = GetActivePresentationProfile();
        float normalized = EvaluatePulse(meleeTimer, currentMeleeDuration);
        float swingDistance = GetMeleeSwingDistance(profile);
        return new Vector3(normalized * swingDistance * 0.35f, 0f, -normalized * swingDistance);
    }

    private Vector3 EvaluateMeleeEuler()
    {
        if (meleeTimer <= 0f)
        {
            return Vector3.zero;
        }

        float normalized = EvaluatePulse(meleeTimer, currentMeleeDuration);
        return new Vector3(0f, normalized * GetMeleeSwingYaw(GetActivePresentationProfile()), 0f);
    }

    private static float EvaluatePulse(float timer, float duration)
    {
        if (timer <= 0f || duration <= 0.0001f)
        {
            return 0f;
        }

        float normalizedTime = 1f - Mathf.Clamp01(timer / duration);
        return Mathf.Sin(normalizedTime * Mathf.PI);
    }

    private static void SetInstanceActive(GameObject instance, bool isActive)
    {
        if (instance != null && instance.activeSelf != isActive)
        {
            instance.SetActive(isActive);
        }
    }

    private static void ResetInstanceTransform(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        Transform instanceTransform = instance.transform;
        instanceTransform.localPosition = Vector3.zero;
        instanceTransform.localRotation = Quaternion.identity;
    }

    private static bool ShouldHideWeaponForUtilityAction(PlayerRuntimeStateSnapshot snapshot)
    {
        return snapshot.UpperBodyAction == PlayerUpperBodyAction.Medical
            || snapshot.UpperBodyAction == PlayerUpperBodyAction.Throwable;
    }

    private void UpdateFirstPersonRigMotion(PlayerRuntimeStateSnapshot snapshot, float deltaTime)
    {
        if (firstPersonArmsRigRoot == null)
        {
            return;
        }

        WeaponPresentationProfile activeProfile = GetActivePresentationProfile();

        float moveInputMagnitude = Mathf.Clamp01(snapshot.MoveInput.magnitude);
        float speedFactor = Mathf.Clamp01(snapshot.PlanarSpeed / 4.5f);
        float activeWeight = moveInputMagnitude * speedFactor;
        if (!snapshot.IsGrounded || !snapshot.IsAlive)
        {
            activeWeight = 0f;
        }

        activeWeight *= snapshot.IsAiming ? GetAimSwayMultiplier(activeProfile) : 1f;

        if (activeWeight > 0.001f)
        {
            float strideSpeed = Mathf.Lerp(0.85f, 1.5f, speedFactor);
            locomotionCycle += Mathf.Max(0f, deltaTime) * GetLocomotionBobFrequency(activeProfile) * strideSpeed;
        }

        float bobX = Mathf.Sin(locomotionCycle) * GetLocomotionBobHorizontalAmplitude(activeProfile) * activeWeight;
        float bobY = Mathf.Abs(Mathf.Cos(locomotionCycle * 0.5f)) * GetLocomotionBobVerticalAmplitude(activeProfile) * activeWeight;
        float moveTilt = -snapshot.MoveInput.x * GetLocomotionRollAmplitude(activeProfile) * activeWeight;
        float forwardTilt = snapshot.MoveInput.y * GetLocomotionPitchAmplitude(activeProfile) * activeWeight;

        Vector3 targetPositionOffset = new Vector3(bobX, bobY, 0f);
        Vector3 targetEulerOffset = new Vector3(forwardTilt, 0f, moveTilt);
        float smoothing = 1f - Mathf.Exp(-GetLocomotionSwaySmoothing(activeProfile) * Mathf.Max(0f, deltaTime));
        currentRigPositionOffset = Vector3.Lerp(currentRigPositionOffset, targetPositionOffset, smoothing);
        currentRigEulerOffset = Vector3.Lerp(currentRigEulerOffset, targetEulerOffset, smoothing);

        firstPersonArmsRigRoot.localPosition = firstPersonRigBaseLocalPosition + currentRigPositionOffset;
        firstPersonArmsRigRoot.localRotation = firstPersonRigBaseLocalRotation * Quaternion.Euler(currentRigEulerOffset);
    }

    private void CacheFirstPersonRigBaseline()
    {
        if (firstPersonArmsRigRoot == null)
        {
            return;
        }

        if (firstPersonRigBaseLocalRotation == Quaternion.identity && firstPersonRigBaseLocalPosition == Vector3.zero)
        {
            firstPersonRigBaseLocalPosition = firstPersonArmsRigRoot.localPosition;
            firstPersonRigBaseLocalRotation = firstPersonArmsRigRoot.localRotation;
            return;
        }

        if (currentRigPositionOffset == Vector3.zero && currentRigEulerOffset == Vector3.zero)
        {
            firstPersonRigBaseLocalPosition = firstPersonArmsRigRoot.localPosition;
            firstPersonRigBaseLocalRotation = firstPersonArmsRigRoot.localRotation;
        }
    }

    private void RestoreFirstPersonRigTransform()
    {
        if (firstPersonArmsRigRoot == null)
        {
            return;
        }

        firstPersonArmsRigRoot.localPosition = firstPersonRigBaseLocalPosition;
        firstPersonArmsRigRoot.localRotation = firstPersonRigBaseLocalRotation;
    }

    private void ApplyScreenRecoil(PlayerRuntimeStateSnapshot snapshot, WeaponPresentationProfile profile)
    {
        if (lookController == null)
        {
            return;
        }

        float pitchKick = Mathf.Clamp(
            GetFireKickPitch(profile) * Mathf.Max(0.01f, screenKickPitchMultiplier),
            Mathf.Max(0f, minScreenKickPitch),
            Mathf.Max(minScreenKickPitch, maxScreenKickPitch));
        if (snapshot.IsAiming)
        {
            pitchKick *= Mathf.Clamp(aimScreenKickMultiplier, 0.1f, 1f);
        }

        float yawKick = UnityEngine.Random.Range(-1f, 1f) * Mathf.Max(0f, maxScreenKickYaw);
        lookController.AddRecoilImpulse(pitchKick, yawKick);
    }

    private void EnsureViewCameraSettings()
    {
        if (viewCamera == null || preferredNearClipPlane <= 0f)
        {
            return;
        }

        if (viewCamera.nearClipPlane > preferredNearClipPlane)
        {
            viewCamera.nearClipPlane = preferredNearClipPlane;
        }
    }

    private static void DestroyViewModelInstance(ref GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(instance);
        }
        else
        {
            DestroyImmediate(instance);
        }

        instance = null;
    }

    private static void ClearViewModelAnchor(Transform anchor)
    {
        if (anchor == null)
        {
            return;
        }

        for (int childIndex = anchor.childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform child = anchor.GetChild(childIndex);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private static bool TryGetViewModelAimPose(
        PrototypeWeaponDefinition definition,
        Transform viewModelRoot,
        Vector3 fallbackLocalPosition,
        Vector3 fallbackLocalEulerAngles,
        out Vector3 targetLocalPosition,
        out Quaternion targetLocalRotation)
    {
        Transform aimPose = FindAimPoseTransform(viewModelRoot);
        if (aimPose != null)
        {
            Quaternion poseLocalRotation = Quaternion.Inverse(viewModelRoot.rotation) * aimPose.rotation;
            Vector3 poseLocalPosition = viewModelRoot.InverseTransformPoint(aimPose.position);
            targetLocalRotation = Quaternion.Inverse(poseLocalRotation);
            targetLocalPosition = -(targetLocalRotation * poseLocalPosition);
            return true;
        }

        if (definition != null && definition.HasAdsPoseOverride)
        {
            targetLocalPosition = definition.AdsViewModelLocalPosition;
            targetLocalRotation = Quaternion.Euler(definition.AdsViewModelLocalEulerAngles);
            return true;
        }

        targetLocalPosition = fallbackLocalPosition;
        targetLocalRotation = Quaternion.Euler(fallbackLocalEulerAngles);
        return true;
    }

    private WeaponPresentationProfile GetActivePresentationProfile()
    {
        return GetPresentationProfile(weaponController != null ? weaponController.ActiveWeaponDefinition : null);
    }

    private static WeaponPresentationProfile GetPresentationProfile(PrototypeWeaponDefinition definition)
    {
        return definition != null ? definition.PresentationProfile : null;
    }

    private float GetFireKickBack(WeaponPresentationProfile profile) => profile != null ? profile.FireKickBack : fireKickBack;
    private float GetFireKickPitch(WeaponPresentationProfile profile) => profile != null ? profile.FireKickPitch : fireKickPitch;
    private float GetRecoilReturnSpeed(WeaponPresentationProfile profile) => profile != null ? profile.RecoilReturnSpeed : Mathf.Max(0.01f, recoilReturnSpeed);
    private float GetEquipDuration(WeaponPresentationProfile profile) => profile != null ? profile.EquipDuration : Mathf.Max(0.01f, equipDuration);
    private float GetEquipDipDistance(WeaponPresentationProfile profile) => profile != null ? profile.EquipDipDistance : equipDipDistance;
    private float GetEquipPitch(WeaponPresentationProfile profile) => profile != null ? profile.EquipPitch : equipPitch;
    private float GetReloadDuration(WeaponPresentationProfile profile) => profile != null ? profile.ReloadDuration : Mathf.Max(0.01f, reloadDuration);
    private float GetReloadDipDistance(WeaponPresentationProfile profile) => profile != null ? profile.ReloadDipDistance : reloadDipDistance;
    private float GetReloadPitch(WeaponPresentationProfile profile) => profile != null ? profile.ReloadPitch : reloadPitch;
    private float GetMeleeDuration(WeaponPresentationProfile profile) => profile != null ? profile.MeleeDuration : Mathf.Max(0.01f, meleeDuration);
    private float GetMeleeSwingDistance(WeaponPresentationProfile profile) => profile != null ? profile.MeleeSwingDistance : meleeSwingDistance;
    private float GetMeleeSwingYaw(WeaponPresentationProfile profile) => profile != null ? profile.MeleeSwingYaw : meleeSwingYaw;
    private float GetLocomotionBobFrequency(WeaponPresentationProfile profile) => profile != null ? profile.LocomotionBobFrequency : locomotionBobFrequency;
    private float GetLocomotionBobVerticalAmplitude(WeaponPresentationProfile profile) => profile != null ? profile.LocomotionBobVerticalAmplitude : locomotionBobVerticalAmplitude;
    private float GetLocomotionBobHorizontalAmplitude(WeaponPresentationProfile profile) => profile != null ? profile.LocomotionBobHorizontalAmplitude : locomotionBobHorizontalAmplitude;
    private float GetLocomotionRollAmplitude(WeaponPresentationProfile profile) => profile != null ? profile.LocomotionRollAmplitude : locomotionRollAmplitude;
    private float GetLocomotionPitchAmplitude(WeaponPresentationProfile profile) => profile != null ? profile.LocomotionPitchAmplitude : locomotionPitchAmplitude;
    private float GetAimSwayMultiplier(WeaponPresentationProfile profile) => profile != null ? profile.AimSwayMultiplier : aimSwayMultiplier;
    private float GetLocomotionSwaySmoothing(WeaponPresentationProfile profile) => profile != null ? profile.LocomotionSwaySmoothing : Mathf.Max(0.01f, locomotionSwaySmoothing);

    private static Transform FindAimPoseTransform(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform bestActiveMatch = null;
        int bestActivePriority = int.MaxValue;
        int bestActiveDepth = int.MinValue;
        Transform bestInactiveMatch = null;
        int bestInactivePriority = int.MaxValue;
        int bestInactiveDepth = int.MinValue;

        FindAimPoseRecursive(
            root,
            0,
            ref bestActiveMatch,
            ref bestActivePriority,
            ref bestActiveDepth,
            ref bestInactiveMatch,
            ref bestInactivePriority,
            ref bestInactiveDepth);

        return bestActiveMatch != null ? bestActiveMatch : bestInactiveMatch;
    }

    private static void FindAimPoseRecursive(
        Transform parent,
        int depth,
        ref Transform bestActiveMatch,
        ref int bestActivePriority,
        ref int bestActiveDepth,
        ref Transform bestInactiveMatch,
        ref int bestInactivePriority,
        ref int bestInactiveDepth)
    {
        if (parent == null)
        {
            return;
        }

        for (int childIndex = 0; childIndex < parent.childCount; childIndex++)
        {
            Transform child = parent.GetChild(childIndex);
            int namePriority = GetAimPoseNamePriority(child.name);
            if (namePriority >= 0)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    if (IsBetterAimPoseCandidate(child, namePriority, depth, bestActiveMatch, bestActivePriority, bestActiveDepth))
                    {
                        bestActiveMatch = child;
                        bestActivePriority = namePriority;
                        bestActiveDepth = depth;
                    }
                }
                else if (IsBetterAimPoseCandidate(child, namePriority, depth, bestInactiveMatch, bestInactivePriority, bestInactiveDepth))
                {
                    bestInactiveMatch = child;
                    bestInactivePriority = namePriority;
                    bestInactiveDepth = depth;
                }
            }

            FindAimPoseRecursive(
                child,
                depth + 1,
                ref bestActiveMatch,
                ref bestActivePriority,
                ref bestActiveDepth,
                ref bestInactiveMatch,
                ref bestInactivePriority,
                ref bestInactiveDepth);
        }
    }

    private static int GetAimPoseNamePriority(string transformName)
    {
        if (string.IsNullOrWhiteSpace(transformName))
        {
            return -1;
        }

        for (int index = 0; index < AimPoseNames.Length; index++)
        {
            if (string.Equals(transformName, AimPoseNames[index], StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsBetterAimPoseCandidate(
        Transform candidate,
        int candidatePriority,
        int candidateDepth,
        Transform currentBest,
        int currentPriority,
        int currentDepth)
    {
        if (candidate == null)
        {
            return false;
        }

        if (currentBest == null)
        {
            return true;
        }

        if (candidatePriority != currentPriority)
        {
            return candidatePriority < currentPriority;
        }

        if (candidateDepth != currentDepth)
        {
            return candidateDepth > currentDepth;
        }

        return string.CompareOrdinal(candidate.name, currentBest.name) < 0;
    }
}
