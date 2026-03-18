using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-40)]
[DisallowMultipleComponent]
public sealed class QuestTrackerHUD : MonoBehaviour
{
    private const string TrackerPrefabResourcePath = "UI/Quest/QuestTrackerHud";
    private const string ToastPrefabResourcePath = "UI/Quest/QuestToast";
    private const string JournalPrefabResourcePath = "UI/Quest/QuestJournal";

    private static QuestTrackerHUD instance;

    private QuestManager manager;
    private PlayerInteractor playerInteractor;
    private PlayerInteractionState interactionState;
    private Font runtimeFont;
    private QuestListUI listView;
    private QuestDetailUI detailView;

    private RectTransform trackerRoot;
    private Text trackerText;
    private Button journalButton;
    private QuestTrackerViewTemplate trackerView;
    private RectTransform toastRoot;
    private Text toastText;
    private CanvasGroup toastCanvasGroup;
    private QuestToastViewTemplate toastView;
    private float toastUntil;

    private PrototypeUiToolkit.WindowChrome journalWindow;
    private RectTransform journalListContent;
    private RectTransform journalDetailContent;
    private QuestJournalViewTemplate journalView;
    private bool journalOpen;
    private string selectedQuestId = string.Empty;
    private string lastTrackerSummary = string.Empty;
    private string lastJournalStateKey = string.Empty;

    public static QuestTrackerHUD Instance => instance;

    public static QuestTrackerHUD GetOrCreate()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<QuestTrackerHUD>();
        if (instance != null)
        {
            return instance;
        }

        GameObject hudObject = new GameObject("QuestTrackerHUD");
        return hudObject.AddComponent<QuestTrackerHUD>();
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

    private void Update()
    {
        UpdateToast();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (trackerRoot != null)
        {
            Destroy(trackerRoot.gameObject);
        }

        if (toastRoot != null)
        {
            Destroy(toastRoot.gameObject);
        }

        if (journalWindow != null && journalWindow.Root != null)
        {
            Destroy(journalWindow.Root.gameObject);
        }
    }

    public void Bind(QuestManager questManager, PlayerInteractor interactor = null)
    {
        manager = questManager;
        lastTrackerSummary = string.Empty;
        lastJournalStateKey = string.Empty;
        if (interactor != null)
        {
            playerInteractor = interactor;
        }

        ResolveReferences();
        EnsureTrackerUi();
        EnsureToastUi();
        listView ??= new QuestListUI(runtimeFont);
        detailView ??= new QuestDetailUI(runtimeFont);
    }

    public void RefreshImmediate()
    {
        ResolveReferences();
        EnsureTrackerUi();
        UpdateTrackerText();
        if (journalOpen)
        {
            RebuildJournal(false);
        }
    }

    public void ShowToast(string message, bool positive)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        ResolveReferences();
        EnsureToastUi();
        if (toastText != null)
        {
            toastText.text = message.Trim();
            toastText.color = positive ? new Color(0.88f, 1f, 0.88f, 1f) : new Color(1f, 0.83f, 0.83f, 1f);
        }

        if (toastRoot != null)
        {
            PrototypeUiToolkit.SetVisible(toastRoot, true);
        }

        if (toastCanvasGroup != null)
        {
            toastCanvasGroup.alpha = 1f;
        }

        toastUntil = Time.unscaledTime + 2.6f;
    }

    public void ToggleJournal()
    {
        SetJournalOpen(!journalOpen);
    }

    public void OpenJournal()
    {
        SetJournalOpen(true);
    }

    private void SetJournalOpen(bool open)
    {
        ResolveReferences();
        EnsureJournalUi();
        journalOpen = open;
        if (journalWindow != null && journalWindow.Root != null)
        {
            PrototypeUiToolkit.SetVisible(journalWindow.Root, open);
        }

        if (interactionState != null)
        {
            interactionState.SetUiFocused(this, open);
        }

        bool keepCursorFree = open || (interactionState != null && interactionState.IsUiFocused);
        Cursor.lockState = keepCursorFree ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = keepCursorFree;

        if (open)
        {
            if (string.IsNullOrWhiteSpace(selectedQuestId) && manager != null)
            {
                Quest firstQuest = ResolveSelectedQuest();
                if (firstQuest == null)
                {
                    var quests = manager.GetAllQuests();
                    if (quests.Count > 0)
                    {
                        selectedQuestId = quests[0].QuestId;
                    }
                }
            }

            RebuildJournal(true);
        }
    }

    private void ResolveReferences()
    {
        if (runtimeFont == null)
        {
            runtimeFont = PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont;
        }

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

    private void EnsureTrackerUi()
    {
        if (trackerRoot != null)
        {
            return;
        }

        RectTransform hudLayer = PrototypeRuntimeUiManager.GetOrCreate().GetLayerRoot(PrototypeUiLayer.Hud);
        if (TryInstantiateTrackerPrefab(hudLayer))
        {
            UpdateTrackerText();
            return;
        }

        trackerRoot = PrototypeUiToolkit.CreatePanel(
            hudLayer,
            "QuestTracker",
            new Color(0.08f, 0.1f, 0.14f, 0.88f),
            new RectOffset(12, 12, 10, 10),
            8f);
        PrototypeUiToolkit.SetAnchor(trackerRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(420f, 180f));

        trackerText = PrototypeUiToolkit.CreateText(
            trackerRoot,
            runtimeFont,
            string.Empty,
            14,
            FontStyle.Normal,
            Color.white,
            TextAnchor.UpperLeft);

        journalButton = PrototypeUiToolkit.CreateButton(
            trackerRoot,
            runtimeFont,
            "任务日志",
            ToggleJournal,
            new Color(0.21f, 0.34f, 0.48f, 0.98f),
            new Color(0.29f, 0.46f, 0.64f, 1f),
            new Color(0.16f, 0.26f, 0.38f, 1f),
            34f);

        UpdateTrackerText();
    }

    private void EnsureToastUi()
    {
        if (toastRoot != null)
        {
            return;
        }

        RectTransform overlayLayer = PrototypeRuntimeUiManager.GetOrCreate().GetLayerRoot(PrototypeUiLayer.Overlay);
        if (TryInstantiateToastPrefab(overlayLayer))
        {
            PrototypeUiToolkit.SetVisible(toastRoot, false);
            if (toastCanvasGroup != null)
            {
                toastCanvasGroup.alpha = 0f;
            }

            return;
        }

        toastRoot = PrototypeUiToolkit.CreatePanel(
            overlayLayer,
            "QuestToast",
            new Color(0.08f, 0.1f, 0.14f, 0.94f),
            new RectOffset(14, 14, 10, 10),
            0f);
        PrototypeUiToolkit.SetAnchor(toastRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(520f, 52f));
        toastCanvasGroup = PrototypeUiToolkit.EnsureCanvasGroup(toastRoot);
        toastText = PrototypeUiToolkit.CreateText(toastRoot, runtimeFont, string.Empty, 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        PrototypeUiToolkit.SetVisible(toastRoot, false);
        toastCanvasGroup.alpha = 0f;
    }

    private void EnsureJournalUi()
    {
        if (journalWindow != null && journalWindow.Root != null)
        {
            return;
        }

        RectTransform modalLayer = PrototypeRuntimeUiManager.GetOrCreate().GetLayerRoot(PrototypeUiLayer.Modal);
        if (TryInstantiateJournalPrefab(modalLayer))
        {
            PrototypeUiToolkit.SetVisible(journalWindow.Root, false);
            return;
        }

        journalWindow = PrototypeUiToolkit.CreateWindowChrome(modalLayer, runtimeFont, "QuestJournal", "任务日志", "查看可接任务、进行中任务与奖励。", new Vector2(980f, 680f));

        RectTransform bodyRoot = journalWindow.BodyRoot;
        HorizontalLayoutGroup bodyLayout = bodyRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 16f;
        bodyLayout.childAlignment = TextAnchor.UpperLeft;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = true;

        RectTransform listPanel = PrototypeUiToolkit.CreatePanel(bodyRoot, "QuestListPanel", new Color(0.12f, 0.15f, 0.2f, 0.92f), new RectOffset(12, 12, 12, 12), 10f);
        LayoutElement listLayout = listPanel.gameObject.AddComponent<LayoutElement>();
        listLayout.preferredWidth = 300f;
        listLayout.flexibleHeight = 1f;
        PrototypeUiToolkit.CreateText(listPanel, runtimeFont, "任务列表", 18, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        ScrollRect listScroll = PrototypeUiToolkit.CreateScrollView(listPanel, out _, out journalListContent, true);
        VerticalLayoutGroup listContentLayout = EnsureVerticalLayoutGroup(journalListContent);
        if (listContentLayout != null)
        {
            listContentLayout.spacing = 8f;
            listContentLayout.childAlignment = TextAnchor.UpperLeft;
            listContentLayout.childControlWidth = true;
            listContentLayout.childControlHeight = true;
            listContentLayout.childForceExpandWidth = true;
            listContentLayout.childForceExpandHeight = false;
        }

        listScroll.verticalNormalizedPosition = 1f;

        RectTransform detailPanel = PrototypeUiToolkit.CreatePanel(bodyRoot, "QuestDetailPanel", new Color(0.11f, 0.14f, 0.18f, 0.92f), new RectOffset(14, 14, 14, 14), 10f);
        LayoutElement detailLayout = detailPanel.gameObject.AddComponent<LayoutElement>();
        detailLayout.flexibleWidth = 1f;
        detailLayout.flexibleHeight = 1f;
        PrototypeUiToolkit.CreateText(detailPanel, runtimeFont, "任务详情", 18, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        ScrollRect detailScroll = PrototypeUiToolkit.CreateScrollView(detailPanel, out _, out journalDetailContent, true);
        VerticalLayoutGroup detailContentLayout = EnsureVerticalLayoutGroup(journalDetailContent);
        if (detailContentLayout != null)
        {
            detailContentLayout.spacing = 8f;
            detailContentLayout.childAlignment = TextAnchor.UpperLeft;
            detailContentLayout.childControlWidth = true;
            detailContentLayout.childControlHeight = true;
            detailContentLayout.childForceExpandWidth = true;
            detailContentLayout.childForceExpandHeight = false;
        }

        detailScroll.verticalNormalizedPosition = 1f;

        PrototypeUiToolkit.CreateButton(
            journalWindow.FooterRoot,
            runtimeFont,
            "关闭",
            () => SetJournalOpen(false),
            new Color(0.2f, 0.27f, 0.36f, 0.98f),
            new Color(0.29f, 0.38f, 0.49f, 1f),
            new Color(0.16f, 0.22f, 0.3f, 1f),
            38f);

        PrototypeUiToolkit.SetVisible(journalWindow.Root, false);
    }

    private void UpdateTrackerText()
    {
        if (trackerText == null)
        {
            return;
        }

        string trackerSummary = manager != null ? manager.BuildTrackerText() : string.Empty;
        if (string.IsNullOrWhiteSpace(trackerSummary))
        {
            trackerSummary = "\u6682\u65e0\u8ffd\u8e2a\u4efb\u52a1\u3002";
        }

        if (string.Equals(lastTrackerSummary, trackerSummary, StringComparison.Ordinal))
        {
            return;
        }

        lastTrackerSummary = trackerSummary;
        trackerText.text = string.IsNullOrWhiteSpace(trackerSummary)
            ? "暂无追踪任务。"
            : trackerSummary;
    }

    private void RebuildJournal(bool force)
    {
        if (manager == null)
        {
            return;
        }

        EnsureJournalUi();
        listView ??= new QuestListUI(runtimeFont);
        detailView ??= new QuestDetailUI(runtimeFont);

        var quests = manager.GetAllQuests();
        Quest selectedQuest = ResolveSelectedQuest();
        if (selectedQuest == null && quests.Count > 0)
        {
            selectedQuest = quests[0];
            selectedQuestId = selectedQuest.QuestId;
        }

        string journalStateKey = BuildJournalStateKey(quests, selectedQuest);
        if (!force && string.Equals(lastJournalStateKey, journalStateKey, StringComparison.Ordinal))
        {
            return;
        }

        lastJournalStateKey = journalStateKey;
        listView.Rebuild(journalListContent, quests, selectedQuest, manager, quest =>
        {
            selectedQuestId = quest != null ? quest.QuestId : string.Empty;
            RebuildJournal(true);
        });
        detailView.Rebuild(journalDetailContent, manager, selectedQuest, () => RebuildJournal(true));
    }

    private Quest ResolveSelectedQuest()
    {
        return manager != null && !string.IsNullOrWhiteSpace(selectedQuestId)
            ? manager.GetQuest(selectedQuestId)
            : null;
    }

    private string BuildJournalStateKey(System.Collections.Generic.IReadOnlyList<Quest> quests, Quest selectedQuest)
    {
        StringBuilder builder = new StringBuilder(256);
        builder.Append(selectedQuest != null ? selectedQuest.QuestId : string.Empty);
        builder.Append('|');
        if (quests == null || manager == null)
        {
            return builder.ToString();
        }

        for (int questIndex = 0; questIndex < quests.Count; questIndex++)
        {
            Quest quest = quests[questIndex];
            if (quest == null)
            {
                continue;
            }

            QuestRuntimeState runtimeState = manager.GetQuestState(quest.QuestId);
            builder.Append(quest.QuestId);
            builder.Append(':');
            if (runtimeState != null)
            {
                builder.Append((int)runtimeState.status);
                builder.Append(':');
                builder.Append(runtimeState.rewardsClaimed ? '1' : '0');
                builder.Append(':');
                builder.Append(runtimeState.tracked ? '1' : '0');
                builder.Append(':');
                if (runtimeState.objectiveProgress != null)
                {
                    for (int index = 0; index < runtimeState.objectiveProgress.Count; index++)
                    {
                        builder.Append(runtimeState.objectiveProgress[index]);
                        builder.Append(',');
                    }
                }
            }

            builder.Append('|');
        }

        return builder.ToString();
    }

    private bool TryInstantiateTrackerPrefab(RectTransform parent)
    {
        GameObject prefabAsset = Resources.Load<GameObject>(TrackerPrefabResourcePath);
        if (prefabAsset == null)
        {
            return false;
        }

        GameObject instance = Instantiate(prefabAsset, parent, false);
        instance.name = prefabAsset.name;

        trackerView = instance.GetComponent<QuestTrackerViewTemplate>();
        if (trackerView == null || trackerView.Root == null)
        {
            Destroy(instance);
            trackerView = null;
            return false;
        }

        trackerRoot = trackerView.Root;
        trackerText = trackerView.TrackerText;
        journalButton = trackerView.JournalButton;
        PrototypeUiToolkit.ApplyFontRecursively(trackerRoot, runtimeFont);
        if (journalButton != null)
        {
            journalButton.onClick.RemoveAllListeners();
            journalButton.onClick.AddListener(ToggleJournal);
        }

        if (trackerText == null || journalButton == null)
        {
            Destroy(instance);
            trackerView = null;
            trackerRoot = null;
            trackerText = null;
            journalButton = null;
            return false;
        }

        return true;
    }

    private bool TryInstantiateToastPrefab(RectTransform parent)
    {
        GameObject prefabAsset = Resources.Load<GameObject>(ToastPrefabResourcePath);
        if (prefabAsset == null)
        {
            return false;
        }

        GameObject instance = Instantiate(prefabAsset, parent, false);
        instance.name = prefabAsset.name;

        toastView = instance.GetComponent<QuestToastViewTemplate>();
        if (toastView == null || toastView.Root == null)
        {
            Destroy(instance);
            toastView = null;
            return false;
        }

        toastRoot = toastView.Root;
        toastText = toastView.MessageText;
        toastCanvasGroup = toastView.CanvasGroup;
        PrototypeUiToolkit.ApplyFontRecursively(toastRoot, runtimeFont);
        if (toastText == null || toastCanvasGroup == null)
        {
            Destroy(instance);
            toastView = null;
            toastRoot = null;
            toastText = null;
            toastCanvasGroup = null;
            return false;
        }

        return true;
    }

    private bool TryInstantiateJournalPrefab(RectTransform parent)
    {
        GameObject prefabAsset = Resources.Load<GameObject>(JournalPrefabResourcePath);
        if (prefabAsset == null)
        {
            return false;
        }

        GameObject instance = Instantiate(prefabAsset, parent, false);
        instance.name = prefabAsset.name;

        journalView = instance.GetComponent<QuestJournalViewTemplate>();
        if (journalView == null || journalView.Root == null)
        {
            Destroy(instance);
            journalView = null;
            return false;
        }

        journalWindow = journalView.CreateWindowChrome();
        journalListContent = journalView.ListContentRoot;
        journalDetailContent = journalView.DetailContentRoot;
        PrototypeUiToolkit.ApplyFontRecursively(journalWindow.Root, runtimeFont);
        if (journalView.CloseButton != null)
        {
            journalView.CloseButton.onClick.RemoveAllListeners();
            journalView.CloseButton.onClick.AddListener(() => SetJournalOpen(false));
        }

        if (journalView.ListScrollRect != null)
        {
            journalView.ListScrollRect.verticalNormalizedPosition = 1f;
        }

        if (journalView.DetailScrollRect != null)
        {
            journalView.DetailScrollRect.verticalNormalizedPosition = 1f;
        }

        if (journalWindow == null
            || journalWindow.Root == null
            || journalListContent == null
            || journalDetailContent == null
            || journalView.CloseButton == null)
        {
            Destroy(instance);
            journalView = null;
            journalWindow = null;
            journalListContent = null;
            journalDetailContent = null;
            return false;
        }

        return true;
    }

    private void UpdateToast()
    {
        if (toastRoot == null || toastCanvasGroup == null)
        {
            return;
        }

        if (toastUntil > Time.unscaledTime)
        {
            return;
        }

        if (toastCanvasGroup.alpha > 0f)
        {
            toastCanvasGroup.alpha = Mathf.MoveTowards(toastCanvasGroup.alpha, 0f, Time.unscaledDeltaTime * 4f);
        }

        if (toastCanvasGroup.alpha <= 0.001f)
        {
            PrototypeUiToolkit.SetVisible(toastRoot, false);
        }
    }

    private static VerticalLayoutGroup EnsureVerticalLayoutGroup(RectTransform root)
    {
        if (root == null)
        {
            return null;
        }

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        return layout;
    }
}
