using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PrototypeRaidProfileFlow : MonoBehaviour
{
    [SerializeField] private PrototypeItemCatalog itemCatalog;
    [SerializeField] private RaidGameMode raidGameMode;
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private PrototypeFpsController fpsController;
    [SerializeField] private PrototypeUnitVitals playerVitals;
    [SerializeField] private PrototypeRaidEquipmentController raidEquipmentController;
    [FormerlySerializedAs("mainMenuSceneName")]
    [SerializeField] private string fallbackMetaSceneName = "MainMenu";
    [SerializeField] private bool showReturnButton = true;
    [SerializeField] private int secureContainerSlots = 4;
    [SerializeField] private float secureContainerMaxWeight = 6f;
    [SerializeField] private int specialEquipmentSlots = 4;
    [SerializeField] private float specialEquipmentMaxWeight = 8f;

    private PrototypeProfileService.ProfileData profile;
    private bool loadoutApplied;
    private bool resultSaved;
    private RectTransform returnButtonRoot;
    private Button returnButton;

    private void Awake()
    {
        ResolveReferences();
        ResolveCatalog();
        ApplyLoadoutToRaidState();
        EnsureQuestRuntime();
        EnsureReturnButtonUi();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (raidGameMode != null)
        {
            raidGameMode.StateChanged += HandleRaidStateChanged;
        }
    }

    private void OnDisable()
    {
        if (raidGameMode != null)
        {
            raidGameMode.StateChanged -= HandleRaidStateChanged;
        }

        PrototypeUiToolkit.SetVisible(returnButtonRoot, false);
    }

    private void OnValidate()
    {
        ResolveReferences();
        ResolveCatalog();
        secureContainerSlots = Mathf.Max(1, secureContainerSlots);
        secureContainerMaxWeight = Mathf.Max(0f, secureContainerMaxWeight);
        specialEquipmentSlots = Mathf.Max(1, specialEquipmentSlots);
        specialEquipmentMaxWeight = Mathf.Max(0f, specialEquipmentMaxWeight);
    }

    private void Update()
    {
        UpdateReturnButtonUi();
    }

    public void Configure(RaidGameMode gameMode, PlayerInteractor interactor, PrototypeItemCatalog catalog, string menuSceneName)
    {
        raidGameMode = gameMode;
        playerInteractor = interactor;
        itemCatalog = catalog;
        fallbackMetaSceneName = string.IsNullOrWhiteSpace(menuSceneName) ? "MainMenu" : menuSceneName.Trim();
        ResolveReferences();
    }

    private void HandleRaidStateChanged(RaidGameMode.RaidState nextState)
    {
        if (nextState == RaidGameMode.RaidState.Running || nextState == RaidGameMode.RaidState.Idle)
        {
            return;
        }

        PersistRaidOutcomeIfNeeded();
    }

    private void ApplyLoadoutToRaidState()
    {
        if (loadoutApplied)
        {
            return;
        }

        ResolveReferences();
        ResolveCatalog();

        InventoryContainer inventory = playerInteractor != null ? playerInteractor.PrimaryInventory : null;
        if (inventory == null)
        {
            return;
        }

        profile = PrototypeProfileService.LoadProfile(itemCatalog);
        PrototypeProfileService.PopulateInventoryInstances(inventory, profile != null ? profile.raidBackpackItemInstances : null, itemCatalog);
        InventoryContainer secureContainer = GetOrCreateAuxInventory("SecureContainer_Runtime", "Secure Container", secureContainerSlots, secureContainerMaxWeight);
        InventoryContainer specialEquipment = GetOrCreateAuxInventory("SpecialEquipment_Runtime", "Special Equipment", specialEquipmentSlots, specialEquipmentMaxWeight);
        PrototypeProfileService.PopulateInventoryInstances(secureContainer, profile != null ? profile.secureContainerItemInstances : null, itemCatalog);
        PrototypeProfileService.PopulateInventoryInstances(specialEquipment, profile != null ? profile.specialEquipmentItemInstances : null, itemCatalog);
        playerInteractor.Configure(playerInteractor.InteractionCamera, inventory, secureContainer, specialEquipment);

        if (playerVitals != null)
        {
            List<ArmorInstance> equippedArmor = PrototypeProfileService.ResolveArmorInstances(
                profile != null ? profile.equippedArmorInstances : null,
                itemCatalog);
            playerVitals.SetArmorInstances(equippedArmor);
        }

        if (fpsController != null && itemCatalog != null)
        {
            fpsController.ConfigureWeaponLoadout(
                PrototypeProfileService.ResolveWeaponInstance(
                    profile != null ? profile.equippedPrimaryWeaponInstance : null,
                    itemCatalog),
                PrototypeProfileService.ResolveWeaponInstance(
                    profile != null ? profile.equippedSecondaryWeaponInstance : null,
                    itemCatalog),
                PrototypeProfileService.ResolveWeaponInstance(
                    profile != null ? profile.equippedMeleeWeaponInstance : null,
                    itemCatalog));
            fpsController.ConfigurePlayerProgression(profile != null ? profile.progression : null);
        }

        if (raidEquipmentController != null)
        {
            raidEquipmentController.Configure(
                itemCatalog,
                PrototypeProfileService.ResolveItemInstance(
                    profile != null ? profile.equippedSecureContainerInstance : null,
                    itemCatalog));
        }

        loadoutApplied = true;
    }

    private void PersistRaidOutcomeIfNeeded()
    {
        if (resultSaved)
        {
            return;
        }

        ResolveReferences();
        ResolveCatalog();

        InventoryContainer inventory = playerInteractor != null ? playerInteractor.PrimaryInventory : null;
        if (raidGameMode == null || inventory == null)
        {
            return;
        }

        PrototypeProfileService.ProfileData latestProfile = PrototypeProfileService.LoadProfile(itemCatalog)
            ?? PrototypeProfileService.CreateDefaultProfile(itemCatalog);
        latestProfile.progression ??= new PlayerProgressionData();
        if (fpsController != null)
        {
            fpsController.CopyPlayerProgressionTo(latestProfile.progression);
        }
        else if (profile != null && profile.progression != null)
        {
            PrototypePlayerProgressionUtility.Copy(profile.progression, latestProfile.progression);
        }
        InventoryContainer secureContainer = playerInteractor != null ? playerInteractor.SecureInventory : null;
        InventoryContainer specialEquipment = playerInteractor != null ? playerInteractor.SpecialInventory : null;
        ItemInstance currentPrimaryWeapon = fpsController != null
            ? fpsController.GetPrimaryItemInstance()
            : PrototypeProfileService.ResolveWeaponInstance(latestProfile.equippedPrimaryWeaponInstance, itemCatalog);
        ItemInstance currentSecondaryWeapon = fpsController != null
            ? fpsController.GetSecondaryItemInstance()
            : PrototypeProfileService.ResolveWeaponInstance(latestProfile.equippedSecondaryWeaponInstance, itemCatalog);
        ItemInstance currentMeleeWeapon = fpsController != null
            ? fpsController.GetMeleeItemInstance()
            : PrototypeProfileService.ResolveWeaponInstance(latestProfile.equippedMeleeWeaponInstance, itemCatalog);

        latestProfile.secureContainerItemInstances = PrototypeProfileService.CaptureInventoryInstances(secureContainer);
        latestProfile.specialEquipmentItemInstances = PrototypeProfileService.CaptureInventoryInstances(specialEquipment);
        latestProfile.equippedSecureContainerInstance = PrototypeProfileService.CaptureItemInstance(
            raidEquipmentController != null ? raidEquipmentController.EquippedSecureContainerItem : null);
        latestProfile.equippedMeleeWeaponInstance = PrototypeProfileService.CaptureWeaponInstance(currentMeleeWeapon);

        latestProfile.secureContainerItems = PrototypeProfileService.CaptureInventory(secureContainer);
        latestProfile.specialEquipmentItems = PrototypeProfileService.CaptureInventory(specialEquipment);
        latestProfile.equippedMeleeWeaponId = currentMeleeWeapon != null && currentMeleeWeapon.WeaponDefinition != null
            ? currentMeleeWeapon.WeaponDefinition.WeaponId
            : string.Empty;

        if (raidGameMode.CurrentState == RaidGameMode.RaidState.Extracted)
        {
            latestProfile.raidBackpackItemInstances = PrototypeProfileService.CaptureInventoryInstances(inventory);
            latestProfile.raidBackpackWeaponInstances.Clear();
            latestProfile.equippedArmorInstances = PrototypeProfileService.CaptureArmorInstances(
                playerVitals != null ? playerVitals.EquippedArmor : null);
            latestProfile.equippedPrimaryWeaponInstance = PrototypeProfileService.CaptureWeaponInstance(currentPrimaryWeapon);
            latestProfile.equippedSecondaryWeaponInstance = PrototypeProfileService.CaptureWeaponInstance(currentSecondaryWeapon);

            latestProfile.raidBackpackItems = PrototypeProfileService.CaptureInventory(inventory);
            latestProfile.equippedArmorItems = PrototypeProfileService.CaptureArmorDefinitions(
                playerVitals != null ? playerVitals.EquippedArmor : null);
            latestProfile.equippedPrimaryWeaponId = currentPrimaryWeapon != null && currentPrimaryWeapon.WeaponDefinition != null
                ? currentPrimaryWeapon.WeaponDefinition.WeaponId
                : string.Empty;
            latestProfile.equippedSecondaryWeaponId = currentSecondaryWeapon != null && currentSecondaryWeapon.WeaponDefinition != null
                ? currentSecondaryWeapon.WeaponDefinition.WeaponId
                : string.Empty;
        }
        else
        {
            latestProfile.raidBackpackItemInstances.Clear();

            latestProfile.raidBackpackWeaponInstances.Clear();
            latestProfile.equippedArmorInstances.Clear();
            latestProfile.equippedPrimaryWeaponInstance = null;
            latestProfile.equippedSecondaryWeaponInstance = null;

            latestProfile.raidBackpackItems.Clear();
            latestProfile.equippedArmorItems.Clear();
            latestProfile.equippedPrimaryWeaponId = string.Empty;
            latestProfile.equippedSecondaryWeaponId = string.Empty;
        }

        latestProfile.loadoutItems.Clear();
        latestProfile.extractedItems.Clear();
        PrototypeProfileService.SaveProfile(latestProfile, itemCatalog);
        profile = latestProfile;
        resultSaved = true;
        MetaEntryRouter.RecordRaidReturnArrival(raidGameMode.CurrentState);

        Debug.Log(
            $"[PrototypeRaidProfileFlow] Saved raid result {raidGameMode.CurrentState} to {PrototypeProfileService.SavePath}. " +
            $"Backpack stacks: {profile.raidBackpackItems.Count}, secure stacks: {profile.secureContainerItems.Count}, " +
            $"special stacks: {profile.specialEquipmentItems.Count}, equipped armor: {profile.equippedArmorItems.Count}, " +
            $"weapons: {profile.equippedPrimaryWeaponId}/{profile.equippedSecondaryWeaponId}/{profile.equippedMeleeWeaponId}");
    }

    private InventoryContainer GetOrCreateAuxInventory(string objectName, string label, int slots, float maxWeight)
    {
        if (playerInteractor == null)
        {
            return null;
        }

        Transform existingChild = playerInteractor.transform.Find(objectName);
        GameObject containerObject = existingChild != null ? existingChild.gameObject : new GameObject(objectName);
        containerObject.transform.SetParent(playerInteractor.transform, false);

        InventoryContainer inventory = containerObject.GetComponent<InventoryContainer>();
        if (inventory == null)
        {
            inventory = containerObject.AddComponent<InventoryContainer>();
        }

        inventory.Configure(label, Mathf.Max(1, slots), Mathf.Max(0f, maxWeight));
        return inventory;
    }

    private void EnsureQuestRuntime()
    {
        QuestManager questManager = QuestManager.GetOrCreate();
        questManager.ConfigureRuntime(itemCatalog, null, playerInteractor, true);
        questManager.TryInitialize();
    }

    private void ResolveCatalog()
    {
        if (itemCatalog == null)
        {
            itemCatalog = Resources.Load<PrototypeItemCatalog>("PrototypeItemCatalog");
        }
    }

    private void ResolveReferences()
    {
        bool allowSceneSearch = Application.isPlaying;

        if (raidGameMode == null)
        {
            raidGameMode = FindFirstObjectByType<RaidGameMode>();
        }

        if (playerInteractor == null && allowSceneSearch)
        {
            playerInteractor = FindFirstObjectByType<PlayerInteractor>();
        }

        if (fpsController == null)
        {
            fpsController = playerInteractor != null
                ? playerInteractor.GetComponent<PrototypeFpsController>()
                : allowSceneSearch ? FindFirstObjectByType<PrototypeFpsController>() : null;
        }

        if (playerVitals == null)
        {
            playerVitals = playerInteractor != null
                ? playerInteractor.GetComponent<PrototypeUnitVitals>()
                : allowSceneSearch ? FindFirstObjectByType<PrototypeUnitVitals>() : null;
        }

        if (raidEquipmentController == null)
        {
            raidEquipmentController = playerInteractor != null
                ? playerInteractor.GetComponent<PrototypeRaidEquipmentController>()
                : allowSceneSearch ? FindFirstObjectByType<PrototypeRaidEquipmentController>() : null;
        }

        if (raidEquipmentController == null && playerInteractor != null && Application.isPlaying)
        {
            raidEquipmentController = playerInteractor.gameObject.AddComponent<PrototypeRaidEquipmentController>();
        }
    }

    private void EnsureReturnButtonUi()
    {
        if (returnButtonRoot != null)
        {
            return;
        }

        PrototypeRuntimeUiManager manager = PrototypeRuntimeUiManager.GetOrCreate();
        RectTransform layerRoot = manager.GetLayerRoot(PrototypeUiLayer.Modal);
        returnButtonRoot = PrototypeUiToolkit.CreateRectTransform("RaidReturnButton", layerRoot);
        PrototypeUiToolkit.SetAnchor(
            returnButtonRoot,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-40f, 34f),
            new Vector2(180f, 42f));
        returnButton = PrototypeUiToolkit.CreateButton(
            returnButtonRoot,
            manager.RuntimeFont,
            "Return To Menu",
            HandleReturnToMenu,
            new Color(0.2f, 0.27f, 0.36f, 0.98f),
            new Color(0.29f, 0.38f, 0.49f, 1f),
            new Color(0.16f, 0.22f, 0.3f, 1f),
            42f);
        PrototypeUiToolkit.SetStretch(returnButton.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        PrototypeUiToolkit.SetVisible(returnButtonRoot, false);
    }

    private void UpdateReturnButtonUi()
    {
        EnsureReturnButtonUi();
        bool visible = showReturnButton && raidGameMode != null && !raidGameMode.IsRunning;
        PrototypeUiToolkit.SetVisible(returnButtonRoot, visible);
    }

    private void HandleReturnToMenu()
    {
        PersistRaidOutcomeIfNeeded();
        MetaEntryRouter.ReturnFromRaid(fallbackMetaSceneName);
    }

    private void OnDestroy()
    {
        if (returnButtonRoot != null)
        {
            Destroy(returnButtonRoot.gameObject);
        }
    }
}
