using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class BaseHubSceneBuilder
{
    // Trigger-based rebuild keeps BaseScene reproducible without manual editor wiring.
    private const string TriggerAssetPath = "Assets/Res/BaseHubScene.trigger.txt";
    private const string BaseScenePath = "Assets/Scenes/BaseScene.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PlayerPrefabPath = "Assets/Res/Prefabs/Player/BaseHubPlayer.prefab";
    private const string ResourcesFolder = "Assets/Resources";
    private const string MaterialFolder = "Assets/Res/Materials/BaseHub";
    private const string WorldTextMaterialPath = "Assets/Res/Materials/BaseHub/Mat_WorldText.mat";
    private const string WorldTextShaderPath = "Assets/Shaders/WorldTextOccluded.shader";
    private const string WorldTextShaderName = "ProjectXX/WorldTextOccluded";
    private const string FusionPixelFontPath = "Assets/Resources/Fonts/FusionPixel/fusion-pixel-12px-proportional-zh_hans.ttf";
    private const string ItemCatalogPath = "Assets/Resources/PrototypeItemCatalog.asset";
    private const string RouteConfigPath = "Assets/Resources/MetaEntryRouteConfig.asset";
    private const int IgnoreRaycastLayer = 2;

    static BaseHubSceneBuilder()
    {
        EditorApplication.delayCall += TryBuildFromTrigger;
    }

    [MenuItem("Tools/Prototype/Build Base Hub Scene")]
    public static void BuildSceneFromMenu()
    {
        BuildScene();
    }

    public static void BuildSceneFromCommandLine()
    {
        BuildScene();
    }

    private static void TryBuildFromTrigger()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (!AssetDatabase.LoadAssetAtPath<TextAsset>(TriggerAssetPath))
        {
            return;
        }

        BuildScene();
        AssetDatabase.DeleteAsset(TriggerAssetPath);
    }

    private static void BuildScene()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Res/Materials");
        EnsureFolder(MaterialFolder);
        EnsureFolder(ResourcesFolder);

        PrototypeItemCatalog itemCatalog = AssetDatabase.LoadAssetAtPath<PrototypeItemCatalog>(ItemCatalogPath);
        if (itemCatalog == null)
        {
            Debug.LogError($"[BaseHubSceneBuilder] Missing item catalog at '{ItemCatalogPath}'.");
            return;
        }

        EnsureMetaEntryRouteConfig();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BaseScene";

        Material floorMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_BaseFloor.mat", BaseHubBlockoutStandards.NeutralColor);
        Material wallMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_BaseWall.mat", new Color(0.58f, 0.62f, 0.68f, 1f));
        Material ceilingMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_BaseCeiling.mat", new Color(0.84f, 0.86f, 0.9f, 1f));
        Material accentMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_BaseAccent.mat", new Color(0.88f, 0.48f, 0.18f, 1f));
        Material readyMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_ReadyRoom.mat", BaseHubBlockoutStandards.ReadyColor);
        Material warehouseMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Warehouse.mat", BaseHubBlockoutStandards.WarehouseColor);
        Material merchantMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Merchant.mat", BaseHubBlockoutStandards.MerchantColor);
        Material taskMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Task.mat", BaseHubBlockoutStandards.MissionColor);
        Material medicalMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Medical.mat", BaseHubBlockoutStandards.RecoveryColor);
        Material signMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Sign.mat", new Color(0.11f, 0.14f, 0.19f, 1f));
        Material trimMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Trim.mat", new Color(0.15f, 0.18f, 0.22f, 1f));

        ConfigureDirectionalLight();

        GameObject root = new GameObject(BaseHubBlockoutStandards.RootName);
        Transform shellRoot = CreateGroup(root.transform, BaseHubBlockoutStandards.ShellRootName);
        Transform readyRoot = CreateGroup(root.transform, BaseHubBlockoutStandards.ReadyZoneRootName);
        Transform warehouseRoot = CreateGroup(root.transform, BaseHubBlockoutStandards.WarehouseZoneRootName);
        Transform merchantRoot = CreateGroup(root.transform, BaseHubBlockoutStandards.MerchantZoneRootName);
        Transform missionRoot = CreateGroup(root.transform, BaseHubBlockoutStandards.MissionZoneRootName);
        Transform recoveryRoot = CreateGroup(root.transform, BaseHubBlockoutStandards.RecoveryZoneRootName);
        Transform gameplayRoot = CreateGroup(root.transform, BaseHubBlockoutStandards.GameplayRootName);
        Transform wayfindingRoot = CreateGroup(root.transform, BaseHubBlockoutStandards.WayfindingRootName);
        Transform lightingRoot = CreateGroup(root.transform, BaseHubBlockoutStandards.LightingRootName);
        CreateGroup(root.transform, BaseHubBlockoutStandards.DebugRootName);

        CreateBox("BH_Shell_Floor", shellRoot, Vector3.zero, new Vector3(30f, 0.4f, 26f), floorMaterial);
        CreateBox("BH_Shell_Ceiling", shellRoot, new Vector3(0f, BaseHubBlockoutStandards.CeilingHeight, 0f), new Vector3(30f, 0.3f, 26f), ceilingMaterial);
        CreateBox("BH_Shell_Wall_North", shellRoot, new Vector3(0f, 2.3f, 13f), new Vector3(30f, BaseHubBlockoutStandards.CeilingHeight, 0.4f), wallMaterial);
        CreateBox("BH_Shell_Wall_South", shellRoot, new Vector3(0f, 2.3f, -13f), new Vector3(30f, BaseHubBlockoutStandards.CeilingHeight, 0.4f), wallMaterial);
        CreateBox("BH_Shell_Wall_East", shellRoot, new Vector3(15f, 2.3f, 0f), new Vector3(0.4f, BaseHubBlockoutStandards.CeilingHeight, 26f), wallMaterial);
        CreateBox("BH_Shell_Wall_West", shellRoot, new Vector3(-15f, 2.3f, 0f), new Vector3(0.4f, BaseHubBlockoutStandards.CeilingHeight, 26f), wallMaterial);
        CreateBox("BH_Shell_Divider_North_Left", shellRoot, new Vector3(-6.8f, 1.7f, 4.8f), new Vector3(9.6f, 3.4f, 0.4f), wallMaterial);
        CreateBox("BH_Shell_Divider_North_Right", shellRoot, new Vector3(6.8f, 1.7f, 4.8f), new Vector3(9.6f, 3.4f, 0.4f), wallMaterial);
        CreateBox("BH_Shell_Divider_South_Left", shellRoot, new Vector3(-7.2f, 1.7f, -4.8f), new Vector3(10.4f, 3.4f, 0.4f), wallMaterial);
        CreateBox("BH_Shell_Divider_South_Right", shellRoot, new Vector3(7.2f, 1.7f, -4.8f), new Vector3(10.4f, 3.4f, 0.4f), wallMaterial);
        CreateBox("BH_Shell_Divider_West_North", shellRoot, new Vector3(-4.8f, 1.7f, 5.8f), new Vector3(0.4f, 3.4f, 8.4f), wallMaterial);
        CreateBox("BH_Shell_Divider_West_South", shellRoot, new Vector3(-4.8f, 1.7f, -6.4f), new Vector3(0.4f, 3.4f, 5.2f), wallMaterial);
        CreateBox("BH_Shell_Divider_East_North", shellRoot, new Vector3(4.8f, 1.7f, 5.8f), new Vector3(0.4f, 3.4f, 8.4f), wallMaterial);
        CreateBox("BH_Shell_Divider_East_South", shellRoot, new Vector3(4.8f, 1.7f, -6.4f), new Vector3(0.4f, 3.4f, 5.2f), wallMaterial);
        CreateBox("BH_Shell_Pillar_NW", shellRoot, new Vector3(-4.8f, 1.7f, 4.8f), new Vector3(0.8f, 3.4f, 0.8f), trimMaterial);
        CreateBox("BH_Shell_Pillar_NE", shellRoot, new Vector3(4.8f, 1.7f, 4.8f), new Vector3(0.8f, 3.4f, 0.8f), trimMaterial);
        CreateBox("BH_Shell_Pillar_SW", shellRoot, new Vector3(-4.8f, 1.7f, -4.8f), new Vector3(0.8f, 3.4f, 0.8f), trimMaterial);
        CreateBox("BH_Shell_Pillar_SE", shellRoot, new Vector3(4.8f, 1.7f, -4.8f), new Vector3(0.8f, 3.4f, 0.8f), trimMaterial);

        CreateBox("BH_Wayfinding_CentralStrip", wayfindingRoot, new Vector3(0f, 0.03f, 0f), Quaternion.identity, new Vector3(6f, 0.05f, 6f), accentMaterial, false);
        CreateBox("BH_Wayfinding_ReadyStrip", wayfindingRoot, new Vector3(0f, 0.03f, 9.1f), Quaternion.identity, new Vector3(8f, 0.05f, 7f), readyMaterial, false);
        CreateBox("BH_Wayfinding_WarehouseStrip", wayfindingRoot, new Vector3(10.1f, 0.03f, 1.3f), Quaternion.identity, new Vector3(7.2f, 0.05f, 10.8f), warehouseMaterial, false);
        CreateBox("BH_Wayfinding_MerchantStrip", wayfindingRoot, new Vector3(-10.1f, 0.03f, 1.3f), Quaternion.identity, new Vector3(7.2f, 0.05f, 10.8f), merchantMaterial, false);
        CreateBox("BH_Wayfinding_TaskStrip", wayfindingRoot, new Vector3(0f, 0.03f, -8.4f), Quaternion.identity, new Vector3(10f, 0.05f, 7.2f), taskMaterial, false);
        CreateBox("BH_Wayfinding_RecoveryStrip", wayfindingRoot, new Vector3(10.1f, 0.03f, -9f), Quaternion.identity, new Vector3(6.6f, 0.05f, 6.4f), medicalMaterial, false);
        CreateBox("BH_Wayfinding_DoorHeader_North", wayfindingRoot, new Vector3(0f, BaseHubBlockoutStandards.DoorHeaderHeight, 4.8f), new Vector3(4.4f, 0.25f, 0.6f), readyMaterial);
        CreateBox("BH_Wayfinding_DoorHeader_South", wayfindingRoot, new Vector3(0f, BaseHubBlockoutStandards.DoorHeaderHeight, -4.8f), new Vector3(4.4f, 0.25f, 0.6f), taskMaterial);
        CreateBox("BH_Wayfinding_DoorHeader_West", wayfindingRoot, new Vector3(-4.8f, BaseHubBlockoutStandards.DoorHeaderHeight, -1.1f), new Vector3(0.6f, 0.25f, 5.6f), merchantMaterial);
        CreateBox("BH_Wayfinding_DoorHeader_East", wayfindingRoot, new Vector3(4.8f, BaseHubBlockoutStandards.DoorHeaderHeight, -1.1f), new Vector3(0.6f, 0.25f, 5.6f), warehouseMaterial);

        CreateInformationBoard("SIGN_DirectoryBoard", wayfindingRoot, new Vector3(0f, 1.7f, 2.2f), Quaternion.Euler(0f, 180f, 0f), new Vector3(3.8f, 2.4f, 0.25f), signMaterial, "基地导览", "↑ 准备区\n← 商人区\n→ 仓库区\n↓ 任务区", Color.white, new Color(0.86f, 0.91f, 0.96f, 1f));
        CreateInformationBoard("SIGN_ReadyRoom", wayfindingRoot, new Vector3(0f, BaseHubBlockoutStandards.MainSignHeight, 12.2f), Quaternion.Euler(0f, 180f, 0f), new Vector3(4f, 1.5f, 0.25f), signMaterial, "准备区", "配装 / 出击 / 状态确认", readyMaterial.color, new Color(0.9f, 0.95f, 0.99f, 1f));
        CreateInformationBoard("SIGN_Warehouse", wayfindingRoot, new Vector3(13.8f, BaseHubBlockoutStandards.MainSignHeight, 2f), Quaternion.Euler(0f, -90f, 0f), new Vector3(4.2f, 1.5f, 0.25f), signMaterial, "仓库区", "仓储 / 武器柜 / 安全箱", warehouseMaterial.color, new Color(0.9f, 0.98f, 0.92f, 1f));
        CreateInformationBoard("SIGN_Merchant", wayfindingRoot, new Vector3(-13.8f, BaseHubBlockoutStandards.MainSignHeight, 2f), Quaternion.Euler(0f, 90f, 0f), new Vector3(4.2f, 1.5f, 0.25f), signMaterial, "商人区", "武器 / 护甲 / 医疗 / 杂货", merchantMaterial.color, new Color(0.99f, 0.94f, 0.88f, 1f));
        CreateInformationBoard("SIGN_Task", wayfindingRoot, new Vector3(0f, BaseHubBlockoutStandards.MainSignHeight, -12.2f), Quaternion.identity, new Vector3(4.6f, 1.5f, 0.25f), signMaterial, "任务区", "公告板 / 汇报 / 情报整理", taskMaterial.color, new Color(0.96f, 0.91f, 0.99f, 1f));

        CreateReadyRoomLayout(readyRoot, wallMaterial, accentMaterial, readyMaterial, signMaterial);
        CreateWarehouseLayout(warehouseRoot, wallMaterial, accentMaterial, warehouseMaterial, signMaterial);
        CreateTaskLayout(missionRoot, gameplayRoot, wallMaterial, accentMaterial, taskMaterial, signMaterial);
        CreateRecoveryLayout(recoveryRoot, accentMaterial, medicalMaterial, signMaterial);

        Transform departureArrivalPoint = CreateMarker(gameplayRoot, "SPAWN_Departure", new Vector3(0f, 0f, 7.6f), Quaternion.Euler(0f, 180f, 0f));
        Transform respawnArrivalPoint = CreateMarker(gameplayRoot, "SPAWN_Respawn", new Vector3(10.4f, 0f, -8.4f), Quaternion.Euler(0f, 140f, 0f));
        CreateZoneMarker(gameplayRoot, "ZONE_Atrium", BaseHubZoneType.Arrival, "中枢大厅", "基地动线中枢，可快速前往四个功能分区。", new Vector3(0f, 0f, 0f), 4.8f);
        CreateZoneMarker(gameplayRoot, "ZONE_ReadyRoom", BaseHubZoneType.ReadyRoom, "准备区", "这里用于整备装备、确认地图并从出击终端进入战斗。", new Vector3(0f, 0f, 8.8f), 4.4f);
        CreateZoneMarker(gameplayRoot, "ZONE_Warehouse", BaseHubZoneType.Warehouse, "仓库区", "安全仓库、武器柜和受保护栏位都集中在这里。", new Vector3(10.3f, 0f, 1.2f), 5.4f);
        CreateZoneMarker(gameplayRoot, "ZONE_Merchant", BaseHubZoneType.Merchants, "商人区", "固定商人柜台已经预留，后续可直接接入 NPC 交互。", new Vector3(-10.3f, 0f, 1.2f), 5.4f);
        CreateZoneMarker(gameplayRoot, "ZONE_Task", BaseHubZoneType.Missions, "任务区", "任务公告板、简报桌和后续任务 NPC 点位位于该区域。", new Vector3(0f, 0f, -8.4f), 5.2f);
        CreateZoneMarker(gameplayRoot, "ZONE_Recovery", BaseHubZoneType.Recovery, "恢复区", "用于撤离失败后的回归落点和医疗恢复。", new Vector3(10.2f, 0f, -9f), 3.8f);

        GameObject player = CreatePlayer(new Vector3(0f, 1.05f, 7.6f));
        PrototypeFpsController fpsController = player.GetComponent<PrototypeFpsController>();
        PrototypeFpsInput fpsInput = player.GetComponent<PrototypeFpsInput>();
        PlayerInteractionState interactionState = player.GetComponent<PlayerInteractionState>();

        GameObject metaUiObject = new GameObject("MetaUi");
        PrototypeMainMenuController menuController = metaUiObject.AddComponent<PrototypeMainMenuController>();
        ConfigureMainMenuController(menuController, itemCatalog);

        GameObject systems = new GameObject("BaseHubSystems");
        BaseHubDirector director = systems.AddComponent<BaseHubDirector>();
        MerchantUIManager merchantUiManager = systems.AddComponent<MerchantUIManager>();
        BaseFacilityManager facilityManager = systems.AddComponent<BaseFacilityManager>();
        SetSerializedReference(director, "fpsController", fpsController);
        SetSerializedReference(director, "fpsInput", fpsInput);
        SetSerializedReference(director, "interactionState", interactionState);
        SetSerializedReference(director, "menuController", menuController);
        SetSerializedReference(director, "facilityManager", facilityManager);
        SetSerializedReference(director, "departureArrivalPoint", departureArrivalPoint);
        SetSerializedReference(director, "respawnArrivalPoint", respawnArrivalPoint);
        SetSerializedString(director, "hubTitle", "幸存者基地");
        SetSerializedString(director, "hubHint", "E：交互  |  Esc：关闭界面");
        SetSerializedString(director, "navigationLegend", "← 商人区  ·  ↑ 准备区  ·  → 仓库区  ·  ↓ 任务区");
        SetSerializedReference(merchantUiManager, "director", director);
        SetSerializedReference(merchantUiManager, "menuController", menuController);
        SetSerializedReference(facilityManager, "menuController", menuController);
        SetSerializedReference(facilityManager, "director", director);
        SetSerializedReference(facilityManager, "itemCatalog", itemCatalog);

        CreateMerchantLayout(merchantRoot, gameplayRoot, wallMaterial, accentMaterial, merchantMaterial, signMaterial, merchantUiManager);

        CreateTerminal(
            "INT_DepartureBoard",
            gameplayRoot,
            new Vector3(0f, 1.1f, 10.1f),
            Quaternion.Euler(0f, 180f, 0f),
            new Vector3(1.5f, 2.2f, 0.9f),
            accentMaterial,
            readyMaterial,
            director,
            BaseHubInteractionKind.Deploy,
            "打开出击终端");
        CreateTerminal(
            "INT_WarehouseTerminal",
            gameplayRoot,
            new Vector3(9.2f, 1.1f, 6.2f),
            Quaternion.Euler(0f, -90f, 0f),
            new Vector3(1.3f, 2.2f, 0.9f),
            accentMaterial,
            warehouseMaterial,
            director,
            BaseHubInteractionKind.Warehouse,
            "打开仓库");
        CreateTerminal(
            "INT_MerchantDirectoryTerminal",
            gameplayRoot,
            new Vector3(-8.2f, 1.1f, 0.2f),
            Quaternion.Euler(0f, 90f, 0f),
            new Vector3(1.3f, 2.2f, 0.9f),
            accentMaterial,
            merchantMaterial,
            director,
            BaseHubInteractionKind.Merchants,
            "查看商人目录");

        CreatePointLight(lightingRoot, "LGT_Atrium_Main", new Vector3(0f, 3.3f, 0f), new Color(0.96f, 0.84f, 0.72f), 4.2f, 14f);
        CreatePointLight(lightingRoot, "LGT_ReadyRoom_Main", new Vector3(0f, 3.3f, 9.2f), new Color(0.58f, 0.84f, 1f), 4.3f, 13f);
        CreatePointLight(lightingRoot, "LGT_Warehouse_Main", new Vector3(10.2f, 3.2f, 1.4f), new Color(0.56f, 0.9f, 0.68f), 4.1f, 12f);
        CreatePointLight(lightingRoot, "LGT_Merchant_Main", new Vector3(-10.2f, 3.2f, 1.4f), new Color(1f, 0.72f, 0.42f), 4.2f, 12f);
        CreatePointLight(lightingRoot, "LGT_Mission_Main", new Vector3(0f, 3.2f, -8.4f), new Color(0.86f, 0.68f, 1f), 4f, 12f);
        CreatePointLight(lightingRoot, "LGT_Recovery_Main", new Vector3(10.4f, 3f, -8.8f), new Color(0.8f, 0.94f, 1f), 3.8f, 10f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, BaseScenePath);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static GameObject CreatePlayer(Vector3 position)
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null)
        {
            Debug.LogError($"[BaseHubSceneBuilder] Missing player prefab at '{PlayerPrefabPath}'.");
            return CreateLegacyPlayer(position);
        }

        GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
        if (player == null)
        {
            Debug.LogError($"[BaseHubSceneBuilder] Failed to instantiate player prefab '{PlayerPrefabPath}'.");
            return CreateLegacyPlayer(position);
        }

        player.name = "BaseHubPlayer";
        player.transform.SetPositionAndRotation(position, Quaternion.identity);
        player.layer = IgnoreRaycastLayer;

        PrototypeUnitVitals playerVitals = player.GetComponent<PrototypeUnitVitals>();
        if (playerVitals != null)
        {
            SetSerializedBool(playerVitals, "bootstrapStatusEffects", false);
        }

        InventoryContainer inventory = player.GetComponent<InventoryContainer>();
        inventory?.Configure("鍩哄湴闅忚韩", 4, 12f);

        PrototypeFpsController fpsController = player.GetComponent<PrototypeFpsController>();
        if (fpsController != null)
        {
            SetSerializedBool(fpsController, "bootstrapCombatRuntime", false);
            SetSerializedBool(fpsController, "showHud", false);
            SetSerializedReference(fpsController, "primaryWeapon", null);
            SetSerializedReference(fpsController, "secondaryWeapon", null);
            SetSerializedReference(fpsController, "meleeWeapon", null);
        }

        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
        if (interactor != null)
        {
            SetSerializedBool(interactor, "autoAddInventoryWindowController", false);
        }

        PlayerHudPresenter hudPresenter = player.GetComponent<PlayerHudPresenter>();
        if (hudPresenter != null)
        {
            SetSerializedBool(hudPresenter, "showHud", false);
        }

        PlayerWeaponController weaponController = player.GetComponent<PlayerWeaponController>();
        if (weaponController != null)
        {
            SetSerializedReference(weaponController, "primaryWeapon", null);
            SetSerializedReference(weaponController, "secondaryWeapon", null);
            SetSerializedReference(weaponController, "meleeWeapon", null);
        }

        return player;
    }

    private static GameObject CreateLegacyPlayer(Vector3 position)
    {
        GameObject player = new GameObject("BaseHubPlayer");
        player.transform.position = position;
        player.layer = IgnoreRaycastLayer;

        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.35f;
        characterController.center = new Vector3(0f, 0.9f, 0f);
        characterController.stepOffset = 0.3f;
        characterController.slopeLimit = 45f;

        GameObject cameraRoot = new GameObject("ViewCamera");
        cameraRoot.transform.SetParent(player.transform, false);
        cameraRoot.transform.localPosition = new Vector3(0f, 0.72f, 0f);

        Camera camera = cameraRoot.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 75f;
        camera.nearClipPlane = 0.03f;
        cameraRoot.tag = "MainCamera";
        cameraRoot.AddComponent<AudioListener>();

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(cameraRoot.transform, false);
        muzzle.transform.localPosition = new Vector3(0.18f, -0.12f, 0.55f);

        PrototypeFpsMovementModule movementModule = player.AddComponent<PrototypeFpsMovementModule>();
        SetSerializedReference(movementModule, "viewCamera", camera);

        PrototypeFpsController fpsController = player.AddComponent<PrototypeFpsController>();
        SetSerializedReference(fpsController, "viewCamera", camera);
        SetSerializedReference(fpsController, "muzzle", muzzle.transform);
        SetSerializedBool(fpsController, "showHud", false);

        InventoryContainer inventory = player.AddComponent<InventoryContainer>();
        inventory.Configure("基地随身", 4, 12f);

        player.AddComponent<PlayerInteractionState>();
        PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
        interactor.Configure(camera, inventory);
        SetSerializedBool(interactor, "autoAddInventoryWindowController", false);

        return player;
    }

    private static void ConfigureMainMenuController(PrototypeMainMenuController controller, PrototypeItemCatalog itemCatalog)
    {
        SerializedObject serializedObject = new SerializedObject(controller);
        serializedObject.FindProperty("itemCatalog").objectReferenceValue = itemCatalog;
        serializedObject.FindProperty("uiVisible").boolValue = false;
        serializedObject.FindProperty("allowBaseHubEntryButton").boolValue = false;
        serializedObject.FindProperty("shellMode").intValue = (int)PrototypeMainMenuController.MetaShellMode.FullBaseHub;
        serializedObject.FindProperty("raidSceneName").stringValue = "SampleScene";
        serializedObject.FindProperty("selectedRaidSceneIndex").intValue = 0;

        SerializedProperty raidSceneOptions = serializedObject.FindProperty("raidSceneOptions");
        raidSceneOptions.arraySize = 1;
        SerializedProperty option = raidSceneOptions.GetArrayElementAtIndex(0);
        option.FindPropertyRelative("displayName").stringValue = "原型战区";
        option.FindPropertyRelative("sceneName").stringValue = "SampleScene";
        option.FindPropertyRelative("description").stringValue = "原型室内战斗区域。";
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureMetaEntryRouteConfig()
    {
        MetaEntryRouteConfig config = AssetDatabase.LoadAssetAtPath<MetaEntryRouteConfig>(RouteConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<MetaEntryRouteConfig>();
            AssetDatabase.CreateAsset(config, RouteConfigPath);
        }

        config.defaultMetaTarget = MetaEntryTarget.BaseScene;
        config.returnFromRaidTarget = MetaEntryTarget.BaseScene;
        config.debugEntryTarget = MetaEntryTarget.MainMenu;
        config.debugEntryEnabled = true;
        config.mainMenuSceneName = "MainMenu";
        config.baseSceneName = "BaseScene";
        EditorUtility.SetDirty(config);
    }

    private static Transform CreateGroup(Transform parent, string objectName)
    {
        GameObject group = new GameObject(objectName);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static void CreateReadyRoomLayout(Transform parent, Material wallMaterial, Material accentMaterial, Material readyMaterial, Material signMaterial)
    {
        CreateBox("Ready_LockerLeft", parent, new Vector3(-6.2f, 1.35f, 10.8f), new Vector3(0.9f, 2.7f, 1.3f), wallMaterial);
        CreateBox("Ready_LockerRight", parent, new Vector3(6.2f, 1.35f, 10.8f), new Vector3(0.9f, 2.7f, 1.3f), wallMaterial);
        CreateBox("Ready_BenchLeft", parent, new Vector3(-2.8f, 0.7f, 8.6f), new Vector3(3f, 0.8f, 1.2f), accentMaterial);
        CreateBox("Ready_BenchRight", parent, new Vector3(3f, 0.7f, 8.6f), new Vector3(2.6f, 0.8f, 1.4f), wallMaterial);
        CreateBox("Ready_MapTable", parent, new Vector3(2.6f, 1.1f, 8.6f), new Vector3(2.1f, 0.2f, 1.2f), readyMaterial);
        CreateBox("Ready_GearCrateA", parent, new Vector3(-5.2f, 0.55f, 7f), new Vector3(1.1f, 1.1f, 1.1f), accentMaterial);
        CreateBox("Ready_GearCrateB", parent, new Vector3(5.2f, 0.55f, 7f), new Vector3(1.1f, 1.1f, 1.1f), accentMaterial);
        CreateInformationBoard(
            "Ready_StatusBoard",
            parent,
            new Vector3(4.2f, 2.1f, 12.2f),
            Quaternion.Euler(0f, 180f, 0f),
            new Vector3(2.6f, 1.2f, 0.2f),
            signMaterial,
            "出击状态",
            "检查配装\n确认资金\n选择地图",
            readyMaterial.color,
            Color.white);
    }

    private static void CreateWarehouseLayout(Transform parent, Material wallMaterial, Material accentMaterial, Material warehouseMaterial, Material signMaterial)
    {
        CreateBox("Warehouse_ShelfNorth", parent, new Vector3(13f, 1.5f, 5.6f), new Vector3(2.2f, 3f, 0.7f), wallMaterial);
        CreateBox("Warehouse_ShelfEast", parent, new Vector3(13.8f, 1.5f, 1.1f), new Vector3(0.7f, 3f, 6.8f), wallMaterial);
        CreateBox("Warehouse_CrateA", parent, new Vector3(11.4f, 0.6f, 2.2f), new Vector3(1.3f, 1.2f, 1.3f), accentMaterial);
        CreateBox("Warehouse_CrateB", parent, new Vector3(12.8f, 0.6f, 0.8f), new Vector3(1.1f, 1.2f, 1.1f), accentMaterial);
        CreateBox("Warehouse_CrateC", parent, new Vector3(10.8f, 0.6f, -2.2f), new Vector3(1.2f, 1.2f, 1.2f), accentMaterial);
        CreateBox("Warehouse_LockerBench", parent, new Vector3(8.4f, 0.7f, -1.8f), new Vector3(1.6f, 0.8f, 3.2f), warehouseMaterial);
        CreateInformationBoard(
            "Warehouse_InfoBoard",
            parent,
            new Vector3(14.2f, 1.8f, -4.8f),
            Quaternion.Euler(0f, -90f, 0f),
            new Vector3(2.6f, 1.2f, 0.2f),
            signMaterial,
            "仓储说明",
            "安全仓库\n武器柜\n保护栏位",
            warehouseMaterial.color,
            Color.white);
    }

    private static void CreateMerchantLayout(
        Transform parent,
        Transform gameplayParent,
        Material wallMaterial,
        Material accentMaterial,
        Material merchantMaterial,
        Material signMaterial,
        MerchantUIManager merchantUiManager)
    {
        CreateMerchantStand(parent, gameplayParent, "WeaponMerchantStand", BaseHubMerchantSpotType.Weapon, "weapons_trader", "武器商人", "武器 / 弹药 / 配件", "有新到的武器和弹药，自己挑。", new Vector3(-12f, 0.7f, 6.2f), wallMaterial, accentMaterial, merchantMaterial, signMaterial, merchantUiManager);
        CreateMerchantStand(parent, gameplayParent, "ArmorMerchantStand", BaseHubMerchantSpotType.Armor, "armor_trader", "护甲商人", "头盔 / 防具 / 战术背心", "想活着回来，先把护甲穿好。", new Vector3(-12f, 0.7f, 2.2f), wallMaterial, accentMaterial, merchantMaterial, signMaterial, merchantUiManager);
        CreateMerchantStand(parent, gameplayParent, "MedicalMerchantStand", BaseHubMerchantSpotType.Medical, "medical_trader", "医疗商人", "医疗包 / 药品 / 注射剂", "伤口别拖，补给都在这边。", new Vector3(-12f, 0.7f, -2.2f), wallMaterial, accentMaterial, merchantMaterial, signMaterial, merchantUiManager);
        CreateMerchantStand(parent, gameplayParent, "GeneralMerchantStand", BaseHubMerchantSpotType.General, "general_trader", "杂货商人", "补给 / 安全箱 / 任务杂项", "常用杂货和补给我这都有。", new Vector3(-12f, 0.7f, -7.8f), wallMaterial, accentMaterial, merchantMaterial, signMaterial, merchantUiManager);
    }

    private static void CreateTaskLayout(Transform parent, Transform gameplayParent, Material wallMaterial, Material accentMaterial, Material taskMaterial, Material signMaterial)
    {
        CreateInformationBoard(
            "TaskBoard",
            parent,
            new Vector3(0f, 2f, -12.1f),
            Quaternion.identity,
            new Vector3(3.4f, 2f, 0.22f),
            signMaterial,
            "任务公告板",
            "主线 / 情报 / 训练\n后续任务 NPC 点位已预留",
            taskMaterial.color,
            Color.white);

        CreateBox("Task_BriefingTable", parent, new Vector3(0f, 0.85f, -8.2f), new Vector3(4.8f, 0.9f, 2.2f), accentMaterial);
        CreateBox("Task_SideDeskLeft", parent, new Vector3(-5.6f, 0.7f, -8.8f), new Vector3(2.2f, 0.8f, 1.2f), wallMaterial);
        CreateBox("Task_SideDeskRight", parent, new Vector3(5.6f, 0.7f, -8.8f), new Vector3(2.2f, 0.8f, 1.2f), wallMaterial);
        CreateBox("Task_IntelRack", parent, new Vector3(-8.8f, 1.4f, -10.8f), new Vector3(1.2f, 2.8f, 0.6f), wallMaterial);
        CreateBox("Task_TrainingRack", parent, new Vector3(8.8f, 1.4f, -10.8f), new Vector3(1.2f, 2.8f, 0.6f), wallMaterial);

        CreateMarker(gameplayParent, "NPC_Commander_Anchor", new Vector3(-5.6f, 0f, -7.2f), Quaternion.identity);
        CreateMarker(gameplayParent, "NPC_IntelOfficer_Anchor", new Vector3(0f, 0f, -10.1f), Quaternion.identity);
        CreateMarker(gameplayParent, "NPC_Trainer_Anchor", new Vector3(5.6f, 0f, -7.2f), Quaternion.identity);

        CreateWorldText(parent, "Task_CommanderLabel", "主线汇报点", new Vector3(-5.6f, 1.8f, -6.4f), Quaternion.identity, 52, taskMaterial.color, TextAnchor.MiddleCenter, TextAlignment.Center, 0.07f);
        CreateWorldText(parent, "Task_IntelLabel", "情报整理点", new Vector3(0f, 1.8f, -10.8f), Quaternion.identity, 52, taskMaterial.color, TextAnchor.MiddleCenter, TextAlignment.Center, 0.07f);
        CreateWorldText(parent, "Task_TrainerLabel", "训练任务点", new Vector3(5.6f, 1.8f, -6.4f), Quaternion.identity, 52, taskMaterial.color, TextAnchor.MiddleCenter, TextAlignment.Center, 0.07f);
    }

    private static void CreateRecoveryLayout(Transform parent, Material accentMaterial, Material medicalMaterial, Material signMaterial)
    {
        CreateBox("Recovery_BedFrame", parent, new Vector3(10.4f, 0.45f, -9.2f), new Vector3(2.6f, 0.4f, 1.2f), accentMaterial);
        CreateBox("Recovery_Mattress", parent, new Vector3(10.4f, 0.72f, -9.2f), new Vector3(2.35f, 0.18f, 1f), medicalMaterial);
        CreateBox("Recovery_Panel", parent, new Vector3(10.4f, 1.55f, -9.9f), new Vector3(1.3f, 1.2f, 0.18f), medicalMaterial);
        CreateInformationBoard(
            "RecoverySign",
            parent,
            new Vector3(13.2f, 1.9f, -9.6f),
            Quaternion.Euler(0f, -90f, 0f),
            new Vector3(2.8f, 1.2f, 0.2f),
            signMaterial,
            "恢复区",
            "死亡返回点\n临时休整",
            medicalMaterial.color,
            new Color(0.12f, 0.18f, 0.24f, 1f));
    }

    private static void CreateMerchantStand(
        Transform parent,
        Transform gameplayParent,
        string objectName,
        BaseHubMerchantSpotType spotType,
        string merchantId,
        string merchantName,
        string previewDescription,
        string greetingLine,
        Vector3 counterPosition,
        Material wallMaterial,
        Material accentMaterial,
        Material merchantMaterial,
        Material signMaterial,
        MerchantUIManager merchantUiManager)
    {
        CreateBox($"{objectName}_Counter", parent, counterPosition, new Vector3(1.5f, 0.95f, 2.4f), accentMaterial);
        CreateBox($"{objectName}_BackPanel", parent, counterPosition + new Vector3(-1.35f, 1.45f, 0f), new Vector3(0.3f, 2.9f, 2.8f), wallMaterial);
        CreateBox($"{objectName}_DisplayBar", parent, counterPosition + new Vector3(-0.7f, 1.9f, 0f), new Vector3(0.5f, 0.35f, 2f), merchantMaterial);
        CreateInformationBoard($"{objectName}_Sign", parent, counterPosition + new Vector3(-0.72f, 2.35f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2.2f, 1f, 0.16f), signMaterial, merchantName, previewDescription, merchantMaterial.color, Color.white, 50, 34, 0.055f, 0.04f);

        Transform anchor = CreateMarker(gameplayParent, $"NPC_{merchantName.Replace(" ", string.Empty)}_Anchor", counterPosition + new Vector3(-1.1f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
        BaseHubMerchantSpot merchantSpot = anchor.gameObject.AddComponent<BaseHubMerchantSpot>();
        SetSerializedEnum(merchantSpot, "spotType", (int)spotType);
        SetSerializedString(merchantSpot, "merchantId", merchantId);
        SetSerializedString(merchantSpot, "merchantName", merchantName);
        SetSerializedString(merchantSpot, "previewDescription", previewDescription);
        SetSerializedReference(merchantSpot, "standAnchor", anchor);

        MerchantNPC merchantNpc = anchor.gameObject.AddComponent<MerchantNPC>();
        SetSerializedReference(merchantNpc, "merchantSpot", merchantSpot);
        SetSerializedReference(merchantNpc, "uiManager", merchantUiManager);
        SetSerializedString(merchantNpc, "greetingLine", greetingLine);
        SetSerializedFloat(merchantNpc, "interactionRange", 3.4f);

        CreateMerchantNpcVisual(anchor, objectName, merchantName, wallMaterial, accentMaterial, merchantMaterial);
    }

    private static void CreateMerchantNpcVisual(
        Transform anchor,
        string objectName,
        string merchantName,
        Material bodyMaterial,
        Material accentMaterial,
        Material merchantMaterial)
    {
        CreateBox($"{objectName}_NpcBody", anchor, new Vector3(0f, 1.05f, 0f), new Vector3(0.75f, 1.35f, 0.55f), bodyMaterial);
        CreateBox($"{objectName}_NpcHead", anchor, new Vector3(0f, 2.05f, 0f), new Vector3(0.55f, 0.55f, 0.55f), accentMaterial);
        CreateBox($"{objectName}_NpcPack", anchor, new Vector3(-0.28f, 1.12f, -0.18f), new Vector3(0.24f, 0.78f, 0.32f), merchantMaterial);
        CreateBox($"{objectName}_NpcDeskLamp", anchor, new Vector3(0.62f, 1.35f, 0.42f), new Vector3(0.18f, 0.52f, 0.18f), merchantMaterial);
        CreateWorldText(anchor, $"{objectName}_NpcLabel", merchantName, new Vector3(0f, 2.75f, 0f), Quaternion.identity, 48, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, 0.05f);
    }

    private static Transform CreateZoneMarker(
        Transform parent,
        string objectName,
        BaseHubZoneType zoneType,
        string zoneName,
        string zoneSummary,
        Vector3 localPosition,
        float guidanceRadius)
    {
        GameObject marker = new GameObject(objectName);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;

        BaseHubZoneMarker zoneMarker = marker.AddComponent<BaseHubZoneMarker>();
        SetSerializedEnum(zoneMarker, "zoneType", (int)zoneType);
        SetSerializedString(zoneMarker, "zoneName", zoneName);
        SetSerializedString(zoneMarker, "zoneSummary", zoneSummary);
        SetSerializedFloat(zoneMarker, "guidanceRadius", guidanceRadius);
        return marker.transform;
    }

    private static void ConfigureDirectionalLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.92f;
        light.color = new Color(0.96f, 0.97f, 1f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(46f, -32f, 0f);
    }

    private static GameObject CreateTerminal(
        string objectName,
        Transform parent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Material bodyMaterial,
        Material screenMaterial,
        BaseHubDirector director,
        BaseHubInteractionKind interactionKind,
        string interactionLabel)
    {
        GameObject terminal = CreateBox(objectName, parent, localPosition, localRotation, localScale, bodyMaterial);

        BaseHubTerminalInteractable interactable = terminal.AddComponent<BaseHubTerminalInteractable>();
        SetSerializedReference(interactable, "director", director);
        SetSerializedEnum(interactable, "interactionKind", (int)interactionKind);
        SetSerializedString(interactable, "interactionLabelOverride", interactionLabel);

        GameObject screen = CreateBox("Screen", terminal.transform, new Vector3(0f, 0.3f, 0.45f), new Vector3(0.78f, 0.6f, 0.08f), screenMaterial, false, false);
        screen.name = "Screen";
        GameObject marker = CreateBox("Marker", terminal.transform, new Vector3(0f, -0.78f, 0.45f), new Vector3(0.8f, 0.08f, 0.08f), screenMaterial, false, false);
        marker.name = "Marker";
        return terminal;
    }

    private static GameObject CreateInformationBoard(
        string objectName,
        Transform parent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 boardScale,
        Material boardMaterial,
        string title,
        string body,
        Color titleColor,
        Color bodyColor,
        int titleFontSize = 64,
        int bodyFontSize = 42,
        float titleCharacterSize = 0.075f,
        float bodyCharacterSize = 0.055f)
    {
        GameObject board = CreateBox(objectName, parent, localPosition, localRotation, boardScale, boardMaterial, false);
        float frontOffset = boardScale.z * 0.5f + 0.01f;

        CreateWorldText(board.transform, "Title", title, new Vector3(0f, boardScale.y * 0.16f, frontOffset), Quaternion.identity, titleFontSize, titleColor, TextAnchor.MiddleCenter, TextAlignment.Center, titleCharacterSize);
        if (!string.IsNullOrWhiteSpace(body))
        {
            CreateWorldText(board.transform, "Body", body, new Vector3(0f, -boardScale.y * 0.12f, frontOffset), Quaternion.identity, bodyFontSize, bodyColor, TextAnchor.MiddleCenter, TextAlignment.Center, bodyCharacterSize);
        }

        return board;
    }

    private static TextMesh CreateWorldText(
        Transform parent,
        string objectName,
        string text,
        Vector3 localPosition,
        Quaternion localRotation,
        int fontSize,
        Color color,
        TextAnchor anchor,
        TextAlignment alignment,
        float characterSize)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;
        // TextMesh front faces need a 180-degree flip so board text reads correctly from the player side.
        textObject.transform.localRotation = localRotation * Quaternion.Euler(0f, 180f, 0f);

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        Font font = ResolveBuiltInFont();
        textMesh.font = font;
        if (font != null)
        {
            MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = ResolveWorldTextMaterial(font) ?? font.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        textMesh.text = text ?? string.Empty;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.anchor = anchor;
        textMesh.alignment = alignment;
        textMesh.color = color;
        return textMesh;
    }

    private static Font ResolveBuiltInFont()
    {
        Font fusionPixelFont = AssetDatabase.LoadAssetAtPath<Font>(FusionPixelFontPath);
        if (fusionPixelFont != null)
        {
            return fusionPixelFont;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static Material ResolveWorldTextMaterial(Font font)
    {
        if (font == null)
        {
            return null;
        }

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(WorldTextShaderPath) ?? Shader.Find(WorldTextShaderName);
        Material fontMaterial = font.material;
        if (shader == null)
        {
            return fontMaterial;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(WorldTextMaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, WorldTextMaterialPath);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        Texture fontAtlas = fontMaterial != null ? fontMaterial.mainTexture : null;
        if (fontAtlas != null && material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", fontAtlas);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Transform CreateMarker(Transform parent, string objectName, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject marker = new GameObject(objectName);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        marker.transform.localRotation = localRotation;
        return marker.transform;
    }

    private static void CreatePointLight(Transform parent, string objectName, Vector3 localPosition, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(string.IsNullOrWhiteSpace(objectName) ? "LGT_Point" : objectName.Trim());
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;

        Light pointLight = lightObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = color;
        pointLight.intensity = intensity;
        pointLight.range = range;
    }

    private static GameObject CreateBox(string objectName, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        return CreateBox(objectName, parent, localPosition, Quaternion.identity, localScale, material, true);
    }

    private static GameObject CreateBox(
        string objectName,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        bool withCollider,
        bool markStatic = true)
    {
        return CreateBox(objectName, parent, localPosition, Quaternion.identity, localScale, material, withCollider, markStatic);
    }

    private static GameObject CreateBox(
        string objectName,
        Transform parent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Material material,
        bool withCollider = true,
        bool markStatic = true)
    {
        // Use ProBuilder cubes so the base hub blockout stays editable in-editor.
        ProBuilderMesh mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, localScale);
        GameObject box = mesh.gameObject;
        box.name = objectName;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localRotation = localRotation;
        box.transform.localScale = Vector3.one;

        MeshRenderer renderer = box.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        BoxCollider collider = box.GetComponent<BoxCollider>();
        if (withCollider)
        {
            if (collider == null)
            {
                collider = box.AddComponent<BoxCollider>();
            }

            collider.center = Vector3.zero;
            collider.size = localScale;
        }
        else
        {
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        if (markStatic)
        {
            MarkStatic(box);
        }

        return box;
    }

    private static void MarkStatic(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return;
        }

        GameObjectUtility.SetStaticEditorFlags(
            gameObject,
            StaticEditorFlags.BatchingStatic
            | StaticEditorFlags.OccluderStatic
            | StaticEditorFlags.OccludeeStatic);
    }

    private static Material CreateOrUpdateMaterial(string assetPath, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        AddSceneIfExists(scenes, MainMenuScenePath);
        AddSceneIfExists(scenes, BaseScenePath);
        AddSceneIfExists(scenes, SampleScenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddSceneIfExists(List<EditorBuildSettingsScene> scenes, string scenePath)
    {
        if (File.Exists(scenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(assetPath);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void SetSerializedReference(Object target, string propertyName, Object reference)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.objectReferenceValue = reference;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerializedBool(Object target, string propertyName, bool value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerializedEnum(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.intValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerializedFloat(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerializedString(Object target, string propertyName, string value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.stringValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}

/// <summary>
/// BaseScene / 基地 Hub 原型搭建统一规范。
/// 这份常量不追求“玩法逻辑”，只用于约束 blockout 的尺度、层级和替换边界。
/// </summary>
internal static class BaseHubBlockoutStandards
{
    public const float MacroGrid = 1f;
    public const float ModuleGrid = 0.5f;
    public const float FineGrid = 0.25f;

    public const float CeilingHeight = 4.6f;
    public const float DoorHeaderHeight = 3.2f;
    public const float MainCorridorWidth = 4f;
    public const float SideCorridorWidth = 3f;
    public const float MainDoorWidth = 3.6f;
    public const float SideDoorWidth = 2.4f;
    public const float CounterHeight = 0.95f;
    public const float MainSignHeight = 2.2f;

    public const string RootName = "BH_BaseHubRoot";
    public const string ShellRootName = "00_Shell";
    public const string ReadyZoneRootName = "10_ReadyRoom";
    public const string WarehouseZoneRootName = "20_Warehouse";
    public const string MerchantZoneRootName = "30_MerchantWing";
    public const string MissionZoneRootName = "40_MissionWing";
    public const string RecoveryZoneRootName = "50_RecoveryBay";
    public const string GameplayRootName = "60_Gameplay";
    public const string WayfindingRootName = "70_Wayfinding";
    public const string LightingRootName = "80_Lighting";
    public const string DebugRootName = "90_Debug";

    public static readonly Color NeutralColor = new Color(0.21f, 0.24f, 0.28f, 1f);
    public static readonly Color ReadyColor = new Color(0.22f, 0.74f, 0.96f, 1f);
    public static readonly Color WarehouseColor = new Color(0.26f, 0.68f, 0.42f, 1f);
    public static readonly Color MerchantColor = new Color(0.92f, 0.56f, 0.18f, 1f);
    public static readonly Color MissionColor = new Color(0.72f, 0.4f, 0.88f, 1f);
    public static readonly Color RecoveryColor = new Color(0.84f, 0.91f, 0.96f, 1f);
}
