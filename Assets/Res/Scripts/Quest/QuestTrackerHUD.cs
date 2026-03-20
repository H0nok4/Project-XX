using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-40)]
[DisallowMultipleComponent]
public sealed class QuestTrackerHUD : ViewBase
{
    private const string TrackerPrefabResourcePath = "UI/Quest/QuestTrackerHud";
    private const string ToastPrefabResourcePath = "UI/Quest/QuestToast";
    private const string JournalPrefabResourcePath = "UI/Quest/QuestJournal";

    private static QuestTrackerHUD instance;

    private QuestManager manager;
    private PlayerInteractor playerInteractor;
    private PlayerInteractionState interactionState;
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
    protected override bool VisibleOnAwake => true;
    protected override bool AddTransparentRootGraphic => false;
    protected override bool RootGraphicRaycastTarget => false;
    protected override string ViewName => "QuestTrackerView";

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

    protected override void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolveReferences();
        base.Awake();
    }

    private void Update()
    {
        UpdateToast();
    }

    protected override void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (toastRoot != null)
        {
            Destroy(toastRoot.gameObject);
        }

        if (journalWindow != null && journalWindow.Root != null)
        {
            Destroy(journalWindow.Root.gameObject);
        }

        base.OnDestroy();
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
        listView ??= new QuestListUI(RuntimeFont);
        detailView ??= new QuestDetailUI(RuntimeFont);
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
        if (journalWindow == null || journalWindow.Root == null)
        {
            return;
        }

        journalOpen = open;
        PrototypeUiToolkit.SetVisible(journalWindow.Root, open);

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
        _ = RuntimeFont;

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
        EnsureView();
    }

    protected override void BuildView(RectTransform root)
    {
        if (root == null || trackerRoot != null)
        {
            return;
        }

        if (!TryInstantiateTrackerPrefab(root))
        {
            Debug.LogWarning($"[{GetType().Name}] Missing quest tracker prefab at Resources/{TrackerPrefabResourcePath}.", this);
            return;
        }

        UpdateTrackerText();
    }

    protected override void OnViewRootDestroyed()
    {
        trackerRoot = null;
        trackerText = null;
        journalButton = null;
        trackerView = null;
    }

    private void EnsureToastUi()
    {
        if (toastRoot != null)
        {
            return;
        }

        RectTransform overlayLayer = PrototypeRuntimeUiManager.GetOrCreate().GetLayerRoot(PrototypeUiLayer.Overlay);
        if (!TryInstantiateToastPrefab(overlayLayer))
        {
            Debug.LogWarning($"[{GetType().Name}] Missing quest toast prefab at Resources/{ToastPrefabResourcePath}.", this);
            return;
        }

        PrototypeUiToolkit.SetVisible(toastRoot, false);
        if (toastCanvasGroup != null)
        {
            toastCanvasGroup.alpha = 0f;
        }
    }

    private void EnsureJournalUi()
    {
        if (journalWindow != null && journalWindow.Root != null)
        {
            return;
        }

        RectTransform modalLayer = PrototypeRuntimeUiManager.GetOrCreate().GetLayerRoot(PrototypeUiLayer.Modal);
        if (!TryInstantiateJournalPrefab(modalLayer))
        {
            Debug.LogWarning($"[{GetType().Name}] Missing quest journal prefab at Resources/{JournalPrefabResourcePath}.", this);
            return;
        }

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
            trackerSummary = "暂无追踪任务。";
        }

        if (string.Equals(lastTrackerSummary, trackerSummary, StringComparison.Ordinal))
        {
            return;
        }

        lastTrackerSummary = trackerSummary;
        trackerText.text = trackerSummary;
    }

    private void RebuildJournal(bool force)
    {
        if (manager == null)
        {
            return;
        }

        EnsureJournalUi();
        if (journalWindow == null || journalWindow.Root == null || journalListContent == null || journalDetailContent == null)
        {
            return;
        }

        listView ??= new QuestListUI(RuntimeFont);
        detailView ??= new QuestDetailUI(RuntimeFont);

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

        GameObject instanceObject = Instantiate(prefabAsset, parent, false);
        instanceObject.name = prefabAsset.name;

        trackerView = instanceObject.GetComponent<QuestTrackerViewTemplate>();
        if (trackerView == null || trackerView.Root == null)
        {
            Destroy(instanceObject);
            trackerView = null;
            return false;
        }

        trackerRoot = trackerView.Root;
        trackerText = trackerView.TrackerText;
        journalButton = trackerView.JournalButton;
        PrototypeUiToolkit.ApplyFontRecursively(trackerRoot, RuntimeFont);

        if (journalButton != null)
        {
            journalButton.onClick.RemoveAllListeners();
            journalButton.onClick.AddListener(ToggleJournal);
        }

        if (trackerText == null || journalButton == null)
        {
            Destroy(instanceObject);
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

        GameObject instanceObject = Instantiate(prefabAsset, parent, false);
        instanceObject.name = prefabAsset.name;

        toastView = instanceObject.GetComponent<QuestToastViewTemplate>();
        if (toastView == null || toastView.Root == null)
        {
            Destroy(instanceObject);
            toastView = null;
            return false;
        }

        toastRoot = toastView.Root;
        toastText = toastView.MessageText;
        toastCanvasGroup = toastView.CanvasGroup;
        PrototypeUiToolkit.ApplyFontRecursively(toastRoot, RuntimeFont);

        if (toastText == null || toastCanvasGroup == null)
        {
            Destroy(instanceObject);
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

        GameObject instanceObject = Instantiate(prefabAsset, parent, false);
        instanceObject.name = prefabAsset.name;

        journalView = instanceObject.GetComponent<QuestJournalViewTemplate>();
        if (journalView == null || journalView.Root == null)
        {
            Destroy(instanceObject);
            journalView = null;
            return false;
        }

        journalWindow = journalView.CreateWindowChrome();
        journalListContent = journalView.ListContentRoot;
        journalDetailContent = journalView.DetailContentRoot;
        PrototypeUiToolkit.ApplyFontRecursively(journalWindow.Root, RuntimeFont);

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
            Destroy(instanceObject);
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
}
