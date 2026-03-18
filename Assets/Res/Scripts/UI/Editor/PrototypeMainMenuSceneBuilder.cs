using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PrototypeMainMenuSceneBuilder
{
    // Trigger-based build keeps the menu scene reproducible without relying on a live MCP session.
    private const string TriggerAssetPath = "Assets/Res/PrototypeMainMenuScene.trigger.txt";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BaseScenePath = "Assets/Scenes/BaseScene.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ResourcesFolder = "Assets/Resources";
    private const string CatalogAssetPath = "Assets/Resources/PrototypeItemCatalog.asset";
    private const string MerchantCatalogAssetPath = "Assets/Resources/PrototypeMerchantCatalog.asset";
    private const string ItemDefinitionFolder = "Assets/Res/Data/PrototypeFPS/Items";
    private const string WeaponDefinitionFolder = "Assets/Res/Data/PrototypeFPS/Weapons";

    static PrototypeMainMenuSceneBuilder()
    {
        EditorApplication.delayCall += TryBuildFromTrigger;
    }

    [MenuItem("Tools/Prototype/Build Main Menu Scene")]
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
        EnsureFolder(ResourcesFolder);

        PrototypeItemCatalog catalog = CreateOrUpdateCatalog();
        PrototypeMerchantCatalog merchantCatalog = CreateOrUpdateMerchantCatalog();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        CreateDirectionalLight();
        CreateBackdrop();

        GameObject cameraObject = new GameObject("MainMenuCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.08f, 0.11f, 1f);
        camera.fieldOfView = 52f;
        camera.transform.position = new Vector3(0f, 4.8f, -10.5f);
        camera.transform.rotation = Quaternion.Euler(16f, 0f, 0f);
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();

        GameObject systems = new GameObject("MainMenuSystems");
        PrototypeMainMenuController controller = systems.AddComponent<PrototypeMainMenuController>();
        SetSerializedReference(controller, "itemCatalog", catalog);
        SetSerializedReference(controller, "merchantCatalog", merchantCatalog);
        SetSerializedEnum(controller, "shellMode", (int)PrototypeMainMenuController.MetaShellMode.DebugShell);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static PrototypeItemCatalog CreateOrUpdateCatalog()
    {
        PrototypeItemCatalog catalog = AssetDatabase.LoadAssetAtPath<PrototypeItemCatalog>(CatalogAssetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<PrototypeItemCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
        }

        var itemAssets = new List<ItemDefinition>();
        string[] itemGuids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { ItemDefinitionFolder });
        for (int index = 0; index < itemGuids.Length; index++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(itemGuids[index]);
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
            if (item != null)
            {
                itemAssets.Add(item);
            }
        }

        var weaponAssets = new List<PrototypeWeaponDefinition>();
        string[] weaponGuids = AssetDatabase.FindAssets("t:PrototypeWeaponDefinition", new[] { WeaponDefinitionFolder });
        for (int index = 0; index < weaponGuids.Length; index++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(weaponGuids[index]);
            PrototypeWeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<PrototypeWeaponDefinition>(assetPath);
            if (weapon != null)
            {
                weaponAssets.Add(weapon);
            }
        }

        SerializedObject serializedCatalog = new SerializedObject(catalog);
        AssignDefinitions(serializedCatalog.FindProperty("items"), itemAssets);
        AssignWeaponDefinitions(serializedCatalog.FindProperty("weapons"), weaponAssets);

        ItemDefinition rifleAmmo = FindItem(itemAssets, "rifle_ammo");
        ItemDefinition pistolAmmo = FindItem(itemAssets, "pistol_ammo");
        ItemDefinition medkit = FindItem(itemAssets, "field_medkit");
        ItemDefinition bandage = FindItem(itemAssets, "bandage_roll");
        ItemDefinition tourniquet = FindItem(itemAssets, "tourniquet");
        ItemDefinition splint = FindItem(itemAssets, "field_splint");
        ItemDefinition painkiller = FindItem(itemAssets, "painkillers");
        ItemDefinition cash = FindItem(itemAssets, "cash_bundle");
        ItemDefinition helmet = FindItem(itemAssets, "helmet_alpha");
        ItemDefinition rig = FindItem(itemAssets, "armored_rig");
        PrototypeWeaponDefinition carbine = FindWeapon(weaponAssets, "carbine_alpha");
        PrototypeWeaponDefinition sidearm = FindWeapon(weaponAssets, "sidearm_9mm");
        PrototypeWeaponDefinition knife = FindWeapon(weaponAssets, "combat_knife");

        AssignPresets(
            serializedCatalog.FindProperty("defaultStashItems"),
            new[]
            {
                (cash, 8),
                (rifleAmmo, 90),
                (pistolAmmo, 48),
                (medkit, 2),
                (bandage, 4),
                (tourniquet, 2),
                (splint, 2),
                (painkiller, 2),
                (helmet, 1),
                (rig, 1)
            });

        AssignPresets(
            serializedCatalog.FindProperty("defaultLoadoutItems"),
            new[]
            {
                (rifleAmmo, 90),
                (pistolAmmo, 45),
                (medkit, 1),
                (bandage, 2),
                (tourniquet, 1),
                (splint, 1),
                (painkiller, 1)
            });

        AssignWeaponPresets(
            serializedCatalog.FindProperty("defaultStashWeapons"),
            new[]
            {
                (carbine, 1),
                (sidearm, 1),
                (knife, 1)
            });

        serializedCatalog.FindProperty("defaultPrimaryWeapon").objectReferenceValue = carbine;
        serializedCatalog.FindProperty("defaultSecondaryWeapon").objectReferenceValue = sidearm;
        serializedCatalog.FindProperty("defaultMeleeWeapon").objectReferenceValue = knife;

        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static PrototypeMerchantCatalog CreateOrUpdateMerchantCatalog()
    {
        PrototypeMerchantCatalog merchantCatalog = AssetDatabase.LoadAssetAtPath<PrototypeMerchantCatalog>(MerchantCatalogAssetPath);
        if (merchantCatalog == null)
        {
            merchantCatalog = ScriptableObject.CreateInstance<PrototypeMerchantCatalog>();
            AssetDatabase.CreateAsset(merchantCatalog, MerchantCatalogAssetPath);
        }

        var itemAssets = new List<ItemDefinition>();
        string[] itemGuids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { ItemDefinitionFolder });
        for (int index = 0; index < itemGuids.Length; index++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(itemGuids[index]);
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
            if (item != null)
            {
                itemAssets.Add(item);
            }
        }

        var weaponAssets = new List<PrototypeWeaponDefinition>();
        string[] weaponGuids = AssetDatabase.FindAssets("t:PrototypeWeaponDefinition", new[] { WeaponDefinitionFolder });
        for (int index = 0; index < weaponGuids.Length; index++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(weaponGuids[index]);
            PrototypeWeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<PrototypeWeaponDefinition>(assetPath);
            if (weapon != null)
            {
                weaponAssets.Add(weapon);
            }
        }

        ItemDefinition rifleAmmo = FindItem(itemAssets, "rifle_ammo");
        ItemDefinition pistolAmmo = FindItem(itemAssets, "pistol_ammo");
        ItemDefinition medkit = FindItem(itemAssets, "field_medkit");
        ItemDefinition bandage = FindItem(itemAssets, "bandage_roll");
        ItemDefinition tourniquet = FindItem(itemAssets, "tourniquet");
        ItemDefinition splint = FindItem(itemAssets, "field_splint");
        ItemDefinition painkiller = FindItem(itemAssets, "painkillers");
        ItemDefinition helmet = FindItem(itemAssets, "helmet_alpha");
        ItemDefinition rig = FindItem(itemAssets, "armored_rig");
        ItemDefinition secureCase = FindItem(itemAssets, "secure_case_alpha");
        PrototypeWeaponDefinition carbine = FindWeapon(weaponAssets, "carbine_alpha");
        PrototypeWeaponDefinition sidearm = FindWeapon(weaponAssets, "sidearm_9mm");
        PrototypeWeaponDefinition knife = FindWeapon(weaponAssets, "combat_knife");

        merchantCatalog.Configure(
            0.55f,
            0.6f,
            new PrototypeMerchantCatalog.MerchantDefinition
            {
                merchantId = "weapons_trader",
                displayName = "武器商人",
                merchantLevel = 4,
                itemOffers = new List<PrototypeMerchantCatalog.ItemOffer>
                {
                    new PrototypeMerchantCatalog.ItemOffer { definition = rifleAmmo, quantity = 30, price = 6 },
                    new PrototypeMerchantCatalog.ItemOffer { definition = pistolAmmo, quantity = 24, price = 4 }
                },
                weaponOffers = new List<PrototypeMerchantCatalog.WeaponOffer>
                {
                    new PrototypeMerchantCatalog.WeaponOffer { definition = carbine, price = 24 },
                    new PrototypeMerchantCatalog.WeaponOffer { definition = sidearm, price = 16 },
                    new PrototypeMerchantCatalog.WeaponOffer { definition = knife, price = 10 }
                }
            },
            new PrototypeMerchantCatalog.MerchantDefinition
            {
                merchantId = "medical_trader",
                displayName = "医疗商人",
                merchantLevel = 3,
                itemOffers = new List<PrototypeMerchantCatalog.ItemOffer>
                {
                    new PrototypeMerchantCatalog.ItemOffer { definition = medkit, quantity = 1, price = 10 },
                    new PrototypeMerchantCatalog.ItemOffer { definition = bandage, quantity = 1, price = 3 },
                    new PrototypeMerchantCatalog.ItemOffer { definition = tourniquet, quantity = 1, price = 5 },
                    new PrototypeMerchantCatalog.ItemOffer { definition = splint, quantity = 1, price = 4 },
                    new PrototypeMerchantCatalog.ItemOffer { definition = painkiller, quantity = 1, price = 4 }
                }
            },
            new PrototypeMerchantCatalog.MerchantDefinition
            {
                merchantId = "armor_trader",
                displayName = "护甲商人",
                merchantLevel = 4,
                itemOffers = new List<PrototypeMerchantCatalog.ItemOffer>
                {
                    new PrototypeMerchantCatalog.ItemOffer { definition = helmet, quantity = 1, price = 14 },
                    new PrototypeMerchantCatalog.ItemOffer { definition = rig, quantity = 1, price = 20 }
                }
            },
            new PrototypeMerchantCatalog.MerchantDefinition
            {
                merchantId = "general_trader",
                displayName = "杂货商人",
                merchantLevel = 2,
                itemOffers = new List<PrototypeMerchantCatalog.ItemOffer>
                {
                    new PrototypeMerchantCatalog.ItemOffer { definition = painkiller, quantity = 1, price = 4 },
                    new PrototypeMerchantCatalog.ItemOffer { definition = splint, quantity = 1, price = 4 },
                    new PrototypeMerchantCatalog.ItemOffer { definition = secureCase, quantity = 1, price = 32 }
                },
                weaponOffers = new List<PrototypeMerchantCatalog.WeaponOffer>
                {
                    new PrototypeMerchantCatalog.WeaponOffer { definition = knife, price = 11 }
                }
            });

        EditorUtility.SetDirty(merchantCatalog);
        return merchantCatalog;
    }

    private static void CreateDirectionalLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.82f;
        light.color = new Color(0.95f, 0.96f, 1f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
    }

    private static void CreateBackdrop()
    {
        Material floorMaterial = CreateRuntimeMaterial("MainMenu_Floor", new Color(0.19f, 0.22f, 0.26f, 1f));
        Material wallMaterial = CreateRuntimeMaterial("MainMenu_Wall", new Color(0.12f, 0.14f, 0.18f, 1f));
        Material accentMaterial = CreateRuntimeMaterial("MainMenu_Accent", new Color(0.95f, 0.55f, 0.18f, 1f));

        CreatePrimitive("Floor", PrimitiveType.Cube, new Vector3(0f, -0.2f, 0f), new Vector3(20f, 0.4f, 12f), floorMaterial);
        CreatePrimitive("BackWall", PrimitiveType.Cube, new Vector3(0f, 4f, 5f), new Vector3(20f, 8f, 0.4f), wallMaterial);
        CreatePrimitive("LeftWall", PrimitiveType.Cube, new Vector3(-10f, 4f, 0f), new Vector3(0.4f, 8f, 12f), wallMaterial);
        CreatePrimitive("RightWall", PrimitiveType.Cube, new Vector3(10f, 4f, 0f), new Vector3(0.4f, 8f, 12f), wallMaterial);
        CreatePrimitive("AccentBar", PrimitiveType.Cube, new Vector3(0f, 2.2f, 4.7f), new Vector3(7.2f, 0.18f, 0.18f), accentMaterial);
        CreatePrimitive("Console_Left", PrimitiveType.Cube, new Vector3(-4.6f, 1.1f, 1.8f), new Vector3(1.1f, 2.2f, 1.1f), floorMaterial);
        CreatePrimitive("Console_Right", PrimitiveType.Cube, new Vector3(4.8f, 1.4f, 2.3f), new Vector3(1.6f, 2.8f, 1.2f), floorMaterial);
    }

    private static Material CreateRuntimeMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader)
        {
            name = name,
            color = color
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        return material;
    }

    private static void CreatePrimitive(string name, PrimitiveType primitiveType, Vector3 position, Vector3 scale, Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = name;
        primitive.transform.position = position;
        primitive.transform.localScale = scale;
        Renderer renderer = primitive.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static ItemDefinition FindItem(List<ItemDefinition> items, string itemId)
    {
        for (int index = 0; index < items.Count; index++)
        {
            ItemDefinition item = items[index];
            if (item != null && item.ItemId == itemId)
            {
                return item;
            }
        }

        return null;
    }

    private static PrototypeWeaponDefinition FindWeapon(List<PrototypeWeaponDefinition> weapons, string weaponId)
    {
        for (int index = 0; index < weapons.Count; index++)
        {
            PrototypeWeaponDefinition weapon = weapons[index];
            if (weapon != null && weapon.WeaponId == weaponId)
            {
                return weapon;
            }
        }

        return null;
    }

    private static void AssignDefinitions(SerializedProperty property, List<ItemDefinition> items)
    {
        property.arraySize = items.Count;
        for (int index = 0; index < items.Count; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = items[index];
        }
    }

    private static void AssignWeaponDefinitions(SerializedProperty property, List<PrototypeWeaponDefinition> weapons)
    {
        property.arraySize = weapons.Count;
        for (int index = 0; index < weapons.Count; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = weapons[index];
        }
    }

    private static void AssignPresets(SerializedProperty property, IEnumerable<(ItemDefinition definition, int quantity)> presets)
    {
        var validPresets = new List<(ItemDefinition definition, int quantity)>();
        foreach ((ItemDefinition definition, int quantity) in presets)
        {
            if (definition == null || quantity <= 0)
            {
                continue;
            }

            validPresets.Add((definition, quantity));
        }

        property.arraySize = validPresets.Count;
        for (int index = 0; index < validPresets.Count; index++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("definition").objectReferenceValue = validPresets[index].definition;
            element.FindPropertyRelative("quantity").intValue = validPresets[index].quantity;
        }
    }

    private static void AssignWeaponPresets(SerializedProperty property, IEnumerable<(PrototypeWeaponDefinition definition, int quantity)> presets)
    {
        var validPresets = new List<(PrototypeWeaponDefinition definition, int quantity)>();
        foreach ((PrototypeWeaponDefinition definition, int quantity) in presets)
        {
            if (definition == null || quantity <= 0)
            {
                continue;
            }

            validPresets.Add((definition, quantity));
        }

        property.arraySize = validPresets.Count;
        for (int index = 0; index < validPresets.Count; index++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("definition").objectReferenceValue = validPresets[index].definition;
            element.FindPropertyRelative("quantity").intValue = validPresets[index].quantity;
        }
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

    private static void SetSerializedEnum(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.intValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
