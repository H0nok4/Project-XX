using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerFpArmsAnimatorDriver : MonoBehaviour
{
    private static readonly int AimBlendHash = Animator.StringToHash("AimBlend");
    private static readonly int SprintBlendHash = Animator.StringToHash("SprintBlend");
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int EquipSlotHash = Animator.StringToHash("EquipSlot");
    private static readonly int WeaponTypeHash = Animator.StringToHash("WeaponType");
    private static readonly int UpperBodyActionHash = Animator.StringToHash("UpperBodyAction");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
    private static readonly int HasWeaponHash = Animator.StringToHash("HasWeapon");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int FireTriggerHash = Animator.StringToHash("Fire");
    private static readonly int ReloadTriggerHash = Animator.StringToHash("Reload");
    private static readonly int MeleeTriggerHash = Animator.StringToHash("Melee");
    private static readonly int EquipTriggerHash = Animator.StringToHash("Equip");
    private static readonly int MedicalTriggerHash = Animator.StringToHash("Medical");
    private static readonly int ThrowTriggerHash = Animator.StringToHash("Throw");

    [Header("References")]
    [SerializeField] private PlayerAnimationRigRefs rigRefs;
    [SerializeField] private PlayerStateHub stateHub;
    [SerializeField] private Animator armsAnimator;

    [Header("Locomotion Hold")]
    [SerializeField, Range(0f, 1f)] private float weaponHoldBlend = 0.82f;

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
        if (armsAnimator == null || stateHub == null)
        {
            return;
        }

        PlayerRuntimeStateSnapshot snapshot = stateHub.Snapshot;
        float locomotionAimBlend = snapshot.HasWeapon
            ? Mathf.Max(snapshot.AimBlend, weaponHoldBlend)
            : snapshot.AimBlend;
        armsAnimator.SetFloat(AimBlendHash, locomotionAimBlend);
        armsAnimator.SetFloat(SprintBlendHash, snapshot.IsSprinting ? 1f : 0f);
        armsAnimator.SetFloat(MoveSpeedHash, snapshot.PlanarSpeed);
        armsAnimator.SetInteger(EquipSlotHash, (int)snapshot.ActiveWeaponSlot);
        armsAnimator.SetInteger(WeaponTypeHash, (int)snapshot.WeaponCategory);
        armsAnimator.SetInteger(UpperBodyActionHash, (int)snapshot.UpperBodyAction);
        armsAnimator.SetBool(IsSprintingHash, snapshot.IsSprinting);
        armsAnimator.SetBool(HasWeaponHash, snapshot.HasWeapon);
        armsAnimator.SetBool(IsDeadHash, !snapshot.IsAlive);

        if (snapshot.FireTriggered)
        {
            if (snapshot.WeaponCategory == PlayerWeaponCategory.Melee)
            {
                armsAnimator.SetTrigger(MeleeTriggerHash);
            }
            else
            {
                armsAnimator.SetTrigger(FireTriggerHash);
            }
        }

        if (snapshot.ReloadTriggered)
        {
            armsAnimator.SetTrigger(ReloadTriggerHash);
        }

        if (snapshot.EquipTriggered)
        {
            armsAnimator.SetTrigger(EquipTriggerHash);
        }

        if (snapshot.MedicalTriggered)
        {
            armsAnimator.SetTrigger(MedicalTriggerHash);
        }

        if (snapshot.ThrowTriggered)
        {
            armsAnimator.SetTrigger(ThrowTriggerHash);
        }
    }

    public void ApplyHostSettings(PlayerAnimationRigRefs hostRigRefs, PlayerStateHub hostStateHub, Animator hostAnimator)
    {
        if (hostRigRefs != null)
        {
            rigRefs = hostRigRefs;
        }

        if (hostStateHub != null)
        {
            stateHub = hostStateHub;
        }

        if (hostAnimator != null)
        {
            armsAnimator = hostAnimator;
        }

        ApplyAnimatorSettings();
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

        if (armsAnimator == null && rigRefs != null)
        {
            armsAnimator = rigRefs.FirstPersonArmsAnimator;
        }

        if (armsAnimator == null && rigRefs != null && rigRefs.FirstPersonArmsRigRoot != null)
        {
            armsAnimator = rigRefs.FirstPersonArmsRigRoot.GetComponentInChildren<Animator>(true);
        }
    }

    private void ApplyAnimatorSettings()
    {
        if (armsAnimator == null)
        {
            return;
        }

        armsAnimator.applyRootMotion = false;
        weaponHoldBlend = Mathf.Clamp01(weaponHoldBlend);
    }
}
