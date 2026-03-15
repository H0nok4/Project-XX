using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class ItemSkillUtility
{
    private const float LowHealthThreshold = 0.35f;
    private const float BattleFrenzyDuration = 4f;
    private const float PerfectDodgeCooldown = 4f;

    private readonly struct RuntimeSkillDefinition
    {
        public readonly ItemSkillType Type;
        public readonly string DisplayName;
        public readonly float MinValue;
        public readonly float MaxValue;
        public readonly float Weight;
        public readonly AffixItemTarget AllowedTargets;

        public RuntimeSkillDefinition(
            ItemSkillType type,
            string displayName,
            float minValue,
            float maxValue,
            float weight,
            AffixItemTarget allowedTargets)
        {
            Type = type;
            DisplayName = displayName;
            MinValue = minValue;
            MaxValue = maxValue;
            Weight = weight;
            AllowedTargets = allowedTargets;
        }
    }

    private static readonly RuntimeSkillDefinition[] RuntimeDefinitions =
    {
        new RuntimeSkillDefinition(ItemSkillType.KillHeal, "生命收割", 10f, 26f, 1f, AffixItemTarget.Any),
        new RuntimeSkillDefinition(ItemSkillType.AmmoRecovery, "弹药回收", 2f, 7f, 0.95f, AffixItemTarget.Weapon),
        new RuntimeSkillDefinition(ItemSkillType.IronBody, "钢铁之躯", 0.06f, 0.16f, 1f, AffixItemTarget.Armor),
        new RuntimeSkillDefinition(ItemSkillType.BattleFrenzy, "战斗狂热", 0.12f, 0.3f, 0.8f, AffixItemTarget.Weapon),
        new RuntimeSkillDefinition(ItemSkillType.PerfectDodge, "完美闪避", 0.08f, 0.18f, 0.7f, AffixItemTarget.Armor),
        new RuntimeSkillDefinition(ItemSkillType.Bloodlust, "嗜血", 0.04f, 0.1f, 0.9f, AffixItemTarget.Weapon),
        new RuntimeSkillDefinition(ItemSkillType.Unyielding, "不屈意志", 0.08f, 0.2f, 0.85f, AffixItemTarget.Armor)
    };

    public static float BattleFrenzyBuffDuration => BattleFrenzyDuration;
    public static float PerfectDodgeInternalCooldown => PerfectDodgeCooldown;
    public static float UnyieldingLowHealthThreshold => LowHealthThreshold;

    public static List<ItemSkill> CloneList(IReadOnlyList<ItemSkill> source)
    {
        var cloned = new List<ItemSkill>();
        if (source == null)
        {
            return cloned;
        }

        for (int index = 0; index < source.Count; index++)
        {
            ItemSkill skill = source[index];
            if (skill == null)
            {
                continue;
            }

            cloned.Add(new ItemSkill(skill));
        }

        return cloned;
    }

    public static void SanitizeSkills(List<ItemSkill> skills)
    {
        if (skills == null)
        {
            return;
        }

        var seenTypes = new HashSet<ItemSkillType>();
        for (int index = skills.Count - 1; index >= 0; index--)
        {
            ItemSkill skill = skills[index];
            if (skill == null)
            {
                skills.RemoveAt(index);
                continue;
            }

            skill.Sanitize();
            if (!seenTypes.Add(skill.type))
            {
                skills.RemoveAt(index);
            }
        }
    }

    public static List<ItemSkill> GenerateSkills(ItemRarity rarity, int itemLevel, AffixItemTarget target)
    {
        var results = new List<ItemSkill>();
        GenerateSkills(rarity, itemLevel, target, results);
        return results;
    }

    public static void GenerateSkills(ItemRarity rarity, int itemLevel, AffixItemTarget target, List<ItemSkill> results)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();
        int skillCount = GetSkillCount(rarity);
        if (skillCount <= 0 || target == AffixItemTarget.None)
        {
            return;
        }

        var candidates = new List<RuntimeSkillDefinition>();
        for (int index = 0; index < RuntimeDefinitions.Length; index++)
        {
            RuntimeSkillDefinition definition = RuntimeDefinitions[index];
            if ((definition.AllowedTargets & target) != 0)
            {
                candidates.Add(definition);
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        float rarityScale = rarity == ItemRarity.Legendary ? 1.18f : 1f;
        float levelScale = 1f + Mathf.Clamp(itemLevel - 1, 0, 49) * 0.01f;
        var usedTypes = new HashSet<ItemSkillType>();
        while (results.Count < skillCount && candidates.Count > 0)
        {
            int selectedIndex = SelectWeighted(candidates);
            if (selectedIndex < 0 || selectedIndex >= candidates.Count)
            {
                break;
            }

            RuntimeSkillDefinition selected = candidates[selectedIndex];
            if (!usedTypes.Add(selected.Type))
            {
                candidates.RemoveAt(selectedIndex);
                continue;
            }

            float value = Random.Range(selected.MinValue, selected.MaxValue) * rarityScale * levelScale;
            value = SanitizeGeneratedValue(selected.Type, value);
            results.Add(new ItemSkill
            {
                type = selected.Type,
                value = value,
                Description = BuildDescription(selected.Type, value)
            });

            candidates.RemoveAt(selectedIndex);
        }

        SanitizeSkills(results);
    }

    public static string BuildSkillSummary(IReadOnlyList<ItemSkill> skills)
    {
        return BuildSkillSummaryInternal(skills, false);
    }

    public static string BuildSkillSummaryRich(IReadOnlyList<ItemSkill> skills)
    {
        return BuildSkillSummaryInternal(skills, true);
    }

    public static string BuildDescription(ItemSkillType type, float value)
    {
        switch (type)
        {
            case ItemSkillType.KillHeal:
                return $"击杀回复 {Mathf.RoundToInt(value)} 点生命";
            case ItemSkillType.AmmoRecovery:
                return $"击杀回复 {Mathf.RoundToInt(value)} 发当前武器弹药";
            case ItemSkillType.IronBody:
                return $"受到直击伤害 -{FormatPercent(value)}";
            case ItemSkillType.BattleFrenzy:
                return $"击杀后 {BattleFrenzyDuration:0} 秒内射速/装填 +{FormatPercent(value)}";
            case ItemSkillType.PerfectDodge:
                return $"受到直击时有 {FormatPercent(value)} 概率完全闪避";
            case ItemSkillType.Bloodlust:
                return $"命中时吸取造成伤害的 {FormatPercent(value)} 生命";
            case ItemSkillType.Unyielding:
                return $"生命低于 {FormatPercent(LowHealthThreshold)} 时所受伤害 -{FormatPercent(value)}";
            default:
                return $"{type} {value:0.##}";
        }
    }

    public static string GetDisplayName(ItemSkillType type)
    {
        for (int index = 0; index < RuntimeDefinitions.Length; index++)
        {
            if (RuntimeDefinitions[index].Type == type)
            {
                return RuntimeDefinitions[index].DisplayName;
            }
        }

        return type.ToString();
    }

    private static string BuildSkillSummaryInternal(IReadOnlyList<ItemSkill> skills, bool richText)
    {
        if (skills == null || skills.Count == 0)
        {
            return string.Empty;
        }

        var ordered = new List<ItemSkill>(skills.Count);
        for (int index = 0; index < skills.Count; index++)
        {
            ItemSkill skill = skills[index];
            if (skill != null)
            {
                ordered.Add(skill);
            }
        }

        if (ordered.Count == 0)
        {
            return string.Empty;
        }

        ordered.Sort((left, right) => left.type.CompareTo(right.type));
        var builder = new StringBuilder();
        for (int index = 0; index < ordered.Count; index++)
        {
            ItemSkill skill = ordered[index];
            string label = $"{GetDisplayName(skill.type)}: {BuildDescription(skill.type, skill.value)}";
            if (richText)
            {
                label = ItemRarityUtility.FormatRichText(label, ItemRarity.Epic);
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(label);
        }

        return builder.ToString();
    }

    private static float SanitizeGeneratedValue(ItemSkillType type, float value)
    {
        switch (type)
        {
            case ItemSkillType.AmmoRecovery:
            case ItemSkillType.KillHeal:
                return Mathf.Max(1f, Mathf.Round(value));
            case ItemSkillType.IronBody:
            case ItemSkillType.BattleFrenzy:
            case ItemSkillType.PerfectDodge:
            case ItemSkillType.Bloodlust:
            case ItemSkillType.Unyielding:
                return Mathf.Clamp(value, 0.01f, 0.9f);
            default:
                return Mathf.Max(0f, value);
        }
    }

    private static int GetSkillCount(ItemRarity rarity)
    {
        switch (ItemRarityUtility.Sanitize(rarity))
        {
            case ItemRarity.Epic:
            case ItemRarity.Legendary:
                return 1;
            default:
                return 0;
        }
    }

    private static int SelectWeighted(List<RuntimeSkillDefinition> candidates)
    {
        float totalWeight = 0f;
        for (int index = 0; index < candidates.Count; index++)
        {
            totalWeight += Mathf.Max(0f, candidates[index].Weight);
        }

        if (totalWeight <= 0f)
        {
            return candidates.Count > 0 ? 0 : -1;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int index = 0; index < candidates.Count; index++)
        {
            cumulative += Mathf.Max(0f, candidates[index].Weight);
            if (roll <= cumulative)
            {
                return index;
            }
        }

        return candidates.Count - 1;
    }

    private static string FormatPercent(float value)
    {
        return $"{Mathf.RoundToInt(Mathf.Max(0f, value) * 100f)}%";
    }
}
