using System.Collections.Generic;
using Akila.FPSFramework;
using JUTPS;
using ProjectXX.Bridges.FPSFramework;
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
        [SerializeField] private RaidSessionRuntime sessionRuntime;

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
            EnsureRuntime();
            EnsureGameManager();

            ProjectXXPlayerFacade player = EnsurePlayer();
            EnsureHud(player);
            EnsureExtraction();
            EnsureEnemies();

            ProjectXXLog.Info("Raid test slice installed.", this);
        }

        private void EnsureRuntime()
        {
            if (sessionRuntime != null)
            {
                return;
            }

            sessionRuntime = FindFirstObjectByType<RaidSessionRuntime>();
            if (sessionRuntime != null)
            {
                return;
            }

            GameObject runtimeRoot = new GameObject("RaidSessionRuntime");
            sessionRuntime = runtimeRoot.AddComponent<RaidSessionRuntime>();
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
            ProjectXXPlayerFacade playerFacade = FindFirstObjectByType<ProjectXXPlayerFacade>();
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

            playerFacade = GetOrAdd<ProjectXXPlayerFacade>(playerObject);
            ProjectXXCharacterStatBridge statBridge = GetOrAdd<ProjectXXCharacterStatBridge>(playerObject);
            GetOrAdd<ProjectXXCharacterBuffBridge>(playerObject);

            ProjectXXEquipmentBridge equipmentBridge = GetOrAdd<ProjectXXEquipmentBridge>(playerObject);
            equipmentBridge.SetStartingWeapon(startingWeaponPrefab);

            ProjectXXWeaponBridge weaponBridge = GetOrAdd<ProjectXXWeaponBridge>(playerObject);
            weaponBridge.SetStartingWeapon(startingWeaponPrefab);

            ProjectXXDamageBridge damageBridge = GetOrAdd<ProjectXXDamageBridge>(playerObject);
            damageBridge.SetSessionRuntime(sessionRuntime);

            GetOrAdd<ProjectXXFirstPersonViewBridge>(playerObject);
            GetOrAdd<ProjectXXMeleeBridge>(playerObject);

            GetOrAdd<JUHealth>(playerObject);
            playerFacade.RefreshReferences();
            GetOrAdd<JutpsHealthProxy>(playerObject);
            ProjectXXFactionMember playerFaction = GetOrAdd<ProjectXXFactionMember>(playerObject);
            playerFaction.SetFaction(ProjectXXFaction.Player, true);

            JutpsTargetAdapter targetAdapter = GetOrAdd<JutpsTargetAdapter>(playerObject);
            targetAdapter.RefreshTargetSettings();

            ProjectXXAkilaPlayerBridge playerBridge = GetOrAdd<ProjectXXAkilaPlayerBridge>(playerObject);
            playerBridge.SetSessionRuntime(sessionRuntime);

            float resolvedMaxHealth = statBridge.ResolveMaxHealth(sessionRuntime.PlayerProfile.BaseMaxHealth);
            sessionRuntime.UpdatePlayerState(playerFacade.DisplayName, resolvedMaxHealth, resolvedMaxHealth, false, "Unarmed", 0, 0);

            return playerFacade;
        }

        private void EnsureHud(ProjectXXPlayerFacade playerFacade)
        {
            if (playerFacade == null)
            {
                return;
            }

            ProjectXXRaidHudController hudController = FindFirstObjectByType<ProjectXXRaidHudController>();
            if (hudController == null)
            {
                hudController = new GameObject("ProjectXXRaidHudController").AddComponent<ProjectXXRaidHudController>();
            }

            hudController.Configure(sessionRuntime, playerFacade, playerFacade.GetComponent<ProjectXXWeaponBridge>());
        }

        private void EnsureExtraction()
        {
            if (extractionPoint == null)
            {
                extractionPoint = FindFirstObjectByType<ProjectXXExtractionPoint>();
            }

            if (extractionPoint != null)
            {
                extractionPoint.Configure(sessionRuntime);
            }
        }

        private void EnsureEnemies()
        {
            JutpsEnemyBridge[] existingEnemies = FindObjectsByType<JutpsEnemyBridge>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (existingEnemies.Length > 0)
            {
                for (int i = 0; i < existingEnemies.Length; i++)
                {
                    existingEnemies[i].Configure(sessionRuntime, enemyDefinition);
                    GetOrAdd<JutpsEnemyDamageableAdapter>(existingEnemies[i].gameObject);
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
                GetOrAdd<JutpsEnemyDamageableAdapter>(enemyObject);
                JutpsEnemyBridge enemyBridge = GetOrAdd<JutpsEnemyBridge>(enemyObject);
                enemyBridge.Configure(sessionRuntime, enemyDefinition);
                spawnedEnemies.Add(enemyObject);
            }
        }

        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            if (target.TryGetComponent(out T component))
            {
                return component;
            }

            return target.AddComponent<T>();
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
