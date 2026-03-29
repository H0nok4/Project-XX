using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(210)]
[DisallowMultipleComponent]
public sealed class PlayerFpArmsRightHandGripIkController : MonoBehaviour
{
    private static readonly string[] RightHandTargetNames = { "RightHandGrip", "MainHandGrip", "GripPoint" };

    [Header("References")]
    [SerializeField] private PlayerAnimationRigRefs rigRefs;
    [SerializeField] private PlayerStateHub stateHub;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private Transform primaryViewAnchor;
    [SerializeField] private Transform secondaryViewAnchor;
    [SerializeField] private Transform firstPersonArmsRigRoot;
    [SerializeField] private RigBuilder rigBuilder;
    [SerializeField] private Rig rightHandRig;
    [SerializeField] private TwoBoneIKConstraint rightHandConstraint;

    [Header("Arm Bones")]
    [SerializeField] private Transform activeArmatureRoot;
    [SerializeField] private Transform upperArmRight;
    [SerializeField] private Transform lowerArmRight;
    [SerializeField] private Transform handRight;

    [Header("Rig Targets")]
    [SerializeField] private Transform rightHandTarget;
    [SerializeField] private Transform rightHandHint;

    [Header("IK Weights")]
    [SerializeField, Range(0f, 1f)] private float hipWeight = 0.92f;
    [SerializeField, Range(0f, 1f)] private float adsWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float sprintWeight = 0.35f;
    [SerializeField, Range(0f, 1f)] private float reloadWeight = 0.18f;
    [SerializeField, Range(0f, 1f)] private float hipRotationWeight = 0.32f;
    [SerializeField, Range(0f, 1f)] private float adsRotationWeight = 0.55f;
    [SerializeField, Range(0f, 1f)] private float hintWeight = 0.8f;

    [Header("Smoothing")]
    [SerializeField] private float targetFollowSmoothing = 28f;
    [SerializeField] private float weightSmoothing = 16f;

    [Header("Offsets")]
    [SerializeField] private Vector3 targetPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 targetRotationOffsetEuler = Vector3.zero;
    [SerializeField] private Vector3 hintOffset = new Vector3(0.18f, -0.09f, -0.1f);

    private readonly Dictionary<int, Transform> rightHandTargetCache = new Dictionary<int, Transform>();
    private Transform activeViewRoot;
    private Transform activeSourceTarget;
    private float currentRigWeight;

    public bool HasActiveGripTarget => activeSourceTarget != null && currentRigWeight > 0.0001f;

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
        if (!CacheRigState() || stateHub == null)
        {
            return;
        }

        TickRig(stateHub.Snapshot, Time.deltaTime);
    }

    public void TickRig(PlayerRuntimeStateSnapshot snapshot, float deltaTime)
    {
        ResolveReferences();
        if (!CacheRigState())
        {
            return;
        }

        UpdateActiveWeaponTarget(snapshot);
        UpdateRigTargets(deltaTime);
        UpdateRigWeights(snapshot, deltaTime);

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
        rightHandTargetCache.Clear();
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

        if (rightHandRig == null)
        {
            Transform rigTransform = firstPersonArmsRigRoot.Find("FPRightHandIKRig");
            if (rigTransform != null)
            {
                rightHandRig = rigTransform.GetComponent<Rig>();
            }
        }

        if (rightHandConstraint == null && rightHandRig != null)
        {
            rightHandConstraint = rightHandRig.GetComponentInChildren<TwoBoneIKConstraint>(true);
        }

        if (rightHandTarget == null)
        {
            Transform targetTransform = firstPersonArmsRigRoot.Find("FPRightHandIKTarget");
            if (targetTransform != null)
            {
                rightHandTarget = targetTransform;
            }
        }

        if (rightHandHint == null)
        {
            Transform hintTransform = firstPersonArmsRigRoot.Find("FPRightHandIKHint");
            if (hintTransform != null)
            {
                rightHandHint = hintTransform;
            }
        }

        if (!ResolveArmBones())
        {
            return false;
        }

        if (rightHandConstraint != null)
        {
            ConfigureConstraintData();
        }

        EnsureRigLayerRegistered();
        return rightHandRig != null && rightHandConstraint != null && rightHandTarget != null && rightHandHint != null;
    }

    private bool ResolveArmBones()
    {
        if (upperArmRight != null && lowerArmRight != null && handRight != null)
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
        upperArmRight = FindChildRecursive(activeArmatureRoot, "upperarm_r");
        lowerArmRight = FindChildRecursive(activeArmatureRoot, "lowerarm_r");
        handRight = FindChildRecursive(activeArmatureRoot, "hand_r");
        return upperArmRight != null && lowerArmRight != null && handRight != null;
    }

    private void ConfigureConstraintData()
    {
        if (rightHandConstraint == null || upperArmRight == null || lowerArmRight == null || handRight == null || rightHandTarget == null || rightHandHint == null)
        {
            return;
        }

        TwoBoneIKConstraintData data = rightHandConstraint.data;
        data.root = upperArmRight;
        data.mid = lowerArmRight;
        data.tip = handRight;
        data.target = rightHandTarget;
        data.hint = rightHandHint;
        data.targetPositionWeight = 1f;
        data.targetRotationWeight = Mathf.Clamp01(hipRotationWeight);
        data.hintWeight = Mathf.Clamp01(hintWeight);
        rightHandConstraint.data = data;
    }

    private void EnsureRigLayerRegistered()
    {
        if (rigBuilder == null || rightHandRig == null)
        {
            return;
        }

        List<RigLayer> layers = rigBuilder.layers;
        for (int index = 0; index < layers.Count; index++)
        {
            RigLayer existingLayer = layers[index];
            if (existingLayer.rig != rightHandRig)
            {
                continue;
            }

            existingLayer.active = true;
            layers[index] = existingLayer;
            return;
        }

        layers.Add(new RigLayer(rightHandRig, true));
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
            activeSourceTarget = ResolveCachedRightHandTarget(viewRoot);
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

    private Transform ResolveCachedRightHandTarget(Transform viewRoot)
    {
        if (viewRoot == null)
        {
            return null;
        }

        int instanceId = viewRoot.GetInstanceID();
        if (rightHandTargetCache.TryGetValue(instanceId, out Transform cachedTarget) && cachedTarget != null)
        {
            return cachedTarget;
        }

        Transform resolvedTarget = FindNamedChildRecursive(viewRoot, RightHandTargetNames);
        rightHandTargetCache[instanceId] = resolvedTarget;
        return resolvedTarget;
    }

    private void UpdateRigTargets(float deltaTime)
    {
        if (handRight == null || lowerArmRight == null || rightHandTarget == null || rightHandHint == null)
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

        Vector3 desiredTargetPosition = handRight.position;
        Quaternion desiredTargetRotation = handRight.rotation;

        if (activeSourceTarget != null)
        {
            desiredTargetPosition = activeSourceTarget.TransformPoint(targetPositionOffset);
            desiredTargetRotation = activeSourceTarget.rotation * Quaternion.Euler(targetRotationOffsetEuler);
        }

        rightHandTarget.position = desiredTargetPosition;
        rightHandTarget.rotation = desiredTargetRotation;

        Vector3 desiredHintPosition = lowerArmRight.position
            + reference.right * hintOffset.x
            + reference.up * hintOffset.y
            + reference.forward * hintOffset.z;
        rightHandHint.position = desiredHintPosition;
        rightHandHint.rotation = Quaternion.LookRotation(reference.forward, reference.up);
    }

    private void UpdateRigWeights(PlayerRuntimeStateSnapshot snapshot, float deltaTime)
    {
        if (rightHandRig == null || rightHandConstraint == null)
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
                float hold = snapshot.IsReloading
                    ? reloadWeight
                    : snapshot.IsSprinting
                        ? sprintWeight
                        : Mathf.Lerp(hipWeight, adsWeight, snapshot.AimBlend);
                targetWeight = Mathf.Clamp01(hold);
                targetRotationWeight = Mathf.Lerp(hipRotationWeight, adsRotationWeight, snapshot.AimBlend);
                targetHintWeight = hintWeight;
            }
        }

        currentRigWeight = targetWeight;
        rightHandRig.weight = currentRigWeight;

        TwoBoneIKConstraintData data = rightHandConstraint.data;
        data.targetPositionWeight = 1f;
        data.targetRotationWeight = Mathf.Clamp01(targetRotationWeight);
        data.hintWeight = Mathf.Clamp01(targetHintWeight);
        rightHandConstraint.data = data;
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
