using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PrototypeIndoorSceneBuilder
{
    private const string TriggerAssetPath = "Assets/Res/PrototypeIndoorScene.trigger.txt";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string MaterialFolder = "Assets/Res/Materials/PrototypeIndoor";
    private const string DataFolder = "Assets/Res/Data";
    private const string PrototypeDataFolder = "Assets/Res/Data/PrototypeFPS";
    private const string UnitDefinitionFolder = "Assets/Res/Data/PrototypeFPS/UnitDefinitions";
    private const string ItemDefinitionFolder = "Assets/Res/Data/PrototypeFPS/Items";
    private const string WeaponDefinitionFolder = "Assets/Res/Data/PrototypeFPS/Weapons";
    private const string LootTableFolder = "Assets/Res/Data/PrototypeFPS/LootTables";
    private const string EnemyProfileFolder = "Assets/Res/Data/PrototypeFPS/EnemyProfiles";
    private const string ItemCatalogPath = "Assets/Resources/PrototypeItemCatalog.asset";
    private const string HumanoidDefinitionPath = "Assets/Res/Data/PrototypeFPS/UnitDefinitions/Unit_Humanoid.asset";
    private const string CashItemPath = "Assets/Res/Data/PrototypeFPS/Items/Item_Cash.asset";
    private const string MedkitItemPath = "Assets/Res/Data/PrototypeFPS/Items/Item_Medkit.asset";
    private const string BandageItemPath = "Assets/Res/Data/PrototypeFPS/Items/Item_Bandage.asset";
    private const string TourniquetItemPath = "Assets/Res/Data/PrototypeFPS/Items/Item_Tourniquet.asset";
    private const string SplintItemPath = "Assets/Res/Data/PrototypeFPS/Items/Item_Splint.asset";
    private const string PainkillerItemPath = "Assets/Res/Data/PrototypeFPS/Items/Item_Painkillers.asset";
    private const string RifleAmmoItemPath = "Assets/Res/Data/PrototypeFPS/Items/Item_RifleAmmo.asset";
    private const string PistolAmmoItemPath = "Assets/Res/Data/PrototypeFPS/Items/Item_PistolAmmo.asset";
    private const string HelmetArmorPath = "Assets/Res/Data/PrototypeFPS/Items/Item_Helmet.asset";
    private const string VestArmorPath = "Assets/Res/Data/PrototypeFPS/Items/Item_ArmoredRig.asset";
    private const string CarbineWeaponPath = "Assets/Res/Data/PrototypeFPS/Weapons/Weapon_Carbine.asset";
    private const string SidearmWeaponPath = "Assets/Res/Data/PrototypeFPS/Weapons/Weapon_Sidearm.asset";
    private const string KnifeWeaponPath = "Assets/Res/Data/PrototypeFPS/Weapons/Weapon_CombatKnife.asset";
    private const string AmmoLootTablePath = "Assets/Res/Data/PrototypeFPS/LootTables/Loot_AmmoCache.asset";
    private const string MedicalLootTablePath = "Assets/Res/Data/PrototypeFPS/LootTables/Loot_MedicalStash.asset";
    private const string FloorLootTablePath = "Assets/Res/Data/PrototypeFPS/LootTables/Loot_FloorScatter.asset";
    private const string RegularZombieProfilePath = "Assets/Res/Data/PrototypeFPS/EnemyProfiles/Spawn_RegularZombie.asset";
    private const string PoliceZombieProfilePath = "Assets/Res/Data/PrototypeFPS/EnemyProfiles/Spawn_PoliceZombie.asset";
    private const string SoldierZombieProfilePath = "Assets/Res/Data/PrototypeFPS/EnemyProfiles/Spawn_SoldierZombie.asset";
    private const string ZombieDogProfilePath = "Assets/Res/Data/PrototypeFPS/EnemyProfiles/Spawn_ZombieDog.asset";
    private const string HeadPartId = "head";
    private const string TorsoPartId = "torso";
    private const string LegsPartId = "legs";
    private const int IgnoreRaycastLayer = 2;

    static PrototypeIndoorSceneBuilder()
    {
        EditorApplication.delayCall += TryBuildFromTrigger;
    }

    [MenuItem("Tools/Prototype/Build Indoor FPS Scene")]
    public static void BuildSceneFromMenu()
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
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        EnsureFolder("Assets/Res/Materials");
        EnsureFolder(MaterialFolder);
        EnsureFolder(DataFolder);
        EnsureFolder(PrototypeDataFolder);
        EnsureFolder(UnitDefinitionFolder);
        EnsureFolder(ItemDefinitionFolder);
        EnsureFolder(WeaponDefinitionFolder);
        EnsureFolder(LootTableFolder);
        EnsureFolder(EnemyProfileFolder);

        PrototypeUnitDefinition humanoidDefinition = CreateOrUpdateHumanoidDefinition(HumanoidDefinitionPath);
        ItemDefinition cashItem = CreateOrUpdateItemDefinition(CashItemPath, "cash_bundle", "Cash Bundle", "Light valuables for a quick extraction run.", 10, 0.2f);
        MedicalItemDefinition medkitItem = CreateOrUpdateMedicalDefinition(MedkitItemPath, "field_medkit", "Field Medkit", "A compact first-aid kit for raid sustain.", 2, 0.8f, 48f, 1, 0, 0, 0f);
        MedicalItemDefinition bandageItem = CreateOrUpdateMedicalDefinition(BandageItemPath, "bandage_roll", "Bandage Roll", "Stops light bleeding quickly.", 4, 0.12f, 8f, 2, 0, 0, 0f);
        MedicalItemDefinition tourniquetItem = CreateOrUpdateMedicalDefinition(TourniquetItemPath, "tourniquet", "Tourniquet", "Emergency heavy bleed control.", 2, 0.18f, 0f, 0, 1, 0, 0f);
        MedicalItemDefinition splintItem = CreateOrUpdateMedicalDefinition(SplintItemPath, "field_splint", "Field Splint", "Stabilizes fractured limbs for movement.", 2, 0.22f, 0f, 0, 0, 1, 0f);
        MedicalItemDefinition painkillerItem = CreateOrUpdateMedicalDefinition(PainkillerItemPath, "painkillers", "Painkillers", "Suppresses fracture penalties for a short time.", 2, 0.1f, 0f, 0, 0, 0, 75f);
        AmmoDefinition rifleAmmoItem = CreateOrUpdateAmmoDefinition(RifleAmmoItemPath, "rifle_ammo", "5.56 FMJ", "Balanced rifle rounds with decent armor penetration.", 90, 0.02f, 33f, 18f, 30f, 24f, 0.14f, 0.05f, 0.1f);
        AmmoDefinition pistolAmmoItem = CreateOrUpdateAmmoDefinition(PistolAmmoItemPath, "pistol_ammo", "9mm JHP", "Soft pistol rounds with lower penetration but stronger flesh damage.", 60, 0.015f, 27f, 12f, 12f, 10f, 0.24f, 0.08f, 0.14f);
        ArmorDefinition helmetArmor = CreateOrUpdateArmorDefinition(HelmetArmorPath, "helmet_alpha", "Prototype Helmet", "Basic head protection for penetration testing.", 1.6f, 4, 38f, 0.16f, 0.85f, 1.15f, 0.55f, 0.2f, HeadPartId);
        ArmorDefinition vestArmor = CreateOrUpdateArmorDefinition(VestArmorPath, "armored_rig", "Prototype Armored Rig", "Torso armor that trades weight for survivability.", 8.2f, 4, 72f, 0.22f, 1.05f, 1.3f, 0.4f, 0.55f, TorsoPartId);
        PrototypeWeaponDefinition carbineWeapon = CreateOrUpdateFirearmDefinition(
            CarbineWeaponPath,
            "carbine_alpha",
            "AR-4 Carbine",
            "A controllable primary weapon with automatic and burst fire.",
            rifleAmmoItem,
            30,
            720f,
            2.1f,
            72f,
            0.18f,
            2f,
            3,
            PrototypeWeaponFireMode.Auto,
            PrototypeWeaponFireMode.Burst);
        PrototypeWeaponDefinition sidearmWeapon = CreateOrUpdateFirearmDefinition(
            SidearmWeaponPath,
            "sidearm_9mm",
            "VX-9 Sidearm",
            "A reliable semi-auto backup pistol.",
            pistolAmmoItem,
            15,
            360f,
            1.45f,
            48f,
            0.08f,
            1f,
            1,
            PrototypeWeaponFireMode.Semi);
        PrototypeWeaponDefinition knifeWeapon = CreateOrUpdateMeleeDefinition(
            KnifeWeaponPath,
            "combat_knife",
            "Tactical Knife",
            "A close-range blade for fast melee takedowns.",
            62f,
            2.15f,
            0.42f,
            0.52f);

        Material floorMat = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Floor.mat", new Color(0.25f, 0.27f, 0.30f));
        Material wallMat = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Wall.mat", new Color(0.54f, 0.59f, 0.66f));
        Material ceilingMat = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Ceiling.mat", new Color(0.80f, 0.82f, 0.84f));
        Material accentMat = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Accent.mat", new Color(0.92f, 0.51f, 0.19f));
        Material targetMat = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Target.mat", new Color(0.82f, 0.22f, 0.18f));
        Material propMat = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Prop.mat", new Color(0.34f, 0.44f, 0.31f));
        Material lootMat = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Loot.mat", new Color(0.91f, 0.79f, 0.29f));
        Material medicalMat = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Medical.mat", new Color(0.25f, 0.68f, 0.48f));
        Material extractMat = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Extract.mat", new Color(0.21f, 0.79f, 0.55f));
        Material glassMat = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Glass.mat", new Color(0.52f, 0.84f, 0.95f, 0.75f));
        Material woodMat = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Wood.mat", new Color(0.58f, 0.38f, 0.19f));
        LootTableDefinition ammoLootTable = CreateOrUpdateLootTable(
            AmmoLootTablePath,
            "ammo_cache",
            "Ammo Cache",
            2,
            4,
            true,
            CreateLootEntry(rifleAmmoItem, 18, 36, 1.2f),
            CreateLootEntry(pistolAmmoItem, 12, 30, 1f),
            CreateLootEntry(cashItem, 1, 3, 0.45f));
        LootTableDefinition medicalLootTable = CreateOrUpdateLootTable(
            MedicalLootTablePath,
            "medical_stash",
            "Medical Stash",
            2,
            4,
            true,
            CreateLootEntry(medkitItem, 1, 1, 0.45f),
            CreateLootEntry(bandageItem, 1, 2, 1.4f),
            CreateLootEntry(tourniquetItem, 1, 1, 0.65f),
            CreateLootEntry(splintItem, 1, 1, 0.75f),
            CreateLootEntry(painkillerItem, 1, 1, 0.9f));
        LootTableDefinition floorLootTable = CreateOrUpdateLootTable(
            FloorLootTablePath,
            "floor_scatter",
            "Floor Scatter",
            1,
            3,
            true,
            CreateLootEntry(cashItem, 1, 4, 1.2f),
            CreateLootEntry(pistolAmmoItem, 8, 18, 0.8f),
            CreateLootEntry(bandageItem, 1, 1, 0.5f));
        PrototypeEnemySpawnProfile regularZombieProfile = CreateOrUpdateEnemySpawnProfile(
            RegularZombieProfilePath,
            "regular_zombie",
            "Regular Zombie",
            PrototypeEnemyArchetype.RegularZombie,
            humanoidDefinition,
            knifeWeapon,
            targetMat,
            new Vector3(0.9f, 1.1f, 0.9f));
        PrototypeEnemySpawnProfile policeZombieProfile = CreateOrUpdateEnemySpawnProfile(
            PoliceZombieProfilePath,
            "police_zombie",
            "Police Zombie",
            PrototypeEnemyArchetype.PoliceZombie,
            humanoidDefinition,
            sidearmWeapon,
            wallMat,
            new Vector3(0.9f, 1.1f, 0.9f),
            helmetArmor);
        PrototypeEnemySpawnProfile soldierZombieProfile = CreateOrUpdateEnemySpawnProfile(
            SoldierZombieProfilePath,
            "soldier_zombie",
            "Soldier Zombie",
            PrototypeEnemyArchetype.SoldierZombie,
            humanoidDefinition,
            carbineWeapon,
            propMat,
            new Vector3(0.9f, 1.1f, 0.9f),
            helmetArmor,
            vestArmor);
        PrototypeEnemySpawnProfile zombieDogProfile = CreateOrUpdateEnemySpawnProfile(
            ZombieDogProfilePath,
            "zombie_dog",
            "Zombie Dog",
            PrototypeEnemyArchetype.ZombieDog,
            humanoidDefinition,
            knifeWeapon,
            accentMat,
            new Vector3(0.68f, 0.72f, 1.05f));
        regularZombieProfile.SetCarriedLootTable(floorLootTable);
        policeZombieProfile.SetCarriedLootTable(medicalLootTable);
        soldierZombieProfile.SetCarriedLootTable(ammoLootTable);
        zombieDogProfile.SetCarriedLootTable(null);
        EditorUtility.SetDirty(regularZombieProfile);
        EditorUtility.SetDirty(policeZombieProfile);
        EditorUtility.SetDirty(soldierZombieProfile);
        EditorUtility.SetDirty(zombieDogProfile);

        DeleteIfExists("PrototypeIndoorRange");
        DeleteIfExists("FpsPlayer");
        DeleteIfExists("Main Camera");

        ConfigureDirectionalLight();

        GameObject root = new GameObject("PrototypeIndoorRange");

        CreateBox("Floor", root.transform, new Vector3(0f, 0f, 0f), new Vector3(16f, 0.4f, 18f), floorMat);
        CreateBox("Ceiling", root.transform, new Vector3(0f, 4f, 0f), new Vector3(16f, 0.3f, 18f), ceilingMat);
        CreateBox("Wall_North", root.transform, new Vector3(0f, 2f, 9f), new Vector3(16f, 4f, 0.4f), wallMat);
        CreateBox("Wall_South", root.transform, new Vector3(0f, 2f, -9f), new Vector3(16f, 4f, 0.4f), wallMat);
        CreateBox("Wall_East", root.transform, new Vector3(8f, 2f, 0f), new Vector3(0.4f, 4f, 18f), wallMat);
        CreateBox("Wall_West", root.transform, new Vector3(-8f, 2f, 0f), new Vector3(0.4f, 4f, 18f), wallMat);

        CreateBox("Divider_A", root.transform, new Vector3(-2.5f, 1.4f, 0f), new Vector3(0.4f, 2.8f, 8f), wallMat);
        CreateBox("Divider_B", root.transform, new Vector3(3f, 1.4f, -2f), new Vector3(0.4f, 2.8f, 7f), wallMat);
        CreateBox("Cover_Center", root.transform, new Vector3(0f, 0.6f, 3.5f), new Vector3(2.6f, 1.2f, 0.8f), accentMat);
        CreateBox("Cover_Left", root.transform, new Vector3(-5.2f, 0.75f, -1.5f), new Vector3(1.6f, 1.5f, 1.6f), propMat);
        CreateBox("Cover_Right", root.transform, new Vector3(5.1f, 0.55f, 2.4f), new Vector3(2.2f, 1.1f, 1.2f), propMat);
        CreateBox("Pillar_Left", root.transform, new Vector3(-6.1f, 1.5f, 5.8f), new Vector3(0.9f, 3f, 0.9f), wallMat);
        CreateBox("Pillar_Right", root.transform, new Vector3(6.2f, 1.5f, -5.5f), new Vector3(0.9f, 3f, 0.9f), wallMat);
        CreateBox("Bench", root.transform, new Vector3(0f, 0.45f, -6.5f), new Vector3(4f, 0.9f, 0.9f), accentMat);
        CreateCoverPoint(root.transform, "CoverPoint_Center_SouthLeft", new Vector3(-0.88f, 0f, 2.96f), Vector3.back);
        CreateCoverPoint(root.transform, "CoverPoint_Center_SouthRight", new Vector3(0.88f, 0f, 2.96f), Vector3.back);
        CreateCoverPoint(root.transform, "CoverPoint_Center_NorthLeft", new Vector3(-0.88f, 0f, 4.04f), Vector3.forward);
        CreateCoverPoint(root.transform, "CoverPoint_Center_NorthRight", new Vector3(0.88f, 0f, 4.04f), Vector3.forward);
        CreateCoverPoint(root.transform, "CoverPoint_LeftCrate_East", new Vector3(-4.28f, 0f, -1.5f), Vector3.right);
        CreateCoverPoint(root.transform, "CoverPoint_LeftCrate_West", new Vector3(-6.12f, 0f, -1.5f), Vector3.left);
        CreateCoverPoint(root.transform, "CoverPoint_LeftCrate_South", new Vector3(-5.2f, 0f, -2.42f), Vector3.back);
        CreateCoverPoint(root.transform, "CoverPoint_RightCrate_West", new Vector3(3.92f, 0f, 2.4f), Vector3.left);
        CreateCoverPoint(root.transform, "CoverPoint_RightCrate_East", new Vector3(6.28f, 0f, 2.4f), Vector3.right);
        CreateCoverPoint(root.transform, "CoverPoint_RightCrate_North", new Vector3(5.1f, 0f, 3.18f), Vector3.forward);
        CreateCoverPoint(root.transform, "CoverPoint_PillarLeft_South", new Vector3(-6.1f, 0f, 4.98f), Vector3.back);
        CreateCoverPoint(root.transform, "CoverPoint_PillarLeft_East", new Vector3(-5.28f, 0f, 5.8f), Vector3.right);
        CreateCoverPoint(root.transform, "CoverPoint_PillarRight_North", new Vector3(6.2f, 0f, -4.68f), Vector3.forward);
        CreateCoverPoint(root.transform, "CoverPoint_PillarRight_West", new Vector3(5.38f, 0f, -5.5f), Vector3.left);

        CreatePointLight(root.transform, new Vector3(-4.8f, 3.2f, -4.8f), new Color(1f, 0.87f, 0.72f), 4.5f, 14f);
        CreatePointLight(root.transform, new Vector3(0f, 3.3f, 0.5f), new Color(0.95f, 0.96f, 1f), 4.2f, 15f);
        CreatePointLight(root.transform, new Vector3(4.8f, 3.2f, 5f), new Color(0.72f, 0.85f, 1f), 4.3f, 14f);

        GameObject player = new GameObject("FpsPlayer");
        player.transform.position = new Vector3(0f, 1.05f, -6.2f);
        player.transform.rotation = Quaternion.identity;
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
        cameraRoot.tag = "MainCamera";
        camera.nearClipPlane = 0.03f;
        camera.fieldOfView = 75f;
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraRoot.AddComponent<AudioListener>();

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(cameraRoot.transform, false);
        muzzle.transform.localPosition = new Vector3(0.18f, -0.12f, 0.55f);

        CreatePrimaryWeaponViewModel(cameraRoot.transform, accentMat, wallMat, propMat);
        CreateSecondaryWeaponViewModel(cameraRoot.transform, accentMat, wallMat, propMat);
        CreateMeleeWeaponViewModel(cameraRoot.transform, wallMat, accentMat);

        PrototypeUnitVitals playerVitals = player.AddComponent<PrototypeUnitVitals>();
        playerVitals.SetUnitDefinition(humanoidDefinition);
        PrototypeStatusEffectController playerStatusEffects = GetOrAddComponent<PrototypeStatusEffectController>(player);
        playerStatusEffects.Bind(playerVitals);
        GetOrAddComponent<PrototypeCombatTextController>(player);
        playerVitals.SetArmorLoadout(helmetArmor, vestArmor);
        CreatePlayerHitboxes(player.transform, playerVitals);

        PrototypeFpsController fpsController = player.AddComponent<PrototypeFpsController>();
        SetSerializedReference(fpsController, "viewCamera", camera);
        SetSerializedReference(fpsController, "muzzle", muzzle.transform);
        fpsController.ConfigureWeaponLoadout(carbineWeapon, sidearmWeapon, knifeWeapon);

        GetOrAddComponent<PlayerInteractionState>(player);

        InventoryContainer inventory = player.AddComponent<InventoryContainer>();
        inventory.Configure("Raid Backpack", 12, 20f);
        inventory.TryAddItem(rifleAmmoItem, 90, out _);
        inventory.TryAddItem(pistolAmmoItem, 45, out _);
        inventory.TryAddItem(medkitItem, 1, out _);
        inventory.TryAddItem(bandageItem, 2, out _);
        inventory.TryAddItem(tourniquetItem, 1, out _);
        inventory.TryAddItem(splintItem, 1, out _);
        inventory.TryAddItem(painkillerItem, 1, out _);

        PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
        interactor.Configure(camera, inventory);
        GetOrAddComponent<LootContainerWindowController>(player);
        GetOrAddComponent<PlayerInventoryWindowController>(player);

        CreateTarget(root.transform, new Vector3(0f, 1.1f, 7f), targetMat, humanoidDefinition);
        CreateDoor(root.transform, "Door_Test", new Vector3(2.45f, 0f, -6.1f), doorWidth: 1.1f, doorHeight: 2.2f, doorThickness: 0.12f, material: wallMat);
        CreateBreakablePanel(root.transform, "Breakable_Glass", new Vector3(0f, 1.35f, 1.9f), new Vector3(2.4f, 2.1f, 0.08f), glassMat, PrototypeBreakableMaterialType.Glass, 16f);
        CreateBreakablePanel(root.transform, "Breakable_Wood", new Vector3(-3.3f, 1.25f, 4.4f), new Vector3(1.8f, 1.8f, 0.12f), woodMat, PrototypeBreakableMaterialType.ThinWood, 24f);

        GameObject raidSystems = new GameObject("RaidSystems");
        raidSystems.transform.SetParent(root.transform, false);
        RaidGameMode raidGameMode = raidSystems.AddComponent<RaidGameMode>();
        raidGameMode.Configure(interactor, playerVitals, 420f);
        PrototypeRaidProfileFlow raidProfileFlow = raidSystems.AddComponent<PrototypeRaidProfileFlow>();
        raidProfileFlow.Configure(
            raidGameMode,
            interactor,
            AssetDatabase.LoadAssetAtPath<PrototypeItemCatalog>(ItemCatalogPath),
            "MainMenu");
        PrototypeEncounterDirector encounterDirector = raidSystems.AddComponent<PrototypeEncounterDirector>();
        encounterDirector.Configure(player.transform, humanoidDefinition);
        CreateEnemySpawnPoint(
            raidSystems.transform,
            "SpawnPoint_Regular",
            new Vector3(1.6f, 0f, 6.4f),
            regularZombieProfile,
            new Vector3(1.6f, 0f, 6.4f),
            new Vector3(-0.2f, 0f, 5.4f),
            new Vector3(2.6f, 0f, 4.8f));
        CreateEnemySpawnPoint(
            raidSystems.transform,
            "SpawnPoint_Police",
            new Vector3(5.8f, 0f, 5f),
            policeZombieProfile,
            new Vector3(5.8f, 0f, 5f),
            new Vector3(6.4f, 0f, 1.6f),
            new Vector3(3.7f, 0f, 3.4f));
        CreateEnemySpawnPoint(
            raidSystems.transform,
            "SpawnPoint_Dog",
            new Vector3(-3.1f, 0f, -4.8f),
            zombieDogProfile,
            new Vector3(-3.1f, 0f, -4.8f),
            new Vector3(-0.8f, 0f, -5.8f),
            new Vector3(-4.8f, 0f, -2.7f));
        CreateEnemySpawnArea(
            raidSystems.transform,
            "SpawnArea_NorthWest",
            new Vector3(-5.2f, 0f, 5.1f),
            new Vector3(3.4f, 2f, 3.2f),
            1,
            2,
            soldierZombieProfile,
            regularZombieProfile);

        CreateRaidPickup(root.transform, "Pickup_Cash", PrimitiveType.Cube, new Vector3(0f, 0.98f, 3.5f), new Vector3(0.32f, 0.12f, 0.22f), lootMat, cashItem, 3);
        CreateRaidPickup(root.transform, "Pickup_Medkit", PrimitiveType.Cube, new Vector3(-5.25f, 1.57f, -1.45f), new Vector3(0.28f, 0.2f, 0.22f), medicalMat, medkitItem, 1);
        CreateRaidPickup(root.transform, "Pickup_Bandage", PrimitiveType.Cube, new Vector3(-4.85f, 1.57f, -1.85f), new Vector3(0.2f, 0.12f, 0.16f), medicalMat, bandageItem, 1);
        CreateRaidPickup(root.transform, "Pickup_Tourniquet", PrimitiveType.Cube, new Vector3(4.55f, 1.18f, 2.05f), new Vector3(0.14f, 0.18f, 0.14f), medicalMat, tourniquetItem, 1);
        CreateRaidPickup(root.transform, "Pickup_Splint", PrimitiveType.Cube, new Vector3(5.25f, 1.18f, 2.05f), new Vector3(0.16f, 0.22f, 0.12f), medicalMat, splintItem, 1);
        CreateRaidPickup(root.transform, "Pickup_Painkillers", PrimitiveType.Cube, new Vector3(0.35f, 0.96f, -6.4f), new Vector3(0.18f, 0.16f, 0.12f), medicalMat, painkillerItem, 1);
        CreateRaidPickup(root.transform, "Pickup_Cash_Desk", PrimitiveType.Sphere, new Vector3(0f, 0.96f, -6.4f), new Vector3(0.22f, 0.16f, 0.22f), lootMat, cashItem, 2);
        CreateRandomLootContainer(root.transform, "Crate_Center", "Weapon Crate", new Vector3(-1.4f, 1.0f, 3.55f), new Vector3(0.95f, 0.45f, 0.65f), propMat, ammoLootTable);
        CreateRandomLootContainer(root.transform, "Crate_Side", "Medical Case", new Vector3(4.9f, 1.18f, 2.4f), new Vector3(0.68f, 0.56f, 0.54f), medicalMat, medicalLootTable);
        CreateGroundLootSpawnPoint(root.transform, "LootSpawn_West", new Vector3(-5.6f, 0.08f, 5.4f), floorLootTable, lootMat);
        CreateGroundLootSpawnPoint(root.transform, "LootSpawn_East", new Vector3(5.4f, 0.08f, -4.7f), ammoLootTable, accentMat);

        CreateExtractionZone(root.transform, raidGameMode, new Vector3(6.2f, 0.95f, 7.1f), extractMat, accentMat);

        CreatePointLight(root.transform, new Vector3(6.25f, 2.4f, 7.1f), new Color(0.31f, 1f, 0.66f), 5f, 9f);

        Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[PrototypeIndoorSceneBuilder] Indoor FPS scene created successfully.");
    }

    // Default anatomy used by the raid sample scene builder.
    private static PrototypeUnitDefinition CreateOrUpdateHumanoidDefinition(string assetPath)
    {
        PrototypeUnitDefinition definition = AssetDatabase.LoadAssetAtPath<PrototypeUnitDefinition>(assetPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<PrototypeUnitDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);
        }

        definition.SetDefinition(
            new List<PrototypeUnitDefinition.PartDefinition>
            {
                CreateHumanoidPart(
                    HeadPartId,
                    "Head",
                    35f,
                    1f,
                    PrototypeUnitDefinition.ZeroKillMode.OnDirectHitOnly,
                    true,
                    0f,
                    true,
                    true),
                CreateHumanoidPart(
                    TorsoPartId,
                    "Torso",
                    155f,
                    1.05f,
                    PrototypeUnitDefinition.ZeroKillMode.Never,
                    false,
                    0f,
                    true,
                    false),
                CreateHumanoidPart(
                    LegsPartId,
                    "Legs",
                    130f,
                    0.7f,
                    PrototypeUnitDefinition.ZeroKillMode.Never,
                    false,
                    0f,
                    true,
                    false)
            },
            HeadPartId);

        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static PrototypeUnitDefinition.PartDefinition CreateHumanoidPart(
        string partId,
        string displayName,
        float maxHealth,
        float overflowMultiplier,
        PrototypeUnitDefinition.ZeroKillMode zeroKillMode,
        bool killUnitWhenBlackedAndDamagedAgain,
        float blackedFollowUpDamageThreshold,
        bool receivesOverflowDamage,
        bool receivesOverflowFollowUpDamage)
    {
        return new PrototypeUnitDefinition.PartDefinition
        {
            partId = partId,
            displayName = displayName,
            maxHealth = maxHealth,
            overflowMultiplier = overflowMultiplier,
            contributesToUnitHealth = true,
            receivesOverflowDamage = receivesOverflowDamage,
            receivesOverflowFollowUpDamage = receivesOverflowFollowUpDamage,
            zeroKillMode = zeroKillMode,
            killUnitWhenBlackedAndDamagedAgain = killUnitWhenBlackedAndDamagedAgain,
            blackedFollowUpDamageThreshold = blackedFollowUpDamageThreshold,
            overflowTargets = new List<PrototypeUnitDefinition.OverflowTarget>()
        };
    }

    private static ItemDefinition CreateOrUpdateItemDefinition(string assetPath, string itemId, string displayName, string description, int maxStackSize, float weight)
    {
        ItemDefinition definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<ItemDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);
        }

        definition.Configure(itemId, displayName, description, maxStackSize, weight);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static AmmoDefinition CreateOrUpdateAmmoDefinition(
        string assetPath,
        string itemId,
        string displayName,
        string description,
        int maxStackSize,
        float weight,
        float damage,
        float impactForce,
        float penetration,
        float armorDamage,
        float lightBleedChance,
        float heavyBleedChance,
        float fractureChance)
    {
        Object existingAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (existingAsset != null && !(existingAsset is AmmoDefinition))
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        AmmoDefinition definition = AssetDatabase.LoadAssetAtPath<AmmoDefinition>(assetPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<AmmoDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);
        }

        definition.ConfigureAmmo(
            itemId,
            displayName,
            description,
            maxStackSize,
            weight,
            damage,
            impactForce,
            penetration,
            armorDamage,
            lightBleedChance,
            heavyBleedChance,
            fractureChance);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static MedicalItemDefinition CreateOrUpdateMedicalDefinition(
        string assetPath,
        string itemId,
        string displayName,
        string description,
        int maxStackSize,
        float weight,
        float healAmount,
        int removesLightBleeds,
        int removesHeavyBleeds,
        int curesFractures,
        float painkillerDuration)
    {
        Object existingAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (existingAsset != null && !(existingAsset is MedicalItemDefinition))
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        MedicalItemDefinition definition = AssetDatabase.LoadAssetAtPath<MedicalItemDefinition>(assetPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<MedicalItemDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);
        }

        definition.ConfigureMedical(
            itemId,
            displayName,
            description,
            maxStackSize,
            weight,
            healAmount,
            removesLightBleeds,
            removesHeavyBleeds,
            curesFractures,
            painkillerDuration);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static ArmorDefinition CreateOrUpdateArmorDefinition(
        string assetPath,
        string itemId,
        string displayName,
        string description,
        float weight,
        int armorClass,
        float durability,
        float bluntDamageMultiplier,
        float blockedLossMultiplier,
        float penetratedLossMultiplier,
        float bleedProtection,
        float fractureProtection,
        params string[] coveredPartIds)
    {
        Object existingAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (existingAsset != null && !(existingAsset is ArmorDefinition))
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        ArmorDefinition definition = AssetDatabase.LoadAssetAtPath<ArmorDefinition>(assetPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<ArmorDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);
        }

        definition.ConfigureArmor(
            itemId,
            displayName,
            description,
            weight,
            armorClass,
            durability,
            bluntDamageMultiplier,
            blockedLossMultiplier,
            penetratedLossMultiplier,
            bleedProtection,
            fractureProtection,
            coveredPartIds);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static PrototypeWeaponDefinition CreateOrUpdateFirearmDefinition(
        string assetPath,
        string weaponId,
        string displayName,
        string description,
        AmmoDefinition ammoDefinition,
        int magazineSize,
        float roundsPerMinute,
        float reloadDuration,
        float range,
        float spread,
        float addedImpactForce,
        int burstCount,
        params PrototypeWeaponFireMode[] supportedModes)
    {
        PrototypeWeaponDefinition definition = AssetDatabase.LoadAssetAtPath<PrototypeWeaponDefinition>(assetPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<PrototypeWeaponDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);
        }

        definition.ConfigureFirearm(
            weaponId,
            displayName,
            description,
            ammoDefinition,
            magazineSize,
            roundsPerMinute,
            reloadDuration,
            range,
            spread,
            addedImpactForce,
            burstCount,
            supportedModes);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static PrototypeWeaponDefinition CreateOrUpdateMeleeDefinition(
        string assetPath,
        string weaponId,
        string displayName,
        string description,
        float damage,
        float range,
        float radius,
        float cooldown)
    {
        PrototypeWeaponDefinition definition = AssetDatabase.LoadAssetAtPath<PrototypeWeaponDefinition>(assetPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<PrototypeWeaponDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);
        }

        definition.ConfigureMelee(weaponId, displayName, description, damage, range, radius, cooldown);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static LootTableDefinition.LootEntry CreateLootEntry(ItemDefinition itemDefinition, int minQuantity, int maxQuantity, float weight)
    {
        return new LootTableDefinition.LootEntry
        {
            itemDefinition = itemDefinition,
            minQuantity = Mathf.Max(1, minQuantity),
            maxQuantity = Mathf.Max(Mathf.Max(1, minQuantity), maxQuantity),
            weight = Mathf.Max(0f, weight)
        };
    }

    private static LootTableDefinition CreateOrUpdateLootTable(
        string assetPath,
        string tableId,
        string displayName,
        int minRolls,
        int maxRolls,
        bool allowDuplicates,
        params LootTableDefinition.LootEntry[] entries)
    {
        LootTableDefinition table = AssetDatabase.LoadAssetAtPath<LootTableDefinition>(assetPath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<LootTableDefinition>();
            AssetDatabase.CreateAsset(table, assetPath);
        }

        table.Configure(tableId, displayName, minRolls, maxRolls, allowDuplicates, entries);
        EditorUtility.SetDirty(table);
        return table;
    }

    private static PrototypeEnemySpawnProfile CreateOrUpdateEnemySpawnProfile(
        string assetPath,
        string enemyId,
        string displayName,
        PrototypeEnemyArchetype archetype,
        PrototypeUnitDefinition unitDefinition,
        PrototypeWeaponDefinition primaryWeapon,
        Material bodyMaterial,
        Vector3 localScale,
        params ArmorDefinition[] armorLoadout)
    {
        PrototypeEnemySpawnProfile profile = AssetDatabase.LoadAssetAtPath<PrototypeEnemySpawnProfile>(assetPath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<PrototypeEnemySpawnProfile>();
            AssetDatabase.CreateAsset(profile, assetPath);
        }

        profile.Configure(enemyId, displayName, archetype, unitDefinition, primaryWeapon, bodyMaterial, localScale, armorLoadout);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void ConfigureDirectionalLight()
    {
        GameObject lightObject = GameObject.Find("Directional Light");
        if (lightObject == null)
        {
            lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
        }

        Light directionalLight = lightObject.GetComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 0.7f;
        directionalLight.color = new Color(0.95f, 0.96f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
    }

    private static void DeleteIfExists(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static void CreatePointLight(Transform parent, Vector3 localPosition, Color color, float intensity, float range)
    {
        GameObject pointLightObject = new GameObject($"PointLight_{localPosition.x}_{localPosition.z}");
        pointLightObject.transform.SetParent(parent, false);
        pointLightObject.transform.localPosition = localPosition;

        Light light = pointLightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = localScale;
        box.GetComponent<MeshRenderer>().sharedMaterial = material;
        return box;
    }

    private static void CreateCoverPoint(Transform parent, string name, Vector3 localPosition, Vector3 exposedDirection)
    {
        GameObject coverPoint = new GameObject(name);
        coverPoint.transform.SetParent(parent, false);
        coverPoint.transform.localPosition = localPosition;

        Vector3 flatDirection = Vector3.ProjectOnPlane(exposedDirection, Vector3.up).normalized;
        if (flatDirection.sqrMagnitude > 0.001f)
        {
            coverPoint.transform.localRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
        }
    }

    private static void CreateWeaponPiece(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = name;
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = localPosition;
        piece.transform.localScale = localScale;
        piece.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(piece.GetComponent<Collider>());
    }

    private static void CreatePrimaryWeaponViewModel(Transform parent, Material bodyMaterial, Material barrelMaterial, Material gripMaterial)
    {
        GameObject root = new GameObject("WeaponView_Primary");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, 0f, 0f);
        root.transform.localRotation = Quaternion.identity;

        CreateWeaponPiece("Body", root.transform, new Vector3(0.25f, -0.22f, 0.55f), new Vector3(0.22f, 0.14f, 0.65f), bodyMaterial);
        CreateWeaponPiece("Barrel", root.transform, new Vector3(0.25f, -0.18f, 0.94f), new Vector3(0.08f, 0.08f, 0.34f), barrelMaterial);
        CreateWeaponPiece("Grip", root.transform, new Vector3(0.19f, -0.34f, 0.5f), new Vector3(0.08f, 0.2f, 0.12f), gripMaterial);
        CreateWeaponPiece("Magazine", root.transform, new Vector3(0.24f, -0.34f, 0.62f), new Vector3(0.08f, 0.2f, 0.14f), gripMaterial);
    }

    private static void CreateSecondaryWeaponViewModel(Transform parent, Material bodyMaterial, Material barrelMaterial, Material gripMaterial)
    {
        GameObject root = new GameObject("WeaponView_Secondary");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(-0.02f, 0.02f, -0.04f);
        root.transform.localRotation = Quaternion.identity;

        CreateWeaponPiece("Body", root.transform, new Vector3(0.18f, -0.2f, 0.46f), new Vector3(0.16f, 0.11f, 0.32f), bodyMaterial);
        CreateWeaponPiece("Slide", root.transform, new Vector3(0.18f, -0.15f, 0.54f), new Vector3(0.12f, 0.06f, 0.28f), barrelMaterial);
        CreateWeaponPiece("Barrel", root.transform, new Vector3(0.18f, -0.15f, 0.73f), new Vector3(0.05f, 0.05f, 0.14f), barrelMaterial);
        CreateWeaponPiece("Grip", root.transform, new Vector3(0.13f, -0.31f, 0.43f), new Vector3(0.08f, 0.18f, 0.1f), gripMaterial);
    }

    private static void CreateMeleeWeaponViewModel(Transform parent, Material bladeMaterial, Material handleMaterial)
    {
        GameObject root = new GameObject("WeaponView_Melee");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, 0f, 0f);
        root.transform.localRotation = Quaternion.identity;

        CreateWeaponPiece("Handle", root.transform, new Vector3(0.16f, -0.28f, 0.42f), new Vector3(0.06f, 0.18f, 0.06f), handleMaterial);
        CreateWeaponPiece("Guard", root.transform, new Vector3(0.16f, -0.18f, 0.48f), new Vector3(0.12f, 0.02f, 0.04f), handleMaterial);
        CreateWeaponPiece("Blade", root.transform, new Vector3(0.16f, -0.14f, 0.7f), new Vector3(0.03f, 0.03f, 0.36f), bladeMaterial);
        root.SetActive(false);
    }

    private static void CreateTarget(
        Transform parent,
        Vector3 localPosition,
        Material material,
        PrototypeUnitDefinition unitDefinition,
        params ArmorDefinition[] armorLoadout)
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        target.name = $"Target_{localPosition.x}_{localPosition.z}";
        target.transform.SetParent(parent, false);
        target.transform.localPosition = localPosition;
        target.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);
        target.layer = IgnoreRaycastLayer;
        target.GetComponent<MeshRenderer>().sharedMaterial = material;

        Rigidbody rigidbody = target.AddComponent<Rigidbody>();
        rigidbody.mass = 1.5f;
        rigidbody.angularDamping = 1.8f;

        PrototypeUnitVitals vitals = target.AddComponent<PrototypeUnitVitals>();
        vitals.SetUnitDefinition(unitDefinition);
        PrototypeStatusEffectController statusEffects = GetOrAddComponent<PrototypeStatusEffectController>(target);
        statusEffects.Bind(vitals);
        GetOrAddComponent<PrototypeCombatTextController>(target);
        vitals.SetArmorLoadout(armorLoadout);
        vitals.SetAllowImpactForceWhenAlive(false);
        CreateTargetHitboxes(target.transform, vitals);

        PrototypeTargetHealthBar targetHealthBar = target.AddComponent<PrototypeTargetHealthBar>();
        targetHealthBar.Configure(vitals, null, unitDefinition != null ? unitDefinition.HealthBarAnchorPartId : HeadPartId);
    }

    private static void CreateBot(
        Transform parent,
        string botName,
        PrototypeEnemyArchetype archetype,
        Vector3 localScale,
        Vector3 localPosition,
        Material material,
        PrototypeUnitDefinition unitDefinition,
        Transform combatTarget,
        PrototypeWeaponDefinition weaponDefinition,
        Vector3[] patrolPoints,
        params ArmorDefinition[] armorLoadout)
    {
        GameObject bot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bot.name = botName;
        bot.transform.SetParent(parent, false);
        bot.transform.localPosition = localPosition;
        bot.transform.localScale = localScale;
        bot.layer = IgnoreRaycastLayer;
        bot.GetComponent<MeshRenderer>().sharedMaterial = material;

        Rigidbody rigidbody = bot.AddComponent<Rigidbody>();
        rigidbody.mass = 1.6f;
        rigidbody.angularDamping = 1.8f;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        PrototypeUnitVitals vitals = bot.AddComponent<PrototypeUnitVitals>();
        vitals.SetUnitDefinition(unitDefinition);
        PrototypeStatusEffectController statusEffects = GetOrAddComponent<PrototypeStatusEffectController>(bot);
        statusEffects.Bind(vitals);
        GetOrAddComponent<PrototypeCombatTextController>(bot);
        vitals.SetArmorLoadout(armorLoadout);
        vitals.SetAllowImpactForceWhenAlive(false);
        CreateTargetHitboxes(bot.transform, vitals);

        PrototypeTargetHealthBar targetHealthBar = bot.AddComponent<PrototypeTargetHealthBar>();
        targetHealthBar.Configure(vitals, null, unitDefinition != null ? unitDefinition.HealthBarAnchorPartId : HeadPartId);

        PrototypeBotController botController = bot.AddComponent<PrototypeBotController>();
        botController.Configure(combatTarget, archetype, weaponDefinition, patrolPoints);
        SetSerializedReference(botController, "combatTarget", combatTarget);
        SetSerializedReference(botController, "primaryWeapon", weaponDefinition);
        SetSerializedInt(botController, "archetype", (int)archetype);
    }

    private static void CreateRaidPickup(
        Transform parent,
        string name,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        ItemDefinition itemDefinition,
        int quantity)
    {
        GameObject pickupObject = GameObject.CreatePrimitive(primitiveType);
        pickupObject.name = name;
        pickupObject.transform.SetParent(parent, false);
        pickupObject.transform.localPosition = localPosition;
        pickupObject.transform.localScale = localScale;
        pickupObject.GetComponent<MeshRenderer>().sharedMaterial = material;

        GroundLootItem groundLoot = pickupObject.AddComponent<GroundLootItem>();
        groundLoot.Configure(itemDefinition, quantity);
    }

    private static void CreateLootContainer(
        Transform parent,
        string objectName,
        string containerLabel,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        ItemDefinition primaryItem,
        int primaryQuantity,
        ItemDefinition secondaryItem,
        int secondaryQuantity)
    {
        GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crate.name = objectName;
        crate.transform.SetParent(parent, false);
        crate.transform.localPosition = localPosition;
        crate.transform.localScale = localScale;
        crate.GetComponent<MeshRenderer>().sharedMaterial = material;

        InventoryContainer inventory = crate.AddComponent<InventoryContainer>();
        inventory.Configure(containerLabel, 6, 0f);
        inventory.TryAddItem(primaryItem, primaryQuantity, out _);
        inventory.TryAddItem(secondaryItem, secondaryQuantity, out _);

        LootContainer lootContainer = crate.AddComponent<LootContainer>();
        lootContainer.Configure(containerLabel, inventory, "Search");

        GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lid.name = $"{objectName}_Lid";
        lid.transform.SetParent(crate.transform, false);
        lid.transform.localPosition = new Vector3(0f, localScale.y * 0.58f, -localScale.z * 0.1f);
        lid.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
        lid.transform.localScale = new Vector3(0.92f, 0.18f, 0.88f);
        lid.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(lid.GetComponent<Collider>());
    }

    private static void CreateRandomLootContainer(
        Transform parent,
        string objectName,
        string containerLabel,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        LootTableDefinition lootTable)
    {
        GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crate.name = objectName;
        crate.transform.SetParent(parent, false);
        crate.transform.localPosition = localPosition;
        crate.transform.localScale = localScale;
        crate.GetComponent<MeshRenderer>().sharedMaterial = material;

        InventoryContainer inventory = crate.AddComponent<InventoryContainer>();
        inventory.Configure(containerLabel, 8, 0f);

        LootContainer lootContainer = crate.AddComponent<LootContainer>();
        lootContainer.Configure(containerLabel, inventory, "Search");
        lootContainer.ConfigureRandomLoot(lootTable, true, true);

        GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lid.name = $"{objectName}_Lid";
        lid.transform.SetParent(crate.transform, false);
        lid.transform.localPosition = new Vector3(0f, localScale.y * 0.58f, -localScale.z * 0.1f);
        lid.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
        lid.transform.localScale = new Vector3(0.92f, 0.18f, 0.88f);
        lid.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(lid.GetComponent<Collider>());
    }

    private static void CreateGroundLootSpawnPoint(Transform parent, string objectName, Vector3 localPosition, LootTableDefinition lootTable, Material markerMaterial)
    {
        GameObject spawnPoint = new GameObject(objectName);
        spawnPoint.transform.SetParent(parent, false);
        spawnPoint.transform.localPosition = localPosition;

        GroundLootSpawnPoint lootSpawnPoint = spawnPoint.AddComponent<GroundLootSpawnPoint>();
        lootSpawnPoint.Configure(lootTable, 0.65f);

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "Marker";
        marker.transform.SetParent(spawnPoint.transform, false);
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = Vector3.one * 0.22f;
        marker.GetComponent<MeshRenderer>().sharedMaterial = markerMaterial;
        Object.DestroyImmediate(marker.GetComponent<Collider>());
    }

    private static void CreateDoor(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        float doorWidth,
        float doorHeight,
        float doorThickness,
        Material material)
    {
        GameObject doorRoot = new GameObject(objectName);
        doorRoot.transform.SetParent(parent, false);
        doorRoot.transform.localPosition = localPosition;

        GameObject hinge = new GameObject("Hinge");
        hinge.transform.SetParent(doorRoot.transform, false);
        hinge.transform.localPosition = new Vector3(-doorWidth * 0.5f, 0f, 0f);

        GameObject doorLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorLeaf.name = "DoorLeaf";
        doorLeaf.transform.SetParent(hinge.transform, false);
        doorLeaf.transform.localPosition = new Vector3(doorWidth * 0.5f, doorHeight * 0.5f, 0f);
        doorLeaf.transform.localScale = new Vector3(doorWidth, doorHeight, doorThickness);
        doorLeaf.GetComponent<MeshRenderer>().sharedMaterial = material;

        PrototypeDoor door = doorRoot.AddComponent<PrototypeDoor>();
        door.Configure(hinge.transform, doorLeaf.GetComponent<Collider>(), 96f, false);
    }

    private static void CreateBreakablePanel(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        PrototypeBreakableMaterialType breakableType,
        float durability)
    {
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = objectName;
        panel.transform.SetParent(parent, false);
        panel.transform.localPosition = localPosition;
        panel.transform.localScale = localScale;
        panel.GetComponent<MeshRenderer>().sharedMaterial = material;

        PrototypeBreakable breakable = panel.AddComponent<PrototypeBreakable>();
        breakable.Configure(breakableType, durability);
    }

    private static void CreateEnemySpawnPoint(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        PrototypeEnemySpawnProfile profile,
        params Vector3[] patrolPoints)
    {
        GameObject spawnPointObject = new GameObject(objectName);
        spawnPointObject.transform.SetParent(parent, false);
        spawnPointObject.transform.localPosition = localPosition;

        var waypointTransforms = new List<Transform>();
        if (patrolPoints != null)
        {
            for (int index = 0; index < patrolPoints.Length; index++)
            {
                GameObject waypoint = new GameObject($"Patrol_{index + 1}");
                waypoint.transform.SetParent(spawnPointObject.transform, false);
                waypoint.transform.localPosition = patrolPoints[index] - localPosition;
                waypointTransforms.Add(waypoint.transform);
            }
        }

        PrototypeEnemySpawnPoint spawnPoint = spawnPointObject.AddComponent<PrototypeEnemySpawnPoint>();
        spawnPoint.Configure(profile, waypointTransforms, true);
    }

    private static void CreateEnemySpawnArea(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Vector3 areaSize,
        int minCount,
        int maxCount,
        params PrototypeEnemySpawnProfile[] profiles)
    {
        GameObject spawnAreaObject = new GameObject(objectName);
        spawnAreaObject.transform.SetParent(parent, false);
        spawnAreaObject.transform.localPosition = localPosition;

        PrototypeEnemySpawnArea spawnArea = spawnAreaObject.AddComponent<PrototypeEnemySpawnArea>();
        spawnArea.Configure(profiles, minCount, maxCount, areaSize, 3f, 3);
    }

    private static void CreateExtractionZone(Transform parent, RaidGameMode raidGameMode, Vector3 localPosition, Material extractMaterial, Material accentMaterial)
    {
        GameObject zoneRoot = new GameObject("Extraction_DockAlpha");
        zoneRoot.transform.SetParent(parent, false);
        zoneRoot.transform.localPosition = localPosition;

        BoxCollider trigger = zoneRoot.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(2.8f, 2.2f, 2.6f);
        trigger.center = new Vector3(0f, 0.5f, 0f);

        ExtractionZone extractionZone = zoneRoot.AddComponent<ExtractionZone>();
        extractionZone.Configure(raidGameMode, "Dock Alpha", "Extract", true);

        GameObject floorBeacon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floorBeacon.name = "Extract_Beacon";
        floorBeacon.transform.SetParent(zoneRoot.transform, false);
        floorBeacon.transform.localPosition = new Vector3(0f, -0.36f, 0f);
        floorBeacon.transform.localScale = new Vector3(2.4f, 0.05f, 2.2f);
        floorBeacon.GetComponent<MeshRenderer>().sharedMaterial = extractMaterial;
        Object.DestroyImmediate(floorBeacon.GetComponent<Collider>());

        GameObject console = GameObject.CreatePrimitive(PrimitiveType.Cube);
        console.name = "Extract_Console";
        console.transform.SetParent(zoneRoot.transform, false);
        console.transform.localPosition = new Vector3(0f, 0.35f, -0.9f);
        console.transform.localScale = new Vector3(0.6f, 1.2f, 0.28f);
        console.GetComponent<MeshRenderer>().sharedMaterial = accentMaterial;

        GameObject lightBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lightBar.name = "Extract_LightBar";
        lightBar.transform.SetParent(zoneRoot.transform, false);
        lightBar.transform.localPosition = new Vector3(0f, 1.08f, -0.72f);
        lightBar.transform.localScale = new Vector3(1.2f, 0.12f, 0.12f);
        lightBar.GetComponent<MeshRenderer>().sharedMaterial = extractMaterial;
        Object.DestroyImmediate(lightBar.GetComponent<Collider>());
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

    private static void SetSerializedInt(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.intValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void CreatePlayerHitboxes(Transform parent, PrototypeUnitVitals vitals)
    {
        CreateBodyHitbox(parent, vitals, "Hitbox_Head", HeadPartId, new Vector3(0f, 1.58f, 0.06f), new Vector3(0.32f, 0.34f, 0.32f));
        CreateBodyHitbox(parent, vitals, "Hitbox_Torso", TorsoPartId, new Vector3(0f, 1.02f, 0.05f), new Vector3(0.56f, 0.7f, 0.42f));
        CreateBodyHitbox(parent, vitals, "Hitbox_Legs", LegsPartId, new Vector3(0f, 0.46f, 0.03f), new Vector3(0.48f, 0.9f, 0.38f));
    }

    private static void CreateTargetHitboxes(Transform parent, PrototypeUnitVitals vitals)
    {
        CreateBodyHitbox(parent, vitals, "Hitbox_Head", HeadPartId, new Vector3(0f, 0.82f, 0f), new Vector3(0.42f, 0.42f, 0.42f));
        CreateBodyHitbox(parent, vitals, "Hitbox_Torso", TorsoPartId, new Vector3(0f, 0.08f, 0f), new Vector3(0.72f, 0.86f, 0.52f));
        CreateBodyHitbox(parent, vitals, "Hitbox_Legs", LegsPartId, new Vector3(0f, -0.72f, 0f), new Vector3(0.64f, 0.88f, 0.48f));
    }

    private static void CreateBodyHitbox(
        Transform parent,
        PrototypeUnitVitals vitals,
        string name,
        string partId,
        Vector3 localPosition,
        Vector3 colliderSize,
        string passthroughPartId = "")
    {
        GameObject hitbox = new GameObject(name);
        hitbox.transform.SetParent(parent, false);
        hitbox.transform.localPosition = localPosition;
        hitbox.transform.localRotation = Quaternion.identity;
        hitbox.layer = 0;

        BoxCollider collider = hitbox.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = colliderSize;

        PrototypeUnitHitbox unitHitbox = hitbox.AddComponent<PrototypeUnitHitbox>();
        unitHitbox.Configure(vitals, partId, passthroughPartId);
    }
}
