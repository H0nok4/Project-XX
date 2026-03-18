using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-40)]
[DisallowMultipleComponent]
public sealed class QuestTrackerHUD : MonoBehaviour
{
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
    private RectTransform toastRoot;
    private Text toastText;
    private CanvasGroup toastCanvasGroup;
    private float toastUntil;

    private PrototypeUiToolkit.WindowChrome journalWindow;
    private RectTransform journalListContent;
    private RectTransform journalDetailContent;
    private bool journalOpen;
    private string selectedQuestId = string.Empty;

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
            RebuildJournal();
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

            RebuildJournal();
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
        VerticalLayoutGroup listContentLayout = journalListContent.gameObject.AddComponent<VerticalLayoutGroup>();
        listContentLayout.spacing = 8f;
        listContentLayout.childAlignment = TextAnchor.UpperLeft;
        listContentLayout.childControlWidth = true;
        listContentLayout.childControlHeight = true;
        listContentLayout.childForceExpandWidth = true;
        listContentLayout.childForceExpandHeight = false;
        listScroll.verticalNormalizedPosition = 1f;

        RectTransform detailPanel = PrototypeUiToolkit.CreatePanel(bodyRoot, "QuestDetailPanel", new Color(0.11f, 0.14f, 0.18f, 0.92f), new RectOffset(14, 14, 14, 14), 10f);
        LayoutElement detailLayout = detailPanel.gameObject.AddComponent<LayoutElement>();
        detailLayout.flexibleWidth = 1f;
        detailLayout.flexibleHeight = 1f;
        PrototypeUiToolkit.CreateText(detailPanel, runtimeFont, "任务详情", 18, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        ScrollRect detailScroll = PrototypeUiToolkit.CreateScrollView(detailPanel, out _, out journalDetailContent, true);
        VerticalLayoutGroup detailContentLayout = journalDetailContent.gameObject.AddComponent<VerticalLayoutGroup>();
        detailContentLayout.spacing = 8f;
        detailContentLayout.childAlignment = TextAnchor.UpperLeft;
        detailContentLayout.childControlWidth = true;
        detailContentLayout.childControlHeight = true;
        detailContentLayout.childForceExpandWidth = true;
        detailContentLayout.childForceExpandHeight = false;
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
        trackerText.text = string.IsNullOrWhiteSpace(trackerSummary)
            ? "暂无追踪任务。"
            : trackerSummary;
    }

    private void RebuildJournal()
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

        listView.Rebuild(journalListContent, quests, selectedQuest, manager, quest =>
        {
            selectedQuestId = quest != null ? quest.QuestId : string.Empty;
            RebuildJournal();
        });
        detailView.Rebuild(journalDetailContent, manager, selectedQuest, RebuildJournal);
    }

    private Quest ResolveSelectedQuest()
    {
        return manager != null && !string.IsNullOrWhiteSpace(selectedQuestId)
            ? manager.GetQuest(selectedQuestId)
            : null;
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
