using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class CharacterStatAggregator
{
    [SerializeField, HideInInspector] private PlayerDerivedStats derivedStats = new PlayerDerivedStats();
    [SerializeField, HideInInspector] private List<ModifierRuntime> activeModifiers = new List<ModifierRuntime>();
    [SerializeField, HideInInspector] private List<string> unlockedNodeIds = new List<string>();

    public PlayerDerivedStats DerivedStats => derivedStats;
    public IReadOnlyList<ModifierRuntime> ActiveModifiers => activeModifiers;
    public IReadOnlyList<string> UnlockedNodeIds => unlockedNodeIds;

    public void Rebuild(PlayerProgressionData progression)
    {
        Rebuild(progression, null, null);
    }

    public void Rebuild(
        PlayerProgressionData progression,
        ItemInstance activeWeapon,
        IReadOnlyList<ItemAffixSummary> armorAffixSummaries)
    {
        derivedStats ??= new PlayerDerivedStats();
        activeModifiers ??= new List<ModifierRuntime>();
        unlockedNodeIds ??= new List<string>();

        derivedStats.Reset();
        activeModifiers.Clear();
        unlockedNodeIds.Clear();

        if (progression == null)
        {
            derivedStats.Sanitize();
            return;
        }

        PrototypePlayerProgressionUtility.Sanitize(progression);
        AddLevelModifiers(progression.playerLevel);
        AddAttributeModifiers(progression.attributeSet);
        AddSkillTreeModifiers(progression.skillTree);
        AddEquipmentModifiers(activeWeapon, armorAffixSummaries);
        ApplyModifiersToDerivedStats();
        derivedStats.Sanitize();
    }

    public string BuildDerivedSummary()
    {
        derivedStats ??= new PlayerDerivedStats();
        derivedStats.Sanitize();

        var builder = new StringBuilder(192);
        builder.Append($"生命加成 +{Mathf.RoundToInt(derivedStats.maxHealthBonus)}  体力加成 +{Mathf.RoundToInt(derivedStats.maxStaminaBonus)}\n");
        builder.Append($"负重 {derivedStats.carryWeight:0.#}  伤害 x{derivedStats.damageMultiplier:0.00}  治疗 x{derivedStats.healingMultiplier:0.00}\n");
        builder.Append($"装填 x{derivedStats.reloadSpeedMultiplier:0.00}  射速 x{derivedStats.fireRateMultiplier:0.00}  移速 x{derivedStats.moveSpeedMultiplier:0.00}\n");
        builder.Append($"控枪 x{derivedStats.recoilControlMultiplier:0.00}  交互范围 x{derivedStats.interactionRangeMultiplier:0.00}\n");
        builder.Append($"暴击 {derivedStats.critChance * 100f:0.#}%  暴伤 x{derivedStats.critDamageMultiplier:0.00}  穿甲 +{derivedStats.armorPenetrationBonus:0.#}\n");
        builder.Append($"散布 x{derivedStats.spreadMultiplier:0.00}  射程 x{derivedStats.effectiveRangeMultiplier:0.00}");
        return builder.ToString();
    }

    public string BuildModifierBreakdown(int maxEntries = 12)
    {
        if (activeModifiers == null || activeModifiers.Count == 0)
        {
            return "当前没有额外成长修正。";
        }

        int limit = Mathf.Clamp(maxEntries, 1, activeModifiers.Count);
        var builder = new StringBuilder(256);
        for (int index = 0; index < limit; index++)
        {
            ModifierRuntime modifier = activeModifiers[index];
            if (modifier == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(modifier.sourceLabel);
            builder.Append("：");
            builder.Append(PrototypePlayerProgressionUtility.FormatModifier(modifier.statType, modifier.operation, modifier.value));
        }

        if (activeModifiers.Count > limit)
        {
            builder.Append($"\n... 其余 {activeModifiers.Count - limit} 条修正省略");
        }

        return builder.ToString();
    }

    public static CharacterStatAggregator BuildPreview(PlayerProgressionData progression)
    {
        var aggregator = new CharacterStatAggregator();
        aggregator.Rebuild(progression);
        return aggregator;
    }

    public static CharacterStatAggregator BuildPreview(
        PlayerProgressionData progression,
        ItemInstance activeWeapon,
        IReadOnlyList<ItemAffixSummary> armorAffixSummaries)
    {
        var aggregator = new CharacterStatAggregator();
        aggregator.Rebuild(progression, activeWeapon, armorAffixSummaries);
        return aggregator;
    }

    private void AddLevelModifiers(int playerLevel)
    {
        int levelAboveBase = Mathf.Max(0, playerLevel - 1);
        if (levelAboveBase <= 0)
        {
            return;
        }

        AddModifier("level_growth", "等级成长", CharacterStatType.MaxHealthBonus, ModifierOperation.Add, levelAboveBase * PrototypePlayerProgressionUtility.LevelHealthBonusPerLevel);
        AddModifier("level_growth", "等级成长", CharacterStatType.MaxStaminaBonus, ModifierOperation.Add, levelAboveBase * PrototypePlayerProgressionUtility.LevelStaminaBonusPerLevel);
        AddModifier("level_growth", "等级成长", CharacterStatType.DamageMultiplier, ModifierOperation.Add, levelAboveBase * PrototypePlayerProgressionUtility.LevelDamageBonusPerLevel);
        AddModifier("level_growth", "等级成长", CharacterStatType.HealingMultiplier, ModifierOperation.Add, levelAboveBase * PrototypePlayerProgressionUtility.LevelHealingBonusPerLevel);
    }

    private void AddAttributeModifiers(PlayerAttributeSet attributes)
    {
        attributes ??= new PlayerAttributeSet();
        attributes.Sanitize();

        AddStrengthModifiers(attributes.GetValue(PlayerAttributeType.Strength));
        AddEnduranceModifiers(attributes.GetValue(PlayerAttributeType.Endurance));
        AddAgilityModifiers(attributes.GetValue(PlayerAttributeType.Agility));
        AddPerceptionModifiers(attributes.GetValue(PlayerAttributeType.Perception));
        AddTechModifiers(attributes.GetValue(PlayerAttributeType.Tech));
    }

    private void AddStrengthModifiers(int attributeValue)
    {
        int pointsAboveBase = Mathf.Max(0, attributeValue - PrototypePlayerProgressionUtility.DefaultAttributeValue);
        if (pointsAboveBase <= 0)
        {
            return;
        }

        AddModifier("attribute_strength", "力量", CharacterStatType.CarryWeight, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.StrengthCarryWeightPerPoint);
        AddModifier("attribute_strength", "力量", CharacterStatType.DamageMultiplier, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.StrengthDamageBonusPerPoint);
        AddModifier("attribute_strength", "力量", CharacterStatType.RecoilControlMultiplier, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.StrengthRecoilControlPerPoint);
    }

    private void AddEnduranceModifiers(int attributeValue)
    {
        int pointsAboveBase = Mathf.Max(0, attributeValue - PrototypePlayerProgressionUtility.DefaultAttributeValue);
        if (pointsAboveBase <= 0)
        {
            return;
        }

        AddModifier("attribute_endurance", "体质", CharacterStatType.MaxHealthBonus, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.EnduranceHealthBonusPerPoint);
        AddModifier("attribute_endurance", "体质", CharacterStatType.MaxStaminaBonus, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.EnduranceStaminaBonusPerPoint);
        AddModifier("attribute_endurance", "体质", CharacterStatType.HealingMultiplier, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.EnduranceHealingBonusPerPoint);
    }

    private void AddAgilityModifiers(int attributeValue)
    {
        int pointsAboveBase = Mathf.Max(0, attributeValue - PrototypePlayerProgressionUtility.DefaultAttributeValue);
        if (pointsAboveBase <= 0)
        {
            return;
        }

        AddModifier("attribute_agility", "敏捷", CharacterStatType.MoveSpeedMultiplier, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.AgilityMoveSpeedPerPoint);
        AddModifier("attribute_agility", "敏捷", CharacterStatType.ReloadSpeedMultiplier, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.AgilityReloadSpeedPerPoint);
        AddModifier("attribute_agility", "敏捷", CharacterStatType.FireRateMultiplier, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.AgilityFireRatePerPoint);
    }

    private void AddPerceptionModifiers(int attributeValue)
    {
        int pointsAboveBase = Mathf.Max(0, attributeValue - PrototypePlayerProgressionUtility.DefaultAttributeValue);
        if (pointsAboveBase <= 0)
        {
            return;
        }

        AddModifier("attribute_perception", "感知", CharacterStatType.DamageMultiplier, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.PerceptionDamageBonusPerPoint);
        AddModifier("attribute_perception", "感知", CharacterStatType.InteractionRangeMultiplier, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.PerceptionInteractionRangePerPoint);
    }

    private void AddTechModifiers(int attributeValue)
    {
        int pointsAboveBase = Mathf.Max(0, attributeValue - PrototypePlayerProgressionUtility.DefaultAttributeValue);
        if (pointsAboveBase <= 0)
        {
            return;
        }

        AddModifier("attribute_tech", "技术", CharacterStatType.HealingMultiplier, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.TechHealingBonusPerPoint);
        AddModifier("attribute_tech", "技术", CharacterStatType.ReloadSpeedMultiplier, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.TechReloadSpeedPerPoint);
        AddModifier("attribute_tech", "技术", CharacterStatType.CarryWeight, ModifierOperation.Add, pointsAboveBase * PrototypePlayerProgressionUtility.TechCarryWeightPerPoint);
    }

    private void AddSkillTreeModifiers(PlayerSkillTree skillTree)
    {
        if (skillTree == null || skillTree.unlockedNodeIds == null)
        {
            return;
        }

        for (int index = 0; index < skillTree.unlockedNodeIds.Count; index++)
        {
            string nodeId = skillTree.unlockedNodeIds[index];
            SkillNodeDefinition definition = PlayerSkillTreeCatalog.GetDefinition(nodeId);
            if (definition == null)
            {
                continue;
            }

            unlockedNodeIds.Add(definition.nodeId);
            if (definition.modifiers == null)
            {
                continue;
            }

            for (int modifierIndex = 0; modifierIndex < definition.modifiers.Count; modifierIndex++)
            {
                ModifierDefinition modifier = definition.modifiers[modifierIndex];
                if (modifier == null)
                {
                    continue;
                }

                AddModifier(definition.nodeId, definition.displayName, modifier.statType, modifier.operation, modifier.value, modifier.description);
            }
        }
    }

    private void AddEquipmentModifiers(ItemInstance activeWeapon, IReadOnlyList<ItemAffixSummary> armorAffixSummaries)
    {
        AddActiveWeaponModifiers(activeWeapon);
        AddArmorModifiers(armorAffixSummaries);
    }

    private void AddActiveWeaponModifiers(ItemInstance activeWeapon)
    {
        if (activeWeapon == null || !activeWeapon.IsDefined())
        {
            return;
        }

        ItemAffixSummary summary = ItemAffixUtility.BuildSummary(activeWeapon.Affixes);
        string sourceLabel = $"{activeWeapon.DisplayName} 词条";
        AddRelativeModifierIfNeeded("equipment_weapon_damage", sourceLabel, CharacterStatType.DamageMultiplier, summary.DamageMultiplier - 1f);
        AddRelativeModifierIfNeeded("equipment_weapon_fire_rate", sourceLabel, CharacterStatType.FireRateMultiplier, summary.FireRateMultiplier - 1f);
        AddRelativeModifierIfNeeded("equipment_weapon_reload", sourceLabel, CharacterStatType.ReloadSpeedMultiplier, summary.ReloadSpeedMultiplier - 1f);
        AddRelativeModifierIfNeeded("equipment_weapon_crit_chance", sourceLabel, CharacterStatType.CritChance, summary.CritChance);
        AddRelativeModifierIfNeeded("equipment_weapon_crit_damage", sourceLabel, CharacterStatType.CritDamageMultiplier, summary.CritDamageMultiplier - 1f);
        AddRelativeModifierIfNeeded("equipment_weapon_armor_pen", sourceLabel, CharacterStatType.ArmorPenetrationBonus, summary.ArmorPenetrationBonus);
        AddRelativeModifierIfNeeded("equipment_weapon_spread", sourceLabel, CharacterStatType.SpreadMultiplier, summary.SpreadMultiplier - 1f);
        AddRelativeModifierIfNeeded("equipment_weapon_range", sourceLabel, CharacterStatType.EffectiveRangeMultiplier, summary.EffectiveRangeMultiplier - 1f);
    }

    private void AddArmorModifiers(IReadOnlyList<ItemAffixSummary> armorAffixSummaries)
    {
        if (armorAffixSummaries == null || armorAffixSummaries.Count == 0)
        {
            return;
        }

        float moveSpeedBonus = 0f;
        for (int index = 0; index < armorAffixSummaries.Count; index++)
        {
            moveSpeedBonus += armorAffixSummaries[index].MoveSpeedMultiplier - 1f;
        }

        AddRelativeModifierIfNeeded("equipment_armor_move", "护甲词条", CharacterStatType.MoveSpeedMultiplier, moveSpeedBonus);
    }

    private void AddRelativeModifierIfNeeded(
        string sourceId,
        string sourceLabel,
        CharacterStatType statType,
        float relativeValue)
    {
        if (Mathf.Abs(relativeValue) <= 0.0001f)
        {
            return;
        }

        AddModifier(sourceId, sourceLabel, statType, ModifierOperation.Add, relativeValue);
    }

    private void AddModifier(
        string sourceId,
        string sourceLabel,
        CharacterStatType statType,
        ModifierOperation operation,
        float value,
        string sourceDetail = "")
    {
        activeModifiers.Add(new ModifierRuntime
        {
            sourceId = string.IsNullOrWhiteSpace(sourceId) ? statType.ToString() : sourceId.Trim(),
            sourceLabel = string.IsNullOrWhiteSpace(sourceLabel) ? statType.ToString() : sourceLabel.Trim(),
            sourceDetail = sourceDetail ?? string.Empty,
            statType = statType,
            operation = operation,
            value = value
        });
    }

    private void ApplyModifiersToDerivedStats()
    {
        for (int index = 0; index < activeModifiers.Count; index++)
        {
            ModifierRuntime modifier = activeModifiers[index];
            if (modifier == null)
            {
                continue;
            }

            ApplyModifier(modifier);
        }
    }

    private void ApplyModifier(ModifierRuntime modifier)
    {
        switch (modifier.statType)
        {
            case CharacterStatType.MaxHealthBonus:
                derivedStats.maxHealthBonus = ApplyFloat(derivedStats.maxHealthBonus, modifier.operation, modifier.value);
                break;
            case CharacterStatType.MaxStaminaBonus:
                derivedStats.maxStaminaBonus = ApplyFloat(derivedStats.maxStaminaBonus, modifier.operation, modifier.value);
                break;
            case CharacterStatType.CarryWeight:
                derivedStats.carryWeight = ApplyFloat(derivedStats.carryWeight, modifier.operation, modifier.value);
                break;
            case CharacterStatType.DamageMultiplier:
                derivedStats.damageMultiplier = ApplyMultiplier(derivedStats.damageMultiplier, modifier.operation, modifier.value);
                break;
            case CharacterStatType.HealingMultiplier:
                derivedStats.healingMultiplier = ApplyMultiplier(derivedStats.healingMultiplier, modifier.operation, modifier.value);
                break;
            case CharacterStatType.ReloadSpeedMultiplier:
                derivedStats.reloadSpeedMultiplier = ApplyMultiplier(derivedStats.reloadSpeedMultiplier, modifier.operation, modifier.value);
                break;
            case CharacterStatType.FireRateMultiplier:
                derivedStats.fireRateMultiplier = ApplyMultiplier(derivedStats.fireRateMultiplier, modifier.operation, modifier.value);
                break;
            case CharacterStatType.MoveSpeedMultiplier:
                derivedStats.moveSpeedMultiplier = ApplyMultiplier(derivedStats.moveSpeedMultiplier, modifier.operation, modifier.value);
                break;
            case CharacterStatType.RecoilControlMultiplier:
                derivedStats.recoilControlMultiplier = ApplyMultiplier(derivedStats.recoilControlMultiplier, modifier.operation, modifier.value);
                break;
            case CharacterStatType.InteractionRangeMultiplier:
                derivedStats.interactionRangeMultiplier = ApplyMultiplier(derivedStats.interactionRangeMultiplier, modifier.operation, modifier.value);
                break;
            case CharacterStatType.CritChance:
                derivedStats.critChance = ApplyFloat(derivedStats.critChance, modifier.operation, modifier.value);
                break;
            case CharacterStatType.CritDamageMultiplier:
                derivedStats.critDamageMultiplier = ApplyMultiplier(derivedStats.critDamageMultiplier, modifier.operation, modifier.value);
                break;
            case CharacterStatType.ArmorPenetrationBonus:
                derivedStats.armorPenetrationBonus = ApplyFloat(derivedStats.armorPenetrationBonus, modifier.operation, modifier.value);
                break;
            case CharacterStatType.SpreadMultiplier:
                derivedStats.spreadMultiplier = ApplyMultiplier(derivedStats.spreadMultiplier, modifier.operation, modifier.value);
                break;
            case CharacterStatType.EffectiveRangeMultiplier:
                derivedStats.effectiveRangeMultiplier = ApplyMultiplier(derivedStats.effectiveRangeMultiplier, modifier.operation, modifier.value);
                break;
        }
    }

    private static float ApplyFloat(float currentValue, ModifierOperation operation, float modifierValue)
    {
        return operation == ModifierOperation.Multiply
            ? currentValue * Mathf.Max(0f, modifierValue)
            : currentValue + modifierValue;
    }

    private static float ApplyMultiplier(float currentValue, ModifierOperation operation, float modifierValue)
    {
        return operation == ModifierOperation.Multiply
            ? currentValue * Mathf.Max(0.1f, modifierValue)
            : currentValue + modifierValue;
    }
}
