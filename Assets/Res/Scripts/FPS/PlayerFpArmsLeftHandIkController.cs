using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class PlayerFpArmsLeftHandIkController : MonoBehaviour
{
    private static readonly string[] LeftHandTargetNames = { "LeftHandIK", "SupportHandIK", "OffHandIK" };

    [Header("References")]
    [SerializeField] private PlayerAnimationRigRefs rigRefs;
    [SerializeField] private PlayerStateHub stateHub;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private Transform primaryViewAnchor;
    [SerializeField] private Transform secondaryViewAnchor;
    [SerializeField] private Transform firstPersonArmsRigRoot;
    [SerializeField] private RigBuilder rigBuilder;
    [SerializeField] private Rig leftHandRig;
    [SerializeField] private TwoBoneIKConstraint leftHandConstraint;

    [Header("Arm Bones")]
    [SerializeField] private Transform activeArmatureRoot;
    [SerializeField] private Transform upperArmLeft;
    [SerializeField] private Transform lowerArmLeft;
    [SerializeField] private Transform handLeft;

    [Header("Rig Targets")]
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform leftHandHint;

    [Header("IK Weights")]
    [SerializeField, Range(0f, 1f)] private float hipWeight = 0.88f;
    [SerializeField, Range(0f, 1f)] private float adsWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float sprintWeight = 0.42f;
    [SerializeField, Range(0f, 1f)] private float reloadWeight = 0.18f;
    [SerializeField, Range(0f, 1f)] private float hipRotationWeight = 0.25f;
    [SerializeField, Range(0f, 1f)] private float adsRotationWeight = 0.58f;
    [SerializeField, Range(0f, 1f)] private float hintWeight = 0.85f;

    [Header("Smoothing")]
    [SerializeField] private float targetFollowSmoothing = 24f;
    [SerializeField] private float weightSmoothing = 14f;

    [Header("Offsets")]
    [SerializeField] private Vector3 targetPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 targetRotationOffsetEuler = Vector3.zero;
    [SerializeField] private Vector3 hintOffset = new Vector3(-0.18f, -0.06f, -0.05f);

    private readonly Dictionary<int, Transform> leftHandTargetCache = new Dictionary<int, Transform>();
    private Transform activeViewRoot;
    private Transform activeSourceTarget;
    private float currentRigWeight;

    private void Awake()
    {
        ResolveReferences();
        CacheRigState();
    }

    private void Start()
    {
        ResolveReferences();
        CacheRigState();
        RebuildRigGraph();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CacheRigState();
    }

    private void OnValidate()
    {
        ResolveReferences();
        ClampSettings();
        CacheRigState();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        if (!CacheRigState())
        {
            return;
        }

        PlayerRuntimeStateSnapshot snapshot = stateHub != null ? stateHub.Snapshot : default;
        UpdateActiveWeaponTarget(snapshot);
        UpdateRigTargets(Time.deltaTime);
        UpdateRigWeights(snapshot, Time.deltaTime);

        if (Application.isPlaying && rigBuilder != null && rigBuilder.graph.IsValid())
        {
            rigBuilder.SyncLayers();
        }
    }

    public void ApplyHostSettings(
        PlayerAnimationRigRefs hostRigRefs,
        PlayerStateHub hostStateHub,
        PlayerWeaponController hostWeaponController,
        Transform hostPrimaryAnchor,
        Transform hostSecondaryAnchor)
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

        CacheRigState();
        RebuildRigGraph();
    }

    public void RefreshRig()
    {
        leftHandTargetCache.Clear();
        activeViewRoot = null;
        activeSourceTarget = null;
        currentRigWeight = 0f;
        CacheRigState();
        RebuildRigGraph();
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

        if (firstPersonArmsRigRoot == null && rigRefs != null)
        {
            firstPersonArmsRigRoot = rigRefs.FirstPersonArmsRigRoot;
        }

        if (primaryViewAnchor == null && rigRefs != null)
        {
            primaryViewAnchor = rigRefs.PrimaryWeaponViewAnchor;
        }

        if (secondaryViewAnchor == null && rigRefs != null)
        {
            secondaryViewAnchor = rigRefs.SecondaryWeaponViewAnchor;
        }

        if (rigBuilder == null && firstPersonArmsRigRoot != null)
        {
            rigBuilder = firstPersonArmsRigRoot.GetComponent<RigBuilder>();
        }
    }

    private bool CacheRigState()
    {
        ClampSettings();

        if (firstPersonArmsRigRoot == null)
        {
            return false;
        }

        if (rigBuilder == null)
        {
            rigBuilder = firstPersonArmsRigRoot.GetComponent<RigBuilder>();
        }

        if (leftHandRig == null)
        {
            Transform rigTransform = firstPersonArmsRigRoot.Find("FPLeftHandIKRig");
            if (rigTransform != null)
            {
                leftHandRig = rigTransform.GetComponent<Rig>();
            }
        }

        if (leftHandConstraint == null && leftHandRig != null)
        {
            leftHandConstraint = leftHandRig.GetComponentInChildren<TwoBoneIKConstraint>(true);
        }

        if (leftHandTarget == null)
        {
            Transform targetTransform = firstPersonArmsRigRoot.Find("FPLeftHandIKTarget");
            if (targetTransform != null)
            {
                leftHandTarget = targetTransform;
            }
        }

        if (leftHandHint == null)
        {
            Transform hintTransform = firstPersonArmsRigRoot.Find("FPLeftHandIKHint");
            if (hintTransform != null)
            {
                leftHandHint = hintTransform;
            }
        }

        if (!ResolveArmBones())
        {
            return false;
        }

        if (leftHandConstraint != null)
        {
            ConfigureConstraintData();
        }

        EnsureRigLayerRegistered();
        return leftHandRig != null && leftHandConstraint != null && leftHandTarget != null && leftHandHint != null;
    }

    private bool ResolveArmBones()
    {
        if (upperArmLeft != null && lowerArmLeft != null && handLeft != null)
        {
            return true;
        }

        if (firstPersonArmsRigRoot == null)
        {
            return false;
        }

        SkinnedMeshRenderer[] renderers = firstPersonArmsRigRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        SkinnedMeshRenderer activeRenderer = null;
        for (int index = 0; index < renderers.Length; index++)
        {
            if (renderers[index] == null || !renderers[index].gameObject.activeInHierarchy || renderers[index].rootBone == null)
            {
                continue;
            }

            activeRenderer = renderers[index];
            break;
        }

        if (activeRenderer == null)
        {
            return false;
        }

        activeArmatureRoot = activeRenderer.rootBone;
        upperArmLeft = FindChildRecursive(activeArmatureRoot, "upperarm_l");
        lowerArmLeft = FindChildRecursive(activeArmatureRoot, "lowerarm_l");
        handLeft = FindChildRecursive(activeArmatureRoot, "hand_l");
        return upperArmLeft != null && lowerArmLeft != null && handLeft != null;
    }

    private void ConfigureConstraintData()
    {
        if (leftHandConstraint == null || upperArmLeft == null || lowerArmLeft == null || handLeft == null || leftHandTarget == null || leftHandHint == null)
        {
            return;
        }

        TwoBoneIKConstraintData data = leftHandConstraint.data;
        data.root = upperArmLeft;
        data.mid = lowerArmLeft;
        data.tip = handLeft;
        data.target = leftHandTarget;
        data.hint = leftHandHint;
        data.targetPositionWeight = 1f;
        data.targetRotationWeight = Mathf.Clamp01(hipRotationWeight);
        data.hintWeight = Mathf.Clamp01(hintWeight);
        leftHandConstraint.data = data;
    }

    private void EnsureRigLayerRegistered()
    {
        if (rigBuilder == null || leftHandRig == null)
        {
            return;
        }

        List<RigLayer> layers = rigBuilder.layers;
        for (int index = 0; index < layers.Count; index++)
        {
            RigLayer existingLayer = layers[index];
            if (existingLayer.rig != leftHandRig)
            {
                continue;
            }

            existingLayer.active = true;
            layers[index] = existingLayer;
            return;
        }

        layers.Add(new RigLayer(leftHandRig, true));
    }

    private void RebuildRigGraph()
    {
        if (!Application.isPlaying || rigBuilder == null)
        {
            return;
        }

        EnsureRigLayerRegistered();
        rigBuilder.Clear();
        rigBuilder.Build();
        rigBuilder.SyncLayers();
    }

    private void UpdateActiveWeaponTarget(PlayerRuntimeStateSnapshot snapshot)
    {
        Transform viewRoot = ResolveActiveViewRoot(snapshot);
        if (viewRoot != activeViewRoot)
        {
            activeViewRoot = viewRoot;
            activeSourceTarget = ResolveCachedLeftHandTarget(viewRoot);
        }
    }

    private Transform ResolveActiveViewRoot(PlayerRuntimeStateSnapshot snapshot)
    {
        if (!snapshot.HasWeapon || snapshot.WeaponCategory != PlayerWeaponCategory.Firearm)
        {
            return null;
        }

        Transform anchor = snapshot.ActiveWeaponSlot switch
        {
            PlayerWeaponSlotType.Primary => primaryViewAnchor,
            PlayerWeaponSlotType.Secondary => secondaryViewAnchor,
            _ => null
        };

        if (anchor == null || anchor.childCount <= 0)
        {
            return null;
        }

        return anchor.GetChild(0);
    }

    private Transform ResolveCachedLeftHandTarget(Transform viewRoot)
    {
        if (viewRoot == null)
        {
            return null;
        }

        int instanceId = viewRoot.GetInstanceID();
        if (leftHandTargetCache.TryGetValue(instanceId, out Transform cachedTarget) && cachedTarget != null)
        {
            return cachedTarget;
        }

        Transform resolvedTarget = FindNamedChildRecursive(viewRoot, LeftHandTargetNames);
        leftHandTargetCache[instanceId] = resolvedTarget;
        return resolvedTarget;
    }

    private void UpdateRigTargets(float deltaTime)
    {
        if (handLeft == null || lowerArmLeft == null || leftHandTarget == null || leftHandHint == null)
        {
            return;
        }

        Transform reference = rigRefs != null && rigRefs.ViewCamera != null
            ? rigRefs.ViewCamera.transform
            : firstPersonArmsRigRoot;
        if (reference == null)
        {
            reference = transform;
        }

        Vector3 desiredTargetPosition = handLeft.position;
        Quaternion desiredTargetRotation = handLeft.rotation;

        if (activeSourceTarget != null)
        {
            desiredTargetPosition = activeSourceTarget.TransformPoint(targetPositionOffset);
            desiredTargetRotation = activeSourceTarget.rotation * Quaternion.Euler(targetRotationOffsetEuler);
        }

        float targetBlend = DampedBlend(targetFollowSmoothing, deltaTime);
        leftHandTarget.position = Vector3.Lerp(leftHandTarget.position, desiredTargetPosition, targetBlend);
        leftHandTarget.rotation = Quaternion.Slerp(leftHandTarget.rotation, desiredTargetRotation, targetBlend);

        Vector3 desiredHintPosition = lowerArmLeft.position
            + reference.right * hintOffset.x
            + reference.up * hintOffset.y
            + reference.forward * hintOffset.z;
        leftHandHint.position = Vector3.Lerp(leftHandHint.position, desiredHintPosition, targetBlend);
        leftHandHint.rotation = Quaternion.LookRotation(reference.forward, reference.up);
    }

    private void UpdateRigWeights(PlayerRuntimeStateSnapshot snapshot, float deltaTime)
    {
        if (leftHandRig == null || leftHandConstraint == null)
        {
            return;
        }

        float targetWeight = 0f;
        float targetRotationWeight = 0f;
        float targetHintWeight = 0f;

        if (activeSourceTarget != null && snapshot.HasWeapon && snapshot.WeaponCategory == PlayerWeaponCategory.Firearm && snapshot.IsAlive && !snapshot.IsUiFocused)
        {
            if (snapshot.UpperBodyAction == PlayerUpperBodyAction.Weapon || snapshot.IsReloading)
            {
                float holdWeight = snapshot.IsReloading
                    ? reloadWeight
                    : snapshot.IsSprinting
                        ? sprintWeight
                        : Mathf.Lerp(hipWeight, adsWeight, snapshot.AimBlend);
                targetWeight = Mathf.Clamp01(holdWeight);
                targetRotationWeight = Mathf.Lerp(hipRotationWeight, adsRotationWeight, snapshot.AimBlend);
                targetHintWeight = hintWeight;
            }
        }

        currentRigWeight = Mathf.Lerp(currentRigWeight, targetWeight, DampedBlend(weightSmoothing, deltaTime));
        leftHandRig.weight = currentRigWeight;

        TwoBoneIKConstraintData data = leftHandConstraint.data;
        data.targetPositionWeight = 1f;
        data.targetRotationWeight = Mathf.Clamp01(targetRotationWeight);
        data.hintWeight = Mathf.Clamp01(targetHintWeight);
        leftHandConstraint.data = data;
    }

    private void ClampSettings()
    {
        hipWeight = Mathf.Clamp01(hipWeight);
        adsWeight = Mathf.Clamp01(adsWeight);
        sprintWeight = Mathf.Clamp01(sprintWeight);
        reloadWeight = Mathf.Clamp01(reloadWeight);
        hipRotationWeight = Mathf.Clamp01(hipRotationWeight);
        adsRotationWeight = Mathf.Clamp01(adsRotationWeight);
        hintWeight = Mathf.Clamp01(hintWeight);
        targetFollowSmoothing = Mathf.Max(0.01f, targetFollowSmoothing);
        weightSmoothing = Mathf.Max(0.01f, weightSmoothing);
    }

    private static float DampedBlend(float sharpness, float deltaTime)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0.01f, sharpness) * Mathf.Max(0f, deltaTime));
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(child.name, name, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    private static Transform FindNamedChildRecursive(Transform root, IReadOnlyList<string> names)
    {
        if (root == null || names == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            for (int index = 0; index < names.Count; index++)
            {
                if (string.Equals(child.name, names[index], StringComparison.Ordinal))
                {
                    return child;
                }
            }
        }

        return null;
    }
}
