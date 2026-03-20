using System.Text;
using UnityEngine;

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
    private BaseHubOverlayView overlayView;
    private Transform playerTransform;
    private BaseHubZoneMarker activeContainedZoneMarker;

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
        EnsureQuestRuntime();
        EnsureQuestNpcs();
        DisableConflictingUi();
        ApplyArrivalSpawn();
        UpdateZoneVisitEvents();
        SetUiFocus(false);
    }

    private void Update()
    {
        UpdateZoneVisitEvents();
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
        activeContainedZoneMarker = null;
        if (uiOpen)
        {
            CloseMenuInternal(false);
            return;
        }

        overlayView?.SetContent(string.Empty, string.Empty, Vector2.zero, false);
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
        overlayView ??= BaseHubOverlayView.GetOrCreate();
    }

    private void UpdateOverlayUi()
    {
        if (overlayView == null)
        {
            EnsureOverlayUi();
        }

        bool visible = showOverlayHint && !uiOpen;
        string title = string.Empty;
        string body = string.Empty;
        Vector2 size = Vector2.zero;
        if (!visible)
        {
            overlayView?.SetContent(title, body, size, false);
            return;
        }

        BaseHubZoneMarker currentZone = ResolveCurrentZoneMarker();
        title = currentZone != null
            ? $"{hubTitle ?? string.Empty} · {currentZone.ZoneName}"
            : hubTitle ?? string.Empty;
        body = BuildOverlayBodyText();
        float width = Mathf.Clamp(Screen.width - 56f, 320f, 560f);
        float height = currentZone != null ? 176f : 148f;
        size = new Vector2(width, height);
        overlayView?.SetContent(title, body, size, true);
    }

    private void EnsureQuestRuntime()
    {
        PlayerInteractor interactor = fpsController != null
            ? fpsController.GetComponent<PlayerInteractor>()
            : FindFirstObjectByType<PlayerInteractor>();
        QuestManager questManager = QuestManager.GetOrCreate();
        questManager.ConfigureRuntime(null, menuController, interactor, true);
        questManager.TryInitialize();
        DialogueSystem.GetOrCreate();
    }

    private void EnsureQuestNpcs()
    {
        EnsureQuestNpc("NPC_Commander_Anchor", "commander", "指挥官", "主线协调", "先把基地跑一遍，再谈下一步行动。", new Color(0.84f, 0.55f, 0.24f, 1f));
        EnsureQuestNpc("NPC_IntelOfficer_Anchor", "intel_officer", "情报官", "情报整理", "所有补给和情报都得按流程归档。", new Color(0.42f, 0.68f, 0.92f, 1f));
        EnsureQuestNpc("NPC_Trainer_Anchor", "trainer", "训练官", "训练任务", "危险区不会等你准备好，先把动作做扎实。", new Color(0.72f, 0.42f, 0.86f, 1f));
    }

    private void EnsureQuestNpc(
        string anchorName,
        string npcId,
        string npcName,
        string npcTitle,
        string greeting,
        Color accentColor)
    {
        Transform anchor = FindAnchor(anchorName);
        if (anchor == null)
        {
            return;
        }

        QuestNPC questNpc = anchor.GetComponent<QuestNPC>();
        if (questNpc == null)
        {
            questNpc = anchor.gameObject.AddComponent<QuestNPC>();
        }

        questNpc.Configure(npcId, npcName, npcTitle, greeting);
        EnsureQuestNpcVisual(anchor, accentColor);
    }

    private void EnsureQuestNpcVisual(Transform anchor, Color accentColor)
    {
        if (anchor == null || anchor.Find("RuntimeNpcVisual") != null)
        {
            return;
        }

        GameObject visualRoot = new GameObject("RuntimeNpcVisual");
        visualRoot.transform.SetParent(anchor, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(visualRoot.transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.72f, 1.08f, 0.72f);
        ApplyNpcColor(body, accentColor * 0.78f);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(visualRoot.transform, false);
        head.transform.localPosition = new Vector3(0f, 2.05f, 0f);
        head.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
        ApplyNpcColor(head, accentColor);

        GameObject backpack = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backpack.name = "Pack";
        backpack.transform.SetParent(visualRoot.transform, false);
        backpack.transform.localPosition = new Vector3(-0.18f, 1.12f, -0.24f);
        backpack.transform.localScale = new Vector3(0.24f, 0.42f, 0.22f);
        ApplyNpcColor(backpack, new Color(0.14f, 0.16f, 0.2f, 1f));
    }

    private static void ApplyNpcColor(GameObject target, Color color)
    {
        if (target == null)
        {
            return;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.material.color = color;
    }

    private static Transform FindAnchor(string anchorName)
    {
        if (string.IsNullOrWhiteSpace(anchorName))
        {
            return null;
        }

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int index = 0; index < transforms.Length; index++)
        {
            Transform candidate = transforms[index];
            if (candidate != null && string.Equals(candidate.name, anchorName, System.StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
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

    private void UpdateZoneVisitEvents()
    {
        BaseHubZoneMarker containedZone = ResolveContainedZoneMarker();
        if (ReferenceEquals(activeContainedZoneMarker, containedZone))
        {
            return;
        }

        activeContainedZoneMarker = containedZone;
        if (activeContainedZoneMarker == null)
        {
            return;
        }

        string exploreLocationId = activeContainedZoneMarker.QuestExploreLocationId;
        if (!string.IsNullOrWhiteSpace(exploreLocationId))
        {
            QuestEventHub.RaiseExplore(exploreLocationId);
        }
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
        BaseHubZoneMarker containedZone = ResolveContainedZoneMarker(playerPosition);

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
        }

        return containedZone != null ? containedZone : fallbackZone;
    }

    private BaseHubZoneMarker ResolveContainedZoneMarker()
    {
        if (playerTransform == null)
        {
            ResolveReferences();
        }

        return playerTransform == null ? null : ResolveContainedZoneMarker(playerTransform.position);
    }

    private BaseHubZoneMarker ResolveContainedZoneMarker(Vector3 playerPosition)
    {
        if (zoneMarkers == null || zoneMarkers.Length == 0)
        {
            return null;
        }

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
            if (!zoneMarker.Contains(playerPosition) || distanceSqr >= containedDistanceSqr)
            {
                continue;
            }

            containedDistanceSqr = distanceSqr;
            containedZone = zoneMarker;
        }

        return containedZone;
    }
}
