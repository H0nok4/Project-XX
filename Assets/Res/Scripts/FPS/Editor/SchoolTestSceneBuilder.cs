using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
/// <summary>
/// 使用 ProBuilder 重建 SchoolTestScene 校园白盒。
/// </summary>
public static class SchoolTestSceneBuilder
{
    private const string TriggerAssetPath = "Assets/Res/SchoolTestScene.trigger.txt";
    private const string AutoBuildSessionKey = "SchoolTestSceneBuilder.AutoBuildRunning";
    private const string ScenePath = "Assets/Scenes/SchoolTestScene.unity";
    private const string MaterialFolder = "Assets/Res/Materials/SchoolCampus";
    private const string RegularZombieProfilePath = "Assets/Res/Data/PrototypeFPS/EnemyProfiles/Spawn_RegularZombie.asset";
    private const string PoliceZombieProfilePath = "Assets/Res/Data/PrototypeFPS/EnemyProfiles/Spawn_PoliceZombie.asset";
    private const string SoldierZombieProfilePath = "Assets/Res/Data/PrototypeFPS/EnemyProfiles/Spawn_SoldierZombie.asset";
    private const string ZombieDogProfilePath = "Assets/Res/Data/PrototypeFPS/EnemyProfiles/Spawn_ZombieDog.asset";

    static SchoolTestSceneBuilder()
    {
        EditorApplication.delayCall += TryBuildFromTrigger;
    }

    [MenuItem("Tools/Prototype/Build School Test Scene")]
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

        if (SessionState.GetBool(AutoBuildSessionKey, false))
        {
            return;
        }

        if (!AssetDatabase.LoadAssetAtPath<TextAsset>(TriggerAssetPath))
        {
            return;
        }

        SessionState.SetBool(AutoBuildSessionKey, true);
        AssetDatabase.DeleteAsset(TriggerAssetPath);

        try
        {
            BuildScene();
        }
        finally
        {
            SessionState.EraseBool(AutoBuildSessionKey);
        }
    }

    private static void BuildScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        EnsureFolder("Assets/Res/Materials");
        EnsureFolder(MaterialFolder);

        DeleteGeneratedRoots(scene);
        ConfigureDirectionalLight();

        Material grassMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Grass.mat", new Color(0.27f, 0.48f, 0.24f, 1f));
        Material asphaltMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Asphalt.mat", new Color(0.19f, 0.2f, 0.23f, 1f));
        Material concreteMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Concrete.mat", new Color(0.63f, 0.66f, 0.7f, 1f));
        Material wallMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_SchoolWall.mat", new Color(0.76f, 0.78f, 0.81f, 1f));
        Material roofMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Roof.mat", new Color(0.35f, 0.23f, 0.2f, 1f));
        Material glassMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Glass.mat", new Color(0.45f, 0.72f, 0.88f, 1f));
        Material academicAccentMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_AcademicAccent.mat", new Color(0.22f, 0.46f, 0.78f, 1f));
        Material dormAccentMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_DormAccent.mat", new Color(0.46f, 0.71f, 0.34f, 1f));
        Material cafeteriaAccentMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_CafeteriaAccent.mat", new Color(0.86f, 0.56f, 0.22f, 1f));
        Material trackMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Track.mat", new Color(0.62f, 0.22f, 0.18f, 1f));
        Material fieldMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Field.mat", new Color(0.16f, 0.52f, 0.22f, 1f));
        Material bleacherMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Bleacher.mat", new Color(0.68f, 0.67f, 0.63f, 1f));
        Material lightMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Light.mat", new Color(0.95f, 0.83f, 0.48f, 1f));
        Material fenceMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Fence.mat", new Color(0.32f, 0.35f, 0.39f, 1f));
        Material extractMaterial = CreateOrUpdateMaterial($"{MaterialFolder}/Mat_Extract.mat", new Color(0.18f, 0.82f, 0.62f, 1f));

        GameObject root = new GameObject("PB_SchoolCampus");
        Transform groundRoot = CreateGroup(root.transform, "00_Ground");
        Transform roadRoot = CreateGroup(root.transform, "10_RoadsAndPlaza");
        Transform academicRoot = CreateGroup(root.transform, "20_TeachingBlock");
        Transform dormRoot = CreateGroup(root.transform, "30_Dormitory");
        Transform cafeteriaRoot = CreateGroup(root.transform, "40_Cafeteria");
        Transform playgroundRoot = CreateGroup(root.transform, "50_Playground");
        Transform scenicRoot = CreateGroup(root.transform, "60_ScenicProps");
        Transform gameplayRoot = CreateGroup(root.transform, "70_Gameplay");
        Transform lightingRoot = CreateGroup(root.transform, "80_Lighting");
        CreateGroup(root.transform, "90_Debug");

        CreateCampusGround(groundRoot, roadRoot, fenceMaterial, grassMaterial, asphaltMaterial, concreteMaterial, academicAccentMaterial);
        CreateTeachingBlock(academicRoot, scenicRoot, wallMaterial, roofMaterial, concreteMaterial, glassMaterial, academicAccentMaterial);
        CreateDormitoryZone(dormRoot, scenicRoot, wallMaterial, roofMaterial, concreteMaterial, glassMaterial, dormAccentMaterial);
        CreateCafeteriaZone(cafeteriaRoot, scenicRoot, wallMaterial, roofMaterial, concreteMaterial, glassMaterial, cafeteriaAccentMaterial);
        CreatePlayground(playgroundRoot, trackMaterial, fieldMaterial, bleacherMaterial, fenceMaterial, lightMaterial);
        CreateScenicProps(scenicRoot, concreteMaterial, lightMaterial, academicAccentMaterial, dormAccentMaterial, cafeteriaAccentMaterial);
        CreateGameplayMarkers(gameplayRoot, extractMaterial);
        CreateCampusLighting(lightingRoot, lightMaterial);
        RepositionPlayer();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SchoolTestSceneBuilder] SchoolTestScene 已使用 ProBuilder 白盒方式重建。");
    }

    private static void CreateCampusGround(
        Transform groundRoot,
        Transform roadRoot,
        Material fenceMaterial,
        Material grassMaterial,
        Material asphaltMaterial,
        Material concreteMaterial,
        Material accentMaterial)
    {
        CreatePbCube("PB_CampusGround", groundRoot, new Vector3(0f, -0.3f, 0f), new Vector3(240f, 0.6f, 190f), grassMaterial);
        CreatePbCube("PB_MainRoad", roadRoot, new Vector3(0f, 0.02f, -34f), new Vector3(16f, 0.04f, 92f), asphaltMaterial);
        CreatePbCube("PB_CentralPlaza", roadRoot, new Vector3(0f, 0.03f, 2f), new Vector3(46f, 0.05f, 30f), concreteMaterial, false);
        CreatePbCube("PB_EastRoad", roadRoot, new Vector3(46f, 0.02f, -8f), new Vector3(76f, 0.04f, 10f), asphaltMaterial);
        CreatePbCube("PB_WestRoad", roadRoot, new Vector3(-46f, 0.02f, -8f), new Vector3(76f, 0.04f, 10f), asphaltMaterial);
        CreatePbCube("PB_SportsRoad", roadRoot, new Vector3(32f, 0.02f, -56f), new Vector3(86f, 0.04f, 12f), asphaltMaterial);
        CreatePbCube("PB_ServiceLane", roadRoot, new Vector3(-62f, 0.02f, 18f), new Vector3(18f, 0.04f, 34f), asphaltMaterial);
        CreatePbCube("PB_DormPlaza", roadRoot, new Vector3(70f, 0.03f, 28f), new Vector3(26f, 0.05f, 42f), concreteMaterial, false);
        CreatePbCube("PB_CafeteriaPatio", roadRoot, new Vector3(-55f, 0.03f, -7f), new Vector3(22f, 0.05f, 14f), concreteMaterial, false);

        CreatePerimeterFence(groundRoot, fenceMaterial);

        Transform gateRoot = CreateGroup(groundRoot, "PB_MainGate");
        CreatePbCube("PB_GatePillar_Left", gateRoot, new Vector3(-7f, 2.2f, -88f), new Vector3(1.4f, 4.4f, 1.4f), concreteMaterial);
        CreatePbCube("PB_GatePillar_Right", gateRoot, new Vector3(7f, 2.2f, -88f), new Vector3(1.4f, 4.4f, 1.4f), concreteMaterial);
        CreatePbCube("PB_GateBeam", gateRoot, new Vector3(0f, 4.8f, -88f), new Vector3(17f, 1f, 1.1f), accentMaterial);
        CreatePbCube("PB_GateGuardBooth", gateRoot, new Vector3(13.5f, 1.3f, -83.5f), new Vector3(4.2f, 2.6f, 3.8f), concreteMaterial);
        CreateSignBoard(gateRoot, "SIGN_CampusName", "第一实验学校", "教学楼 / 宿舍 / 食堂 / 操场", new Vector3(0f, 6.3f, -88f), new Vector3(13f, 2f, 0.4f), accentMaterial, Color.white);
    }

    private static void CreateTeachingBlock(
        Transform academicRoot,
        Transform scenicRoot,
        Material wallMaterial,
        Material roofMaterial,
        Material floorMaterial,
        Material glassMaterial,
        Material accentMaterial)
    {
        Transform buildingRoot = CreateGroup(academicRoot, "PB_TeachingBuilding");

        Transform southWing = CreateOpenShell(buildingRoot, "PB_Academic_SouthWing", new Vector3(0f, 0f, 18f), Quaternion.identity, new Vector2(42f, 18f), 10.5f, 10f, wallMaterial, floorMaterial, roofMaterial);
        Transform northWing = CreateOpenShell(buildingRoot, "PB_Academic_NorthWing", new Vector3(0f, 0f, 48f), Quaternion.Euler(0f, 180f, 0f), new Vector2(54f, 14f), 10.5f, 8f, wallMaterial, floorMaterial, roofMaterial);
        Transform westConnector = CreateOpenShell(buildingRoot, "PB_Academic_WestConnector", new Vector3(-22f, 0f, 33f), Quaternion.Euler(0f, 90f, 0f), new Vector2(12f, 18f), 8.5f, 5f, wallMaterial, floorMaterial, roofMaterial);
        Transform eastConnector = CreateOpenShell(buildingRoot, "PB_Academic_EastConnector", new Vector3(22f, 0f, 33f), Quaternion.Euler(0f, -90f, 0f), new Vector2(12f, 18f), 8.5f, 5f, wallMaterial, floorMaterial, roofMaterial);

        CreatePbCube("PB_Academic_Courtyard", buildingRoot, new Vector3(0f, 0.03f, 33f), new Vector3(24f, 0.05f, 16f), accentMaterial, false);
        CreateStepStack(buildingRoot, "PB_Academic_EntranceSteps", new Vector3(0f, 0f, 7.6f), new Vector3(10f, 1.2f, 5.6f), 4, floorMaterial, -1f);
        CreatePbCube("PB_Academic_Portico", buildingRoot, new Vector3(0f, 4.4f, 9.5f), new Vector3(12f, 0.6f, 4f), accentMaterial);
        CreatePbCube("PB_Academic_PorticoColumn_L", buildingRoot, new Vector3(-4.4f, 2.2f, 9.5f), new Vector3(0.9f, 4.4f, 0.9f), accentMaterial);
        CreatePbCube("PB_Academic_PorticoColumn_R", buildingRoot, new Vector3(4.4f, 2.2f, 9.5f), new Vector3(0.9f, 4.4f, 0.9f), accentMaterial);

        CreateWindowBands(southWing, "PB_AcademicSouth", new Vector2(42f, 18f), 10f, 1.2f, glassMaterial);
        CreateWindowBands(northWing, "PB_AcademicNorth", new Vector2(54f, 14f), 10f, 1.2f, glassMaterial);
        CreateWindowBands(westConnector, "PB_AcademicWest", new Vector2(12f, 18f), 8f, 1f, glassMaterial);
        CreateWindowBands(eastConnector, "PB_AcademicEast", new Vector2(12f, 18f), 8f, 1f, glassMaterial);

        CreateClassroomProps(buildingRoot, floorMaterial, accentMaterial);
        CreateSignBoard(buildingRoot, "SIGN_Teaching", "教学楼", "教室 / 实验室 / 办公区", new Vector3(0f, 8.8f, 8.8f), new Vector3(10f, 1.6f, 0.3f), accentMaterial, Color.white);

        CreateBench(scenicRoot, "PB_AcademicBench_A", new Vector3(-10f, 0.35f, 4f), floorMaterial);
        CreateBench(scenicRoot, "PB_AcademicBench_B", new Vector3(10f, 0.35f, 4f), floorMaterial);
        CreatePlanter(scenicRoot, "PB_CourtyardPlanter_A", new Vector3(-6.5f, 0.45f, 33f), new Vector3(4f, 0.9f, 2.8f), accentMaterial, floorMaterial);
        CreatePlanter(scenicRoot, "PB_CourtyardPlanter_B", new Vector3(6.5f, 0.45f, 33f), new Vector3(4f, 0.9f, 2.8f), accentMaterial, floorMaterial);
    }

    private static void CreateDormitoryZone(
        Transform dormRoot,
        Transform scenicRoot,
        Material wallMaterial,
        Material roofMaterial,
        Material floorMaterial,
        Material glassMaterial,
        Material accentMaterial)
    {
        Transform zoneRoot = CreateGroup(dormRoot, "PB_DormitoryZone");

        Transform dormA = CreateOpenShell(zoneRoot, "PB_Dorm_A", new Vector3(68f, 0f, 16f), Quaternion.Euler(0f, -90f, 0f), new Vector2(28f, 12f), 9.2f, 6f, wallMaterial, floorMaterial, roofMaterial);
        Transform dormB = CreateOpenShell(zoneRoot, "PB_Dorm_B", new Vector3(68f, 0f, 40f), Quaternion.Euler(0f, -90f, 0f), new Vector2(28f, 12f), 9.2f, 6f, wallMaterial, floorMaterial, roofMaterial);
        Transform utility = CreateOpenShell(zoneRoot, "PB_Dorm_Utility", new Vector3(84f, 0f, 28f), Quaternion.Euler(0f, -90f, 0f), new Vector2(12f, 10f), 5.4f, 3.5f, accentMaterial, floorMaterial, roofMaterial);

        CreateWindowBands(dormA, "PB_DormA", new Vector2(28f, 12f), 8.6f, 1.05f, glassMaterial);
        CreateWindowBands(dormB, "PB_DormB", new Vector2(28f, 12f), 8.6f, 1.05f, glassMaterial);
        CreateWindowBands(utility, "PB_Utility", new Vector2(12f, 10f), 5.2f, 0.9f, glassMaterial);

        CreatePbCube("PB_Dorm_Courtyard", zoneRoot, new Vector3(70f, 0.03f, 28f), new Vector3(20f, 0.05f, 38f), accentMaterial, false);
        CreateStepStack(zoneRoot, "PB_Dorm_A_Steps", new Vector3(54f, 0f, 16f), new Vector3(6.6f, 0.8f, 3.8f), 3, floorMaterial, 1f);
        CreateStepStack(zoneRoot, "PB_Dorm_B_Steps", new Vector3(54f, 0f, 40f), new Vector3(6.6f, 0.8f, 3.8f), 3, floorMaterial, 1f);

        CreateBench(scenicRoot, "PB_DormBench_A", new Vector3(69f, 0.35f, 23f), floorMaterial);
        CreateBench(scenicRoot, "PB_DormBench_B", new Vector3(69f, 0.35f, 33f), floorMaterial);
        CreatePlanter(scenicRoot, "PB_DormPlanter_A", new Vector3(62f, 0.45f, 28f), new Vector3(3.4f, 0.9f, 2.4f), accentMaterial, floorMaterial);
        CreatePlanter(scenicRoot, "PB_DormPlanter_B", new Vector3(77f, 0.45f, 28f), new Vector3(3.4f, 0.9f, 2.4f), accentMaterial, floorMaterial);
        CreateSignBoard(zoneRoot, "SIGN_Dormitory", "宿舍区", "寝室 / 值班室 / 洗衣间", new Vector3(54.5f, 5.8f, 28f), new Vector3(7.5f, 1.6f, 0.3f), accentMaterial, Color.white);
    }

    private static void CreateCafeteriaZone(
        Transform cafeteriaRoot,
        Transform scenicRoot,
        Material wallMaterial,
        Material roofMaterial,
        Material floorMaterial,
        Material glassMaterial,
        Material accentMaterial)
    {
        Transform zoneRoot = CreateGroup(cafeteriaRoot, "PB_CafeteriaZone");

        Transform diningHall = CreateOpenShell(zoneRoot, "PB_Cafeteria_MainHall", new Vector3(-56f, 0f, 10f), Quaternion.identity, new Vector2(30f, 18f), 7.8f, 10f, wallMaterial, floorMaterial, roofMaterial);
        Transform kitchen = CreateOpenShell(zoneRoot, "PB_Cafeteria_Kitchen", new Vector3(-72f, 0f, 18f), Quaternion.Euler(0f, 90f, 0f), new Vector2(12f, 10f), 6.2f, 4f, accentMaterial, floorMaterial, roofMaterial);

        CreateWindowBands(diningHall, "PB_CafeteriaHall", new Vector2(30f, 18f), 7.4f, 1.1f, glassMaterial);
        CreateWindowBands(kitchen, "PB_CafeteriaKitchen", new Vector2(12f, 10f), 5.8f, 0.8f, glassMaterial);

        CreatePbCube("PB_Cafeteria_ServingCounter", zoneRoot, new Vector3(-56f, 1f, 15.5f), new Vector3(14f, 2f, 1.6f), accentMaterial);
        CreatePbCube("PB_Cafeteria_DiningZone", zoneRoot, new Vector3(-56f, 0.03f, 2f), new Vector3(24f, 0.05f, 10f), accentMaterial, false);
        CreateBench(scenicRoot, "PB_CafeBench_A", new Vector3(-61f, 0.35f, -6f), floorMaterial);
        CreateBench(scenicRoot, "PB_CafeBench_B", new Vector3(-49f, 0.35f, -6f), floorMaterial);
        CreatePlanter(scenicRoot, "PB_CafePlanter_A", new Vector3(-68f, 0.45f, -6f), new Vector3(3.2f, 0.9f, 2.2f), accentMaterial, floorMaterial);
        CreateSignBoard(zoneRoot, "SIGN_Cafeteria", "食堂", "用餐区 / 后厨 / 配送口", new Vector3(-56f, 6.4f, -0.4f), new Vector3(8f, 1.6f, 0.3f), accentMaterial, Color.white);
    }

    private static void CreatePlayground(
        Transform playgroundRoot,
        Material trackMaterial,
        Material fieldMaterial,
        Material bleacherMaterial,
        Material fenceMaterial,
        Material lightMaterial)
    {
        Transform zoneRoot = CreateGroup(playgroundRoot, "PB_PlaygroundZone");

        CreatePbCube("PB_Track_Base", zoneRoot, new Vector3(32f, 0.02f, -56f), new Vector3(72f, 0.04f, 42f), trackMaterial, false);
        CreatePbCube("PB_Field", zoneRoot, new Vector3(32f, 0.04f, -56f), new Vector3(52f, 0.03f, 22f), fieldMaterial, false);
        CreatePbCube("PB_Stand_Platform", zoneRoot, new Vector3(-10f, 0.25f, -56f), new Vector3(12f, 0.5f, 26f), bleacherMaterial);
        CreateStepStack(zoneRoot, "PB_Stand_Steps", new Vector3(-13f, 0.5f, -56f), new Vector3(7f, 2.8f, 24f), 5, bleacherMaterial, 1f);
        CreatePbCube("PB_Stand_Canopy", zoneRoot, new Vector3(-10f, 5.6f, -56f), new Vector3(13f, 0.6f, 28f), fenceMaterial);
        CreatePbCube("PB_Court_Base", zoneRoot, new Vector3(-32f, 0.03f, -56f), new Vector3(24f, 0.05f, 16f), fenceMaterial, false);
        CreatePbCube("PB_Goal_Left", zoneRoot, new Vector3(6f, 1.4f, -56f), new Vector3(0.25f, 2.8f, 7.2f), lightMaterial);
        CreatePbCube("PB_Goal_Right", zoneRoot, new Vector3(58f, 1.4f, -56f), new Vector3(0.25f, 2.8f, 7.2f), lightMaterial);
        CreatePbCube("PB_FlagPole_A", zoneRoot, new Vector3(68f, 4f, -37f), new Vector3(0.25f, 8f, 0.25f), lightMaterial);
        CreatePbCube("PB_FlagPole_B", zoneRoot, new Vector3(68f, 4f, -75f), new Vector3(0.25f, 8f, 0.25f), lightMaterial);

        CreatePerimeterFenceSection(zoneRoot, "PB_PlaygroundFence_North", new Vector3(32f, 1.6f, -33.5f), new Vector3(74f, 3.2f, 0.3f), fenceMaterial);
        CreatePerimeterFenceSection(zoneRoot, "PB_PlaygroundFence_South", new Vector3(32f, 1.6f, -78.5f), new Vector3(74f, 3.2f, 0.3f), fenceMaterial);
        CreatePerimeterFenceSection(zoneRoot, "PB_PlaygroundFence_East", new Vector3(69f, 1.6f, -56f), new Vector3(0.3f, 3.2f, 45f), fenceMaterial);
        CreateSignBoard(zoneRoot, "SIGN_Playground", "操场", "跑道 / 球场 / 看台", new Vector3(-10f, 7.2f, -42f), new Vector3(9f, 1.6f, 0.3f), bleacherMaterial, Color.white);
    }

    private static void CreateScenicProps(
        Transform scenicRoot,
        Material benchMaterial,
        Material lightMaterial,
        Material academicAccentMaterial,
        Material dormAccentMaterial,
        Material cafeteriaAccentMaterial)
    {
        CreateLampPost(scenicRoot, "PB_Lamp_Entrance_A", new Vector3(-10f, 0f, -54f), lightMaterial);
        CreateLampPost(scenicRoot, "PB_Lamp_Entrance_B", new Vector3(10f, 0f, -54f), lightMaterial);
        CreateLampPost(scenicRoot, "PB_Lamp_Academic_A", new Vector3(-18f, 0f, 6f), lightMaterial);
        CreateLampPost(scenicRoot, "PB_Lamp_Academic_B", new Vector3(18f, 0f, 6f), lightMaterial);
        CreateLampPost(scenicRoot, "PB_Lamp_Dorm_A", new Vector3(52f, 0f, 14f), lightMaterial);
        CreateLampPost(scenicRoot, "PB_Lamp_Dorm_B", new Vector3(52f, 0f, 42f), lightMaterial);
        CreateLampPost(scenicRoot, "PB_Lamp_Cafe", new Vector3(-42f, 0f, -4f), lightMaterial);
        CreateLampPost(scenicRoot, "PB_Lamp_Playground", new Vector3(6f, 0f, -38f), lightMaterial);

        CreatePlanter(scenicRoot, "PB_MainPlazaPlanter_A", new Vector3(-16f, 0.45f, -10f), new Vector3(5f, 0.9f, 3f), academicAccentMaterial, benchMaterial);
        CreatePlanter(scenicRoot, "PB_MainPlazaPlanter_B", new Vector3(16f, 0.45f, -10f), new Vector3(5f, 0.9f, 3f), academicAccentMaterial, benchMaterial);
        CreatePlanter(scenicRoot, "PB_RoadsidePlanter_Dorm", new Vector3(46f, 0.45f, 28f), new Vector3(3.8f, 0.9f, 3f), dormAccentMaterial, benchMaterial);
        CreatePlanter(scenicRoot, "PB_RoadsidePlanter_Cafe", new Vector3(-36f, 0.45f, 10f), new Vector3(3.8f, 0.9f, 3f), cafeteriaAccentMaterial, benchMaterial);
    }

    private static void CreateGameplayMarkers(Transform gameplayRoot, Material extractMaterial)
    {
        RaidGameMode raidGameMode = Object.FindFirstObjectByType<RaidGameMode>();
        PrototypeEnemySpawnProfile regularProfile = AssetDatabase.LoadAssetAtPath<PrototypeEnemySpawnProfile>(RegularZombieProfilePath);
        PrototypeEnemySpawnProfile policeProfile = AssetDatabase.LoadAssetAtPath<PrototypeEnemySpawnProfile>(PoliceZombieProfilePath);
        PrototypeEnemySpawnProfile soldierProfile = AssetDatabase.LoadAssetAtPath<PrototypeEnemySpawnProfile>(SoldierZombieProfilePath);
        PrototypeEnemySpawnProfile dogProfile = AssetDatabase.LoadAssetAtPath<PrototypeEnemySpawnProfile>(ZombieDogProfilePath);

        CreatePlayerSpawnPoint(gameplayRoot, "SPAWN_MainGate", new Vector3(0f, 0f, -78f), Quaternion.identity);
        CreatePlayerSpawnPoint(gameplayRoot, "SPAWN_DormSide", new Vector3(50f, 0f, 10f), Quaternion.Euler(0f, -90f, 0f));
        CreatePlayerSpawnPoint(gameplayRoot, "SPAWN_Cafeteria", new Vector3(-34f, 0f, 0f), Quaternion.Euler(0f, 60f, 0f));

        CreateExtractionZone(gameplayRoot, raidGameMode, "EXT_Playground", "撤离_操场", new Vector3(4f, 0f, -56f), new Vector3(4f, 2.2f, 6f), extractMaterial);
        CreateExtractionZone(gameplayRoot, raidGameMode, "EXT_ServiceGate", "撤离_后勤口", new Vector3(-82f, 0f, 16f), new Vector3(4f, 2.2f, 6f), extractMaterial);

        CreateSpawnArea(gameplayRoot, "SpawnArea_AcademicCourtyard", new Vector3(0f, 0f, 33f), new Vector3(20f, 3f, 14f), 2, 4, regularProfile, policeProfile);
        CreateSpawnArea(gameplayRoot, "SpawnArea_DormLane", new Vector3(68f, 0f, 28f), new Vector3(26f, 3f, 40f), 2, 4, regularProfile, dogProfile);
        CreateSpawnArea(gameplayRoot, "SpawnArea_CafeteriaRear", new Vector3(-70f, 0f, 18f), new Vector3(16f, 3f, 20f), 1, 3, policeProfile, soldierProfile);
        CreateSpawnArea(gameplayRoot, "SpawnArea_Playground", new Vector3(30f, 0f, -56f), new Vector3(56f, 3f, 30f), 2, 5, regularProfile, dogProfile, soldierProfile);
    }

    private static void CreateCampusLighting(Transform lightingRoot, Material lightMaterial)
    {
        CreatePointLight(lightingRoot, "LGT_MainGate", new Vector3(0f, 5.8f, -82f), new Color(1f, 0.9f, 0.72f), 6.5f, 24f);
        CreatePointLight(lightingRoot, "LGT_AcademicPlaza", new Vector3(0f, 6.2f, 6f), new Color(1f, 0.92f, 0.8f), 6.3f, 26f);
        CreatePointLight(lightingRoot, "LGT_DormCourt", new Vector3(70f, 5.6f, 28f), new Color(0.84f, 0.94f, 1f), 5.2f, 22f);
        CreatePointLight(lightingRoot, "LGT_Cafeteria", new Vector3(-56f, 5.4f, -1f), new Color(1f, 0.88f, 0.7f), 5f, 20f);
        CreatePointLight(lightingRoot, "LGT_PlaygroundStand", new Vector3(-10f, 7f, -56f), new Color(0.95f, 0.94f, 0.88f), 7f, 28f);
        CreatePbCube("PB_LightMarker_Academic", lightingRoot, new Vector3(0f, 5.4f, 6f), new Vector3(0.3f, 0.3f, 0.3f), lightMaterial, false);
    }

    private static Transform CreateOpenShell(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector2 footprint,
        float wallHeight,
        float entranceWidth,
        Material wallMaterial,
        Material floorMaterial,
        Material roofMaterial)
    {
        const float wallThickness = 0.45f;
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = localRotation;

        CreatePbCube("Floor", root.transform, new Vector3(0f, -0.1f, 0f), new Vector3(footprint.x, 0.2f, footprint.y), floorMaterial);
        CreatePbCube("Roof", root.transform, new Vector3(0f, wallHeight + 0.12f, 0f), new Vector3(footprint.x + 0.6f, 0.24f, footprint.y + 0.6f), roofMaterial);
        CreatePbCube("Wall_Back", root.transform, new Vector3(0f, wallHeight * 0.5f, footprint.y * 0.5f - wallThickness * 0.5f), new Vector3(footprint.x, wallHeight, wallThickness), wallMaterial);
        CreatePbCube("Wall_Left", root.transform, new Vector3(-footprint.x * 0.5f + wallThickness * 0.5f, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, footprint.y), wallMaterial);
        CreatePbCube("Wall_Right", root.transform, new Vector3(footprint.x * 0.5f - wallThickness * 0.5f, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, footprint.y), wallMaterial);

        float clampedEntranceWidth = Mathf.Clamp(entranceWidth, 0f, footprint.x - 2f);
        if (clampedEntranceWidth <= 0.01f)
        {
            CreatePbCube("Wall_Front", root.transform, new Vector3(0f, wallHeight * 0.5f, -footprint.y * 0.5f + wallThickness * 0.5f), new Vector3(footprint.x, wallHeight, wallThickness), wallMaterial);
        }
        else
        {
            float sideWidth = (footprint.x - clampedEntranceWidth) * 0.5f;
            CreatePbCube("Wall_Front_Left", root.transform, new Vector3(-(clampedEntranceWidth * 0.5f + sideWidth * 0.5f), wallHeight * 0.5f, -footprint.y * 0.5f + wallThickness * 0.5f), new Vector3(sideWidth, wallHeight, wallThickness), wallMaterial);
            CreatePbCube("Wall_Front_Right", root.transform, new Vector3(clampedEntranceWidth * 0.5f + sideWidth * 0.5f, wallHeight * 0.5f, -footprint.y * 0.5f + wallThickness * 0.5f), new Vector3(sideWidth, wallHeight, wallThickness), wallMaterial);
            CreatePbCube("Lintel_Front", root.transform, new Vector3(0f, wallHeight - 1.1f, -footprint.y * 0.5f + wallThickness * 0.5f), new Vector3(clampedEntranceWidth, 0.6f, wallThickness), wallMaterial);
        }

        CreatePbCube("Trim_Front", root.transform, new Vector3(0f, 3.5f, -footprint.y * 0.5f + 0.08f), new Vector3(Mathf.Max(footprint.x - 2f, 4f), 0.18f, 0.15f), roofMaterial, false);
        CreatePbCube("Trim_Back", root.transform, new Vector3(0f, 3.5f, footprint.y * 0.5f - 0.08f), new Vector3(Mathf.Max(footprint.x - 2f, 4f), 0.18f, 0.15f), roofMaterial, false);
        return root.transform;
    }

    private static void CreateWindowBands(Transform shellRoot, string prefix, Vector2 footprint, float windowHeight, float windowBandHeight, Material glassMaterial)
    {
        float frontZ = -footprint.y * 0.5f + 0.12f;
        float backZ = footprint.y * 0.5f - 0.12f;
        float sideX = footprint.x * 0.5f - 0.12f;

        float frontWidth = Mathf.Max(footprint.x - 6f, 6f);
        float sideDepth = Mathf.Max(footprint.y - 6f, 5f);

        CreatePbCube($"{prefix}_WindowFront_L1", shellRoot, new Vector3(0f, 2.6f, frontZ), new Vector3(frontWidth, windowBandHeight, 0.12f), glassMaterial, false);
        CreatePbCube($"{prefix}_WindowBack_L1", shellRoot, new Vector3(0f, 2.6f, backZ), new Vector3(frontWidth, windowBandHeight, 0.12f), glassMaterial, false);
        CreatePbCube($"{prefix}_WindowLeft_L1", shellRoot, new Vector3(-sideX, 2.6f, 0f), new Vector3(0.12f, windowBandHeight, sideDepth), glassMaterial, false);
        CreatePbCube($"{prefix}_WindowRight_L1", shellRoot, new Vector3(sideX, 2.6f, 0f), new Vector3(0.12f, windowBandHeight, sideDepth), glassMaterial, false);

        if (windowHeight > 7f)
        {
            CreatePbCube($"{prefix}_WindowFront_L2", shellRoot, new Vector3(0f, 5.9f, frontZ), new Vector3(frontWidth, windowBandHeight, 0.12f), glassMaterial, false);
            CreatePbCube($"{prefix}_WindowBack_L2", shellRoot, new Vector3(0f, 5.9f, backZ), new Vector3(frontWidth, windowBandHeight, 0.12f), glassMaterial, false);
            CreatePbCube($"{prefix}_WindowLeft_L2", shellRoot, new Vector3(-sideX, 5.9f, 0f), new Vector3(0.12f, windowBandHeight, sideDepth), glassMaterial, false);
            CreatePbCube($"{prefix}_WindowRight_L2", shellRoot, new Vector3(sideX, 5.9f, 0f), new Vector3(0.12f, windowBandHeight, sideDepth), glassMaterial, false);
        }
    }

    private static void CreateClassroomProps(Transform parent, Material propMaterial, Material accentMaterial)
    {
        CreatePbCube("PB_ClassroomBlock_A", parent, new Vector3(-14f, 1.1f, 18f), new Vector3(7f, 2.2f, 4f), propMaterial);
        CreatePbCube("PB_ClassroomBlock_B", parent, new Vector3(14f, 1.1f, 18f), new Vector3(7f, 2.2f, 4f), propMaterial);
        CreatePbCube("PB_LabBlock_A", parent, new Vector3(-18f, 1.1f, 48f), new Vector3(8f, 2.2f, 4.2f), propMaterial);
        CreatePbCube("PB_LabBlock_B", parent, new Vector3(18f, 1.1f, 48f), new Vector3(8f, 2.2f, 4.2f), propMaterial);
        CreatePbCube("PB_NoticeBoard", parent, new Vector3(0f, 2f, 17.4f), new Vector3(4.4f, 2.2f, 0.2f), accentMaterial, false);
    }

    private static void CreatePerimeterFence(Transform parent, Material material)
    {
        CreatePerimeterFenceSection(parent, "PB_Fence_North", new Vector3(0f, 1.8f, 94.5f), new Vector3(238f, 3.6f, 0.4f), material);
        CreatePerimeterFenceSection(parent, "PB_Fence_South_Left", new Vector3(-62f, 1.8f, -94.5f), new Vector3(112f, 3.6f, 0.4f), material);
        CreatePerimeterFenceSection(parent, "PB_Fence_South_Right", new Vector3(62f, 1.8f, -94.5f), new Vector3(112f, 3.6f, 0.4f), material);
        CreatePerimeterFenceSection(parent, "PB_Fence_West", new Vector3(-119.5f, 1.8f, 0f), new Vector3(0.4f, 3.6f, 189f), material);
        CreatePerimeterFenceSection(parent, "PB_Fence_East", new Vector3(119.5f, 1.8f, 0f), new Vector3(0.4f, 3.6f, 189f), material);
    }

    private static void CreatePerimeterFenceSection(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        CreatePbCube(name, parent, position, size, material);
    }

    private static void CreateStepStack(Transform parent, string objectName, Vector3 baseCenter, Vector3 totalSize, int steps, Material material, float forwardDirection)
    {
        Transform root = CreateGroup(parent, objectName);
        float stepHeight = totalSize.y / Mathf.Max(1, steps);
        float stepDepth = totalSize.z / Mathf.Max(1, steps);
        float zStart = baseCenter.z - forwardDirection * (totalSize.z * 0.5f) + forwardDirection * (stepDepth * 0.5f);

        for (int index = 0; index < steps; index++)
        {
            float currentHeight = stepHeight * (index + 1);
            float currentDepth = stepDepth * (steps - index);
            float z = zStart + forwardDirection * (stepDepth * 0.5f * index);
            CreatePbCube($"Step_{index + 1}", root, new Vector3(baseCenter.x, baseCenter.y + currentHeight * 0.5f, z), new Vector3(totalSize.x, currentHeight, currentDepth), material);
        }
    }

    private static void CreatePlanter(Transform parent, string objectName, Vector3 position, Vector3 boxSize, Material boxMaterial, Material innerMaterial)
    {
        Transform root = CreateGroup(parent, objectName);
        CreatePbCube("Outer", root, position, boxSize, boxMaterial);
        CreatePbCube("Inner", root, position + new Vector3(0f, boxSize.y * 0.2f, 0f), new Vector3(boxSize.x * 0.78f, boxSize.y * 0.55f, boxSize.z * 0.78f), innerMaterial);
    }

    private static void CreateBench(Transform parent, string objectName, Vector3 position, Material material)
    {
        Transform root = CreateGroup(parent, objectName);
        CreatePbCube("Seat", root, position + new Vector3(0f, 0.45f, 0f), new Vector3(2.6f, 0.18f, 0.65f), material);
        CreatePbCube("Back", root, position + new Vector3(0f, 0.88f, -0.24f), new Vector3(2.6f, 0.65f, 0.12f), material);
        CreatePbCube("Leg_L", root, position + new Vector3(-1f, 0.22f, 0f), new Vector3(0.18f, 0.44f, 0.18f), material);
        CreatePbCube("Leg_R", root, position + new Vector3(1f, 0.22f, 0f), new Vector3(0.18f, 0.44f, 0.18f), material);
    }

    private static void CreateLampPost(Transform parent, string objectName, Vector3 position, Material material)
    {
        Transform root = CreateGroup(parent, objectName);
        CreatePbCube("Pole", root, position + new Vector3(0f, 2.6f, 0f), new Vector3(0.18f, 5.2f, 0.18f), material);
        CreatePbCube("Head", root, position + new Vector3(0f, 5.35f, 0f), new Vector3(0.75f, 0.24f, 0.75f), material, false);
        CreatePointLight(root, "Light", position + new Vector3(0f, 5.1f, 0f), new Color(1f, 0.93f, 0.78f), 3.2f, 14f);
    }

    private static void CreateSignBoard(
        Transform parent,
        string objectName,
        string title,
        string subtitle,
        Vector3 localPosition,
        Vector3 boardSize,
        Material material,
        Color textColor)
    {
        GameObject board = CreatePbCube(objectName, parent, localPosition, boardSize, material, false);
        float offset = boardSize.z * 0.5f + 0.02f;
        CreateWorldText(board.transform, "Title", title, new Vector3(0f, boardSize.y * 0.18f, offset), 60, textColor, 0.08f);
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            CreateWorldText(board.transform, "Subtitle", subtitle, new Vector3(0f, -boardSize.y * 0.14f, offset), 40, textColor, 0.055f);
        }
    }

    private static void CreatePlayerSpawnPoint(Transform parent, string name, Vector3 position, Quaternion rotation)
    {
        GameObject spawn = new GameObject(name);
        spawn.transform.SetParent(parent, false);
        spawn.transform.localPosition = position;
        spawn.transform.localRotation = rotation;
        spawn.AddComponent<RaidPlayerSpawnPoint>();
    }

    private static void CreateExtractionZone(Transform parent, RaidGameMode raidGameMode, string objectName, string zoneName, Vector3 position, Vector3 size, Material extractMaterial)
    {
        GameObject zoneRoot = new GameObject(objectName);
        zoneRoot.transform.SetParent(parent, false);
        zoneRoot.transform.localPosition = position;

        BoxCollider trigger = zoneRoot.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = size;
        trigger.center = new Vector3(0f, size.y * 0.5f, 0f);

        ExtractionZone extractionZone = zoneRoot.AddComponent<ExtractionZone>();
        extractionZone.Configure(raidGameMode, zoneName, "Extract", true);

        CreatePbCube("Beacon", zoneRoot.transform, new Vector3(0f, -0.35f, 0f), new Vector3(size.x - 0.8f, 0.05f, size.z - 0.8f), extractMaterial, false);
    }

    private static void CreateSpawnArea(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Vector3 areaSize,
        int minCount,
        int maxCount,
        params PrototypeEnemySpawnProfile[] profiles)
    {
        var validProfiles = new List<PrototypeEnemySpawnProfile>();
        if (profiles != null)
        {
            foreach (PrototypeEnemySpawnProfile profile in profiles)
            {
                if (profile != null)
                {
                    validProfiles.Add(profile);
                }
            }
        }

        if (validProfiles.Count == 0)
        {
            return;
        }

        GameObject spawnAreaObject = new GameObject(objectName);
        spawnAreaObject.transform.SetParent(parent, false);
        spawnAreaObject.transform.localPosition = localPosition;
        PrototypeEnemySpawnArea spawnArea = spawnAreaObject.AddComponent<PrototypeEnemySpawnArea>();
        spawnArea.Configure(validProfiles, minCount, maxCount, areaSize, 5f, 4);
    }

    private static void RepositionPlayer()
    {
        GameObject player = GameObject.Find("FpsPlayer");
        if (player == null)
        {
            player = GameObject.Find("RaidPlayerBootstrap");
        }

        if (player == null)
        {
            player = GameObject.Find("BaseHubPlayer");
        }

        if (player == null)
        {
            return;
        }

        player.transform.position = new Vector3(0f, 1.1f, -78f);
        player.transform.rotation = Quaternion.identity;
    }

    private static void ConfigureDirectionalLight()
    {
        GameObject lightObject = GameObject.Find("Directional Light");
        if (lightObject == null)
        {
            lightObject = new GameObject("Directional Light");
        }

        Light light = lightObject.GetComponent<Light>();
        if (light == null)
        {
            light = lightObject.AddComponent<Light>();
        }

        light.type = LightType.Directional;
        light.intensity = 1.08f;
        light.color = new Color(1f, 0.98f, 0.94f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(48f, -34f, 0f);
    }

    private static void DeleteGeneratedRoots(Scene scene)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            if (rootObject == null)
            {
                continue;
            }

            if (rootObject.name == "PB_SchoolCampus" || rootObject.name == "PrototypeSchoolRaid")
            {
                Object.DestroyImmediate(rootObject);
            }
        }
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static GameObject CreatePbCube(
        string objectName,
        Transform parent,
        Vector3 localPosition,
        Vector3 size,
        Material material,
        bool withCollider = true)
    {
        ProBuilderMesh mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
        mesh.gameObject.name = objectName;
        mesh.transform.SetParent(parent, false);
        mesh.transform.localPosition = localPosition;
        mesh.GetComponent<MeshRenderer>().sharedMaterial = material;
        mesh.ToMesh();
        mesh.Refresh();

        if (withCollider)
        {
            BoxCollider collider = mesh.gameObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = mesh.gameObject.AddComponent<BoxCollider>();
            }

            collider.center = Vector3.zero;
            collider.size = size;
        }
        else
        {
            Collider collider = mesh.gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        MarkStatic(mesh.gameObject);
        return mesh.gameObject;
    }

    private static void CreatePointLight(Transform parent, string objectName, Vector3 localPosition, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(objectName);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;

        Light pointLight = lightObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = color;
        pointLight.intensity = intensity;
        pointLight.range = range;
    }

    private static void CreateWorldText(Transform parent, string objectName, string text, Vector3 localPosition, int fontSize, Color color, float characterSize)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.font = ResolveBuiltInFont();
        if (textMesh.font != null)
        {
            MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = textMesh.font.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        textMesh.text = text ?? string.Empty;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = color;
    }

    private static Font ResolveBuiltInFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void MarkStatic(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return;
        }

        GameObjectUtility.SetStaticEditorFlags(
            gameObject,
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic);
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
}
