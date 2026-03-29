using System;
using UnityEngine;

[DefaultExecutionOrder(150)]
[DisallowMultipleComponent]
public sealed class PlayerFpArmsRightHandPoseCorrector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimationRigRefs rigRefs;
    [SerializeField] private PlayerStateHub stateHub;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private PlayerFpArmsRightHandGripIkController rightHandGripIkController;
    [SerializeField] private Transform firstPersonArmsRigRoot;

    [Header("Arm Bones")]
    [SerializeField] private Transform activeArmatureRoot;
    [SerializeField] private Transform clavicleRight;
    [SerializeField] private Transform upperArmRight;
    [SerializeField] private Transform lowerArmRight;
    [SerializeField] private Transform handRight;

    [Header("Pose Weights")]
    [SerializeField, Range(0f, 1f)] private float holdWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float sprintWeight = 0.45f;
    [SerializeField, Range(0f, 1f)] private float reloadWeight = 0.18f;
    [SerializeField] private float weightSmoothing = 14f;

    [Header("Primary Hip Pose")]
    [SerializeField] private Vector3 primaryHipClavicleEuler = new Vector3(-4f, -6f, 8f);
    [SerializeField] private Vector3 primaryHipUpperArmEuler = new Vector3(-12f, -40f, -22f);
    [SerializeField] private Vector3 primaryHipLowerArmEuler = new Vector3(8f, 22f, 8f);
    [SerializeField] private Vector3 primaryHipHandEuler = new Vector3(13f, -3f, -23f);

    [Header("Primary ADS Pose")]
    [SerializeField] private Vector3 primaryAdsClavicleEuler = new Vector3(-2f, -4f, 6f);
    [SerializeField] private Vector3 primaryAdsUpperArmEuler = new Vector3(-8f, -30f, -16f);
    [SerializeField] private Vector3 primaryAdsLowerArmEuler = new Vector3(6f, 14f, 6f);
    [SerializeField] private Vector3 primaryAdsHandEuler = new Vector3(10f, -2f, -18f);

    [Header("Secondary Hip Pose")]
    [SerializeField] private Vector3 secondaryHipClavicleEuler = new Vector3(-2f, -3f, 5f);
    [SerializeField] private Vector3 secondaryHipUpperArmEuler = new Vector3(-9f, -18f, -18f);
    [SerializeField] private Vector3 secondaryHipLowerArmEuler = new Vector3(2f, 8f, 8f);
    [SerializeField] private Vector3 secondaryHipHandEuler = new Vector3(5f, 0f, -10f);

    [Header("Secondary ADS Pose")]
    [SerializeField] private Vector3 secondaryAdsClavicleEuler = new Vector3(-1f, -2f, 4f);
    [SerializeField] private Vector3 secondaryAdsUpperArmEuler = new Vector3(-6f, -14f, -14f);
    [SerializeField] private Vector3 secondaryAdsLowerArmEuler = new Vector3(1f, 5f, 6f);
    [SerializeField] private Vector3 secondaryAdsHandEuler = new Vector3(3f, 0f, -7f);

    private float currentWeight;

    private void Awake()
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
        PoseOffsets offsets = EvaluatePose(snapshot);
        currentWeight = Mathf.Lerp(currentWeight, EvaluateWeight(snapshot), DampedBlend(weightSmoothing, Time.deltaTime));
        ApplyOffsets(offsets, currentWeight);
    }

    public void ApplyHostSettings(
        PlayerAnimationRigRefs hostRigRefs,
        PlayerStateHub hostStateHub,
        PlayerWeaponController hostWeaponController)
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

        if (rightHandGripIkController == null)
        {
            rightHandGripIkController = GetComponent<PlayerFpArmsRightHandGripIkController>();
        }

        CacheRigState();
    }

    public void RefreshPose()
    {
        currentWeight = 0f;
        CacheRigState();
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

        if (rightHandGripIkController == null)
        {
            rightHandGripIkController = GetComponent<PlayerFpArmsRightHandGripIkController>();
        }

        if (firstPersonArmsRigRoot == null && rigRefs != null)
        {
            firstPersonArmsRigRoot = rigRefs.FirstPersonArmsRigRoot;
        }
    }

    private bool CacheRigState()
    {
        ClampSettings();
        if (clavicleRight != null && upperArmRight != null && lowerArmRight != null && handRight != null)
        {
            return true;
        }

        if (firstPersonArmsRigRoot == null)
        {
            return false;
        }

        SkinnedMeshRenderer[] renderers = firstPersonArmsRigRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int index = 0; index < renderers.Length; index++)
        {
            SkinnedMeshRenderer renderer = renderers[index];
            if (renderer == null || !renderer.gameObject.activeInHierarchy || renderer.rootBone == null)
            {
                continue;
            }

            activeArmatureRoot = renderer.rootBone;
            clavicleRight = FindChildRecursive(activeArmatureRoot, "clavicle_r");
            upperArmRight = FindChildRecursive(activeArmatureRoot, "upperarm_r");
            lowerArmRight = FindChildRecursive(activeArmatureRoot, "lowerarm_r");
            handRight = FindChildRecursive(activeArmatureRoot, "hand_r");
            break;
        }

        return clavicleRight != null && upperArmRight != null && lowerArmRight != null && handRight != null;
    }

    private PoseOffsets EvaluatePose(PlayerRuntimeStateSnapshot snapshot)
    {
        if (!snapshot.HasWeapon || snapshot.WeaponCategory != PlayerWeaponCategory.Firearm || !snapshot.IsAlive)
        {
            return default;
        }

        return snapshot.ActiveWeaponSlot switch
        {
            PlayerWeaponSlotType.Secondary => PoseOffsets.Lerp(
                secondaryHipClavicleEuler,
                secondaryHipUpperArmEuler,
                secondaryHipLowerArmEuler,
                secondaryHipHandEuler,
                secondaryAdsClavicleEuler,
                secondaryAdsUpperArmEuler,
                secondaryAdsLowerArmEuler,
                secondaryAdsHandEuler,
                snapshot.AimBlend),
            _ => PoseOffsets.Lerp(
                primaryHipClavicleEuler,
                primaryHipUpperArmEuler,
                primaryHipLowerArmEuler,
                primaryHipHandEuler,
                primaryAdsClavicleEuler,
                primaryAdsUpperArmEuler,
                primaryAdsLowerArmEuler,
                primaryAdsHandEuler,
                snapshot.AimBlend)
        };
    }

    private float EvaluateWeight(PlayerRuntimeStateSnapshot snapshot)
    {
        if (!snapshot.HasWeapon || snapshot.WeaponCategory != PlayerWeaponCategory.Firearm || !snapshot.IsAlive || snapshot.IsUiFocused)
        {
            return 0f;
        }

        if (rightHandGripIkController != null && rightHandGripIkController.HasActiveGripTarget)
        {
            return 0f;
        }

        if (snapshot.UpperBodyAction == PlayerUpperBodyAction.Medical || snapshot.UpperBodyAction == PlayerUpperBodyAction.Throwable)
        {
            return 0f;
        }

        if (snapshot.IsReloading)
        {
            return reloadWeight;
        }

        if (snapshot.IsSprinting)
        {
            return sprintWeight;
        }

        return holdWeight;
    }

    private void ApplyOffsets(PoseOffsets offsets, float weight)
    {
        if (weight <= 0.0001f)
        {
            return;
        }

        ApplyOffset(clavicleRight, offsets.Clavicle, weight);
        ApplyOffset(upperArmRight, offsets.UpperArm, weight);
        ApplyOffset(lowerArmRight, offsets.LowerArm, weight);
        ApplyOffset(handRight, offsets.Hand, weight);
    }

    private static void ApplyOffset(Transform bone, Vector3 eulerOffset, float weight)
    {
        if (bone == null)
        {
            return;
        }

        Quaternion correction = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(eulerOffset), Mathf.Clamp01(weight));
        bone.localRotation = bone.localRotation * correction;
    }

    private void ClampSettings()
    {
        holdWeight = Mathf.Clamp01(holdWeight);
        sprintWeight = Mathf.Clamp01(sprintWeight);
        reloadWeight = Mathf.Clamp01(reloadWeight);
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

    private readonly struct PoseOffsets
    {
        public readonly Vector3 Clavicle;
        public readonly Vector3 UpperArm;
        public readonly Vector3 LowerArm;
        public readonly Vector3 Hand;

        public PoseOffsets(Vector3 clavicle, Vector3 upperArm, Vector3 lowerArm, Vector3 hand)
        {
            Clavicle = clavicle;
            UpperArm = upperArm;
            LowerArm = lowerArm;
            Hand = hand;
        }

        public static PoseOffsets Lerp(
            Vector3 hipClavicle,
            Vector3 hipUpperArm,
            Vector3 hipLowerArm,
            Vector3 hipHand,
            Vector3 adsClavicle,
            Vector3 adsUpperArm,
            Vector3 adsLowerArm,
            Vector3 adsHand,
            float blend)
        {
            float clampedBlend = Mathf.Clamp01(blend);
            return new PoseOffsets(
                Vector3.Lerp(hipClavicle, adsClavicle, clampedBlend),
                Vector3.Lerp(hipUpperArm, adsUpperArm, clampedBlend),
                Vector3.Lerp(hipLowerArm, adsLowerArm, clampedBlend),
                Vector3.Lerp(hipHand, adsHand, clampedBlend));
        }
    }
}
