using System.Collections.Generic;
using Akila.FPSFramework;
using JUTPS;
using ProjectXX.Bridges.FPSFramework;
using ProjectXX.Bridges.Combat;
using ProjectXX.Bridges.JUTPS;
using ProjectXX.Domain.Combat;
using ProjectXX.Domain.Interaction;
using ProjectXX.Domain.Inventory;
using ProjectXX.Domain.Raid;
using ProjectXX.Foundation;
using ProjectXX.Infrastructure.Definitions;
using ProjectXX.Presentation.Raid;
using UnityEngine;

namespace ProjectXX.Bootstrap
{
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class ProjectXXRaidSceneInstaller : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private ProjectXXRaidRuntimeRegistry runtimeRegistry;
        [SerializeField] private RaidSessionRuntime sessionRuntime;
        [SerializeField] private ProjectXXCompatibilitySettings compatibilitySettings;

        [Header("Authored Scene References")]
        [SerializeField] private ProjectXXPlayerFacade authoredPlayer;
        [SerializeField] private ProjectXXRaidHudController authoredHud;
        [SerializeField] private JutpsEnemyBridge[] authoredEnemies;

        [Header("Authored Akila Managers")]
        [SerializeField] private SpawnManager authoredSpawnManager;
        [SerializeField] private SettingsManager authoredSettingsManager;
        [SerializeField] private GamepadManager authoredGamepadManager;
        [SerializeField] private GameManager authoredGameManager;

        [Header("Framework Prefabs")]
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private ProjectXXEquippableItemDefinition startingWeaponDefinition;
        [SerializeField] private InventoryItem startingWeaponPrefab;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private ProjectXXEnemyDefinition enemyDefinition;

        [Header("Spawn Anchors")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private ProjectXXExtractionPoint extractionPoint;

        [Header("R2 Interaction")]
        [SerializeField] private ProjectXXContainerRuntime[] authoredContainers;
        [SerializeField] private ProjectXXContainerDefinition demoLootContainerDefinition;
        [SerializeField] private ProjectXXItemDefinition demoLootItemDefinition;
        [SerializeField] private bool spawnDemoLootContainer = true;
        [SerializeField, Min(1)] private int demoLootItemQuantity = 3;
        [SerializeField] private Vector3 demoLootContainerPosition = new Vector3(3f, 0.5f, 3f);
        [SerializeField] private Vector3 demoLootContainerScale = new Vector3(1.2f, 0.8f, 1f);

        [Header("Flow")]
        [SerializeField] private bool useMinimalFrameworkManagers = true;
        [SerializeField] private bool spawnOnStart = true;

        private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
        private ProjectXXItemDefinition runtimeDemoLootItemDefinition;
        private bool compatibilityValidated;

        private void Start()
        {
            if (!spawnOnStart)
            {
                return;
            }

            BuildSlice();
        }

        [ContextMenu("Build R1 Slice")]
        public void BuildSlice()
        {
            EnsureRuntimeRegistry();
            EnsureCompatibilitySettings();
            EnsureRuntime();
            EnsureGameManager();

            ProjectXXPlayerFacade player = EnsurePlayer();
            EnsureHud(player);
            EnsureExtraction();
            EnsureContainers();
            EnsureEnemies();

            ProjectXXLog.Info("Raid test slice installed.", this);
        }

        private void Awake()
        {
            EnsureRuntimeRegistry();
            EnsureCompatibilitySettings();
        }

        private void EnsureRuntimeRegistry()
        {
            if (runtimeRegistry == null)
            {
                runtimeRegistry = GetComponent<ProjectXXRaidRuntimeRegistry>();
            }

            if (runtimeRegistry != null)
            {
                return;
            }

            GameObject registryRoot = new GameObject("ProjectXXRaidRuntimeRegistry");
            registryRoot.transform.SetParent(transform, false);
            runtimeRegistry = registryRoot.AddComponent<ProjectXXRaidRuntimeRegistry>();
        }

        private void EnsureCompatibilitySettings()
        {
            compatibilitySettings ??= ProjectXXCompatibilitySettingsProvider.GetOrDefault();
            if (compatibilityValidated)
            {
                return;
            }

            compatibilityValidated = true;
            ProjectXXCompatibilityValidator.Validate(compatibilitySettings, this);
        }

        private void EnsureRuntime()
        {
            if (sessionRuntime != null)
            {
                runtimeRegistry?.SetSessionRuntime(sessionRuntime);
                return;
            }

            if (runtimeRegistry != null && runtimeRegistry.SessionRuntime != null)
            {
                sessionRuntime = runtimeRegistry.SessionRuntime;
                return;
            }

            sessionRuntime = FindFirstObjectByType<RaidSessionRuntime>();
            if (sessionRuntime != null)
            {
                runtimeRegistry?.SetSessionRuntime(sessionRuntime);
                return;
            }

            GameObject runtimeRoot = new GameObject("RaidSessionRuntime");
            sessionRuntime = runtimeRoot.AddComponent<RaidSessionRuntime>();
            runtimeRegistry?.SetSessionRuntime(sessionRuntime);
        }

        private void EnsureGameManager()
        {
            if (useMinimalFrameworkManagers)
            {
                authoredSpawnManager ??= FindFirstObjectByType<SpawnManager>();
                authoredSettingsManager ??= FindFirstObjectByType<SettingsManager>();
                authoredGamepadManager ??= FindFirstObjectByType<GamepadManager>();

                if (authoredSpawnManager != null &&
                    authoredSettingsManager != null &&
                    authoredGamepadManager != null)
                {
                    return;
                }

                GameObject managerRoot = new GameObject("ProjectXX Akila Runtime");
                managerRoot.SetActive(false);

                if (authoredSpawnManager == null)
                {
                    authoredSpawnManager = managerRoot.AddComponent<SpawnManager>();
                    Transform fallbackSpawnPoint = playerSpawnPoint != null
                        ? playerSpawnPoint
                        : new GameObject("Fallback Spawn Point").transform;
                    fallbackSpawnPoint.SetPositionAndRotation(
                        playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.up,
                        playerSpawnPoint != null ? playerSpawnPoint.rotation : Quaternion.identity);
                    authoredSpawnManager.sides = new List<SpawnManager.SpwanSide>
                    {
                        new SpawnManager.SpwanSide
                        {
                            points = new[] { fallbackSpawnPoint }
                        }
                    };
                }

                if (authoredSettingsManager == null)
                {
                    authoredSettingsManager = managerRoot.AddComponent<SettingsManager>();
                    authoredSettingsManager.autoApply = false;
                    authoredSettingsManager.settingsPresets = System.Array.Empty<SettingsPreset>();
                }

                if (authoredGamepadManager == null)
                {
                    authoredGamepadManager = managerRoot.AddComponent<GamepadManager>();
                }

                managerRoot.SetActive(true);
                return;
            }

            authoredGameManager ??= FindFirstObjectByType<GameManager>();
            if (authoredGameManager != null || gameManagerPrefab == null)
            {
                return;
            }

            GameObject gameManagerObject = Instantiate(gameManagerPrefab, Vector3.zero, Quaternion.identity);
            authoredGameManager = gameManagerObject.GetComponent<GameManager>();
        }

        private ProjectXXPlayerFacade EnsurePlayer()
        {
            ProjectXXPlayerFacade playerFacade = runtimeRegistry != null ? runtimeRegistry.PlayerFacade : null;
            if (playerFacade == null)
            {
                playerFacade = authoredPlayer != null
                    ? authoredPlayer
                    : FindFirstObjectByType<ProjectXXPlayerFacade>();
            }

            GameObject playerObject = playerFacade != null ? playerFacade.gameObject : null;

            if (playerObject == null)
            {
                if (playerPrefab == null)
                {
                    ProjectXXLog.Error("Player prefab is missing on the raid installer.", this);
                    return null;
                }

                Vector3 spawnPosition = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.up;
                Quaternion spawnRotation = playerSpawnPoint != null ? playerSpawnPoint.rotation : Quaternion.identity;
                playerObject = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            }
            else if (playerSpawnPoint != null)
            {
                playerObject.transform.SetPositionAndRotation(playerSpawnPoint.position, playerSpawnPoint.rotation);
            }

            ResetPlayerView(playerObject.transform);

            playerFacade = RequireExisting<ProjectXXPlayerFacade>(playerObject, "raid player prefab");
            ProjectXXCharacterStatBridge statBridge = RequireExisting<ProjectXXCharacterStatBridge>(playerObject, "raid player prefab");
            RequireExisting<ProjectXXCharacterBuffBridge>(playerObject, "raid player prefab");
            ProjectXXEquipmentBridge equipmentBridge = RequireExisting<ProjectXXEquipmentBridge>(playerObject, "raid player prefab");
            ProjectXXWeaponBridge weaponBridge = RequireExisting<ProjectXXWeaponBridge>(playerObject, "raid player prefab");
            ProjectXXDamageBridge damageBridge = RequireExisting<ProjectXXDamageBridge>(playerObject, "raid player prefab");
            ProjectXXPlayerInteractionBridge interactionBridge = RequireExisting<ProjectXXPlayerInteractionBridge>(playerObject, "raid player prefab");
            ProjectXXLoadoutRuntime loadoutRuntime = RequireExisting<ProjectXXLoadoutRuntime>(playerObject, "raid player prefab");
            ProjectXXLootWindowController lootWindowController = RequireExisting<ProjectXXLootWindowController>(playerObject, "raid player prefab");
            RequireExisting<ProjectXXFirstPersonViewBridge>(playerObject, "raid player prefab");
            RequireExisting<ProjectXXMeleeBridge>(playerObject, "raid player prefab");
            RequireExisting<ProjectXXCombatant>(playerObject, "raid player prefab");
            RequireExisting<ProjectXXCombatantSync>(playerObject, "raid player prefab");
            RequireExisting<JUHealth>(playerObject, "raid player prefab");
            ProjectXXFactionMember playerFaction = RequireExisting<ProjectXXFactionMember>(playerObject, "raid player prefab");
            JutpsTargetAdapter targetAdapter = RequireExisting<JutpsTargetAdapter>(playerObject, "raid player prefab");
            ProjectXXAkilaPlayerBridge playerBridge = RequireExisting<ProjectXXAkilaPlayerBridge>(playerObject, "raid player prefab");
            if (playerFacade == null ||
                statBridge == null ||
                equipmentBridge == null ||
                weaponBridge == null ||
                damageBridge == null ||
                interactionBridge == null ||
                loadoutRuntime == null ||
                lootWindowController == null ||
                playerFaction == null ||
                targetAdapter == null ||
                playerBridge == null)
            {
                return null;
            }

            authoredPlayer = playerFacade;
            ConfigureStartingWeapon(equipmentBridge, weaponBridge);
            damageBridge.SetSessionRuntime(sessionRuntime);
            playerFacade.RefreshReferences();
            playerFaction.SetFaction(ProjectXXFaction.Player, true);
            targetAdapter.RefreshTargetSettings();
            playerBridge.SetSessionRuntime(sessionRuntime);
            lootWindowController.Configure(sessionRuntime, loadoutRuntime);
            interactionBridge.Configure(sessionRuntime, lootWindowController);
            runtimeRegistry?.SetPlayer(playerFacade, weaponBridge);

            ProjectXXCombatant playerCombatant = playerFacade.Combatant;
            if (playerCombatant != null)
            {
                sessionRuntime.UpdatePlayerState(
                    playerFacade.DisplayName,
                    playerCombatant.MaxHealth,
                    playerCombatant.CurrentHealth,
                    playerCombatant.IsDead,
                    weaponBridge.WeaponName,
                    weaponBridge.CurrentAmmoInMagazine,
                    weaponBridge.CurrentReserveAmmo);
            }

            return playerFacade;
        }

        private void ConfigureStartingWeapon(ProjectXXEquipmentBridge equipmentBridge, ProjectXXWeaponBridge weaponBridge)
        {
            if (startingWeaponDefinition != null)
            {
                equipmentBridge.SetStartingWeapon(startingWeaponDefinition, startingWeaponPrefab);
                weaponBridge.SetStartingWeapon(startingWeaponDefinition, startingWeaponPrefab);
                return;
            }

            equipmentBridge.SetStartingWeapon(startingWeaponPrefab);
            weaponBridge.SetStartingWeapon(startingWeaponPrefab);
        }

        private void EnsureHud(ProjectXXPlayerFacade playerFacade)
        {
            if (playerFacade == null)
            {
                return;
            }

            ProjectXXRaidHudController hudController = runtimeRegistry != null ? runtimeRegistry.HudController : null;
            if (hudController == null)
            {
                hudController = authoredHud != null
                    ? authoredHud
                    : FindFirstObjectByType<ProjectXXRaidHudController>();
            }

            if (hudController == null)
            {
                hudController = new GameObject("ProjectXXRaidHudController").AddComponent<ProjectXXRaidHudController>();
            }

            authoredHud = hudController;
            hudController.Configure(sessionRuntime);
            runtimeRegistry?.SetHudController(hudController);
        }

        private void EnsureExtraction()
        {
            if (extractionPoint == null)
            {
                extractionPoint = runtimeRegistry != null ? runtimeRegistry.ExtractionPoint : null;
                if (extractionPoint == null)
                {
                    extractionPoint = FindFirstObjectByType<ProjectXXExtractionPoint>();
                }
            }

            if (extractionPoint != null)
            {
                extractionPoint.Configure(sessionRuntime);
                runtimeRegistry?.SetExtractionPoint(extractionPoint);
            }
        }

        private void EnsureContainers()
        {
            int configuredCount = 0;
            if (authoredContainers != null)
            {
                for (int i = 0; i < authoredContainers.Length; i++)
                {
                    configuredCount += ConfigureContainer(authoredContainers[i]) ? 1 : 0;
                }
            }

            if (configuredCount == 0)
            {
                ProjectXXContainerRuntime[] existingContainers = FindObjectsByType<ProjectXXContainerRuntime>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

                for (int i = 0; i < existingContainers.Length; i++)
                {
                    configuredCount += ConfigureContainer(existingContainers[i]) ? 1 : 0;
                }
            }

            if (configuredCount > 0 || !spawnDemoLootContainer)
            {
                return;
            }

            ProjectXXContainerRuntime demoContainer = CreateDemoLootContainer();
            ConfigureContainer(demoContainer);
        }

        private ProjectXXContainerRuntime CreateDemoLootContainer()
        {
            GameObject containerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            containerObject.name = "ProjectXX Small Loot Crate";
            containerObject.transform.SetPositionAndRotation(demoLootContainerPosition, Quaternion.identity);
            containerObject.transform.localScale = demoLootContainerScale;

            ProjectXXContainerRuntime containerRuntime = containerObject.AddComponent<ProjectXXContainerRuntime>();
            if (demoLootContainerDefinition != null)
            {
                containerRuntime.SetDefinition(demoLootContainerDefinition, true);
            }
            else
            {
                containerRuntime.Configure(
                    "container.loot.small_crate",
                    "Small Loot Crate",
                    new ProjectXXGridSize(6, 4),
                    true);
            }

            ProjectXXItemDefinition lootItemDefinition = ResolveDemoLootItemDefinition();
            if (lootItemDefinition != null)
            {
                containerRuntime.TryAddFirstFit(lootItemDefinition, demoLootItemQuantity, true, out _);
            }

            return containerRuntime;
        }

        private ProjectXXItemDefinition ResolveDemoLootItemDefinition()
        {
            if (demoLootItemDefinition != null)
            {
                return demoLootItemDefinition;
            }

            if (runtimeDemoLootItemDefinition != null)
            {
                return runtimeDemoLootItemDefinition;
            }

            runtimeDemoLootItemDefinition = ScriptableObject.CreateInstance<ProjectXXItemDefinition>();
            runtimeDemoLootItemDefinition.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            runtimeDemoLootItemDefinition.Configure(
                "item.loot.field_rations",
                "Field Rations",
                ProjectXXItemCategory.Consumable,
                6,
                ProjectXXGridSize.One);
            return runtimeDemoLootItemDefinition;
        }

        private static bool ConfigureContainer(ProjectXXContainerRuntime containerRuntime)
        {
            if (containerRuntime == null)
            {
                return false;
            }

            ProjectXXContainerInteractable interactable = containerRuntime.GetComponent<ProjectXXContainerInteractable>();
            if (interactable == null)
            {
                interactable = containerRuntime.gameObject.AddComponent<ProjectXXContainerInteractable>();
            }

            interactable.RefreshReferences();
            return true;
        }

        private void EnsureEnemies()
        {
            runtimeRegistry?.ClearEnemies();
            if (authoredEnemies != null && authoredEnemies.Length > 0)
            {
                bool registeredAnyAuthoredEnemy = false;
                for (int i = 0; i < authoredEnemies.Length; i++)
                {
                    registeredAnyAuthoredEnemy |= ConfigureExistingEnemy(authoredEnemies[i]);
                }

                if (!registeredAnyAuthoredEnemy)
                {
                    ProjectXXLog.Error("Authored enemies are assigned but none have a valid Project-XX/JUTPS bridge setup.", this);
                }

                return;
            }

            JutpsEnemyBridge[] existingEnemies = FindObjectsByType<JutpsEnemyBridge>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (existingEnemies.Length > 0)
            {
                for (int i = 0; i < existingEnemies.Length; i++)
                {
                    ConfigureExistingEnemy(existingEnemies[i]);
                }

                return;
            }

            if (enemyPrefab == null || enemySpawnPoints == null)
            {
                return;
            }

            for (int i = 0; i < enemySpawnPoints.Length; i++)
            {
                Transform spawnPoint = enemySpawnPoints[i];
                if (spawnPoint == null)
                {
                    continue;
                }

                GameObject enemyObject = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                EnsureEnemyProjectXXComponents(enemyObject);
                if (!ValidateEnemyPrefab(enemyObject))
                {
                    ProjectXXLog.Error($"Spawned enemy '{enemyObject.name}' failed Project-XX validation and was destroyed.", this);
                    Destroy(enemyObject);
                    continue;
                }

                JutpsEnemyBridge enemyBridge = enemyObject.GetComponent<JutpsEnemyBridge>();
                if (ConfigureExistingEnemy(enemyBridge))
                {
                    spawnedEnemies.Add(enemyObject);
                }
            }
        }

        private bool ConfigureExistingEnemy(JutpsEnemyBridge enemyBridge)
        {
            if (enemyBridge == null)
            {
                return false;
            }

            EnsureEnemyProjectXXComponents(enemyBridge.gameObject);
            if (!ValidateEnemyPrefab(enemyBridge.gameObject))
            {
                return false;
            }

            enemyBridge.Configure(sessionRuntime, enemyDefinition);
            runtimeRegistry?.RegisterEnemy(enemyBridge);
            return true;
        }

        private bool ValidateEnemyPrefab(GameObject enemyObject)
        {
            return RequireExisting<ProjectXXCombatant>(enemyObject, "enemy prefab") != null &&
                   RequireExisting<ProjectXXCombatantSync>(enemyObject, "enemy prefab") != null &&
                   RequireExisting<JutpsEnemyDamageableAdapter>(enemyObject, "enemy prefab") != null &&
                   RequireExisting<ProjectXXFactionMember>(enemyObject, "enemy prefab") != null &&
                   RequireExisting<JutpsTargetAdapter>(enemyObject, "enemy prefab") != null &&
                   RequireExisting<ProjectXXJutpsFactionBridge>(enemyObject, "enemy prefab") != null &&
                   RequireExisting<JutpsEnemyBridge>(enemyObject, "enemy prefab") != null;
        }

        private static void EnsureEnemyProjectXXComponents(GameObject enemyObject)
        {
            if (enemyObject == null)
            {
                return;
            }

            EnsureComponent<ProjectXXCombatant>(enemyObject);
            EnsureComponent<ProjectXXCombatantSync>(enemyObject);
            EnsureComponent<ProjectXXFactionMember>(enemyObject);
            EnsureComponent<JutpsTargetAdapter>(enemyObject);
            EnsureComponent<ProjectXXJutpsFactionTargetFilter>(enemyObject);
            EnsureComponent<ProjectXXJutpsFactionBridge>(enemyObject);
            EnsureComponent<JutpsEnemyDamageableAdapter>(enemyObject);
            EnsureComponent<JutpsEnemyBridge>(enemyObject);
        }

        private static T EnsureComponent<T>(GameObject target)
            where T : Component
        {
            return target.TryGetComponent(out T component)
                ? component
                : target.AddComponent<T>();
        }

        private static T RequireExisting<T>(GameObject target, string targetLabel)
            where T : Component
        {
            if (target != null && target.TryGetComponent(out T component))
            {
                return component;
            }

            string targetName = target != null ? target.name : "null";
            ProjectXXLog.Error($"{targetLabel} is missing required component {typeof(T).Name} on {targetName}.", target);
            return null;
        }

        private static void ResetPlayerView(Transform playerRoot)
        {
            if (playerRoot == null)
            {
                return;
            }

            Transform cameraRoot = playerRoot.Find("CameraRoot");
            if (cameraRoot != null)
            {
                cameraRoot.localRotation = Quaternion.identity;
            }

            Transform cameraPivot = playerRoot.Find("CameraRoot/CameraPivot");
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.identity;
            }

            Transform viewAnimator = playerRoot.Find("CameraRoot/CameraPivot/ViewAnimator");
            if (viewAnimator != null)
            {
                viewAnimator.localRotation = Quaternion.identity;
            }
        }
    }
}
