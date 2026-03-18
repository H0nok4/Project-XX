using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-35)]
[DisallowMultipleComponent]
public sealed class DialogueSystem : MonoBehaviour
{
    private static DialogueSystem instance;

    private readonly Dictionary<string, DialogueNode> nodes = new Dictionary<string, DialogueNode>(StringComparer.OrdinalIgnoreCase);

    private Font runtimeFont;
    private PlayerInteractor playerInteractor;
    private PlayerInteractionState interactionState;
    private QuestNPC activeNpc;
    private string currentNodeId = string.Empty;
    private PrototypeUiToolkit.WindowChrome windowChrome;
    private RectTransform optionContainer;
    private bool dialogueOpen;

    public static DialogueSystem Instance => instance;

    public static DialogueSystem GetOrCreate()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<DialogueSystem>();
        if (instance != null)
        {
            return instance;
        }

        GameObject systemObject = new GameObject("DialogueSystem");
        return systemObject.AddComponent<DialogueSystem>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (windowChrome != null && windowChrome.Root != null)
        {
            Destroy(windowChrome.Root.gameObject);
        }
    }

    public void OpenNpcDialogue(QuestNPC npc, PlayerInteractor interactor)
    {
        if (npc == null)
        {
            return;
        }

        activeNpc = npc;
        if (interactor != null)
        {
            playerInteractor = interactor;
        }

        ResolveReferences();
        BuildNpcConversation(npc);
        QuestEventHub.RaiseTalk(npc.NpcId, npc.NpcName);
        SetDialogueOpen(true);
        ShowNode("root");
    }

    public void RefreshCurrentConversation()
    {
        if (activeNpc == null)
        {
            return;
        }

        BuildNpcConversation(activeNpc);
        ShowNode(string.IsNullOrWhiteSpace(currentNodeId) ? "root" : currentNodeId);
    }

    public void CloseDialogue()
    {
        SetDialogueOpen(false);
    }

    public void ShowNode(string nodeId)
    {
        if (!EnsureWindow() || string.IsNullOrWhiteSpace(nodeId))
        {
            return;
        }

        if (!nodes.TryGetValue(nodeId.Trim(), out DialogueNode node) || node == null)
        {
            return;
        }

        currentNodeId = node.NodeId;
        RebuildWindow(node);
    }

    private void ResolveReferences()
    {
        runtimeFont ??= PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont;
        if (playerInteractor == null)
        {
            playerInteractor = FindFirstObjectByType<PlayerInteractor>();
        }

        if (interactionState == null && playerInteractor != null)
        {
            interactionState = playerInteractor.GetComponent<PlayerInteractionState>();
        }

        if (interactionState == null)
        {
            interactionState = FindFirstObjectByType<PlayerInteractionState>();
        }
    }

    private bool EnsureWindow()
    {
        ResolveReferences();
        if (windowChrome != null && windowChrome.Root != null)
        {
            return true;
        }

        RectTransform modalLayer = PrototypeRuntimeUiManager.GetOrCreate().GetLayerRoot(PrototypeUiLayer.Modal);
        windowChrome = PrototypeUiToolkit.CreateWindowChrome(modalLayer, runtimeFont, "DialogueWindow", "对话", string.Empty, new Vector2(760f, 560f));

        VerticalLayoutGroup bodyLayout = windowChrome.BodyRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.spacing = 10f;
        bodyLayout.childAlignment = TextAnchor.UpperLeft;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;

        PrototypeUiToolkit.CreateButton(
            windowChrome.FooterRoot,
            runtimeFont,
            "结束对话",
            CloseDialogue,
            new Color(0.22f, 0.27f, 0.34f, 0.98f),
            new Color(0.31f, 0.38f, 0.48f, 1f),
            new Color(0.17f, 0.21f, 0.29f, 1f),
            38f);

        PrototypeUiToolkit.SetVisible(windowChrome.Root, false);
        return true;
    }

    private void SetDialogueOpen(bool open)
    {
        if (!EnsureWindow())
        {
            return;
        }

        dialogueOpen = open;
        PrototypeUiToolkit.SetVisible(windowChrome.Root, open);
        if (interactionState != null)
        {
            interactionState.SetUiFocused(this, open);
        }

        bool keepCursorFree = open || (interactionState != null && interactionState.IsUiFocused);
        Cursor.lockState = keepCursorFree ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = keepCursorFree;
    }

    private void BuildNpcConversation(QuestNPC npc)
    {
        nodes.Clear();
        QuestManager questManager = QuestManager.GetOrCreate();
        DialogueNode root = new DialogueNode
        {
            nodeId = "root",
            speakerName = npc.NpcName,
            dynamicTextProvider = () => BuildRootText(npc, questManager)
        };

        AddQuestOptions(root, npc, questManager, questManager.GetCompletableQuestsForNpc(npc.NpcId), "提交");
        AddQuestOptions(root, npc, questManager, questManager.GetAvailableQuestsForNpc(npc.NpcId), "查看");
        AddQuestOptions(root, npc, questManager, questManager.GetRelevantQuestsForNpc(npc.NpcId), "汇报");

        root.options.Add(new DialogueOption
        {
            optionText = "查看任务日志",
            onSelected = system =>
            {
                QuestTrackerHUD.GetOrCreate().OpenJournal();
                system.CloseDialogue();
            }
        });
        root.options.Add(new DialogueOption
        {
            optionText = "离开",
            onSelected = system => system.CloseDialogue()
        });
        nodes[root.NodeId] = root;
    }

    private void AddQuestOptions(DialogueNode root, QuestNPC npc, QuestManager manager, IReadOnlyList<Quest> quests, string verb)
    {
        if (root == null || quests == null)
        {
            return;
        }

        for (int index = 0; index < quests.Count; index++)
        {
            Quest quest = quests[index];
            if (quest == null || nodes.ContainsKey($"quest_{quest.QuestId}"))
            {
                continue;
            }

            string nodeId = $"quest_{quest.QuestId}";
            root.options.Add(new DialogueOption
            {
                optionText = $"{verb}【{quest.QuestName}】",
                targetNodeId = nodeId
            });

            DialogueNode detailNode = new DialogueNode
            {
                nodeId = nodeId,
                speakerName = npc.NpcName,
                dynamicTextProvider = () => BuildQuestDetailText(manager, quest)
            };
            detailNode.options.Add(new DialogueOption
            {
                labelProvider = () => ResolvePrimaryActionLabel(manager, quest, npc.NpcId),
                availabilityEvaluator = () => HasPrimaryAction(manager, quest, npc.NpcId),
                onSelected = system =>
                {
                    ExecutePrimaryAction(manager, quest, npc.NpcId);
                    system.RefreshCurrentConversation();
                }
            });
            detailNode.options.Add(new DialogueOption
            {
                optionText = "返回",
                targetNodeId = root.NodeId
            });
            nodes[detailNode.NodeId] = detailNode;
        }
    }

    private void RebuildWindow(DialogueNode node)
    {
        if (windowChrome == null || node == null)
        {
            return;
        }

        windowChrome.TitleText.text = activeNpc != null ? activeNpc.DisplayName : "对话";
        if (windowChrome.SubtitleText != null)
        {
            windowChrome.SubtitleText.text = activeNpc != null ? activeNpc.Title : string.Empty;
            windowChrome.SubtitleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(windowChrome.SubtitleText.text));
        }

        ClearChildren(windowChrome.BodyRoot);
        PrototypeUiToolkit.CreateText(windowChrome.BodyRoot, runtimeFont, node.SpeakerName, 18, FontStyle.Bold, new Color(0.96f, 0.8f, 0.44f, 1f), TextAnchor.UpperLeft);
        PrototypeUiToolkit.CreateText(windowChrome.BodyRoot, runtimeFont, node.DialogueText, 16, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);

        optionContainer = PrototypeUiToolkit.CreateRectTransform("Options", windowChrome.BodyRoot);
        VerticalLayoutGroup optionLayout = optionContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        optionLayout.spacing = 8f;
        optionLayout.childAlignment = TextAnchor.UpperLeft;
        optionLayout.childControlWidth = true;
        optionLayout.childControlHeight = true;
        optionLayout.childForceExpandWidth = true;
        optionLayout.childForceExpandHeight = false;

        bool createdAnyOption = false;
        if (node.options != null)
        {
            for (int index = 0; index < node.options.Count; index++)
            {
                DialogueOption option = node.options[index];
                if (option == null || !option.IsAvailable())
                {
                    continue;
                }

                createdAnyOption = true;
                PrototypeUiToolkit.CreateButton(
                    optionContainer,
                    runtimeFont,
                    option.OptionText,
                    () => HandleOptionSelected(option),
                    new Color(0.18f, 0.24f, 0.33f, 0.98f),
                    new Color(0.26f, 0.35f, 0.47f, 1f),
                    new Color(0.14f, 0.19f, 0.27f, 1f),
                    42f);
            }
        }

        if (!createdAnyOption)
        {
            PrototypeUiToolkit.CreateText(optionContainer, runtimeFont, "没有可执行的对话选项。", 14, FontStyle.Normal, new Color(0.82f, 0.87f, 0.92f, 1f), TextAnchor.UpperLeft);
        }
    }

    private void HandleOptionSelected(DialogueOption option)
    {
        if (option == null)
        {
            return;
        }

        option.Invoke(this);
        if (!string.IsNullOrWhiteSpace(option.TargetNodeId))
        {
            ShowNode(option.TargetNodeId);
            return;
        }

        if (dialogueOpen)
        {
            RebuildWindow(nodes[currentNodeId]);
        }
        else
        {
            CloseDialogue();
        }
    }

    private static string BuildRootText(QuestNPC npc, QuestManager manager)
    {
        StringBuilder builder = new StringBuilder(192);
        if (npc != null && !string.IsNullOrWhiteSpace(npc.GreetingLine))
        {
            builder.Append(npc.GreetingLine);
        }

        if (manager == null || npc == null)
        {
            return builder.ToString();
        }

        int available = manager.GetAvailableQuestsForNpc(npc.NpcId).Count;
        int completable = manager.GetCompletableQuestsForNpc(npc.NpcId).Count;
        int active = manager.GetRelevantQuestsForNpc(npc.NpcId).Count;
        if (builder.Length > 0)
        {
            builder.Append("\n\n");
        }

        builder.Append($"可接任务 {available}  ·  可交任务 {completable}  ·  相关任务 {active}");
        return builder.ToString();
    }

    private static string BuildQuestDetailText(QuestManager manager, Quest quest)
    {
        return manager != null && quest != null ? manager.BuildQuestSummary(quest) : string.Empty;
    }

    private static bool HasPrimaryAction(QuestManager manager, Quest quest, string npcId)
    {
        if (manager == null || quest == null)
        {
            return false;
        }

        if (manager.CanClaimQuest(quest.QuestId, npcId) || manager.CanStartQuest(quest.QuestId))
        {
            return true;
        }

        QuestRuntimeState state = manager.GetQuestState(quest.QuestId);
        return state != null && !state.rewardsClaimed && state.status == QuestStatus.InProgress;
    }

    private static string ResolvePrimaryActionLabel(QuestManager manager, Quest quest, string npcId)
    {
        if (manager == null || quest == null)
        {
            return string.Empty;
        }

        if (manager.CanClaimQuest(quest.QuestId, npcId))
        {
            return "提交任务";
        }

        if (manager.CanStartQuest(quest.QuestId))
        {
            return "接取任务";
        }

        return manager.IsQuestTracked(quest.QuestId) ? "取消追踪" : "追踪任务";
    }

    private static void ExecutePrimaryAction(QuestManager manager, Quest quest, string npcId)
    {
        if (manager == null || quest == null)
        {
            return;
        }

        if (manager.CanClaimQuest(quest.QuestId, npcId))
        {
            manager.TryClaimQuest(quest.QuestId, npcId);
            return;
        }

        if (manager.CanStartQuest(quest.QuestId))
        {
            manager.StartQuest(quest.QuestId, npcId);
            return;
        }

        manager.ToggleTracked(quest.QuestId);
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
