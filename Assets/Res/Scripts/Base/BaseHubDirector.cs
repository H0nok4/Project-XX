using System.Text;
using UnityEngine;
using UnityEngine.UI;

public enum BaseHubInteractionKind
{
    Deploy = 0,
    Warehouse = 1,
    Merchants = 2
}

[DisallowMultipleComponent]
public class BaseHubDirector : MonoBehaviour
{
    [SerializeField] private string hubTitle = "基地";
    [TextArea(2, 4)]
    [SerializeField] private string hubHint = "E：交互  |  Esc：关闭界面";
    [TextArea(1, 3)]
    [SerializeField] private string navigationLegend = "← 商人区  ·  ↑ 准备区  ·  → 仓库区  ·  ↓ 任务区";
    [SerializeField] private bool showOverlayHint = true;

    [Header("References")]
    [SerializeField] private PrototypeFpsController fpsController;
    [SerializeField] private PrototypeFpsInput fpsInput;
    [SerializeField] private PlayerInteractionState interactionState;
    [SerializeField] private PrototypeMainMenuController menuController;
    [SerializeField] private BaseFacilityManager facilityManager;
    [SerializeField] private Transform departureArrivalPoint;
    [SerializeField] private Transform respawnArrivalPoint;
    [SerializeField] private BaseHubZoneMarker[] zoneMarkers;

    private bool uiOpen;
    private RectTransform overlayRoot;
    private Text overlayTitleText;
    private Text overlayBodyText;
    private Transform playerTransform;

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

        if (interactionKind == BaseHubInteractionKind.Merchants)
        {
            OpenMerchantDirectory();
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

    public void OpenMerchantDirectory()
    {
        ResolveReferences();
        if (menuController == null)
        {
            return;
        }

        menuController.ShowMerchantDirectory();
        uiOpen = true;
        SetUiFocus(true);
    }

    public bool OpenMerchant(string merchantId, string merchantName = null)
    {
        ResolveReferences();
        if (menuController == null)
        {
            return false;
        }

        bool opened = menuController.ShowMerchant(merchantId, merchantName);
        if (!opened)
        {
            return false;
        }

        uiOpen = true;
        SetUiFocus(true);
        return true;
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

        if (page == PrototypeMainMenuController.MenuPage.Merchants)
        {
            menuController.ShowMerchantDirectory();
        }
        else
        {
            menuController.ShowPage(page);
        }

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

        BaseHubArrivalMode arrivalMode = MetaEntryRouter.ConsumeBaseHubArrivalMode();
        Transform spawnPoint = ResolveArrivalSpawnPoint(arrivalMode);
        if (spawnPoint == null)
        {
            return;
        }

        fpsController.ApplySpawnPose(spawnPoint.position, spawnPoint.rotation);
        facilityManager?.HandleArrival(arrivalMode);
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

        if (playerTransform == null && fpsController != null)
        {
            playerTransform = fpsController.transform;
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

        if (facilityManager == null)
        {
            facilityManager = FindFirstObjectByType<BaseFacilityManager>();
        }

        if (zoneMarkers == null || zoneMarkers.Length == 0)
        {
            zoneMarkers = FindObjectsByType<BaseHubZoneMarker>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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
            BaseHubZoneMarker currentZone = ResolveCurrentZoneMarker();
            overlayTitleText.text = currentZone != null
                ? $"{hubTitle ?? string.Empty} · {currentZone.ZoneName}"
                : hubTitle ?? string.Empty;
        }

        if (overlayBodyText != null)
        {
            overlayBodyText.text = BuildOverlayBodyText();
        }

        if (overlayRoot != null)
        {
            float width = Mathf.Clamp(Screen.width - 56f, 320f, 560f);
            float height = ResolveCurrentZoneMarker() != null ? 176f : 148f;
            overlayRoot.sizeDelta = new Vector2(width, height);
        }
    }

    private void OnDestroy()
    {
        if (overlayRoot != null)
        {
            Destroy(overlayRoot.gameObject);
        }
    }

    private string BuildOverlayBodyText()
    {
        StringBuilder builder = new StringBuilder(192);
        if (!string.IsNullOrWhiteSpace(hubHint))
        {
            builder.Append(hubHint.Trim());
        }

        BaseHubZoneMarker currentZone = ResolveCurrentZoneMarker();
        if (currentZone != null)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append("当前区域：");
            builder.Append(currentZone.ZoneName);

            if (!string.IsNullOrWhiteSpace(currentZone.ZoneSummary))
            {
                builder.Append('\n');
                builder.Append(currentZone.ZoneSummary);
            }
        }

        if (!string.IsNullOrWhiteSpace(navigationLegend))
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append("导航：");
            builder.Append(navigationLegend.Trim());
        }

        return builder.ToString();
    }

    private BaseHubZoneMarker ResolveCurrentZoneMarker()
    {
        if (playerTransform == null)
        {
            ResolveReferences();
        }

        if (playerTransform == null || zoneMarkers == null || zoneMarkers.Length == 0)
        {
            return null;
        }

        Vector3 playerPosition = playerTransform.position;
        BaseHubZoneMarker fallbackZone = null;
        float fallbackDistanceSqr = float.PositiveInfinity;

        BaseHubZoneMarker containedZone = null;
        float containedDistanceSqr = float.PositiveInfinity;

        for (int index = 0; index < zoneMarkers.Length; index++)
        {
            BaseHubZoneMarker zoneMarker = zoneMarkers[index];
            if (zoneMarker == null || !zoneMarker.isActiveAndEnabled)
            {
                continue;
            }

            float distanceSqr = zoneMarker.GetPlanarDistanceSqr(playerPosition);
            if (distanceSqr < fallbackDistanceSqr)
            {
                fallbackDistanceSqr = distanceSqr;
                fallbackZone = zoneMarker;
            }

            if (!zoneMarker.Contains(playerPosition) || distanceSqr >= containedDistanceSqr)
            {
                continue;
            }

            containedDistanceSqr = distanceSqr;
            containedZone = zoneMarker;
        }

        return containedZone != null ? containedZone : fallbackZone;
    }
}
