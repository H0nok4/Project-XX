using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class SchoolInteriorBuilder
{
    // Domain reload is used as the safest bridge for the open Unity editor to consume build requests.
    private const string ScenePath = "Assets/Scenes/SchoolTestScene.unity";
    private const string CampusRootName = "PB_SchoolCampus";
    private const string TeachingInteriorRootName = "PB_Academic_InteriorGenerated";
    private const string DormitoryInteriorRootName = "PB_Dormitory_InteriorGenerated";
    private const string CafeteriaInteriorRootName = "PB_Cafeteria_InteriorGenerated";
    private const string AutoBuildRequestRelativePath = "Temp/SchoolInteriorBuilder.request";
    private const string AutoBuildResultRelativePath = "Temp/SchoolInteriorBuilder.result";
    private static readonly StaticEditorFlags GeneratedStaticFlags = (StaticEditorFlags)22;
    private static readonly string[] TeachingPlaceholderNames =
    {
        "PB_ClassroomBlock_A",
        "PB_ClassroomBlock_B",
        "PB_LabBlock_A",
        "PB_LabBlock_B"
    };

    private sealed class MaterialSet
    {
        public Material Wall;
        public Material Floor;
        public Material Ceiling;
        public Material Wood;
        public Material Board;
        public Material Accent;
        public Material Locker;
        public Material Glass;
    }

    [InitializeOnLoadMethod]
    private static void RegisterPendingAutomation()
    {
        string requestPath = GetProjectRelativeAbsolutePath(AutoBuildRequestRelativePath);
        if (!File.Exists(requestPath))
        {
            return;
        }

        File.Delete(requestPath);
        EditorApplication.delayCall += ExecutePendingAutoBuild;
    }

    [MenuItem("Tools/Prototype/School Interiors/Build All Interiors")]
    public static void BuildAllInteriors()
    {
        if (!TryOpenSchoolScene(interactive: true))
        {
            return;
        }

        BuildAllInteriorsInternal(saveScene: true);
    }

    [MenuItem("Tools/Prototype/School Interiors/Build Teaching Building")]
    public static void BuildTeachingBuildingInteriors()
    {
        if (!TryOpenSchoolScene(interactive: true))
        {
            return;
        }

        MaterialSet materials = LoadMaterials();
        Transform campus = GetCampusRoot();
        BuildTeachingBuildingInteriors(campus, materials);
        FinalizeSceneChanges(SceneManager.GetActiveScene(), saveScene: true);
    }

    [MenuItem("Tools/Prototype/School Interiors/Build Dormitory")]
    public static void BuildDormitoryInteriors()
    {
        if (!TryOpenSchoolScene(interactive: true))
        {
            return;
        }

        MaterialSet materials = LoadMaterials();
        Transform campus = GetCampusRoot();
        BuildDormitoryInteriors(campus, materials);
        FinalizeSceneChanges(SceneManager.GetActiveScene(), saveScene: true);
    }

    [MenuItem("Tools/Prototype/School Interiors/Build Cafeteria")]
    public static void BuildCafeteriaInteriors()
    {
        if (!TryOpenSchoolScene(interactive: true))
        {
            return;
        }

        MaterialSet materials = LoadMaterials();
        Transform campus = GetCampusRoot();
        BuildCafeteriaInteriors(campus, materials);
        FinalizeSceneChanges(SceneManager.GetActiveScene(), saveScene: true);
    }

    [MenuItem("Tools/Prototype/School Interiors/Clear Generated Interiors")]
    public static void ClearGeneratedInteriors()
    {
        if (!TryOpenSchoolScene(interactive: true))
        {
            return;
        }

        Transform campus = GetCampusRoot();
        ClearGeneratedInteriors(campus);
        RestoreTeachingPlaceholders(campus);
        FinalizeSceneChanges(SceneManager.GetActiveScene(), saveScene: true);
    }

    [MenuItem("Tools/Prototype/School Interiors/Restore Teaching Placeholders")]
    public static void RestoreTeachingPlaceholderVolumes()
    {
        if (!TryOpenSchoolScene(interactive: true))
        {
            return;
        }

        Transform campus = GetCampusRoot();
        RestoreTeachingPlaceholders(campus);
        FinalizeSceneChanges(SceneManager.GetActiveScene(), saveScene: true);
    }

    public static void BuildAllInteriorsBatch()
    {
        if (!TryOpenSchoolScene(interactive: false))
        {
            return;
        }

        BuildAllInteriorsInternal(saveScene: true);
    }

    private static void ExecutePendingAutoBuild()
    {
        string resultPath = GetProjectRelativeAbsolutePath(AutoBuildResultRelativePath);

        try
        {
            if (!TryOpenSchoolScene(interactive: false))
            {
                WriteAutomationResult(resultPath, "error", "SchoolTestScene could not be opened.");
                return;
            }

            BuildAllInteriorsInternal(saveScene: true);
            WriteAutomationResult(resultPath, "ok", $"Interiors built at {DateTime.Now:O}");
        }
        catch (Exception exception)
        {
            WriteAutomationResult(resultPath, "error", exception.ToString());
            Debug.LogException(exception);
        }
    }

    private static void BuildAllInteriorsInternal(bool saveScene)
    {
        Scene scene = SceneManager.GetActiveScene();
        MaterialSet materials = LoadMaterials();
        Transform campus = GetCampusRoot();

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build School Interiors");

        ClearGeneratedInteriors(campus);
        DisableTeachingPlaceholders(campus);
        BuildTeachingBuildingInteriors(campus, materials);
        BuildDormitoryInteriors(campus, materials);
        BuildCafeteriaInteriors(campus, materials);

        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = campus.gameObject;
        FinalizeSceneChanges(scene, saveScene);
    }

    private static void BuildTeachingBuildingInteriors(Transform campus, MaterialSet materials)
    {
        Transform teachingBuilding = FindChildRecursive(campus, "PB_TeachingBuilding");
        if (teachingBuilding == null)
        {
            throw new InvalidOperationException("PB_TeachingBuilding was not found in PB_SchoolCampus.");
        }

        DisableTeachingPlaceholders(campus);

        Transform interiorRoot = ResetGeneratedRoot(teachingBuilding, TeachingInteriorRootName);
        Transform southWing = CreateGroup(interiorRoot, "PB_SouthWingInterior", new Vector3(0f, 0f, 18f));
        Transform northWing = CreateGroup(interiorRoot, "PB_NorthWingInterior", new Vector3(0f, 0f, 48f));
        Transform westConnector = CreateGroup(interiorRoot, "PB_WestConnectorInterior", new Vector3(-22f, 0f, 33f));
        Transform eastConnector = CreateGroup(interiorRoot, "PB_EastConnectorInterior", new Vector3(22f, 0f, 33f));

        CreateBox(southWing, "PB_SouthWing_L2Slab", new Vector3(0f, 4.45f, 0f), new Vector3(40.8f, 0.18f, 17.2f), materials.Floor);
        CreateBox(northWing, "PB_NorthWing_L2Slab", new Vector3(0f, 4.45f, 0f), new Vector3(52.8f, 0.18f, 13.2f), materials.Floor);
        CreateBox(westConnector, "PB_WestConnector_L2Slab", new Vector3(0f, 4.45f, 0f), new Vector3(11.2f, 0.18f, 17.2f), materials.Floor);
        CreateBox(eastConnector, "PB_EastConnector_L2Slab", new Vector3(0f, 4.45f, 0f), new Vector3(11.2f, 0.18f, 17.2f), materials.Floor);

        CreateLockerBank(southWing, "PB_SouthHall_Lockers_L1", new Vector3(-16.2f, 1.05f, -5.8f), 0f, 6, materials);
        CreateLockerBank(southWing, "PB_SouthHall_Lockers_L2", new Vector3(-16.2f, 5.05f, -5.8f), 0f, 6, materials);
        CreateBench(southWing, "PB_SouthHall_Bench_L1", new Vector3(10.8f, 0.45f, -6.4f), 0f, materials.Wood, materials.Accent, 2.8f);
        CreateBench(southWing, "PB_SouthHall_Bench_L2", new Vector3(10.8f, 4.45f, -6.4f), 0f, materials.Wood, materials.Accent, 2.8f);
        CreateNoticeBoard(southWing, "PB_SouthHall_Notice_L1", new Vector3(0f, 1.8f, -8.5f), 0f, materials);
        CreateNoticeBoard(southWing, "PB_SouthHall_Notice_L2", new Vector3(0f, 5.8f, -8.5f), 0f, materials);

        CreateLockerBank(northWing, "PB_NorthHall_Lockers_L1", new Vector3(20f, 1.05f, -4.1f), 180f, 4, materials);
        CreateLockerBank(northWing, "PB_NorthHall_Lockers_L2", new Vector3(20f, 5.05f, -4.1f), 180f, 4, materials);
        CreateBench(northWing, "PB_NorthHall_Bench_L1", new Vector3(-20f, 0.45f, -4.8f), 180f, materials.Wood, materials.Accent, 2.4f);
        CreateBench(northWing, "PB_NorthHall_Bench_L2", new Vector3(-20f, 4.45f, -4.8f), 180f, materials.Wood, materials.Accent, 2.4f);

        BuildClassroomRoom(southWing, "PB_Classroom_A_L1", new Vector3(-13.5f, 0f, 1.2f), new Vector3(9.4f, 2.95f, 6.2f), 1.45f, materials);
        BuildStudyCommonsRoom(southWing, "PB_StudyCommons_L1", new Vector3(0f, 0f, 1.1f), new Vector3(10.2f, 2.95f, 6.4f), 1.45f, materials);
        BuildClassroomRoom(southWing, "PB_Classroom_B_L1", new Vector3(13.5f, 0f, 1.2f), new Vector3(9.4f, 2.95f, 6.2f), 1.45f, materials);

        BuildClassroomRoom(southWing, "PB_Classroom_A_L2", new Vector3(-13.5f, 4.45f, 1.2f), new Vector3(9.4f, 2.8f, 6.2f), 1.4f, materials);
        BuildStudyCommonsRoom(southWing, "PB_StudyCommons_L2", new Vector3(0f, 4.45f, 1.1f), new Vector3(10.2f, 2.8f, 6.4f), 1.4f, materials);
        BuildClassroomRoom(southWing, "PB_Classroom_B_L2", new Vector3(13.5f, 4.45f, 1.2f), new Vector3(9.4f, 2.8f, 6.2f), 1.4f, materials);

        BuildLabRoom(northWing, "PB_Lab_A_L1", new Vector3(-18f, 0f, 0.7f), new Vector3(10.2f, 3f, 6.4f), 1.5f, materials);
        BuildFacultyRoom(northWing, "PB_FacultyHub_L1", new Vector3(0f, 0f, 0.4f), new Vector3(13f, 3f, 6f), 1.5f, materials);
        BuildLabRoom(northWing, "PB_Lab_B_L1", new Vector3(18f, 0f, 0.7f), new Vector3(10.2f, 3f, 6.4f), 1.5f, materials);

        BuildLabRoom(northWing, "PB_Lab_A_L2", new Vector3(-18f, 4.45f, 0.7f), new Vector3(10.2f, 2.85f, 6.4f), 1.425f, materials);
        BuildFacultyRoom(northWing, "PB_FacultyHub_L2", new Vector3(0f, 4.45f, 0.4f), new Vector3(13f, 2.85f, 6f), 1.425f, materials);
        BuildLabRoom(northWing, "PB_Lab_B_L2", new Vector3(18f, 4.45f, 0.7f), new Vector3(10.2f, 2.85f, 6.4f), 1.425f, materials);

        BuildConnectorCore(westConnector, "PB_WestConnector", materials);
        BuildConnectorCore(eastConnector, "PB_EastConnector", materials);
    }

    private static void BuildDormitoryInteriors(Transform campus, MaterialSet materials)
    {
        Transform dormitoryZone = FindChildRecursive(campus, "PB_DormitoryZone");
        if (dormitoryZone == null)
        {
            throw new InvalidOperationException("PB_DormitoryZone was not found in PB_SchoolCampus.");
        }

        Transform interiorRoot = ResetGeneratedRoot(dormitoryZone, DormitoryInteriorRootName);
        Transform dormA = CreateGroup(interiorRoot, "PB_DormA_Interior", new Vector3(68f, 0f, 16f));
        Transform dormB = CreateGroup(interiorRoot, "PB_DormB_Interior", new Vector3(68f, 0f, 40f));
        Transform utility = CreateGroup(interiorRoot, "PB_DormUtility_Interior", new Vector3(84f, 0f, 28f));

        BuildDormBlock(dormA, "A", materials);
        BuildDormBlock(dormB, "B", materials);
        BuildDormUtility(utility, materials);
    }

    private static void BuildCafeteriaInteriors(Transform campus, MaterialSet materials)
    {
        Transform cafeteriaZone = FindChildRecursive(campus, "PB_CafeteriaZone");
        if (cafeteriaZone == null)
        {
            throw new InvalidOperationException("PB_CafeteriaZone was not found in PB_SchoolCampus.");
        }

        Transform interiorRoot = ResetGeneratedRoot(cafeteriaZone, CafeteriaInteriorRootName);
        Transform mainHall = CreateGroup(interiorRoot, "PB_MainHall_Interior", new Vector3(-56f, 0f, 10f));
        Transform kitchen = CreateGroup(interiorRoot, "PB_Kitchen_Interior", new Vector3(-72f, 0f, 18f));
        Transform service = CreateGroup(interiorRoot, "PB_ServiceLine_Interior", new Vector3(-56f, 0f, 15.5f));

        BuildCafeteriaMainHall(mainHall, materials);
        BuildCafeteriaKitchen(kitchen, materials);
        BuildServingLine(service, materials);
    }

    private static void BuildConnectorCore(Transform connectorRoot, string connectorPrefix, MaterialSet materials)
    {
        CreateStairs(connectorRoot, $"{connectorPrefix}_Stairs", new Vector3(0f, 2.05f, -2.4f), new Vector3(3.2f, 4.1f, 7f), 11, 0f, materials.Accent);
        CreateBox(connectorRoot, $"{connectorPrefix}_Landing", new Vector3(0f, 4.45f, 4.8f), new Vector3(4.2f, 0.18f, 6.2f), materials.Floor);
        CreateBench(connectorRoot, $"{connectorPrefix}_Bench_L1", new Vector3(-2.2f, 0.45f, 6.5f), 90f, materials.Wood, materials.Accent, 2f);
        CreateBench(connectorRoot, $"{connectorPrefix}_Bench_L2", new Vector3(-2.2f, 4.45f, 6.5f), 90f, materials.Wood, materials.Accent, 2f);
        CreateNoticeBoard(connectorRoot, $"{connectorPrefix}_Board", new Vector3(2.6f, 1.8f, 6.9f), -90f, materials);
    }

    private static void BuildDormBlock(Transform dormRoot, string dormLabel, MaterialSet materials)
    {
        CreateBox(dormRoot, $"PB_Dorm{dormLabel}_L2Slab", new Vector3(0f, 4.15f, 0f), new Vector3(27f, 0.18f, 11f), materials.Floor);
        CreateStairs(dormRoot, $"PB_Dorm{dormLabel}_Stairs", new Vector3(10.2f, 2f, 0f), new Vector3(3f, 4f, 5.4f), 10, 90f, materials.Accent);
        CreateLockerBank(dormRoot, $"PB_Dorm{dormLabel}_Lockers_L1", new Vector3(-11.4f, 1.05f, 0f), 90f, 4, materials);
        CreateLockerBank(dormRoot, $"PB_Dorm{dormLabel}_Lockers_L2", new Vector3(-11.4f, 5.05f, 0f), 90f, 4, materials);
        CreateBench(dormRoot, $"PB_Dorm{dormLabel}_Bench_L1", new Vector3(0f, 0.45f, 0f), 0f, materials.Wood, materials.Accent, 2.2f);
        CreateBench(dormRoot, $"PB_Dorm{dormLabel}_Bench_L2", new Vector3(0f, 4.45f, 0f), 0f, materials.Wood, materials.Accent, 2.2f);

        BuildDormRoom(dormRoot, $"PB_Dorm{dormLabel}_Room_NW_L1", new Vector3(-7f, 0f, 3.3f), new Vector3(11.2f, 2.8f, 4f), 0f, 1.4f, materials);
        BuildDormRoom(dormRoot, $"PB_Dorm{dormLabel}_Room_NE_L1", new Vector3(4.8f, 0f, 3.3f), new Vector3(8.4f, 2.8f, 4f), 0f, 1.4f, materials);
        BuildDormRoom(dormRoot, $"PB_Dorm{dormLabel}_Room_SW_L1", new Vector3(-7f, 0f, -3.3f), new Vector3(11.2f, 2.8f, 4f), 180f, 1.4f, materials);
        BuildDormRoom(dormRoot, $"PB_Dorm{dormLabel}_Room_SE_L1", new Vector3(4.8f, 0f, -3.3f), new Vector3(8.4f, 2.8f, 4f), 180f, 1.4f, materials);

        BuildDormRoom(dormRoot, $"PB_Dorm{dormLabel}_Room_NW_L2", new Vector3(-7f, 4.15f, 3.3f), new Vector3(11.2f, 2.7f, 4f), 0f, 1.35f, materials);
        BuildDormRoom(dormRoot, $"PB_Dorm{dormLabel}_Room_NE_L2", new Vector3(4.8f, 4.15f, 3.3f), new Vector3(8.4f, 2.7f, 4f), 0f, 1.35f, materials);
        BuildDormRoom(dormRoot, $"PB_Dorm{dormLabel}_Room_SW_L2", new Vector3(-7f, 4.15f, -3.3f), new Vector3(11.2f, 2.7f, 4f), 180f, 1.35f, materials);
        BuildDormRoom(dormRoot, $"PB_Dorm{dormLabel}_Room_SE_L2", new Vector3(4.8f, 4.15f, -3.3f), new Vector3(8.4f, 2.7f, 4f), 180f, 1.35f, materials);
    }

    private static void BuildDormUtility(Transform utilityRoot, MaterialSet materials)
    {
        CreateBox(utilityRoot, "PB_UtilityDivider_Left", new Vector3(-1.65f, 1.45f, 0f), new Vector3(0.18f, 2.9f, 9.2f), materials.Wall);
        CreateBox(utilityRoot, "PB_UtilityDivider_Right", new Vector3(1.65f, 1.45f, 0f), new Vector3(0.18f, 2.9f, 9.2f), materials.Wall);
        CreateBox(utilityRoot, "PB_UtilityWasherBank", new Vector3(-3.5f, 0.55f, 3.2f), new Vector3(2.2f, 1.1f, 0.8f), materials.Accent);
        CreateBox(utilityRoot, "PB_UtilityDryerBank", new Vector3(-3.5f, 1.7f, 3.2f), new Vector3(2.2f, 1.1f, 0.8f), materials.Locker);
        CreateBox(utilityRoot, "PB_UtilityFoldCounter", new Vector3(-3.2f, 0.95f, -2.8f), new Vector3(2.8f, 1.9f, 0.8f), materials.Wood);
        CreateBox(utilityRoot, "PB_UtilitySinkCounter", new Vector3(3.4f, 0.55f, -3f), new Vector3(2.4f, 1.1f, 0.8f), materials.Accent);
        CreateStall(utilityRoot, "PB_UtilityStall_A", new Vector3(3.3f, 0f, 0.8f), materials);
        CreateStall(utilityRoot, "PB_UtilityStall_B", new Vector3(3.3f, 0f, 3.5f), materials);
        CreateLockerBank(utilityRoot, "PB_UtilityDayLockers", new Vector3(-5.2f, 1.05f, 0f), 90f, 4, materials);
    }

    private static void BuildCafeteriaMainHall(Transform hallRoot, MaterialSet materials)
    {
        CreateBox(hallRoot, "PB_MainHall_CeilingCloud", new Vector3(0f, 3.2f, 0f), new Vector3(27.5f, 0.12f, 14.5f), materials.Ceiling);

        int tableIndex = 0;
        for (int row = -1; row <= 1; row++)
        {
            for (int column = -1; column <= 1; column++)
            {
                Vector3 tablePosition = new Vector3(column * 7f, 0f, row * 4.2f - 1.2f);
                CreateDiningTableSet(hallRoot, $"PB_DiningTable_{tableIndex}", tablePosition, materials);
                tableIndex++;
            }
        }

        CreateBench(hallRoot, "PB_BoothBench_West", new Vector3(-13f, 0.45f, -4.6f), 90f, materials.Wood, materials.Accent, 4.2f);
        CreateBench(hallRoot, "PB_BoothBench_East", new Vector3(13f, 0.45f, -4.6f), -90f, materials.Wood, materials.Accent, 4.2f);
        CreateBench(hallRoot, "PB_BoothBench_West_Rear", new Vector3(-13f, 0.45f, 4.8f), 90f, materials.Wood, materials.Accent, 4.2f);
        CreateBench(hallRoot, "PB_BoothBench_East_Rear", new Vector3(13f, 0.45f, 4.8f), -90f, materials.Wood, materials.Accent, 4.2f);
        CreateBox(hallRoot, "PB_TrayReturn", new Vector3(11.8f, 1f, 7f), new Vector3(2.2f, 2f, 0.9f), materials.Accent);
        CreateBox(hallRoot, "PB_RecyclingPoint", new Vector3(-11.8f, 0.65f, 7.1f), new Vector3(1.4f, 1.3f, 0.9f), materials.Locker);
    }

    private static void BuildCafeteriaKitchen(Transform kitchenRoot, MaterialSet materials)
    {
        CreateBox(kitchenRoot, "PB_KitchenPrepIsland_A", new Vector3(-2.2f, 0.55f, -1.8f), new Vector3(3.2f, 1.1f, 1.2f), materials.Wood);
        CreateBox(kitchenRoot, "PB_KitchenPrepIsland_B", new Vector3(2.2f, 0.55f, -1.8f), new Vector3(3.2f, 1.1f, 1.2f), materials.Wood);
        CreateBox(kitchenRoot, "PB_KitchenCookline", new Vector3(0f, 0.75f, 3.4f), new Vector3(8.8f, 1.5f, 1f), materials.Accent);
        CreateBox(kitchenRoot, "PB_KitchenColdStorage", new Vector3(-4.3f, 1.2f, -3.8f), new Vector3(1.4f, 2.4f, 1.2f), materials.Locker);
        CreateShelf(kitchenRoot, "PB_KitchenShelf", new Vector3(4.4f, 0f, -3.6f), 0f, materials);
        CreateBox(kitchenRoot, "PB_KitchenWashStation", new Vector3(4f, 0.55f, 2f), new Vector3(2.4f, 1.1f, 0.8f), materials.Accent);
    }

    private static void BuildServingLine(Transform serviceRoot, MaterialSet materials)
    {
        CreateQueueRail(serviceRoot, "PB_QueueRail_A", new Vector3(-4.2f, 0f, -1.7f), 0f, 4.8f, materials);
        CreateQueueRail(serviceRoot, "PB_QueueRail_B", new Vector3(0f, 0f, -1.7f), 0f, 4.8f, materials);
        CreateQueueRail(serviceRoot, "PB_QueueRail_C", new Vector3(4.2f, 0f, -1.7f), 0f, 4.8f, materials);
        CreateBox(serviceRoot, "PB_Cashier_A", new Vector3(-4.5f, 1.25f, -0.85f), new Vector3(1.6f, 2.5f, 0.4f), materials.Glass, addCollider: false);
        CreateBox(serviceRoot, "PB_Cashier_B", new Vector3(0f, 1.25f, -0.85f), new Vector3(1.6f, 2.5f, 0.4f), materials.Glass, addCollider: false);
        CreateBox(serviceRoot, "PB_Cashier_C", new Vector3(4.5f, 1.25f, -0.85f), new Vector3(1.6f, 2.5f, 0.4f), materials.Glass, addCollider: false);
    }

    private static void BuildClassroomRoom(Transform parent, string name, Vector3 roomBasePosition, Vector3 roomSize, float roomCenterHeight, MaterialSet materials)
    {
        Transform roomRoot = CreateRoomShell(parent, name, roomBasePosition + new Vector3(0f, roomCenterHeight, 0f), roomSize, 0f, 1.35f, materials);
        CreateTeacherDesk(roomRoot, "TeacherDesk", new Vector3(0f, 0f, roomSize.z * 0.24f), 0f, materials);
        CreateNoticeBoard(roomRoot, "ClassroomBoard", new Vector3(0f, roomSize.y * 0.63f, roomSize.z * 0.5f - 0.12f), 0f, materials, width: roomSize.x - 2f, height: 1.25f);
        CreateLockerBank(roomRoot, "ClassroomStorage", new Vector3(-roomSize.x * 0.36f, 1.05f - roomCenterHeight, roomSize.z * 0.24f), 90f, 3, materials);

        float[] xOffsets = { -roomSize.x * 0.26f, 0f, roomSize.x * 0.26f };
        float[] zOffsets = { -roomSize.z * 0.16f, roomSize.z * 0.02f, roomSize.z * 0.2f };
        int index = 0;
        foreach (float zOffset in zOffsets)
        {
            foreach (float xOffset in xOffsets)
            {
                CreateStudentDeskSet(roomRoot, $"Desk_{index}", new Vector3(xOffset, 0f, zOffset), 0f, materials);
                index++;
            }
        }
    }

    private static void BuildLabRoom(Transform parent, string name, Vector3 roomBasePosition, Vector3 roomSize, float roomCenterHeight, MaterialSet materials)
    {
        Transform roomRoot = CreateRoomShell(parent, name, roomBasePosition + new Vector3(0f, roomCenterHeight, 0f), roomSize, 0f, 1.35f, materials);
        CreateNoticeBoard(roomRoot, "LabBoard", new Vector3(0f, roomSize.y * 0.63f, roomSize.z * 0.5f - 0.12f), 0f, materials, width: roomSize.x - 1.8f, height: 1.15f);
        CreateLabIsland(roomRoot, "LabIsland_A", new Vector3(-roomSize.x * 0.18f, 0f, roomSize.z * 0.05f), materials);
        CreateLabIsland(roomRoot, "LabIsland_B", new Vector3(roomSize.x * 0.18f, 0f, roomSize.z * 0.05f), materials);
        CreateShelf(roomRoot, "LabCabinet_Left", new Vector3(-roomSize.x * 0.38f, 0f, roomSize.z * 0.26f), 90f, materials);
        CreateShelf(roomRoot, "LabCabinet_Right", new Vector3(roomSize.x * 0.38f, 0f, roomSize.z * 0.26f), -90f, materials);
    }

    private static void BuildStudyCommonsRoom(Transform parent, string name, Vector3 roomBasePosition, Vector3 roomSize, float roomCenterHeight, MaterialSet materials)
    {
        Transform roomRoot = CreateRoomShell(parent, name, roomBasePosition + new Vector3(0f, roomCenterHeight, 0f), roomSize, 0f, 1.5f, materials);
        CreateShelf(roomRoot, "Shelf_West", new Vector3(-roomSize.x * 0.36f, 0f, roomSize.z * 0.22f), 90f, materials);
        CreateShelf(roomRoot, "Shelf_East", new Vector3(roomSize.x * 0.36f, 0f, roomSize.z * 0.22f), -90f, materials);
        CreateStudyTable(roomRoot, "StudyTable_A", new Vector3(-1.9f, 0f, -0.1f), 0f, materials);
        CreateStudyTable(roomRoot, "StudyTable_B", new Vector3(1.9f, 0f, -0.1f), 0f, materials);
        CreateBench(roomRoot, "CommonsBench", new Vector3(0f, 0.45f - roomCenterHeight, roomSize.z * 0.26f), 0f, materials.Wood, materials.Accent, 2.6f);
    }

    private static void BuildFacultyRoom(Transform parent, string name, Vector3 roomBasePosition, Vector3 roomSize, float roomCenterHeight, MaterialSet materials)
    {
        Transform roomRoot = CreateRoomShell(parent, name, roomBasePosition + new Vector3(0f, roomCenterHeight, 0f), roomSize, 0f, 1.5f, materials);
        CreateMeetingTable(roomRoot, "MeetingTable", new Vector3(0f, 0f, -0.2f), materials);
        CreateLockerBank(roomRoot, "FacultyStorage", new Vector3(-roomSize.x * 0.34f, 1.05f - roomCenterHeight, roomSize.z * 0.24f), 90f, 4, materials);
        CreateNoticeBoard(roomRoot, "FacultyBoard", new Vector3(0f, roomSize.y * 0.62f, roomSize.z * 0.5f - 0.12f), 0f, materials, width: roomSize.x - 2.2f, height: 1.05f);
    }

    private static void BuildDormRoom(Transform parent, string name, Vector3 roomBasePosition, Vector3 roomSize, float yaw, float roomCenterHeight, MaterialSet materials)
    {
        Transform roomRoot = CreateRoomShell(parent, name, roomBasePosition + new Vector3(0f, roomCenterHeight, 0f), roomSize, yaw, 1.2f, materials);
        CreateBunkBed(roomRoot, "Bunk_A", new Vector3(-roomSize.x * 0.2f, 0f, roomSize.z * 0.18f), 0f, materials);
        CreateBunkBed(roomRoot, "Bunk_B", new Vector3(roomSize.x * 0.2f, 0f, roomSize.z * 0.18f), 0f, materials);
        CreateStudyTable(roomRoot, "StudyDesk", new Vector3(0f, 0f, -roomSize.z * 0.1f), 180f, materials, width: 1.8f);
        CreateLockerBank(roomRoot, "Wardrobe", new Vector3(roomSize.x * 0.32f, 1.05f - roomCenterHeight, -roomSize.z * 0.2f), 90f, 2, materials);
    }

    private static Transform CreateRoomShell(
        Transform parent,
        string name,
        Vector3 localCenter,
        Vector3 roomSize,
        float yaw,
        float doorWidth,
        MaterialSet materials)
    {
        Transform roomRoot = CreateGroup(parent, name, localCenter, Quaternion.Euler(0f, yaw, 0f));
        Transform shellRoot = CreateGroup(roomRoot, "Shell");

        float wallThickness = 0.16f;
        float halfHeight = roomSize.y * 0.5f;
        float halfWidth = roomSize.x * 0.5f;
        float halfDepth = roomSize.z * 0.5f;
        float sideWidth = Mathf.Max(0.4f, (roomSize.x - doorWidth) * 0.5f);

        CreateBox(shellRoot, "Wall_Back", new Vector3(0f, 0f, halfDepth - wallThickness * 0.5f), new Vector3(roomSize.x, roomSize.y, wallThickness), materials.Wall);
        CreateBox(shellRoot, "Wall_Left", new Vector3(-halfWidth + wallThickness * 0.5f, 0f, 0f), new Vector3(wallThickness, roomSize.y, roomSize.z), materials.Wall);
        CreateBox(shellRoot, "Wall_Right", new Vector3(halfWidth - wallThickness * 0.5f, 0f, 0f), new Vector3(wallThickness, roomSize.y, roomSize.z), materials.Wall);
        CreateBox(shellRoot, "Wall_Front_Left", new Vector3(-(doorWidth * 0.5f + sideWidth * 0.5f), 0f, -halfDepth + wallThickness * 0.5f), new Vector3(sideWidth, roomSize.y, wallThickness), materials.Wall);
        CreateBox(shellRoot, "Wall_Front_Right", new Vector3(doorWidth * 0.5f + sideWidth * 0.5f, 0f, -halfDepth + wallThickness * 0.5f), new Vector3(sideWidth, roomSize.y, wallThickness), materials.Wall);
        CreateBox(shellRoot, "Door_Lintel", new Vector3(0f, halfHeight - 0.18f, -halfDepth + wallThickness * 0.5f), new Vector3(doorWidth, 0.35f, wallThickness), materials.Wall);
        CreateBox(shellRoot, "Ceiling", new Vector3(0f, halfHeight + 0.05f, 0f), new Vector3(roomSize.x - 0.12f, 0.1f, roomSize.z - 0.12f), materials.Ceiling, addCollider: false);
        CreateDoorPanel(shellRoot, "DoorPanel", new Vector3(doorWidth * 0.18f, -halfHeight + 1.04f, -halfDepth + 0.06f), 24f, materials.Wood);

        return roomRoot;
    }

    private static void CreateDoorPanel(Transform parent, string name, Vector3 localPosition, float yaw, Material material)
    {
        CreateBox(parent, name, localPosition, new Vector3(1.05f, 2.08f, 0.08f), material, Quaternion.Euler(0f, yaw, 0f), addCollider: false, setStatic: false);
    }

    private static void CreateTeacherDesk(Transform parent, string name, Vector3 localPosition, float yaw, MaterialSet materials)
    {
        Transform root = CreateGroup(parent, name, localPosition, Quaternion.Euler(0f, yaw, 0f));
        CreateBox(root, "DeskBase", new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.9f, 0.7f), materials.Wood);
        CreateBox(root, "DeskTop", new Vector3(0f, 0.9f, 0f), new Vector3(1.8f, 0.1f, 0.85f), materials.Accent);
    }

    private static void CreateStudentDeskSet(Transform parent, string name, Vector3 localPosition, float yaw, MaterialSet materials)
    {
        Transform root = CreateGroup(parent, name, localPosition, Quaternion.Euler(0f, yaw, 0f));
        CreateBox(root, "Desk", new Vector3(0f, 0.38f, 0f), new Vector3(1.15f, 0.76f, 0.6f), materials.Wood);
        CreateBox(root, "Chair", new Vector3(0f, 0.28f, -0.58f), new Vector3(0.8f, 0.56f, 0.5f), materials.Accent);
    }

    private static void CreateStudyTable(Transform parent, string name, Vector3 localPosition, float yaw, MaterialSet materials, float width = 2.4f)
    {
        Transform root = CreateGroup(parent, name, localPosition, Quaternion.Euler(0f, yaw, 0f));
        CreateBox(root, "Top", new Vector3(0f, 0.74f, 0f), new Vector3(width, 0.1f, 0.85f), materials.Wood);
        CreateBox(root, "Base", new Vector3(0f, 0.38f, 0f), new Vector3(width - 0.2f, 0.76f, 0.18f), materials.Accent);
        CreateBench(root, "BenchA", new Vector3(0f, 0f, 0.8f), 0f, materials.Wood, materials.Accent, width - 0.4f);
        CreateBench(root, "BenchB", new Vector3(0f, 0f, -0.8f), 180f, materials.Wood, materials.Accent, width - 0.4f);
    }

    private static void CreateMeetingTable(Transform parent, string name, Vector3 localPosition, MaterialSet materials)
    {
        Transform root = CreateGroup(parent, name, localPosition);
        CreateBox(root, "Top", new Vector3(0f, 0.74f, 0f), new Vector3(2.8f, 0.1f, 1.3f), materials.Wood);
        CreateBox(root, "Base", new Vector3(0f, 0.38f, 0f), new Vector3(1.8f, 0.76f, 0.22f), materials.Accent);
        CreateBench(root, "Seat_North", new Vector3(0f, 0f, 1.1f), 0f, materials.Wood, materials.Accent, 2.2f);
        CreateBench(root, "Seat_South", new Vector3(0f, 0f, -1.1f), 180f, materials.Wood, materials.Accent, 2.2f);
    }

    private static void CreateBench(Transform parent, string name, Vector3 localPosition, float yaw, Material seatMaterial, Material legMaterial, float width)
    {
        Transform root = CreateGroup(parent, name, localPosition, Quaternion.Euler(0f, yaw, 0f));
        CreateBox(root, "Seat", new Vector3(0f, 0.45f, 0f), new Vector3(width, 0.1f, 0.45f), seatMaterial);
        CreateBox(root, "Back", new Vector3(0f, 0.82f, -0.16f), new Vector3(width, 0.75f, 0.1f), seatMaterial);
        CreateBox(root, "Leg_Left", new Vector3(-width * 0.4f, 0.22f, 0f), new Vector3(0.12f, 0.44f, 0.12f), legMaterial);
        CreateBox(root, "Leg_Right", new Vector3(width * 0.4f, 0.22f, 0f), new Vector3(0.12f, 0.44f, 0.12f), legMaterial);
    }

    private static void CreateLockerBank(Transform parent, string name, Vector3 localPosition, float yaw, int lockerCount, MaterialSet materials)
    {
        Transform root = CreateGroup(parent, name, localPosition, Quaternion.Euler(0f, yaw, 0f));
        float width = Mathf.Max(0.9f, lockerCount * 0.55f);
        CreateBox(root, "Body", new Vector3(0f, 1.05f, 0f), new Vector3(width, 2.1f, 0.55f), materials.Locker);
        CreateBox(root, "TopCap", new Vector3(0f, 2.13f, 0f), new Vector3(width + 0.08f, 0.08f, 0.62f), materials.Accent);
    }

    private static void CreateNoticeBoard(Transform parent, string name, Vector3 localPosition, float yaw, MaterialSet materials, float width = 2.4f, float height = 1f)
    {
        Transform root = CreateGroup(parent, name, localPosition, Quaternion.Euler(0f, yaw, 0f));
        CreateBox(root, "Board", Vector3.zero, new Vector3(width, height, 0.08f), materials.Board, addCollider: false);
        CreateBox(root, "Trim", new Vector3(0f, 0f, -0.03f), new Vector3(width + 0.1f, height + 0.1f, 0.04f), materials.Wood, addCollider: false);
    }

    private static void CreateLabIsland(Transform parent, string name, Vector3 localPosition, MaterialSet materials)
    {
        Transform root = CreateGroup(parent, name, localPosition);
        CreateBox(root, "Counter", new Vector3(0f, 0.55f, 0f), new Vector3(2.4f, 1.1f, 0.85f), materials.Accent);
        CreateBox(root, "Top", new Vector3(0f, 1.1f, 0f), new Vector3(2.6f, 0.08f, 0.95f), materials.Wood);
        CreateBox(root, "Glass", new Vector3(0f, 1.45f, 0f), new Vector3(1.6f, 0.5f, 0.08f), materials.Glass, addCollider: false);
    }

    private static void CreateShelf(Transform parent, string name, Vector3 localPosition, float yaw, MaterialSet materials)
    {
        Transform root = CreateGroup(parent, name, localPosition, Quaternion.Euler(0f, yaw, 0f));
        CreateBox(root, "Frame", new Vector3(0f, 1.05f, 0f), new Vector3(1.8f, 2.1f, 0.42f), materials.Accent);
        CreateBox(root, "Shelf_A", new Vector3(0f, 0.55f, 0f), new Vector3(1.72f, 0.08f, 0.46f), materials.Wood);
        CreateBox(root, "Shelf_B", new Vector3(0f, 1.1f, 0f), new Vector3(1.72f, 0.08f, 0.46f), materials.Wood);
        CreateBox(root, "Shelf_C", new Vector3(0f, 1.65f, 0f), new Vector3(1.72f, 0.08f, 0.46f), materials.Wood);
    }

    private static void CreateBunkBed(Transform parent, string name, Vector3 localPosition, float yaw, MaterialSet materials)
    {
        Transform root = CreateGroup(parent, name, localPosition, Quaternion.Euler(0f, yaw, 0f));
        CreateBox(root, "LowerBed", new Vector3(0f, 0.38f, 0f), new Vector3(1.95f, 0.12f, 0.95f), materials.Wood);
        CreateBox(root, "UpperBed", new Vector3(0f, 1.3f, 0f), new Vector3(1.95f, 0.12f, 0.95f), materials.Wood);
        CreateBox(root, "Frame_Left", new Vector3(-0.9f, 0.95f, 0f), new Vector3(0.08f, 1.9f, 0.08f), materials.Accent);
        CreateBox(root, "Frame_Right", new Vector3(0.9f, 0.95f, 0f), new Vector3(0.08f, 1.9f, 0.08f), materials.Accent);
        CreateBox(root, "Ladder", new Vector3(-1.02f, 0.95f, 0.3f), new Vector3(0.08f, 1.9f, 0.08f), materials.Accent);
    }

    private static void CreateDiningTableSet(Transform parent, string name, Vector3 localPosition, MaterialSet materials)
    {
        Transform root = CreateGroup(parent, name, localPosition);
        CreateBox(root, "TableTop", new Vector3(0f, 0.74f, 0f), new Vector3(2f, 0.1f, 0.9f), materials.Wood);
        CreateBox(root, "TableBase", new Vector3(0f, 0.38f, 0f), new Vector3(1f, 0.76f, 0.22f), materials.Accent);
        CreateBench(root, "Bench_North", new Vector3(0f, 0f, 0.85f), 0f, materials.Wood, materials.Accent, 1.7f);
        CreateBench(root, "Bench_South", new Vector3(0f, 0f, -0.85f), 180f, materials.Wood, materials.Accent, 1.7f);
    }

    private static void CreateQueueRail(Transform parent, string name, Vector3 localPosition, float yaw, float length, MaterialSet materials)
    {
        Transform root = CreateGroup(parent, name, localPosition, Quaternion.Euler(0f, yaw, 0f));
        CreateBox(root, "LeftPost", new Vector3(-length * 0.5f, 0.5f, 0f), new Vector3(0.1f, 1f, 0.1f), materials.Accent);
        CreateBox(root, "RightPost", new Vector3(length * 0.5f, 0.5f, 0f), new Vector3(0.1f, 1f, 0.1f), materials.Accent);
        CreateBox(root, "Rail", new Vector3(0f, 0.82f, 0f), new Vector3(length, 0.08f, 0.08f), materials.Accent);
    }

    private static void CreateStall(Transform parent, string name, Vector3 localPosition, MaterialSet materials)
    {
        Transform root = CreateGroup(parent, name, localPosition);
        CreateBox(root, "Partition_Left", new Vector3(-0.55f, 1.05f, 0f), new Vector3(0.08f, 2.1f, 1.3f), materials.Wall);
        CreateBox(root, "Partition_Right", new Vector3(0.55f, 1.05f, 0f), new Vector3(0.08f, 2.1f, 1.3f), materials.Wall);
        CreateBox(root, "ToiletBlock", new Vector3(0f, 0.42f, 0.2f), new Vector3(0.65f, 0.84f, 0.8f), materials.Accent);
        CreateDoorPanel(root, "Door", new Vector3(0.22f, 1.04f, -0.62f), 18f, materials.Wood);
    }

    private static Transform CreateStairs(Transform parent, string name, Vector3 localPosition, Vector3 size, int steps, float yaw, Material material)
    {
        ProBuilderMesh mesh = ShapeGenerator.GenerateStair(PivotLocation.Center, SanitizeSize(size), Mathf.Max(4, steps), true);
        GameObject stairObject = mesh.gameObject;
        stairObject.name = name;
        Undo.RegisterCreatedObjectUndo(stairObject, $"Create {name}");
        stairObject.transform.SetParent(parent, false);
        stairObject.transform.localPosition = localPosition;
        stairObject.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        stairObject.transform.localScale = Vector3.one;

        MeshRenderer renderer = stairObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        MeshCollider meshCollider = stairObject.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = stairObject.AddComponent<MeshCollider>();
        }

        MeshFilter filter = stairObject.GetComponent<MeshFilter>();
        meshCollider.sharedMesh = filter != null ? filter.sharedMesh : null;
        GameObjectUtility.SetStaticEditorFlags(stairObject, GeneratedStaticFlags);
        return stairObject.transform;
    }

    private static Transform CreateBox(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 size,
        Material material,
        Quaternion? localRotation = null,
        bool addCollider = true,
        bool setStatic = true)
    {
        ProBuilderMesh mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, SanitizeSize(size));
        GameObject meshObject = mesh.gameObject;
        meshObject.name = name;
        Undo.RegisterCreatedObjectUndo(meshObject, $"Create {name}");
        meshObject.transform.SetParent(parent, false);
        meshObject.transform.localPosition = localPosition;
        meshObject.transform.localRotation = localRotation ?? Quaternion.identity;
        meshObject.transform.localScale = Vector3.one;

        MeshRenderer renderer = meshObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        if (addCollider)
        {
            BoxCollider collider = meshObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = meshObject.AddComponent<BoxCollider>();
            }

            collider.size = SanitizeSize(size);
            collider.center = Vector3.zero;
        }

        if (setStatic)
        {
            GameObjectUtility.SetStaticEditorFlags(meshObject, GeneratedStaticFlags);
        }

        return meshObject.transform;
    }

    private static Transform CreateGroup(Transform parent, string name, Vector3 localPosition = default, Quaternion? localRotation = null)
    {
        GameObject group = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(group, $"Create {name}");
        group.transform.SetParent(parent, false);
        group.transform.localPosition = localPosition;
        group.transform.localRotation = localRotation ?? Quaternion.identity;
        return group.transform;
    }

    private static Transform ResetGeneratedRoot(Transform parent, string rootName)
    {
        Transform existing = parent.Find(rootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        return CreateGroup(parent, rootName);
    }

    private static void ClearGeneratedInteriors(Transform campus)
    {
        DestroyGeneratedRoot(campus, "PB_TeachingBuilding", TeachingInteriorRootName);
        DestroyGeneratedRoot(campus, "PB_DormitoryZone", DormitoryInteriorRootName);
        DestroyGeneratedRoot(campus, "PB_CafeteriaZone", CafeteriaInteriorRootName);
    }

    private static void DestroyGeneratedRoot(Transform campus, string parentName, string generatedRootName)
    {
        Transform parent = FindChildRecursive(campus, parentName);
        if (parent == null)
        {
            return;
        }

        Transform generatedRoot = parent.Find(generatedRootName);
        if (generatedRoot != null)
        {
            Undo.DestroyObjectImmediate(generatedRoot.gameObject);
        }
    }

    private static void DisableTeachingPlaceholders(Transform campus)
    {
        SetTeachingPlaceholderState(campus, false);
    }

    private static void RestoreTeachingPlaceholders(Transform campus)
    {
        SetTeachingPlaceholderState(campus, true);
    }

    private static void SetTeachingPlaceholderState(Transform campus, bool isActive)
    {
        foreach (string placeholderName in TeachingPlaceholderNames)
        {
            Transform placeholder = FindChildRecursive(campus, placeholderName);
            if (placeholder != null && placeholder.gameObject.activeSelf != isActive)
            {
                Undo.RecordObject(placeholder.gameObject, isActive ? $"Enable {placeholderName}" : $"Disable {placeholderName}");
                placeholder.gameObject.SetActive(isActive);
                EditorUtility.SetDirty(placeholder.gameObject);
            }
        }
    }

    private static bool TryOpenSchoolScene(bool interactive)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path == ScenePath)
        {
            return true;
        }

        if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return false;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return true;
    }

    private static Transform GetCampusRoot()
    {
        GameObject campusRoot = GameObject.Find(CampusRootName);
        if (campusRoot == null)
        {
            throw new InvalidOperationException("PB_SchoolCampus was not found in the active scene.");
        }

        return campusRoot.transform;
    }

    private static MaterialSet LoadMaterials()
    {
        const string basePath = "Assets/Res/Materials/PrototypeRaidVariants/School/";

        return new MaterialSet
        {
            Wall = LoadMaterial(basePath + "Mat_SchoolWall.mat"),
            Floor = LoadMaterial(basePath + "Mat_SchoolFloor.mat"),
            Ceiling = LoadMaterial(basePath + "Mat_SchoolCeiling.mat"),
            Wood = LoadMaterial(basePath + "Mat_SchoolWood.mat"),
            Board = LoadMaterial(basePath + "Mat_SchoolBoard.mat"),
            Accent = LoadMaterial(basePath + "Mat_SchoolAccent.mat"),
            Locker = LoadMaterial(basePath + "Mat_SchoolLocker.mat"),
            Glass = LoadMaterial(basePath + "Mat_SchoolGlass.mat")
        };
    }

    private static Material LoadMaterial(string assetPath)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        return material != null ? material : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform match = FindChildRecursive(child, name);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static Vector3 SanitizeSize(Vector3 size)
    {
        size.x = Mathf.Max(0.08f, Mathf.Abs(size.x));
        size.y = Mathf.Max(0.08f, Mathf.Abs(size.y));
        size.z = Mathf.Max(0.08f, Mathf.Abs(size.z));
        return size;
    }

    private static void FinalizeSceneChanges(Scene scene, bool saveScene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        if (saveScene)
        {
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }

    private static string GetProjectRelativeAbsolutePath(string relativePath)
    {
        return Path.GetFullPath(relativePath);
    }

    private static void WriteAutomationResult(string resultPath, string status, string message)
    {
        string directory = Path.GetDirectoryName(resultPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(resultPath, $"{status}{Environment.NewLine}{message}");
    }
}
