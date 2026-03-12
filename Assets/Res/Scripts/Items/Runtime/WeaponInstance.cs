using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponInstance
{
    [SerializeField] private string instanceId = string.Empty;
    [SerializeField] private PrototypeWeaponDefinition definition;
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;
    [Min(0)]
    [SerializeField] private int magazineAmmo;
    [Min(0f)]
    [SerializeField] private float durability = 1f;
    [SerializeField] private List<ItemAffix> affixes = new List<ItemAffix>();

    public string InstanceId => instanceId;
    public PrototypeWeaponDefinition Definition => definition;
    public ItemRarity Rarity => ItemRarityUtility.Sanitize(rarity);
    public int MagazineAmmo => magazineAmmo;
    public float Durability => durability;
    public IReadOnlyList<ItemAffix> Affixes => affixes;
    public bool HasAffixes => affixes != null && affixes.Count > 0;
    public string DisplayName => definition != null
        ? $"{definition.DisplayNameWithLevel} [{ItemRarityUtility.GetDisplayName(Rarity)}]"
        : "Unknown Weapon";
    public string RichDisplayName => ItemRarityUtility.FormatRichText(DisplayName, Rarity);
    public float StatMultiplier => ItemRarityUtility.GetStatMultiplier(Rarity);

    public static WeaponInstance Create(
        PrototypeWeaponDefinition weaponDefinition,
        int startingAmmo,
        float startingDurability = 1f,
        string instanceIdOverride = null,
        ItemRarity itemRarity = ItemRarity.Common,
        IReadOnlyList<ItemAffix> affixesOverride = null,
        bool generateAffixesIfMissing = true)
    {
        var instance = new WeaponInstance();
        instance.ApplyDefinition(weaponDefinition, startingAmmo, startingDurability, instanceIdOverride, itemRarity, affixesOverride, generateAffixesIfMissing);
        return instance;
    }

    public void ApplyDefinition(
        PrototypeWeaponDefinition weaponDefinition,
        int startingAmmo,
        float startingDurability,
        string instanceIdOverride = null,
        ItemRarity itemRarity = ItemRarity.Common,
        IReadOnlyList<ItemAffix> affixesOverride = null,
        bool generateAffixesIfMissing = true)
    {
        definition = weaponDefinition;
        rarity = ItemRarityUtility.Sanitize(itemRarity);
        if (definition != null && !definition.IsMeleeWeapon)
        {
            magazineAmmo = Mathf.Clamp(startingAmmo, 0, definition.MagazineSize);
        }
        else
        {
            magazineAmmo = 0;
        }

        durability = Mathf.Max(0f, startingDurability);
        SetAffixes(affixesOverride, generateAffixesIfMissing);
        SetInstanceId(instanceIdOverride);
    }

    public void SetAffixes(IReadOnlyList<ItemAffix> newAffixes, bool generateIfMissing = true)
    {
        if (newAffixes != null)
        {
            affixes = ItemAffixUtility.CloneList(newAffixes);
        }
        else if (generateIfMissing)
        {
            affixes = GenerateAffixes();
        }
        else
        {
            affixes = new List<ItemAffix>();
        }

        ItemAffixUtility.SanitizeAffixes(affixes);
    }

    private List<ItemAffix> GenerateAffixes()
    {
        if (definition == null)
        {
            return new List<ItemAffix>();
        }

        return ItemAffixUtility.DefaultPool != null
            ? ItemAffixUtility.DefaultPool.GenerateAffixes(Rarity, definition.ItemLevel, AffixItemTarget.Weapon)
            : new List<ItemAffix>();
    }

    public void Sanitize()
    {
        if (affixes == null)
        {
            affixes = new List<ItemAffix>();
        }

        ItemAffixUtility.SanitizeAffixes(affixes);
        rarity = ItemRarityUtility.Sanitize(rarity);
        if (definition != null && !definition.IsMeleeWeapon)
        {
            magazineAmmo = Mathf.Clamp(magazineAmmo, 0, definition.MagazineSize);
        }
        else
        {
            magazineAmmo = 0;
        }

        durability = Mathf.Max(0f, durability);
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
