using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ArmorInstance
{
    [SerializeField] private string instanceId = string.Empty;
    [SerializeField] private ArmorDefinition definition;
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;
    [Min(0f)]
    [SerializeField] private float currentDurability = 1f;
    [SerializeField] private List<ItemAffix> affixes = new List<ItemAffix>();
    [SerializeField] private List<ItemSkill> skills = new List<ItemSkill>();

    private ItemAffixSummary affixSummary = ItemAffixSummary.CreateDefault();

    public string InstanceId => instanceId;
    public ArmorDefinition Definition => definition;
    public ItemRarity Rarity => ItemRarityUtility.Sanitize(rarity);
    public float CurrentDurability => currentDurability;
    public IReadOnlyList<ItemAffix> Affixes => affixes;
    public IReadOnlyList<ItemSkill> Skills => skills;
    public bool HasAffixes => affixes != null && affixes.Count > 0;
    public bool HasSkills => skills != null && skills.Count > 0;
    public ItemAffixSummary AffixSummary => affixSummary;
    public float MaxDurability => definition != null
        ? Mathf.Max(1f, ItemRarityUtility.ScaleValue(definition.MaxDurability, Rarity) * affixSummary.DurabilityMultiplier)
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
        ItemRarity itemRarity = ItemRarity.Common,
        IReadOnlyList<ItemAffix> affixesOverride = null,
        bool generateAffixesIfMissing = true,
        IReadOnlyList<ItemSkill> skillsOverride = null,
        bool generateSkillsIfMissing = true)
    {
        var instance = new ArmorInstance();
        instance.ApplyDefinition(armorDefinition, durability, instanceIdOverride, itemRarity, affixesOverride, generateAffixesIfMissing, skillsOverride, generateSkillsIfMissing);
        return instance;
    }

    public void ApplyDefinition(
        ArmorDefinition armorDefinition,
        float durability,
        string instanceIdOverride = null,
        ItemRarity itemRarity = ItemRarity.Common,
        IReadOnlyList<ItemAffix> affixesOverride = null,
        bool generateAffixesIfMissing = true,
        IReadOnlyList<ItemSkill> skillsOverride = null,
        bool generateSkillsIfMissing = true)
    {
        definition = armorDefinition;
        rarity = ItemRarityUtility.Sanitize(itemRarity);
        SetAffixes(affixesOverride, generateAffixesIfMissing);
        SetSkills(skillsOverride, generateAffixesIfMissing && generateSkillsIfMissing);
        float maxDurability = MaxDurability;
        currentDurability = Mathf.Clamp(durability, 0f, maxDurability);
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
        affixSummary = ItemAffixUtility.BuildSummary(affixes);
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
            ? ItemAffixUtility.DefaultPool.GenerateAffixes(Rarity, definition.ItemLevel, AffixItemTarget.Armor)
            : new List<ItemAffix>();
    }

    private List<ItemSkill> GenerateSkills()
    {
        return definition != null
            ? ItemSkillUtility.GenerateSkills(Rarity, definition.ItemLevel, AffixItemTarget.Armor)
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
        affixSummary = ItemAffixUtility.BuildSummary(affixes);
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
