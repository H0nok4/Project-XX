using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Quest
{
    public string questId = string.Empty;
    public string questName = "任务";
    [TextArea(2, 6)]
    public string description = string.Empty;
    public QuestType type = QuestType.Side;
    public string giverNpcId = string.Empty;
    public string turnInNpcId = string.Empty;
    public bool autoTrack = true;
    public List<string> prerequisiteQuests = new List<string>();
    public List<string> requiredStoryFlags = new List<string>();
    [SerializeReference] public List<QuestObjective> objectives = new List<QuestObjective>();
    public QuestReward reward = new QuestReward();

    public string QuestId => string.IsNullOrWhiteSpace(questId) ? string.Empty : questId.Trim();
    public string QuestName => string.IsNullOrWhiteSpace(questName) ? QuestId : questName.Trim();
    public string Description => string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
    public string GiverNpcId => string.IsNullOrWhiteSpace(giverNpcId) ? string.Empty : giverNpcId.Trim();
    public string TurnInNpcId => string.IsNullOrWhiteSpace(turnInNpcId) ? GiverNpcId : turnInNpcId.Trim();
    public virtual bool IsRepeatable => type == QuestType.Daily;
    public IReadOnlyList<QuestObjective> Objectives => objectives;

    public virtual void Sanitize()
    {
        questId = QuestId;
        questName = QuestName;
        description = Description;
        giverNpcId = GiverNpcId;
        turnInNpcId = TurnInNpcId;
        objectives ??= new List<QuestObjective>();
        reward ??= new QuestReward();
        reward.Sanitize();
        prerequisiteQuests ??= new List<string>();
        requiredStoryFlags ??= new List<string>();

        SanitizeStringList(prerequisiteQuests);
        SanitizeStringList(requiredStoryFlags);

        for (int index = objectives.Count - 1; index >= 0; index--)
        {
            if (objectives[index] == null)
            {
                objectives.RemoveAt(index);
            }
        }
    }

    public bool CanStart(Func<string, bool> isQuestTurnedIn, Func<string, bool> hasStoryFlag)
    {
        if (string.IsNullOrWhiteSpace(QuestId))
        {
            return false;
        }

        if (prerequisiteQuests != null)
        {
            for (int index = 0; index < prerequisiteQuests.Count; index++)
            {
                string prerequisiteQuestId = prerequisiteQuests[index];
                if (!string.IsNullOrWhiteSpace(prerequisiteQuestId)
                    && (isQuestTurnedIn == null || !isQuestTurnedIn(prerequisiteQuestId.Trim())))
                {
                    return false;
                }
            }
        }

        if (requiredStoryFlags != null)
        {
            for (int index = 0; index < requiredStoryFlags.Count; index++)
            {
                string storyFlag = requiredStoryFlags[index];
                if (!string.IsNullOrWhiteSpace(storyFlag)
                    && (hasStoryFlag == null || !hasStoryFlag(storyFlag.Trim())))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool IsGivenByNpc(string npcId)
    {
        return !string.IsNullOrWhiteSpace(npcId)
            && string.Equals(GiverNpcId, npcId.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public bool IsTurnInTarget(string npcId)
    {
        return !string.IsNullOrWhiteSpace(npcId)
            && string.Equals(TurnInNpcId, npcId.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public virtual string GrantRewards(QuestManager manager)
    {
        return manager != null ? manager.GrantRewardBundle(reward) : string.Empty;
    }

    private static void SanitizeStringList(List<string> values)
    {
        if (values == null)
        {
            return;
        }

        for (int index = values.Count - 1; index >= 0; index--)
        {
            string sanitized = string.IsNullOrWhiteSpace(values[index]) ? string.Empty : values[index].Trim();
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                values.RemoveAt(index);
                continue;
            }

            values[index] = sanitized;
        }
    }
}
