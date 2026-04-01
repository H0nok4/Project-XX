using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerWeaponPresentationController : MonoBehaviour
{
    // Prefer explicit optic/scope markers over fallback iron sight markers when multiple poses exist.
    private static readonly string[] AimPoseNames = { "ScopePose", "AdsPose", "AimPose", "IronSightPose" };
    private static readonly string[] MuzzleNames = { "Muzzle", "muzzle", "BarrelEnd", "MuzzleFlash" };
    private const string WorldWeaponSocketName = "WeaponWorldSocket_RightHand";
    private const string GeneratedWorldMuzzleName = "__GeneratedWorldMuzzle";

    [Header("References")]
    [SerializeField] private PlayerAnimationRigRefs rigRefs;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private Transform primaryViewModel;
    [SerializeField] private Transform secondaryViewModel;
    [SerializeField] private Transform meleeViewModel;
    [SerializeField] private Transform rightHandBone;
    [SerializeField] private Transform worldWeaponSocket;

    [Header("Visibility")]
    [SerializeField] private bool showFirstPersonViewModels;
    [SerializeField] private bool showThirdPersonWeapons = true;

    [Header("Aim")]
    [SerializeField] private Vector3 primaryAdsViewModelLocalPosition = new Vector3(-0.25f, 0.18f, -0.58f);
    [SerializeField] private Vector3 primaryAdsViewModelLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 secondaryAdsViewModelLocalPosition = new Vector3(-0.18f, 0.18f, -0.48f);
    [SerializeField] private Vector3 secondaryAdsViewModelLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 meleeAdsViewModelLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 meleeAdsViewModelLocalEulerAngles = Vector3.zero;

    [Header("Third-Person Weapon Offsets")]
    [SerializeField] private Vector3 primaryWorldLocalPosition = new Vector3(0.02f, 0.01f, 0.03f);
    [SerializeField] private Vector3 primaryWorldLocalEulerAngles = new Vector3(0f, -90f, 0f);
    [SerializeField] private Vector3 secondaryWorldLocalPosition = new Vector3(0.02f, 0.01f, 0.02f);
    [SerializeField] private Vector3 secondaryWorldLocalEulerAngles = new Vector3(0f, -90f, 0f);
    [SerializeField] private Vector3 meleeWorldLocalPosition = new Vector3(0.03f, -0.02f, 0.04f);
    [SerializeField] private Vector3 meleeWorldLocalEulerAngles = new Vector3(0f, 0f, -90f);
    [Header("Third-Person Precision Offsets")]
    [SerializeField] private Vector3 primaryPrecisionWorldLocalPosition = new Vector3(0.006f, 0.026f, 0.012f);
    [SerializeField] private Vector3 primaryPrecisionWorldLocalEulerAngles = new Vector3(-2f, -92f, -8f);
    [SerializeField] private Vector3 secondaryPrecisionWorldLocalPosition = new Vector3(0.004f, 0.022f, 0.012f);
    [SerializeField] private Vector3 secondaryPrecisionWorldLocalEulerAngles = new Vector3(-1f, -94f, -10f);
    [SerializeField] private Vector3 meleePrecisionWorldLocalPosition = new Vector3(0.03f, -0.02f, 0.04f);
    [SerializeField] private Vector3 meleePrecisionWorldLocalEulerAngles = new Vector3(0f, 0f, -90f);
    [SerializeField] private Vector3 worldWeaponLocalScale = Vector3.one;

    private GameObject primaryViewModelInstance;
    private GameObject secondaryViewModelInstance;
    private GameObject meleeViewModelInstance;
    private GameObject primaryWorldWeaponInstance;
    private GameObject secondaryWorldWeaponInstance;
    private GameObject meleeWorldWeaponInstance;
    private PrototypeWeaponDefinition primaryViewModelSource;
    private PrototypeWeaponDefinition secondaryViewModelSource;
    private PrototypeWeaponDefinition meleeViewModelSource;
    private PrototypeWeaponDefinition primaryWorldWeaponSource;
    private PrototypeWeaponDefinition secondaryWorldWeaponSource;
    private PrototypeWeaponDefinition meleeWorldWeaponSource;
    private GameObject activeWorldWeaponInstance;
    private float currentAimBlend;

    public Transform ActiveWorldMuzzle
    {
        get
        {
            RefreshActiveWorldWeaponReference();
            return activeWorldWeaponInstance != null ? FindPreferredMuzzleTransform(activeWorldWeaponInstance.transform) : null;
        }
    }

    private void Awake()
    {
        ResolveReferences();
        ResolveViewModels();
        ResolveWorldWeaponSocket();
    }

    private void OnValidate()
    {
        ResolveReferences();
        ResolveViewModels();
        ResolveWorldWeaponSocket();
        worldWeaponLocalScale.x = Mathf.Max(0.01f, worldWeaponLocalScale.x);
        worldWeaponLocalScale.y = Mathf.Max(0.01f, worldWeaponLocalScale.y);
        worldWeaponLocalScale.z = Mathf.Max(0.01f, worldWeaponLocalScale.z);
    }

    public void ApplyHostSettings(
        Camera hostCamera,
        PlayerWeaponController hostWeaponController,
        Transform hostPrimaryViewModel,
        Transform hostSecondaryViewModel,
        Transform hostMeleeViewModel,
        Vector3 hostPrimaryAdsViewModelLocalPosition,
        Vector3 hostPrimaryAdsViewModelLocalEulerAngles,
        Vector3 hostSecondaryAdsViewModelLocalPosition,
        Vector3 hostSecondaryAdsViewModelLocalEulerAngles,
        Vector3 hostMeleeAdsViewModelLocalPosition,
        Vector3 hostMeleeAdsViewModelLocalEulerAngles)
    {
        if (hostCamera != null)
        {
            viewCamera = hostCamera;
        }

        if (hostWeaponController != null)
        {
            weaponController = hostWeaponController;
        }

        if (hostPrimaryViewModel != null)
        {
            primaryViewModel = hostPrimaryViewModel;
        }

        if (hostSecondaryViewModel != null)
        {
            secondaryViewModel = hostSecondaryViewModel;
        }

        if (hostMeleeViewModel != null)
        {
            meleeViewModel = hostMeleeViewModel;
        }

        primaryAdsViewModelLocalPosition = hostPrimaryAdsViewModelLocalPosition;
        primaryAdsViewModelLocalEulerAngles = hostPrimaryAdsViewModelLocalEulerAngles;
        secondaryAdsViewModelLocalPosition = hostSecondaryAdsViewModelLocalPosition;
        secondaryAdsViewModelLocalEulerAngles = hostSecondaryAdsViewModelLocalEulerAngles;
        meleeAdsViewModelLocalPosition = hostMeleeAdsViewModelLocalPosition;
        meleeAdsViewModelLocalEulerAngles = hostMeleeAdsViewModelLocalEulerAngles;

        ResolveReferences();
        ResolveViewModels();
        ResolveWorldWeaponSocket();
    }

    public void RefreshPresentation()
    {
        ResolveReferences();
        ResolveViewModels();
        ResolveWorldWeaponSocket();

        EnsureWeaponViewModel(
            GetWeaponDefinition(PlayerWeaponSlotType.Primary),
            primaryViewModel,
            ref primaryViewModelInstance,
            ref primaryViewModelSource);
        EnsureWeaponViewModel(
            GetWeaponDefinition(PlayerWeaponSlotType.Secondary),
            secondaryViewModel,
            ref secondaryViewModelInstance,
            ref secondaryViewModelSource);
        EnsureWeaponViewModel(
            GetWeaponDefinition(PlayerWeaponSlotType.Melee),
            meleeViewModel,
            ref meleeViewModelInstance,
            ref meleeViewModelSource);

        EnsureWorldWeaponVisual(
            GetWeaponDefinition(PlayerWeaponSlotType.Primary),
            ref primaryWorldWeaponInstance,
            ref primaryWorldWeaponSource);
        EnsureWorldWeaponVisual(
            GetWeaponDefinition(PlayerWeaponSlotType.Secondary),
            ref secondaryWorldWeaponInstance,
            ref secondaryWorldWeaponSource);
        EnsureWorldWeaponVisual(
            GetWeaponDefinition(PlayerWeaponSlotType.Melee),
            ref meleeWorldWeaponInstance,
            ref meleeWorldWeaponSource);

        UpdateAimPresentation(currentAimBlend);
        UpdateSlotVisibility();
        RefreshActiveWorldWeaponReference();
    }

    public void UpdateAimPresentation(float aimBlend)
    {
        currentAimBlend = Mathf.Clamp01(aimBlend);

        ApplyViewModelAimPose(
            GetWeaponDefinition(PlayerWeaponSlotType.Primary),
            primaryViewModelInstance,
            GetSlotAimBlend(PlayerWeaponSlotType.Primary),
            primaryAdsViewModelLocalPosition,
            primaryAdsViewModelLocalEulerAngles);
        ApplyViewModelAimPose(
            GetWeaponDefinition(PlayerWeaponSlotType.Secondary),
            secondaryViewModelInstance,
            GetSlotAimBlend(PlayerWeaponSlotType.Secondary),
            secondaryAdsViewModelLocalPosition,
            secondaryAdsViewModelLocalEulerAngles);
        ApplyViewModelAimPose(
            GetWeaponDefinition(PlayerWeaponSlotType.Melee),
            meleeViewModelInstance,
            GetSlotAimBlend(PlayerWeaponSlotType.Melee),
            meleeAdsViewModelLocalPosition,
            meleeAdsViewModelLocalEulerAngles);

        ApplyWorldWeaponPose(
            GetWeaponDefinition(PlayerWeaponSlotType.Primary),
            primaryWorldWeaponInstance,
            PlayerWeaponSlotType.Primary);
        ApplyWorldWeaponPose(
            GetWeaponDefinition(PlayerWeaponSlotType.Secondary),
            secondaryWorldWeaponInstance,
            PlayerWeaponSlotType.Secondary);
        ApplyWorldWeaponPose(
            GetWeaponDefinition(PlayerWeaponSlotType.Melee),
            meleeWorldWeaponInstance,
            PlayerWeaponSlotType.Melee);
    }

    public void ResetAimPresentationImmediate()
    {
        currentAimBlend = 0f;
        UpdateAimPresentation(0f);
    }

    private void ResolveReferences()
    {
        if (rigRefs == null)
        {
            rigRefs = GetComponent<PlayerAnimationRigRefs>();
        }

        if (viewCamera == null)
        {
            viewCamera = rigRefs != null ? rigRefs.ViewCamera : GetComponentInChildren<Camera>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<PlayerWeaponController>();
        }

        if (rightHandBone == null)
        {
            Animator animator = rigRefs != null ? rigRefs.CharacterVisualAnimator : null;
            if (animator != null && animator.isHuman)
            {
                rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            }

            if (rightHandBone == null && rigRefs != null && rigRefs.CharacterVisualRigRoot != null)
            {
                rightHandBone = FindChildRecursive(rigRefs.CharacterVisualRigRoot, "hand_r");
            }
        }
    }

    private void ResolveViewModels()
    {
        if (viewCamera == null)
        {
            return;
        }

        if (primaryViewModel == null)
        {
            primaryViewModel = rigRefs != null ? rigRefs.PrimaryWeaponViewAnchor : null;
        }

        if (secondaryViewModel == null)
        {
            secondaryViewModel = rigRefs != null ? rigRefs.SecondaryWeaponViewAnchor : null;
        }

        if (meleeViewModel == null)
        {
            meleeViewModel = rigRefs != null ? rigRefs.MeleeWeaponViewAnchor : null;
        }

        primaryViewModel = GetOrCreateViewModelAnchor(primaryViewModel, "WeaponView_Primary");
        secondaryViewModel = GetOrCreateViewModelAnchor(secondaryViewModel, "WeaponView_Secondary");
        meleeViewModel = GetOrCreateViewModelAnchor(meleeViewModel, "WeaponView_Melee");
    }

    private void ResolveWorldWeaponSocket()
    {
        if (worldWeaponSocket != null || rightHandBone == null)
        {
            return;
        }

        Transform existingSocket = rightHandBone.Find(WorldWeaponSocketName);
        if (existingSocket != null)
        {
            worldWeaponSocket = existingSocket;
            return;
        }

        if (!Application.isPlaying || !rightHandBone.gameObject.scene.IsValid())
        {
            return;
        }

        GameObject socketObject = new GameObject(WorldWeaponSocketName);
        worldWeaponSocket = socketObject.transform;
        worldWeaponSocket.SetParent(rightHandBone, false);
    }

    private void UpdateSlotVisibility()
    {
        PlayerWeaponSlotType activeSlot = weaponController != null
            ? weaponController.ActiveWeaponSlotType
            : PlayerWeaponSlotType.None;

        SetAnchorVisibility(primaryViewModel, showFirstPersonViewModels && activeSlot == PlayerWeaponSlotType.Primary);
        SetAnchorVisibility(secondaryViewModel, showFirstPersonViewModels && activeSlot == PlayerWeaponSlotType.Secondary);
        SetAnchorVisibility(meleeViewModel, showFirstPersonViewModels && activeSlot == PlayerWeaponSlotType.Melee);

        SetObjectVisibility(primaryWorldWeaponInstance, showThirdPersonWeapons && activeSlot == PlayerWeaponSlotType.Primary);
        SetObjectVisibility(secondaryWorldWeaponInstance, showThirdPersonWeapons && activeSlot == PlayerWeaponSlotType.Secondary);
        SetObjectVisibility(meleeWorldWeaponInstance, showThirdPersonWeapons && activeSlot == PlayerWeaponSlotType.Melee);
    }

    private float GetSlotAimBlend(PlayerWeaponSlotType slotType)
    {
        return weaponController != null && weaponController.ActiveWeaponSlotType == slotType
            ? currentAimBlend
            : 0f;
    }

    private PrototypeWeaponDefinition GetWeaponDefinition(PlayerWeaponSlotType slotType)
    {
        if (weaponController == null)
        {
            return null;
        }

        switch (slotType)
        {
            case PlayerWeaponSlotType.Primary:
                return weaponController.EquippedPrimaryWeapon;
            case PlayerWeaponSlotType.Secondary:
                return weaponController.EquippedSecondaryWeapon;
            case PlayerWeaponSlotType.Melee:
                return weaponController.EquippedMeleeWeapon;
            default:
                return null;
        }
    }

    private Transform GetOrCreateViewModelAnchor(Transform currentAnchor, string anchorName)
    {
        if (viewCamera == null)
        {
            return currentAnchor;
        }

        if (currentAnchor != null)
        {
            return currentAnchor;
        }

        Transform found = viewCamera.transform.Find(anchorName);
        if (found != null)
        {
            return found;
        }

        GameObject anchorObject = new GameObject(anchorName);
        anchorObject.transform.SetParent(viewCamera.transform, false);
        return anchorObject.transform;
    }

    private void EnsureWeaponViewModel(
        PrototypeWeaponDefinition definition,
        Transform anchor,
        ref GameObject currentInstance,
        ref PrototypeWeaponDefinition currentSource)
    {
        if (anchor == null)
        {
            DestroyObject(ref currentInstance);
            currentSource = null;
            return;
        }

        GameObject nextPrefab = definition != null ? definition.FirstPersonViewPrefab : null;
        bool needsRefresh = currentSource != definition || currentInstance == null;
        if (!needsRefresh)
        {
            return;
        }

        DestroyObject(ref currentInstance);
        ClearAnchor(anchor);
        currentSource = definition;

        if (nextPrefab == null)
        {
            return;
        }

        currentInstance = Instantiate(nextPrefab, anchor, false);
        currentInstance.name = nextPrefab.name;
    }

    private void EnsureWorldWeaponVisual(
        PrototypeWeaponDefinition definition,
        ref GameObject currentInstance,
        ref PrototypeWeaponDefinition currentSource)
    {
        if (worldWeaponSocket == null)
        {
            DestroyObject(ref currentInstance);
            currentSource = null;
            return;
        }

        GameObject nextPrefab = null;
        if (definition != null)
        {
            nextPrefab = definition.EquippedWorldPrefab != null
                ? definition.EquippedWorldPrefab
                : definition.FirstPersonViewPrefab;
        }

        bool needsRefresh = currentSource != definition || currentInstance == null;
        if (!needsRefresh)
        {
            return;
        }

        DestroyObject(ref currentInstance);
        currentSource = definition;

        if (nextPrefab == null)
        {
            return;
        }

        currentInstance = Instantiate(nextPrefab, worldWeaponSocket, false);
        currentInstance.name = nextPrefab.name;
        PrepareWorldWeaponVisual(currentInstance);
    }

    private void ApplyViewModelAimPose(
        PrototypeWeaponDefinition definition,
        GameObject instance,
        float aimBlend,
        Vector3 fallbackLocalPosition,
        Vector3 fallbackLocalEulerAngles)
    {
        if (instance == null)
        {
            return;
        }

        float clampedBlend = definition != null && definition.IsFirearmWeapon
            ? Mathf.Clamp01(aimBlend)
            : 0f;

        Vector3 targetLocalPosition = Vector3.zero;
        Quaternion targetLocalRotation = Quaternion.identity;
        if (clampedBlend > 0f
            && TryGetViewModelAimPose(definition, instance.transform, fallbackLocalPosition, fallbackLocalEulerAngles, out Vector3 adsLocalPosition, out Quaternion adsLocalRotation))
        {
            targetLocalPosition = adsLocalPosition;
            targetLocalRotation = adsLocalRotation;
        }

        Transform instanceTransform = instance.transform;
        instanceTransform.localPosition = Vector3.Lerp(Vector3.zero, targetLocalPosition, clampedBlend);
        instanceTransform.localRotation = Quaternion.Slerp(Quaternion.identity, targetLocalRotation, clampedBlend);
    }

    private void ApplyWorldWeaponPose(
        PrototypeWeaponDefinition definition,
        GameObject instance,
        PlayerWeaponSlotType slotType)
    {
        if (instance == null)
        {
            return;
        }

        float aimBlend = definition != null && definition.IsFirearmWeapon
            ? GetSlotAimBlend(slotType)
            : 0f;
        GetWorldWeaponPose(definition, slotType, aimBlend, out Vector3 localPosition, out Vector3 localEulerAngles);

        Transform instanceTransform = instance.transform;
        instanceTransform.localPosition = ConvertDesiredWorldOffsetToParentLocal(instanceTransform.parent, localPosition);
        instanceTransform.localRotation = Quaternion.Euler(localEulerAngles);
        instanceTransform.localScale = ConvertDesiredWorldScaleToParentLocal(instanceTransform.parent, worldWeaponLocalScale);
    }

    private void GetWorldWeaponPose(
        PrototypeWeaponDefinition definition,
        PlayerWeaponSlotType slotType,
        float aimBlend,
        out Vector3 localPosition,
        out Vector3 localEulerAngles)
    {
        GetWorldWeaponOffsets(definition, slotType, false, out Vector3 hipPosition, out Vector3 hipEulerAngles);
        GetWorldWeaponOffsets(definition, slotType, true, out Vector3 precisionPosition, out Vector3 precisionEulerAngles);

        float clampedBlend = Mathf.Clamp01(aimBlend);
        float poseBlend = clampedBlend * clampedBlend * (3f - (2f * clampedBlend));
        localPosition = Vector3.Lerp(hipPosition, precisionPosition, poseBlend);
        localEulerAngles = Vector3.Lerp(hipEulerAngles, precisionEulerAngles, poseBlend);
    }

    private void GetWorldWeaponOffsets(
        PrototypeWeaponDefinition definition,
        PlayerWeaponSlotType slotType,
        bool precisionPose,
        out Vector3 localPosition,
        out Vector3 localEulerAngles)
    {
        if (definition != null && definition.IsMeleeWeapon)
        {
            localPosition = precisionPose ? meleePrecisionWorldLocalPosition : meleeWorldLocalPosition;
            localEulerAngles = precisionPose ? meleePrecisionWorldLocalEulerAngles : meleeWorldLocalEulerAngles;
            return;
        }

        if (slotType == PlayerWeaponSlotType.Secondary)
        {
            localPosition = precisionPose ? secondaryPrecisionWorldLocalPosition : secondaryWorldLocalPosition;
            localEulerAngles = precisionPose ? secondaryPrecisionWorldLocalEulerAngles : secondaryWorldLocalEulerAngles;
            return;
        }

        localPosition = precisionPose ? primaryPrecisionWorldLocalPosition : primaryWorldLocalPosition;
        localEulerAngles = precisionPose ? primaryPrecisionWorldLocalEulerAngles : primaryWorldLocalEulerAngles;
    }

    private bool TryGetViewModelAimPose(
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

    private void RefreshActiveWorldWeaponReference()
    {
        PlayerWeaponSlotType activeSlot = weaponController != null
            ? weaponController.ActiveWeaponSlotType
            : PlayerWeaponSlotType.None;

        switch (activeSlot)
        {
            case PlayerWeaponSlotType.Primary:
                activeWorldWeaponInstance = primaryWorldWeaponInstance;
                break;
            case PlayerWeaponSlotType.Secondary:
                activeWorldWeaponInstance = secondaryWorldWeaponInstance;
                break;
            case PlayerWeaponSlotType.Melee:
                activeWorldWeaponInstance = meleeWorldWeaponInstance;
                break;
            default:
                activeWorldWeaponInstance = null;
                break;
        }
    }

    private static Transform FindPreferredMuzzleTransform(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        for (int nameIndex = 0; nameIndex < MuzzleNames.Length; nameIndex++)
        {
            Transform match = FindChildRecursive(root, MuzzleNames[nameIndex]);
            if (match != null)
            {
                return match;
            }
        }

        return FindChildRecursive(root, GeneratedWorldMuzzleName);
    }

    private void PrepareWorldWeaponVisual(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        HideWorldOnlyMarkers(instance.transform);
        EnsureGeneratedWorldMuzzle(instance.transform);
    }

    private static void HideWorldOnlyMarkers(Transform root)
    {
        if (root == null)
        {
            return;
        }

        for (int childIndex = 0; childIndex < root.childCount; childIndex++)
        {
            Transform child = root.GetChild(childIndex);
            if (GetAimPoseNamePriority(child.name) >= 0)
            {
                child.gameObject.SetActive(false);
                continue;
            }

            HideWorldOnlyMarkers(child);
        }
    }

    private static Transform EnsureGeneratedWorldMuzzle(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform existingNamedMuzzle = FindPreferredMuzzleTransform(root);
        if (existingNamedMuzzle != null)
        {
            return existingNamedMuzzle;
        }

        Transform generated = FindChildRecursive(root, GeneratedWorldMuzzleName);
        if (generated != null)
        {
            return generated;
        }

        GameObject generatedObject = new GameObject(GeneratedWorldMuzzleName);
        generated = generatedObject.transform;
        generated.SetParent(root, false);

        if (TryGetCombinedRenderableLocalBounds(root, out Bounds localBounds))
        {
            Vector3 localPosition = localBounds.center;
            localPosition.z = localBounds.max.z + 0.01f;
            generated.localPosition = localPosition;
        }
        else
        {
            generated.localPosition = new Vector3(0f, 0f, 0.35f);
        }

        generated.localRotation = Quaternion.identity;
        return generated;
    }

    private static bool TryGetCombinedRenderableLocalBounds(Transform root, out Bounds localBounds)
    {
        localBounds = default;
        bool hasBounds = false;

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int index = 0; index < meshFilters.Length; index++)
        {
            MeshFilter meshFilter = meshFilters[index];
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            Renderer renderer = meshFilter.GetComponent<Renderer>();
            if (renderer == null)
            {
                continue;
            }

            EncapsulateMeshBounds(
                root,
                meshFilter.transform,
                meshFilter.sharedMesh.bounds,
                ref localBounds,
                ref hasBounds);
        }

        SkinnedMeshRenderer[] skinnedMeshRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int index = 0; index < skinnedMeshRenderers.Length; index++)
        {
            SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[index];
            if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
            {
                continue;
            }

            EncapsulateMeshBounds(
                root,
                skinnedMeshRenderer.transform,
                skinnedMeshRenderer.sharedMesh.bounds,
                ref localBounds,
                ref hasBounds);
        }

        return hasBounds;
    }

    private static void EncapsulateMeshBounds(
        Transform root,
        Transform meshTransform,
        Bounds meshBounds,
        ref Bounds combinedBounds,
        ref bool hasBounds)
    {
        Matrix4x4 localToRootMatrix = root.worldToLocalMatrix * meshTransform.localToWorldMatrix;
        Vector3 center = meshBounds.center;
        Vector3 extents = meshBounds.extents;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 localCorner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 rootLocalCorner = localToRootMatrix.MultiplyPoint3x4(localCorner);
                    if (!hasBounds)
                    {
                        combinedBounds = new Bounds(rootLocalCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(rootLocalCorner);
                    }
                }
            }
        }
    }

    private static Vector3 ConvertDesiredWorldOffsetToParentLocal(Transform parent, Vector3 desiredWorldOffset)
    {
        Vector3 parentScale = GetSafeLossyScale(parent);
        return new Vector3(
            desiredWorldOffset.x / parentScale.x,
            desiredWorldOffset.y / parentScale.y,
            desiredWorldOffset.z / parentScale.z);
    }

    private static Vector3 ConvertDesiredWorldScaleToParentLocal(Transform parent, Vector3 desiredWorldScale)
    {
        Vector3 parentScale = GetSafeLossyScale(parent);
        return new Vector3(
            desiredWorldScale.x / parentScale.x,
            desiredWorldScale.y / parentScale.y,
            desiredWorldScale.z / parentScale.z);
    }

    private static Vector3 GetSafeLossyScale(Transform target)
    {
        if (target == null)
        {
            return Vector3.one;
        }

        Vector3 lossyScale = target.lossyScale;
        return new Vector3(
            Mathf.Max(0.0001f, Mathf.Abs(lossyScale.x)),
            Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y)),
            Mathf.Max(0.0001f, Mathf.Abs(lossyScale.z)));
    }

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

    private void ClearAnchor(Transform anchor)
    {
        if (anchor == null)
        {
            return;
        }

        for (int childIndex = anchor.childCount - 1; childIndex >= 0; childIndex--)
        {
            DestroyObject(anchor.GetChild(childIndex).gameObject);
        }
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        if (string.Equals(parent.name, childName, StringComparison.Ordinal))
        {
            return parent;
        }

        for (int childIndex = 0; childIndex < parent.childCount; childIndex++)
        {
            Transform match = FindChildRecursive(parent.GetChild(childIndex), childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void SetAnchorVisibility(Transform anchor, bool visible)
    {
        if (anchor != null && anchor.gameObject.activeSelf != visible)
        {
            anchor.gameObject.SetActive(visible);
        }
    }

    private static void SetObjectVisibility(GameObject target, bool visible)
    {
        if (target != null && target.activeSelf != visible)
        {
            target.SetActive(visible);
        }
    }

    private void DestroyObject(ref GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }

        target = null;
    }

    private void DestroyObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
            return;
        }

        DestroyImmediate(target);
    }
}
