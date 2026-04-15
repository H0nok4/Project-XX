using Akila.FPSFramework;
using JUTPS;
using ProjectXX.Domain.Raid;
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
    public sealed class ProjectXXAkilaPlayerBridge : MonoBehaviour
    {
        [SerializeField] private RaidSessionRuntime sessionRuntime;

        private ProjectXXPlayerFacade playerFacade;
        private ProjectXXCharacterStatBridge statBridge;
        private ProjectXXEquipmentBridge equipmentBridge;
        private ProjectXXWeaponBridge weaponBridge;
        private ProjectXXDamageBridge damageBridge;

        private void Awake()
        {
            playerFacade = GetComponent<ProjectXXPlayerFacade>();
            statBridge = GetComponent<ProjectXXCharacterStatBridge>();
            equipmentBridge = GetComponent<ProjectXXEquipmentBridge>();
            weaponBridge = GetComponent<ProjectXXWeaponBridge>();
            damageBridge = GetComponent<ProjectXXDamageBridge>();

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

            playerFacade.Damageable.maxHealth = resolvedMaxHealth;
            playerFacade.Damageable.health = resolvedMaxHealth;
            playerFacade.Damageable.autoHeal = false;
            playerFacade.Damageable.allowDamageableEffects = false;
            playerFacade.Damageable.allowRespawn = false;

            if (TryGetComponent(out Actor actor))
            {
                actor.respawnable = false;
                actor.playerUIEnabled = false;
                actor.playerCardActive = false;
            }
        }

        private void ConfigureWeapon()
        {
            if (weaponBridge != null && equipmentBridge != null && equipmentBridge.StartingWeaponPrefab != null)
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
                juHealth = gameObject.AddComponent<JUHealth>();
                playerFacade.RefreshReferences();
            }

            juHealth.BloodScreenEffect = false;
            juHealth.MaxHealth = playerFacade.Damageable.maxHealth;
            juHealth.Health = playerFacade.Damageable.health;
            juHealth.CheckHealthState();

            if (GetComponent<JutpsHealthProxy>() == null)
            {
                gameObject.AddComponent<JutpsHealthProxy>();
            }

            if (GetComponent<JutpsTargetAdapter>() == null)
            {
                gameObject.AddComponent<JutpsTargetAdapter>();
            }

            ProjectXXLog.Info("Akila player configured for JUTPS compatibility.", this);
        }
    }
}
