using System;
using System.Collections.Generic;
using ProjectXX.Domain.Meta;
using UnityEngine;

namespace ProjectXX.Domain.Raid
{
    [DisallowMultipleComponent]
    public sealed class RaidSessionRuntime : MonoBehaviour
    {
        [SerializeField] private PlayerProfileRuntime playerProfile = new PlayerProfileRuntime();
        [SerializeField] private RaidPlayerRuntime playerRuntime = new RaidPlayerRuntime();
        [SerializeField] private int registeredEnemyCount;
        [SerializeField] private int aliveEnemyCount;
        [SerializeField] private int killedEnemyCount;
        [SerializeField] private bool playerInsideExtractionZone;
        [SerializeField] private bool extractedSuccessfully;

        private readonly HashSet<int> registeredEnemies = new HashSet<int>();
        private readonly HashSet<int> killedEnemies = new HashSet<int>();

        public event Action StateChanged;

        public PlayerProfileRuntime PlayerProfile => playerProfile;
        public RaidPlayerRuntime PlayerRuntime => playerRuntime;
        public int RegisteredEnemyCount => registeredEnemyCount;
        public int AliveEnemyCount => aliveEnemyCount;
        public int KilledEnemyCount => killedEnemyCount;
        public bool PlayerInsideExtractionZone => playerInsideExtractionZone;
        public bool ExtractedSuccessfully => extractedSuccessfully;

        public void UpdatePlayerState(
            string displayName,
            float maxHealth,
            float currentHealth,
            bool dead,
            string weaponName,
            int ammoInMagazine,
            int reserveAmmo)
        {
            string resolvedDisplayName = string.IsNullOrWhiteSpace(displayName) ? "Operator" : displayName.Trim();
            float resolvedMaxHealth = Mathf.Max(1f, maxHealth);
            float resolvedCurrentHealth = Mathf.Clamp(currentHealth, 0f, resolvedMaxHealth);
            string resolvedWeaponName = string.IsNullOrWhiteSpace(weaponName) ? "Unarmed" : weaponName.Trim();
            int resolvedAmmoInMagazine = Mathf.Max(0, ammoInMagazine);
            int resolvedReserveAmmo = Mathf.Max(0, reserveAmmo);

            if (playerRuntime.DisplayName == resolvedDisplayName &&
                Mathf.Approximately(playerRuntime.MaxHealth, resolvedMaxHealth) &&
                Mathf.Approximately(playerRuntime.CurrentHealth, resolvedCurrentHealth) &&
                playerRuntime.Dead == dead &&
                playerRuntime.WeaponName == resolvedWeaponName &&
                playerRuntime.AmmoInMagazine == resolvedAmmoInMagazine &&
                playerRuntime.ReserveAmmo == resolvedReserveAmmo)
            {
                return;
            }

            playerRuntime.UpdateState(
                resolvedDisplayName,
                resolvedMaxHealth,
                resolvedCurrentHealth,
                dead,
                resolvedWeaponName,
                resolvedAmmoInMagazine,
                resolvedReserveAmmo);

            NotifyStateChanged();
        }

        public void RegisterEnemy(int instanceId)
        {
            if (!registeredEnemies.Add(instanceId))
            {
                return;
            }

            registeredEnemyCount = registeredEnemies.Count;
            aliveEnemyCount = Mathf.Max(0, registeredEnemyCount - killedEnemies.Count);
            NotifyStateChanged();
        }

        public void NotifyEnemyKilled(int instanceId)
        {
            if (!registeredEnemies.Contains(instanceId) || !killedEnemies.Add(instanceId))
            {
                return;
            }

            killedEnemyCount = killedEnemies.Count;
            aliveEnemyCount = Mathf.Max(0, registeredEnemyCount - killedEnemyCount);
            NotifyStateChanged();
        }

        public void SetPlayerInsideExtractionZone(bool insideZone)
        {
            if (playerInsideExtractionZone == insideZone)
            {
                return;
            }

            playerInsideExtractionZone = insideZone;
            NotifyStateChanged();
        }

        public bool TryExtract()
        {
            if (!playerInsideExtractionZone || playerRuntime.Dead)
            {
                return false;
            }

            extractedSuccessfully = true;
            NotifyStateChanged();
            return true;
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
