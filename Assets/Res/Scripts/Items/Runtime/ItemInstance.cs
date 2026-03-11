using System;
using UnityEngine;

[Serializable]
public class ItemInstance
{
    [SerializeField] private string instanceId = string.Empty;
    [SerializeField] private ItemDefinition definition;
    [Min(1)]
    [SerializeField] private int quantity = 1;

    public string InstanceId => instanceId;
    public ItemDefinition Definition => definition;
    public int Quantity => quantity;
    public string DisplayName => definition != null ? definition.DisplayName : "Unknown Item";
    public float TotalWeight => definition != null ? definition.UnitWeight * quantity : 0f;
    public int MaxStackSize => definition != null ? definition.MaxStackSize : 1;

    public static ItemInstance Create(ItemDefinition itemDefinition, int amount, string instanceIdOverride = null)
    {
        var instance = new ItemInstance();
        instance.SetDefinition(itemDefinition);
        instance.SetQuantity(amount);
        instance.SetInstanceId(instanceIdOverride);
        return instance;
    }

    public ItemInstance Clone()
    {
        return new ItemInstance
        {
            instanceId = Guid.NewGuid().ToString("N"),
            definition = definition,
            quantity = quantity
        };
    }

    public bool IsDefined()
    {
        return definition != null;
    }

    public bool CanStackWith(ItemDefinition itemDefinition)
    {
        return definition != null
            && definition == itemDefinition
            && definition.MaxStackSize > 1;
    }

    public int AddQuantity(int amount)
    {
        if (amount <= 0 || definition == null)
        {
            return 0;
        }

        int acceptedAmount = Mathf.Clamp(amount, 0, MaxStackSize - quantity);
        quantity += acceptedAmount;
        return acceptedAmount;
    }

    public int RemoveQuantity(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int removedAmount = Mathf.Min(quantity, amount);
        quantity -= removedAmount;
        return removedAmount;
    }

    public void SetDefinition(ItemDefinition itemDefinition)
    {
        definition = itemDefinition;
        EnsureInstanceId();
    }

    public void SetQuantity(int amount)
    {
        quantity = Mathf.Clamp(amount, 1, MaxStackSize);
        EnsureInstanceId();
    }

    public void SetInstanceId(string newInstanceId)
    {
        if (!string.IsNullOrWhiteSpace(newInstanceId))
        {
            instanceId = newInstanceId.Trim();
        }

        EnsureInstanceId();
    }

    public void Sanitize()
    {
        quantity = Mathf.Clamp(quantity, 1, MaxStackSize);
        EnsureInstanceId();
    }

    private void EnsureInstanceId()
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            instanceId = Guid.NewGuid().ToString("N");
        }
    }
}