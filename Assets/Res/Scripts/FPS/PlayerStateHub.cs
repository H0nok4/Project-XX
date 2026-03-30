using System.Text;
using UnityEngine;

public enum PlayerControlContext
{
    Gameplay = 0,
    UiFocused = 1,
    Dead = 2
}

public enum PlayerUpperBodyAction
{
    Idle = 0,
    Weapon = 1,
    Reload = 2,
    Medical = 3,
    Throwable = 4,
    UiFocused = 5,
    Dead = 6
}

public enum PlayerWeaponSlotType
{
    None = 0,
    Primary = 1,
    Secondary = 2,
    Melee = 3
}

public enum PlayerWeaponCategory
{
    None = 0,
    Firearm = 1,
    Melee = 2,
    Throwable = 3
}

public struct PlayerRuntimeStateSnapshot
{
    public PlayerControlContext ControlContext;
    public PlayerUpperBodyAction UpperBodyAction;
    public PlayerWeaponSlotType ActiveWeaponSlot;
    public PlayerWeaponCategory WeaponCategory;
    public PrototypeWeaponFireMode FireMode;
    public bool IsUiFocused;
    public bool IsAlive;
    public bool IsCursorLocked;
    public bool IsGrounded;
    public bool IsCrouching;
    public bool IsSprinting;
    public bool IsAiming;
    public bool CanAim;
    public bool HasWeapon;
    public bool IsReloading;
    public bool CanFire;
    public bool CanReload;
    public bool CanUseMedical;
    public bool CanThrow;
    public bool ShowCrosshair;
    public bool ShowHitMarker;
    public bool HasHeavyBleed;
    public bool HasLightBleed;
    public bool HasFracture;
    public bool IsPainkillerActive;
    public bool IsExhausted;
    public bool IsStaminaRecoveryBlocked;
    public bool JumpTriggered;
    public bool LandTriggered;
    public bool FireTriggered;
    public bool ReloadTriggered;
    public bool EquipTriggered;
    public bool MedicalTriggered;
    public bool ThrowTriggered;
    public float Pitch;
    public float AimBlend;
    public float PlanarSpeed;
    public float VelocityY;
    public float MovementSpeedRatio;
    public float CurrentStamina;
    public float MaxStamina;
    public float StaminaNormalized;
    public float TotalCurrentHealth;
    public float TotalMaxHealth;
    public float PainkillerRemaining;
    public float HeadArmorNormalized;
    public float TorsoArmorNormalized;
    public float ReloadRemaining;
    public float AttackCooldownRemaining;
    public int MagazineAmmo;
    public int MagazineSize;
    public int ReserveAmmo;
    public Vector2 MoveInput;
    public Color StaminaColor;
    public string StaminaLabel;
    public string HudDetailText;
}

[DisallowMultipleComponent]
public sealed class PlayerStateHub : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PrototypeFpsInput fpsInput;
    [SerializeField] private PlayerInteractionState interactionState;
    [SerializeField] private PrototypeUnitVitals playerVitals;
    [SerializeField] private PrototypeFpsMovementModule movementModule;
    [SerializeField] private PlayerLookController lookController;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private PlayerMedicalController medicalController;
    [SerializeField] private PlayerThrowableController throwableController;
    [SerializeField] private PlayerSkillManager skillManager;
    [SerializeField] private PlayerProgressionRuntime progressionRuntime;
    [SerializeField] private PlayerActionChannel actionChannel;

    private PlayerRuntimeStateSnapshot snapshot;

    public PlayerRuntimeStateSnapshot Snapshot => snapshot;

    private void Awake()
    {
        ResolveReferences();
        RefreshSnapshot();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void ApplyHostSettings(
        PrototypeFpsInput hostInput,
        PlayerInteractionState hostInteractionState,
        PrototypeUnitVitals hostVitals,
        PrototypeFpsMovementModule hostMovementModule,
        PlayerLookController hostLookController,
        PlayerWeaponController hostWeaponController,
        PlayerAimController hostAimController,
        PlayerMedicalController hostMedicalController,
        PlayerThrowableController hostThrowableController,
        PlayerSkillManager hostSkillManager,
        PlayerProgressionRuntime hostProgressionRuntime,
        PlayerActionChannel hostActionChannel)
    {
        if (hostInput != null)
        {
            fpsInput = hostInput;
        }

        if (hostInteractionState != null)
        {
            interactionState = hostInteractionState;
        }

        if (hostVitals != null)
        {
            playerVitals = hostVitals;
        }

        if (hostMovementModule != null)
        {
            movementModule = hostMovementModule;
        }

        if (hostLookController != null)
        {
            lookController = hostLookController;
        }

        if (hostWeaponController != null)
        {
            weaponController = hostWeaponController;
        }

        if (hostAimController != null)
        {
            aimController = hostAimController;
        }

        if (hostMedicalController != null)
        {
            medicalController = hostMedicalController;
        }

        if (hostThrowableController != null)
        {
            throwableController = hostThrowableController;
        }

        if (hostSkillManager != null)
        {
            skillManager = hostSkillManager;
        }

        if (hostProgressionRuntime != null)
        {
            progressionRuntime = hostProgressionRuntime;
        }

        if (hostActionChannel != null)
        {
            actionChannel = hostActionChannel;
        }
    }

    public void BeginFrame()
    {
        ResolveReferences();
        actionChannel?.BeginFrame();
    }

    public void RefreshSnapshot()
    {
        ResolveReferences();

        bool isUiFocused = interactionState != null && interactionState.IsUiFocused;
        bool isAlive = playerVitals == null || !playerVitals.IsDead;
        PlayerControlContext controlContext = !isAlive
            ? PlayerControlContext.Dead
            : isUiFocused
                ? PlayerControlContext.UiFocused
                : PlayerControlContext.Gameplay;

        PlayerWeaponController.WeaponHudState weaponHudState = default;
        bool hasWeaponHudState = weaponController != null && weaponController.TryGetHudState(out weaponHudState);
        BuildStaminaHudState(out float staminaNormalized, out Color staminaColor, out string staminaLabel);

        float reloadRemaining = hasWeaponHudState && weaponHudState.IsReloading
            ? Mathf.Max(0f, weaponHudState.ReloadEndTime - Time.time)
            : 0f;
        float attackCooldownRemaining = hasWeaponHudState
            ? Mathf.Max(0f, weaponHudState.NextAttackTime - Time.time)
            : 0f;
        bool isReloading = hasWeaponHudState && weaponHudState.IsReloading;
        bool hasWeapon = hasWeaponHudState && weaponHudState.Definition != null;

        PlayerUpperBodyAction upperBodyAction = actionChannel != null
            ? actionChannel.ResolveCurrentAction(isUiFocused, !isAlive)
            : ResolveFallbackUpperBodyAction(isUiFocused, !isAlive, isReloading, hasWeapon);

        snapshot = new PlayerRuntimeStateSnapshot
        {
            ControlContext = controlContext,
            UpperBodyAction = upperBodyAction,
            ActiveWeaponSlot = weaponController != null ? weaponController.ActiveWeaponSlotType : PlayerWeaponSlotType.None,
            WeaponCategory = weaponController != null ? weaponController.ActiveWeaponCategory : PlayerWeaponCategory.None,
            FireMode = hasWeaponHudState ? weaponHudState.FireMode : PrototypeWeaponFireMode.Semi,
            IsUiFocused = isUiFocused,
            IsAlive = isAlive,
            IsCursorLocked = Cursor.lockState == CursorLockMode.Locked,
            IsGrounded = movementModule != null && movementModule.IsGrounded,
            IsCrouching = movementModule != null && movementModule.IsCrouching,
            IsSprinting = movementModule != null && movementModule.IsSprinting,
            IsAiming = aimController != null && aimController.IsAiming,
            CanAim = aimController != null && weaponController != null && weaponController.CanAimActiveWeapon,
            HasWeapon = hasWeapon,
            IsReloading = isReloading,
            CanFire = CanFire(isAlive, isUiFocused, hasWeapon, isReloading),
            CanReload = CanReload(isAlive, isUiFocused, hasWeaponHudState, weaponHudState, isReloading),
            CanUseMedical = isAlive && !isUiFocused,
            CanThrow = isAlive && !isUiFocused,
            ShowCrosshair = aimController == null || !aimController.ShouldHideHipFireCrosshair,
            ShowHitMarker = weaponController != null && weaponController.ShowHitMarker,
            HasHeavyBleed = playerVitals != null && playerVitals.HasHeavyBleed,
            HasLightBleed = playerVitals != null && playerVitals.HasLightBleed,
            HasFracture = playerVitals != null && playerVitals.HasFracture,
            IsPainkillerActive = playerVitals != null && playerVitals.IsPainkillerActive,
            IsExhausted = playerVitals != null && playerVitals.IsExhausted,
            IsStaminaRecoveryBlocked = playerVitals != null && playerVitals.IsStaminaRecoveryBlocked,
            JumpTriggered = movementModule != null && movementModule.JumpTriggeredThisFrame,
            LandTriggered = movementModule != null && movementModule.LandTriggeredThisFrame,
            FireTriggered = weaponController != null && weaponController.FireTriggeredThisFrame,
            ReloadTriggered = weaponController != null && weaponController.ReloadTriggeredThisFrame,
            EquipTriggered = weaponController != null && weaponController.EquipTriggeredThisFrame,
            MedicalTriggered = medicalController != null && medicalController.MedicalTriggeredThisFrame,
            ThrowTriggered = throwableController != null && throwableController.ThrowableTriggeredThisFrame,
            Pitch = lookController != null ? lookController.Pitch : 0f,
            AimBlend = aimController != null ? aimController.AimBlend : 0f,
            PlanarSpeed = movementModule != null ? movementModule.PlanarSpeed : 0f,
            VelocityY = movementModule != null ? movementModule.VerticalVelocity : 0f,
            MovementSpeedRatio = movementModule != null ? movementModule.SelectedMovementSpeedRatio : 1f,
            CurrentStamina = playerVitals != null ? playerVitals.CurrentStamina : 0f,
            MaxStamina = playerVitals != null ? playerVitals.MaxStamina : 0f,
            StaminaNormalized = staminaNormalized,
            TotalCurrentHealth = playerVitals != null ? playerVitals.TotalCurrentHealth : 0f,
            TotalMaxHealth = playerVitals != null ? playerVitals.TotalMaxHealth : 0f,
            PainkillerRemaining = playerVitals != null ? playerVitals.PainkillerRemaining : 0f,
            HeadArmorNormalized = playerVitals != null ? playerVitals.GetArmorDurabilityNormalized("head") : 0f,
            TorsoArmorNormalized = playerVitals != null ? playerVitals.GetArmorDurabilityNormalized("torso") : 0f,
            ReloadRemaining = reloadRemaining,
            AttackCooldownRemaining = attackCooldownRemaining,
            MagazineAmmo = hasWeaponHudState ? weaponHudState.MagazineAmmo : 0,
            MagazineSize = hasWeaponHudState && weaponHudState.Definition != null ? weaponHudState.Definition.MagazineSize : 0,
            ReserveAmmo = hasWeaponHudState ? weaponHudState.ReserveAmmo : 0,
            MoveInput = movementModule != null ? movementModule.CurrentMoveInput : Vector2.zero,
            StaminaColor = staminaColor,
            StaminaLabel = staminaLabel,
            HudDetailText = BuildHudDetailText(hasWeaponHudState, weaponHudState)
        };
    }

    private void ResolveReferences()
    {
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

        if (movementModule == null)
        {
            movementModule = GetComponent<PrototypeFpsMovementModule>();
        }

        if (lookController == null)
        {
            lookController = GetComponent<PlayerLookController>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<PlayerWeaponController>();
        }

        if (aimController == null)
        {
            aimController = GetComponent<PlayerAimController>();
        }

        if (medicalController == null)
        {
            medicalController = GetComponent<PlayerMedicalController>();
        }

        if (throwableController == null)
        {
            throwableController = GetComponent<PlayerThrowableController>();
        }

        if (skillManager == null)
        {
            skillManager = GetComponent<PlayerSkillManager>();
        }

        if (progressionRuntime == null)
        {
            progressionRuntime = GetComponent<PlayerProgressionRuntime>();
        }

        if (actionChannel == null)
        {
            actionChannel = GetComponent<PlayerActionChannel>();
        }
    }

    private static PlayerUpperBodyAction ResolveFallbackUpperBodyAction(bool isUiFocused, bool isDead, bool isReloading, bool hasWeapon)
    {
        if (isDead)
        {
            return PlayerUpperBodyAction.Dead;
        }

        if (isUiFocused)
        {
            return PlayerUpperBodyAction.UiFocused;
        }

        if (isReloading)
        {
            return PlayerUpperBodyAction.Reload;
        }

        return hasWeapon ? PlayerUpperBodyAction.Weapon : PlayerUpperBodyAction.Idle;
    }

    private static bool CanFire(bool isAlive, bool isUiFocused, bool hasWeapon, bool isReloading)
    {
        return isAlive && !isUiFocused && hasWeapon && !isReloading;
    }

    private static bool CanReload(
        bool isAlive,
        bool isUiFocused,
        bool hasWeaponHudState,
        PlayerWeaponController.WeaponHudState hudState,
        bool isReloading)
    {
        return isAlive
            && !isUiFocused
            && hasWeaponHudState
            && hudState.Definition != null
            && !hudState.Definition.IsMeleeWeapon
            && !isReloading
            && hudState.MagazineAmmo < hudState.Definition.MagazineSize
            && hudState.ReserveAmmo > 0;
    }

    private void BuildStaminaHudState(out float normalized, out Color fillColor, out string label)
    {
        normalized = 0f;
        fillColor = new Color(0.27f, 0.82f, 0.38f, 0.95f);
        label = string.Empty;

        if (playerVitals == null)
        {
            return;
        }

        normalized = playerVitals.StaminaNormalized;
        bool recoveryBlocked = playerVitals.IsStaminaRecoveryBlocked;
        bool lowStamina = playerVitals.IsBelowStaminaActionThreshold;
        fillColor = lowStamina
            ? new Color(0.8f, 0.2f, 0.18f, 0.95f)
            : recoveryBlocked
                ? (playerVitals.IsExhausted ? new Color(0.78f, 0.28f, 0.2f, 0.95f) : new Color(0.88f, 0.58f, 0.14f, 0.95f))
                : Color.Lerp(new Color(0.94f, 0.68f, 0.16f, 0.95f), new Color(0.27f, 0.82f, 0.38f, 0.95f), normalized);

        label = recoveryBlocked
            ? playerVitals.IsExhausted
                ? $"体力 {Mathf.RoundToInt(playerVitals.CurrentStamina)}/{Mathf.RoundToInt(playerVitals.MaxStamina)}  力竭 {playerVitals.StaminaRecoveryBlockedRemaining:0.0}s"
                : $"体力 {Mathf.RoundToInt(playerVitals.CurrentStamina)}/{Mathf.RoundToInt(playerVitals.MaxStamina)}  恢复 {playerVitals.StaminaRecoveryBlockedRemaining:0.0}s"
            : $"体力 {Mathf.RoundToInt(playerVitals.CurrentStamina)}/{Mathf.RoundToInt(playerVitals.MaxStamina)}";
    }

    private string BuildHudDetailText(bool hasWeaponHudState, PlayerWeaponController.WeaponHudState hudState)
    {
        string combatStatusText = BuildCombatStatusText();
        if (!hasWeaponHudState || hudState.Definition == null)
        {
            return combatStatusText;
        }

        string weaponLine = ItemRarityUtility.FormatRichText(
            $"{hudState.Definition.DisplayNameWithLevel} [{ItemRarityUtility.GetDisplayName(hudState.Rarity)}]",
            hudState.Rarity);
        string stateLine;
        if (hudState.Definition.IsMeleeWeapon)
        {
            float cooldownRemaining = Mathf.Max(0f, hudState.NextAttackTime - Time.time);
            stateLine = cooldownRemaining > 0f
                ? $"冷却 {cooldownRemaining:0.00}s"
                : "就绪";
        }
        else if (hudState.IsReloading)
        {
            stateLine = $"换弹 {Mathf.Max(0f, hudState.ReloadEndTime - Time.time):0.0}s";
        }
        else
        {
            stateLine = $"{GetFireModeLabel(hudState.FireMode)}  弹夹 {hudState.MagazineAmmo}/{hudState.Definition.MagazineSize}  备弹 {hudState.ReserveAmmo}";
        }

        string statsLine = BuildWeaponStatsText(hudState);
        return $"{weaponLine}\n{stateLine}\n{statsLine}\n{combatStatusText}";
    }

    private string BuildCombatStatusText()
    {
        if (playerVitals == null)
        {
            return "生命系统未就绪";
        }

        var builder = new StringBuilder(192);
        builder.Append("生命 ");
        builder.Append(Mathf.RoundToInt(playerVitals.TotalCurrentHealth));
        builder.Append('/');
        builder.Append(Mathf.RoundToInt(playerVitals.TotalMaxHealth));
        builder.Append("\n体力 ");
        builder.Append(Mathf.RoundToInt(playerVitals.CurrentStamina));
        builder.Append('/');
        builder.Append(Mathf.RoundToInt(playerVitals.MaxStamina));

        if (playerVitals.StaminaRecoveryBlockedRemaining > 0f)
        {
            builder.Append(playerVitals.IsExhausted ? "  力竭 " : "  恢复 ");
            builder.Append(playerVitals.StaminaRecoveryBlockedRemaining.ToString("0.0"));
            builder.Append('s');
        }

        builder.Append("\n姿态 ");
        bool isSprinting = movementModule != null && movementModule.IsSprinting;
        bool isCrouching = movementModule != null && movementModule.IsCrouching;
        builder.Append(isSprinting ? "冲刺" : isCrouching ? "蹲伏" : "站立");
        builder.Append(' ');
        builder.Append(Mathf.RoundToInt((movementModule != null ? movementModule.SelectedMovementSpeedRatio : 1f) * 100f));
        builder.Append('%');

        float headArmor = playerVitals.GetArmorDurabilityNormalized("head");
        float torsoArmor = playerVitals.GetArmorDurabilityNormalized("torso");
        if (headArmor > 0f || torsoArmor > 0f)
        {
            builder.Append("\n护甲 头 ");
            builder.Append(Mathf.RoundToInt(headArmor * 100f));
            builder.Append("%  胸 ");
            builder.Append(Mathf.RoundToInt(torsoArmor * 100f));
            builder.Append('%');
        }

        builder.Append("\n状态 ");
        if (playerVitals.HasHeavyBleed)
        {
            builder.Append("重出血 ");
        }

        if (playerVitals.HasLightBleed)
        {
            builder.Append("轻出血 ");
        }

        if (playerVitals.HasFracture)
        {
            builder.Append("骨折 ");
        }

        if (playerVitals.IsPainkillerActive)
        {
            builder.Append("止痛 ");
            builder.Append(playerVitals.PainkillerRemaining.ToString("0"));
            builder.Append('s');
        }

        if (!playerVitals.HasAnyBleed && !playerVitals.HasFracture && !playerVitals.IsPainkillerActive)
        {
            builder.Append("正常");
        }

        AppendLineIfPresent(builder, medicalController != null ? medicalController.FeedbackMessage : string.Empty);
        AppendLineIfPresent(builder, weaponController != null ? weaponController.FeedbackMessage : string.Empty);
        AppendLineIfPresent(builder, throwableController != null ? throwableController.BuildHudSummary() : string.Empty);
        AppendLineIfPresent(builder, throwableController != null ? throwableController.FeedbackMessage : string.Empty);
        AppendLineIfPresent(builder, progressionRuntime != null ? progressionRuntime.BuildHudSummary() : string.Empty);
        AppendLineIfPresent(builder, progressionRuntime != null ? progressionRuntime.FeedbackMessage : string.Empty);
        AppendLineIfPresent(builder, skillManager != null ? skillManager.BuildHudSummary() : string.Empty);
        return builder.ToString();
    }

    private static void AppendLineIfPresent(StringBuilder builder, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.Append('\n');
        builder.Append(value);
    }

    private static string BuildWeaponStatsText(PlayerWeaponController.WeaponHudState hudState)
    {
        PrototypeWeaponDefinition definition = hudState.Definition;
        if (definition == null)
        {
            return string.Empty;
        }

        if (definition.IsThrowableWeapon)
        {
            return $"爆炸 {definition.ExplosionDamage:0}  半径 {definition.ExplosionRadius:0.0}m";
        }

        if (definition.IsMeleeWeapon)
        {
            return $"伤害 {definition.MeleeDamage:0}  穿深 {definition.PenetrationPower:0}";
        }

        AmmoDefinition ammoDefinition = definition.AmmoDefinition;
        float ammoMultiplier = ammoDefinition != null ? ammoDefinition.DamageMultiplier : 1f;
        float finalDamage = definition.FirearmDamage * ammoMultiplier;
        float penetration = ammoDefinition != null ? ammoDefinition.PenetrationPower : definition.PenetrationPower;
        return $"伤害 {definition.FirearmDamage:0} x {ammoMultiplier:0.00} = {finalDamage:0}  穿深 {penetration:0}";
    }

    private static string GetFireModeLabel(PrototypeWeaponFireMode fireMode)
    {
        switch (fireMode)
        {
            case PrototypeWeaponFireMode.Auto:
                return "全自动";
            case PrototypeWeaponFireMode.Burst:
                return "点射";
            default:
                return "半自动";
        }
    }
}
