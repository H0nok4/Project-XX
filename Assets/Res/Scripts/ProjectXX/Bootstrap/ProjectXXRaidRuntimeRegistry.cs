using System.Collections.Generic;
using ProjectXX.Bridges.FPSFramework;
using ProjectXX.Bridges.JUTPS;
using ProjectXX.Domain.Raid;
using ProjectXX.Presentation.Raid;
using UnityEngine;

namespace ProjectXX.Bootstrap
{
    [AddComponentMenu("ProjectXX/Bootstrap/ProjectXX Raid Runtime Registry")]
    [DisallowMultipleComponent]
    public sealed class ProjectXXRaidRuntimeRegistry : MonoBehaviour
    {
        [SerializeField] private RaidSessionRuntime sessionRuntime;
        [SerializeField] private ProjectXXPlayerFacade playerFacade;
        [SerializeField] private ProjectXXWeaponBridge playerWeaponBridge;
        [SerializeField] private ProjectXXRaidHudController hudController;
        [SerializeField] private ProjectXXExtractionPoint extractionPoint;
        [SerializeField] private List<JutpsEnemyBridge> enemyBridges = new List<JutpsEnemyBridge>();

        public RaidSessionRuntime SessionRuntime => sessionRuntime;
        public ProjectXXPlayerFacade PlayerFacade => playerFacade;
        public ProjectXXWeaponBridge PlayerWeaponBridge => playerWeaponBridge;
        public ProjectXXRaidHudController HudController => hudController;
        public ProjectXXExtractionPoint ExtractionPoint => extractionPoint;
        public IReadOnlyList<JutpsEnemyBridge> EnemyBridges => enemyBridges;

        public void SetSessionRuntime(RaidSessionRuntime runtime)
        {
            sessionRuntime = runtime;
        }

        public void SetPlayer(ProjectXXPlayerFacade facade, ProjectXXWeaponBridge weaponBridge)
        {
            playerFacade = facade;
            playerWeaponBridge = weaponBridge != null
                ? weaponBridge
                : facade != null
                    ? facade.GetComponent<ProjectXXWeaponBridge>()
                    : null;
        }

        public void SetHudController(ProjectXXRaidHudController controller)
        {
            hudController = controller;
        }

        public void SetExtractionPoint(ProjectXXExtractionPoint point)
        {
            extractionPoint = point;
        }

        public void ClearEnemies()
        {
            enemyBridges.Clear();
        }

        public void RegisterEnemy(JutpsEnemyBridge enemyBridge)
        {
            if (enemyBridge == null || enemyBridges.Contains(enemyBridge))
            {
                return;
            }

            enemyBridges.Add(enemyBridge);
        }
    }
}
