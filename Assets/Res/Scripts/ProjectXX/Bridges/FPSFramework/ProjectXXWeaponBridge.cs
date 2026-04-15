using Akila.FPSFramework;
using UnityEngine;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXPlayerFacade))]
    public sealed class ProjectXXWeaponBridge : MonoBehaviour
    {
        [SerializeField] private InventoryItem startingWeaponPrefab;
        [SerializeField] private bool suppressFrameworkHud = true;

        private ProjectXXPlayerFacade playerFacade;
        private Firearm currentFirearm;

        public Firearm CurrentFirearm => currentFirearm;
        public string WeaponName => currentFirearm != null ? currentFirearm.Name : "Unarmed";
        public int CurrentAmmoInMagazine => currentFirearm != null ? currentFirearm.remainingAmmoCount : 0;
        public int CurrentReserveAmmo => currentFirearm != null ? currentFirearm.remainingAmmoTypeCount : 0;

        private void Awake()
        {
            playerFacade = GetComponent<ProjectXXPlayerFacade>();
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
                currentFirearm.isHudActive = !suppressFrameworkHud;
                return;
            }

            currentFirearm = null;
        }

        public void SetStartingWeapon(InventoryItem weaponPrefab)
        {
            startingWeaponPrefab = weaponPrefab;
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
