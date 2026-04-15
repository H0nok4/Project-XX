using System;
using UnityEngine;

namespace ProjectXX.Domain.Raid
{
    [Serializable]
    public sealed class RaidPlayerRuntime
    {
        [SerializeField] private string displayName = "Operator";
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private bool dead;
        [SerializeField] private string weaponName = "Unarmed";
        [SerializeField] private int ammoInMagazine;
        [SerializeField] private int reserveAmmo;

        public string DisplayName => displayName;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool Dead => dead;
        public string WeaponName => weaponName;
        public int AmmoInMagazine => ammoInMagazine;
        public int ReserveAmmo => reserveAmmo;

        public void UpdateState(
            string newDisplayName,
            float newMaxHealth,
            float newCurrentHealth,
            bool isDead,
            string newWeaponName,
            int newAmmoInMagazine,
            int newReserveAmmo)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName) ? "Operator" : newDisplayName.Trim();
            maxHealth = Mathf.Max(1f, newMaxHealth);
            currentHealth = Mathf.Clamp(newCurrentHealth, 0f, maxHealth);
            dead = isDead;
            weaponName = string.IsNullOrWhiteSpace(newWeaponName) ? "Unarmed" : newWeaponName.Trim();
            ammoInMagazine = Mathf.Max(0, newAmmoInMagazine);
            reserveAmmo = Mathf.Max(0, newReserveAmmo);
        }
    }
}
