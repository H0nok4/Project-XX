using Akila.FPSFramework;
using ProjectXX.Domain.Inventory;
using UnityEngine;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXEquipmentRuntime))]
    public sealed class ProjectXXEquipmentBridge : MonoBehaviour
    {
        [SerializeField] private ProjectXXEquipmentRuntime equipmentRuntime;
        [SerializeField] private ProjectXXEquippableItemDefinition startingWeaponDefinition;
        [SerializeField] private InventoryItem startingWeaponPrefab;

        public ProjectXXEquipmentRuntime EquipmentRuntime => equipmentRuntime;
        public ProjectXXEquippableItemDefinition StartingWeaponDefinition => startingWeaponDefinition;
        public InventoryItem StartingWeaponPrefab => startingWeaponPrefab;

        private void Awake()
        {
            EnsureEquipmentRuntime();
            ApplyStartingWeaponDefinition();
        }

        public void SetStartingWeapon(ProjectXXEquippableItemDefinition weaponDefinition, InventoryItem weaponPrefab = null)
        {
            startingWeaponDefinition = weaponDefinition;

            if (weaponPrefab != null)
            {
                startingWeaponPrefab = weaponPrefab;
            }

            ApplyStartingWeaponDefinition();
        }

        public void SetStartingWeapon(InventoryItem weaponPrefab)
        {
            startingWeaponPrefab = weaponPrefab;
            ApplyStartingWeaponDefinition();
        }

        private void ApplyStartingWeaponDefinition()
        {
            EnsureEquipmentRuntime();
            if (equipmentRuntime == null || startingWeaponDefinition == null)
            {
                return;
            }

            equipmentRuntime.Equip(startingWeaponDefinition);
        }

        private void EnsureEquipmentRuntime()
        {
            if (equipmentRuntime == null)
            {
                equipmentRuntime = GetComponent<ProjectXXEquipmentRuntime>();
            }
        }
    }
}
