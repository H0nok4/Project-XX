using System;
using UnityEngine;

[Serializable]
public class ArmorInstance
{
    [SerializeField] private string instanceId = string.Empty;
    [SerializeField] private ArmorDefinition definition;
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;
    [Min(0f)]
    [SerializeField] private float currentDurability = 1f;

    public string InstanceId => instanceId;
    public ArmorDefinition Definition => definition;
    public ItemRarity Rarity => ItemRarityUtility.Sanitize(rarity);
    public float CurrentDurability => currentDurability;
    public float MaxDurability => definition != null
        ? Mathf.Max(1f, ItemRarityUtility.ScaleValue(definition.MaxDurability, Rarity))
        : Mathf.Max(1f, currentDurability);
    public string DisplayName => definition != null
        ? $"{definition.DisplayNameWithLevel} [{ItemRarityUtility.GetDisplayName(Rarity)}]"
        : "Unknown Armor";
    public string RichDisplayName => ItemRarityUtility.FormatRichText(DisplayName, Rarity);
    public float StatMultiplier => ItemRarityUtility.GetStatMultiplier(Rarity);

    public static ArmorInstance Create(
        ArmorDefinition armorDefinition,
        float durability,
        string instanceIdOverride = null,
        ItemRarity itemRarity = ItemRarity.Common)
    {
        var instance = new ArmorInstance();
        instance.ApplyDefinition(armorDefinition, durability, instanceIdOverride, itemRarity);
        return instance;
    }

    public void ApplyDefinition(
        ArmorDefinition armorDefinition,
        float durability,
        string instanceIdOverride = null,
        ItemRarity itemRarity = ItemRarity.Common)
    {
        definition = armorDefinition;
        rarity = ItemRarityUtility.Sanitize(itemRarity);
        float maxDurability = MaxDurability;
        currentDurability = Mathf.Clamp(durability, 0f, maxDurability);
        SetInstanceId(instanceIdOverride);
    }

    public void Sanitize()
    {
        rarity = ItemRarityUtility.Sanitize(rarity);
        float maxDurability = MaxDurability;
        currentDurability = Mathf.Clamp(currentDurability, 0f, maxDurability);
        EnsureInstanceId();
    }

    private void SetInstanceId(string desiredId)
    {
        if (!string.IsNullOrWhiteSpace(desiredId))
        {
            instanceId = desiredId.Trim();
        }

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
