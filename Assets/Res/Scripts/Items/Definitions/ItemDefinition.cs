using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Raid/Item Definition", fileName = "ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    [SerializeField] private string itemId = "item";
    [SerializeField] private string displayName = "Item";
    [TextArea]
    [SerializeField] private string description = string.Empty;
    [Min(1)]
    [SerializeField] private int maxStackSize = 1;
    [Min(0f)]
    [SerializeField] private float unitWeight = 0.1f;
    [SerializeField] private Sprite icon;

    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId.Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? ItemId : displayName.Trim();
    public string Description => description;
    public int MaxStackSize => Mathf.Max(1, maxStackSize);
    public float UnitWeight => Mathf.Max(0f, unitWeight);
    public Sprite Icon => icon;

    public void Configure(string id, string nameLabel, string itemDescription, int stackSize, float weight, Sprite itemIcon = null)
    {
        itemId = string.IsNullOrWhiteSpace(id) ? name : id.Trim();
        displayName = string.IsNullOrWhiteSpace(nameLabel) ? itemId : nameLabel.Trim();
        description = itemDescription ?? string.Empty;
        maxStackSize = Mathf.Max(1, stackSize);
        unitWeight = Mathf.Max(0f, weight);
        icon = itemIcon;
    }

    private void OnValidate()
    {
        itemId = string.IsNullOrWhiteSpace(itemId) ? name : itemId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? itemId : displayName.Trim();
        maxStackSize = Mathf.Max(1, maxStackSize);
        unitWeight = Mathf.Max(0f, unitWeight);
    }
}

public enum PrototypeWeaponFireMode
{
    Semi = 0,
    Burst = 1,
    Auto = 2
}

[CreateAssetMenu(menuName = "Prototype/Raid/Ammo Definition", fileName = "AmmoDefinition")]
public class AmmoDefinition : ItemDefinition
{
    [Header("Ballistics")]
    [SerializeField] private float directDamage = 24f;
    [SerializeField] private float impactForce = 16f;
    [SerializeField] private float penetrationPower = 24f;
    [SerializeField] private float armorDamage = 18f;
    [Range(0f, 1f)]
    [SerializeField] private float lightBleedChance = 0.18f;
    [Range(0f, 1f)]
    [SerializeField] private float heavyBleedChance = 0.05f;
    [Range(0f, 1f)]
    [SerializeField] private float fractureChance = 0.08f;

    public float DirectDamage => Mathf.Max(1f, directDamage);
    public float ImpactForce => Mathf.Max(0f, impactForce);
    public float PenetrationPower => Mathf.Max(0f, penetrationPower);
    public float ArmorDamage => Mathf.Max(0f, armorDamage);
    public float LightBleedChance => Mathf.Clamp01(lightBleedChance);
    public float HeavyBleedChance => Mathf.Clamp01(heavyBleedChance);
    public float FractureChance => Mathf.Clamp01(fractureChance);

    public void ConfigureAmmo(
        string id,
        string nameLabel,
        string itemDescription,
        int stackSize,
        float weight,
        float damage,
        float force,
        float penetration,
        float armorDamageValue,
        float lightBleed,
        float heavyBleed,
        float fracture,
        Sprite itemIcon = null)
    {
        Configure(id, nameLabel, itemDescription, stackSize, weight, itemIcon);
        directDamage = Mathf.Max(1f, damage);
        impactForce = Mathf.Max(0f, force);
        penetrationPower = Mathf.Max(0f, penetration);
        armorDamage = Mathf.Max(0f, armorDamageValue);
        lightBleedChance = Mathf.Clamp01(lightBleed);
        heavyBleedChance = Mathf.Clamp01(heavyBleed);
        fractureChance = Mathf.Clamp01(fracture);
    }

    private void OnValidate()
    {
        directDamage = Mathf.Max(1f, directDamage);
        impactForce = Mathf.Max(0f, impactForce);
        penetrationPower = Mathf.Max(0f, penetrationPower);
        armorDamage = Mathf.Max(0f, armorDamage);
        lightBleedChance = Mathf.Clamp01(lightBleedChance);
        heavyBleedChance = Mathf.Clamp01(heavyBleedChance);
        fractureChance = Mathf.Clamp01(fractureChance);
    }
}

[CreateAssetMenu(menuName = "Prototype/Raid/Armor Definition", fileName = "ArmorDefinition")]
public class ArmorDefinition : ItemDefinition
{
    [SerializeField] private List<string> coveredPartIds = new List<string>();
    [Min(1)]
    [SerializeField] private int armorClass = 3;
    [Min(1f)]
    [SerializeField] private float maxDurability = 60f;
    [Range(0f, 1f)]
    [SerializeField] private float bluntDamageMultiplier = 0.18f;
    [Min(0f)]
    [SerializeField] private float blockedDurabilityLossMultiplier = 1.1f;
    [Min(0f)]
    [SerializeField] private float penetratedDurabilityLossMultiplier = 1.45f;
    [Range(0f, 1f)]
    [SerializeField] private float bleedProtection = 0.45f;
    [Range(0f, 1f)]
    [SerializeField] private float fractureProtection = 0.35f;

    public IReadOnlyList<string> CoveredPartIds => coveredPartIds;
    public int ArmorClass => Mathf.Max(1, armorClass);
    public float MaxDurability => Mathf.Max(1f, maxDurability);
    public float BluntDamageMultiplier => Mathf.Clamp01(bluntDamageMultiplier);
    public float BlockedDurabilityLossMultiplier => Mathf.Max(0f, blockedDurabilityLossMultiplier);
    public float PenetratedDurabilityLossMultiplier => Mathf.Max(0f, penetratedDurabilityLossMultiplier);
    public float BleedProtection => Mathf.Clamp01(bleedProtection);
    public float FractureProtection => Mathf.Clamp01(fractureProtection);

    public void ConfigureArmor(
        string id,
        string nameLabel,
        string itemDescription,
        float weight,
        int armorLevel,
        float durability,
        float bluntDamage,
        float blockedLoss,
        float penetratedLoss,
        float bleedGuard,
        float fractureGuard,
        params string[] coverage)
    {
        Configure(id, nameLabel, itemDescription, 1, weight);
        armorClass = Mathf.Max(1, armorLevel);
        maxDurability = Mathf.Max(1f, durability);
        bluntDamageMultiplier = Mathf.Clamp01(bluntDamage);
        blockedDurabilityLossMultiplier = Mathf.Max(0f, blockedLoss);
        penetratedDurabilityLossMultiplier = Mathf.Max(0f, penetratedLoss);
        bleedProtection = Mathf.Clamp01(bleedGuard);
        fractureProtection = Mathf.Clamp01(fractureGuard);
        coveredPartIds = new List<string>();

        if (coverage != null)
        {
            foreach (string coveredPartId in coverage)
            {
                string normalizedPartId = NormalizePartId(coveredPartId);
                if (!string.IsNullOrWhiteSpace(normalizedPartId) && !coveredPartIds.Contains(normalizedPartId))
                {
                    coveredPartIds.Add(normalizedPartId);
                }
            }
        }
    }

    public bool CoversPart(string partId)
    {
        string normalizedPartId = NormalizePartId(partId);
        if (string.IsNullOrWhiteSpace(normalizedPartId))
        {
            return false;
        }

        foreach (string coveredPartId in coveredPartIds)
        {
            if (string.Equals(NormalizePartId(coveredPartId), normalizedPartId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void OnValidate()
    {
        armorClass = Mathf.Max(1, armorClass);
        maxDurability = Mathf.Max(1f, maxDurability);
        bluntDamageMultiplier = Mathf.Clamp01(bluntDamageMultiplier);
        blockedDurabilityLossMultiplier = Mathf.Max(0f, blockedDurabilityLossMultiplier);
        penetratedDurabilityLossMultiplier = Mathf.Max(0f, penetratedDurabilityLossMultiplier);
        bleedProtection = Mathf.Clamp01(bleedProtection);
        fractureProtection = Mathf.Clamp01(fractureProtection);

        if (coveredPartIds == null)
        {
            coveredPartIds = new List<string>();
            return;
        }

        var seenPartIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = coveredPartIds.Count - 1; index >= 0; index--)
        {
            string normalizedPartId = NormalizePartId(coveredPartIds[index]);
            if (string.IsNullOrWhiteSpace(normalizedPartId) || !seenPartIds.Add(normalizedPartId))
            {
                coveredPartIds.RemoveAt(index);
                continue;
            }

            coveredPartIds[index] = normalizedPartId;
        }
    }

    private static string NormalizePartId(string partId)
    {
        return PrototypeUnitDefinition.NormalizePartId(partId);
    }
}

[CreateAssetMenu(menuName = "Prototype/Raid/Medical Definition", fileName = "MedicalDefinition")]
public class MedicalItemDefinition : ItemDefinition
{
    [Header("Treatment")]
    [Min(0f)]
    [SerializeField] private float healAmount = 0f;
    [Min(0)]
    [SerializeField] private int removesLightBleeds = 0;
    [Min(0)]
    [SerializeField] private int removesHeavyBleeds = 0;
    [Min(0)]
    [SerializeField] private int curesFractures = 0;
    [Min(0f)]
    [SerializeField] private float painkillerDuration = 0f;

    public float HealAmount => Mathf.Max(0f, healAmount);
    public int RemovesLightBleeds => Mathf.Max(0, removesLightBleeds);
    public int RemovesHeavyBleeds => Mathf.Max(0, removesHeavyBleeds);
    public int CuresFractures => Mathf.Max(0, curesFractures);
    public float PainkillerDuration => Mathf.Max(0f, painkillerDuration);

    public void ConfigureMedical(
        string id,
        string nameLabel,
        string itemDescription,
        int stackSize,
        float weight,
        float healing,
        int lightBleedCures,
        int heavyBleedCures,
        int fractureCures,
        float painkillerSeconds,
        Sprite itemIcon = null)
    {
        Configure(id, nameLabel, itemDescription, stackSize, weight, itemIcon);
        healAmount = Mathf.Max(0f, healing);
        removesLightBleeds = Mathf.Max(0, lightBleedCures);
        removesHeavyBleeds = Mathf.Max(0, heavyBleedCures);
        curesFractures = Mathf.Max(0, fractureCures);
        painkillerDuration = Mathf.Max(0f, painkillerSeconds);
    }

    private void OnValidate()
    {
        healAmount = Mathf.Max(0f, healAmount);
        removesLightBleeds = Mathf.Max(0, removesLightBleeds);
        removesHeavyBleeds = Mathf.Max(0, removesHeavyBleeds);
        curesFractures = Mathf.Max(0, curesFractures);
        painkillerDuration = Mathf.Max(0f, painkillerDuration);
    }
}

[CreateAssetMenu(menuName = "Prototype/Raid/Weapon Definition", fileName = "WeaponDefinition")]
public class PrototypeWeaponDefinition : ScriptableObject
{
    [SerializeField] private string weaponId = "weapon";
    [SerializeField] private string displayName = "Weapon";
    [TextArea]
    [SerializeField] private string description = string.Empty;
    [SerializeField] private bool meleeWeapon;
    [SerializeField] private AmmoDefinition ammoDefinition;
    [Min(1)]
    [SerializeField] private int magazineSize = 30;
    [Min(0.05f)]
    [SerializeField] private float reloadDuration = 2f;
    [Min(30f)]
    [SerializeField] private float roundsPerMinute = 600f;
    [SerializeField] private PrototypeWeaponFireMode[] fireModes = { PrototypeWeaponFireMode.Auto };
    [Min(1)]
    [SerializeField] private int burstCount = 3;
    [Min(1f)]
    [SerializeField] private float effectiveRange = 65f;
    [Min(0f)]
    [SerializeField] private float spreadAngle = 0.18f;
    [Min(0f)]
    [SerializeField] private float addedImpactForce = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float lightBleedChance = 0.16f;
    [Range(0f, 1f)]
    [SerializeField] private float heavyBleedChance = 0.04f;
    [Range(0f, 1f)]
    [SerializeField] private float fractureChance = 0.08f;
    [Min(0f)]
    [SerializeField] private float penetrationPower = 6f;
    [Min(1f)]
    [SerializeField] private float meleeDamage = 55f;
    [Min(0.5f)]
    [SerializeField] private float meleeRange = 2.2f;
    [Min(0f)]
    [SerializeField] private float meleeRadius = 0.45f;
    [Min(0.05f)]
    [SerializeField] private float meleeCooldown = 0.55f;

    public string WeaponId => string.IsNullOrWhiteSpace(weaponId) ? name : weaponId.Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? WeaponId : displayName.Trim();
    public string Description => description ?? string.Empty;
    public bool IsMeleeWeapon => meleeWeapon;
    public AmmoDefinition AmmoDefinition => ammoDefinition;
    public int MagazineSize => Mathf.Max(1, magazineSize);
    public float ReloadDuration => Mathf.Max(0.05f, reloadDuration);
    public float RoundsPerMinute => Mathf.Max(30f, roundsPerMinute);
    public float SecondsPerShot => 60f / RoundsPerMinute;
    public int BurstCount => Mathf.Max(1, burstCount);
    public float EffectiveRange => Mathf.Max(1f, effectiveRange);
    public float SpreadAngle => Mathf.Max(0f, spreadAngle);
    public float AddedImpactForce => Mathf.Max(0f, addedImpactForce);
    public float LightBleedChance => Mathf.Clamp01(lightBleedChance);
    public float HeavyBleedChance => Mathf.Clamp01(heavyBleedChance);
    public float FractureChance => Mathf.Clamp01(fractureChance);
    public float PenetrationPower => Mathf.Max(0f, penetrationPower);
    public float MeleeDamage => Mathf.Max(1f, meleeDamage);
    public float MeleeRange => Mathf.Max(0.5f, meleeRange);
    public float MeleeRadius => Mathf.Max(0f, meleeRadius);
    public float MeleeCooldown => Mathf.Max(0.05f, meleeCooldown);
    public PrototypeWeaponFireMode[] FireModes => fireModes != null && fireModes.Length > 0
        ? fireModes
        : new[] { meleeWeapon ? PrototypeWeaponFireMode.Semi : PrototypeWeaponFireMode.Auto };

    public PrototypeWeaponFireMode GetFireMode(int modeIndex)
    {
        PrototypeWeaponFireMode[] modes = FireModes;
        return modes[Mathf.Clamp(modeIndex, 0, modes.Length - 1)];
    }

    public int GetValidModeIndex(int requestedIndex)
    {
        return Mathf.Clamp(requestedIndex, 0, FireModes.Length - 1);
    }

    public void ConfigureFirearm(
        string id,
        string nameLabel,
        string weaponDescription,
        AmmoDefinition ammo,
        int magSize,
        float rpm,
        float reloadSeconds,
        float range,
        float spread,
        float extraForce,
        int burstShots,
        params PrototypeWeaponFireMode[] supportedModes)
    {
        weaponId = string.IsNullOrWhiteSpace(id) ? name : id.Trim();
        displayName = string.IsNullOrWhiteSpace(nameLabel) ? weaponId : nameLabel.Trim();
        description = weaponDescription ?? string.Empty;
        meleeWeapon = false;
        ammoDefinition = ammo;
        magazineSize = Mathf.Max(1, magSize);
        roundsPerMinute = Mathf.Max(30f, rpm);
        reloadDuration = Mathf.Max(0.05f, reloadSeconds);
        effectiveRange = Mathf.Max(1f, range);
        spreadAngle = Mathf.Max(0f, spread);
        addedImpactForce = Mathf.Max(0f, extraForce);
        burstCount = Mathf.Max(1, burstShots);
        penetrationPower = ammo != null ? ammo.PenetrationPower : penetrationPower;
        SetFireModes(supportedModes);
    }

    public void ConfigureMelee(
        string id,
        string nameLabel,
        string weaponDescription,
        float damage,
        float range,
        float radius,
        float cooldown)
    {
        weaponId = string.IsNullOrWhiteSpace(id) ? name : id.Trim();
        displayName = string.IsNullOrWhiteSpace(nameLabel) ? weaponId : nameLabel.Trim();
        description = weaponDescription ?? string.Empty;
        meleeWeapon = true;
        ammoDefinition = null;
        penetrationPower = 6f;
        lightBleedChance = 0.55f;
        heavyBleedChance = 0.18f;
        fractureChance = 0.12f;
        meleeDamage = Mathf.Max(1f, damage);
        meleeRange = Mathf.Max(0.5f, range);
        meleeRadius = Mathf.Max(0f, radius);
        meleeCooldown = Mathf.Max(0.05f, cooldown);
        SetFireModes(PrototypeWeaponFireMode.Semi);
    }

    public void SetFireModes(params PrototypeWeaponFireMode[] supportedModes)
    {
        if (supportedModes == null || supportedModes.Length == 0)
        {
            fireModes = new[] { meleeWeapon ? PrototypeWeaponFireMode.Semi : PrototypeWeaponFireMode.Auto };
            return;
        }

        fireModes = new PrototypeWeaponFireMode[supportedModes.Length];
        for (int index = 0; index < supportedModes.Length; index++)
        {
            fireModes[index] = supportedModes[index];
        }
    }

    private void OnValidate()
    {
        weaponId = string.IsNullOrWhiteSpace(weaponId) ? name : weaponId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? weaponId : displayName.Trim();
        magazineSize = Mathf.Max(1, magazineSize);
        reloadDuration = Mathf.Max(0.05f, reloadDuration);
        roundsPerMinute = Mathf.Max(30f, roundsPerMinute);
        burstCount = Mathf.Max(1, burstCount);
        effectiveRange = Mathf.Max(1f, effectiveRange);
        spreadAngle = Mathf.Max(0f, spreadAngle);
        addedImpactForce = Mathf.Max(0f, addedImpactForce);
        lightBleedChance = Mathf.Clamp01(lightBleedChance);
        heavyBleedChance = Mathf.Clamp01(heavyBleedChance);
        fractureChance = Mathf.Clamp01(fractureChance);
        penetrationPower = Mathf.Max(0f, penetrationPower);
        meleeDamage = Mathf.Max(1f, meleeDamage);
        meleeRange = Mathf.Max(0.5f, meleeRange);
        meleeRadius = Mathf.Max(0f, meleeRadius);
        meleeCooldown = Mathf.Max(0.05f, meleeCooldown);
        SetFireModes(fireModes);
    }
}
