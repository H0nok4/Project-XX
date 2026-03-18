using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public sealed class QuestManager : MonoBehaviour
{
    private static QuestManager instance;

    [SerializeField] private PrototypeMainMenuController menuController;
    [SerializeField] private PrototypeItemCatalog itemCatalog;
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private bool autoLoadStandaloneProfile = true;

    private readonly Dictionary<string, Quest> questLookup = new Dictionary<string, Quest>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, QuestRuntimeState> stateLookup = new Dictionary<string, QuestRuntimeState>(StringComparer.OrdinalIgnoreCase);

    private PrototypeProfileService.ProfileData standaloneProfile;
    private WorldStateData activeWorldState;
    private QuestTrackerHUD trackerHud;
    private bool initialized;
    private float inventoryRefreshTimer;

    public static QuestManager Instance => instance;
    public event Action<Quest, QuestRuntimeState> QuestChanged;

    public WorldStateData WorldState => activeWorldState;
    public bool IsInitialized => initialized;

    public static QuestManager GetOrCreate()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<QuestManager>();
        if (instance != null)
        {
            return instance;
        }

        GameObject managerObject = new GameObject("QuestManager");
        return managerObject.AddComponent<QuestManager>();
    }

    public void ConfigureRuntime(
        PrototypeItemCatalog catalog = null,
        PrototypeMainMenuController controller = null,
        PlayerInteractor interactor = null,
        bool loadStandaloneProfile = true)
    {
        if (catalog != null)
        {
            itemCatalog = catalog;
        }

        if (controller != null)
        {
            menuController = controller;
        }

        if (interactor != null)
        {
            playerInteractor = interactor;
        }

        autoLoadStandaloneProfile = loadStandaloneProfile;
        initialized = false;
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

    private void Start()
    {
        TryInitialize();
    }

    private void OnEnable()
    {
        QuestEventHub.EventRaised += HandleQuestEvent;
    }

    private void OnDisable()
    {
        QuestEventHub.EventRaised -= HandleQuestEvent;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (!TryInitialize())
        {
            return;
        }

        if (Time.unscaledTime < inventoryRefreshTimer)
        {
            return;
        }

        inventoryRefreshTimer = Time.unscaledTime + 0.35f;
        RefreshInventoryDrivenQuestCompletion();
    }

    public bool TryInitialize()
    {
        ResolveReferences();
        if (itemCatalog == null)
        {
            return false;
        }

        if (menuController != null)
        {
            if (menuController.profile == null || menuController.ProfileWorldState == null)
            {
                return false;
            }
        }
        else if (standaloneProfile == null && autoLoadStandaloneProfile)
        {
            standaloneProfile = PrototypeProfileService.LoadProfile(itemCatalog) ?? PrototypeProfileService.CreateDefaultProfile(itemCatalog);
        }

        WorldStateData worldState = ResolveWorldState();
        if (worldState == null)
        {
            return false;
        }

        if (initialized && ReferenceEquals(worldState, activeWorldState))
        {
            if (trackerHud != null)
            {
                trackerHud.Bind(this, playerInteractor);
            }

            return true;
        }

        activeWorldState = worldState;
        EnsureWorldState(activeWorldState);
        BuildQuestCatalog();
        RebuildStateLookup();
        initialized = true;
        inventoryRefreshTimer = 0f;
        RefreshInventoryDrivenQuestCompletion();

        trackerHud = QuestTrackerHUD.GetOrCreate();
        trackerHud.Bind(this, playerInteractor);
        trackerHud.RefreshImmediate();
        return true;
    }

    public IReadOnlyList<Quest> GetAllQuests()
    {
        var quests = new List<Quest>();
        if (!TryInitialize())
        {
            return quests;
        }

        foreach (Quest quest in questLookup.Values)
        {
            if (quest != null)
            {
                quests.Add(quest);
            }
        }

        SortQuestList(quests);
        return quests;
    }

    public Quest GetQuest(string questId)
    {
        if (!TryInitialize() || string.IsNullOrWhiteSpace(questId))
        {
            return null;
        }

        questLookup.TryGetValue(questId.Trim(), out Quest quest);
        return quest;
    }

    public QuestRuntimeState GetQuestState(string questId)
    {
        if (!TryInitialize() || string.IsNullOrWhiteSpace(questId))
        {
            return null;
        }

        stateLookup.TryGetValue(questId.Trim(), out QuestRuntimeState runtimeState);
        return runtimeState;
    }

    public IReadOnlyList<Quest> GetAvailableQuestsForNpc(string npcId)
    {
        var quests = new List<Quest>();
        if (!TryInitialize() || string.IsNullOrWhiteSpace(npcId))
        {
            return quests;
        }

        string sanitizedNpcId = npcId.Trim();
        foreach (Quest quest in questLookup.Values)
        {
            if (quest == null || !quest.IsGivenByNpc(sanitizedNpcId) || !CanStartQuest(quest.QuestId))
            {
                continue;
            }

            quests.Add(quest);
        }

        SortQuestList(quests);
        return quests;
    }

    public IReadOnlyList<Quest> GetCompletableQuestsForNpc(string npcId)
    {
        var quests = new List<Quest>();
        if (!TryInitialize() || string.IsNullOrWhiteSpace(npcId))
        {
            return quests;
        }

        string sanitizedNpcId = npcId.Trim();
        foreach (Quest quest in questLookup.Values)
        {
            if (quest == null || !quest.IsTurnInTarget(sanitizedNpcId))
            {
                continue;
            }

            QuestRuntimeState runtimeState = GetQuestState(quest.QuestId);
            if (runtimeState == null || runtimeState.rewardsClaimed)
            {
                continue;
            }

            RefreshQuestCompletion(quest, runtimeState, false);
            if (runtimeState.status == QuestStatus.Completed)
            {
                quests.Add(quest);
            }
        }

        SortQuestList(quests);
        return quests;
    }

    public IReadOnlyList<Quest> GetRelevantQuestsForNpc(string npcId)
    {
        var quests = new List<Quest>();
        if (!TryInitialize() || string.IsNullOrWhiteSpace(npcId))
        {
            return quests;
        }

        string sanitizedNpcId = npcId.Trim();
        foreach (Quest quest in questLookup.Values)
        {
            if (quest == null || (!quest.IsGivenByNpc(sanitizedNpcId) && !quest.IsTurnInTarget(sanitizedNpcId)))
            {
                continue;
            }

            QuestRuntimeState runtimeState = GetQuestState(quest.QuestId);
            if (runtimeState == null || runtimeState.rewardsClaimed || runtimeState.status == QuestStatus.NotStarted)
            {
                continue;
            }

            quests.Add(quest);
        }

        SortQuestList(quests);
        return quests;
    }

    public IReadOnlyList<Quest> GetTrackedQuests()
    {
        var quests = new List<Quest>();
        if (!TryInitialize())
        {
            return quests;
        }

        foreach (Quest quest in questLookup.Values)
        {
            if (quest == null)
            {
                continue;
            }

            QuestRuntimeState runtimeState = GetQuestState(quest.QuestId);
            if (runtimeState == null || runtimeState.rewardsClaimed || runtimeState.status != QuestStatus.InProgress || !runtimeState.tracked)
            {
                continue;
            }

            quests.Add(quest);
        }

        SortQuestList(quests);
        return quests;
    }

    public IReadOnlyList<Quest> GetActiveQuests()
    {
        var quests = new List<Quest>();
        if (!TryInitialize())
        {
            return quests;
        }

        foreach (Quest quest in questLookup.Values)
        {
            if (quest == null)
            {
                continue;
            }

            QuestRuntimeState runtimeState = GetQuestState(quest.QuestId);
            if (runtimeState == null || runtimeState.rewardsClaimed || runtimeState.status == QuestStatus.NotStarted)
            {
                continue;
            }

            quests.Add(quest);
        }

        SortQuestList(quests);
        return quests;
    }

    public bool CanStartQuest(string questId)
    {
        Quest quest = GetQuest(questId);
        if (quest == null)
        {
            return false;
        }

        QuestRuntimeState runtimeState = GetQuestState(questId);
        if (runtimeState != null)
        {
            if (runtimeState.rewardsClaimed && !quest.IsRepeatable)
            {
                return false;
            }

            if (runtimeState.status == QuestStatus.InProgress)
            {
                return false;
            }

            if (runtimeState.status == QuestStatus.Completed && !runtimeState.rewardsClaimed)
            {
                return false;
            }
        }

        return quest.CanStart(IsQuestTurnedIn, HasStoryFlag);
    }

    public bool CanClaimQuest(string questId, string npcId = null)
    {
        Quest quest = GetQuest(questId);
        QuestRuntimeState runtimeState = GetQuestState(questId);
        if (quest == null || runtimeState == null || runtimeState.rewardsClaimed)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(npcId) && !string.IsNullOrWhiteSpace(quest.TurnInNpcId))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(npcId) && !quest.IsTurnInTarget(npcId))
        {
            return false;
        }

        RefreshQuestCompletion(quest, runtimeState, false);
        if (runtimeState.status != QuestStatus.Completed)
        {
            return false;
        }

        for (int index = 0; index < quest.Objectives.Count; index++)
        {
            if (!quest.Objectives[index].CanClaim(this))
            {
                return false;
            }
        }

        return true;
    }
    public bool IsQuestTracked(string questId)
    {
        QuestRuntimeState runtimeState = GetQuestState(questId);
        return runtimeState != null && runtimeState.tracked && !runtimeState.rewardsClaimed;
    }

    public bool ToggleTracked(string questId)
    {
        Quest quest = GetQuest(questId);
        QuestRuntimeState runtimeState = GetQuestState(questId);
        if (quest == null || runtimeState == null || runtimeState.rewardsClaimed || runtimeState.status == QuestStatus.NotStarted)
        {
            return false;
        }

        runtimeState.tracked = !runtimeState.tracked;
        SaveRuntimeState();
        RaiseQuestChanged(quest, runtimeState);
        ShowFeedback(runtimeState.tracked ? $"开始追踪：{quest.QuestName}" : $"已取消追踪：{quest.QuestName}", true);
        return true;
    }

    public bool StartQuest(string questId, string starterNpcId = null)
    {
        Quest quest = GetQuest(questId);
        if (quest == null)
        {
            return false;
        }

        if (!CanStartQuest(questId))
        {
            ShowFeedback($"任务“{quest.QuestName}”尚未解锁。", false);
            return false;
        }

        QuestRuntimeState runtimeState = GetOrCreateState(quest);
        runtimeState.status = QuestStatus.InProgress;
        runtimeState.rewardsClaimed = false;
        runtimeState.tracked = quest.autoTrack || GetTrackedQuests().Count == 0;
        runtimeState.ResetProgress(quest.Objectives.Count);
        runtimeState.Sanitize(quest.QuestId, quest.Objectives.Count);

        if (!string.IsNullOrWhiteSpace(starterNpcId))
        {
            ApplyEventInternal(new QuestEventRecord(QuestEventType.Talk, starterNpcId.Trim()), false);
        }

        RefreshQuestCompletion(quest, runtimeState, false);
        SaveRuntimeState();
        RaiseQuestChanged(quest, runtimeState);
        ShowFeedback($"已接取任务：{quest.QuestName}", true);
        return true;
    }

    public bool TryClaimQuest(string questId, string npcId = null)
    {
        Quest quest = GetQuest(questId);
        QuestRuntimeState runtimeState = GetQuestState(questId);
        if (quest == null || runtimeState == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(npcId) && !string.IsNullOrWhiteSpace(quest.TurnInNpcId))
        {
            ShowFeedback($"请前往 {ResolveNpcDisplayName(quest.TurnInNpcId)} 提交任务。", false);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(npcId) && !quest.IsTurnInTarget(npcId))
        {
            ShowFeedback("这里不能提交这项任务。", false);
            return false;
        }

        RefreshQuestCompletion(quest, runtimeState, false);
        if (runtimeState.status != QuestStatus.Completed || runtimeState.rewardsClaimed)
        {
            ShowFeedback("目标尚未全部完成。", false);
            return false;
        }

        for (int index = 0; index < quest.Objectives.Count; index++)
        {
            QuestObjective objective = quest.Objectives[index];
            if (objective == null || objective.CanClaim(this))
            {
                continue;
            }

            string itemLabel = objective is DeliverObjective deliverObjective
                ? ResolveDefinitionDisplayName(deliverObjective.ItemId)
                : "任务物资";
            ShowFeedback($"提交失败：缺少 {itemLabel}。", false);
            return false;
        }

        for (int index = 0; index < quest.Objectives.Count; index++)
        {
            QuestObjective objective = quest.Objectives[index];
            if (objective != null && !objective.ConsumeClaimRequirements(this))
            {
                ShowFeedback("任务物资校验失败，请重新尝试。", false);
                return false;
            }
        }

        string rewardSummary = quest.GrantRewards(this);
        runtimeState.rewardsClaimed = true;
        runtimeState.tracked = false;
        runtimeState.status = QuestStatus.Completed;
        RefreshInventoryDrivenQuestCompletion();
        SaveRuntimeState();
        RaiseQuestChanged(quest, runtimeState);
        ShowFeedback(
            string.IsNullOrWhiteSpace(rewardSummary)
                ? $"已提交任务：{quest.QuestName}"
                : $"已提交任务：{quest.QuestName} · {rewardSummary}",
            true);
        return true;
    }

    public int GetStorageItemCount(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
        {
            return 0;
        }

        string sanitizedId = definitionId.Trim();
        if (menuController != null)
        {
            int total = 0;
            total += CountDefinitionInInventory(menuController.StashInventory, sanitizedId);
            total += CountDefinitionInWeapons(menuController.WeaponLocker, sanitizedId);
            return total;
        }

        if (standaloneProfile == null)
        {
            return 0;
        }

        int standaloneTotal = 0;
        standaloneTotal += CountDefinitionInRecords(standaloneProfile.stashItemInstances, sanitizedId);
        standaloneTotal += CountWeaponInRecords(standaloneProfile.stashWeaponInstances, sanitizedId);
        return standaloneTotal;
    }

    public bool TryConsumeStorageItem(string definitionId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(definitionId) || quantity <= 0)
        {
            return false;
        }

        string sanitizedId = definitionId.Trim();
        bool isWeapon = itemCatalog != null && itemCatalog.FindWeaponById(sanitizedId) != null;

        if (menuController != null)
        {
            return isWeapon
                ? TryConsumeWeaponLocker(menuController.WeaponLocker, sanitizedId, quantity)
                : TryConsumeInventoryDefinition(menuController.StashInventory, sanitizedId, quantity);
        }

        if (standaloneProfile == null || GetStorageItemCount(sanitizedId) < quantity)
        {
            return false;
        }

        return isWeapon
            ? TryConsumeWeaponRecords(standaloneProfile.stashWeaponInstances, sanitizedId, quantity)
            : TryConsumeItemRecords(standaloneProfile.stashItemInstances, sanitizedId, quantity);
    }

    public string GrantRewardBundle(QuestReward reward)
    {
        if (reward == null)
        {
            return string.Empty;
        }

        reward.Sanitize();
        var parts = new List<string>();

        if (reward.funds > 0)
        {
            GrantFunds(reward.funds);
            parts.Add($"现金 +{reward.funds}");
        }

        if (reward.experience > 0)
        {
            GrantExperience(reward.experience, out int levelsGained);
            parts.Add(levelsGained > 0
                ? $"经验 +{reward.experience} (Lv +{levelsGained})"
                : $"经验 +{reward.experience}");
        }

        if (reward.items != null)
        {
            for (int index = 0; index < reward.items.Count; index++)
            {
                QuestRewardItem rewardItem = reward.items[index];
                if (rewardItem == null || string.IsNullOrWhiteSpace(rewardItem.DefinitionId))
                {
                    continue;
                }

                if (TryGrantRewardItem(rewardItem.DefinitionId, rewardItem.Quantity, out string itemSummary))
                {
                    parts.Add(itemSummary);
                }
                else
                {
                    parts.Add($"奖励已跳过：{ResolveDefinitionDisplayName(rewardItem.DefinitionId)} 无法放入仓库");
                }
            }
        }

        ApplyUnlockList(activeWorldState != null ? activeWorldState.storyFlags : null, reward.storyFlags);
        ApplyUnlockList(activeWorldState != null ? activeWorldState.unlockedRaidNpcIds : null, reward.unlockedNpcIds);
        ApplyUnlockList(activeWorldState != null ? activeWorldState.unlockedRaidMerchantIds : null, reward.unlockedMerchantIds);
        return string.Join("，", parts);
    }

    public string AddMerchantReputationReward(string merchantId, int reputationReward)
    {
        if (!TryInitialize() || string.IsNullOrWhiteSpace(merchantId) || reputationReward <= 0 || activeWorldState == null)
        {
            return string.Empty;
        }

        MerchantManager merchantManager = new MerchantManager(activeWorldState);
        MerchantManager.TradeUpdateResult result = merchantManager.AddReputation(merchantId.Trim(), reputationReward);
        MerchantData merchantData = merchantManager.GetMerchantData(merchantId.Trim());
        if (menuController != null && menuController.MerchantCatalog != null)
        {
            menuController.MerchantCatalog.RepriceRuntimeInventories(merchant => merchantManager.GetPriceMultiplier(merchant.MerchantId));
            menuController.RequestUiRefresh();
        }

        if (merchantData == null)
        {
            return string.Empty;
        }

        string levelSuffix = result.ReputationChanged ? $" ({GetReputationLabel(merchantData.Reputation)})" : string.Empty;
        return $"信誉 +{reputationReward}{levelSuffix}";
    }

    public string BuildQuestStatusLabel(Quest quest)
    {
        if (quest == null)
        {
            return string.Empty;
        }

        QuestRuntimeState runtimeState = GetQuestState(quest.QuestId);
        if (runtimeState != null)
        {
            if (runtimeState.rewardsClaimed)
            {
                return quest.IsRepeatable && CanStartQuest(quest.QuestId) ? "可重复接取" : "已提交";
            }

            if (runtimeState.status == QuestStatus.Completed)
            {
                return "可提交";
            }

            if (runtimeState.status == QuestStatus.InProgress)
            {
                return "进行中";
            }

            if (runtimeState.status == QuestStatus.Failed)
            {
                return "失败";
            }
        }

        return CanStartQuest(quest.QuestId) ? "可接取" : "未解锁";
    }

    public string BuildQuestSummary(Quest quest)
    {
        if (quest == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(256);
        builder.Append(BuildQuestTypeLabel(quest.type));
        builder.Append(" · ");
        builder.Append(BuildQuestStatusLabel(quest));

        if (!string.IsNullOrWhiteSpace(quest.Description))
        {
            builder.Append('\n');
            builder.Append(quest.Description);
        }

        QuestRuntimeState runtimeState = GetQuestState(quest.QuestId);
        for (int index = 0; index < quest.Objectives.Count; index++)
        {
            builder.Append('\n');
            builder.Append(BuildObjectiveLine(quest, quest.Objectives[index], runtimeState, index));
        }

        string rewardSummary = BuildRewardSummary(quest);
        if (!string.IsNullOrWhiteSpace(rewardSummary))
        {
            builder.Append("\n奖励：");
            builder.Append(rewardSummary);
        }

        return builder.ToString();
    }
    public string BuildObjectiveLine(Quest quest, QuestObjective objective, int objectiveIndex)
    {
        return BuildObjectiveLine(quest, objective, GetQuestState(quest != null ? quest.QuestId : string.Empty), objectiveIndex);
    }

    public string BuildRewardSummary(Quest quest)
    {
        if (quest == null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        QuestReward reward = quest.reward;
        if (reward != null)
        {
            reward.Sanitize();
            if (reward.funds > 0)
            {
                parts.Add($"现金 +{reward.funds}");
            }

            if (reward.experience > 0)
            {
                parts.Add($"经验 +{reward.experience}");
            }

            if (reward.items != null)
            {
                for (int index = 0; index < reward.items.Count; index++)
                {
                    QuestRewardItem rewardItem = reward.items[index];
                    if (rewardItem == null || string.IsNullOrWhiteSpace(rewardItem.DefinitionId))
                    {
                        continue;
                    }

                    parts.Add($"{ResolveDefinitionDisplayName(rewardItem.DefinitionId)} x{Mathf.Max(1, rewardItem.Quantity)}");
                }
            }
        }

        if (quest is MerchantQuest merchantQuest && merchantQuest.ReputationReward > 0)
        {
            parts.Add($"信誉 +{merchantQuest.ReputationReward}");
        }

        return string.Join("，", parts);
    }

    public string GetNpcDisplayName(string npcId)
    {
        return ResolveNpcDisplayName(npcId);
    }

    public string BuildTrackerText()
    {
        IReadOnlyList<Quest> trackedQuests = GetTrackedQuests();
        if (trackedQuests == null || trackedQuests.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(256);
        for (int questIndex = 0; questIndex < trackedQuests.Count; questIndex++)
        {
            Quest quest = trackedQuests[questIndex];
            QuestRuntimeState runtimeState = GetQuestState(quest.QuestId);
            if (quest == null || runtimeState == null || runtimeState.rewardsClaimed)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append("\n\n");
            }

            builder.Append(quest.QuestName);
            for (int objectiveIndex = 0; objectiveIndex < quest.Objectives.Count; objectiveIndex++)
            {
                builder.Append('\n');
                builder.Append(BuildObjectiveLine(quest, quest.Objectives[objectiveIndex], runtimeState, objectiveIndex));
            }
        }

        return builder.ToString();
    }

    public string BuildMerchantQuestSummary(string merchantId)
    {
        Quest quest = GetPrimaryMerchantQuest(merchantId, out QuestRuntimeState runtimeState);
        if (quest == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(192);
        builder.Append(quest.QuestName);
        builder.Append("\n");
        builder.Append(BuildQuestStatusLabel(quest));

        if (!string.IsNullOrWhiteSpace(quest.Description))
        {
            builder.Append("\n");
            builder.Append(quest.Description);
        }

        for (int index = 0; index < quest.Objectives.Count; index++)
        {
            builder.Append("\n");
            builder.Append(BuildObjectiveLine(quest, quest.Objectives[index], runtimeState, index));
        }

        string rewardSummary = BuildRewardSummary(quest);
        if (!string.IsNullOrWhiteSpace(rewardSummary))
        {
            builder.Append("\n奖励：");
            builder.Append(rewardSummary);
        }

        return builder.ToString();
    }

    public bool HasMerchantQuestAction(string merchantId)
    {
        Quest quest = GetPrimaryMerchantQuest(merchantId, out QuestRuntimeState runtimeState);
        if (quest == null)
        {
            return false;
        }

        if (CanClaimQuest(quest.QuestId, merchantId) || CanStartQuest(quest.QuestId))
        {
            return true;
        }

        return runtimeState != null && !runtimeState.rewardsClaimed && runtimeState.status == QuestStatus.InProgress;
    }

    public string GetMerchantQuestActionLabel(string merchantId)
    {
        Quest quest = GetPrimaryMerchantQuest(merchantId, out QuestRuntimeState runtimeState);
        if (quest == null)
        {
            return string.Empty;
        }

        if (CanClaimQuest(quest.QuestId, merchantId))
        {
            return "提交委托";
        }

        if (CanStartQuest(quest.QuestId))
        {
            return "接取委托";
        }

        if (runtimeState != null && !runtimeState.rewardsClaimed && runtimeState.status == QuestStatus.InProgress)
        {
            return runtimeState.tracked ? "取消追踪" : "追踪委托";
        }

        return string.Empty;
    }

    public bool ExecutePrimaryMerchantQuestAction(string merchantId)
    {
        Quest quest = GetPrimaryMerchantQuest(merchantId, out QuestRuntimeState runtimeState);
        if (quest == null)
        {
            return false;
        }

        if (CanClaimQuest(quest.QuestId, merchantId))
        {
            return TryClaimQuest(quest.QuestId, merchantId);
        }

        if (CanStartQuest(quest.QuestId))
        {
            return StartQuest(quest.QuestId, merchantId);
        }

        if (runtimeState != null && !runtimeState.rewardsClaimed && runtimeState.status == QuestStatus.InProgress)
        {
            return ToggleTracked(quest.QuestId);
        }

        return false;
    }

    private string BuildObjectiveLine(Quest quest, QuestObjective objective, QuestRuntimeState runtimeState, int objectiveIndex)
    {
        if (objective == null)
        {
            return string.Empty;
        }

        int current = objective.GetCurrentProgress(this, runtimeState, objectiveIndex);
        int required = objective.RequiredProgress;
        bool completed = current >= required;
        string prefix = completed ? "[x]" : "[ ]";
        string progressSuffix = required > 1
            || objective is DeliverObjective
            || objective is KillObjective
            || objective is CollectObjective
            || objective is CustomEventObjective
            ? $" ({Mathf.Clamp(current, 0, required)}/{required})"
            : string.Empty;
        return $"{prefix} {objective.Description}{progressSuffix}";
    }

    private void ResolveReferences()
    {
        if (menuController == null)
        {
            menuController = FindFirstObjectByType<PrototypeMainMenuController>();
        }

        if (itemCatalog == null && menuController != null)
        {
            itemCatalog = menuController.ItemCatalog;
        }

        if (itemCatalog == null)
        {
            itemCatalog = Resources.Load<PrototypeItemCatalog>("PrototypeItemCatalog");
        }

        if (playerInteractor == null)
        {
            playerInteractor = FindFirstObjectByType<PlayerInteractor>();
        }

        if (trackerHud == null)
        {
            trackerHud = QuestTrackerHUD.Instance != null ? QuestTrackerHUD.Instance : FindFirstObjectByType<QuestTrackerHUD>();
        }
    }

    private WorldStateData ResolveWorldState()
    {
        if (menuController != null)
        {
            return menuController.ProfileWorldState;
        }

        standaloneProfile ??= autoLoadStandaloneProfile && itemCatalog != null
            ? PrototypeProfileService.LoadProfile(itemCatalog) ?? PrototypeProfileService.CreateDefaultProfile(itemCatalog)
            : null;
        if (standaloneProfile != null)
        {
            standaloneProfile.worldState ??= new WorldStateData();
            return standaloneProfile.worldState;
        }

        return null;
    }

    private void EnsureWorldState(WorldStateData worldState)
    {
        if (worldState == null)
        {
            return;
        }

        worldState.unlockedRaidMerchantIds ??= new List<string>();
        worldState.unlockedRaidNpcIds ??= new List<string>();
        worldState.questChainStages ??= new List<WorldStateData.QuestChainStageRecord>();
        worldState.storyFlags ??= new List<string>();
        worldState.merchantProgress ??= new List<MerchantData>();
        worldState.baseFacilities ??= new List<FacilityData>();
        worldState.questStates ??= new List<QuestRuntimeState>();
    }

    private void BuildQuestCatalog()
    {
        questLookup.Clear();
        List<Quest> quests = PrototypeQuestCatalog.CreateDefaultQuests(itemCatalog);
        for (int index = 0; index < quests.Count; index++)
        {
            Quest quest = quests[index];
            if (quest == null)
            {
                continue;
            }

            quest.Sanitize();
            if (string.IsNullOrWhiteSpace(quest.QuestId) || questLookup.ContainsKey(quest.QuestId))
            {
                continue;
            }

            questLookup.Add(quest.QuestId, quest);
        }
    }

    private void RebuildStateLookup()
    {
        stateLookup.Clear();
        if (activeWorldState == null)
        {
            return;
        }

        activeWorldState.questStates ??= new List<QuestRuntimeState>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = activeWorldState.questStates.Count - 1; index >= 0; index--)
        {
            QuestRuntimeState runtimeState = activeWorldState.questStates[index];
            if (runtimeState == null)
            {
                activeWorldState.questStates.RemoveAt(index);
                continue;
            }

            string questId = runtimeState.QuestId;
            if (string.IsNullOrWhiteSpace(questId) || !questLookup.TryGetValue(questId, out Quest quest) || !seenIds.Add(questId))
            {
                activeWorldState.questStates.RemoveAt(index);
                continue;
            }

            runtimeState.Sanitize(quest.QuestId, quest.Objectives.Count);
            stateLookup.Add(quest.QuestId, runtimeState);
        }
    }

    private QuestRuntimeState GetOrCreateState(Quest quest)
    {
        if (quest == null || activeWorldState == null)
        {
            return null;
        }

        if (stateLookup.TryGetValue(quest.QuestId, out QuestRuntimeState existing))
        {
            existing.Sanitize(quest.QuestId, quest.Objectives.Count);
            return existing;
        }

        activeWorldState.questStates ??= new List<QuestRuntimeState>();
        var created = new QuestRuntimeState
        {
            questId = quest.QuestId,
            status = QuestStatus.NotStarted,
            tracked = quest.autoTrack
        };
        created.Sanitize(quest.QuestId, quest.Objectives.Count);
        activeWorldState.questStates.Add(created);
        stateLookup[quest.QuestId] = created;
        return created;
    }
    private void HandleQuestEvent(QuestEventRecord record)
    {
        if (!TryInitialize())
        {
            return;
        }

        ApplyEventInternal(record, true);
    }

    private bool ApplyEventInternal(QuestEventRecord record, bool showProgressFeedback)
    {
        if (!TryInitialize())
        {
            return false;
        }

        bool anyChanged = false;
        var changedQuests = new List<Quest>();
        foreach (Quest quest in questLookup.Values)
        {
            if (quest == null)
            {
                continue;
            }

            QuestRuntimeState runtimeState = GetQuestState(quest.QuestId);
            if (runtimeState == null || runtimeState.rewardsClaimed || runtimeState.status != QuestStatus.InProgress)
            {
                continue;
            }

            bool questChanged = false;
            bool objectiveCompleted = false;
            for (int objectiveIndex = 0; objectiveIndex < quest.Objectives.Count; objectiveIndex++)
            {
                QuestObjective objective = quest.Objectives[objectiveIndex];
                if (objective == null)
                {
                    continue;
                }

                int previousProgress = runtimeState.GetProgress(objectiveIndex);
                if (!objective.TryApplyEvent(this, runtimeState, objectiveIndex, record, out int newProgress))
                {
                    continue;
                }

                questChanged = true;
                if (previousProgress < objective.RequiredProgress && newProgress >= objective.RequiredProgress)
                {
                    objectiveCompleted = true;
                }
            }

            if (!questChanged)
            {
                continue;
            }

            bool completionChanged = RefreshQuestCompletion(quest, runtimeState, true);
            if (!changedQuests.Contains(quest))
            {
                changedQuests.Add(quest);
            }

            if (showProgressFeedback && objectiveCompleted && runtimeState.status != QuestStatus.Completed)
            {
                ShowFeedback($"目标完成：{quest.QuestName}", true);
            }

            anyChanged |= questChanged || completionChanged;
        }

        if (!anyChanged)
        {
            return false;
        }

        SaveRuntimeState();
        for (int index = 0; index < changedQuests.Count; index++)
        {
            Quest quest = changedQuests[index];
            QuestRuntimeState runtimeState = GetQuestState(quest.QuestId);
            if (quest != null && runtimeState != null)
            {
                RaiseQuestChanged(quest, runtimeState);
            }
        }

        return true;
    }

    private void RefreshInventoryDrivenQuestCompletion()
    {
        if (!TryInitialize())
        {
            return;
        }

        bool anyChanged = false;
        var changedQuests = new List<Quest>();
        foreach (Quest quest in questLookup.Values)
        {
            if (quest == null)
            {
                continue;
            }

            QuestRuntimeState runtimeState = GetQuestState(quest.QuestId);
            if (runtimeState == null || runtimeState.rewardsClaimed || runtimeState.status == QuestStatus.NotStarted)
            {
                continue;
            }

            bool hasInventoryDrivenObjective = false;
            for (int index = 0; index < quest.Objectives.Count; index++)
            {
                if (quest.Objectives[index] is DeliverObjective)
                {
                    hasInventoryDrivenObjective = true;
                    break;
                }
            }

            if (!hasInventoryDrivenObjective)
            {
                continue;
            }

            if (!RefreshQuestCompletion(quest, runtimeState, true))
            {
                continue;
            }

            anyChanged = true;
            changedQuests.Add(quest);
        }

        if (!anyChanged)
        {
            return;
        }

        SaveRuntimeState();
        for (int index = 0; index < changedQuests.Count; index++)
        {
            Quest quest = changedQuests[index];
            QuestRuntimeState runtimeState = GetQuestState(quest.QuestId);
            if (quest != null && runtimeState != null)
            {
                RaiseQuestChanged(quest, runtimeState);
            }
        }
    }

    private bool RefreshQuestCompletion(Quest quest, QuestRuntimeState runtimeState, bool emitFeedback)
    {
        if (quest == null || runtimeState == null)
        {
            return false;
        }

        runtimeState.Sanitize(quest.QuestId, quest.Objectives.Count);
        if (runtimeState.rewardsClaimed)
        {
            return false;
        }

        bool changed = false;
        bool allCompleted = quest.Objectives.Count > 0;
        for (int objectiveIndex = 0; objectiveIndex < quest.Objectives.Count; objectiveIndex++)
        {
            QuestObjective objective = quest.Objectives[objectiveIndex];
            if (objective == null)
            {
                continue;
            }

            int progress = Mathf.Clamp(objective.GetCurrentProgress(this, runtimeState, objectiveIndex), 0, objective.RequiredProgress);
            if (runtimeState.SetProgress(objectiveIndex, progress))
            {
                changed = true;
            }

            if (progress < objective.RequiredProgress)
            {
                allCompleted = false;
            }
        }

        QuestStatus previousStatus = runtimeState.status;
        QuestStatus nextStatus = previousStatus;
        if (previousStatus == QuestStatus.NotStarted)
        {
            nextStatus = QuestStatus.NotStarted;
        }
        else if (allCompleted)
        {
            nextStatus = QuestStatus.Completed;
        }
        else if (previousStatus != QuestStatus.Failed)
        {
            nextStatus = QuestStatus.InProgress;
        }

        if (runtimeState.status != nextStatus)
        {
            runtimeState.status = nextStatus;
            changed = true;
            if (emitFeedback && previousStatus != QuestStatus.Completed && nextStatus == QuestStatus.Completed)
            {
                ShowFeedback($"任务完成：{quest.QuestName}，返回 {ResolveNpcDisplayName(quest.TurnInNpcId)} 提交。", true);
            }
        }

        return changed;
    }

    private bool IsQuestTurnedIn(string questId)
    {
        QuestRuntimeState runtimeState = GetQuestState(questId);
        return runtimeState != null && runtimeState.IsTurnedIn;
    }

    private bool HasStoryFlag(string storyFlag)
    {
        if (activeWorldState == null || activeWorldState.storyFlags == null || string.IsNullOrWhiteSpace(storyFlag))
        {
            return false;
        }

        string sanitizedFlag = storyFlag.Trim();
        for (int index = 0; index < activeWorldState.storyFlags.Count; index++)
        {
            if (string.Equals(activeWorldState.storyFlags[index], sanitizedFlag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void GrantFunds(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (menuController != null && menuController.TryAddFunds(amount))
        {
            return;
        }

        if (standaloneProfile == null)
        {
            standaloneProfile = itemCatalog != null
                ? PrototypeProfileService.LoadProfile(itemCatalog) ?? PrototypeProfileService.CreateDefaultProfile(itemCatalog)
                : null;
        }

        if (standaloneProfile != null)
        {
            long total = (long)Mathf.Max(0, standaloneProfile.funds) + amount;
            standaloneProfile.funds = total > int.MaxValue ? int.MaxValue : (int)total;
        }
    }

    private void GrantExperience(int amount, out int levelsGained)
    {
        levelsGained = 0;
        if (amount <= 0)
        {
            return;
        }

        PlayerProgressionData progression = menuController != null && menuController.profile != null
            ? menuController.profile.progression
            : standaloneProfile != null ? standaloneProfile.progression : null;
        if (progression == null)
        {
            return;
        }

        PrototypePlayerProgressionUtility.AddExperience(progression, amount, out levelsGained);
        menuController?.RequestUiRefresh();
    }

    private bool TryGrantRewardItem(string definitionId, int quantity, out string itemSummary)
    {
        itemSummary = string.Empty;
        if (itemCatalog == null || string.IsNullOrWhiteSpace(definitionId) || quantity <= 0)
        {
            return false;
        }

        string sanitizedId = definitionId.Trim();
        PrototypeWeaponDefinition weaponDefinition = itemCatalog.FindWeaponById(sanitizedId);
        if (weaponDefinition != null)
        {
            int granted = 0;
            for (int index = 0; index < quantity; index++)
            {
                ItemInstance rewardWeapon = ItemInstance.Create(
                    weaponDefinition,
                    weaponDefinition.IsMeleeWeapon ? 0 : weaponDefinition.MagazineSize,
                    1f,
                    null,
                    ItemRarity.Common,
                    null,
                    false,
                    null,
                    false);
                if (!TryGrantWeaponInstance(rewardWeapon))
                {
                    break;
                }

                granted++;
            }

            if (granted <= 0)
            {
                return false;
            }

            itemSummary = $"{weaponDefinition.DisplayNameWithLevel} x{granted}";
            return true;
        }

        ItemDefinition itemDefinition = itemCatalog.FindByItemId(sanitizedId);
        if (itemDefinition == null)
        {
            return false;
        }

        if (itemDefinition is ArmorDefinition armorDefinition)
        {
            int granted = 0;
            for (int index = 0; index < quantity; index++)
            {
                ItemInstance armorInstance = ItemInstance.Create(armorDefinition, armorDefinition.MaxDurability, null, ItemRarity.Common, null, false, null, false);
                if (!TryGrantInventoryInstance(armorInstance))
                {
                    break;
                }

                granted++;
            }

            if (granted <= 0)
            {
                return false;
            }

            itemSummary = $"{armorDefinition.DisplayNameWithLevel} x{granted}";
            return true;
        }

        int grantedQuantity = 0;
        if (menuController != null && menuController.StashInventory != null)
        {
            menuController.StashInventory.TryAddItem(itemDefinition, quantity, out grantedQuantity);
        }
        else if (standaloneProfile != null)
        {
            grantedQuantity = AddStandaloneItemRecords(itemDefinition, quantity);
        }

        if (grantedQuantity <= 0)
        {
            return false;
        }

        itemSummary = $"{itemDefinition.DisplayNameWithLevel} x{grantedQuantity}";
        return true;
    }

    private bool TryGrantWeaponInstance(ItemInstance weaponInstance)
    {
        if (weaponInstance == null || !weaponInstance.IsDefined() || !weaponInstance.IsWeapon)
        {
            return false;
        }

        if (menuController != null)
        {
            return menuController.TryAddWeaponToLocker(weaponInstance, $"武器柜空间不足，无法存放 {weaponInstance.DisplayName}。");
        }

        if (standaloneProfile == null)
        {
            return false;
        }

        standaloneProfile.stashWeaponInstances ??= new List<SavedItemInstanceDto>();
        SavedItemInstanceDto record = PrototypeProfileService.CaptureWeaponInstance(weaponInstance);
        if (record == null)
        {
            return false;
        }

        standaloneProfile.stashWeaponInstances.Add(record);
        return true;
    }

    private bool TryGrantInventoryInstance(ItemInstance itemInstance)
    {
        if (itemInstance == null || !itemInstance.IsDefined())
        {
            return false;
        }

        if (menuController != null && menuController.StashInventory != null)
        {
            return menuController.StashInventory.TryAddItemInstance(itemInstance);
        }

        if (standaloneProfile == null)
        {
            return false;
        }

        standaloneProfile.stashItemInstances ??= new List<SavedItemInstanceDto>();
        SavedItemInstanceDto record = PrototypeProfileService.CaptureItemInstance(itemInstance);
        if (record == null)
        {
            return false;
        }

        standaloneProfile.stashItemInstances.Add(record);
        return true;
    }

    private int AddStandaloneItemRecords(ItemDefinition definition, int quantity)
    {
        if (standaloneProfile == null || definition == null || quantity <= 0)
        {
            return 0;
        }

        standaloneProfile.stashItemInstances ??= new List<SavedItemInstanceDto>();
        int remaining = quantity;
        int granted = 0;
        int maxStack = Mathf.Max(1, definition.MaxStackSize);
        while (remaining > 0)
        {
            int stackQuantity = Mathf.Min(maxStack, remaining);
            ItemInstance instance = ItemInstance.Create(definition, stackQuantity, null, ItemRarity.Common, null, false, null, false);
            SavedItemInstanceDto record = PrototypeProfileService.CaptureItemInstance(instance);
            if (record == null)
            {
                break;
            }

            standaloneProfile.stashItemInstances.Add(record);
            granted += stackQuantity;
            remaining -= stackQuantity;
        }

        return granted;
    }

    private static void ApplyUnlockList(List<string> destination, List<string> source)
    {
        if (destination == null || source == null)
        {
            return;
        }

        for (int index = 0; index < source.Count; index++)
        {
            string value = string.IsNullOrWhiteSpace(source[index]) ? string.Empty : source[index].Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            bool exists = false;
            for (int existingIndex = 0; existingIndex < destination.Count; existingIndex++)
            {
                if (string.Equals(destination[existingIndex], value, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                destination.Add(value);
            }
        }
    }

    private void SaveRuntimeState()
    {
        if (menuController != null)
        {
            menuController.SaveProfileFromContainers();
            return;
        }

        if (standaloneProfile != null && itemCatalog != null)
        {
            PrototypeProfileService.SaveProfile(standaloneProfile, itemCatalog);
        }
    }

    private void RaiseQuestChanged(Quest quest, QuestRuntimeState runtimeState)
    {
        QuestChanged?.Invoke(quest, runtimeState);
        trackerHud?.RefreshImmediate();
    }

    private void ShowFeedback(string message, bool positive)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        trackerHud?.ShowToast(message, positive);
        if (menuController != null)
        {
            menuController.SetFeedback(message);
            return;
        }

        if (positive)
        {
            Debug.Log($"[QuestManager] {message}");
        }
        else
        {
            Debug.LogWarning($"[QuestManager] {message}");
        }
    }

    private Quest GetPrimaryMerchantQuest(string merchantId, out QuestRuntimeState runtimeState)
    {
        runtimeState = null;
        if (!TryInitialize() || string.IsNullOrWhiteSpace(merchantId))
        {
            return null;
        }

        string sanitizedMerchantId = merchantId.Trim();
        Quest bestQuest = null;
        int bestRank = int.MaxValue;

        foreach (Quest quest in questLookup.Values)
        {
            if (!(quest is MerchantQuest merchantQuest) || !string.Equals(merchantQuest.MerchantId, sanitizedMerchantId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            QuestRuntimeState state = GetQuestState(quest.QuestId);
            int rank;
            if (CanClaimQuest(quest.QuestId, sanitizedMerchantId))
            {
                rank = 0;
            }
            else if (CanStartQuest(quest.QuestId))
            {
                rank = 1;
            }
            else if (state != null && !state.rewardsClaimed && state.status == QuestStatus.InProgress)
            {
                rank = 2;
            }
            else if (state != null)
            {
                rank = 3;
            }
            else
            {
                rank = 4;
            }

            if (rank >= bestRank)
            {
                continue;
            }

            bestRank = rank;
            bestQuest = quest;
            runtimeState = state;
        }

        return bestQuest;
    }
    private static void SortQuestList(List<Quest> quests)
    {
        if (quests == null)
        {
            return;
        }

        quests.Sort(CompareQuests);
    }

    private static int CompareQuests(Quest left, Quest right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int typeComparison = GetQuestSortKey(left.type).CompareTo(GetQuestSortKey(right.type));
        if (typeComparison != 0)
        {
            return typeComparison;
        }

        return string.Compare(left.QuestName, right.QuestName, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetQuestSortKey(QuestType type)
    {
        switch (type)
        {
            case QuestType.Main:
                return 0;
            case QuestType.Side:
                return 1;
            case QuestType.Daily:
                return 2;
            case QuestType.Hidden:
                return 3;
            default:
                return 10;
        }
    }

    private static string BuildQuestTypeLabel(QuestType type)
    {
        switch (type)
        {
            case QuestType.Main:
                return "主线";
            case QuestType.Side:
                return "支线";
            case QuestType.Daily:
                return "委托";
            case QuestType.Hidden:
                return "隐藏";
            default:
                return type.ToString();
        }
    }

    private string ResolveDefinitionDisplayName(string definitionId)
    {
        if (itemCatalog == null || string.IsNullOrWhiteSpace(definitionId))
        {
            return string.IsNullOrWhiteSpace(definitionId) ? "物资" : definitionId.Trim();
        }

        ItemDefinitionBase definition = itemCatalog.FindDefinitionById(definitionId.Trim());
        return definition != null ? definition.DisplayNameWithLevel : definitionId.Trim();
    }

    private static string GetReputationLabel(ReputationLevel reputationLevel)
    {
        switch (reputationLevel)
        {
            case ReputationLevel.Friendly:
                return "友好";
            case ReputationLevel.Honored:
                return "尊敬";
            case ReputationLevel.Revered:
                return "崇敬";
            default:
                return "中立";
        }
    }

    private static int CountDefinitionInInventory(InventoryContainer inventory, string definitionId)
    {
        if (inventory == null || inventory.Items == null || string.IsNullOrWhiteSpace(definitionId))
        {
            return 0;
        }

        int total = 0;
        string sanitizedId = definitionId.Trim();
        for (int index = 0; index < inventory.Items.Count; index++)
        {
            ItemInstance item = inventory.Items[index];
            if (item == null || !item.IsDefined() || item.IsWeapon || item.Definition == null)
            {
                continue;
            }

            if (string.Equals(item.Definition.ItemId, sanitizedId, StringComparison.OrdinalIgnoreCase))
            {
                total += Mathf.Max(1, item.Quantity);
            }
        }

        return total;
    }

    private static int CountDefinitionInWeapons(IReadOnlyList<ItemInstance> weapons, string weaponId)
    {
        if (weapons == null || string.IsNullOrWhiteSpace(weaponId))
        {
            return 0;
        }

        int total = 0;
        string sanitizedId = weaponId.Trim();
        for (int index = 0; index < weapons.Count; index++)
        {
            ItemInstance weapon = weapons[index];
            if (weapon == null || !weapon.IsDefined() || !weapon.IsWeapon || weapon.WeaponDefinition == null)
            {
                continue;
            }

            if (string.Equals(weapon.WeaponDefinition.WeaponId, sanitizedId, StringComparison.OrdinalIgnoreCase))
            {
                total += 1;
            }
        }

        return total;
    }

    private static bool TryConsumeInventoryDefinition(InventoryContainer inventory, string definitionId, int quantity)
    {
        if (inventory == null || string.IsNullOrWhiteSpace(definitionId) || quantity <= 0)
        {
            return false;
        }

        string sanitizedId = definitionId.Trim();
        if (CountDefinitionInInventory(inventory, sanitizedId) < quantity)
        {
            return false;
        }

        int remaining = quantity;
        for (int index = inventory.Items.Count - 1; index >= 0 && remaining > 0; index--)
        {
            ItemInstance item = inventory.Items[index];
            if (item == null || !item.IsDefined() || item.IsWeapon || item.Definition == null)
            {
                continue;
            }

            if (!string.Equals(item.Definition.ItemId, sanitizedId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int extractQuantity = Mathf.Min(remaining, Mathf.Max(1, item.Quantity));
            if (extractQuantity > 0 && inventory.TryExtractItem(index, extractQuantity, out _))
            {
                remaining -= extractQuantity;
            }
        }

        return remaining <= 0;
    }

    private static bool TryConsumeWeaponLocker(List<ItemInstance> weapons, string weaponId, int quantity)
    {
        if (weapons == null || string.IsNullOrWhiteSpace(weaponId) || quantity <= 0)
        {
            return false;
        }

        string sanitizedId = weaponId.Trim();
        if (CountDefinitionInWeapons(weapons, sanitizedId) < quantity)
        {
            return false;
        }

        int remaining = quantity;
        for (int index = weapons.Count - 1; index >= 0 && remaining > 0; index--)
        {
            ItemInstance weapon = weapons[index];
            if (weapon == null || !weapon.IsDefined() || !weapon.IsWeapon || weapon.WeaponDefinition == null)
            {
                continue;
            }

            if (!string.Equals(weapon.WeaponDefinition.WeaponId, sanitizedId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            weapons.RemoveAt(index);
            remaining--;
        }

        return remaining <= 0;
    }
    private static int CountDefinitionInRecords(List<SavedItemInstanceDto> records, string itemId)
    {
        if (records == null || string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        int total = 0;
        string sanitizedId = itemId.Trim();
        for (int index = 0; index < records.Count; index++)
        {
            SavedItemInstanceDto record = records[index];
            if (record == null || !string.IsNullOrWhiteSpace(record.weaponId))
            {
                continue;
            }

            if (string.Equals(record.itemId, sanitizedId, StringComparison.OrdinalIgnoreCase))
            {
                total += Mathf.Max(1, record.quantity);
            }
        }

        return total;
    }

    private static int CountWeaponInRecords(List<SavedItemInstanceDto> records, string weaponId)
    {
        if (records == null || string.IsNullOrWhiteSpace(weaponId))
        {
            return 0;
        }

        int total = 0;
        string sanitizedId = weaponId.Trim();
        for (int index = 0; index < records.Count; index++)
        {
            SavedItemInstanceDto record = records[index];
            if (record == null)
            {
                continue;
            }

            if (string.Equals(record.weaponId, sanitizedId, StringComparison.OrdinalIgnoreCase))
            {
                total += 1;
            }
        }

        return total;
    }

    private static bool TryConsumeItemRecords(List<SavedItemInstanceDto> records, string itemId, int quantity)
    {
        if (records == null || string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return false;
        }

        string sanitizedId = itemId.Trim();
        if (CountDefinitionInRecords(records, sanitizedId) < quantity)
        {
            return false;
        }

        int remaining = quantity;
        for (int index = records.Count - 1; index >= 0 && remaining > 0; index--)
        {
            SavedItemInstanceDto record = records[index];
            if (record == null || !string.IsNullOrWhiteSpace(record.weaponId) || !string.Equals(record.itemId, sanitizedId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int available = Mathf.Max(1, record.quantity);
            int consume = Mathf.Min(remaining, available);
            if (consume >= available)
            {
                records.RemoveAt(index);
            }
            else
            {
                record.quantity = available - consume;
            }

            remaining -= consume;
        }

        return remaining <= 0;
    }

    private static bool TryConsumeWeaponRecords(List<SavedItemInstanceDto> records, string weaponId, int quantity)
    {
        if (records == null || string.IsNullOrWhiteSpace(weaponId) || quantity <= 0)
        {
            return false;
        }

        string sanitizedId = weaponId.Trim();
        if (CountWeaponInRecords(records, sanitizedId) < quantity)
        {
            return false;
        }

        int remaining = quantity;
        for (int index = records.Count - 1; index >= 0 && remaining > 0; index--)
        {
            SavedItemInstanceDto record = records[index];
            if (record == null || !string.Equals(record.weaponId, sanitizedId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            records.RemoveAt(index);
            remaining--;
        }

        return remaining <= 0;
    }

    private static string ResolveNpcDisplayName(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            return "任务发布人";
        }

        switch (npcId.Trim().ToLowerInvariant())
        {
            case "commander":
                return "指挥官";
            case "intel_officer":
                return "情报官";
            case "trainer":
                return "训练官";
            case "weapons_trader":
                return "武器商人";
            case "medical_trader":
                return "医疗商人";
            case "armor_trader":
                return "护甲商人";
            case "general_trader":
                return "杂货商人";
            default:
                return npcId.Trim();
        }
    }
}
