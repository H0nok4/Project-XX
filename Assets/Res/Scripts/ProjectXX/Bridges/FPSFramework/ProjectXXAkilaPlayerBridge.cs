using Akila.FPSFramework;
using JUTPS;
using ProjectXX.Bridges.Combat;
using ProjectXX.Domain.Raid;
using ProjectXX.Domain.Combat;
using ProjectXX.Domain.Inventory;
using ProjectXX.Foundation;
using ProjectXX.Bridges.JUTPS;
using UnityEngine;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXPlayerFacade))]
    [RequireComponent(typeof(ProjectXXCharacterStatBridge))]
    [RequireComponent(typeof(ProjectXXCharacterBuffBridge))]
    [RequireComponent(typeof(ProjectXXEquipmentBridge))]
        [RequireComponent(typeof(ProjectXXWeaponBridge))]
        [RequireComponent(typeof(ProjectXXDamageBridge))]
        [RequireComponent(typeof(ProjectXXCombatant))]
        [RequireComponent(typeof(ProjectXXCombatantSync))]
        [RequireComponent(typeof(ProjectXXEquipmentRuntime))]
        [RequireComponent(typeof(ProjectXXFactionMember))]
        [RequireComponent(typeof(JUHealth))]
        [RequireComponent(typeof(JutpsTargetAdapter))]
    public sealed class ProjectXXAkilaPlayerBridge : MonoBehaviour
    {
        [SerializeField] private RaidSessionRuntime sessionRuntime;

        private ProjectXXPlayerFacade playerFacade;
        private ProjectXXCharacterStatBridge statBridge;
        private ProjectXXEquipmentBridge equipmentBridge;
        private ProjectXXWeaponBridge weaponBridge;
        private ProjectXXDamageBridge damageBridge;
        private ProjectXXCombatant combatant;

        private void Awake()
        {
            playerFacade = GetComponent<ProjectXXPlayerFacade>();
            statBridge = GetComponent<ProjectXXCharacterStatBridge>();
            equipmentBridge = GetComponent<ProjectXXEquipmentBridge>();
            weaponBridge = GetComponent<ProjectXXWeaponBridge>();
            damageBridge = GetComponent<ProjectXXDamageBridge>();
            combatant = GetComponent<ProjectXXCombatant>();

            ConfigureHealth();
            ConfigureWeapon();
            EnsureJutpsCompatibility();
        }

        public void SetSessionRuntime(RaidSessionRuntime runtime)
        {
            sessionRuntime = runtime;
            if (damageBridge != null)
            {
                damageBridge.SetSessionRuntime(runtime);
            }

            ConfigureHealth();
            EnsureJutpsCompatibility();
            damageBridge?.ForceSync();
        }

        private void ConfigureHealth()
        {
            if (playerFacade == null || playerFacade.Damageable == null)
            {
                return;
            }

            float baseMaxHealth = sessionRuntime != null
                ? sessionRuntime.PlayerProfile.BaseMaxHealth
                : 100f;

            float resolvedMaxHealth = statBridge != null
                ? statBridge.ResolveMaxHealth(baseMaxHealth)
                : baseMaxHealth;

            combatant?.Configure(resolvedMaxHealth, resolvedMaxHealth, true);
            playerFacade.Damageable.autoHeal = false;
            playerFacade.Damageable.allowDamageableEffects = false;
            playerFacade.Damageable.allowRespawn = false;

            if (TryGetComponent(out Actor actor))
            {
                actor.respawnable = false;
                actor.playerUIEnabled = false;
                actor.playerCardActive = false;
            }

            if (TryGetComponent(out ProjectXXFactionMember factionMember))
            {
                factionMember.SetFaction(ProjectXXFaction.Player, true);
            }
        }

        private void ConfigureWeapon()
        {
            if (weaponBridge == null || equipmentBridge == null)
            {
                return;
            }

            if (equipmentBridge.StartingWeaponDefinition != null)
            {
                weaponBridge.SetStartingWeapon(equipmentBridge.StartingWeaponDefinition, equipmentBridge.StartingWeaponPrefab);
                return;
            }

            if (equipmentBridge.StartingWeaponPrefab != null)
            {
                weaponBridge.SetStartingWeapon(equipmentBridge.StartingWeaponPrefab);
            }
        }

        private void EnsureJutpsCompatibility()
        {
            if (playerFacade == null)
            {
                return;
            }

            JUHealth juHealth = playerFacade.JutpsHealth;
            if (juHealth == null)
            {
                ProjectXXLog.Error("ProjectXXAkilaPlayerBridge requires JUHealth on the formal player prefab.", this);
                return;
            }

            juHealth.BloodScreenEffect = false;
            float maxHealth = combatant != null ? combatant.MaxHealth : playerFacade.Damageable.maxHealth;
            float currentHealth = combatant != null ? combatant.CurrentHealth : playerFacade.Damageable.health;
            juHealth.SetHealthState(currentHealth, maxHealth, true);

            if (!TryGetComponent(out JutpsTargetAdapter targetAdapter))
            {
                ProjectXXLog.Error("ProjectXXAkilaPlayerBridge requires JutpsTargetAdapter on the formal player prefab.", this);
                return;
            }

            targetAdapter.RefreshTargetSettings();
            ProjectXXLog.Info("Akila player configured for JUTPS compatibility.", this);
        }
    }
}
