using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Raid/Item Definition", fileName = "ItemDefinition")]
public class ItemDefinition : ItemDefinitionBase
{
    public const int MinItemLevel = 1;
    public const int MaxItemLevel = 50;

    [SerializeField] private string itemId = "item";
    [SerializeField] private string displayName = "Item";
    [TextArea]
    [SerializeField] private string description = string.Empty;
    [Min(1)]
    [SerializeField] private int maxStackSize = 1;
    [Min(0f)]
    [SerializeField] private float unitWeight = 0.1f;
    [SerializeField] private Sprite icon;
    [Range(MinItemLevel, MaxItemLevel)]
    [SerializeField] private int itemLevel = MinItemLevel;
    [Range(MinItemLevel, MaxItemLevel)]
    [SerializeField] private int requiredLevel = MinItemLevel;

    public override string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId.Trim();
    public override string DisplayName => string.IsNullOrWhiteSpace(displayName) ? ItemId : displayName.Trim();
    public override string DisplayNameWithLevel => $"{DisplayName} (Lv {ItemLevel})";
    public override string Description => description ?? string.Empty;
    public override int MaxStackSize => Mathf.Max(1, maxStackSize);
    public override float UnitWeight => Mathf.Max(0f, unitWeight);
    public override Sprite Icon => icon;
    public override int ItemLevel => Mathf.Clamp(itemLevel, MinItemLevel, MaxItemLevel);
    public override int RequiredLevel => Mathf.Clamp(requiredLevel, MinItemLevel, MaxItemLevel);

    public void Configure(
        string id,
        string nameLabel,
        string itemDescription,
        int stackSize,
        float weight,
        Sprite itemIcon = null,
        int level = MinItemLevel,
        int required = MinItemLevel)
    {
        itemId = string.IsNullOrWhiteSpace(id) ? name : id.Trim();
        displayName = string.IsNullOrWhiteSpace(nameLabel) ? itemId : nameLabel.Trim();
        description = itemDescription ?? string.Empty;
        maxStackSize = Mathf.Max(1, stackSize);
        unitWeight = Mathf.Max(0f, weight);
        icon = itemIcon;
        itemLevel = Mathf.Clamp(level, MinItemLevel, MaxItemLevel);
        requiredLevel = Mathf.Clamp(required, MinItemLevel, MaxItemLevel);
    }

    public float GetScaledValue(float baseValue)
    {
        return GetScaledValue(baseValue, ItemLevel);
    }

    public void SetProgression(int level, int required = MinItemLevel)
    {
        itemLevel = Mathf.Clamp(level, MinItemLevel, MaxItemLevel);
        requiredLevel = Mathf.Clamp(required, MinItemLevel, MaxItemLevel);
    }

    public static float GetScaledValue(float baseValue, int level)
    {
        int clampedLevel = Mathf.Clamp(level, MinItemLevel, MaxItemLevel);
        return baseValue * (1f + (clampedLevel - MinItemLevel) * 0.1f);
    }

    private void OnValidate()
    {
        itemId = string.IsNullOrWhiteSpace(itemId) ? name : itemId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? itemId : displayName.Trim();
        maxStackSize = Mathf.Max(1, maxStackSize);
        unitWeight = Mathf.Max(0f, unitWeight);
        itemLevel = Mathf.Clamp(itemLevel, MinItemLevel, MaxItemLevel);
        requiredLevel = Mathf.Clamp(requiredLevel, MinItemLevel, MaxItemLevel);
    }
}

public enum PrototypeWeaponFireMode
{
    Semi = 0,
    Burst = 1,
    Auto = 2
}
