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
    [SerializeField] private List<ItemSkill> skills = new List<ItemSkill>();

    public string InstanceId => instanceId;
    public PrototypeWeaponDefinition Definition => definition;
    public ItemRarity Rarity => ItemRarityUtility.Sanitize(rarity);
    public int MagazineAmmo => magazineAmmo;
    public float Durability => durability;
    public IReadOnlyList<ItemAffix> Affixes => affixes;
    public IReadOnlyList<ItemSkill> Skills => skills;
    public bool HasAffixes => affixes != null && affixes.Count > 0;
    public bool HasSkills => skills != null && skills.Count > 0;
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
        bool generateAffixesIfMissing = true,
        IReadOnlyList<ItemSkill> skillsOverride = null,
        bool generateSkillsIfMissing = true)
    {
        var instance = new WeaponInstance();
        instance.ApplyDefinition(weaponDefinition, startingAmmo, startingDurability, instanceIdOverride, itemRarity, affixesOverride, generateAffixesIfMissing, skillsOverride, generateSkillsIfMissing);
        return instance;
    }

    public void ApplyDefinition(
        PrototypeWeaponDefinition weaponDefinition,
        int startingAmmo,
        float startingDurability,
        string instanceIdOverride = null,
        ItemRarity itemRarity = ItemRarity.Common,
        IReadOnlyList<ItemAffix> affixesOverride = null,
        bool generateAffixesIfMissing = true,
        IReadOnlyList<ItemSkill> skillsOverride = null,
        bool generateSkillsIfMissing = true)
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
        SetSkills(skillsOverride, generateAffixesIfMissing && generateSkillsIfMissing);
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

    public void SetSkills(IReadOnlyList<ItemSkill> newSkills, bool generateIfMissing = true)
    {
        if (newSkills != null)
        {
            skills = ItemSkillUtility.CloneList(newSkills);
        }
        else if (generateIfMissing)
        {
            skills = GenerateSkills();
        }
        else
        {
            skills = new List<ItemSkill>();
        }

        ItemSkillUtility.SanitizeSkills(skills);
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

    private List<ItemSkill> GenerateSkills()
    {
        return definition != null
            ? ItemSkillUtility.GenerateSkills(Rarity, definition.ItemLevel, AffixItemTarget.Weapon)
            : new List<ItemSkill>();
    }

    public void Sanitize()
    {
        if (affixes == null)
        {
            affixes = new List<ItemAffix>();
        }

        if (skills == null)
        {
            skills = new List<ItemSkill>();
        }

        ItemAffixUtility.SanitizeAffixes(affixes);
        ItemSkillUtility.SanitizeSkills(skills);
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
