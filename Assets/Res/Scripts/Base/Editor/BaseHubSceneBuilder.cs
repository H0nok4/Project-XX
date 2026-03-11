using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class BaseHubSceneBuilder
{
    // Trigger-based rebuild keeps BaseScene reproducible without manual editor wiring.
    private const string TriggerAssetPath = "Assets/Res/BaseHubScene.trigger.txt";
    private const string BaseScenePath = "Assets/Scenes/BaseScene.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ResourcesFolder = "Assets/Resources";
    private const string MaterialFolder = "Assets/Res/Materials/BaseHub";
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

        Material floorMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_BaseFloor.mat", new Color(0.22f, 0.24f, 0.28f, 1f));
        Material wallMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_BaseWall.mat", new Color(0.57f, 0.6f, 0.66f, 1f));
        Material ceilingMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_BaseCeiling.mat", new Color(0.82f, 0.84f, 0.88f, 1f));
        Material accentMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_BaseAccent.mat", new Color(0.89f, 0.48f, 0.18f, 1f));
        Material deployMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Deploy.mat", new Color(0.22f, 0.74f, 0.96f, 1f));
        Material warehouseMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Warehouse.mat", new Color(0.26f, 0.68f, 0.42f, 1f));
        Material medicalMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Medical.mat", new Color(0.88f, 0.9f, 0.95f, 1f));

        ConfigureDirectionalLight();

        GameObject root = new GameObject("BaseHubRoot");
        CreateBox("Floor", root.transform, new Vector3(0f, 0f, 0f), new Vector3(18f, 0.4f, 18f), floorMaterial);
        CreateBox("Ceiling", root.transform, new Vector3(0f, 4.2f, 0f), new Vector3(18f, 0.3f, 18f), ceilingMaterial);
        CreateBox("Wall_North", root.transform, new Vector3(0f, 2.1f, 9f), new Vector3(18f, 4.2f, 0.4f), wallMaterial);
        CreateBox("Wall_South", root.transform, new Vector3(0f, 2.1f, -9f), new Vector3(18f, 4.2f, 0.4f), wallMaterial);
        CreateBox("Wall_East", root.transform, new Vector3(9f, 2.1f, 0f), new Vector3(0.4f, 4.2f, 18f), wallMaterial);
        CreateBox("Wall_West", root.transform, new Vector3(-9f, 2.1f, 0f), new Vector3(0.4f, 4.2f, 18f), wallMaterial);
        CreateBox("Divider_RespawnBay", root.transform, new Vector3(-3.8f, 1.4f, -4.2f), new Vector3(0.4f, 2.8f, 5.6f), wallMaterial);
        CreateBox("Divider_Warehouse", root.transform, new Vector3(3.9f, 1.4f, 2.4f), new Vector3(0.4f, 2.8f, 7.4f), wallMaterial);
        CreateBox("ArrivalStrip", root.transform, new Vector3(0f, 0.02f, 4.8f), new Vector3(3.2f, 0.05f, 2.6f), deployMaterial);
        CreateBox("MedicalStrip", root.transform, new Vector3(-5.4f, 0.02f, -5.2f), new Vector3(2.8f, 0.05f, 2.8f), medicalMaterial);
        CreateBox("WarehouseStrip", root.transform, new Vector3(6f, 0.02f, 4.2f), new Vector3(2.8f, 0.05f, 2.6f), warehouseMaterial);

        CreateBox("RespawnBed_Frame", root.transform, new Vector3(-5.5f, 0.45f, -5.5f), new Vector3(2.4f, 0.4f, 1.1f), accentMaterial);
        CreateBox("RespawnBed_Mattress", root.transform, new Vector3(-5.5f, 0.72f, -5.5f), new Vector3(2.2f, 0.18f, 0.95f), medicalMaterial);
        CreateBox("RespawnBed_Panel", root.transform, new Vector3(-5.5f, 1.55f, -6.1f), new Vector3(1.2f, 1.2f, 0.16f), medicalMaterial);

        CreateBox("Warehouse_CrateA", root.transform, new Vector3(6.2f, 0.55f, 1.8f), new Vector3(1.2f, 1.1f, 1.2f), accentMaterial);
        CreateBox("Warehouse_CrateB", root.transform, new Vector3(7.1f, 0.55f, 2.9f), new Vector3(1.1f, 1.1f, 1.1f), accentMaterial);
        CreateBox("Warehouse_Shelf", root.transform, new Vector3(7f, 1.4f, 6.8f), new Vector3(2.4f, 2.8f, 0.7f), wallMaterial);

        Transform departureArrivalPoint = CreateMarker(root.transform, "Spawn_Departure", new Vector3(0f, 0f, 4.2f), Quaternion.identity);
        Transform respawnArrivalPoint = CreateMarker(root.transform, "Spawn_Respawn", new Vector3(-5.4f, 0f, -3.8f), Quaternion.Euler(0f, 20f, 0f));

        GameObject player = CreatePlayer(new Vector3(0f, 1.05f, 4.2f));
        PrototypeFpsController fpsController = player.GetComponent<PrototypeFpsController>();
        PrototypeFpsInput fpsInput = player.GetComponent<PrototypeFpsInput>();
        PlayerInteractionState interactionState = player.GetComponent<PlayerInteractionState>();

        GameObject metaUiObject = new GameObject("MetaUi");
        PrototypeMainMenuController menuController = metaUiObject.AddComponent<PrototypeMainMenuController>();
        ConfigureMainMenuController(menuController, itemCatalog);

        GameObject systems = new GameObject("BaseHubSystems");
        BaseHubDirector director = systems.AddComponent<BaseHubDirector>();
        SetSerializedReference(director, "fpsController", fpsController);
        SetSerializedReference(director, "fpsInput", fpsInput);
        SetSerializedReference(director, "interactionState", interactionState);
        SetSerializedReference(director, "menuController", menuController);
        SetSerializedReference(director, "departureArrivalPoint", departureArrivalPoint);
        SetSerializedReference(director, "respawnArrivalPoint", respawnArrivalPoint);
        SetSerializedString(director, "hubTitle", "基地");
        SetSerializedString(director, "hubHint", "靠近出击终端按 E 可打开出击界面，靠近仓库终端按 E 可管理仓库。按 Esc 可以关闭当前界面。");

        CreateTerminal(
            "DepartureBoard",
            root.transform,
            new Vector3(0f, 1.1f, 7.2f),
            new Vector3(1.4f, 2.2f, 0.8f),
            accentMaterial,
            deployMaterial,
            director,
            BaseHubInteractionKind.Deploy,
            "打开出击终端");
        CreateTerminal(
            "WarehouseTerminal",
            root.transform,
            new Vector3(5.8f, 1.1f, 4.5f),
            new Vector3(1.2f, 2.2f, 0.8f),
            accentMaterial,
            warehouseMaterial,
            director,
            BaseHubInteractionKind.Warehouse,
            "打开仓库");

        CreatePointLight(root.transform, new Vector3(-5.5f, 2.9f, -5.1f), new Color(0.85f, 0.94f, 1f), 4.2f, 12f);
        CreatePointLight(root.transform, new Vector3(0f, 3.1f, 6.4f), new Color(0.58f, 0.84f, 1f), 4.4f, 13f);
        CreatePointLight(root.transform, new Vector3(6.2f, 3f, 4.1f), new Color(0.56f, 0.9f, 0.68f), 4.1f, 12f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, BaseScenePath);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static GameObject CreatePlayer(Vector3 position)
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
        Vector3 localScale,
        Material bodyMaterial,
        Material screenMaterial,
        BaseHubDirector director,
        BaseHubInteractionKind interactionKind,
        string interactionLabel)
    {
        GameObject terminal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        terminal.name = objectName;
        terminal.transform.SetParent(parent, false);
        terminal.transform.localPosition = localPosition;
        terminal.transform.localScale = localScale;
        terminal.GetComponent<MeshRenderer>().sharedMaterial = bodyMaterial;

        BaseHubTerminalInteractable interactable = terminal.AddComponent<BaseHubTerminalInteractable>();
        SetSerializedReference(interactable, "director", director);
        SetSerializedEnum(interactable, "interactionKind", (int)interactionKind);
        SetSerializedString(interactable, "interactionLabelOverride", interactionLabel);

        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screen.name = "Screen";
        screen.transform.SetParent(terminal.transform, false);
        screen.transform.localPosition = new Vector3(0f, 0.3f, 0.45f);
        screen.transform.localScale = new Vector3(0.78f, 0.6f, 0.08f);
        screen.GetComponent<MeshRenderer>().sharedMaterial = screenMaterial;
        Object.DestroyImmediate(screen.GetComponent<Collider>());

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "Marker";
        marker.transform.SetParent(terminal.transform, false);
        marker.transform.localPosition = new Vector3(0f, -0.78f, 0.45f);
        marker.transform.localScale = new Vector3(0.8f, 0.08f, 0.08f);
        marker.GetComponent<MeshRenderer>().sharedMaterial = screenMaterial;
        Object.DestroyImmediate(marker.GetComponent<Collider>());

        return terminal;
    }

    private static Transform CreateMarker(Transform parent, string objectName, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject marker = new GameObject(objectName);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        marker.transform.localRotation = localRotation;
        return marker.transform;
    }

    private static void CreatePointLight(Transform parent, Vector3 localPosition, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject("PointLight");
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
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = localScale;

        MeshRenderer renderer = box.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        return box;
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

    private static void SetSerializedString(Object target, string propertyName, string value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.stringValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
