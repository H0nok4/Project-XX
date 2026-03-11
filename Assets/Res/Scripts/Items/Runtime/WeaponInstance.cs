using System;
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

    public string InstanceId => instanceId;
    public PrototypeWeaponDefinition Definition => definition;
    public ItemRarity Rarity => ItemRarityUtility.Sanitize(rarity);
    public int MagazineAmmo => magazineAmmo;
    public float Durability => durability;
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
        ItemRarity itemRarity = ItemRarity.Common)
    {
        var instance = new WeaponInstance();
        instance.ApplyDefinition(weaponDefinition, startingAmmo, startingDurability, instanceIdOverride, itemRarity);
        return instance;
    }

    public void ApplyDefinition(
        PrototypeWeaponDefinition weaponDefinition,
        int startingAmmo,
        float startingDurability,
        string instanceIdOverride = null,
        ItemRarity itemRarity = ItemRarity.Common)
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
        SetInstanceId(instanceIdOverride);
    }

    public void Sanitize()
    {
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
