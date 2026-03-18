using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class PrototypePlayerProgressionUtility
{
    private const int BaseExperienceToNextLevel = 100;
    private const int ExperienceLinearStep = 50;
    private const int ExperienceQuadraticStep = 15;

    public const int DefaultAttributeValue = 5;
    public const int MaxAttributeValue = 20;
    public const int AttributePointsPerLevel = 3;
    public const int SkillPointsPerLevel = 1;

    public const float BaseCarryWeight = 18f;
    public const float LevelHealthBonusPerLevel = 4f;
    public const float LevelStaminaBonusPerLevel = 2f;
    public const float LevelDamageBonusPerLevel = 0.01f;
    public const float LevelHealingBonusPerLevel = 0.005f;

    public const float StrengthCarryWeightPerPoint = 2f;
    public const float StrengthDamageBonusPerPoint = 0.015f;
    public const float StrengthRecoilControlPerPoint = 0.02f;

    public const float EnduranceHealthBonusPerPoint = 8f;
    public const float EnduranceStaminaBonusPerPoint = 6f;
    public const float EnduranceHealingBonusPerPoint = 0.01f;

    public const float AgilityMoveSpeedPerPoint = 0.015f;
    public const float AgilityReloadSpeedPerPoint = 0.02f;
    public const float AgilityFireRatePerPoint = 0.01f;

    public const float PerceptionDamageBonusPerPoint = 0.01f;
    public const float PerceptionInteractionRangePerPoint = 0.05f;

    public const float TechHealingBonusPerPoint = 0.02f;
    public const float TechReloadSpeedPerPoint = 0.015f;
    public const float TechCarryWeightPerPoint = 0.5f;

    public static void Sanitize(PlayerProgressionData progression)
    {
        if (progression == null)
        {
            return;
        }

        int previousVersion = progression.progressionDataVersion;
        progression.playerLevel = Mathf.Max(1, progression.playerLevel);
        progression.currentExperience = Mathf.Max(0, progression.currentExperience);
        progression.lifetimeExperience = Mathf.Max(progression.currentExperience, progression.lifetimeExperience);
        progression.killCount = Mathf.Max(0, progression.killCount);
        progression.attributeSet ??= new PlayerAttributeSet();
        progression.skillTree ??= new PlayerSkillTree();
        progression.attributeSet.Sanitize();
        progression.skillTree.Sanitize();

        int spentAttributePoints = progression.attributeSet.GetAllocatedPoints();
        int spentSkillPoints = progression.skillTree.GetUnlockedCount();
        int grantedAttributePoints = GetGrantedAttributePoints(progression.playerLevel);
        int grantedSkillPoints = GetGrantedSkillPoints(progression.playerLevel);
        int maxUnspentAttributePoints = Mathf.Max(0, grantedAttributePoints - spentAttributePoints);
        int maxUnspentSkillPoints = Mathf.Max(0, grantedSkillPoints - spentSkillPoints);

        if (previousVersion < ProfileSchemaVersion.CurrentProgressionDataVersion)
        {
            progression.unspentAttributePoints = maxUnspentAttributePoints;
            progression.unspentSkillPoints = maxUnspentSkillPoints;
        }
        else
        {
            progression.unspentAttributePoints = Mathf.Clamp(progression.unspentAttributePoints, 0, maxUnspentAttributePoints);
            progression.unspentSkillPoints = Mathf.Clamp(progression.unspentSkillPoints, 0, maxUnspentSkillPoints);
        }

        int experienceToNext = GetExperienceToNextLevel(progression.playerLevel);
        if (experienceToNext > 0)
        {
            progression.currentExperience = Mathf.Clamp(progression.currentExperience, 0, experienceToNext - 1);
        }

        progression.progressionDataVersion = ProfileSchemaVersion.CurrentProgressionDataVersion;
    }

    public static void Copy(PlayerProgressionData source, PlayerProgressionData target)
    {
        if (target == null)
        {
            return;
        }

        if (source == null)
        {
            target.progressionDataVersion = ProfileSchemaVersion.CurrentProgressionDataVersion;
            target.playerLevel = 1;
            target.currentExperience = 0;
            target.lifetimeExperience = 0;
            target.killCount = 0;
            target.unspentAttributePoints = 0;
            target.unspentSkillPoints = 0;
            target.attributeSet = new PlayerAttributeSet();
            target.skillTree = new PlayerSkillTree();
            Sanitize(target);
            return;
        }

        target.progressionDataVersion = source.progressionDataVersion;
        target.playerLevel = source.playerLevel;
        target.currentExperience = source.currentExperience;
        target.lifetimeExperience = source.lifetimeExperience;
        target.killCount = source.killCount;
        target.unspentAttributePoints = source.unspentAttributePoints;
        target.unspentSkillPoints = source.unspentSkillPoints;
        target.attributeSet ??= new PlayerAttributeSet();
        target.skillTree ??= new PlayerSkillTree();

        PlayerAttributeSet sourceAttributes = source.attributeSet ?? new PlayerAttributeSet();
        target.attributeSet.strength = sourceAttributes.strength;
        target.attributeSet.endurance = sourceAttributes.endurance;
        target.attributeSet.agility = sourceAttributes.agility;
        target.attributeSet.perception = sourceAttributes.perception;
        target.attributeSet.tech = sourceAttributes.tech;

        target.skillTree.unlockedNodeIds = source.skillTree != null && source.skillTree.unlockedNodeIds != null
            ? new List<string>(source.skillTree.unlockedNodeIds)
            : new List<string>();

        Sanitize(target);
    }

    public static int GetExperienceToNextLevel(int playerLevel)
    {
        int normalizedLevel = Mathf.Max(1, playerLevel);
        int levelIndex = normalizedLevel - 1;
        return BaseExperienceToNextLevel
            + levelIndex * ExperienceLinearStep
            + levelIndex * levelIndex * ExperienceQuadraticStep;
    }

    public static int GetGrantedAttributePoints(int playerLevel)
    {
        return Mathf.Max(0, playerLevel - 1) * AttributePointsPerLevel;
    }

    public static int GetGrantedSkillPoints(int playerLevel)
    {
        return Mathf.Max(0, playerLevel - 1) * SkillPointsPerLevel;
    }

    public static bool TryAllocateAttributePoint(PlayerProgressionData progression, PlayerAttributeType type, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (progression == null)
        {
            errorMessage = "成长数据缺失。";
            return false;
        }

        Sanitize(progression);
        if (progression.unspentAttributePoints <= 0)
        {
            errorMessage = "没有可分配的属性点。";
            return false;
        }

        int currentValue = progression.attributeSet.GetValue(type);
        if (currentValue >= MaxAttributeValue)
        {
            errorMessage = $"{GetAttributeDisplayName(type)}已达到当前上限。";
            return false;
        }

        progression.attributeSet.SetValue(type, currentValue + 1);
        progression.unspentAttributePoints--;
        Sanitize(progression);
        return true;
    }

    public static bool TryUnlockSkillNode(PlayerProgressionData progression, string nodeId, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (progression == null)
        {
            errorMessage = "成长数据缺失。";
            return false;
        }

        if (!PlayerSkillTreeCatalog.CanUnlock(progression, nodeId, out errorMessage))
        {
            return false;
        }

        progression.skillTree ??= new PlayerSkillTree();
        progression.skillTree.unlockedNodeIds ??= new List<string>();
        progression.skillTree.unlockedNodeIds.Add(nodeId.Trim());
        progression.unspentSkillPoints--;
        Sanitize(progression);
        return true;
    }

    public static string GetAttributeDisplayName(PlayerAttributeType type)
    {
        switch (type)
        {
            case PlayerAttributeType.Strength:
                return "力量";
            case PlayerAttributeType.Endurance:
                return "体质";
            case PlayerAttributeType.Agility:
                return "敏捷";
            case PlayerAttributeType.Perception:
                return "感知";
            case PlayerAttributeType.Tech:
                return "技术";
            default:
                return "属性";
        }
    }

    public static string BuildAttributeDetail(PlayerProgressionData progression, PlayerAttributeType type)
    {
        progression ??= new PlayerProgressionData();
        Sanitize(progression);

        int currentValue = progression.attributeSet.GetValue(type);
        int pointsAboveBase = Mathf.Max(0, currentValue - DefaultAttributeValue);
        switch (type)
        {
            case PlayerAttributeType.Strength:
                return $"当前值 {currentValue}，累计提供负重 +{pointsAboveBase * StrengthCarryWeightPerPoint:0.#}、伤害 +{pointsAboveBase * StrengthDamageBonusPerPoint * 100f:0.#}%、控枪 +{pointsAboveBase * StrengthRecoilControlPerPoint * 100f:0.#}%。";
            case PlayerAttributeType.Endurance:
                return $"当前值 {currentValue}，累计提供生命 +{pointsAboveBase * EnduranceHealthBonusPerPoint:0.#}、体力 +{pointsAboveBase * EnduranceStaminaBonusPerPoint:0.#}、治疗 +{pointsAboveBase * EnduranceHealingBonusPerPoint * 100f:0.#}%。";
            case PlayerAttributeType.Agility:
                return $"当前值 {currentValue}，累计提供移速 +{pointsAboveBase * AgilityMoveSpeedPerPoint * 100f:0.#}%、装填 +{pointsAboveBase * AgilityReloadSpeedPerPoint * 100f:0.#}%、射速 +{pointsAboveBase * AgilityFireRatePerPoint * 100f:0.#}%。";
            case PlayerAttributeType.Perception:
                return $"当前值 {currentValue}，累计提供伤害 +{pointsAboveBase * PerceptionDamageBonusPerPoint * 100f:0.#}%、交互范围 +{pointsAboveBase * PerceptionInteractionRangePerPoint * 100f:0.#}%。";
            case PlayerAttributeType.Tech:
                return $"当前值 {currentValue}，累计提供治疗 +{pointsAboveBase * TechHealingBonusPerPoint * 100f:0.#}%、装填 +{pointsAboveBase * TechReloadSpeedPerPoint * 100f:0.#}%、负重 +{pointsAboveBase * TechCarryWeightPerPoint:0.#}。";
            default:
                return $"当前值 {currentValue}";
        }
    }

    public static string GetCharacterStatDisplayName(CharacterStatType statType)
    {
        switch (statType)
        {
            case CharacterStatType.MaxHealthBonus:
                return "生命上限";
            case CharacterStatType.MaxStaminaBonus:
                return "体力上限";
            case CharacterStatType.CarryWeight:
                return "负重";
            case CharacterStatType.DamageMultiplier:
                return "伤害";
            case CharacterStatType.HealingMultiplier:
                return "治疗";
            case CharacterStatType.ReloadSpeedMultiplier:
                return "装填速度";
            case CharacterStatType.FireRateMultiplier:
                return "射速";
            case CharacterStatType.MoveSpeedMultiplier:
                return "移速";
            case CharacterStatType.RecoilControlMultiplier:
                return "控枪";
            case CharacterStatType.InteractionRangeMultiplier:
                return "交互范围";
            case CharacterStatType.CritChance:
                return "暴击率";
            case CharacterStatType.CritDamageMultiplier:
                return "暴击伤害";
            case CharacterStatType.ArmorPenetrationBonus:
                return "穿甲";
            case CharacterStatType.SpreadMultiplier:
                return "散布";
            case CharacterStatType.EffectiveRangeMultiplier:
                return "有效射程";
            default:
                return "构筑";
        }
    }

    public static string FormatModifier(CharacterStatType statType, ModifierOperation operation, float value)
    {
        string label = GetCharacterStatDisplayName(statType);
        bool isFlatValue = statType == CharacterStatType.MaxHealthBonus
            || statType == CharacterStatType.MaxStaminaBonus
            || statType == CharacterStatType.CarryWeight
            || statType == CharacterStatType.ArmorPenetrationBonus;

        string valueText;
        if (operation == ModifierOperation.Multiply)
        {
            valueText = $"x{Mathf.Max(0f, value):0.00}";
        }
        else if (isFlatValue)
        {
            valueText = $"{(value >= 0f ? "+" : string.Empty)}{value:0.#}";
        }
        else
        {
            valueText = $"{(value >= 0f ? "+" : string.Empty)}{value * 100f:0.#}%";
        }

        return $"{label} {valueText}";
    }

    public static CharacterStatAggregator BuildPreviewAggregator(PlayerProgressionData progression)
    {
        return CharacterStatAggregator.BuildPreview(progression);
    }

    public static CharacterStatAggregator BuildPreviewAggregator(
        PlayerProgressionData progression,
        ItemInstance activeWeapon,
        IReadOnlyList<ItemAffixSummary> armorAffixSummaries)
    {
        return CharacterStatAggregator.BuildPreview(progression, activeWeapon, armorAffixSummaries);
    }

    public static int GetEnemyBaseLevel(PrototypeEnemyArchetype archetype)
    {
        switch (archetype)
        {
            case PrototypeEnemyArchetype.PoliceZombie:
                return 2;
            case PrototypeEnemyArchetype.SoldierZombie:
                return 4;
            case PrototypeEnemyArchetype.ZombieDog:
                return 2;
            default:
                return 1;
        }
    }

    public static float GetEnemyHealthMultiplier(int enemyLevel, bool bossProfile)
    {
        float multiplier = 1f + Mathf.Max(0, enemyLevel - 1) * 0.16f;
        return bossProfile ? multiplier + 0.35f : multiplier;
    }

    public static float GetEnemyDamageMultiplier(int enemyLevel, bool bossProfile)
    {
        float multiplier = 1f + Mathf.Max(0, enemyLevel - 1) * 0.12f;
        return bossProfile ? multiplier + 0.2f : multiplier;
    }

    public static int GetEnemyExperienceReward(int enemyLevel, PrototypeEnemyArchetype archetype, bool bossProfile)
    {
        int archetypeBonus;
        switch (archetype)
        {
            case PrototypeEnemyArchetype.PoliceZombie:
                archetypeBonus = 8;
                break;
            case PrototypeEnemyArchetype.SoldierZombie:
                archetypeBonus = 18;
                break;
            case PrototypeEnemyArchetype.ZombieDog:
                archetypeBonus = 6;
                break;
            default:
                archetypeBonus = 0;
                break;
        }

        int reward = 28 + Mathf.Max(0, enemyLevel - 1) * 14 + archetypeBonus;
        if (bossProfile)
        {
            reward += 50;
        }

        return Mathf.Max(10, reward);
    }

    public static int AddExperience(PlayerProgressionData progression, int amount, out int levelsGained)
    {
        levelsGained = 0;
        if (progression == null || amount <= 0)
        {
            return 0;
        }

        Sanitize(progression);

        progression.currentExperience += amount;
        progression.lifetimeExperience += amount;

        int experienceToNext = GetExperienceToNextLevel(progression.playerLevel);
        while (experienceToNext > 0 && progression.currentExperience >= experienceToNext)
        {
            progression.currentExperience -= experienceToNext;
            progression.playerLevel++;
            levelsGained++;
            progression.unspentAttributePoints += AttributePointsPerLevel;
            progression.unspentSkillPoints += SkillPointsPerLevel;
            experienceToNext = GetExperienceToNextLevel(progression.playerLevel);
        }

        Sanitize(progression);
        return amount;
    }

    public static string BuildUnlockedSkillSummary(PlayerProgressionData progression)
    {
        if (progression == null)
        {
            return "尚未初始化成长数据。";
        }

        Sanitize(progression);
        if (progression.skillTree == null || progression.skillTree.unlockedNodeIds == null || progression.skillTree.unlockedNodeIds.Count == 0)
        {
            return "尚未解锁专精节点。";
        }

        var builder = new StringBuilder(160);
        foreach (SkillBranch branch in new[] { SkillBranch.Combat, SkillBranch.Survival, SkillBranch.Engineering })
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(PlayerSkillTreeCatalog.GetBranchDisplayName(branch));
            builder.Append("：");

            bool wroteAny = false;
            for (int index = 0; index < progression.skillTree.unlockedNodeIds.Count; index++)
            {
                SkillNodeDefinition definition = PlayerSkillTreeCatalog.GetDefinition(progression.skillTree.unlockedNodeIds[index]);
                if (definition == null || definition.branch != branch)
                {
                    continue;
                }

                if (wroteAny)
                {
                    builder.Append(" / ");
                }

                builder.Append(definition.displayName);
                wroteAny = true;
            }

            if (!wroteAny)
            {
                builder.Append("未解锁");
            }
        }

        return builder.ToString();
    }
}
