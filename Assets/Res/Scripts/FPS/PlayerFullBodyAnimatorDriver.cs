using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerFullBodyAnimatorDriver : MonoBehaviour
{
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
    private static readonly int HasWeaponHash = Animator.StringToHash("HasWeapon");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int UpperBodyActionHash = Animator.StringToHash("UpperBodyAction");
    private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");
    private static readonly int LandTriggerHash = Animator.StringToHash("Land");
    private static readonly int FireTriggerHash = Animator.StringToHash("Fire");
    private static readonly int ReloadTriggerHash = Animator.StringToHash("Reload");
    private static readonly int MeleeTriggerHash = Animator.StringToHash("Melee");
    private static readonly int EquipTriggerHash = Animator.StringToHash("Equip");
    private static readonly int MedicalTriggerHash = Animator.StringToHash("Medical");
    private static readonly int ThrowTriggerHash = Animator.StringToHash("Throw");

    [Header("References")]
    [SerializeField] private PlayerAnimationRigRefs rigRefs;
    [SerializeField] private PlayerStateHub stateHub;
    [SerializeField] private Animator characterAnimator;

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
        characterAnimator.SetFloat(MoveSpeedHash, snapshot.PlanarSpeed);
        characterAnimator.SetFloat(VerticalSpeedHash, snapshot.VelocityY);
        characterAnimator.SetBool(IsGroundedHash, snapshot.IsGrounded);
        characterAnimator.SetBool(IsCrouchingHash, snapshot.IsCrouching);
        characterAnimator.SetBool(IsSprintingHash, snapshot.IsSprinting);
        characterAnimator.SetBool(IsAimingHash, snapshot.IsAiming && snapshot.HasWeapon && snapshot.IsAlive);
        characterAnimator.SetBool(HasWeaponHash, snapshot.HasWeapon);
        characterAnimator.SetBool(IsDeadHash, !snapshot.IsAlive);
        characterAnimator.SetInteger(UpperBodyActionHash, (int)snapshot.UpperBodyAction);

        TriggerIfTrue(JumpTriggerHash, snapshot.JumpTriggered);
        TriggerIfTrue(LandTriggerHash, snapshot.LandTriggered);
        if (snapshot.FireTriggered)
        {
            if (snapshot.WeaponCategory == PlayerWeaponCategory.Melee)
            {
                characterAnimator.SetTrigger(MeleeTriggerHash);
            }
            else
            {
                characterAnimator.SetTrigger(FireTriggerHash);
            }
        }

        TriggerIfTrue(ReloadTriggerHash, snapshot.ReloadTriggered);
        TriggerIfTrue(EquipTriggerHash, snapshot.EquipTriggered);
        TriggerIfTrue(MedicalTriggerHash, snapshot.MedicalTriggered);
        TriggerIfTrue(ThrowTriggerHash, snapshot.ThrowTriggered);
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
    }

    private void ApplyAnimatorSettings()
    {
        if (characterAnimator == null)
        {
            return;
        }

        characterAnimator.applyRootMotion = false;
    }

    private void TriggerIfTrue(int triggerHash, bool shouldTrigger)
    {
        if (shouldTrigger)
        {
            characterAnimator.SetTrigger(triggerHash);
        }
    }
}
