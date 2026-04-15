using Akila.FPSFramework;
using UnityEngine;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    public sealed class ProjectXXEquipmentBridge : MonoBehaviour
    {
        [SerializeField] private InventoryItem startingWeaponPrefab;

        public InventoryItem StartingWeaponPrefab => startingWeaponPrefab;

        public void SetStartingWeapon(InventoryItem weaponPrefab)
        {
            startingWeaponPrefab = weaponPrefab;
        }
    }
}
