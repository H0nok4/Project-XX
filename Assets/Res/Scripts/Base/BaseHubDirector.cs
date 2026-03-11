using UnityEngine;

public enum BaseHubInteractionKind
{
    Deploy = 0,
    Warehouse = 1
}

[DisallowMultipleComponent]
public class BaseHubDirector : MonoBehaviour
{
    [SerializeField] private string hubTitle = "基地";
    [TextArea(2, 4)]
    [SerializeField] private string hubHint = "靠近出击终端按 E 可打开出击界面，靠近仓库终端按 E 可管理仓库。按 Esc 可以关闭当前界面。";
    [SerializeField] private bool showOverlayHint = true;

    [Header("References")]
    [SerializeField] private PrototypeFpsController fpsController;
    [SerializeField] private PrototypeFpsInput fpsInput;
    [SerializeField] private PlayerInteractionState interactionState;
    [SerializeField] private PrototypeMainMenuController menuController;
    [SerializeField] private Transform departureArrivalPoint;
    [SerializeField] private Transform respawnArrivalPoint;

    private bool uiOpen;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;

    private void Awake()
    {
        ResolveReferences();
        if (menuController != null)
        {
            menuController.HideUi();
        }
    }

    private void Start()
    {
        ResolveReferences();
        DisableConflictingUi();
        ApplyArrivalSpawn();
        SetUiFocus(false);
    }

    private void Update()
    {
        if (!uiOpen || fpsInput == null)
        {
            return;
        }

        if (fpsInput.ToggleCursorPressedThisFrame || fpsInput.InventoryTogglePressedThisFrame)
        {
            CloseMenu();
        }
    }

    private void OnDisable()
    {
        if (uiOpen)
        {
            CloseMenuInternal(false);
            return;
        }

        SetUiFocus(false);
    }

    private void OnGUI()
    {
        if (!showOverlayHint || uiOpen)
        {
            return;
        }

        EnsureStyles();
        Rect panelRect = new Rect(28f, 28f, Mathf.Min(520f, Screen.width - 56f), 120f);
        GUI.Box(panelRect, string.Empty);

        GUILayout.BeginArea(new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - 28f, panelRect.height - 24f));
        GUILayout.Label(hubTitle, titleStyle);
        GUILayout.Space(4f);
        GUILayout.Label(hubHint, bodyStyle);
        GUILayout.EndArea();
    }

    public void OpenInteraction(BaseHubInteractionKind interactionKind)
    {
        if (interactionKind == BaseHubInteractionKind.Warehouse)
        {
            OpenMenu(PrototypeMainMenuController.MenuPage.Warehouse);
            return;
        }

        OpenMenu(PrototypeMainMenuController.MenuPage.Home);
    }

    public void OpenDeployMenu()
    {
        OpenMenu(PrototypeMainMenuController.MenuPage.Home);
    }

    public void OpenWarehouseMenu()
    {
        OpenMenu(PrototypeMainMenuController.MenuPage.Warehouse);
    }

    public void CloseMenu()
    {
        CloseMenuInternal(true);
    }

    private void OpenMenu(PrototypeMainMenuController.MenuPage page)
    {
        ResolveReferences();
        if (menuController == null)
        {
            return;
        }

        menuController.ShowPage(page);
        uiOpen = true;
        SetUiFocus(true);
    }

    private void CloseMenuInternal(bool saveProfile)
    {
        if (menuController != null)
        {
            if (saveProfile)
            {
                menuController.SaveProfileFromContainers();
            }

            menuController.HideUi();
        }

        uiOpen = false;
        SetUiFocus(false);
    }

    private void ApplyArrivalSpawn()
    {
        if (fpsController == null)
        {
            return;
        }

        Transform spawnPoint = ResolveArrivalSpawnPoint(MetaEntryRouter.ConsumeBaseHubArrivalMode());
        if (spawnPoint == null)
        {
            return;
        }

        fpsController.ApplySpawnPose(spawnPoint.position, spawnPoint.rotation);
    }

    private Transform ResolveArrivalSpawnPoint(BaseHubArrivalMode arrivalMode)
    {
        if (arrivalMode == BaseHubArrivalMode.Respawn && respawnArrivalPoint != null)
        {
            return respawnArrivalPoint;
        }

        if (departureArrivalPoint != null)
        {
            return departureArrivalPoint;
        }

        return respawnArrivalPoint;
    }

    private void DisableConflictingUi()
    {
        if (fpsController == null)
        {
            return;
        }

        PlayerInventoryWindowController inventoryWindowController = fpsController.GetComponent<PlayerInventoryWindowController>();
        if (inventoryWindowController != null)
        {
            inventoryWindowController.enabled = false;
        }

        LootContainerWindowController lootWindowController = fpsController.GetComponent<LootContainerWindowController>();
        if (lootWindowController != null)
        {
            lootWindowController.enabled = false;
        }
    }

    private void SetUiFocus(bool focused)
    {
        if (interactionState != null)
        {
            interactionState.SetUiFocused(this, focused);
        }

        Cursor.lockState = focused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = focused;
    }

    private void ResolveReferences()
    {
        if (fpsController == null)
        {
            fpsController = FindFirstObjectByType<PrototypeFpsController>();
        }

        if (fpsInput == null && fpsController != null)
        {
            fpsInput = fpsController.GetComponent<PrototypeFpsInput>();
        }

        if (interactionState == null && fpsController != null)
        {
            interactionState = fpsController.GetComponent<PlayerInteractionState>();
        }

        if (menuController == null)
        {
            menuController = FindFirstObjectByType<PrototypeMainMenuController>();
        }
    }

    private void EnsureStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (bodyStyle == null)
        {
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.94f, 0.98f, 1f) }
            };
        }
    }
}
