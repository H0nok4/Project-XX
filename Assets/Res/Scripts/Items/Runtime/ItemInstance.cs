using System;
using UnityEngine;

[Serializable]
public class ItemInstance
{
    [SerializeField] private string instanceId = string.Empty;
    [SerializeField] private ItemDefinition definition;
    [SerializeField] private PrototypeWeaponDefinition weaponDefinition;
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;
    [Min(1)]
    [SerializeField] private int quantity = 1;
    [Min(0)]
    [SerializeField] private int magazineAmmo;
    [SerializeField] private float currentDurability = -1f;

    public string InstanceId => instanceId;
    public ItemDefinition Definition => definition;
    public PrototypeWeaponDefinition WeaponDefinition => weaponDefinition;
    public ItemRarity Rarity => ItemRarityUtility.Sanitize(rarity);
    public int Quantity => quantity;
    public int MagazineAmmo => weaponDefinition != null && !weaponDefinition.IsMeleeWeapon
        ? Mathf.Clamp(magazineAmmo, 0, weaponDefinition.MagazineSize)
        : 0;
    public float CurrentDurability => currentDurability;
    public bool IsWeapon => weaponDefinition != null;
    public bool IsArmor => definition is ArmorDefinition;
    public bool HasInstanceState => IsWeapon || IsArmor;
    public string DisplayName => IsWeapon
        ? $"{weaponDefinition.DisplayNameWithLevel} [{ItemRarityUtility.GetDisplayName(Rarity)}]"
        : definition != null
            ? $"{definition.DisplayNameWithLevel} [{ItemRarityUtility.GetDisplayName(Rarity)}]"
            : "Unknown Item";
    public string RichDisplayName => ItemRarityUtility.FormatRichText(DisplayName, Rarity);
    public float TotalWeight => GetUnitWeight() * quantity;
    public int MaxStackSize => IsWeapon ? 1 : definition != null ? definition.MaxStackSize : 1;

    public static ItemInstance Create(
        ItemDefinition itemDefinition,
        int amount,
        string instanceIdOverride = null,
        ItemRarity itemRarity = ItemRarity.Common)
    {
        var instance = new ItemInstance();
        instance.SetDefinition(itemDefinition);
        instance.SetRarity(itemRarity);
        instance.SetQuantity(amount);
        instance.SetInstanceId(instanceIdOverride);
        return instance;
    }

    public static ItemInstance Create(
        ArmorDefinition armorDefinition,
        float durability,
        string instanceIdOverride = null,
        ItemRarity itemRarity = ItemRarity.Common)
    {
        var instance = new ItemInstance();
        instance.SetDefinition(armorDefinition);
        instance.SetRarity(itemRarity);
        instance.SetCurrentDurability(durability);
        instance.SetQuantity(1);
        instance.SetInstanceId(instanceIdOverride);
        return instance;
    }

    public static ItemInstance Create(
        PrototypeWeaponDefinition definition,
        int loadedAmmo,
        float durability = 1f,
        string instanceIdOverride = null,
        ItemRarity itemRarity = ItemRarity.Common)
    {
        var instance = new ItemInstance();
        instance.SetWeaponDefinition(definition);
        instance.SetRarity(itemRarity);
        instance.SetMagazineAmmo(loadedAmmo);
        instance.SetCurrentDurability(durability);
        instance.SetInstanceId(instanceIdOverride);
        return instance;
    }

    public static ItemInstance Create(ArmorInstance armorInstance)
    {
        return armorInstance != null && armorInstance.Definition != null
            ? Create(armorInstance.Definition, armorInstance.CurrentDurability, armorInstance.InstanceId, armorInstance.Rarity)
            : null;
    }

    public static ItemInstance Create(WeaponInstance weaponInstance)
    {
        return weaponInstance != null && weaponInstance.Definition != null
            ? Create(weaponInstance.Definition, weaponInstance.MagazineAmmo, weaponInstance.Durability, weaponInstance.InstanceId, weaponInstance.Rarity)
            : null;
    }

    public ItemInstance Clone()
    {
        return Copy(false, quantity);
    }

    public ItemInstance CloneWithQuantity(int amount)
    {
        return Copy(false, amount);
    }

    public bool IsDefined()
    {
        return definition != null || weaponDefinition != null;
    }

    public bool CanStackWith(ItemDefinition itemDefinition)
    {
        return CanStackWith(itemDefinition, ItemRarity.Common);
    }

    public bool CanStackWith(ItemDefinition itemDefinition, ItemRarity itemRarity)
    {
        return definition != null
            && weaponDefinition == null
            && definition == itemDefinition
            && definition.MaxStackSize > 1
            && Rarity == ItemRarityUtility.Sanitize(itemRarity)
            && !IsArmor;
    }

    public bool CanStackWith(ItemInstance other)
    {
        return other != null
            && other.weaponDefinition == null
            && CanStackWith(other.definition, other.Rarity);
    }

    public int AddQuantity(int amount)
    {
        if (amount <= 0 || !IsDefined() || MaxStackSize <= 1)
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

    public WeaponInstance ToWeaponInstance()
    {
        return weaponDefinition != null
            ? WeaponInstance.Create(weaponDefinition, MagazineAmmo, Mathf.Max(0f, currentDurability), instanceId, Rarity)
            : null;
    }

    public ArmorInstance ToArmorInstance()
    {
        if (!(definition is ArmorDefinition armorDefinition))
        {
            return null;
        }

        float storedDurability = currentDurability >= 0f
            ? currentDurability
            : GetArmorMaxDurability(armorDefinition, Rarity);
        return ArmorInstance.Create(armorDefinition, storedDurability, instanceId, Rarity);
    }

    public void SetDefinition(ItemDefinition itemDefinition)
    {
        definition = itemDefinition;
        weaponDefinition = null;
        magazineAmmo = 0;
        currentDurability = -1f;
        EnsureInstanceId();
    }

    public void SetWeaponDefinition(PrototypeWeaponDefinition definitionToSet)
    {
        weaponDefinition = definitionToSet;
        definition = null;
        quantity = 1;
        magazineAmmo = 0;
        currentDurability = 1f;
        EnsureInstanceId();
    }

    public void SetRarity(ItemRarity itemRarity)
    {
        rarity = ItemRarityUtility.Sanitize(itemRarity);
    }

    public void SetQuantity(int amount)
    {
        quantity = HasInstanceState
            ? 1
            : Mathf.Clamp(amount, 1, MaxStackSize);
        EnsureInstanceId();
    }

    public void SetMagazineAmmo(int ammo)
    {
        magazineAmmo = weaponDefinition != null && !weaponDefinition.IsMeleeWeapon
            ? Mathf.Clamp(ammo, 0, weaponDefinition.MagazineSize)
            : 0;
    }

    public void SetCurrentDurability(float durability)
    {
        if (weaponDefinition != null)
        {
            currentDurability = Mathf.Max(0f, durability);
            quantity = 1;
            return;
        }

        if (definition is ArmorDefinition armorDefinition)
        {
            currentDurability = Mathf.Clamp(durability, 0f, GetArmorMaxDurability(armorDefinition, Rarity));
            quantity = 1;
            return;
        }

        currentDurability = -1f;
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
        rarity = ItemRarityUtility.Sanitize(rarity);

        if (weaponDefinition != null)
        {
            definition = null;
            quantity = 1;
            magazineAmmo = weaponDefinition.IsMeleeWeapon
                ? 0
                : Mathf.Clamp(magazineAmmo, 0, weaponDefinition.MagazineSize);
            currentDurability = Mathf.Max(0f, currentDurability >= 0f ? currentDurability : 1f);
            EnsureInstanceId();
            return;
        }

        if (definition == null)
        {
            quantity = 1;
            magazineAmmo = 0;
            currentDurability = -1f;
            EnsureInstanceId();
            return;
        }

        weaponDefinition = null;
        magazineAmmo = 0;

        if (definition is ArmorDefinition armorDefinition)
        {
            quantity = 1;
            float maxDurability = GetArmorMaxDurability(armorDefinition, Rarity);
            currentDurability = Mathf.Clamp(currentDurability >= 0f ? currentDurability : maxDurability, 0f, maxDurability);
        }
        else
        {
            quantity = Mathf.Clamp(quantity, 1, MaxStackSize);
            currentDurability = -1f;
        }

        EnsureInstanceId();
    }

    private ItemInstance Copy(bool preserveInstanceId, int amount)
    {
        var instance = new ItemInstance
        {
            instanceId = preserveInstanceId ? instanceId : Guid.NewGuid().ToString("N"),
            definition = definition,
            weaponDefinition = weaponDefinition,
            rarity = rarity,
            quantity = HasInstanceState ? 1 : Mathf.Clamp(amount, 1, MaxStackSize),
            magazineAmmo = magazineAmmo,
            currentDurability = currentDurability
        };
        instance.Sanitize();
        return instance;
    }

    private float GetUnitWeight()
    {
        if (weaponDefinition != null)
        {
            return weaponDefinition.UnitWeight;
        }

        return definition != null ? definition.UnitWeight : 0f;
    }

    private static float GetArmorMaxDurability(ArmorDefinition armorDefinition, ItemRarity rarity)
    {
        return armorDefinition != null
            ? Mathf.Max(1f, ItemRarityUtility.ScaleValue(armorDefinition.MaxDurability, rarity))
            : 1f;
    }

    private void EnsureInstanceId()
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            instanceId = Guid.NewGuid().ToString("N");
        }
    }
}
