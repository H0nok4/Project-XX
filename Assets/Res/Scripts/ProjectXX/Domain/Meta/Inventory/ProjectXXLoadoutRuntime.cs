using UnityEngine;

namespace ProjectXX.Domain.Inventory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXEquipmentRuntime))]
    public sealed class ProjectXXLoadoutRuntime : MonoBehaviour
    {
        [SerializeField] private ProjectXXEquipmentRuntime equipmentRuntime;
        [SerializeField] private ProjectXXInventoryGridRuntime backpackGrid = new ProjectXXInventoryGridRuntime(6, 5);
        [SerializeField] private ProjectXXInventoryGridRuntime quickAccessGrid = new ProjectXXInventoryGridRuntime(4, 1);

        public ProjectXXEquipmentRuntime Equipment => equipmentRuntime;
        public ProjectXXInventoryGridRuntime BackpackGrid => backpackGrid ??= new ProjectXXInventoryGridRuntime(6, 5);
        public ProjectXXInventoryGridRuntime QuickAccessGrid => quickAccessGrid ??= new ProjectXXInventoryGridRuntime(4, 1);

        private void Awake()
        {
            EnsureEquipmentRuntime();
        }

        public bool TryAddToBackpack(ProjectXXItemDefinition definition, int quantity, ProjectXXGridCoord origin, bool rotated, out ProjectXXItemPlacementRuntime placement)
        {
            placement = null;
            if (definition == null)
            {
                return false;
            }

            return BackpackGrid.TryAdd(new ProjectXXItemInstanceRuntime(definition, quantity), origin, rotated, out placement);
        }

        public bool TryAddToBackpackFirstFit(ProjectXXItemDefinition definition, int quantity, bool allowRotation, out ProjectXXItemPlacementRuntime placement)
        {
            placement = null;
            if (definition == null)
            {
                return false;
            }

            return BackpackGrid.TryAddFirstFit(new ProjectXXItemInstanceRuntime(definition, quantity), allowRotation, out placement);
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
