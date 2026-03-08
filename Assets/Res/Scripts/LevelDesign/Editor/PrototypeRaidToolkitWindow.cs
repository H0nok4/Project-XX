using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PrototypeRaidToolkitWindow : EditorWindow
{
    private const string BlockoutRootName = "Level_Blockout";
    private const string PrefabRootName = "Level_Prefabs";
    private const string InteractableRootName = "Level_Interactables";
    private const string LootRootName = "Level_Loot";
    private const string DirectorRootName = "EncounterDirector";

    [SerializeField] private Material blockoutMaterial;
    [SerializeField] private Vector3 blockoutBoxSize = new Vector3(4f, 2f, 4f);
    [SerializeField] private Vector3 rampSize = new Vector3(4f, 1f, 6f);
    [SerializeField] private float rampAngle = 18f;

    [SerializeField] private GameObject prefabToPlace;
    [SerializeField] private Vector3 prefabScale = Vector3.one;
    [SerializeField] private bool randomizePrefabYaw = true;
    [SerializeField] private bool dropPrefabToGround = true;

    [SerializeField] private Material doorMaterial;
    [SerializeField] private float doorWidth = 1.15f;
    [SerializeField] private float doorHeight = 2.2f;
    [SerializeField] private float doorThickness = 0.14f;
    [SerializeField] private float doorOpenAngle = 96f;

    [SerializeField] private Material breakableMaterial;
    [SerializeField] private Vector3 breakableSize = new Vector3(1.2f, 1.2f, 0.08f);
    [SerializeField] private PrototypeBreakableMaterialType breakableType = PrototypeBreakableMaterialType.Glass;

    [SerializeField] private Material lootContainerMaterial;
    [SerializeField] private Vector3 lootContainerSize = new Vector3(1.2f, 0.9f, 0.9f);
    [SerializeField] private LootTableDefinition selectedLootTable;

    [SerializeField] private PrototypeEnemySpawnProfile selectedEnemyProfile;
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(6f, 2f, 6f);
    [SerializeField] private int spawnAreaMinCount = 2;
    [SerializeField] private int spawnAreaMaxCount = 4;

    [MenuItem("Tools/Prototype/Raid Toolkit")]
    public static void OpenWindow()
    {
        GetWindow<PrototypeRaidToolkitWindow>("Raid Toolkit");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Blockout And Terrain", EditorStyles.boldLabel);
        blockoutMaterial = (Material)EditorGUILayout.ObjectField("Material", blockoutMaterial, typeof(Material), false);
        blockoutBoxSize = EditorGUILayout.Vector3Field("Box Size", blockoutBoxSize);
        if (GUILayout.Button("Create Blockout Box"))
        {
            CreateBlockoutBox();
        }

        rampSize = EditorGUILayout.Vector3Field("Ramp Size", rampSize);
        rampAngle = EditorGUILayout.Slider("Ramp Angle", rampAngle, 5f, 45f);
        if (GUILayout.Button("Create Ramp"))
        {
            CreateRamp();
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Prefab Placement", EditorStyles.boldLabel);
        prefabToPlace = (GameObject)EditorGUILayout.ObjectField("Prefab", prefabToPlace, typeof(GameObject), false);
        prefabScale = EditorGUILayout.Vector3Field("Scale", prefabScale);
        randomizePrefabYaw = EditorGUILayout.Toggle("Randomize Yaw", randomizePrefabYaw);
        dropPrefabToGround = EditorGUILayout.Toggle("Drop To Ground", dropPrefabToGround);
        if (GUILayout.Button("Place Prefab At Scene Pivot"))
        {
            PlacePrefab();
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Interactables", EditorStyles.boldLabel);
        doorMaterial = (Material)EditorGUILayout.ObjectField("Door Material", doorMaterial, typeof(Material), false);
        doorWidth = EditorGUILayout.FloatField("Door Width", doorWidth);
        doorHeight = EditorGUILayout.FloatField("Door Height", doorHeight);
        doorThickness = EditorGUILayout.FloatField("Door Thickness", doorThickness);
        doorOpenAngle = EditorGUILayout.FloatField("Door Open Angle", doorOpenAngle);
        if (GUILayout.Button("Create Door"))
        {
            CreateDoor();
        }

        breakableMaterial = (Material)EditorGUILayout.ObjectField("Breakable Material", breakableMaterial, typeof(Material), false);
        breakableSize = EditorGUILayout.Vector3Field("Breakable Size", breakableSize);
        breakableType = (PrototypeBreakableMaterialType)EditorGUILayout.EnumPopup("Breakable Type", breakableType);
        if (GUILayout.Button("Create Breakable Panel"))
        {
            CreateBreakablePanel();
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Loot", EditorStyles.boldLabel);
        lootContainerMaterial = (Material)EditorGUILayout.ObjectField("Loot Material", lootContainerMaterial, typeof(Material), false);
        selectedLootTable = (LootTableDefinition)EditorGUILayout.ObjectField("Loot Table", selectedLootTable, typeof(LootTableDefinition), false);
        lootContainerSize = EditorGUILayout.Vector3Field("Container Size", lootContainerSize);
        if (GUILayout.Button("Create Random Loot Container"))
        {
            CreateRandomLootContainer();
        }

        if (GUILayout.Button("Create Ground Loot Spawn Point"))
        {
            CreateGroundLootSpawnPoint();
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Enemy Spawns", EditorStyles.boldLabel);
        selectedEnemyProfile = (PrototypeEnemySpawnProfile)EditorGUILayout.ObjectField("Enemy Profile", selectedEnemyProfile, typeof(PrototypeEnemySpawnProfile), false);
        spawnAreaSize = EditorGUILayout.Vector3Field("Area Size", spawnAreaSize);
        spawnAreaMinCount = EditorGUILayout.IntField("Area Min Count", spawnAreaMinCount);
        spawnAreaMaxCount = EditorGUILayout.IntField("Area Max Count", spawnAreaMaxCount);
        if (GUILayout.Button("Create Enemy Spawn Point"))
        {
            CreateEnemySpawnPoint();
        }

        if (GUILayout.Button("Create Enemy Spawn Area"))
        {
            CreateEnemySpawnArea();
        }
    }

    private void CreateBlockoutBox()
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "Blockout_Box";
        block.transform.position = ResolvePlacementPosition();
        block.transform.localScale = SanitizeSize(blockoutBoxSize);
        block.transform.SetParent(GetOrCreateRoot(BlockoutRootName), true);
        ApplyMaterial(block.GetComponent<Renderer>(), blockoutMaterial);
        RegisterCreatedObject(block, "Create Blockout Box");
    }

    private void CreateRamp()
    {
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Blockout_Ramp";
        ramp.transform.position = ResolvePlacementPosition();
        ramp.transform.localScale = SanitizeSize(rampSize);
        ramp.transform.rotation = Quaternion.Euler(rampAngle, 0f, 0f);
        ramp.transform.SetParent(GetOrCreateRoot(BlockoutRootName), true);
        ApplyMaterial(ramp.GetComponent<Renderer>(), blockoutMaterial);
        RegisterCreatedObject(ramp, "Create Ramp");
    }

    private void PlacePrefab()
    {
        if (prefabToPlace == null)
        {
            return;
        }

        GameObject instance = PrefabUtility.IsPartOfPrefabAsset(prefabToPlace)
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefabToPlace)
            : Instantiate(prefabToPlace);

        instance.name = prefabToPlace.name;
        instance.transform.position = ResolvePlacementPosition(dropPrefabToGround);
        instance.transform.rotation = randomizePrefabYaw
            ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
            : Quaternion.identity;
        instance.transform.localScale = prefabScale;
        instance.transform.SetParent(GetOrCreateRoot(PrefabRootName), true);
        RegisterCreatedObject(instance, "Place Prefab");
    }

    private void CreateDoor()
    {
        Vector3 position = ResolvePlacementPosition();
        Transform parent = GetOrCreateRoot(InteractableRootName);
        GameObject root = new GameObject("Door");
        root.transform.SetParent(parent, true);
        root.transform.position = position;

        GameObject hinge = new GameObject("Hinge");
        hinge.transform.SetParent(root.transform, false);
        hinge.transform.localPosition = new Vector3(-Mathf.Abs(doorWidth) * 0.5f, 0f, 0f);

        GameObject doorLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorLeaf.name = "DoorLeaf";
        doorLeaf.transform.SetParent(hinge.transform, false);
        doorLeaf.transform.localPosition = new Vector3(Mathf.Abs(doorWidth) * 0.5f, Mathf.Abs(doorHeight) * 0.5f, 0f);
        doorLeaf.transform.localScale = new Vector3(Mathf.Abs(doorWidth), Mathf.Abs(doorHeight), Mathf.Abs(doorThickness));
        ApplyMaterial(doorLeaf.GetComponent<Renderer>(), doorMaterial);

        PrototypeDoor door = root.AddComponent<PrototypeDoor>();
        door.Configure(hinge.transform, doorLeaf.GetComponent<Collider>(), doorOpenAngle, false);
        RegisterCreatedObject(root, "Create Door");
    }

    private void CreateBreakablePanel()
    {
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = breakableType == PrototypeBreakableMaterialType.Glass ? "Breakable_Glass" : "Breakable_Wood";
        panel.transform.position = ResolvePlacementPosition();
        panel.transform.localScale = SanitizeSize(breakableSize);
        panel.transform.SetParent(GetOrCreateRoot(InteractableRootName), true);
        ApplyMaterial(panel.GetComponent<Renderer>(), breakableMaterial);

        PrototypeBreakable breakable = panel.AddComponent<PrototypeBreakable>();
        breakable.Configure(breakableType, breakableType == PrototypeBreakableMaterialType.Glass ? 14f : 22f);
        RegisterCreatedObject(panel, "Create Breakable Panel");
    }

    private void CreateRandomLootContainer()
    {
        GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crate.name = "LootContainer_Random";
        crate.transform.position = ResolvePlacementPosition();
        crate.transform.localScale = SanitizeSize(lootContainerSize);
        crate.transform.SetParent(GetOrCreateRoot(LootRootName), true);
        ApplyMaterial(crate.GetComponent<Renderer>(), lootContainerMaterial);

        InventoryContainer inventory = crate.AddComponent<InventoryContainer>();
        inventory.Configure("Supply Crate", 8, 0f);

        LootContainer lootContainer = crate.AddComponent<LootContainer>();
        lootContainer.Configure("Supply Crate", inventory, "Search");
        if (selectedLootTable != null)
        {
            lootContainer.ConfigureRandomLoot(selectedLootTable, true, true);
        }

        RegisterCreatedObject(crate, "Create Random Loot Container");
    }

    private void CreateGroundLootSpawnPoint()
    {
        GameObject spawnPointObject = new GameObject("GroundLootSpawnPoint");
        spawnPointObject.transform.position = ResolvePlacementPosition();
        spawnPointObject.transform.SetParent(GetOrCreateRoot(LootRootName), true);

        GroundLootSpawnPoint spawnPoint = spawnPointObject.AddComponent<GroundLootSpawnPoint>();
        spawnPoint.Configure(selectedLootTable, 0.75f);

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "Marker";
        marker.transform.SetParent(spawnPointObject.transform, false);
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = Vector3.one * 0.22f;
        ApplyMaterial(marker.GetComponent<Renderer>(), lootContainerMaterial);
        DestroyImmediate(marker.GetComponent<Collider>());

        RegisterCreatedObject(spawnPointObject, "Create Ground Loot Spawn Point");
    }

    private void CreateEnemySpawnPoint()
    {
        if (selectedEnemyProfile == null)
        {
            return;
        }

        PrototypeEncounterDirector director = GetOrCreateEncounterDirector();
        GameObject spawnPointObject = new GameObject($"SpawnPoint_{selectedEnemyProfile.DisplayName}");
        spawnPointObject.transform.position = ResolvePlacementPosition();
        spawnPointObject.transform.SetParent(director.transform, true);

        PrototypeEnemySpawnPoint spawnPoint = spawnPointObject.AddComponent<PrototypeEnemySpawnPoint>();
        spawnPoint.Configure(selectedEnemyProfile);
        RegisterCreatedObject(spawnPointObject, "Create Enemy Spawn Point");
    }

    private void CreateEnemySpawnArea()
    {
        if (selectedEnemyProfile == null)
        {
            return;
        }

        PrototypeEncounterDirector director = GetOrCreateEncounterDirector();
        GameObject spawnAreaObject = new GameObject($"SpawnArea_{selectedEnemyProfile.DisplayName}");
        spawnAreaObject.transform.position = ResolvePlacementPosition();
        spawnAreaObject.transform.SetParent(director.transform, true);

        PrototypeEnemySpawnArea spawnArea = spawnAreaObject.AddComponent<PrototypeEnemySpawnArea>();
        spawnArea.Configure(
            new[] { selectedEnemyProfile },
            Mathf.Max(1, spawnAreaMinCount),
            Mathf.Max(Mathf.Max(1, spawnAreaMinCount), spawnAreaMaxCount),
            SanitizeSize(spawnAreaSize),
            3f,
            3);
        RegisterCreatedObject(spawnAreaObject, "Create Enemy Spawn Area");
    }

    private PrototypeEncounterDirector GetOrCreateEncounterDirector()
    {
        PrototypeEncounterDirector director = Object.FindFirstObjectByType<PrototypeEncounterDirector>();
        if (director != null)
        {
            return director;
        }

        GameObject directorObject = new GameObject(DirectorRootName);
        director = directorObject.AddComponent<PrototypeEncounterDirector>();
        RegisterCreatedObject(directorObject, "Create Encounter Director");
        return director;
    }

    private static void RegisterCreatedObject(GameObject createdObject, string undoLabel)
    {
        if (createdObject == null)
        {
            return;
        }

        Undo.RegisterCreatedObjectUndo(createdObject, undoLabel);
        Selection.activeGameObject = createdObject;
        EditorSceneManager.MarkSceneDirty(createdObject.scene);
    }

    private static void ApplyMaterial(Renderer renderer, Material material)
    {
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static Transform GetOrCreateRoot(string rootName)
    {
        GameObject existing = GameObject.Find(rootName);
        if (existing != null)
        {
            return existing.transform;
        }

        GameObject root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, $"Create {rootName}");
        return root.transform;
    }

    private static Vector3 ResolvePlacementPosition(bool snapToGround = false)
    {
        Vector3 position = Selection.activeTransform != null
            ? Selection.activeTransform.position
            : (SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero);

        if (!snapToGround)
        {
            return position;
        }

        Vector3 rayOrigin = position + Vector3.up * 8f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return position;
    }

    private static Vector3 SanitizeSize(Vector3 size)
    {
        size.x = Mathf.Max(0.1f, Mathf.Abs(size.x));
        size.y = Mathf.Max(0.1f, Mathf.Abs(size.y));
        size.z = Mathf.Max(0.1f, Mathf.Abs(size.z));
        return size;
    }
}
