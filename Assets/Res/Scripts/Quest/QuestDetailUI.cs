using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuestDetailUI
{
    private readonly Font font;

    public QuestDetailUI(Font runtimeFont)
    {
        font = runtimeFont != null ? runtimeFont : PrototypeUiToolkit.ResolveDefaultFont();
    }

    public void Rebuild(RectTransform parent, QuestManager manager, Quest quest, Action onRefreshRequested)
    {
        if (parent == null)
        {
            return;
        }

        ClearChildren(parent);
        if (quest == null || manager == null)
        {
            PrototypeUiToolkit.CreateText(parent, font, "选择一项任务查看详情。", 15, FontStyle.Normal, new Color(0.84f, 0.88f, 0.93f, 1f), TextAnchor.UpperLeft);
            return;
        }

        PrototypeUiToolkit.CreateText(parent, font, quest.QuestName, 22, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        PrototypeUiToolkit.CreateText(parent, font, manager.BuildQuestStatusLabel(quest), 14, FontStyle.Bold, new Color(0.94f, 0.79f, 0.36f, 1f), TextAnchor.UpperLeft);

        if (!string.IsNullOrWhiteSpace(quest.Description))
        {
            PrototypeUiToolkit.CreateText(parent, font, quest.Description, 15, FontStyle.Normal, new Color(0.84f, 0.88f, 0.93f, 1f), TextAnchor.UpperLeft);
        }

        for (int index = 0; index < quest.Objectives.Count; index++)
        {
            PrototypeUiToolkit.CreateText(parent, font, manager.BuildObjectiveLine(quest, quest.Objectives[index], index), 15, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
        }

        string rewardSummary = manager.BuildRewardSummary(quest);
        if (!string.IsNullOrWhiteSpace(rewardSummary))
        {
            PrototypeUiToolkit.CreateText(parent, font, $"奖励：{rewardSummary}", 14, FontStyle.Normal, new Color(0.73f, 0.9f, 0.75f, 1f), TextAnchor.UpperLeft);
        }

        QuestRuntimeState questState = manager.GetQuestState(quest.QuestId);
        RectTransform actionRow = PrototypeUiToolkit.CreateRectTransform("Actions", parent);
        HorizontalLayoutGroup layout = actionRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (manager.CanClaimQuest(quest.QuestId))
        {
            PrototypeUiToolkit.CreateButton(
                actionRow,
                font,
                "提交任务",
                () =>
                {
                    manager.TryClaimQuest(quest.QuestId);
                    onRefreshRequested?.Invoke();
                },
                new Color(0.18f, 0.46f, 0.31f, 0.98f),
                new Color(0.24f, 0.58f, 0.39f, 1f),
                new Color(0.14f, 0.34f, 0.24f, 1f),
                38f);
        }
        else if (manager.CanStartQuest(quest.QuestId))
        {
            PrototypeUiToolkit.CreateButton(
                actionRow,
                font,
                "接取任务",
                () =>
                {
                    manager.StartQuest(quest.QuestId, quest.GiverNpcId);
                    onRefreshRequested?.Invoke();
                },
                new Color(0.21f, 0.38f, 0.59f, 0.98f),
                new Color(0.29f, 0.5f, 0.74f, 1f),
                new Color(0.16f, 0.29f, 0.46f, 1f),
                38f);
        }
        else if (questState != null
            && questState.status == QuestStatus.Completed
            && !questState.rewardsClaimed
            && !string.IsNullOrWhiteSpace(quest.TurnInNpcId))
        {
            UnityEngine.Object.Destroy(actionRow.gameObject);
            PrototypeUiToolkit.CreateText(
                parent,
                font,
                $"请前往 {manager.GetNpcDisplayName(quest.TurnInNpcId)} 提交任务。",
                14,
                FontStyle.Italic,
                new Color(0.94f, 0.79f, 0.36f, 1f),
                TextAnchor.UpperLeft);
        }
        else if (questState != null && !questState.rewardsClaimed)
        {
            PrototypeUiToolkit.CreateButton(
                actionRow,
                font,
                manager.IsQuestTracked(quest.QuestId) ? "取消追踪" : "追踪任务",
                () =>
                {
                    manager.ToggleTracked(quest.QuestId);
                    onRefreshRequested?.Invoke();
                },
                new Color(0.32f, 0.28f, 0.16f, 0.98f),
                new Color(0.46f, 0.39f, 0.22f, 1f),
                new Color(0.24f, 0.2f, 0.12f, 1f),
                38f);
        }
    }

    private static void ClearChildren(RectTransform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            UnityEngine.Object.Destroy(parent.GetChild(index).gameObject);
        }
    }
}
