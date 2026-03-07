using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PrototypeRaidProfileFlow : MonoBehaviour
{
    [SerializeField] private PrototypeItemCatalog itemCatalog;
    [SerializeField] private RaidGameMode raidGameMode;
    [SerializeField] private PlayerInteractor playerInteractor;
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
        ApplyLoadoutToRaidInventory();
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
    }

    private void HandleRaidStateChanged(RaidGameMode.RaidState nextState)
    {
        if (nextState == RaidGameMode.RaidState.Running || nextState == RaidGameMode.RaidState.Idle)
        {
            return;
        }

        PersistRaidOutcomeIfNeeded();
    }

    private void ApplyLoadoutToRaidInventory()
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
        PrototypeProfileService.PopulateInventory(inventory, profile != null ? profile.loadoutItems : null, itemCatalog);
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

        if (profile == null)
        {
            profile = PrototypeProfileService.LoadProfile(itemCatalog);
        }

        if (raidGameMode.CurrentState == RaidGameMode.RaidState.Extracted)
        {
            PrototypeProfileService.MergeInventoryIntoStash(profile, inventory);
        }

        profile.loadoutItems.Clear();
        PrototypeProfileService.SaveProfile(profile, itemCatalog);
        resultSaved = true;
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
