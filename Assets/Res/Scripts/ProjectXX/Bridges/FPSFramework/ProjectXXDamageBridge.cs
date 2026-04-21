using ProjectXX.Domain.Raid;
using ProjectXX.Domain.Combat;
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
        private ProjectXXCombatant combatant;

        private void Awake()
        {
            playerFacade = GetComponent<ProjectXXPlayerFacade>();
            weaponBridge = GetComponent<ProjectXXWeaponBridge>();
            combatant = GetComponent<ProjectXXCombatant>();
        }

        private void LateUpdate()
        {
            ForceSync();
        }

        public void ForceSync()
        {
            if (playerFacade == null)
            {
                return;
            }

            combatant ??= playerFacade.Combatant != null
                ? playerFacade.Combatant
                : GetComponent<ProjectXXCombatant>();
            if (combatant == null)
            {
                return;
            }

            if (sessionRuntime == null)
            {
                return;
            }

            sessionRuntime.UpdatePlayerState(
                playerFacade.DisplayName,
                combatant.MaxHealth,
                combatant.CurrentHealth,
                combatant.IsDead,
                weaponBridge != null ? weaponBridge.WeaponName : "Unarmed",
                weaponBridge != null ? weaponBridge.CurrentAmmoInMagazine : 0,
                weaponBridge != null ? weaponBridge.CurrentReserveAmmo : 0);
        }

        public void SetSessionRuntime(RaidSessionRuntime runtime)
        {
            sessionRuntime = runtime;
            ForceSync();
        }
    }
}
