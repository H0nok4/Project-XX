using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerFullBodyAnimatorDriver : MonoBehaviour
{
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int AimBlendHash = Animator.StringToHash("AimBlend");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
    private static readonly int WeaponSlotHash = Animator.StringToHash("WeaponSlot");
    private static readonly int WeaponCategoryHash = Animator.StringToHash("WeaponCategory");
    private static readonly int IsFacingCameraYawHash = Animator.StringToHash("IsFacingCameraYaw");
    private static readonly int CharacterYawDeltaToCameraHash = Animator.StringToHash("CharacterYawDeltaToCamera");
    private static readonly int HasWeaponHash = Animator.StringToHash("HasWeapon");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int UpperBodyActionHash = Animator.StringToHash("UpperBodyAction");
    private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");
    private static readonly int LandTriggerHash = Animator.StringToHash("Land");
    private static readonly int FireTriggerHash = Animator.StringToHash("Fire");
    private static readonly int ReloadTriggerHash = Animator.StringToHash("Reload");
    private static readonly int EquipTriggerHash = Animator.StringToHash("Equip");
    private static readonly int MedicalTriggerHash = Animator.StringToHash("Medical");
    private static readonly int ThrowTriggerHash = Animator.StringToHash("Throw");

    [Header("References")]
    [SerializeField] private PlayerAnimationRigRefs rigRefs;
    [SerializeField] private PlayerStateHub stateHub;
    [SerializeField] private Animator characterAnimator;

    [Header("Precision Aim Pose")]
    [SerializeField] private bool enablePrecisionAimPose = true;
    [SerializeField] private Vector3 chestPrecisionLocalEuler = new Vector3(-4f, 6f, 0f);
    [SerializeField] private Vector3 upperChestPrecisionLocalEuler = new Vector3(-6f, 8f, 0f);
    [SerializeField] private Vector3 neckPrecisionLocalEuler = new Vector3(-1f, 2f, 0f);
    [SerializeField] private Vector3 headPrecisionLocalEuler = new Vector3(-0.5f, 1f, 0f);
    [SerializeField] private Vector3 leftShoulderPrecisionLocalEuler = new Vector3(0f, -4f, 6f);
    [SerializeField] private Vector3 rightShoulderPrecisionLocalEuler = new Vector3(0f, 3f, -5f);
    [SerializeField] private Vector3 leftUpperArmPrecisionLocalEuler = new Vector3(-2f, -6f, 8f);
    [SerializeField] private Vector3 rightUpperArmPrecisionLocalEuler = new Vector3(1f, 4f, -7f);

    private Transform chestBone;
    private Transform upperChestBone;
    private Transform neckBone;
    private Transform headBone;
    private Transform leftShoulderBone;
    private Transform rightShoulderBone;
    private Transform leftUpperArmBone;
    private Transform rightUpperArmBone;

    private void Awake()
    {
        ResolveReferences();
        ApplyAnimatorSettings();
    }

    private void OnValidate()
    {
        ResolveReferences();
        ApplyAnimatorSettings();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        if (characterAnimator == null || stateHub == null)
        {
            return;
        }

        PlayerRuntimeStateSnapshot snapshot = stateHub.Snapshot;
        bool hasReadyFirearmPose = snapshot.IsAlive
            && !snapshot.IsSprinting
            && snapshot.HasWeapon
            && snapshot.WeaponCategory == PlayerWeaponCategory.Firearm;
        characterAnimator.SetFloat(MoveXHash, snapshot.BodyMoveX);
        characterAnimator.SetFloat(MoveYHash, snapshot.BodyMoveY);
        characterAnimator.SetFloat(MoveSpeedHash, snapshot.PlanarSpeed);
        characterAnimator.SetFloat(VerticalSpeedHash, snapshot.VelocityY);
        characterAnimator.SetFloat(AimBlendHash, snapshot.AimBlend);
        characterAnimator.SetBool(IsGroundedHash, snapshot.IsGrounded);
        characterAnimator.SetBool(IsCrouchingHash, snapshot.IsCrouching);
        characterAnimator.SetBool(IsSprintingHash, snapshot.IsSprinting);
        characterAnimator.SetBool(IsAimingHash, hasReadyFirearmPose);
        characterAnimator.SetInteger(WeaponSlotHash, (int)snapshot.ActiveWeaponSlot);
        characterAnimator.SetInteger(WeaponCategoryHash, (int)snapshot.WeaponCategory);
        characterAnimator.SetBool(IsFacingCameraYawHash, snapshot.IsFacingCameraYaw);
        characterAnimator.SetFloat(CharacterYawDeltaToCameraHash, snapshot.CharacterYawDeltaToCamera);
        characterAnimator.SetBool(HasWeaponHash, snapshot.HasWeapon);
        characterAnimator.SetBool(IsDeadHash, !snapshot.IsAlive);
        characterAnimator.SetInteger(UpperBodyActionHash, (int)snapshot.UpperBodyAction);

        TriggerIfTrue(JumpTriggerHash, snapshot.JumpTriggered);
        TriggerIfTrue(LandTriggerHash, snapshot.LandTriggered);
        TriggerIfTrue(FireTriggerHash, snapshot.FireTriggered);
        TriggerIfTrue(ReloadTriggerHash, snapshot.ReloadTriggered);
        TriggerIfTrue(EquipTriggerHash, snapshot.EquipTriggered);
        TriggerIfTrue(MedicalTriggerHash, snapshot.MedicalTriggered);
        TriggerIfTrue(ThrowTriggerHash, snapshot.ThrowTriggered);

        ApplyPrecisionAimPose(snapshot);
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

        if (characterAnimator == null && rigRefs != null)
        {
            characterAnimator = rigRefs.CharacterVisualAnimator;
        }

        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>(true);
        }

        ResolveHumanoidBones();
    }

    private void ApplyAnimatorSettings()
    {
        if (characterAnimator == null)
        {
            return;
        }

        characterAnimator.applyRootMotion = false;
    }

    private void ResolveHumanoidBones()
    {
        if (characterAnimator == null)
        {
            return;
        }

        Transform searchRoot = rigRefs != null ? rigRefs.CharacterVisualRigRoot : characterAnimator.transform;
        chestBone = ResolveAimBone(chestBone, searchRoot, HumanBodyBones.Chest, "spine_02");
        upperChestBone = ResolveAimBone(upperChestBone, searchRoot, HumanBodyBones.UpperChest, "spine_03");
        neckBone = ResolveAimBone(neckBone, searchRoot, HumanBodyBones.Neck, "neck_01");
        headBone = ResolveAimBone(headBone, searchRoot, HumanBodyBones.Head, "Head");
        leftShoulderBone = ResolveAimBone(leftShoulderBone, searchRoot, HumanBodyBones.LeftShoulder, "clavicle_l");
        rightShoulderBone = ResolveAimBone(rightShoulderBone, searchRoot, HumanBodyBones.RightShoulder, "clavicle_r");
        leftUpperArmBone = ResolveAimBone(leftUpperArmBone, searchRoot, HumanBodyBones.LeftUpperArm, "upperarm_l");
        rightUpperArmBone = ResolveAimBone(rightUpperArmBone, searchRoot, HumanBodyBones.RightUpperArm, "upperarm_r");
    }

    private Transform ResolveAimBone(Transform existingBone, Transform searchRoot, HumanBodyBones humanoidBone, string fallbackName)
    {
        if (existingBone != null)
        {
            return existingBone;
        }

        if (characterAnimator != null && characterAnimator.isHuman)
        {
            Transform humanoidTransform = characterAnimator.GetBoneTransform(humanoidBone);
            if (humanoidTransform != null)
            {
                return humanoidTransform;
            }
        }

        return FindChildRecursive(searchRoot, fallbackName);
    }

    private void ApplyPrecisionAimPose(PlayerRuntimeStateSnapshot snapshot)
    {
        if (!enablePrecisionAimPose || characterAnimator == null)
        {
            return;
        }

        if (!snapshot.IsAlive
            || !snapshot.HasWeapon
            || snapshot.WeaponCategory != PlayerWeaponCategory.Firearm
            || snapshot.UpperBodyAction != PlayerUpperBodyAction.Weapon)
        {
            return;
        }

        float precisionWeight = snapshot.AimBlend;
        if (precisionWeight <= 0.0001f)
        {
            return;
        }

        float smoothedWeight = precisionWeight * precisionWeight * (3f - (2f * precisionWeight));
        ApplyBoneOffset(chestBone, chestPrecisionLocalEuler, smoothedWeight);
        ApplyBoneOffset(upperChestBone, upperChestPrecisionLocalEuler, smoothedWeight);
        ApplyBoneOffset(neckBone, neckPrecisionLocalEuler, smoothedWeight);
        ApplyBoneOffset(headBone, headPrecisionLocalEuler, smoothedWeight);
        ApplyBoneOffset(leftShoulderBone, leftShoulderPrecisionLocalEuler, smoothedWeight);
        ApplyBoneOffset(rightShoulderBone, rightShoulderPrecisionLocalEuler, smoothedWeight);
        ApplyBoneOffset(leftUpperArmBone, leftUpperArmPrecisionLocalEuler, smoothedWeight);
        ApplyBoneOffset(rightUpperArmBone, rightUpperArmPrecisionLocalEuler, smoothedWeight);
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        if (parent.name == childName)
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

    private static void ApplyBoneOffset(Transform bone, Vector3 localEulerOffset, float weight)
    {
        if (bone == null || weight <= 0.0001f || localEulerOffset.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion offset = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(localEulerOffset), weight);
        bone.localRotation = bone.localRotation * offset;
    }

    private void TriggerIfTrue(int triggerHash, bool shouldTrigger)
    {
        if (shouldTrigger)
        {
            characterAnimator.SetTrigger(triggerHash);
        }
    }
}
