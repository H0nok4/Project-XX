using UnityEngine;
using UnityEngine.UI;

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
    private RectTransform overlayRoot;
    private Text overlayTitleText;
    private Text overlayBodyText;

    private void Awake()
    {
        ResolveReferences();
        EnsureOverlayUi();
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
        UpdateOverlayUi();
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

        PrototypeUiToolkit.SetVisible(overlayRoot, false);
        SetUiFocus(false);
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

    private void EnsureOverlayUi()
    {
        if (overlayRoot != null)
        {
            return;
        }

        PrototypeRuntimeUiManager manager = PrototypeRuntimeUiManager.GetOrCreate();
        RectTransform layerRoot = manager.GetLayerRoot(PrototypeUiLayer.Overlay);
        overlayRoot = PrototypeUiToolkit.CreatePanel(layerRoot, "BaseHubHint", new Color(0.08f, 0.1f, 0.14f, 0.92f), new RectOffset(16, 16, 14, 14), 6f);
        PrototypeUiToolkit.SetAnchor(overlayRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(520f, 120f));

        overlayTitleText = PrototypeUiToolkit.CreateText(
            overlayRoot,
            manager.RuntimeFont,
            hubTitle,
            20,
            FontStyle.Bold,
            Color.white,
            TextAnchor.UpperLeft);
        overlayBodyText = PrototypeUiToolkit.CreateText(
            overlayRoot,
            manager.RuntimeFont,
            hubHint,
            13,
            FontStyle.Normal,
            new Color(0.92f, 0.94f, 0.98f, 1f),
            TextAnchor.UpperLeft);
        PrototypeUiToolkit.SetVisible(overlayRoot, false);
    }

    private void UpdateOverlayUi()
    {
        if (overlayRoot == null)
        {
            EnsureOverlayUi();
        }

        bool visible = showOverlayHint && !uiOpen;
        PrototypeUiToolkit.SetVisible(overlayRoot, visible);
        if (!visible)
        {
            return;
        }

        if (overlayTitleText != null)
        {
            overlayTitleText.text = hubTitle ?? string.Empty;
        }

        if (overlayBodyText != null)
        {
            overlayBodyText.text = hubHint ?? string.Empty;
        }

        if (overlayRoot != null)
        {
            overlayRoot.sizeDelta = new Vector2(Mathf.Min(520f, Screen.width - 56f), 120f);
        }
    }

    private void OnDestroy()
    {
        if (overlayRoot != null)
        {
            Destroy(overlayRoot.gameObject);
        }
    }
}
