using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuestDetailUI
{
    private const string DetailPrefabResourcePath = "UI/Quest/QuestJournalDetail";
    private const string ObjectiveLinePrefabResourcePath = "UI/Quest/QuestJournalObjectiveLine";

    private readonly Font font;
    private QuestJournalDetailTemplate detailPrefab;
    private QuestJournalObjectiveLineTemplate objectiveLinePrefab;

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
        if (!EnsurePrefabsLoaded())
        {
            return;
        }

        QuestJournalDetailTemplate detail = UnityEngine.Object.Instantiate(detailPrefab, parent, false);
        PrototypeUiToolkit.ApplyFontRecursively(detail.Root, font);
        detail.gameObject.name = "QuestDetail";
        ClearChildren(detail.ObjectivesRoot);

        if (quest == null || manager == null)
        {
            SetPlaceholderState(detail, "Select a quest to view details.");
            return;
        }

        SetPlaceholderState(detail, null);
        SetText(detail.TitleText, quest.QuestName);
        SetText(detail.StatusText, manager.BuildQuestStatusLabel(quest));
        SetText(detail.DescriptionText, quest.Description);
        SetActive(detail.DescriptionText, !string.IsNullOrWhiteSpace(quest.Description));

        for (int index = 0; index < quest.Objectives.Count; index++)
        {
            CreateObjectiveLine(detail.ObjectivesRoot, manager.BuildObjectiveLine(quest, quest.Objectives[index], index), Color.white, FontStyle.Normal);
        }

        string rewardSummary = manager.BuildRewardSummary(quest);
        if (detail.RewardsText != null)
        {
            detail.RewardsText.text = string.IsNullOrWhiteSpace(rewardSummary) ? string.Empty : $"Rewards: {rewardSummary}";
            detail.RewardsText.gameObject.SetActive(!string.IsNullOrWhiteSpace(rewardSummary));
        }

        QuestRuntimeState questState = manager.GetQuestState(quest.QuestId);
        ConfigureButtons(detail, manager, quest, questState, onRefreshRequested);
    }

    private bool EnsurePrefabsLoaded()
    {
        if (detailPrefab == null)
        {
            GameObject detailPrefabAsset = Resources.Load<GameObject>(DetailPrefabResourcePath);
            detailPrefab = detailPrefabAsset != null ? detailPrefabAsset.GetComponent<QuestJournalDetailTemplate>() : null;
        }

        if (objectiveLinePrefab == null)
        {
            GameObject objectivePrefabAsset = Resources.Load<GameObject>(ObjectiveLinePrefabResourcePath);
            objectiveLinePrefab = objectivePrefabAsset != null ? objectivePrefabAsset.GetComponent<QuestJournalObjectiveLineTemplate>() : null;
        }

        if (detailPrefab == null)
        {
            Debug.LogWarning($"[{nameof(QuestDetailUI)}] Missing quest detail prefab at Resources/{DetailPrefabResourcePath}.");
            return false;
        }

        if (objectiveLinePrefab == null)
        {
            Debug.LogWarning($"[{nameof(QuestDetailUI)}] Missing quest objective line prefab at Resources/{ObjectiveLinePrefabResourcePath}.");
            return false;
        }

        return true;
    }

    private void SetPlaceholderState(QuestJournalDetailTemplate detail, string placeholder)
    {
        bool showPlaceholder = !string.IsNullOrWhiteSpace(placeholder);
        SetText(detail.PlaceholderText, placeholder);
        SetActive(detail.PlaceholderText, showPlaceholder);
        SetActive(detail.TitleText, !showPlaceholder);
        SetActive(detail.StatusText, !showPlaceholder);
        SetActive(detail.DescriptionText, !showPlaceholder);
        SetActive(detail.ObjectivesRoot, !showPlaceholder);
        SetActive(detail.RewardsText, false);
        SetActive(detail.TurnInHintText, false);
        SetActive(detail.ClaimButton, false);
        SetActive(detail.AcceptButton, false);
        SetActive(detail.TrackButton, false);
    }

    private void ConfigureButtons(
        QuestJournalDetailTemplate detail,
        QuestManager manager,
        Quest quest,
        QuestRuntimeState questState,
        Action onRefreshRequested)
    {
        BindButton(detail.ClaimButton, null);
        BindButton(detail.AcceptButton, null);
        BindButton(detail.TrackButton, null);
        SetActive(detail.ClaimButton, false);
        SetActive(detail.AcceptButton, false);
        SetActive(detail.TrackButton, false);
        SetActive(detail.TurnInHintText, false);

        if (manager.CanClaimQuest(quest.QuestId))
        {
            BindButton(detail.ClaimButton, () =>
            {
                manager.TryClaimQuest(quest.QuestId);
                onRefreshRequested?.Invoke();
            });
            SetActive(detail.ClaimButton, true);
        }
        else if (manager.CanStartQuest(quest.QuestId))
        {
            BindButton(detail.AcceptButton, () =>
            {
                manager.StartQuest(quest.QuestId, quest.GiverNpcId);
                onRefreshRequested?.Invoke();
            });
            SetActive(detail.AcceptButton, true);
        }
        else if (questState != null
            && questState.status == QuestStatus.Completed
            && !questState.rewardsClaimed
            && !string.IsNullOrWhiteSpace(quest.TurnInNpcId))
        {
            SetText(detail.TurnInHintText, $"Turn in this quest at {manager.GetNpcDisplayName(quest.TurnInNpcId)}.");
            SetActive(detail.TurnInHintText, true);
        }
        else if (questState != null && !questState.rewardsClaimed)
        {
            BindButton(detail.TrackButton, () =>
            {
                manager.ToggleTracked(quest.QuestId);
                onRefreshRequested?.Invoke();
            });
            if (detail.TrackButtonLabel != null)
            {
                detail.TrackButtonLabel.text = manager.IsQuestTracked(quest.QuestId) ? "Stop Tracking" : "Track Quest";
            }

            SetActive(detail.TrackButton, true);
        }
    }

    private void CreateObjectiveLine(RectTransform parent, string text, Color color, FontStyle fontStyle)
    {
        if (parent == null)
        {
            return;
        }

        QuestJournalObjectiveLineTemplate line = UnityEngine.Object.Instantiate(objectiveLinePrefab, parent, false);
        PrototypeUiToolkit.ApplyFontRecursively(line.Root, font);
        if (line.LabelText != null)
        {
            line.LabelText.text = text ?? string.Empty;
            line.LabelText.color = color;
            line.LabelText.fontStyle = PrototypeUiToolkit.ConvertFontStyle(fontStyle);
        }
    }

    private static void BindButton(Button button, Action callback)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        if (callback != null)
        {
            button.onClick.AddListener(() => callback());
        }
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null)
        {
            label.text = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }
    }

    private static void SetActive(Component component, bool active)
    {
        if (component != null)
        {
            component.gameObject.SetActive(active);
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
