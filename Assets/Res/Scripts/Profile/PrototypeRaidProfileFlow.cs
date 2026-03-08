using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PrototypeRaidProfileFlow : MonoBehaviour
{
    [SerializeField] private PrototypeItemCatalog itemCatalog;
    [SerializeField] private RaidGameMode raidGameMode;
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private PrototypeFpsController fpsController;
    [SerializeField] private PrototypeUnitVitals playerVitals;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool showReturnButton = true;
    [SerializeField] private int secureContainerSlots = 4;
    [SerializeField] private float secureContainerMaxWeight = 6f;
    [SerializeField] private int specialEquipmentSlots = 4;
    [SerializeField] private float specialEquipmentMaxWeight = 8f;

    private PrototypeProfileService.ProfileData profile;
    private bool loadoutApplied;
    private bool resultSaved;
    private GUIStyle buttonStyle;

    private void Awake()
    {
        ResolveReferences();
        ResolveCatalog();
        ApplyLoadoutToRaidState();
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

    private void OnGUI()
    {
        if (!showReturnButton || raidGameMode == null || raidGameMode.IsRunning)
        {
            return;
        }

        EnsureStyles();
        if (GUI.Button(new Rect(Screen.width - 220f, Screen.height - 76f, 180f, 42f), "Return To Menu", buttonStyle))
        {
            PersistRaidOutcomeIfNeeded();
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    public void Configure(RaidGameMode gameMode, PlayerInteractor interactor, PrototypeItemCatalog catalog, string menuSceneName)
    {
        raidGameMode = gameMode;
        playerInteractor = interactor;
        itemCatalog = catalog;
        mainMenuSceneName = string.IsNullOrWhiteSpace(menuSceneName) ? "MainMenu" : menuSceneName.Trim();
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
        PrototypeProfileService.PopulateInventory(inventory, profile != null ? profile.raidBackpackItems : null, itemCatalog);
        InventoryContainer secureContainer = GetOrCreateAuxInventory("SecureContainer_Runtime", "Secure Container", secureContainerSlots, secureContainerMaxWeight);
        InventoryContainer specialEquipment = GetOrCreateAuxInventory("SpecialEquipment_Runtime", "Special Equipment", specialEquipmentSlots, specialEquipmentMaxWeight);
        PrototypeProfileService.PopulateInventory(secureContainer, profile != null ? profile.secureContainerItems : null, itemCatalog);
        PrototypeProfileService.PopulateInventory(specialEquipment, profile != null ? profile.specialEquipmentItems : null, itemCatalog);
        playerInteractor.Configure(playerInteractor.InteractionCamera, inventory, secureContainer, specialEquipment);

        if (playerVitals != null)
        {
            List<ArmorDefinition> equippedArmor = PrototypeProfileService.ResolveArmorDefinitions(
                profile != null ? profile.equippedArmorItems : null,
                itemCatalog);
            playerVitals.SetArmorLoadout(equippedArmor.ToArray());
        }

        if (fpsController != null && itemCatalog != null)
        {
            fpsController.ConfigureWeaponLoadout(
                itemCatalog.FindWeaponById(profile != null ? profile.equippedPrimaryWeaponId : null),
                itemCatalog.FindWeaponById(profile != null ? profile.equippedSecondaryWeaponId : null),
                itemCatalog.FindWeaponById(profile != null ? profile.equippedMeleeWeaponId : null));
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
        InventoryContainer secureContainer = playerInteractor != null ? playerInteractor.SecureInventory : null;
        InventoryContainer specialEquipment = playerInteractor != null ? playerInteractor.SpecialInventory : null;
        PrototypeWeaponDefinition currentPrimaryWeapon = fpsController != null ? fpsController.EquippedPrimaryWeapon : itemCatalog != null ? itemCatalog.FindWeaponById(latestProfile.equippedPrimaryWeaponId) : null;
        PrototypeWeaponDefinition currentSecondaryWeapon = fpsController != null ? fpsController.EquippedSecondaryWeapon : itemCatalog != null ? itemCatalog.FindWeaponById(latestProfile.equippedSecondaryWeaponId) : null;
        PrototypeWeaponDefinition currentMeleeWeapon = fpsController != null ? fpsController.EquippedMeleeWeapon : itemCatalog != null ? itemCatalog.FindWeaponById(latestProfile.equippedMeleeWeaponId) : null;

        latestProfile.secureContainerItems = PrototypeProfileService.CaptureInventory(secureContainer);
        latestProfile.specialEquipmentItems = PrototypeProfileService.CaptureInventory(specialEquipment);
        latestProfile.equippedMeleeWeaponId = currentMeleeWeapon != null ? currentMeleeWeapon.WeaponId : string.Empty;

        if (raidGameMode.CurrentState == RaidGameMode.RaidState.Extracted)
        {
            latestProfile.raidBackpackItems = PrototypeProfileService.CaptureInventory(inventory);
            latestProfile.equippedArmorItems = PrototypeProfileService.CaptureDefinitions(GetCurrentArmorDefinitions());
            latestProfile.equippedPrimaryWeaponId = currentPrimaryWeapon != null ? currentPrimaryWeapon.WeaponId : string.Empty;
            latestProfile.equippedSecondaryWeaponId = currentSecondaryWeapon != null ? currentSecondaryWeapon.WeaponId : string.Empty;
        }
        else
        {
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

        Debug.Log(
            $"[PrototypeRaidProfileFlow] Saved raid result {raidGameMode.CurrentState} to {PrototypeProfileService.SavePath}. " +
            $"Backpack stacks: {profile.raidBackpackItems.Count}, secure stacks: {profile.secureContainerItems.Count}, " +
            $"special stacks: {profile.specialEquipmentItems.Count}, equipped armor: {profile.equippedArmorItems.Count}, " +
            $"weapons: {profile.equippedPrimaryWeaponId}/{profile.equippedSecondaryWeaponId}/{profile.equippedMeleeWeaponId}");
    }

    private List<ArmorDefinition> GetCurrentArmorDefinitions()
    {
        var armorDefinitions = new List<ArmorDefinition>();
        if (playerVitals == null || playerVitals.EquippedArmor == null)
        {
            return armorDefinitions;
        }

        for (int index = 0; index < playerVitals.EquippedArmor.Count; index++)
        {
            PrototypeUnitVitals.ArmorState armorState = playerVitals.EquippedArmor[index];
            if (armorState?.definition != null)
            {
                armorDefinitions.Add(armorState.definition);
            }
        }

        return armorDefinitions;
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

    private void ResolveCatalog()
    {
        if (itemCatalog == null)
        {
            itemCatalog = Resources.Load<PrototypeItemCatalog>("PrototypeItemCatalog");
        }
    }

    private void ResolveReferences()
    {
        if (raidGameMode == null)
        {
            raidGameMode = FindFirstObjectByType<RaidGameMode>();
        }

        if (playerInteractor == null)
        {
            playerInteractor = FindFirstObjectByType<PlayerInteractor>();
        }

        if (fpsController == null)
        {
            fpsController = playerInteractor != null
                ? playerInteractor.GetComponent<PrototypeFpsController>()
                : FindFirstObjectByType<PrototypeFpsController>();
        }

        if (playerVitals == null)
        {
            playerVitals = playerInteractor != null
                ? playerInteractor.GetComponent<PrototypeUnitVitals>()
                : FindFirstObjectByType<PrototypeUnitVitals>();
        }
    }

    private void EnsureStyles()
    {
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15
            };
        }
    }
}
