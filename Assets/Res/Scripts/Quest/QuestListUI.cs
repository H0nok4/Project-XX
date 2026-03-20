using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuestListUI
{
    private const string ListItemPrefabResourcePath = "UI/Quest/QuestJournalListItem";
    private const string InfoLinePrefabResourcePath = "UI/Quest/QuestJournalObjectiveLine";

    private readonly Font font;
    private QuestJournalListItemTemplate listItemPrefab;
    private QuestJournalObjectiveLineTemplate infoLinePrefab;

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
        if (!EnsurePrefabsLoaded())
        {
            return;
        }

        if (quests == null || quests.Count == 0)
        {
            CreateInfoLine(parent, "No quests available.", new Color(0.82f, 0.87f, 0.92f, 1f), FontStyle.Italic);
            return;
        }

        for (int index = 0; index < quests.Count; index++)
        {
            Quest quest = quests[index];
            if (quest == null)
            {
                continue;
            }

            QuestJournalListItemTemplate item = UnityEngine.Object.Instantiate(listItemPrefab, parent, false);
            PrototypeUiToolkit.ApplyFontRecursively(item.Root, font);
            item.gameObject.name = $"Quest_{quest.QuestId}";

            string status = manager != null ? manager.BuildQuestStatusLabel(quest) : string.Empty;
            if (item.TitleText != null)
            {
                item.TitleText.text = quest.QuestName;
            }

            if (item.StatusText != null)
            {
                item.StatusText.text = status;
                item.StatusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(status));
            }

            bool isSelected = selectedQuest != null && string.Equals(selectedQuest.QuestId, quest.QuestId, StringComparison.OrdinalIgnoreCase);
            ApplySelectionState(item, isSelected);

            if (item.Button != null)
            {
                item.Button.onClick.RemoveAllListeners();
                item.Button.onClick.AddListener(() => onSelected?.Invoke(quest));
            }
        }
    }

    private bool EnsurePrefabsLoaded()
    {
        if (listItemPrefab == null)
        {
            GameObject itemPrefabAsset = Resources.Load<GameObject>(ListItemPrefabResourcePath);
            listItemPrefab = itemPrefabAsset != null ? itemPrefabAsset.GetComponent<QuestJournalListItemTemplate>() : null;
        }

        if (infoLinePrefab == null)
        {
            GameObject infoPrefabAsset = Resources.Load<GameObject>(InfoLinePrefabResourcePath);
            infoLinePrefab = infoPrefabAsset != null ? infoPrefabAsset.GetComponent<QuestJournalObjectiveLineTemplate>() : null;
        }

        if (listItemPrefab == null)
        {
            Debug.LogWarning($"[{nameof(QuestListUI)}] Missing quest list item prefab at Resources/{ListItemPrefabResourcePath}.");
            return false;
        }

        if (infoLinePrefab == null)
        {
            Debug.LogWarning($"[{nameof(QuestListUI)}] Missing quest info line prefab at Resources/{InfoLinePrefabResourcePath}.");
            return false;
        }

        return true;
    }

    private void CreateInfoLine(RectTransform parent, string text, Color color, FontStyle fontStyle)
    {
        QuestJournalObjectiveLineTemplate infoLine = UnityEngine.Object.Instantiate(infoLinePrefab, parent, false);
        PrototypeUiToolkit.ApplyFontRecursively(infoLine.Root, font);
        infoLine.gameObject.name = "InfoLine";
        if (infoLine.LabelText != null)
        {
            infoLine.LabelText.text = text ?? string.Empty;
            infoLine.LabelText.color = color;
            infoLine.LabelText.fontStyle = fontStyle;
        }
    }

    private static void ApplySelectionState(QuestJournalListItemTemplate item, bool isSelected)
    {
        if (item == null || item.Button == null)
        {
            return;
        }

        Color normal = isSelected ? new Color(0.26f, 0.41f, 0.54f, 1f) : new Color(0.16f, 0.20f, 0.27f, 0.96f);
        Color highlighted = isSelected ? new Color(0.32f, 0.49f, 0.63f, 1f) : new Color(0.24f, 0.32f, 0.42f, 1f);
        Color pressed = isSelected ? new Color(0.20f, 0.34f, 0.45f, 1f) : new Color(0.12f, 0.16f, 0.22f, 1f);

        ColorBlock colors = item.Button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.selectedColor = highlighted;
        colors.pressedColor = pressed;
        colors.disabledColor = new Color(normal.r * 0.65f, normal.g * 0.65f, normal.b * 0.65f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        item.Button.colors = colors;

        if (item.BackgroundImage != null)
        {
            item.BackgroundImage.color = normal;
        }

        if (item.TitleText != null)
        {
            item.TitleText.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
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
