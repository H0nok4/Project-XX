using System.Collections.Generic;
using UnityEngine;

public static class PlayerSkillTreeCatalog
{
    private static readonly List<SkillNodeDefinition> Definitions = new List<SkillNodeDefinition>
    {
        Create(
            "combat_weapon_mastery",
            "武器精通",
            "熟悉持续作战节奏，提升基础伤害与控枪能力。",
            SkillBranch.Combat,
            1,
            null,
            new ModifierDefinition
            {
                statType = CharacterStatType.DamageMultiplier,
                operation = ModifierOperation.Add,
                value = 0.08f,
                description = "伤害 +8%"
            },
            new ModifierDefinition
            {
                statType = CharacterStatType.RecoilControlMultiplier,
                operation = ModifierOperation.Add,
                value = 0.06f,
                description = "后坐控制 +6%"
            }),
        Create(
            "combat_close_quarters",
            "近距压制",
            "近距离突击训练会显著加快开火节奏与冲锋机动。",
            SkillBranch.Combat,
            3,
            new[] { "combat_weapon_mastery" },
            new ModifierDefinition
            {
                statType = CharacterStatType.FireRateMultiplier,
                operation = ModifierOperation.Add,
                value = 0.08f,
                description = "射速 +8%"
            },
            new ModifierDefinition
            {
                statType = CharacterStatType.MoveSpeedMultiplier,
                operation = ModifierOperation.Add,
                value = 0.04f,
                description = "移速 +4%"
            }),
        Create(
            "survival_field_medic",
            "战地医疗",
            "在战场上更高效地处理伤势，提升血量上限与治疗收益。",
            SkillBranch.Survival,
            1,
            null,
            new ModifierDefinition
            {
                statType = CharacterStatType.MaxHealthBonus,
                operation = ModifierOperation.Add,
                value = 12f,
                description = "生命 +12"
            },
            new ModifierDefinition
            {
                statType = CharacterStatType.HealingMultiplier,
                operation = ModifierOperation.Add,
                value = 0.12f,
                description = "治疗效果 +12%"
            }),
        Create(
            "survival_tireless",
            "耐力储备",
            "延长持续行动能力，让体力条更耐用。",
            SkillBranch.Survival,
            3,
            new[] { "survival_field_medic" },
            new ModifierDefinition
            {
                statType = CharacterStatType.MaxStaminaBonus,
                operation = ModifierOperation.Add,
                value = 18f,
                description = "体力 +18"
            },
            new ModifierDefinition
            {
                statType = CharacterStatType.MoveSpeedMultiplier,
                operation = ModifierOperation.Add,
                value = 0.05f,
                description = "移速 +5%"
            }),
        Create(
            "engineering_quick_hands",
            "快手改装",
            "让装填和临时调整动作更流畅，适合高频交战构筑。",
            SkillBranch.Engineering,
            1,
            null,
            new ModifierDefinition
            {
                statType = CharacterStatType.ReloadSpeedMultiplier,
                operation = ModifierOperation.Add,
                value = 0.12f,
                description = "装填速度 +12%"
            },
            new ModifierDefinition
            {
                statType = CharacterStatType.CarryWeight,
                operation = ModifierOperation.Add,
                value = 3f,
                description = "负重 +3"
            }),
        Create(
            "engineering_rig_optimizer",
            "挂载优化",
            "优化携行装备与战术挂载，强化整理效率与任务交互范围。",
            SkillBranch.Engineering,
            3,
            new[] { "engineering_quick_hands" },
            new ModifierDefinition
            {
                statType = CharacterStatType.ReloadSpeedMultiplier,
                operation = ModifierOperation.Add,
                value = 0.08f,
                description = "装填速度 +8%"
            },
            new ModifierDefinition
            {
                statType = CharacterStatType.InteractionRangeMultiplier,
                operation = ModifierOperation.Add,
                value = 0.2f,
                description = "交互范围 +20%"
            })
    };

    private static readonly Dictionary<string, SkillNodeDefinition> DefinitionsById = BuildDefinitionMap();

    public static IReadOnlyList<SkillNodeDefinition> GetDefinitions()
    {
        return Definitions;
    }

    public static SkillNodeDefinition GetDefinition(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return null;
        }

        DefinitionsById.TryGetValue(nodeId.Trim(), out SkillNodeDefinition definition);
        return definition;
    }

    public static string GetDisplayName(string nodeId)
    {
        SkillNodeDefinition definition = GetDefinition(nodeId);
        return definition != null ? definition.displayName : string.Empty;
    }

    public static bool CanUnlock(PlayerProgressionData progression, string nodeId, out string reason)
    {
        reason = string.Empty;
        if (progression == null)
        {
            reason = "成长数据缺失";
            return false;
        }

        PrototypePlayerProgressionUtility.Sanitize(progression);
        SkillNodeDefinition definition = GetDefinition(nodeId);
        if (definition == null)
        {
            reason = "节点不存在";
            return false;
        }

        if (progression.skillTree != null && progression.skillTree.IsUnlocked(definition.nodeId))
        {
            reason = "已解锁";
            return false;
        }

        if (progression.unspentSkillPoints <= 0)
        {
            reason = "技能点不足";
            return false;
        }

        if (progression.playerLevel < Mathf.Max(1, definition.requiredPlayerLevel))
        {
            reason = $"需要 Lv {Mathf.Max(1, definition.requiredPlayerLevel)}";
            return false;
        }

        if (definition.prerequisiteNodeIds != null)
        {
            for (int index = 0; index < definition.prerequisiteNodeIds.Count; index++)
            {
                string prerequisiteNodeId = definition.prerequisiteNodeIds[index];
                if (string.IsNullOrWhiteSpace(prerequisiteNodeId))
                {
                    continue;
                }

                if (progression.skillTree == null || !progression.skillTree.IsUnlocked(prerequisiteNodeId))
                {
                    reason = $"需要前置：{GetDisplayName(prerequisiteNodeId)}";
                    return false;
                }
            }
        }

        return true;
    }

    public static int CountUnlockedInBranch(PlayerProgressionData progression, SkillBranch branch)
    {
        if (progression == null || progression.skillTree == null || progression.skillTree.unlockedNodeIds == null)
        {
            return 0;
        }

        int count = 0;
        for (int index = 0; index < progression.skillTree.unlockedNodeIds.Count; index++)
        {
            SkillNodeDefinition definition = GetDefinition(progression.skillTree.unlockedNodeIds[index]);
            if (definition != null && definition.branch == branch)
            {
                count++;
            }
        }

        return count;
    }

    public static string GetBranchDisplayName(SkillBranch branch)
    {
        switch (branch)
        {
            case SkillBranch.Combat:
                return "战斗";
            case SkillBranch.Survival:
                return "生存";
            case SkillBranch.Engineering:
                return "工程";
            default:
                return "专精";
        }
    }

    public static string BuildModifierSummary(SkillNodeDefinition definition)
    {
        if (definition == null || definition.modifiers == null || definition.modifiers.Count == 0)
        {
            return "无附加效果";
        }

        var parts = new List<string>();
        for (int index = 0; index < definition.modifiers.Count; index++)
        {
            ModifierDefinition modifier = definition.modifiers[index];
            if (modifier == null)
            {
                continue;
            }

            string formatted = !string.IsNullOrWhiteSpace(modifier.description)
                ? modifier.description
                : PrototypePlayerProgressionUtility.FormatModifier(modifier.statType, modifier.operation, modifier.value);
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                parts.Add(formatted);
            }
        }

        return parts.Count > 0 ? string.Join("  ", parts) : "无附加效果";
    }

    private static Dictionary<string, SkillNodeDefinition> BuildDefinitionMap()
    {
        var definitionsById = new Dictionary<string, SkillNodeDefinition>(System.StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < Definitions.Count; index++)
        {
            SkillNodeDefinition definition = Definitions[index];
            if (definition == null || string.IsNullOrWhiteSpace(definition.nodeId))
            {
                continue;
            }

            string sanitizedNodeId = definition.nodeId.Trim();
            definition.nodeId = sanitizedNodeId;
            definition.displayName = string.IsNullOrWhiteSpace(definition.displayName) ? sanitizedNodeId : definition.displayName.Trim();
            definition.description = string.IsNullOrWhiteSpace(definition.description) ? definition.displayName : definition.description.Trim();
            definition.requiredPlayerLevel = Mathf.Max(1, definition.requiredPlayerLevel);
            definition.prerequisiteNodeIds ??= new List<string>();
            definition.modifiers ??= new List<ModifierDefinition>();
            definitionsById[sanitizedNodeId] = definition;
        }

        return definitionsById;
    }

    private static SkillNodeDefinition Create(
        string nodeId,
        string displayName,
        string description,
        SkillBranch branch,
        int requiredPlayerLevel,
        IEnumerable<string> prerequisiteNodeIds,
        params ModifierDefinition[] modifiers)
    {
        return new SkillNodeDefinition
        {
            nodeId = nodeId,
            displayName = displayName,
            description = description,
            branch = branch,
            requiredPlayerLevel = requiredPlayerLevel,
            prerequisiteNodeIds = prerequisiteNodeIds != null ? new List<string>(prerequisiteNodeIds) : new List<string>(),
            modifiers = modifiers != null ? new List<ModifierDefinition>(modifiers) : new List<ModifierDefinition>()
        };
    }
}
