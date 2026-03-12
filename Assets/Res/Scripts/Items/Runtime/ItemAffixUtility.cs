using System.Collections.Generic;
using System.Text;
using UnityEngine;

public struct ItemAffixSummary
{
    public float DamageMultiplier;
    public float CritChance;
    public float CritDamageMultiplier;
    public float ArmorPenetrationBonus;
    public float FireRateMultiplier;
    public float ReloadSpeedMultiplier;
    public float SpreadMultiplier;
    public float EffectiveRangeMultiplier;
    public float ArmorClassBonus;
    public float DamageReduction;
    public float MoveSpeedMultiplier;
    public float DurabilityMultiplier;

    public static ItemAffixSummary CreateDefault()
    {
        return new ItemAffixSummary
        {
            DamageMultiplier = 1f,
            CritChance = 0f,
            CritDamageMultiplier = 1f,
            ArmorPenetrationBonus = 0f,
            FireRateMultiplier = 1f,
            ReloadSpeedMultiplier = 1f,
            SpreadMultiplier = 1f,
            EffectiveRangeMultiplier = 1f,
            ArmorClassBonus = 0f,
            DamageReduction = 0f,
            MoveSpeedMultiplier = 1f,
            DurabilityMultiplier = 1f
        };
    }
}

public static class ItemAffixUtility
{
    private const string DefaultPoolResource = "AffixPool";
    private static AffixPool cachedPool;

    public static AffixPool DefaultPool
    {
        get
        {
            if (cachedPool == null)
            {
                cachedPool = Resources.Load<AffixPool>(DefaultPoolResource);
                if (cachedPool == null)
                {
                    cachedPool = CreateRuntimeDefaultPool();
                }
            }

            return cachedPool;
        }
    }

    public static void SetDefaultPool(AffixPool pool)
    {
        cachedPool = pool;
    }

    private static AffixPool CreateRuntimeDefaultPool()
    {
        var pool = ScriptableObject.CreateInstance<AffixPool>();
        pool.hideFlags = HideFlags.HideAndDontSave;

        var definitions = new List<AffixDefinition>
        {
            CreateRuntimeDefinition(AffixType.DamageBonus, AffixCategory.Offensive, AffixItemTarget.Weapon, 0.05f, 0.25f, AffixValueKind.Percent),
            CreateRuntimeDefinition(AffixType.CritChance, AffixCategory.Offensive, AffixItemTarget.Weapon, 0.03f, 0.15f, AffixValueKind.Percent),
            CreateRuntimeDefinition(AffixType.CritDamage, AffixCategory.Offensive, AffixItemTarget.Weapon, 0.2f, 1f, AffixValueKind.Percent),
            CreateRuntimeDefinition(AffixType.ArmorPenetration, AffixCategory.Offensive, AffixItemTarget.Weapon, 5f, 25f, AffixValueKind.Flat),
            CreateRuntimeDefinition(AffixType.FireRate, AffixCategory.Offensive, AffixItemTarget.Weapon, 0.05f, 0.2f, AffixValueKind.Percent),
            CreateRuntimeDefinition(AffixType.Accuracy, AffixCategory.Offensive, AffixItemTarget.Weapon, 0.1f, 0.3f, AffixValueKind.Percent),
            CreateRuntimeDefinition(AffixType.EffectiveRange, AffixCategory.Offensive, AffixItemTarget.Weapon, 0.1f, 0.3f, AffixValueKind.Percent),
            CreateRuntimeDefinition(AffixType.ReloadSpeed, AffixCategory.Mobility, AffixItemTarget.Weapon, 0.1f, 0.4f, AffixValueKind.Percent),
            CreateRuntimeDefinition(AffixType.ArmorBonus, AffixCategory.Defensive, AffixItemTarget.Armor, 10f, 50f, AffixValueKind.Flat),
            CreateRuntimeDefinition(AffixType.ArmorLevel, AffixCategory.Defensive, AffixItemTarget.Armor, 1f, 2f, AffixValueKind.Flat),
            CreateRuntimeDefinition(AffixType.DurabilityBonus, AffixCategory.Defensive, AffixItemTarget.Armor, 0.2f, 1f, AffixValueKind.Percent),
            CreateRuntimeDefinition(AffixType.DamageReduction, AffixCategory.Defensive, AffixItemTarget.Armor, 0.03f, 0.15f, AffixValueKind.Percent),
            CreateRuntimeDefinition(AffixType.MoveSpeed, AffixCategory.Mobility, AffixItemTarget.Armor, 0.05f, 0.2f, AffixValueKind.Percent)
        };

        pool.ConfigureRuntime(definitions, 0, 1, 2, 3, 4, 1.6f, 0.12f, 0.12f);
        return pool;
    }

    private static AffixDefinition CreateRuntimeDefinition(
        AffixType type,
        AffixCategory category,
        AffixItemTarget targets,
        float minValue,
        float maxValue,
        AffixValueKind valueKind,
        int minTier = 1,
        int maxTier = 3,
        float weight = 1f,
        string groupId = "")
    {
        var definition = ScriptableObject.CreateInstance<AffixDefinition>();
        definition.hideFlags = HideFlags.HideAndDontSave;
        definition.ConfigureRuntime(
            type,
            category,
            valueKind,
            GetDisplayName(type),
            minValue,
            maxValue,
            minTier,
            maxTier,
            weight,
            targets,
            groupId);
        return definition;
    }

    public static AffixItemTarget ResolveTarget(ItemDefinition definition)
    {
        if (definition is ArmorDefinition)
        {
            return AffixItemTarget.Armor;
        }

        return AffixItemTarget.None;
    }

    public static AffixItemTarget ResolveTarget(PrototypeWeaponDefinition definition)
    {
        return definition != null ? AffixItemTarget.Weapon : AffixItemTarget.None;
    }

    public static List<ItemAffix> CloneList(IReadOnlyList<ItemAffix> source)
    {
        var cloned = new List<ItemAffix>();
        if (source == null)
        {
            return cloned;
        }

        for (int index = 0; index < source.Count; index++)
        {
            ItemAffix affix = source[index];
            if (affix == null)
            {
                continue;
            }

            cloned.Add(new ItemAffix(affix));
        }

        return cloned;
    }

    public static void SanitizeAffixes(List<ItemAffix> affixes)
    {
        if (affixes == null)
        {
            return;
        }

        var seenTypes = new HashSet<AffixType>();
        for (int index = affixes.Count - 1; index >= 0; index--)
        {
            ItemAffix affix = affixes[index];
            if (affix == null)
            {
                affixes.RemoveAt(index);
                continue;
            }

            if (!seenTypes.Add(affix.type))
            {
                affixes.RemoveAt(index);
                continue;
            }

            affix.tier = Mathf.Max(1, affix.tier);
        }
    }

    public static ItemAffixSummary BuildSummary(IReadOnlyList<ItemAffix> affixes)
    {
        ItemAffixSummary summary = ItemAffixSummary.CreateDefault();
        if (affixes == null)
        {
            return summary;
        }

        float damageBonus = 0f;
        float critChance = 0f;
        float critDamage = 0f;
        float armorPenetration = 0f;
        float fireRateBonus = 0f;
        float reloadBonus = 0f;
        float accuracyBonus = 0f;
        float rangeBonus = 0f;
        float armorBonus = 0f;
        float armorLevelBonus = 0f;
        float damageReduction = 0f;
        float moveSpeedBonus = 0f;
        float durabilityBonus = 0f;

        for (int index = 0; index < affixes.Count; index++)
        {
            ItemAffix affix = affixes[index];
            if (affix == null)
            {
                continue;
            }

            switch (affix.type)
            {
                case AffixType.DamageBonus:
                    damageBonus += affix.value;
                    break;

                case AffixType.CritChance:
                    critChance += affix.value;
                    break;

                case AffixType.CritDamage:
                    critDamage += affix.value;
                    break;

                case AffixType.ArmorPenetration:
                    armorPenetration += affix.value;
                    break;

                case AffixType.FireRate:
                    fireRateBonus += affix.value;
                    break;

                case AffixType.ReloadSpeed:
                    reloadBonus += affix.value;
                    break;

                case AffixType.Accuracy:
                    accuracyBonus += affix.value;
                    break;

                case AffixType.EffectiveRange:
                    rangeBonus += affix.value;
                    break;

                case AffixType.ArmorBonus:
                    armorBonus += affix.value;
                    break;

                case AffixType.ArmorLevel:
                    armorLevelBonus += affix.value;
                    break;

                case AffixType.DamageReduction:
                    damageReduction += affix.value;
                    break;

                case AffixType.MoveSpeed:
                    moveSpeedBonus += affix.value;
                    break;

                case AffixType.DurabilityBonus:
                    durabilityBonus += affix.value;
                    break;
            }
        }

        summary.DamageMultiplier = Mathf.Max(0.1f, 1f + damageBonus);
        summary.CritChance = Mathf.Clamp01(critChance);
        summary.CritDamageMultiplier = Mathf.Max(1f, 1f + critDamage);
        summary.ArmorPenetrationBonus = armorPenetration;
        summary.FireRateMultiplier = Mathf.Clamp(1f + fireRateBonus, 0.4f, 3f);
        summary.ReloadSpeedMultiplier = Mathf.Clamp(1f + reloadBonus, 0.35f, 3f);
        summary.SpreadMultiplier = Mathf.Clamp(1f - accuracyBonus, 0.25f, 1f);
        summary.EffectiveRangeMultiplier = Mathf.Clamp(1f + rangeBonus, 0.5f, 3f);
        summary.ArmorClassBonus = Mathf.Max(0f, armorBonus + armorLevelBonus);
        summary.DamageReduction = Mathf.Clamp01(damageReduction);
        summary.MoveSpeedMultiplier = Mathf.Clamp(1f + moveSpeedBonus, 0.4f, 1.8f);
        summary.DurabilityMultiplier = Mathf.Clamp(1f + durabilityBonus, 0.25f, 3f);

        return summary;
    }

    public static string BuildAffixSummary(IReadOnlyList<ItemAffix> affixes, AffixPool pool = null)
    {
        return BuildAffixSummaryInternal(affixes, pool, false, false, false);
    }

    public static string BuildAffixSummaryRich(
        IReadOnlyList<ItemAffix> affixes,
        AffixPool pool = null,
        bool includeCategoryLabel = true,
        bool includeTierLabel = false)
    {
        return BuildAffixSummaryInternal(affixes, pool, true, includeCategoryLabel, includeTierLabel);
    }

    private static string BuildAffixSummaryInternal(
        IReadOnlyList<ItemAffix> affixes,
        AffixPool pool,
        bool richText,
        bool includeCategoryLabel,
        bool includeTierLabel)
    {
        if (affixes == null || affixes.Count == 0)
        {
            return string.Empty;
        }

        AffixPool resolvedPool = pool ?? DefaultPool;
        var ordered = new List<ItemAffix>(affixes.Count);
        for (int index = 0; index < affixes.Count; index++)
        {
            ItemAffix affix = affixes[index];
            if (affix == null)
            {
                continue;
            }

            ordered.Add(affix);
        }

        if (ordered.Count == 0)
        {
            return string.Empty;
        }

        ordered.Sort(CompareAffixes);
        var builder = new StringBuilder();
        for (int index = 0; index < ordered.Count; index++)
        {
            ItemAffix affix = ordered[index];
            string label = richText
                ? FormatAffixRich(affix, resolvedPool, includeCategoryLabel, includeTierLabel)
                : FormatAffix(affix, resolvedPool);
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(label);
        }

        return builder.ToString();
    }

    public static string FormatAffixRich(
        ItemAffix affix,
        AffixPool pool = null,
        bool includeCategoryLabel = true,
        bool includeTierLabel = false)
    {
        if (affix == null)
        {
            return string.Empty;
        }

        string label = FormatAffix(affix, pool);
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Empty;
        }

        string categoryLabel = includeCategoryLabel ? $"[{GetCategoryLabel(affix.category)}] " : string.Empty;
        string tierLabel = includeTierLabel ? $" (T{Mathf.Max(1, affix.tier)})" : string.Empty;
        string richText = $"{categoryLabel}{label}{tierLabel}";
        return ItemRarityUtility.FormatRichText(richText, GetTierRarity(affix.tier));
    }

    private static int CompareAffixes(ItemAffix left, ItemAffix right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int categoryOrder = left.category.CompareTo(right.category);
        if (categoryOrder != 0)
        {
            return categoryOrder;
        }

        int typeOrder = left.type.CompareTo(right.type);
        if (typeOrder != 0)
        {
            return typeOrder;
        }

        return right.tier.CompareTo(left.tier);
    }

    private static string GetCategoryLabel(AffixCategory category)
    {
        switch (category)
        {
            case AffixCategory.Offensive:
                return "Offense";
            case AffixCategory.Defensive:
                return "Defense";
            case AffixCategory.Mobility:
                return "Mobility";
            case AffixCategory.Survival:
                return "Survival";
            case AffixCategory.Special:
                return "Special";
            default:
                return "Affix";
        }
    }

    private static ItemRarity GetTierRarity(int tier)
    {
        int clamped = Mathf.Clamp(tier, 1, 5);
        return (ItemRarity)(clamped - 1);
    }

    public static string FormatAffix(ItemAffix affix, AffixPool pool = null)
    {
        if (affix == null)
        {
            return string.Empty;
        }

        AffixDefinition definition = pool != null ? pool.FindDefinition(affix.type) : null;
        string displayName = !string.IsNullOrWhiteSpace(definition != null ? definition.DisplayName : string.Empty)
            ? definition.DisplayName
            : GetDisplayName(affix.type);
        AffixValueKind valueKind = definition != null
            ? definition.ValueKind
            : GetDefaultValueKind(affix.type);
        string valueText = FormatValue(affix.value, valueKind);
        return string.IsNullOrWhiteSpace(displayName) ? valueText : $"{displayName} {valueText}";
    }

    public static string FormatValue(float value, AffixValueKind valueKind)
    {
        string sign = value >= 0f ? "+" : string.Empty;
        if (valueKind == AffixValueKind.Percent)
        {
            return $"{sign}{value * 100f:0.#}%";
        }

        return $"{sign}{value:0.#}";
    }

    public static AffixValueKind GetDefaultValueKind(AffixType type)
    {
        switch (type)
        {
            case AffixType.ArmorPenetration:
            case AffixType.ArmorBonus:
            case AffixType.ArmorLevel:
                return AffixValueKind.Flat;

            default:
                return AffixValueKind.Percent;
        }
    }

    public static string GetDisplayName(AffixType type)
    {
        switch (type)
        {
            case AffixType.DamageBonus:
                return "Damage Bonus";
            case AffixType.CritChance:
                return "Crit Chance";
            case AffixType.CritDamage:
                return "Crit Damage";
            case AffixType.ArmorPenetration:
                return "Armor Penetration";
            case AffixType.FireRate:
                return "Fire Rate";
            case AffixType.Accuracy:
                return "Accuracy";
            case AffixType.EffectiveRange:
                return "Effective Range";
            case AffixType.ArmorBonus:
                return "Armor Bonus";
            case AffixType.ArmorLevel:
                return "Armor Level";
            case AffixType.DurabilityBonus:
                return "Durability";
            case AffixType.DamageReduction:
                return "Damage Reduction";
            case AffixType.BodyPartProtection:
                return "Part Protection";
            case AffixType.MoveSpeed:
                return "Move Speed";
            case AffixType.ReloadSpeed:
                return "Reload Speed";
            case AffixType.SwapSpeed:
                return "Swap Speed";
            case AffixType.AimSpeed:
                return "Aim Speed";
            case AffixType.WeightReduction:
                return "Weight Reduction";
            case AffixType.HealthBonus:
                return "Health";
            case AffixType.StaminaBonus:
                return "Stamina";
            case AffixType.StaminaRegen:
                return "Stamina Regen";
            case AffixType.HealingBonus:
                return "Healing Bonus";
            case AffixType.StatusResistance:
                return "Status Resistance";
            case AffixType.ExperienceBonus:
                return "Experience";
            case AffixType.LootFind:
                return "Loot Find";
            default:
                return "Affix";
        }
    }
}
