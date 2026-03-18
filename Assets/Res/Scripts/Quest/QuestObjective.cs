using System;
using UnityEngine;

public enum QuestEventType
{
    Talk = 0,
    Kill = 1,
    Collect = 2,
    Explore = 3,
    Extract = 4,
    Custom = 5
}

public readonly struct QuestEventRecord
{
    public QuestEventRecord(QuestEventType type, string targetId, string secondaryId = "", int amount = 1, bool isBoss = false)
    {
        Type = type;
        TargetId = string.IsNullOrWhiteSpace(targetId) ? string.Empty : targetId.Trim();
        SecondaryId = string.IsNullOrWhiteSpace(secondaryId) ? string.Empty : secondaryId.Trim();
        Amount = Mathf.Max(1, amount);
        IsBoss = isBoss;
    }

    public QuestEventType Type { get; }
    public string TargetId { get; }
    public string SecondaryId { get; }
    public int Amount { get; }
    public bool IsBoss { get; }
}

public static class QuestEventHub
{
    public static event Action<QuestEventRecord> EventRaised;

    public static void RaiseTalk(string speakerId, string displayName = "")
    {
        Raise(new QuestEventRecord(QuestEventType.Talk, speakerId, displayName));
    }

    public static void RaiseKill(string enemyId, string enemyType, bool isBoss, int amount = 1)
    {
        Raise(new QuestEventRecord(QuestEventType.Kill, enemyId, enemyType, amount, isBoss));
    }

    public static void RaiseCollect(string itemId, int amount = 1, string source = "")
    {
        Raise(new QuestEventRecord(QuestEventType.Collect, itemId, source, amount));
    }

    public static void RaiseExplore(string locationId)
    {
        Raise(new QuestEventRecord(QuestEventType.Explore, locationId));
    }

    public static void RaiseExtract(string extractionId)
    {
        Raise(new QuestEventRecord(QuestEventType.Extract, extractionId));
    }

    public static void RaiseCustom(string eventId, int amount = 1, string secondaryId = "")
    {
        Raise(new QuestEventRecord(QuestEventType.Custom, eventId, secondaryId, amount));
    }

    private static void Raise(QuestEventRecord record)
    {
        EventRaised?.Invoke(record);
    }
}

[Serializable]
public abstract class QuestObjective
{
    [TextArea(1, 3)]
    public string description = string.Empty;
    [Min(1)]
    public int requiredProgress = 1;

    public string Description => string.IsNullOrWhiteSpace(description) ? "未命名目标" : description.Trim();
    public int RequiredProgress => Mathf.Max(1, requiredProgress);

    public virtual int GetCurrentProgress(QuestManager manager, QuestRuntimeState runtimeState, int objectiveIndex)
    {
        return runtimeState != null
            ? Mathf.Clamp(runtimeState.GetProgress(objectiveIndex), 0, RequiredProgress)
            : 0;
    }

    public bool IsCompleted(QuestManager manager, QuestRuntimeState runtimeState, int objectiveIndex)
    {
        return GetCurrentProgress(manager, runtimeState, objectiveIndex) >= RequiredProgress;
    }

    public virtual bool TryApplyEvent(QuestManager manager, QuestRuntimeState runtimeState, int objectiveIndex, QuestEventRecord record, out int newProgress)
    {
        newProgress = GetCurrentProgress(manager, runtimeState, objectiveIndex);
        return false;
    }

    public virtual bool CanClaim(QuestManager manager)
    {
        return true;
    }

    public virtual bool ConsumeClaimRequirements(QuestManager manager)
    {
        return true;
    }

    protected bool TrySetProgress(QuestRuntimeState runtimeState, int objectiveIndex, int newValue, out int appliedProgress)
    {
        appliedProgress = Mathf.Max(0, newValue);
        if (runtimeState == null)
        {
            return false;
        }

        return runtimeState.SetProgress(objectiveIndex, appliedProgress);
    }

    protected static bool MatchesId(string left, string right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

[Serializable]
public abstract class EventQuestObjective : QuestObjective
{
    [SerializeField] private QuestEventType eventType = QuestEventType.Custom;

    protected EventQuestObjective(QuestEventType type)
    {
        eventType = type;
    }

    protected abstract bool Matches(QuestEventRecord record);

    protected virtual int ResolveProgressDelta(QuestEventRecord record)
    {
        return Mathf.Max(1, record.Amount);
    }

    public override bool TryApplyEvent(QuestManager manager, QuestRuntimeState runtimeState, int objectiveIndex, QuestEventRecord record, out int newProgress)
    {
        int currentProgress = GetCurrentProgress(manager, runtimeState, objectiveIndex);
        newProgress = currentProgress;

        if (runtimeState == null || record.Type != eventType || !Matches(record))
        {
            return false;
        }

        int targetProgress = Mathf.Min(RequiredProgress, currentProgress + ResolveProgressDelta(record));
        if (!TrySetProgress(runtimeState, objectiveIndex, targetProgress, out newProgress))
        {
            return false;
        }

        return newProgress != currentProgress;
    }
}

[Serializable]
public sealed class TalkObjective : EventQuestObjective
{
    public string speakerId = string.Empty;

    public TalkObjective() : base(QuestEventType.Talk)
    {
    }

    protected override bool Matches(QuestEventRecord record)
    {
        return string.IsNullOrWhiteSpace(speakerId) || MatchesId(record.TargetId, speakerId);
    }
}

[Serializable]
public sealed class KillObjective : EventQuestObjective
{
    public string enemyId = string.Empty;
    public string enemyType = string.Empty;
    public bool requireBoss;

    public KillObjective() : base(QuestEventType.Kill)
    {
    }

    protected override bool Matches(QuestEventRecord record)
    {
        if (requireBoss && !record.IsBoss)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(enemyId) && !MatchesId(record.TargetId, enemyId))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(enemyType) && !MatchesId(record.SecondaryId, enemyType))
        {
            return false;
        }

        return true;
    }
}

[Serializable]
public sealed class CollectObjective : EventQuestObjective
{
    public string itemId = string.Empty;

    public CollectObjective() : base(QuestEventType.Collect)
    {
    }

    protected override bool Matches(QuestEventRecord record)
    {
        return string.IsNullOrWhiteSpace(itemId) || MatchesId(record.TargetId, itemId);
    }
}

[Serializable]
public sealed class ExploreObjective : EventQuestObjective
{
    public string locationId = string.Empty;

    public ExploreObjective() : base(QuestEventType.Explore)
    {
    }

    protected override bool Matches(QuestEventRecord record)
    {
        return string.IsNullOrWhiteSpace(locationId) || MatchesId(record.TargetId, locationId);
    }
}

[Serializable]
public sealed class ExtractObjective : EventQuestObjective
{
    public string extractionId = string.Empty;

    public ExtractObjective() : base(QuestEventType.Extract)
    {
    }

    protected override bool Matches(QuestEventRecord record)
    {
        return string.IsNullOrWhiteSpace(extractionId) || MatchesId(record.TargetId, extractionId);
    }
}

[Serializable]
public sealed class CustomEventObjective : EventQuestObjective
{
    public string eventId = string.Empty;

    public CustomEventObjective() : base(QuestEventType.Custom)
    {
    }

    protected override bool Matches(QuestEventRecord record)
    {
        return string.IsNullOrWhiteSpace(eventId) || MatchesId(record.TargetId, eventId);
    }
}

[Serializable]
public sealed class DeliverObjective : QuestObjective
{
    public string itemId = string.Empty;

    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();

    public override int GetCurrentProgress(QuestManager manager, QuestRuntimeState runtimeState, int objectiveIndex)
    {
        return manager != null
            ? Mathf.Clamp(manager.GetStorageItemCount(ItemId), 0, RequiredProgress)
            : 0;
    }

    public override bool CanClaim(QuestManager manager)
    {
        return manager != null && manager.GetStorageItemCount(ItemId) >= RequiredProgress;
    }

    public override bool ConsumeClaimRequirements(QuestManager manager)
    {
        return manager != null && manager.TryConsumeStorageItem(ItemId, RequiredProgress);
    }
}
