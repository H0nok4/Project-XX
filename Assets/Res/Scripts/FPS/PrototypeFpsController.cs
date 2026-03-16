using System.Text;
using UnityEngine;

[RequireComponent(typeof(PrototypeFpsInput), typeof(PrototypeFpsMovementModule))]
public class PrototypeFpsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform primaryViewModel;
    [SerializeField] private Transform secondaryViewModel;
    [SerializeField] private Transform meleeViewModel;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.14f;
    [SerializeField] private float maxLookAngle = 85f;
    [SerializeField] private bool showHud = true;

    [Header("Combat")]
    [SerializeField] private PrototypeWeaponDefinition primaryWeapon;
    [SerializeField] private PrototypeWeaponDefinition secondaryWeapon;
    [SerializeField] private PrototypeWeaponDefinition meleeWeapon;
    [SerializeField] private float shootDistance = 60f;
    [SerializeField] private float baseDamage = 42f;
    [SerializeField] private float shootForce = 18f;
    [SerializeField] private float meleeImpactForce = 9f;
    [SerializeField] private float meleeStaminaCost = 22f;
    [SerializeField] private float impactMarkerLifetime = 0.2f;
    [SerializeField] private LayerMask shootMask = Physics.DefaultRaycastLayers;

    [Header("Medical")]
    [SerializeField] private float medicalUseCooldown = 0.28f;
    [SerializeField] private float medicalFeedbackLifetime = 1.4f;

    [Header("AI Awareness")]
    [SerializeField] private float firearmNoiseRadius = 26f;
    [SerializeField] private float meleeNoiseRadius = 8f;

    [Header("Controllers")]
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private PlayerMedicalController medicalController;
    [SerializeField] private PlayerThrowableController throwableController;
    [SerializeField] private PlayerSkillManager skillManager;
    [SerializeField] private PlayerProgressionRuntime progressionRuntime;

    private PrototypeFpsMovementModule movementModule;
    private PrototypeFpsInput fpsInput;
    private PlayerInteractionState interactionState;
    private PrototypeUnitVitals playerVitals;
    private InventoryContainer inventory;
    private float pitch;
    private GUIStyle hudStyle;
    private GUIStyle centerStyle;
    private GUIStyle barLabelStyle;

    public PrototypeWeaponDefinition EquippedPrimaryWeapon => weaponController != null ? weaponController.EquippedPrimaryWeapon : null;
    public PrototypeWeaponDefinition EquippedSecondaryWeapon => weaponController != null ? weaponController.EquippedSecondaryWeapon : null;
    public PrototypeWeaponDefinition EquippedMeleeWeapon => weaponController != null ? weaponController.EquippedMeleeWeapon : null;

    private void Awake()
    {
        movementModule = GetOrCreateMovementModule();
        fpsInput = GetOrCreateInput();
        interactionState = GetOrCreateInteractionState();
        playerVitals = GetComponent<PrototypeUnitVitals>();
        inventory = GetComponent<InventoryContainer>();
        EnsureCombatSettings();

        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }

        if (muzzle == null && viewCamera != null)
        {
            Transform muzzleTransform = viewCamera.transform.Find("Muzzle");
            if (muzzleTransform != null)
            {
                muzzle = muzzleTransform;
            }
        }

        weaponController = GetOrCreateWeaponController();
        medicalController = GetOrCreateMedicalController();
        throwableController = GetOrCreateThrowableController();
        skillManager = GetOrCreateSkillManager();
        progressionRuntime = GetOrCreateProgressionRuntime();

        ApplyControllerSettings();

        if (progressionRuntime != null)
        {
            progressionRuntime.SetPlayerDependencies(playerVitals, weaponController, skillManager);
        }

        if (weaponController != null)
        {
            weaponController.SetPlayerDependencies(playerVitals, inventory);
            weaponController.InitializeRuntime();
        }

        if (skillManager != null)
        {
            skillManager.SetPlayerDependencies(playerVitals, weaponController, progressionRuntime);
            skillManager.RefreshFromEquipment();
        }

        if (medicalController != null)
        {
            medicalController.SetPlayerDependencies(playerVitals, inventory);
        }

        if (throwableController != null)
        {
            throwableController.SetPlayerDependencies(viewCamera, playerVitals, inventory);
        }

        movementModule?.SetViewCamera(viewCamera);
    }

    private void OnEnable()
    {
        LockCursor(true);
        weaponController?.RefreshWeaponViewModels();
    }

    private void OnDisable()
    {
        LockCursor(false);
    }

    public void ConfigureWeaponLoadout(
        PrototypeWeaponDefinition primaryDefinition,
        PrototypeWeaponDefinition secondaryDefinition,
        PrototypeWeaponDefinition meleeDefinition)
    {
        primaryWeapon = primaryDefinition;
        secondaryWeapon = secondaryDefinition;
        meleeWeapon = meleeDefinition;

        weaponController?.ConfigureWeaponLoadout(primaryDefinition, secondaryDefinition, meleeDefinition);
        skillManager?.RefreshFromEquipment();
    }

    public void ConfigureWeaponLoadout(
        WeaponInstance primaryInstance,
        WeaponInstance secondaryInstance,
        WeaponInstance meleeInstance)
    {
        primaryWeapon = primaryInstance != null ? primaryInstance.Definition : null;
        secondaryWeapon = secondaryInstance != null ? secondaryInstance.Definition : null;
        meleeWeapon = meleeInstance != null ? meleeInstance.Definition : null;

        weaponController?.ConfigureWeaponLoadout(primaryInstance, secondaryInstance, meleeInstance);
        skillManager?.RefreshFromEquipment();
    }

    public void ConfigureWeaponLoadout(
        ItemInstance primaryInstance,
        ItemInstance secondaryInstance,
        ItemInstance meleeInstance)
    {
        WeaponInstance primaryWeaponInstance = primaryInstance != null ? primaryInstance.ToWeaponInstance() : null;
        WeaponInstance secondaryWeaponInstance = secondaryInstance != null ? secondaryInstance.ToWeaponInstance() : null;
        WeaponInstance meleeWeaponInstance = meleeInstance != null ? meleeInstance.ToWeaponInstance() : null;

        ConfigureWeaponLoadout(primaryWeaponInstance, secondaryWeaponInstance, meleeWeaponInstance);
    }

    public WeaponInstance GetPrimaryWeaponInstance()
    {
        return weaponController != null ? weaponController.GetPrimaryWeaponInstance() : null;
    }

    public WeaponInstance GetSecondaryWeaponInstance()
    {
        return weaponController != null ? weaponController.GetSecondaryWeaponInstance() : null;
    }

    public WeaponInstance GetMeleeWeaponInstance()
    {
        return weaponController != null ? weaponController.GetMeleeWeaponInstance() : null;
    }

    public ItemInstance GetPrimaryItemInstance()
    {
        return ItemInstance.Create(GetPrimaryWeaponInstance());
    }

    public ItemInstance GetSecondaryItemInstance()
    {
        return ItemInstance.Create(GetSecondaryWeaponInstance());
    }

    public ItemInstance GetMeleeItemInstance()
    {
        return ItemInstance.Create(GetMeleeWeaponInstance());
    }

    public void ConfigurePlayerProgression(PlayerProgressionData progressionData)
    {
        progressionRuntime = GetOrCreateProgressionRuntime();
        progressionRuntime?.SetPlayerDependencies(playerVitals, weaponController, skillManager);
        progressionRuntime?.Configure(progressionData);
        skillManager?.SetPlayerDependencies(playerVitals, weaponController, progressionRuntime);
    }

    public void CopyPlayerProgressionTo(PlayerProgressionData target)
    {
        if (target == null)
        {
            return;
        }

        progressionRuntime?.ExportTo(target);
    }

    private void Update()
    {
        if (fpsInput == null || !fpsInput.IsReady || viewCamera == null)
        {
            return;
        }

        if (interactionState != null && interactionState.IsUiFocused)
        {
            movementModule?.TickMovement();
            if (Cursor.lockState != CursorLockMode.None)
            {
                LockCursor(false);
            }

            weaponController?.TickVisuals(Time.deltaTime);
            weaponController?.TickFeedback(Time.deltaTime);
            medicalController?.TickFeedback(Time.deltaTime);
            throwableController?.TickFeedback(Time.deltaTime);
            progressionRuntime?.TickFeedback(Time.deltaTime);
            return;
        }

        if (fpsInput.ToggleCursorPressedThisFrame)
        {
            LockCursor(false);
        }
        else if (fpsInput.ShootPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor(true);
        }

        HandleLook();
        HandleMovement();

        weaponController?.HandleWeaponInput(fpsInput);

        bool usedMedical = medicalController != null && medicalController.HandleMedicalInput(fpsInput);
        bool usedThrowable = !usedMedical && throwableController != null && throwableController.HandleThrowableInput(fpsInput);
        if (!usedMedical && !usedThrowable)
        {
            weaponController?.HandleCombat(fpsInput);
        }

        weaponController?.TickVisuals(Time.deltaTime);
        weaponController?.TickFeedback(Time.deltaTime);
        medicalController?.TickFeedback(Time.deltaTime);
        throwableController?.TickFeedback(Time.deltaTime);
        progressionRuntime?.TickFeedback(Time.deltaTime);
    }

    private void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Vector2 mouseDelta = fpsInput.LookDelta;
        float lookYawDelta = mouseDelta.x * mouseSensitivity;

        transform.Rotate(Vector3.up * lookYawDelta);

        pitch -= mouseDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        viewCamera.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        movementModule?.TickMovement();
    }

    public void ApplySpawnPose(Vector3 position, Quaternion rotation)
    {
        if (movementModule != null)
        {
            movementModule.TeleportTo(position, rotation);
        }
        else
        {
            transform.SetPositionAndRotation(position, rotation);
        }

        pitch = 0f;
        if (viewCamera != null)
        {
            viewCamera.transform.localEulerAngles = Vector3.zero;
        }
    }

    public string GetSuggestedPickupSlotLabel(PrototypeWeaponDefinition weaponDefinition)
    {
        if (weaponController != null)
        {
            return weaponController.GetSuggestedPickupSlotLabel(weaponDefinition);
        }

        if (weaponDefinition != null && weaponDefinition.IsThrowableWeapon)
        {
            return "Special";
        }

        return weaponDefinition != null && weaponDefinition.IsMeleeWeapon ? "Melee" : "Primary";
    }

    public bool PickupWouldReplaceEquippedWeapon(PrototypeWeaponDefinition weaponDefinition)
    {
        return weaponController != null && weaponController.PickupWouldReplaceEquippedWeapon(weaponDefinition);
    }

    public bool PickupWouldStoreInBackpack(PrototypeWeaponDefinition weaponDefinition)
    {
        return weaponController != null && weaponController.PickupWouldStoreInBackpack(weaponDefinition);
    }

    public bool TryEquipLootedWeapon(
        PrototypeWeaponDefinition weaponDefinition,
        int startingMagazineAmmo,
        out PrototypeWeaponDefinition droppedWeaponDefinition,
        out int droppedMagazineAmmo)
    {
        if (weaponController == null)
        {
            droppedWeaponDefinition = null;
            droppedMagazineAmmo = 0;
            return false;
        }

        bool result = weaponController.TryEquipLootedWeapon(
            weaponDefinition,
            startingMagazineAmmo,
            out droppedWeaponDefinition,
            out droppedMagazineAmmo);

        if (result)
        {
            SyncWeaponDefinitionsFromController();
        }

        return result;
    }

    public bool TryEquipLootedWeapon(ItemInstance weaponInstance, out ItemInstance droppedWeapon)
    {
        droppedWeapon = null;
        if (weaponController == null)
        {
            return false;
        }

        bool result = weaponController.TryEquipLootedWeapon(weaponInstance, out droppedWeapon);
        if (result)
        {
            SyncWeaponDefinitionsFromController();
        }

        return result;
    }

    public bool TryEquipLootedWeapon(WeaponInstance weaponInstance, out WeaponInstance droppedWeapon)
    {
        bool result = TryEquipLootedWeapon(ItemInstance.Create(weaponInstance), out ItemInstance droppedItem);
        droppedWeapon = droppedItem != null ? droppedItem.ToWeaponInstance() : null;
        return result;
    }

    public bool TryEquipInventoryWeapon(ItemInstance itemInstance, out ItemInstance droppedWeapon)
    {
        droppedWeapon = null;
        if (weaponController == null)
        {
            return false;
        }

        bool result = weaponController.TryEquipInventoryWeapon(itemInstance, out droppedWeapon);
        if (result)
        {
            SyncWeaponDefinitionsFromController();
        }

        return result;
    }

    public bool TryEquipInventoryWeapon(ItemInstance itemInstance, out WeaponInstance droppedWeapon)
    {
        bool result = TryEquipInventoryWeapon(itemInstance, out ItemInstance droppedItem);
        droppedWeapon = droppedItem != null ? droppedItem.ToWeaponInstance() : null;
        return result;
    }

    private void SyncWeaponDefinitionsFromController()
    {
        if (weaponController == null)
        {
            return;
        }

        primaryWeapon = weaponController.EquippedPrimaryWeapon;
        secondaryWeapon = weaponController.EquippedSecondaryWeapon;
        meleeWeapon = weaponController.EquippedMeleeWeapon;
    }

    private void ApplyControllerSettings()
    {
        if (weaponController != null)
        {
            weaponController.ApplyHostSettings(
                viewCamera,
                muzzle,
                primaryViewModel,
                secondaryViewModel,
                meleeViewModel,
                primaryWeapon,
                secondaryWeapon,
                meleeWeapon,
                shootDistance,
                baseDamage,
                shootForce,
                meleeImpactForce,
                meleeStaminaCost,
                impactMarkerLifetime,
                shootMask,
                firearmNoiseRadius,
                meleeNoiseRadius);
        }

        if (medicalController != null)
        {
            medicalController.ApplyHostSettings(medicalUseCooldown, medicalFeedbackLifetime);
        }

        if (throwableController != null)
        {
            throwableController.ApplyHostSettings(viewCamera, medicalFeedbackLifetime);
        }
    }

    private PlayerWeaponController GetOrCreateWeaponController()
    {
        PlayerWeaponController controller = weaponController != null ? weaponController : GetComponent<PlayerWeaponController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<PlayerWeaponController>();
        }

        weaponController = controller;
        return controller;
    }

    private PlayerMedicalController GetOrCreateMedicalController()
    {
        PlayerMedicalController controller = medicalController != null ? medicalController : GetComponent<PlayerMedicalController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<PlayerMedicalController>();
        }

        medicalController = controller;
        return controller;
    }

    private PlayerThrowableController GetOrCreateThrowableController()
    {
        PlayerThrowableController controller = throwableController != null ? throwableController : GetComponent<PlayerThrowableController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<PlayerThrowableController>();
        }

        throwableController = controller;
        return controller;
    }

    private PlayerSkillManager GetOrCreateSkillManager()
    {
        PlayerSkillManager manager = skillManager != null ? skillManager : GetComponent<PlayerSkillManager>();
        if (manager == null)
        {
            manager = gameObject.AddComponent<PlayerSkillManager>();
        }

        skillManager = manager;
        return manager;
    }

    private PlayerProgressionRuntime GetOrCreateProgressionRuntime()
    {
        PlayerProgressionRuntime runtime = progressionRuntime != null ? progressionRuntime : GetComponent<PlayerProgressionRuntime>();
        if (runtime == null)
        {
            runtime = gameObject.AddComponent<PlayerProgressionRuntime>();
        }

        progressionRuntime = runtime;
        return runtime;
    }

    private void OnGUI()
    {
        if (!showHud)
        {
            return;
        }

        EnsureHudStyles();

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        bool showHitMarker = weaponController != null && weaponController.ShowHitMarker;

        GUI.Label(new Rect(centerX - 10f, centerY - 20f, 20f, 40f), showHitMarker ? "X" : "+", centerStyle);

        GUI.Label(
            new Rect(18f, 100f, 380f, 280f),
            "移动 WASD\n观察 鼠标\n攻击 左键\n投掷 G\n交互 E\n背包 Tab\n切枪 1 / 2 / 3\n换弹 R\n切换射击模式 B\n快速治疗 4\n止血 5\n夹板 6\n止痛 7\n跳跃 Space\n冲刺 Shift\n切换蹲伏 C\n步速 LeftCtrl + 滚轮\n切换鼠标 Esc",
            hudStyle);

        if (weaponController == null || !weaponController.TryGetHudState(out PlayerWeaponController.WeaponHudState hudState))
        {
            return;
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
            stateLine = $"{GetFireModeLabel(hudState.FireMode)}  弹匣 {hudState.MagazineAmmo}/{hudState.Definition.MagazineSize}  备弹 {hudState.ReserveAmmo}";
        }

        string statsLine = BuildWeaponStatsText(hudState);

        DrawStaminaBar(new Rect(18f, 394f, 280f, 24f));
        GUI.Box(
            new Rect(Screen.width - 394f, 18f, 364f, 236f),
            $"{weaponLine}\n{stateLine}\n{statsLine}\n{BuildCombatStatusText()}",
            hudStyle);
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

        string feedbackMessage = medicalController != null ? medicalController.FeedbackMessage : string.Empty;
        if (!string.IsNullOrWhiteSpace(feedbackMessage))
        {
            builder.Append("\n");
            builder.Append(feedbackMessage);
        }

        string weaponFeedback = weaponController != null ? weaponController.FeedbackMessage : string.Empty;
        if (!string.IsNullOrWhiteSpace(weaponFeedback))
        {
            builder.Append("\n");
            builder.Append(weaponFeedback);
        }

        string throwableSummary = throwableController != null ? throwableController.BuildHudSummary() : string.Empty;
        if (!string.IsNullOrWhiteSpace(throwableSummary))
        {
            builder.Append("\n");
            builder.Append(throwableSummary);
        }

        string throwableFeedback = throwableController != null ? throwableController.FeedbackMessage : string.Empty;
        if (!string.IsNullOrWhiteSpace(throwableFeedback))
        {
            builder.Append("\n");
            builder.Append(throwableFeedback);
        }

        string progressionSummary = progressionRuntime != null ? progressionRuntime.BuildHudSummary() : string.Empty;
        if (!string.IsNullOrWhiteSpace(progressionSummary))
        {
            builder.Append("\n");
            builder.Append(progressionSummary);
        }

        string progressionFeedback = progressionRuntime != null ? progressionRuntime.FeedbackMessage : string.Empty;
        if (!string.IsNullOrWhiteSpace(progressionFeedback))
        {
            builder.Append("\n");
            builder.Append(progressionFeedback);
        }

        string skillSummary = skillManager != null ? skillManager.BuildHudSummary() : string.Empty;
        if (!string.IsNullOrWhiteSpace(skillSummary))
        {
            builder.Append("\n");
            builder.Append(skillSummary);
        }

        return builder.ToString();
    }

    private void DrawStaminaBar(Rect rect)
    {
        if (playerVitals == null)
        {
            return;
        }

        GUI.Box(rect, GUIContent.none);

        float normalized = playerVitals.StaminaNormalized;
        Rect fillRect = new Rect(rect.x + 3f, rect.y + 3f, Mathf.Max(0f, (rect.width - 6f) * normalized), rect.height - 6f);
        bool recoveryBlocked = playerVitals.IsStaminaRecoveryBlocked;
        bool lowStamina = playerVitals.IsBelowStaminaActionThreshold;
        Color fillColor = lowStamina
            ? new Color(0.8f, 0.2f, 0.18f, 0.95f)
            : recoveryBlocked
                ? (playerVitals.IsExhausted ? new Color(0.78f, 0.28f, 0.2f, 0.95f) : new Color(0.88f, 0.58f, 0.14f, 0.95f))
            : Color.Lerp(new Color(0.94f, 0.68f, 0.16f, 0.95f), new Color(0.27f, 0.82f, 0.38f, 0.95f), normalized);

        Color previousColor = GUI.color;
        GUI.color = fillColor;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = previousColor;

        string label = recoveryBlocked
            ? playerVitals.IsExhausted
                ? $"体力 {Mathf.RoundToInt(playerVitals.CurrentStamina)}/{Mathf.RoundToInt(playerVitals.MaxStamina)}  力竭 {playerVitals.StaminaRecoveryBlockedRemaining:0.0}s"
                : $"体力 {Mathf.RoundToInt(playerVitals.CurrentStamina)}/{Mathf.RoundToInt(playerVitals.MaxStamina)}  恢复 {playerVitals.StaminaRecoveryBlockedRemaining:0.0}s"
            : $"体力 {Mathf.RoundToInt(playerVitals.CurrentStamina)}/{Mathf.RoundToInt(playerVitals.MaxStamina)}";
        GUI.Label(rect, label, barLabelStyle);
    }

    private void EnsureHudStyles()
    {
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                alignment = TextAnchor.UpperLeft,
                richText = true,
                normal = { textColor = Color.white }
            };
            hudStyle.padding = new RectOffset(12, 12, 10, 10);
        }

        if (centerStyle == null)
        {
            centerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }

        if (barLabelStyle == null)
        {
            barLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }
    }

    private void LockCursor(bool shouldLock)
    {
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    private void OnValidate()
    {
        EnsureCombatSettings();

        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }

        if (muzzle == null && viewCamera != null)
        {
            Transform muzzleTransform = viewCamera.transform.Find("Muzzle");
            if (muzzleTransform != null)
            {
                muzzle = muzzleTransform;
            }
        }

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

        if (inventory == null)
        {
            inventory = GetComponent<InventoryContainer>();
        }

        if (movementModule == null)
        {
            movementModule = GetComponent<PrototypeFpsMovementModule>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<PlayerWeaponController>();
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

#if UNITY_EDITOR
        if (!Application.isPlaying && movementModule == null)
        {
            movementModule = gameObject.AddComponent<PrototypeFpsMovementModule>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        if (!Application.isPlaying && fpsInput == null)
        {
            fpsInput = gameObject.AddComponent<PrototypeFpsInput>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        if (!Application.isPlaying && interactionState == null)
        {
            interactionState = gameObject.AddComponent<PlayerInteractionState>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        if (!Application.isPlaying && skillManager == null)
        {
            skillManager = gameObject.AddComponent<PlayerSkillManager>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        if (!Application.isPlaying && progressionRuntime == null)
        {
            progressionRuntime = gameObject.AddComponent<PlayerProgressionRuntime>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif

        ApplyControllerSettings();
        movementModule?.SetViewCamera(viewCamera);
    }

    private void EnsureCombatSettings()
    {
        shootDistance = Mathf.Max(shootDistance, 1f);
        baseDamage = Mathf.Max(baseDamage, 1f);
        shootForce = Mathf.Max(shootForce, 0f);
        meleeImpactForce = Mathf.Max(meleeImpactForce, 0f);
        meleeStaminaCost = Mathf.Max(0f, meleeStaminaCost);
        impactMarkerLifetime = Mathf.Max(impactMarkerLifetime, 0.05f);
        medicalUseCooldown = Mathf.Max(0.05f, medicalUseCooldown);
        medicalFeedbackLifetime = Mathf.Max(0.25f, medicalFeedbackLifetime);
        firearmNoiseRadius = Mathf.Max(0f, firearmNoiseRadius);
        meleeNoiseRadius = Mathf.Max(0f, meleeNoiseRadius);
        if (shootMask.value == 0 || shootMask.value == ~0)
        {
            shootMask = Physics.DefaultRaycastLayers;
        }
    }

    private PrototypeFpsInput GetOrCreateInput()
    {
        PrototypeFpsInput input = GetComponent<PrototypeFpsInput>();
        if (input == null)
        {
            input = gameObject.AddComponent<PrototypeFpsInput>();
        }

        return input;
    }

    private PrototypeFpsMovementModule GetOrCreateMovementModule()
    {
        PrototypeFpsMovementModule module = GetComponent<PrototypeFpsMovementModule>();
        if (module == null)
        {
            module = gameObject.AddComponent<PrototypeFpsMovementModule>();
        }

        return module;
    }

    private PlayerInteractionState GetOrCreateInteractionState()
    {
        PlayerInteractionState state = GetComponent<PlayerInteractionState>();
        if (state == null)
        {
            state = gameObject.AddComponent<PlayerInteractionState>();
        }

        return state;
    }
}
