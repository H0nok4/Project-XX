using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerWeaponPresentationController : MonoBehaviour
{
    // Prefer explicit optic/scope markers over fallback iron sight markers when multiple poses exist.
    private static readonly string[] AimPoseNames = { "ScopePose", "AdsPose", "AimPose", "IronSightPose" };

    [Header("References")]
    [SerializeField] private PlayerAnimationRigRefs rigRefs;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private Transform primaryViewModel;
    [SerializeField] private Transform secondaryViewModel;
    [SerializeField] private Transform meleeViewModel;

    [Header("Aim")]
    [SerializeField] private Vector3 primaryAdsViewModelLocalPosition = new Vector3(-0.25f, 0.18f, -0.58f);
    [SerializeField] private Vector3 primaryAdsViewModelLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 secondaryAdsViewModelLocalPosition = new Vector3(-0.18f, 0.18f, -0.48f);
    [SerializeField] private Vector3 secondaryAdsViewModelLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 meleeAdsViewModelLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 meleeAdsViewModelLocalEulerAngles = Vector3.zero;

    private GameObject primaryViewModelInstance;
    private GameObject secondaryViewModelInstance;
    private GameObject meleeViewModelInstance;
    private PrototypeWeaponDefinition primaryViewModelSource;
    private PrototypeWeaponDefinition secondaryViewModelSource;
    private PrototypeWeaponDefinition meleeViewModelSource;
    private float currentAimBlend;

    private void Awake()
    {
        ResolveReferences();
        ResolveViewModels();
    }

    private void OnValidate()
    {
        ResolveReferences();
        ResolveViewModels();
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
    }

    public void RefreshPresentation()
    {
        ResolveReferences();
        ResolveViewModels();

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

        UpdateAimPresentation(currentAimBlend);
        UpdateSlotVisibility();
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

    private void UpdateSlotVisibility()
    {
        PlayerWeaponSlotType activeSlot = weaponController != null
            ? weaponController.ActiveWeaponSlotType
            : PlayerWeaponSlotType.None;

        if (primaryViewModel != null)
        {
            primaryViewModel.gameObject.SetActive(activeSlot == PlayerWeaponSlotType.Primary);
        }

        if (secondaryViewModel != null)
        {
            secondaryViewModel.gameObject.SetActive(activeSlot == PlayerWeaponSlotType.Secondary);
        }

        if (meleeViewModel != null)
        {
            meleeViewModel.gameObject.SetActive(activeSlot == PlayerWeaponSlotType.Melee);
        }
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

    private void ClearViewModelAnchor(Transform anchor)
    {
        if (anchor == null)
        {
            return;
        }

        for (int childIndex = anchor.childCount - 1; childIndex >= 0; childIndex--)
        {
            DestroyViewModelObject(anchor.GetChild(childIndex).gameObject);
        }
    }

    private void DestroyViewModelInstance(ref GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        DestroyViewModelObject(instance);
        instance = null;
    }

    private void DestroyViewModelObject(GameObject target)
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
