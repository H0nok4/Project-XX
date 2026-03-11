using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InventoryContainer : MonoBehaviour
{
    [SerializeField] private string containerName = "Container";
    [Min(1)]
    [SerializeField] private int maxSlots = 16;
    [Min(0f)]
    [SerializeField] private float maxWeight = 0f;
    [SerializeField] private List<ItemInstance> items = new List<ItemInstance>();

    public string ContainerName => string.IsNullOrWhiteSpace(containerName) ? name : containerName.Trim();
    public int MaxSlots => Mathf.Max(1, maxSlots);
    public float MaxWeight => Mathf.Max(0f, maxWeight);
    public IReadOnlyList<ItemInstance> Items => items;
    public int OccupiedSlots => items != null ? items.Count : 0;
    public bool IsEmpty => items == null || items.Count == 0;

    public float CurrentWeight
    {
        get
        {
            float totalWeight = 0f;
            foreach (ItemInstance item in items)
            {
                if (item != null && item.IsDefined())
                {
                    totalWeight += item.TotalWeight;
                }
            }

            return totalWeight;
        }
    }

    public bool HasFreeSlot => OccupiedSlots < MaxSlots;

    private void Awake()
    {
        SanitizeItems();
    }

    private void OnValidate()
    {
        maxSlots = Mathf.Max(1, maxSlots);
        maxWeight = Mathf.Max(0f, maxWeight);
        SanitizeItems();
    }

    public void Configure(string displayName, int slotCount, float carryWeight)
    {
        containerName = string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();
        maxSlots = Mathf.Max(1, slotCount);
        maxWeight = Mathf.Max(0f, carryWeight);
        SanitizeItems();
    }

    public bool CanAccept(ItemDefinition definition, int quantity = 1)
    {
        return CanAccept(definition, quantity, ItemRarity.Common);
    }

    public bool CanAccept(ItemDefinition definition, int quantity, ItemRarity rarity)
    {
        if (definition == null || quantity <= 0)
        {
            return false;
        }

        return GetAddableQuantity(definition, quantity, rarity) >= quantity;
    }

    public int GetAddableQuantity(ItemDefinition definition, int requestedQuantity)
    {
        return GetAddableQuantity(definition, requestedQuantity, ItemRarity.Common);
    }

    public int GetAddableQuantity(ItemDefinition definition, int requestedQuantity, ItemRarity rarity)
    {
        if (definition == null || requestedQuantity <= 0)
        {
            return 0;
        }

        int remainingQuantity = requestedQuantity;
        float projectedWeight = CurrentWeight;
        int projectedSlotCount = OccupiedSlots;

        foreach (ItemInstance item in items)
        {
            if (remainingQuantity <= 0)
            {
                break;
            }

            if (item == null || !item.CanStackWith(definition, rarity))
            {
                continue;
            }

            int stackCapacity = Mathf.Max(0, item.MaxStackSize - item.Quantity);
            int weightLimitedCapacity = LimitQuantityByWeight(definition, stackCapacity, projectedWeight);
            int acceptedAmount = Mathf.Min(remainingQuantity, weightLimitedCapacity);
            remainingQuantity -= acceptedAmount;
            projectedWeight += acceptedAmount * definition.UnitWeight;
        }

        while (remainingQuantity > 0 && projectedSlotCount < MaxSlots)
        {
            int nextStackSize = Mathf.Min(remainingQuantity, definition.MaxStackSize);
            int weightLimitedStackSize = LimitQuantityByWeight(definition, nextStackSize, projectedWeight);
            if (weightLimitedStackSize <= 0)
            {
                break;
            }

            remainingQuantity -= weightLimitedStackSize;
            projectedWeight += weightLimitedStackSize * definition.UnitWeight;
            projectedSlotCount++;
        }

        return requestedQuantity - remainingQuantity;
    }

    public bool TryAddItem(ItemDefinition definition, int quantity, out int addedQuantity)
    {
        return TryAddItem(definition, quantity, ItemRarity.Common, out addedQuantity);
    }

    public bool TryAddItem(ItemDefinition definition, int quantity, ItemRarity rarity, out int addedQuantity)
    {
        addedQuantity = 0;
        if (definition == null || quantity <= 0)
        {
            return false;
        }

        SanitizeItems();

        int remainingQuantity = quantity;

        foreach (ItemInstance item in items)
        {
            if (remainingQuantity <= 0)
            {
                break;
            }

            if (item == null || !item.CanStackWith(definition, rarity))
            {
                continue;
            }

            int weightLimitedAmount = LimitQuantityByWeight(definition, remainingQuantity, CurrentWeight);
            if (weightLimitedAmount <= 0)
            {
                break;
            }

            int acceptedAmount = item.AddQuantity(weightLimitedAmount);
            addedQuantity += acceptedAmount;
            remainingQuantity -= acceptedAmount;
        }

        while (remainingQuantity > 0 && items.Count < MaxSlots)
        {
            int nextStackSize = Mathf.Min(remainingQuantity, definition.MaxStackSize);
            int weightLimitedStackSize = LimitQuantityByWeight(definition, nextStackSize, CurrentWeight);
            if (weightLimitedStackSize <= 0)
            {
                break;
            }

            items.Add(ItemInstance.Create(definition, weightLimitedStackSize, null, rarity));
            addedQuantity += weightLimitedStackSize;
            remainingQuantity -= weightLimitedStackSize;
        }

        return addedQuantity > 0;
    }

    public bool TryAddItemInstance(ItemInstance instance)
    {
        if (instance == null || !instance.IsDefined() || instance.Quantity <= 0)
        {
            return false;
        }

        SanitizeItems();
        instance.Sanitize();
        if (!CanAccept(instance))
        {
            return false;
        }

        if (instance.MaxStackSize > 1)
        {
            foreach (ItemInstance existing in items)
            {
                if (existing == null || !existing.CanStackWith(instance))
                {
                    continue;
                }

                int acceptedAmount = existing.AddQuantity(instance.Quantity);
                if (acceptedAmount >= instance.Quantity)
                {
                    return true;
                }

                if (acceptedAmount > 0)
                {
                    instance.SetQuantity(instance.Quantity - acceptedAmount);
                }
            }
        }

        if (OccupiedSlots >= MaxSlots)
        {
            return false;
        }

        if (MaxWeight > 0f && CurrentWeight + instance.TotalWeight > MaxWeight + 0.0001f)
        {
            return false;
        }

        items.Add(instance);
        return true;
    }

    public bool CanAccept(ItemInstance instance)
    {
        return GetAddableQuantity(instance) >= (instance != null ? instance.Quantity : 0);
    }

    public int GetAddableQuantity(ItemInstance instance)
    {
        if (instance == null || !instance.IsDefined() || instance.Quantity <= 0)
        {
            return 0;
        }

        instance.Sanitize();
        if (instance.Definition != null && instance.MaxStackSize > 1 && !instance.HasInstanceState)
        {
            return GetAddableQuantity(instance.Definition, instance.Quantity, instance.Rarity);
        }

        if (OccupiedSlots >= MaxSlots)
        {
            return 0;
        }

        if (MaxWeight > 0f && CurrentWeight + instance.TotalWeight > MaxWeight + 0.0001f)
        {
            return 0;
        }

        return instance.Quantity;
    }

    public bool TryRemoveItem(ItemDefinition definition, int quantity, out int removedQuantity)
    {
        removedQuantity = 0;
        if (definition == null || quantity <= 0)
        {
            return false;
        }

        for (int index = items.Count - 1; index >= 0 && removedQuantity < quantity; index--)
        {
            ItemInstance item = items[index];
            if (item == null || item.Definition != definition)
            {
                continue;
            }

            removedQuantity += item.RemoveQuantity(quantity - removedQuantity);
            if (item.Quantity <= 0)
            {
                items.RemoveAt(index);
            }
        }

        SanitizeItems();
        return removedQuantity > 0;
    }

    public bool TryTransferItemTo(InventoryContainer destination, int itemIndex, int requestedQuantity, out int movedQuantity)
    {
        movedQuantity = 0;
        if (destination == null || destination == this || requestedQuantity <= 0)
        {
            return false;
        }

        SanitizeItems();

        if (itemIndex < 0 || itemIndex >= items.Count)
        {
            return false;
        }

        ItemInstance sourceItem = items[itemIndex];
        if (sourceItem == null || !sourceItem.IsDefined())
        {
            return false;
        }

        int desiredQuantity = Mathf.Clamp(requestedQuantity, 1, sourceItem.Quantity);
        if (desiredQuantity == sourceItem.Quantity)
        {
            if (destination.TryAddItemInstance(sourceItem))
            {
                items.RemoveAt(itemIndex);
                movedQuantity = desiredQuantity;
                SanitizeItems();
                return true;
            }
        }

        int transferableQuantity = destination.GetAddableQuantity(sourceItem.Definition, desiredQuantity, sourceItem.Rarity);
        if (transferableQuantity <= 0)
        {
            return false;
        }

        if (!destination.TryAddItem(sourceItem.Definition, transferableQuantity, sourceItem.Rarity, out movedQuantity) || movedQuantity <= 0)
        {
            return false;
        }

        sourceItem.RemoveQuantity(movedQuantity);
        if (sourceItem.Quantity <= 0)
        {
            items.RemoveAt(itemIndex);
        }

        SanitizeItems();
        return true;
    }

    public bool TryExtractItem(int itemIndex, int requestedQuantity, out ItemInstance extractedItem)
    {
        extractedItem = null;
        if (requestedQuantity <= 0)
        {
            return false;
        }

        SanitizeItems();

        if (itemIndex < 0 || itemIndex >= items.Count)
        {
            return false;
        }

        ItemInstance sourceItem = items[itemIndex];
        if (sourceItem == null || !sourceItem.IsDefined())
        {
            return false;
        }

        int extractQuantity = Mathf.Clamp(requestedQuantity, 1, sourceItem.Quantity);
        if (extractQuantity >= sourceItem.Quantity)
        {
            extractedItem = sourceItem;
            items.RemoveAt(itemIndex);
            SanitizeItems();
            return true;
        }

        int removedQuantity = sourceItem.RemoveQuantity(extractQuantity);
        if (removedQuantity <= 0)
        {
            return false;
        }

        extractedItem = sourceItem.CloneWithQuantity(removedQuantity);

        if (sourceItem.Quantity <= 0)
        {
            items.RemoveAt(itemIndex);
        }

        SanitizeItems();
        return true;
    }

    public int CountItem(ItemDefinition definition)
    {
        if (definition == null)
        {
            return 0;
        }

        int count = 0;
        foreach (ItemInstance item in items)
        {
            if (item != null && item.Definition == definition)
            {
                count += item.Quantity;
            }
        }

        return count;
    }

    public void Clear()
    {
        items.Clear();
    }

    private void SanitizeItems()
    {
        if (items == null)
        {
            items = new List<ItemInstance>();
            return;
        }

        for (int index = items.Count - 1; index >= 0; index--)
        {
            ItemInstance item = items[index];
            if (item == null || !item.IsDefined())
            {
                items.RemoveAt(index);
                continue;
            }

            item.Sanitize();
        }
    }

    private int LimitQuantityByWeight(ItemDefinition definition, int requestedQuantity, float projectedWeight)
    {
        if (definition == null || requestedQuantity <= 0)
        {
            return 0;
        }

        if (MaxWeight <= 0f || definition.UnitWeight <= 0f)
        {
            return requestedQuantity;
        }

        float availableWeight = MaxWeight - projectedWeight;
        if (availableWeight <= 0f)
        {
            return 0;
        }

        int maxByWeight = Mathf.FloorToInt((availableWeight + 0.0001f) / definition.UnitWeight);
        return Mathf.Clamp(requestedQuantity, 0, maxByWeight);
    }

}
