using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuestListUI
{
    private readonly Font font;

    public QuestListUI(Font runtimeFont)
    {
        font = runtimeFont != null ? runtimeFont : PrototypeUiToolkit.ResolveDefaultFont();
    }

    public void Rebuild(RectTransform parent, IReadOnlyList<Quest> quests, Quest selectedQuest, QuestManager manager, Action<Quest> onSelected)
    {
        if (parent == null)
        {
            return;
        }

        ClearChildren(parent);
        if (quests == null || quests.Count == 0)
        {
            PrototypeUiToolkit.CreateText(parent, font, "暂无任务。", 15, FontStyle.Normal, new Color(0.82f, 0.87f, 0.92f, 1f), TextAnchor.UpperLeft);
            return;
        }

        for (int index = 0; index < quests.Count; index++)
        {
            Quest quest = quests[index];
            if (quest == null)
            {
                continue;
            }

            string status = manager != null ? manager.BuildQuestStatusLabel(quest) : string.Empty;
            Button button = PrototypeUiToolkit.CreateButton(
                parent,
                font,
                string.IsNullOrWhiteSpace(status) ? quest.QuestName : $"{quest.QuestName}\n{status}",
                () => onSelected?.Invoke(quest),
                new Color(0.16f, 0.2f, 0.27f, 0.96f),
                new Color(0.24f, 0.32f, 0.42f, 1f),
                new Color(0.12f, 0.16f, 0.22f, 1f),
                52f);
            if (button == null)
            {
                continue;
            }

            bool isSelected = selectedQuest != null && string.Equals(selectedQuest.QuestId, quest.QuestId, StringComparison.OrdinalIgnoreCase);
            Image background = button.GetComponent<Image>();
            if (background != null)
            {
                background.color = isSelected ? new Color(0.26f, 0.41f, 0.54f, 1f) : new Color(0.16f, 0.2f, 0.27f, 0.96f);
            }
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
