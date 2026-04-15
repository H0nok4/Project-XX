using ProjectXX.Domain.Raid;
using UnityEngine;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXPlayerFacade))]
    [RequireComponent(typeof(ProjectXXWeaponBridge))]
    public sealed class ProjectXXDamageBridge : MonoBehaviour
    {
        [SerializeField] private RaidSessionRuntime sessionRuntime;

        private ProjectXXPlayerFacade playerFacade;
        private ProjectXXWeaponBridge weaponBridge;

        private void Awake()
        {
            playerFacade = GetComponent<ProjectXXPlayerFacade>();
            weaponBridge = GetComponent<ProjectXXWeaponBridge>();
        }

        private void LateUpdate()
        {
            ForceSync();
        }

        public void ForceSync()
        {
            if (playerFacade == null || playerFacade.Damageable == null)
            {
                return;
            }

            if (sessionRuntime == null)
            {
                sessionRuntime = FindFirstObjectByType<RaidSessionRuntime>();
                if (sessionRuntime == null)
                {
                    return;
                }
            }

            float maxHealth = playerFacade.Damageable.maxHealth > 0f
                ? playerFacade.Damageable.maxHealth
                : playerFacade.Damageable.health;

            sessionRuntime.UpdatePlayerState(
                playerFacade.DisplayName,
                maxHealth,
                playerFacade.Damageable.health,
                playerFacade.Damageable.health <= 0f || playerFacade.Damageable.DeadConfirmed,
                weaponBridge != null ? weaponBridge.WeaponName : "Unarmed",
                weaponBridge != null ? weaponBridge.CurrentAmmoInMagazine : 0,
                weaponBridge != null ? weaponBridge.CurrentReserveAmmo : 0);
        }

        public void SetSessionRuntime(RaidSessionRuntime runtime)
        {
            sessionRuntime = runtime;
        }
    }
}
