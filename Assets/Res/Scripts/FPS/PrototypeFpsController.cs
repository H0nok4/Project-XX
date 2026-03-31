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
    [SerializeField] private PlayerLookController lookController;
    [SerializeField] private PlayerOrientationController orientationController;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private PlayerMedicalController medicalController;
    [SerializeField] private PlayerThrowableController throwableController;
    [SerializeField] private PlayerSkillManager skillManager;
    [SerializeField] private PlayerProgressionRuntime progressionRuntime;
    [SerializeField] private PlayerHudPresenter hudPresenter;
    [SerializeField] private PlayerActionChannel actionChannel;
    [SerializeField] private PlayerStateHub stateHub;

    private PrototypeFpsMovementModule movementModule;
    private PrototypeFpsInput fpsInput;
    private PlayerInteractionState interactionState;
    private PrototypeUnitVitals playerVitals;
    private InventoryContainer inventory;

    public PrototypeWeaponDefinition EquippedPrimaryWeapon => weaponController != null ? weaponController.EquippedPrimaryWeapon : null;
    public PrototypeWeaponDefinition EquippedSecondaryWeapon => weaponController != null ? weaponController.EquippedSecondaryWeapon : null;
    public PrototypeWeaponDefinition EquippedMeleeWeapon => weaponController != null ? weaponController.EquippedMeleeWeapon : null;

    private void Awake()
    {
        ResolveViewReferences();
        movementModule = GetOrCreateMovementModule();
        fpsInput = GetOrCreateInput();
        interactionState = GetOrCreateInteractionState();
        playerVitals = GetComponent<PrototypeUnitVitals>();
        inventory = GetComponent<InventoryContainer>();
        EnsureCombatSettings();

        weaponController = GetOrCreateWeaponController();
        aimController = GetOrCreateAimController();
        medicalController = GetOrCreateMedicalController();
        throwableController = GetOrCreateThrowableController();
        skillManager = GetOrCreateSkillManager();
        progressionRuntime = GetOrCreateProgressionRuntime();
        lookController = GetOrCreateLookController();
        orientationController = GetOrCreateOrientationController();
        hudPresenter = GetOrCreateHudPresenter();
        actionChannel = GetOrCreateActionChannel();
        stateHub = GetOrCreateStateHub();

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
        aimController?.ResetAimImmediately();
        orientationController?.SnapBodyToCameraYaw();
        weaponController?.RefreshWeaponViewModels();
        stateHub?.RefreshSnapshot();
        hudPresenter?.RefreshHud();
    }

    private void OnDisable()
    {
        LockCursor(false);
        aimController?.ResetAimImmediately();
        hudPresenter?.SetHudVisible(false);
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

        stateHub?.BeginFrame();

        if (interactionState != null && interactionState.IsUiFocused)
        {
            movementModule?.TickMovement();
            if (Cursor.lockState != CursorLockMode.None)
            {
                LockCursor(false);
            }

            aimController?.TickAim(fpsInput, true);
            weaponController?.TickVisuals(Time.deltaTime);
            weaponController?.TickFeedback(Time.deltaTime);
            medicalController?.TickFeedback(Time.deltaTime);
            throwableController?.TickFeedback(Time.deltaTime);
            progressionRuntime?.TickFeedback(Time.deltaTime);
            stateHub?.RefreshSnapshot();
            hudPresenter?.RefreshHud();
            return;
        }

        if (fpsInput.ToggleCursorPressedThisFrame)
        {
            LockCursor(false);
        }
        else if ((fpsInput.ShootPressedThisFrame || fpsInput.AimPressedThisFrame) && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor(true);
        }

        lookController?.TickLook(fpsInput);
        HandleMovement();

        weaponController?.HandleWeaponInput(fpsInput);
        aimController?.TickAim(fpsInput, false);
        orientationController?.TickOrientation();
        if (actionChannel != null)
        {
            actionChannel.ExecuteGameplayActions(fpsInput);
        }
        else
        {
            bool usedMedical = medicalController != null && medicalController.HandleMedicalInput(fpsInput);
            bool usedThrowable = !usedMedical && throwableController != null && throwableController.HandleThrowableInput(fpsInput);
            if (!usedMedical && !usedThrowable)
            {
                weaponController?.HandleCombat(fpsInput);
            }
        }

        weaponController?.TickVisuals(Time.deltaTime);
        weaponController?.TickFeedback(Time.deltaTime);
        medicalController?.TickFeedback(Time.deltaTime);
        throwableController?.TickFeedback(Time.deltaTime);
        progressionRuntime?.TickFeedback(Time.deltaTime);
        stateHub?.RefreshSnapshot();
        hudPresenter?.RefreshHud();
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

        lookController?.ResetPitch();
        orientationController?.SnapBodyToCameraYaw();
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

    public bool TrySetWeaponSlotItem(PrototypeRaidGearSlotType gearSlot, ItemInstance itemInstance, out ItemInstance displacedWeapon)
    {
        displacedWeapon = null;
        if (weaponController == null)
        {
            return false;
        }

        bool result = weaponController.TrySetWeaponSlotItem(gearSlot, itemInstance, out displacedWeapon);
        if (result)
        {
            SyncWeaponDefinitionsFromController();
        }

        return result;
    }

    public bool TryTakeWeaponSlotItem(PrototypeRaidGearSlotType gearSlot, out ItemInstance removedWeapon)
    {
        removedWeapon = null;
        if (weaponController == null)
        {
            return false;
        }

        bool result = weaponController.TryTakeWeaponSlotItem(gearSlot, out removedWeapon);
        if (result)
        {
            SyncWeaponDefinitionsFromController();
        }

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

        if (aimController != null)
        {
            aimController.ApplyHostSettings(viewCamera, weaponController, movementModule);
        }

        if (orientationController != null)
        {
            orientationController.ApplyHostSettings(movementModule, aimController);
        }

        if (medicalController != null)
        {
            medicalController.ApplyHostSettings(medicalUseCooldown, medicalFeedbackLifetime);
        }

        if (throwableController != null)
        {
            throwableController.ApplyHostSettings(viewCamera, medicalFeedbackLifetime);
        }

        if (lookController != null)
        {
            lookController.ApplyHostSettings(viewCamera, mouseSensitivity, maxLookAngle);
        }

        if (actionChannel != null)
        {
            actionChannel.ApplyHostSettings(interactionState, playerVitals, weaponController, medicalController, throwableController);
        }

        if (stateHub != null)
        {
            stateHub.ApplyHostSettings(
                fpsInput,
                interactionState,
                playerVitals,
                movementModule,
                lookController,
                orientationController,
                weaponController,
                aimController,
                medicalController,
                throwableController,
                skillManager,
                progressionRuntime,
                actionChannel,
                GetComponent<PlayerShoulderCameraController>());
        }

        if (hudPresenter != null)
        {
            hudPresenter.ApplyHostSettings(showHud, stateHub);
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

    private PlayerAimController GetOrCreateAimController()
    {
        PlayerAimController controller = aimController != null ? aimController : GetComponent<PlayerAimController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<PlayerAimController>();
        }

        aimController = controller;
        return controller;
    }

    private PlayerOrientationController GetOrCreateOrientationController()
    {
        PlayerOrientationController controller = orientationController != null ? orientationController : GetComponent<PlayerOrientationController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<PlayerOrientationController>();
        }

        orientationController = controller;
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

    private PlayerLookController GetOrCreateLookController()
    {
        PlayerLookController controller = lookController != null ? lookController : GetComponent<PlayerLookController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<PlayerLookController>();
        }

        lookController = controller;
        return controller;
    }

    private PlayerHudPresenter GetOrCreateHudPresenter()
    {
        PlayerHudPresenter presenter = hudPresenter != null ? hudPresenter : GetComponent<PlayerHudPresenter>();
        if (presenter == null)
        {
            presenter = gameObject.AddComponent<PlayerHudPresenter>();
        }

        hudPresenter = presenter;
        return presenter;
    }

    private PlayerActionChannel GetOrCreateActionChannel()
    {
        PlayerActionChannel channel = actionChannel != null ? actionChannel : GetComponent<PlayerActionChannel>();
        if (channel == null)
        {
            channel = gameObject.AddComponent<PlayerActionChannel>();
        }

        actionChannel = channel;
        return channel;
    }

    private PlayerStateHub GetOrCreateStateHub()
    {
        PlayerStateHub hub = stateHub != null ? stateHub : GetComponent<PlayerStateHub>();
        if (hub == null)
        {
            hub = gameObject.AddComponent<PlayerStateHub>();
        }

        stateHub = hub;
        return hub;
    }

    private void LockCursor(bool shouldLock)
    {
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    private void OnValidate()
    {
        EnsureCombatSettings();
        ResolveViewReferences();

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

        if (lookController == null)
        {
            lookController = GetComponent<PlayerLookController>();
        }

        if (orientationController == null)
        {
            orientationController = GetComponent<PlayerOrientationController>();
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

        if (hudPresenter == null)
        {
            hudPresenter = GetComponent<PlayerHudPresenter>();
        }

        if (actionChannel == null)
        {
            actionChannel = GetComponent<PlayerActionChannel>();
        }

        if (stateHub == null)
        {
            stateHub = GetComponent<PlayerStateHub>();
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

        if (!Application.isPlaying && aimController == null)
        {
            aimController = gameObject.AddComponent<PlayerAimController>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        if (!Application.isPlaying && lookController == null)
        {
            lookController = gameObject.AddComponent<PlayerLookController>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        if (!Application.isPlaying && orientationController == null)
        {
            orientationController = gameObject.AddComponent<PlayerOrientationController>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        if (!Application.isPlaying && progressionRuntime == null)
        {
            progressionRuntime = gameObject.AddComponent<PlayerProgressionRuntime>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        if (!Application.isPlaying && hudPresenter == null)
        {
            hudPresenter = gameObject.AddComponent<PlayerHudPresenter>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        if (!Application.isPlaying && actionChannel == null)
        {
            actionChannel = gameObject.AddComponent<PlayerActionChannel>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        if (!Application.isPlaying && stateHub == null)
        {
            stateHub = gameObject.AddComponent<PlayerStateHub>();
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

    private void ResolveViewReferences()
    {
        PlayerAnimationRigRefs rigRefs = GetComponent<PlayerAnimationRigRefs>();
        if (viewCamera == null)
        {
            viewCamera = rigRefs != null ? rigRefs.ViewCamera : GetComponentInChildren<Camera>();
        }

        if (muzzle == null)
        {
            muzzle = rigRefs != null ? rigRefs.Muzzle : null;
        }

        if (muzzle == null && viewCamera != null)
        {
            Transform muzzleTransform = viewCamera.transform.Find("Muzzle");
            if (muzzleTransform != null)
            {
                muzzle = muzzleTransform;
            }
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
