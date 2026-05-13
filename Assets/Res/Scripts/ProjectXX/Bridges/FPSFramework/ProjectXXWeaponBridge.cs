using Akila.FPSFramework;
using ProjectXX.Domain.Inventory;
using UnityEngine;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXPlayerFacade))]
    public sealed class ProjectXXWeaponBridge : MonoBehaviour
    {
        [SerializeField] private ProjectXXEquippableItemDefinition startingWeaponDefinition;
        [SerializeField] private InventoryItem startingWeaponPrefab;
        [SerializeField] private bool suppressFrameworkHud = true;

        private ProjectXXPlayerFacade playerFacade;
        private Firearm currentFirearm;

        public Firearm CurrentFirearm => currentFirearm;
        public ProjectXXEquippableItemDefinition CurrentWeaponDefinition { get; private set; }
        public string WeaponName => currentFirearm != null
            ? currentFirearm.Name
            : CurrentWeaponDefinition != null
                ? CurrentWeaponDefinition.DisplayName
                : "Unarmed";
        public int CurrentAmmoInMagazine => currentFirearm != null ? currentFirearm.remainingAmmoCount : 0;
        public int CurrentReserveAmmo => currentFirearm != null ? currentFirearm.remainingAmmoTypeCount : 0;

        private void Awake()
        {
            playerFacade = GetComponent<ProjectXXPlayerFacade>();
            CurrentWeaponDefinition = startingWeaponDefinition;
            EnsureStartingWeapon();
        }

        private void Update()
        {
            if (playerFacade == null)
            {
                return;
            }

            if (playerFacade.TryGetCurrentFirearm(out Firearm firearm))
            {
                currentFirearm = firearm;
                CurrentWeaponDefinition ??= startingWeaponDefinition;
                currentFirearm.isHudActive = !suppressFrameworkHud;
                return;
            }

            currentFirearm = null;
        }

        public void SetStartingWeapon(InventoryItem weaponPrefab)
        {
            startingWeaponPrefab = weaponPrefab;
            CurrentWeaponDefinition ??= startingWeaponDefinition;
            EnsureStartingWeapon();
        }

        public void SetStartingWeapon(ProjectXXEquippableItemDefinition weaponDefinition, InventoryItem presentationPrefab = null)
        {
            startingWeaponDefinition = weaponDefinition;
            CurrentWeaponDefinition = weaponDefinition;

            if (presentationPrefab != null)
            {
                startingWeaponPrefab = presentationPrefab;
            }

            EnsureStartingWeapon();
        }

        private void EnsureStartingWeapon()
        {
            if (playerFacade == null)
            {
                playerFacade = GetComponent<ProjectXXPlayerFacade>();
            }

            Inventory inventory = playerFacade != null ? playerFacade.Inventory : null;
            if (inventory == null || startingWeaponPrefab == null)
            {
                return;
            }

            if (inventory.startItems.Count == 0)
            {
                inventory.startItems.Add(startingWeaponPrefab);
            }

            inventory.maxSlots = Mathf.Max(2, inventory.maxSlots);
        }
    }
}
