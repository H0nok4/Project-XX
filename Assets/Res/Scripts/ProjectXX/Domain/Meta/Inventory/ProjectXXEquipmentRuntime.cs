using System;
using UnityEngine;

namespace ProjectXX.Domain.Inventory
{
    [DisallowMultipleComponent]
    public sealed class ProjectXXEquipmentRuntime : MonoBehaviour
    {
        [SerializeField] private ProjectXXEquippableItemDefinition primaryWeapon;
        [SerializeField] private ProjectXXEquippableItemDefinition secondaryWeapon;
        [SerializeField] private ProjectXXEquippableItemDefinition meleeWeapon;
        [SerializeField] private ProjectXXEquippableItemDefinition armor;
        [SerializeField] private ProjectXXEquippableItemDefinition utility;

        public event Action<ProjectXXEquipmentSlot, ProjectXXEquippableItemDefinition> EquippedChanged;

        public ProjectXXEquippableItemDefinition PrimaryWeapon => primaryWeapon;
        public ProjectXXEquippableItemDefinition SecondaryWeapon => secondaryWeapon;
        public ProjectXXEquippableItemDefinition MeleeWeapon => meleeWeapon;
        public ProjectXXEquippableItemDefinition Armor => armor;
        public ProjectXXEquippableItemDefinition Utility => utility;

        public ProjectXXEquippableItemDefinition GetEquipped(ProjectXXEquipmentSlot slot)
        {
            return slot switch
            {
                ProjectXXEquipmentSlot.PrimaryWeapon => primaryWeapon,
                ProjectXXEquipmentSlot.SecondaryWeapon => secondaryWeapon,
                ProjectXXEquipmentSlot.MeleeWeapon => meleeWeapon,
                ProjectXXEquipmentSlot.Armor => armor,
                ProjectXXEquipmentSlot.Utility => utility,
                _ => null
            };
        }

        public void Equip(ProjectXXEquippableItemDefinition itemDefinition)
        {
            if (itemDefinition == null)
            {
                return;
            }

            SetEquipped(itemDefinition.EquipmentSlot, itemDefinition);
        }

        public void Clear(ProjectXXEquipmentSlot slot)
        {
            SetEquipped(slot, null);
        }

        private void SetEquipped(ProjectXXEquipmentSlot slot, ProjectXXEquippableItemDefinition itemDefinition)
        {
            if (GetEquipped(slot) == itemDefinition)
            {
                return;
            }

            switch (slot)
            {
                case ProjectXXEquipmentSlot.PrimaryWeapon:
                    primaryWeapon = itemDefinition;
                    break;
                case ProjectXXEquipmentSlot.SecondaryWeapon:
                    secondaryWeapon = itemDefinition;
                    break;
                case ProjectXXEquipmentSlot.MeleeWeapon:
                    meleeWeapon = itemDefinition;
                    break;
                case ProjectXXEquipmentSlot.Armor:
                    armor = itemDefinition;
                    break;
                case ProjectXXEquipmentSlot.Utility:
                    utility = itemDefinition;
                    break;
                default:
                    return;
            }

            EquippedChanged?.Invoke(slot, itemDefinition);
        }
    }
}
