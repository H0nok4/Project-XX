using UnityEngine;

namespace ProjectXX.Domain.Inventory
{
    [CreateAssetMenu(fileName = "EquippableItemDefinition", menuName = "ProjectXX/Definitions/Equippable Item Definition")]
    public sealed class ProjectXXEquippableItemDefinition : ProjectXXItemDefinition
    {
        [SerializeField] private ProjectXXEquipmentSlot equipmentSlot = ProjectXXEquipmentSlot.PrimaryWeapon;

        public ProjectXXEquipmentSlot EquipmentSlot => equipmentSlot;
    }
}
