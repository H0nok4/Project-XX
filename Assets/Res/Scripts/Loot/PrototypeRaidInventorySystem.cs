using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum PrototypeRaidGearSlotType
{
    PrimaryWeapon = 0,
    SecondaryWeapon = 1,
    MeleeWeapon = 2,
    Armor = 3,
    Helmet = 4,
    SecureContainer = 5
}

public enum PrototypeRaidItemLocationKind
{
    InventoryItem = 0,
    WeaponSlot = 1,
    ArmorSlot = 2,
    SecureContainerGear = 3,
    CorpseWeapon = 4
}

public enum PrototypeRaidDropTargetKind
{
    Inventory = 0,
    WeaponSlot = 1,
    ArmorSlot = 2,
    SecureContainerGear = 3
}

public readonly struct PrototypeRaidSecureContainerSpec
{
    public PrototypeRaidSecureContainerSpec(string itemId, string displayName, int slotCount, float maxWeight)
    {
        ItemId = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? ItemId : displayName.Trim();
        SlotCount = Mathf.Max(1, slotCount);
        MaxWeight = Mathf.Max(0f, maxWeight);
    }

    public string ItemId { get; }
    public string DisplayName { get; }
    public int SlotCount { get; }
    public float MaxWeight { get; }
}

public sealed class PrototypeRaidItemLocation
{
    public PrototypeRaidItemLocationKind Kind;
    public InventoryContainer Inventory;
    public string ItemInstanceId;
    public int Quantity;
    public PrototypeRaidGearSlotType GearSlot;
    public PrototypeCorpseLoot CorpseLoot;
    public int CorpseWeaponIndex;

    public static PrototypeRaidItemLocation FromInventory(InventoryContainer inventory, ItemInstance item)
    {
        if (inventory == null || item == null)
        {
            return null;
        }

        return new PrototypeRaidItemLocation
        {
            Kind = PrototypeRaidItemLocationKind.InventoryItem,
            Inventory = inventory,
            ItemInstanceId = item.InstanceId,
            Quantity = Mathf.Max(1, item.Quantity)
        };
    }

    public static PrototypeRaidItemLocation FromWeaponSlot(PrototypeRaidGearSlotType gearSlot)
    {
        return new PrototypeRaidItemLocation
        {
            Kind = PrototypeRaidItemLocationKind.WeaponSlot,
            GearSlot = gearSlot,
            Quantity = 1
        };
    }

    public static PrototypeRaidItemLocation FromArmorSlot(PrototypeRaidGearSlotType gearSlot)
    {
        return new PrototypeRaidItemLocation
        {
            Kind = PrototypeRaidItemLocationKind.ArmorSlot,
            GearSlot = gearSlot,
            Quantity = 1
        };
    }

    public static PrototypeRaidItemLocation FromSecureContainerGear()
    {
        return new PrototypeRaidItemLocation
        {
            Kind = PrototypeRaidItemLocationKind.SecureContainerGear,
            GearSlot = PrototypeRaidGearSlotType.SecureContainer,
            Quantity = 1
        };
    }

    public static PrototypeRaidItemLocation FromCorpseWeapon(PrototypeCorpseLoot corpseLoot, int weaponIndex)
    {
        if (corpseLoot == null || weaponIndex < 0)
        {
            return null;
        }

        return new PrototypeRaidItemLocation
        {
            Kind = PrototypeRaidItemLocationKind.CorpseWeapon,
            CorpseLoot = corpseLoot,
            CorpseWeaponIndex = weaponIndex,
            Quantity = 1
        };
    }

    public bool IsEquivalentTo(PrototypeRaidDropTarget target)
    {
        if (target == null)
        {
            return false;
        }

        switch (Kind)
        {
            case PrototypeRaidItemLocationKind.InventoryItem:
                return target.Kind == PrototypeRaidDropTargetKind.Inventory && target.Inventory == Inventory;
            case PrototypeRaidItemLocationKind.WeaponSlot:
            case PrototypeRaidItemLocationKind.ArmorSlot:
                return target.Kind != PrototypeRaidDropTargetKind.Inventory && target.GearSlot == GearSlot;
            case PrototypeRaidItemLocationKind.SecureContainerGear:
                return target.Kind == PrototypeRaidDropTargetKind.SecureContainerGear;
            default:
                return false;
        }
    }
}

public sealed class PrototypeRaidDropTarget
{
    public PrototypeRaidDropTargetKind Kind;
    public InventoryContainer Inventory;
    public PrototypeRaidGearSlotType GearSlot;

    public static PrototypeRaidDropTarget ForInventory(InventoryContainer inventory)
    {
        return inventory == null
            ? null
            : new PrototypeRaidDropTarget
            {
                Kind = PrototypeRaidDropTargetKind.Inventory,
                Inventory = inventory
            };
    }

    public static PrototypeRaidDropTarget ForWeaponSlot(PrototypeRaidGearSlotType gearSlot)
    {
        return new PrototypeRaidDropTarget
        {
            Kind = PrototypeRaidDropTargetKind.WeaponSlot,
            GearSlot = gearSlot
        };
    }

    public static PrototypeRaidDropTarget ForArmorSlot(PrototypeRaidGearSlotType gearSlot)
    {
        return new PrototypeRaidDropTarget
        {
            Kind = PrototypeRaidDropTargetKind.ArmorSlot,
            GearSlot = gearSlot
        };
    }

    public static PrototypeRaidDropTarget ForSecureContainerGear()
    {
        return new PrototypeRaidDropTarget
        {
            Kind = PrototypeRaidDropTargetKind.SecureContainerGear,
            GearSlot = PrototypeRaidGearSlotType.SecureContainer
        };
    }
}

public static class PrototypeRaidInventoryRules
{
    public const string HeadPartId = "head";
    public const string TorsoPartId = "torso";
    public const string DefaultSecureContainerItemId = "secure_case_alpha";

    public static bool IsWeaponSlot(PrototypeRaidGearSlotType gearSlot)
    {
        return gearSlot == PrototypeRaidGearSlotType.PrimaryWeapon
            || gearSlot == PrototypeRaidGearSlotType.SecondaryWeapon
            || gearSlot == PrototypeRaidGearSlotType.MeleeWeapon;
    }

    public static bool IsArmorSlot(PrototypeRaidGearSlotType gearSlot)
    {
        return gearSlot == PrototypeRaidGearSlotType.Armor
            || gearSlot == PrototypeRaidGearSlotType.Helmet;
    }

    public static string GetSlotDisplayName(PrototypeRaidGearSlotType gearSlot)
    {
        switch (gearSlot)
        {
            case PrototypeRaidGearSlotType.PrimaryWeapon:
                return "Primary Weapon";
            case PrototypeRaidGearSlotType.SecondaryWeapon:
                return "Secondary Weapon";
            case PrototypeRaidGearSlotType.MeleeWeapon:
                return "Melee Weapon";
            case PrototypeRaidGearSlotType.Armor:
                return "Armor";
            case PrototypeRaidGearSlotType.Helmet:
                return "Helmet";
            case PrototypeRaidGearSlotType.SecureContainer:
                return "Secure Container";
            default:
                return "Slot";
        }
    }

    public static PrototypeRaidGearSlotType ResolveArmorSlot(ArmorDefinition armorDefinition)
    {
        if (armorDefinition == null)
        {
            return PrototypeRaidGearSlotType.Armor;
        }

        bool coversHead = armorDefinition.CoversPart(HeadPartId);
        bool coversTorso = armorDefinition.CoversPart(TorsoPartId);
        if (coversHead && !coversTorso)
        {
            return PrototypeRaidGearSlotType.Helmet;
        }

        if (coversTorso)
        {
            return PrototypeRaidGearSlotType.Armor;
        }

        return coversHead ? PrototypeRaidGearSlotType.Helmet : PrototypeRaidGearSlotType.Armor;
    }

    public static bool CanEquipWeaponToSlot(ItemInstance itemInstance, PrototypeRaidGearSlotType gearSlot)
    {
        if (itemInstance == null || !itemInstance.IsWeapon || itemInstance.WeaponDefinition == null)
        {
            return false;
        }

        if (itemInstance.WeaponDefinition.IsThrowableWeapon)
        {
            return false;
        }

        switch (gearSlot)
        {
            case PrototypeRaidGearSlotType.PrimaryWeapon:
            case PrototypeRaidGearSlotType.SecondaryWeapon:
                return !itemInstance.WeaponDefinition.IsMeleeWeapon;
            case PrototypeRaidGearSlotType.MeleeWeapon:
                return itemInstance.WeaponDefinition.IsMeleeWeapon;
            default:
                return false;
        }
    }

    public static bool CanEquipArmorToSlot(ItemInstance itemInstance, PrototypeRaidGearSlotType gearSlot)
    {
        return itemInstance != null
            && itemInstance.Definition is ArmorDefinition armorDefinition
            && ResolveArmorSlot(armorDefinition) == gearSlot;
    }

    public static bool IsSecureContainerItem(ItemDefinitionBase definition)
    {
        return TryGetSecureContainerSpec(definition, out _);
    }

    public static bool IsSecureContainerItem(ItemInstance itemInstance)
    {
        return itemInstance != null && TryGetSecureContainerSpec(itemInstance.DefinitionBase, out _);
    }

    public static bool TryGetSecureContainerSpec(ItemDefinitionBase definition, out PrototypeRaidSecureContainerSpec spec)
    {
        spec = default;
        if (!(definition is SecureContainerDefinition secureContainerDefinition))
        {
            return false;
        }

        spec = new PrototypeRaidSecureContainerSpec(
            secureContainerDefinition.ItemId,
            secureContainerDefinition.DisplayName,
            secureContainerDefinition.SlotCapacity,
            secureContainerDefinition.MaxStoredWeight);
        return true;
    }

    public static ItemDefinition ResolveDefaultSecureContainerDefinition(PrototypeItemCatalog catalog)
    {
        return catalog != null
            ? catalog.FindByItemId(DefaultSecureContainerItemId) as SecureContainerDefinition
            : null;
    }

    public static ItemInstance CreateDefaultSecureContainerInstance(PrototypeItemCatalog catalog)
    {
        ItemDefinition definition = ResolveDefaultSecureContainerDefinition(catalog);
        return definition != null
            ? ItemInstance.Create(definition, 1, null, ItemRarity.Common, null, false, null, false)
            : null;
    }

    public static int BuildItemHash(ItemInstance item)
    {
        unchecked
        {
            if (item == null || !item.IsDefined())
            {
                return 0;
            }

            int hash = 17;
            string itemId = item.DefinitionBase != null ? item.DefinitionBase.ItemId : string.Empty;
            hash = hash * 31 + (itemId != null ? itemId.GetHashCode() : 0);
            hash = hash * 31 + item.Quantity;
            hash = hash * 31 + (int)item.Rarity;
            hash = hash * 31 + item.MagazineAmmo;
            hash = hash * 31 + Mathf.RoundToInt(item.CurrentDurability * 1000f);
            return hash;
        }
    }

    public static int BuildInventoryHash(InventoryContainer inventory)
    {
        unchecked
        {
            if (inventory == null)
            {
                return 0;
            }

            int hash = inventory.MaxSlots;
            hash = hash * 31 + Mathf.RoundToInt(inventory.MaxWeight * 100f);
            hash = hash * 31 + inventory.OccupiedSlots;
            hash = hash * 31 + Mathf.RoundToInt(inventory.CurrentWeight * 100f);
            for (int index = 0; index < inventory.Items.Count; index++)
            {
                hash = hash * 31 + BuildItemHash(inventory.Items[index]);
            }

            return hash;
        }
    }

    public static int BuildCorpseWeaponHash(PrototypeCorpseLoot corpseLoot)
    {
        unchecked
        {
            if (corpseLoot == null || corpseLoot.Weapons == null)
            {
                return 0;
            }

            int hash = corpseLoot.Weapons.Count;
            for (int index = 0; index < corpseLoot.Weapons.Count; index++)
            {
                PrototypeCorpseLoot.WeaponEntry entry = corpseLoot.GetWeaponEntry(index);
                ItemInstance instance = entry != null ? entry.CreateInstance() : null;
                hash = hash * 31 + BuildItemHash(instance);
            }

            return hash;
        }
    }
}

[DisallowMultipleComponent]
public sealed class PrototypeRaidEquipmentController : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PrototypeFpsController fpsController;
    [SerializeField] private PrototypeUnitVitals playerVitals;
    [SerializeField] private ItemInstance equippedSecureContainerItem;
    [SerializeField] private float statusMessageDuration = 2.4f;

    private PrototypeItemCatalog itemCatalog;
    private string statusMessage = string.Empty;
    private float statusMessageUntil;

    public event Action Changed;

    public ItemInstance EquippedSecureContainerItem => CloneItem(equippedSecureContainerItem);
    public string StatusMessage => Time.time <= statusMessageUntil ? statusMessage : string.Empty;

    private void Awake()
    {
        ResolveReferences();
        ApplySecureContainerConfiguration();
    }

    private void OnValidate()
    {
        ResolveReferences();
        statusMessageDuration = Mathf.Max(0.5f, statusMessageDuration);
    }

    public void Configure(PrototypeItemCatalog catalog, ItemInstance secureContainerItem)
    {
        itemCatalog = catalog;
        ResolveReferences();

        if (PrototypeRaidInventoryRules.IsSecureContainerItem(secureContainerItem))
        {
            equippedSecureContainerItem = CloneItem(secureContainerItem);
        }
        else if (equippedSecureContainerItem == null || !PrototypeRaidInventoryRules.IsSecureContainerItem(equippedSecureContainerItem))
        {
            equippedSecureContainerItem = PrototypeRaidInventoryRules.CreateDefaultSecureContainerInstance(itemCatalog);
        }

        ApplySecureContainerConfiguration();
        NotifyChanged();
    }

    public PrototypeRaidSecureContainerSpec GetCurrentSecureContainerSpec()
    {
        return PrototypeRaidInventoryRules.TryGetSecureContainerSpec(equippedSecureContainerItem != null ? equippedSecureContainerItem.DefinitionBase : null, out PrototypeRaidSecureContainerSpec spec)
            ? spec
            : default;
    }

    public ItemInstance GetSlotItem(PrototypeRaidGearSlotType gearSlot)
    {
        switch (gearSlot)
        {
            case PrototypeRaidGearSlotType.PrimaryWeapon:
                return fpsController != null ? fpsController.GetPrimaryItemInstance() : null;
            case PrototypeRaidGearSlotType.SecondaryWeapon:
                return fpsController != null ? fpsController.GetSecondaryItemInstance() : null;
            case PrototypeRaidGearSlotType.MeleeWeapon:
                return fpsController != null ? fpsController.GetMeleeItemInstance() : null;
            case PrototypeRaidGearSlotType.Armor:
            case PrototypeRaidGearSlotType.Helmet:
                return GetArmorSlotItem(gearSlot);
            case PrototypeRaidGearSlotType.SecureContainer:
                return CloneItem(equippedSecureContainerItem);
            default:
                return null;
        }
    }

    public int BuildStateHash(LootContainer openContainer = null)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + PrototypeRaidInventoryRules.BuildInventoryHash(interactor != null ? interactor.PrimaryInventory : null);
            hash = hash * 31 + PrototypeRaidInventoryRules.BuildInventoryHash(interactor != null ? interactor.SecureInventory : null);
            hash = hash * 31 + PrototypeRaidInventoryRules.BuildInventoryHash(interactor != null ? interactor.SpecialInventory : null);
            hash = hash * 31 + PrototypeRaidInventoryRules.BuildItemHash(GetSlotItem(PrototypeRaidGearSlotType.PrimaryWeapon));
            hash = hash * 31 + PrototypeRaidInventoryRules.BuildItemHash(GetSlotItem(PrototypeRaidGearSlotType.SecondaryWeapon));
            hash = hash * 31 + PrototypeRaidInventoryRules.BuildItemHash(GetSlotItem(PrototypeRaidGearSlotType.MeleeWeapon));
            hash = hash * 31 + PrototypeRaidInventoryRules.BuildItemHash(GetSlotItem(PrototypeRaidGearSlotType.Armor));
            hash = hash * 31 + PrototypeRaidInventoryRules.BuildItemHash(GetSlotItem(PrototypeRaidGearSlotType.Helmet));
            hash = hash * 31 + PrototypeRaidInventoryRules.BuildItemHash(GetSlotItem(PrototypeRaidGearSlotType.SecureContainer));
            if (openContainer != null)
            {
                hash = hash * 31 + PrototypeRaidInventoryRules.BuildInventoryHash(openContainer.Inventory);
                hash = hash * 31 + PrototypeRaidInventoryRules.BuildCorpseWeaponHash(openContainer.GetComponent<PrototypeCorpseLoot>());
            }

            return hash;
        }
    }

    public bool TryDropBackpackItem(string instanceId, out string feedback)
    {
        feedback = string.Empty;
        ResolveReferences();
        InventoryContainer backpack = interactor != null ? interactor.PrimaryInventory : null;
        if (backpack == null || string.IsNullOrWhiteSpace(instanceId))
        {
            return false;
        }

        int itemIndex = FindInventoryIndexByInstanceId(backpack, instanceId);
        if (itemIndex < 0)
        {
            return false;
        }

        ItemInstance sourceItem = backpack.Items[itemIndex];
        if (sourceItem == null || !sourceItem.IsDefined())
        {
            return false;
        }

        if (!backpack.TryExtractItem(itemIndex, sourceItem.Quantity, out ItemInstance extractedItem) || extractedItem == null)
        {
            return false;
        }

        Transform dropOrigin = interactor != null && interactor.InteractionCamera != null
            ? interactor.InteractionCamera.transform
            : transform;
        GroundLootItem.SpawnDroppedItem(dropOrigin, extractedItem);
        feedback = $"Dropped {extractedItem.DisplayName}.";
        SetStatusMessage(feedback);
        NotifyChanged();
        return true;
    }

    public bool TryMove(PrototypeRaidItemLocation source, PrototypeRaidDropTarget target, out string feedback)
    {
        feedback = string.Empty;
        ResolveReferences();
        if (source == null || target == null || source.IsEquivalentTo(target))
        {
            return false;
        }

        if (!TryTakeFromSource(source, out ItemInstance sourceItem, out feedback) || sourceItem == null)
        {
            SetStatusMessage(feedback);
            return false;
        }

        ItemInstance displacedTargetItem = PeekTargetItem(target);
        if (displacedTargetItem != null && !CanRestoreToSource(source, displacedTargetItem))
        {
            TryRestoreToSource(source, sourceItem, out _);
            feedback = "There is not enough room to swap the replaced item back to the source.";
            SetStatusMessage(feedback);
            return false;
        }

        if (!TryPlaceInTarget(target, sourceItem, out ItemInstance displacedAfterPlacement, out feedback))
        {
            TryRestoreToSource(source, sourceItem, out _);
            SetStatusMessage(feedback);
            return false;
        }

        ItemInstance displacedItem = displacedAfterPlacement ?? displacedTargetItem;
        if (displacedItem != null && !TryRestoreToSource(source, displacedItem, out string restoreFeedback))
        {
            TryTakeFromTarget(target, out _, out _);
            TryRestoreToSource(source, sourceItem, out _);
            if (displacedTargetItem != null)
            {
                TryPlaceInTarget(target, displacedTargetItem, out _, out _);
            }

            feedback = string.IsNullOrWhiteSpace(restoreFeedback)
                ? "The swapped item could not be returned to its previous location."
                : restoreFeedback;
            SetStatusMessage(feedback);
            return false;
        }

        feedback = BuildSuccessMessage(target, sourceItem);
        SetStatusMessage(feedback);
        NotifyChanged();
        return true;
    }

    private string BuildSuccessMessage(PrototypeRaidDropTarget target, ItemInstance itemInstance)
    {
        string itemLabel = itemInstance != null ? itemInstance.DisplayName : "Item";
        switch (target.Kind)
        {
            case PrototypeRaidDropTargetKind.Inventory:
                if (target.Inventory == interactor?.PrimaryInventory)
                {
                    return $"Stored {itemLabel} in the backpack.";
                }

                if (target.Inventory == interactor?.SecureInventory)
                {
                    return $"Stored {itemLabel} in the secure container.";
                }

                if (target.Inventory == interactor?.SpecialInventory)
                {
                    return $"Stored {itemLabel} in special equipment.";
                }

                return $"Moved {itemLabel}.";

            case PrototypeRaidDropTargetKind.WeaponSlot:
            case PrototypeRaidDropTargetKind.ArmorSlot:
            case PrototypeRaidDropTargetKind.SecureContainerGear:
                return $"Equipped {itemLabel}.";

            default:
                return $"Moved {itemLabel}.";
        }
    }

    private void ApplySecureContainerConfiguration()
    {
        ResolveReferences();
        InventoryContainer secureInventory = interactor != null ? interactor.SecureInventory : null;
        if (secureInventory == null)
        {
            return;
        }

        if (PrototypeRaidInventoryRules.TryGetSecureContainerSpec(equippedSecureContainerItem != null ? equippedSecureContainerItem.DefinitionBase : null, out PrototypeRaidSecureContainerSpec spec))
        {
            secureInventory.Configure("Secure Container", spec.SlotCount, spec.MaxWeight);
        }
        else
        {
            secureInventory.Configure("Secure Container", 1, 0f);
        }
    }

    private bool TryTakeFromSource(PrototypeRaidItemLocation source, out ItemInstance sourceItem, out string feedback)
    {
        sourceItem = null;
        feedback = string.Empty;

        switch (source.Kind)
        {
            case PrototypeRaidItemLocationKind.InventoryItem:
                return TryTakeFromInventory(source.Inventory, source.ItemInstanceId, source.Quantity, out sourceItem, out feedback);
            case PrototypeRaidItemLocationKind.WeaponSlot:
                return TryTakeFromWeaponSlot(source.GearSlot, out sourceItem, out feedback);
            case PrototypeRaidItemLocationKind.ArmorSlot:
                return TryTakeFromArmorSlot(source.GearSlot, out sourceItem, out feedback);
            case PrototypeRaidItemLocationKind.SecureContainerGear:
                return TryTakeSecureContainerGear(out sourceItem, out feedback);
            case PrototypeRaidItemLocationKind.CorpseWeapon:
                return TryTakeFromCorpseWeapon(source.CorpseLoot, source.CorpseWeaponIndex, out sourceItem, out feedback);
            default:
                feedback = "This item source is unavailable.";
                return false;
        }
    }

    private bool TryTakeFromTarget(PrototypeRaidDropTarget target, out ItemInstance removedItem, out string feedback)
    {
        removedItem = null;
        feedback = string.Empty;
        if (target == null)
        {
            return false;
        }

        switch (target.Kind)
        {
            case PrototypeRaidDropTargetKind.WeaponSlot:
                return TryTakeFromWeaponSlot(target.GearSlot, out removedItem, out feedback);
            case PrototypeRaidDropTargetKind.ArmorSlot:
                return TryTakeFromArmorSlot(target.GearSlot, out removedItem, out feedback);
            case PrototypeRaidDropTargetKind.SecureContainerGear:
                return TryTakeSecureContainerGear(out removedItem, out feedback);
            default:
                return false;
        }
    }

    private ItemInstance PeekTargetItem(PrototypeRaidDropTarget target)
    {
        if (target == null)
        {
            return null;
        }

        switch (target.Kind)
        {
            case PrototypeRaidDropTargetKind.WeaponSlot:
            case PrototypeRaidDropTargetKind.ArmorSlot:
                return GetSlotItem(target.GearSlot);
            case PrototypeRaidDropTargetKind.SecureContainerGear:
                return CloneItem(equippedSecureContainerItem);
            default:
                return null;
        }
    }

    private bool TryPlaceInTarget(PrototypeRaidDropTarget target, ItemInstance itemInstance, out ItemInstance displacedItem, out string feedback)
    {
        displacedItem = null;
        feedback = string.Empty;
        if (target == null || itemInstance == null || !itemInstance.IsDefined())
        {
            feedback = "The destination is unavailable.";
            return false;
        }

        switch (target.Kind)
        {
            case PrototypeRaidDropTargetKind.Inventory:
                if (target.Inventory != null && target.Inventory.TryAddItemInstance(itemInstance))
                {
                    return true;
                }

                feedback = "The destination container does not have enough free space.";
                return false;

            case PrototypeRaidDropTargetKind.WeaponSlot:
                if (TrySetWeaponSlot(target.GearSlot, itemInstance, out displacedItem))
                {
                    return true;
                }

                feedback = $"Only a matching weapon can be equipped to {PrototypeRaidInventoryRules.GetSlotDisplayName(target.GearSlot)}.";
                return false;

            case PrototypeRaidDropTargetKind.ArmorSlot:
                if (TrySetArmorSlot(target.GearSlot, itemInstance, out displacedItem))
                {
                    return true;
                }

                feedback = $"Only matching armor can be equipped to {PrototypeRaidInventoryRules.GetSlotDisplayName(target.GearSlot)}.";
                return false;

            case PrototypeRaidDropTargetKind.SecureContainerGear:
                if (TrySetSecureContainerGear(itemInstance, out displacedItem, out feedback))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(feedback))
                {
                    feedback = "Only secure container equipment can be placed in this slot.";
                }

                return false;

            default:
                feedback = "The destination is unavailable.";
                return false;
        }
    }

    private bool CanRestoreToSource(PrototypeRaidItemLocation source, ItemInstance itemInstance)
    {
        if (source == null || itemInstance == null || !itemInstance.IsDefined())
        {
            return false;
        }

        switch (source.Kind)
        {
            case PrototypeRaidItemLocationKind.InventoryItem:
                return source.Inventory != null && source.Inventory.CanAccept(itemInstance);
            case PrototypeRaidItemLocationKind.WeaponSlot:
                return PrototypeRaidInventoryRules.CanEquipWeaponToSlot(itemInstance, source.GearSlot);
            case PrototypeRaidItemLocationKind.ArmorSlot:
                return PrototypeRaidInventoryRules.CanEquipArmorToSlot(itemInstance, source.GearSlot);
            case PrototypeRaidItemLocationKind.SecureContainerGear:
                return CanEquipSecureContainerItem(itemInstance);
            case PrototypeRaidItemLocationKind.CorpseWeapon:
                return itemInstance.IsWeapon && itemInstance.WeaponDefinition != null;
            default:
                return false;
        }
    }

    private bool TryRestoreToSource(PrototypeRaidItemLocation source, ItemInstance itemInstance, out string feedback)
    {
        feedback = string.Empty;
        if (source == null || itemInstance == null || !itemInstance.IsDefined())
        {
            return false;
        }

        switch (source.Kind)
        {
            case PrototypeRaidItemLocationKind.InventoryItem:
                if (source.Inventory != null && source.Inventory.TryAddItemInstance(itemInstance))
                {
                    return true;
                }

                feedback = "The item could not be returned to its source container.";
                return false;

            case PrototypeRaidItemLocationKind.WeaponSlot:
                if (TrySetWeaponSlot(source.GearSlot, itemInstance, out _))
                {
                    return true;
                }

                feedback = "The weapon could not be returned to its previous slot.";
                return false;

            case PrototypeRaidItemLocationKind.ArmorSlot:
                if (TrySetArmorSlot(source.GearSlot, itemInstance, out _))
                {
                    return true;
                }

                feedback = "The armor could not be returned to its previous slot.";
                return false;

            case PrototypeRaidItemLocationKind.SecureContainerGear:
                if (TrySetSecureContainerGear(itemInstance, out _, out feedback))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(feedback))
                {
                    feedback = "The secure container could not be returned to its slot.";
                }

                return false;

            case PrototypeRaidItemLocationKind.CorpseWeapon:
                if (source.CorpseLoot != null && itemInstance.IsWeapon && itemInstance.WeaponDefinition != null)
                {
                    source.CorpseLoot.AddWeapon(itemInstance);
                    return true;
                }

                feedback = "The weapon could not be returned to the corpse.";
                return false;

            default:
                return false;
        }
    }

    private bool TryTakeFromInventory(
        InventoryContainer inventory,
        string instanceId,
        int quantity,
        out ItemInstance extractedItem,
        out string feedback)
    {
        extractedItem = null;
        feedback = string.Empty;
        if (inventory == null || string.IsNullOrWhiteSpace(instanceId))
        {
            feedback = "The source container is unavailable.";
            return false;
        }

        int itemIndex = FindInventoryIndexByInstanceId(inventory, instanceId);
        if (itemIndex < 0)
        {
            feedback = "The source item is no longer available.";
            return false;
        }

        ItemInstance item = inventory.Items[itemIndex];
        int extractQuantity = Mathf.Clamp(quantity, 1, item != null ? item.Quantity : 1);
        if (!inventory.TryExtractItem(itemIndex, extractQuantity, out extractedItem) || extractedItem == null)
        {
            feedback = "The item could not be extracted from its source container.";
            return false;
        }

        return true;
    }

    private bool TryTakeFromWeaponSlot(PrototypeRaidGearSlotType gearSlot, out ItemInstance extractedItem, out string feedback)
    {
        extractedItem = null;
        feedback = string.Empty;
        if (fpsController == null)
        {
            feedback = "Weapon equipment is unavailable.";
            return false;
        }

        if (!fpsController.TryTakeWeaponSlotItem(gearSlot, out extractedItem) || extractedItem == null)
        {
            feedback = $"The {PrototypeRaidInventoryRules.GetSlotDisplayName(gearSlot)} slot is empty.";
            return false;
        }

        return true;
    }

    private bool TrySetWeaponSlot(PrototypeRaidGearSlotType gearSlot, ItemInstance itemInstance, out ItemInstance displacedItem)
    {
        displacedItem = null;
        return fpsController != null
            && PrototypeRaidInventoryRules.CanEquipWeaponToSlot(itemInstance, gearSlot)
            && fpsController.TrySetWeaponSlotItem(gearSlot, itemInstance, out displacedItem);
    }

    private bool TryTakeFromArmorSlot(PrototypeRaidGearSlotType gearSlot, out ItemInstance extractedItem, out string feedback)
    {
        extractedItem = null;
        feedback = string.Empty;
        List<ArmorInstance> equippedArmor = GetEquippedArmorInstances();
        int armorIndex = FindArmorIndexBySlot(equippedArmor, gearSlot);
        if (armorIndex < 0)
        {
            feedback = $"The {PrototypeRaidInventoryRules.GetSlotDisplayName(gearSlot)} slot is empty.";
            return false;
        }

        ArmorInstance armorInstance = equippedArmor[armorIndex];
        extractedItem = ItemInstance.Create(armorInstance);
        equippedArmor.RemoveAt(armorIndex);
        ApplyArmorInstances(equippedArmor);
        return extractedItem != null;
    }

    private bool TrySetArmorSlot(PrototypeRaidGearSlotType gearSlot, ItemInstance itemInstance, out ItemInstance displacedItem)
    {
        displacedItem = null;
        if (!PrototypeRaidInventoryRules.CanEquipArmorToSlot(itemInstance, gearSlot))
        {
            return false;
        }

        ArmorInstance incomingArmor = itemInstance.ToArmorInstance();
        if (incomingArmor == null)
        {
            return false;
        }

        List<ArmorInstance> equippedArmor = GetEquippedArmorInstances();
        int armorIndex = FindArmorIndexBySlot(equippedArmor, gearSlot);
        if (armorIndex >= 0)
        {
            displacedItem = ItemInstance.Create(equippedArmor[armorIndex]);
            equippedArmor.RemoveAt(armorIndex);
        }

        equippedArmor.Add(incomingArmor);
        ApplyArmorInstances(equippedArmor);
        return true;
    }

    private bool TryTakeSecureContainerGear(out ItemInstance extractedItem, out string feedback)
    {
        extractedItem = null;
        feedback = string.Empty;
        if (equippedSecureContainerItem == null)
        {
            feedback = "No secure container is equipped.";
            return false;
        }

        InventoryContainer secureInventory = interactor != null ? interactor.SecureInventory : null;
        if (secureInventory != null && !secureInventory.IsEmpty)
        {
            feedback = "Empty the secure container before unequipping it.";
            return false;
        }

        extractedItem = CloneItem(equippedSecureContainerItem);
        equippedSecureContainerItem = null;
        ApplySecureContainerConfiguration();
        return extractedItem != null;
    }

    private bool TrySetSecureContainerGear(ItemInstance itemInstance, out ItemInstance displacedItem, out string feedback)
    {
        displacedItem = null;
        feedback = string.Empty;
        if (!PrototypeRaidInventoryRules.IsSecureContainerItem(itemInstance))
        {
            return false;
        }

        if (!CanEquipSecureContainerItem(itemInstance))
        {
            feedback = "The equipped contents do not fit inside that secure container.";
            return false;
        }

        displacedItem = CloneItem(equippedSecureContainerItem);
        equippedSecureContainerItem = CloneItem(itemInstance);
        ApplySecureContainerConfiguration();
        return true;
    }

    private bool CanEquipSecureContainerItem(ItemInstance itemInstance)
    {
        if (!PrototypeRaidInventoryRules.TryGetSecureContainerSpec(itemInstance != null ? itemInstance.DefinitionBase : null, out PrototypeRaidSecureContainerSpec spec))
        {
            return false;
        }

        InventoryContainer secureInventory = interactor != null ? interactor.SecureInventory : null;
        if (secureInventory == null)
        {
            return true;
        }

        int occupiedSlots = secureInventory.Items != null ? secureInventory.Items.Count : 0;
        float currentWeight = secureInventory.CurrentWeight;
        return occupiedSlots <= spec.SlotCount && currentWeight <= spec.MaxWeight + 0.0001f;
    }

    private bool TryTakeFromCorpseWeapon(
        PrototypeCorpseLoot corpseLoot,
        int weaponIndex,
        out ItemInstance extractedItem,
        out string feedback)
    {
        extractedItem = null;
        feedback = string.Empty;
        if (corpseLoot == null || weaponIndex < 0)
        {
            feedback = "The corpse weapon is unavailable.";
            return false;
        }

        if (!corpseLoot.TryExtractWeapon(weaponIndex, out extractedItem) || extractedItem == null)
        {
            feedback = "The corpse weapon is no longer available.";
            return false;
        }

        return true;
    }

    private ItemInstance GetArmorSlotItem(PrototypeRaidGearSlotType gearSlot)
    {
        if (playerVitals == null || playerVitals.EquippedArmor == null)
        {
            return null;
        }

        for (int index = 0; index < playerVitals.EquippedArmor.Count; index++)
        {
            PrototypeUnitVitals.ArmorState armorState = playerVitals.EquippedArmor[index];
            if (armorState == null || armorState.definition == null)
            {
                continue;
            }

            if (PrototypeRaidInventoryRules.ResolveArmorSlot(armorState.definition) == gearSlot)
            {
                return ItemInstance.Create(
                    armorState.definition,
                    armorState.currentDurability,
                    armorState.instanceId,
                    armorState.Rarity,
                    armorState.affixes,
                    false,
                    armorState.skills,
                    false);
            }
        }

        return null;
    }

    private List<ArmorInstance> GetEquippedArmorInstances()
    {
        var results = new List<ArmorInstance>();
        if (playerVitals == null || playerVitals.EquippedArmor == null)
        {
            return results;
        }

        for (int index = 0; index < playerVitals.EquippedArmor.Count; index++)
        {
            PrototypeUnitVitals.ArmorState armorState = playerVitals.EquippedArmor[index];
            if (armorState == null || armorState.definition == null)
            {
                continue;
            }

            ArmorInstance instance = ArmorInstance.Create(
                armorState.definition,
                armorState.currentDurability,
                armorState.instanceId,
                armorState.Rarity,
                armorState.affixes,
                false,
                armorState.skills,
                false);
            if (instance != null)
            {
                results.Add(instance);
            }
        }

        return results;
    }

    private void ApplyArmorInstances(List<ArmorInstance> equippedArmor)
    {
        if (playerVitals != null)
        {
            playerVitals.SetArmorInstances(equippedArmor);
        }
    }

    private int FindArmorIndexBySlot(List<ArmorInstance> equippedArmor, PrototypeRaidGearSlotType gearSlot)
    {
        if (equippedArmor == null)
        {
            return -1;
        }

        for (int index = 0; index < equippedArmor.Count; index++)
        {
            ArmorInstance armorInstance = equippedArmor[index];
            if (armorInstance != null
                && armorInstance.Definition != null
                && PrototypeRaidInventoryRules.ResolveArmorSlot(armorInstance.Definition) == gearSlot)
            {
                return index;
            }
        }

        return -1;
    }

    private int FindInventoryIndexByInstanceId(InventoryContainer inventory, string instanceId)
    {
        if (inventory == null || string.IsNullOrWhiteSpace(instanceId))
        {
            return -1;
        }

        for (int index = 0; index < inventory.Items.Count; index++)
        {
            ItemInstance item = inventory.Items[index];
            if (item != null && string.Equals(item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static ItemInstance CloneItem(ItemInstance itemInstance)
    {
        return itemInstance != null ? itemInstance.Clone() : null;
    }

    private void SetStatusMessage(string message)
    {
        statusMessage = message ?? string.Empty;
        statusMessageUntil = string.IsNullOrWhiteSpace(statusMessage) ? 0f : Time.time + statusMessageDuration;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }

    private void ResolveReferences()
    {
        if (interactor == null)
        {
            interactor = GetComponent<PlayerInteractor>();
        }

        if (fpsController == null)
        {
            fpsController = GetComponent<PrototypeFpsController>();
        }

        if (playerVitals == null)
        {
            playerVitals = GetComponent<PrototypeUnitVitals>();
        }
    }
}

public sealed class PrototypeRaidDragPayload
{
    public PrototypeRaidEquipmentController Controller;
    public PrototypeRaidItemLocation Source;
    public ItemInstance Item;
}

[DisallowMultipleComponent]
public sealed class PrototypeRaidDragService : MonoBehaviour
{
    private const string DragGhostPrefabResourcePath = "UI/Loot/RaidDragGhost";

    private static PrototypeRaidDragService instance;

    private RectTransform ghostRoot;
    private Text ghostLabel;
    private RaidDragGhostTemplate dragGhostTemplate;

    public static PrototypeRaidDragService CurrentInstance => instance;
    public static PrototypeRaidDragService Instance => GetOrCreate();
    public PrototypeRaidDragPayload CurrentPayload { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static PrototypeRaidDragService GetOrCreate()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<PrototypeRaidDragService>();
        if (instance != null)
        {
            return instance;
        }

        GameObject serviceObject = new GameObject("PrototypeRaidDragService");
        instance = serviceObject.AddComponent<PrototypeRaidDragService>();
        return instance;
    }

    public void BeginDrag(PrototypeRaidDragPayload payload, PointerEventData eventData)
    {
        CurrentPayload = payload;
        EnsureGhostUi();
        if (ghostRoot == null)
        {
            return;
        }

        UpdateDrag(eventData);

        if (ghostLabel != null)
        {
            ghostLabel.text = payload != null && payload.Item != null ? payload.Item.DisplayName : "Item";
        }

        PrototypeUiToolkit.SetVisible(ghostRoot, true);
    }

    public void UpdateDrag(PointerEventData eventData)
    {
        if (ghostRoot == null || eventData == null)
        {
            return;
        }

        RectTransform canvasRoot = PrototypeRuntimeUiManager.GetOrCreate().CanvasRoot;
        PrototypeUiToolkit.SetScreenPosition(canvasRoot, ghostRoot, eventData.position + new Vector2(22f, -22f));
    }

    public void AcceptDrop()
    {
    }

    public void EndDrag()
    {
        CurrentPayload = null;
        if (ghostRoot != null)
        {
            PrototypeUiToolkit.SetVisible(ghostRoot, false);
        }
    }

    private void EnsureGhostUi()
    {
        if (ghostRoot != null)
        {
            return;
        }

        PrototypeRuntimeUiManager manager = PrototypeRuntimeUiManager.GetOrCreate();
        GameObject prefabAsset = Resources.Load<GameObject>(DragGhostPrefabResourcePath);
        if (prefabAsset == null)
        {
            Debug.LogWarning($"[{nameof(PrototypeRaidDragService)}] Missing drag ghost prefab at Resources/{DragGhostPrefabResourcePath}.");
            return;
        }

        GameObject instanceObject = UnityEngine.Object.Instantiate(prefabAsset, manager.GetLayerRoot(PrototypeUiLayer.Overlay), false);
        instanceObject.name = prefabAsset.name;
        dragGhostTemplate = instanceObject.GetComponent<RaidDragGhostTemplate>();
        if (dragGhostTemplate == null || dragGhostTemplate.Root == null)
        {
            UnityEngine.Object.Destroy(instanceObject);
            dragGhostTemplate = null;
            Debug.LogWarning($"[{nameof(PrototypeRaidDragService)}] Drag ghost prefab is missing {nameof(RaidDragGhostTemplate)}.");
            return;
        }

        ghostRoot = dragGhostTemplate.Root;
        ghostLabel = dragGhostTemplate.LabelText;
        PrototypeUiToolkit.ApplyFontRecursively(ghostRoot, manager.RuntimeFont);
        if (dragGhostTemplate.CanvasGroup != null)
        {
            dragGhostTemplate.CanvasGroup.blocksRaycasts = false;
            dragGhostTemplate.CanvasGroup.interactable = false;
        }

        PrototypeUiToolkit.SetVisible(ghostRoot, false);
    }
}

[DisallowMultipleComponent]
public sealed class PrototypeRaidDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private PrototypeRaidDragPayload payload;

    public void Configure(PrototypeRaidDragPayload dragPayload)
    {
        payload = dragPayload;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (payload == null || payload.Item == null || payload.Source == null)
        {
            return;
        }

        PrototypeRaidDragService.Instance.BeginDrag(payload, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        PrototypeRaidDragService.Instance.UpdateDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        PrototypeRaidDragService.Instance.EndDrag();
    }

    private void OnDisable()
    {
        CancelOwnedDrag();
    }

    private void OnDestroy()
    {
        CancelOwnedDrag();
    }

    private void CancelOwnedDrag()
    {
        PrototypeRaidDragService dragService = PrototypeRaidDragService.CurrentInstance;
        if (dragService != null && ReferenceEquals(dragService.CurrentPayload, payload))
        {
            dragService.EndDrag();
        }
    }
}

[DisallowMultipleComponent]
public sealed class PrototypeRaidDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    private PrototypeRaidDropTarget target;
    private Action dropSucceeded;
    private Image highlightImage;
    private Color normalColor;
    private Color highlightColor;

    public void Configure(
        PrototypeRaidDropTarget dropTarget,
        Action onDropSucceeded,
        Image background,
        Color normal,
        Color highlight)
    {
        target = dropTarget;
        dropSucceeded = onDropSucceeded;
        highlightImage = background;
        normalColor = normal;
        highlightColor = highlight;
    }

    public void OnDrop(PointerEventData eventData)
    {
        PrototypeRaidDragPayload payload = PrototypeRaidDragService.Instance.CurrentPayload;
        if (payload == null || payload.Controller == null || target == null)
        {
            return;
        }

        if (payload.Controller.TryMove(payload.Source, target, out _))
        {
            PrototypeRaidDragService.Instance.AcceptDrop();
            dropSucceeded?.Invoke();
        }

        SetHighlighted(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (PrototypeRaidDragService.Instance.CurrentPayload != null)
        {
            SetHighlighted(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlighted(false);
    }

    private void SetHighlighted(bool highlighted)
    {
        if (highlightImage != null)
        {
            highlightImage.color = highlighted ? highlightColor : normalColor;
        }
    }
}
