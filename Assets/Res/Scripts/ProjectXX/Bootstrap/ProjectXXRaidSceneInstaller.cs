using System.Collections.Generic;
using Akila.FPSFramework;
using JUTPS;
using ProjectXX.Bridges.FPSFramework;
using ProjectXX.Bridges.Combat;
using ProjectXX.Bridges.JUTPS;
using ProjectXX.Domain.Combat;
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

        [Header("Framework Prefabs")]
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private InventoryItem startingWeaponPrefab;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private ProjectXXEnemyDefinition enemyDefinition;

        [Header("Spawn Anchors")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private ProjectXXExtractionPoint extractionPoint;

        [Header("Flow")]
        [SerializeField] private bool useMinimalFrameworkManagers = true;
        [SerializeField] private bool spawnOnStart = true;

        private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
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
                if (FindFirstObjectByType<SpawnManager>() != null &&
                    FindFirstObjectByType<SettingsManager>() != null &&
                    FindFirstObjectByType<GamepadManager>() != null)
                {
                    return;
                }

                GameObject managerRoot = new GameObject("ProjectXX Akila Runtime");
                managerRoot.SetActive(false);

                SpawnManager spawnManager = managerRoot.AddComponent<SpawnManager>();
                Transform fallbackSpawnPoint = playerSpawnPoint != null
                    ? playerSpawnPoint
                    : new GameObject("Fallback Spawn Point").transform;
                fallbackSpawnPoint.SetPositionAndRotation(
                    playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.up,
                    playerSpawnPoint != null ? playerSpawnPoint.rotation : Quaternion.identity);
                spawnManager.sides = new List<SpawnManager.SpwanSide>
                {
                    new SpawnManager.SpwanSide
                    {
                        points = new[] { fallbackSpawnPoint }
                    }
                };

                SettingsManager settingsManager = managerRoot.AddComponent<SettingsManager>();
                settingsManager.autoApply = false;
                settingsManager.settingsPresets = System.Array.Empty<SettingsPreset>();

                managerRoot.AddComponent<GamepadManager>();
                managerRoot.SetActive(true);
                return;
            }

            if (FindFirstObjectByType<GameManager>() != null || gameManagerPrefab == null)
            {
                return;
            }

            Instantiate(gameManagerPrefab, Vector3.zero, Quaternion.identity);
        }

        private ProjectXXPlayerFacade EnsurePlayer()
        {
            ProjectXXPlayerFacade playerFacade = runtimeRegistry != null ? runtimeRegistry.PlayerFacade : null;
            if (playerFacade == null)
            {
                playerFacade = FindFirstObjectByType<ProjectXXPlayerFacade>();
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
                playerFaction == null ||
                targetAdapter == null ||
                playerBridge == null)
            {
                return null;
            }

            equipmentBridge.SetStartingWeapon(startingWeaponPrefab);
            weaponBridge.SetStartingWeapon(startingWeaponPrefab);
            damageBridge.SetSessionRuntime(sessionRuntime);
            playerFacade.RefreshReferences();
            playerFaction.SetFaction(ProjectXXFaction.Player, true);
            targetAdapter.RefreshTargetSettings();
            playerBridge.SetSessionRuntime(sessionRuntime);
            runtimeRegistry?.SetPlayer(playerFacade, weaponBridge);

            ProjectXXCombatant playerCombatant = playerFacade.Combatant;
            if (playerCombatant != null)
            {
                sessionRuntime.UpdatePlayerState(
                    playerFacade.DisplayName,
                    playerCombatant.MaxHealth,
                    playerCombatant.CurrentHealth,
                    playerCombatant.IsDead,
                    "Unarmed",
                    0,
                    0);
            }

            return playerFacade;
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
                hudController = FindFirstObjectByType<ProjectXXRaidHudController>();
            }

            if (hudController == null)
            {
                hudController = new GameObject("ProjectXXRaidHudController").AddComponent<ProjectXXRaidHudController>();
            }

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

        private void EnsureEnemies()
        {
            runtimeRegistry?.ClearEnemies();
            JutpsEnemyBridge[] existingEnemies = FindObjectsByType<JutpsEnemyBridge>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (existingEnemies.Length > 0)
            {
                for (int i = 0; i < existingEnemies.Length; i++)
                {
                    JutpsEnemyBridge enemyBridge = existingEnemies[i];
                    if (!ValidateEnemyPrefab(enemyBridge.gameObject))
                    {
                        continue;
                    }

                    enemyBridge.Configure(sessionRuntime, enemyDefinition);
                    runtimeRegistry?.RegisterEnemy(enemyBridge);
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
                if (!ValidateEnemyPrefab(enemyObject))
                {
                    continue;
                }

                JutpsEnemyBridge enemyBridge = enemyObject.GetComponent<JutpsEnemyBridge>();
                enemyBridge.Configure(sessionRuntime, enemyDefinition);
                runtimeRegistry?.RegisterEnemy(enemyBridge);
                spawnedEnemies.Add(enemyObject);
            }
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
