using UnityEngine;

public static class RespecService
{
    public const int AttributeRespecCost = 500;
    public const int SkillRespecCost = 800;

    public static bool CanResetAttributes(PlayerProgressionData progression, out string reason)
    {
        reason = string.Empty;
        if (progression == null)
        {
            reason = "成长数据缺失。";
            return false;
        }

        PrototypePlayerProgressionUtility.Sanitize(progression);
        if (progression.attributeSet == null || progression.attributeSet.GetAllocatedPoints() <= 0)
        {
            reason = "当前没有已分配的属性点。";
            return false;
        }

        return true;
    }

    public static bool CanResetSkills(PlayerProgressionData progression, out string reason)
    {
        reason = string.Empty;
        if (progression == null)
        {
            reason = "成长数据缺失。";
            return false;
        }

        PrototypePlayerProgressionUtility.Sanitize(progression);
        if (progression.skillTree == null || progression.skillTree.GetUnlockedCount() <= 0)
        {
            reason = "当前没有已解锁的专精节点。";
            return false;
        }

        return true;
    }

    public static int ResetAttributes(PlayerProgressionData progression)
    {
        if (!CanResetAttributes(progression, out _))
        {
            return 0;
        }

        int refundedPoints = progression.attributeSet.GetAllocatedPoints();
        progression.attributeSet = new PlayerAttributeSet();
        progression.unspentAttributePoints += refundedPoints;
        PrototypePlayerProgressionUtility.Sanitize(progression);
        return refundedPoints;
    }

    public static int ResetSkills(PlayerProgressionData progression)
    {
        if (!CanResetSkills(progression, out _))
        {
            return 0;
        }

        int refundedPoints = progression.skillTree.GetUnlockedCount();
        progression.skillTree = new PlayerSkillTree();
        progression.unspentSkillPoints += refundedPoints;
        PrototypePlayerProgressionUtility.Sanitize(progression);
        return refundedPoints;
    }

    public static string BuildRespecSummaryText()
    {
        return $"属性重置：{AttributeRespecCost} 现金，返还全部已分配属性点。\n"
            + $"技能重置：{SkillRespecCost} 现金，返还全部已消耗技能点。\n"
            + "重置不会影响等级、经验和已获得的成长总点数。";
    }
}
