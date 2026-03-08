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

        if (raidGameMode.CurrentState == RaidGameMode.RaidState.Extracted)
        {
            latestProfile.raidBackpackItems = PrototypeProfileService.CaptureInventory(inventory);
            latestProfile.equippedArmorItems = PrototypeProfileService.CaptureDefinitions(GetCurrentArmorDefinitions());
        }
        else
        {
            latestProfile.raidBackpackItems.Clear();
            latestProfile.equippedArmorItems.Clear();
            latestProfile.equippedPrimaryWeaponId = string.Empty;
            latestProfile.equippedSecondaryWeaponId = string.Empty;
            latestProfile.equippedMeleeWeaponId = string.Empty;
        }

        latestProfile.loadoutItems.Clear();
        latestProfile.extractedItems.Clear();
        PrototypeProfileService.SaveProfile(latestProfile, itemCatalog);
        profile = latestProfile;
        resultSaved = true;

        Debug.Log(
            $"[PrototypeRaidProfileFlow] Saved raid result {raidGameMode.CurrentState} to {PrototypeProfileService.SavePath}. " +
            $"Backpack stacks: {profile.raidBackpackItems.Count}, equipped armor: {profile.equippedArmorItems.Count}, " +
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
