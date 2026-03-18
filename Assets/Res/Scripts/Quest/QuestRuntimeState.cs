using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class QuestRuntimeState
{
    public string questId = string.Empty;
    public QuestStatus status = QuestStatus.NotStarted;
    public bool rewardsClaimed;
    public bool tracked = true;
    public List<int> objectiveProgress = new List<int>();

    public string QuestId => string.IsNullOrWhiteSpace(questId) ? string.Empty : questId.Trim();
    public bool IsTurnedIn => status == QuestStatus.Completed && rewardsClaimed;

    public void Sanitize(string fallbackQuestId, int objectiveCount)
    {
        questId = string.IsNullOrWhiteSpace(questId)
            ? (fallbackQuestId ?? string.Empty).Trim()
            : questId.Trim();
        tracked = tracked || status == QuestStatus.InProgress;
        objectiveProgress ??= new List<int>();

        if (objectiveCount >= 0)
        {
            while (objectiveProgress.Count < objectiveCount)
            {
                objectiveProgress.Add(0);
            }

            if (objectiveProgress.Count > objectiveCount)
            {
                objectiveProgress.RemoveRange(objectiveCount, objectiveProgress.Count - objectiveCount);
            }
        }

        for (int index = 0; index < objectiveProgress.Count; index++)
        {
            objectiveProgress[index] = Mathf.Max(0, objectiveProgress[index]);
        }

        if (status < QuestStatus.NotStarted || status > QuestStatus.Failed)
        {
            status = QuestStatus.NotStarted;
        }
    }

    public int GetProgress(int objectiveIndex)
    {
        if (objectiveIndex < 0 || objectiveIndex >= objectiveProgress.Count)
        {
            return 0;
        }

        return Mathf.Max(0, objectiveProgress[objectiveIndex]);
    }

    public bool SetProgress(int objectiveIndex, int newValue)
    {
        if (objectiveIndex < 0)
        {
            return false;
        }

        while (objectiveProgress.Count <= objectiveIndex)
        {
            objectiveProgress.Add(0);
        }

        int sanitizedValue = Mathf.Max(0, newValue);
        if (objectiveProgress[objectiveIndex] == sanitizedValue)
        {
            return false;
        }

        objectiveProgress[objectiveIndex] = sanitizedValue;
        return true;
    }

    public void ResetProgress(int objectiveCount)
    {
        objectiveProgress ??= new List<int>();
        objectiveProgress.Clear();

        for (int index = 0; index < Mathf.Max(0, objectiveCount); index++)
        {
            objectiveProgress.Add(0);
        }
    }
}
