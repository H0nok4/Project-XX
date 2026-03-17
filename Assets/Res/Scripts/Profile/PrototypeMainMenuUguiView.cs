using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PrototypeMainMenuUguiView : MonoBehaviour
{
    private sealed class PanelRefs
    {
        public RectTransform root;
        public RectTransform content;
        public RectTransform footer;
    }

    private readonly struct ButtonSpec
    {
        public ButtonSpec(string label, Action onClick)
        {
            Label = label;
            OnClick = onClick;
        }

        public string Label { get; }
        public Action OnClick { get; }
    }

    private const string ButtonTemplateResourcePath = "UI/MainMenu/PrototypeMainMenuButton";
    private const string CardTemplateResourcePath = "UI/MainMenu/PrototypeMainMenuCard";
    private const string PanelTemplateResourcePath = "UI/MainMenu/PrototypeMainMenuPanel";

    private PrototypeMainMenuController host;
    private MetaInventoryPresenter inventoryActions;
    private MetaMerchantPresenter merchantActions;
    private Font uiFont;

    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private RectTransform pageHost;
    [SerializeField] private RectTransform homePage;
    [SerializeField] private RectTransform warehousePage;
    [SerializeField] private RectTransform merchantPage;

    [SerializeField] private Text feedbackText;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button warehouseButton;
    [SerializeField] private Button merchantButton;
    [SerializeField] private Button saveProfileButton;
    [SerializeField] private Button resetProfileButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private PrototypeMainMenuButtonTemplate buttonTemplate;
    [SerializeField] private PrototypeMainMenuCardTemplate cardTemplate;
    [SerializeField] private PrototypeMainMenuPanelTemplate panelTemplate;

    private bool initialized;
    private bool refreshRequested = true;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private PrototypeMainMenuController.MenuPage lastPage;
    private bool lastVisible;

    public void Initialize(PrototypeMainMenuController controller)
    {
        if (controller == null)
        {
            return;
        }

        host = controller;
        inventoryActions ??= new MetaInventoryPresenter(host);
        merchantActions ??= new MetaMerchantPresenter(host);
        EnsureBuilt();
        RequestRefresh();
        SetViewVisible(host.IsUiVisible);
    }

    public void RequestRefresh()
    {
        refreshRequested = true;
    }

    public void SetViewVisible(bool visible)
    {
        if (canvasRoot == null)
        {
            return;
        }

        if (canvasRoot.gameObject.activeSelf != visible)
        {
            canvasRoot.gameObject.SetActive(visible);
        }

        lastVisible = visible;
    }

    private void Update()
    {
        if (host == null)
        {
            return;
        }

        EnsureBuilt();
        bool shouldBeVisible = host.IsUiVisible;
        if (shouldBeVisible != lastVisible)
        {
            SetViewVisible(shouldBeVisible);
        }

        if (!shouldBeVisible)
        {
            return;
        }

        UpdateFeedback();
        UpdateNavigationState();

        bool pageChanged = lastPage != host.CurrentPage;
        bool resolutionChanged = lastScreenWidth != Screen.width || lastScreenHeight != Screen.height;
        if (!refreshRequested && !pageChanged && !resolutionChanged)
        {
            return;
        }

        lastPage = host.CurrentPage;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        refreshRequested = false;
        RebuildCurrentPage();
    }

    private void EnsureBuilt()
    {
        if (initialized)
        {
            return;
        }

        uiFont = ResolveFont();
        EnsureTemplatesLoaded();
        if (!HasShellReferences())
        {
            TryResolveShellReferences();
        }

        if (!HasShellReferences())
        {
            BuildShellHierarchy();
        }

        ApplyFontToExistingText(canvasRoot);
        BindStaticButtons();
        EnsureEventSystem();
        initialized = HasShellReferences();
    }

    public void BuildPrefabShellForEditor()
    {
        ResetShellReferences();
        ClearChildren(transform);
        uiFont = ResolveBuiltinFallbackFont();
        BuildShellHierarchy();
        ApplyFontToExistingText(canvasRoot);
        initialized = false;
    }

    public void ConfigureTemplates(
        PrototypeMainMenuCardTemplate configuredCardTemplate,
        PrototypeMainMenuButtonTemplate configuredButtonTemplate,
        PrototypeMainMenuPanelTemplate configuredPanelTemplate)
    {
        cardTemplate = configuredCardTemplate;
        buttonTemplate = configuredButtonTemplate;
        panelTemplate = configuredPanelTemplate;
    }

    private void BuildShellHierarchy()
    {
        canvasRoot = CreateRectTransform("MetaMenuCanvas", transform);
        SetStretch(canvasRoot, 0f, 0f, 0f, 0f);

        Canvas canvas = canvasRoot.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasRoot.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRoot.gameObject.AddComponent<GraphicRaycaster>();

        Image background = canvasRoot.gameObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.1f, 0.14f, 0.94f);

        RectTransform header = CreateRectTransform("Header", canvasRoot);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.offsetMin = new Vector2(32f, -110f);
        header.offsetMax = new Vector2(-32f, -24f);

        CreateText(header, "Project-XX", 34, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        RectTransform subtitleRoot = CreateRectTransform("Subtitle", header);
        subtitleRoot.anchorMin = new Vector2(0f, 0f);
        subtitleRoot.anchorMax = new Vector2(1f, 1f);
        subtitleRoot.offsetMin = new Vector2(4f, 4f);
        subtitleRoot.offsetMax = new Vector2(-4f, -44f);
        CreateText(subtitleRoot, "单人撤离原型", 18, FontStyle.Normal, new Color(0.84f, 0.89f, 0.94f), TextAnchor.LowerLeft);

        RectTransform navPanel = CreatePanelRoot("Navigation", canvasRoot, new Color(0.11f, 0.15f, 0.2f, 0.98f), 236f);
        navPanel.anchorMin = new Vector2(0f, 0f);
        navPanel.anchorMax = new Vector2(0f, 1f);
        navPanel.pivot = new Vector2(0f, 1f);
        navPanel.offsetMin = new Vector2(32f, 72f);
        navPanel.offsetMax = new Vector2(268f, -140f);
        BuildNavigation(navPanel);

        pageHost = CreateRectTransform("PageHost", canvasRoot);
        pageHost.anchorMin = new Vector2(0f, 0f);
        pageHost.anchorMax = new Vector2(1f, 1f);
        pageHost.offsetMin = new Vector2(292f, 72f);
        pageHost.offsetMax = new Vector2(-32f, -88f);

        homePage = CreateRectTransform("HomePage", pageHost);
        warehousePage = CreateRectTransform("WarehousePage", pageHost);
        merchantPage = CreateRectTransform("MerchantPage", pageHost);
        SetStretch(homePage, 0f, 0f, 0f, 0f);
        SetStretch(warehousePage, 0f, 0f, 0f, 0f);
        SetStretch(merchantPage, 0f, 0f, 0f, 0f);

        RectTransform footer = CreateRectTransform("Footer", canvasRoot);
        footer.anchorMin = new Vector2(0f, 0f);
        footer.anchorMax = new Vector2(1f, 0f);
        footer.pivot = new Vector2(0.5f, 0f);
        footer.offsetMin = new Vector2(36f, 18f);
        footer.offsetMax = new Vector2(-36f, 56f);
        feedbackText = CreateText(footer, string.Empty, 16, FontStyle.Normal, new Color(0.98f, 0.84f, 0.45f), TextAnchor.MiddleLeft);
    }

    private void BuildNavigation(RectTransform navPanel)
    {
        VerticalLayoutGroup layout = navPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = navPanel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        CreateText(navPanel, "行动", 24, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);

        homeButton = CreateButton(navPanel, "出击", null, 42f);
        homeButton.gameObject.name = "HomeButton";
        warehouseButton = CreateButton(navPanel, "仓库", null, 42f);
        warehouseButton.gameObject.name = "WarehouseButton";
        merchantButton = CreateButton(navPanel, "商人", null, 42f);
        merchantButton.gameObject.name = "MerchantButton";

        CreateSpacer(navPanel, 6f);
        saveProfileButton = CreateButton(navPanel, "保存档案", null, 34f);
        saveProfileButton.gameObject.name = "SaveProfileButton";
        resetProfileButton = CreateButton(navPanel, "重置档案", null, 34f);
        resetProfileButton.gameObject.name = "ResetProfileButton";
        CreateSpacer(navPanel, 6f);
        exitButton = CreateButton(navPanel, "退出", null, 34f);
        exitButton.gameObject.name = "ExitButton";
    }

    private bool HasShellReferences()
    {
        return canvasRoot != null
            && pageHost != null
            && homePage != null
            && warehousePage != null
            && merchantPage != null
            && feedbackText != null
            && homeButton != null
            && warehouseButton != null
            && merchantButton != null
            && saveProfileButton != null
            && resetProfileButton != null
            && exitButton != null;
    }

    private void ResetShellReferences()
    {
        canvasRoot = null;
        pageHost = null;
        homePage = null;
        warehousePage = null;
        merchantPage = null;
        feedbackText = null;
        homeButton = null;
        warehouseButton = null;
        merchantButton = null;
        saveProfileButton = null;
        resetProfileButton = null;
        exitButton = null;
    }

    private void TryResolveShellReferences()
    {
        canvasRoot ??= FindRectTransform("MetaMenuCanvas");
        pageHost ??= FindRectTransform("MetaMenuCanvas/PageHost");
        homePage ??= FindRectTransform("MetaMenuCanvas/PageHost/HomePage");
        warehousePage ??= FindRectTransform("MetaMenuCanvas/PageHost/WarehousePage");
        merchantPage ??= FindRectTransform("MetaMenuCanvas/PageHost/MerchantPage");
        feedbackText ??= FindText("MetaMenuCanvas/Footer");
        homeButton ??= FindButton("MetaMenuCanvas/Navigation/HomeButton");
        warehouseButton ??= FindButton("MetaMenuCanvas/Navigation/WarehouseButton");
        merchantButton ??= FindButton("MetaMenuCanvas/Navigation/MerchantButton");
        saveProfileButton ??= FindButton("MetaMenuCanvas/Navigation/SaveProfileButton");
        resetProfileButton ??= FindButton("MetaMenuCanvas/Navigation/ResetProfileButton");
        exitButton ??= FindButton("MetaMenuCanvas/Navigation/ExitButton");
    }

    private void EnsureTemplatesLoaded()
    {
        buttonTemplate ??= LoadTemplateFromResources<PrototypeMainMenuButtonTemplate>(ButtonTemplateResourcePath);
        cardTemplate ??= LoadTemplateFromResources<PrototypeMainMenuCardTemplate>(CardTemplateResourcePath);
        panelTemplate ??= LoadTemplateFromResources<PrototypeMainMenuPanelTemplate>(PanelTemplateResourcePath);
    }

    private static T LoadTemplateFromResources<T>(string resourcePath) where T : Component
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        return prefab != null ? prefab.GetComponent<T>() : null;
    }

    private void BindStaticButtons()
    {
        BindButton(homeButton, () => host.CurrentPage = PrototypeMainMenuController.MenuPage.Home);
        BindButton(warehouseButton, () => host.CurrentPage = PrototypeMainMenuController.MenuPage.Warehouse);
        BindButton(merchantButton, host.ShowMerchantDirectory);
        BindButton(saveProfileButton, SaveProfile);
        BindButton(resetProfileButton, ResetProfile);
        BindButton(exitButton, ExitApplication);
    }

    private void BindButton(Button button, Action onClick)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        if (onClick == null)
        {
            return;
        }

        button.onClick.AddListener(() =>
        {
            onClick();
            RequestRefresh();
        });
    }

    private RectTransform FindRectTransform(string relativePath)
    {
        Transform target = transform.Find(relativePath);
        return target != null ? target as RectTransform : null;
    }

    private Text FindText(string relativePath)
    {
        Transform target = transform.Find(relativePath);
        return target != null ? target.GetComponentInChildren<Text>(true) : null;
    }

    private Button FindButton(string relativePath)
    {
        Transform target = transform.Find(relativePath);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private void ApplyFontToExistingText(Transform root)
    {
        if (root == null || uiFont == null)
        {
            return;
        }

        Text[] textComponents = root.GetComponentsInChildren<Text>(true);
        for (int index = 0; index < textComponents.Length; index++)
        {
            Text label = textComponents[index];
            if (label != null)
            {
                label.font = uiFont;
            }
        }
    }

    private void SaveProfile()
    {
        host.SaveProfileFromContainers();
        host.SetFeedback("档案已保存。");
        RequestRefresh();
    }

    private void ResetProfile()
    {
        host.ResetProfile();
        RequestRefresh();
    }

    private void ExitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void UpdateFeedback()
    {
        if (feedbackText == null)
        {
            return;
        }

        feedbackText.text = !string.IsNullOrWhiteSpace(host.FeedbackMessage) && Time.time <= host.FeedbackUntilTime
            ? host.FeedbackMessage
            : string.Empty;
    }

    private void UpdateNavigationState()
    {
        ApplyNavigationButtonState(homeButton, host.CurrentPage == PrototypeMainMenuController.MenuPage.Home);
        ApplyNavigationButtonState(warehouseButton, host.CurrentPage == PrototypeMainMenuController.MenuPage.Warehouse);
        ApplyNavigationButtonState(merchantButton, host.CurrentPage == PrototypeMainMenuController.MenuPage.Merchants);
    }

    private void RebuildCurrentPage()
    {
        bool showHome = host.CurrentPage == PrototypeMainMenuController.MenuPage.Home;
        bool showWarehouse = host.CurrentPage == PrototypeMainMenuController.MenuPage.Warehouse;
        homePage.gameObject.SetActive(showHome);
        warehousePage.gameObject.SetActive(showWarehouse);
        merchantPage.gameObject.SetActive(!showHome && !showWarehouse);

        if (showHome)
        {
            BuildHomePage(homePage);
            return;
        }

        if (showWarehouse)
        {
            BuildWarehousePage(warehousePage);
            return;
        }

        BuildMerchantPage(merchantPage);
    }

    private void BuildHomePage(RectTransform pageRoot)
    {
        ClearChildren(pageRoot);

        RectTransform scrollRoot = CreateRectTransform("HomeScroll", pageRoot);
        SetStretch(scrollRoot, 0f, 0f, 0f, 0f);
        RectTransform scrollContent;
        CreateScrollView(scrollRoot, out scrollContent);

        VerticalLayoutGroup contentLayout = scrollContent.gameObject.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 16f;

        RectTransform introCard = CreateCard(scrollContent, false, 0f);
        CreateSectionTitle(introCard, "战备室");
        CreateBodyText(
            introCard,
            "仓库物品和武器柜中的武器是安全的。战局背包、已装备枪械和护甲存在丢失风险。近战槽、安全箱和特殊装备槽属于保护栏位，角色在战斗中死亡后仍会保留。");

        RectTransform mapCard = CreateCard(scrollContent, false, 0f);
        CreateSectionTitle(mapCard, "出击地图");
        CreateBodyText(mapCard, host.GetSelectedRaidSceneDisplayName(), 18, FontStyle.Bold, Color.white);
        CreateBodyText(mapCard, host.GetSelectedRaidSceneDescription(), 15, FontStyle.Normal, new Color(0.83f, 0.88f, 0.93f));

        if (host.GetRaidSceneOptionCount() > 1)
        {
            List<ButtonSpec> specs = new List<ButtonSpec>();
            for (int index = 0; index < host.GetRaidSceneOptionCount(); index++)
            {
                int optionIndex = index;
                string label = optionIndex == host.GetSelectedRaidSceneIndex()
                    ? $"> {host.GetRaidSceneOptionDisplayName(optionIndex)}"
                    : host.GetRaidSceneOptionDisplayName(optionIndex);
                specs.Add(new ButtonSpec(label, () =>
                {
                    host.SelectRaidScene(optionIndex);
                    RequestRefresh();
                }));
            }

            CreateButtonRows(mapCard, specs, 3, 36f, host.GetSelectedRaidSceneIndex());
        }

        RectTransform statsCard = CreateCard(scrollContent, false, 0f);
        CreateSectionTitle(statsCard, "角色状态");
        CreateBodyText(statsCard, $"资金：{host.GetAvailableFunds()} {host.GetCurrencyLabel()}", 16, FontStyle.Bold, Color.white);
        CreateBodyText(statsCard, $"等级：{host.PlayerLevel}", 16, FontStyle.Bold, Color.white);
        CreateSectionTitle(statsCard, "Character Growth", 18);
        CreateBodyText(statsCard, host.BuildPlayerProgressionSummaryText());
        CreateSectionTitle(statsCard, "Attributes", 18);
        CreateBodyText(statsCard, host.BuildPlayerAttributeSummaryText());

        RectTransform summaryCard = CreateCard(scrollContent, false, 0f);
        CreateSectionTitle(summaryCard, "当前整备");
        CreateBodyText(summaryCard, host.BuildHomePageSummaryText());

        RectTransform actionCard = CreateCard(scrollContent, false, 0f);
        CreateSectionTitle(actionCard, "行动");
        List<ButtonSpec> actions = new List<ButtonSpec>
        {
            new ButtonSpec("进入战斗", host.StartRaid),
            new ButtonSpec("打开仓库", () => host.CurrentPage = PrototypeMainMenuController.MenuPage.Warehouse),
            new ButtonSpec("拜访商人", host.ShowMerchantDirectory)
        };

        if (host.ShouldShowBaseHubEntry())
        {
            actions.Add(new ButtonSpec("进入基地", host.EnterBaseHub));
        }

        CreateButtonRows(actionCard, actions, 2, 44f);
    }

    private void BuildWarehousePage(RectTransform pageRoot)
    {
        ClearChildren(pageRoot);

        RectTransform columns = CreateRectTransform("WarehouseColumns", pageRoot);
        SetStretch(columns, 0f, 0f, 0f, 0f);
        HorizontalLayoutGroup layout = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        PanelRefs stashPanel = CreateScrollablePanel(
            columns,
            "仓库储藏",
            host.StashInventory != null
                ? $"堆叠 {host.StashInventory.Items.Count}/{host.StashInventory.MaxSlots}  重量 {host.StashInventory.CurrentWeight:0.0}/{host.StashInventory.MaxWeight:0.0}"
                : "未配置容器",
            host.StashColor);
        BuildStashPanelContent(stashPanel.content);

        PanelRefs backpackPanel = CreateScrollablePanel(
            columns,
            "战局背包",
            host.RaidBackpackInventory != null
                ? $"格位 {host.RaidBackpackInventory.OccupiedSlots}/{host.RaidBackpackInventory.MaxSlots}  重量 {host.RaidBackpackInventory.CurrentWeight:0.0}/{host.RaidBackpackInventory.MaxWeight:0.0}"
                : "未配置容器",
            host.BackpackColor);
        BuildBackpackPanelContent(backpackPanel.footer, backpackPanel.content);

        PanelRefs lockerPanel = CreateScrollablePanel(
            columns,
            "武器柜",
            $"已存武器 {host.WeaponLocker.Count}",
            host.LockerColor);
        BuildLockerPanelContent(lockerPanel.content);

        PanelRefs protectedPanel = CreateScrollablePanel(
            columns,
            "保护装备",
            $"护甲 {host.EquippedArmor.Count}  安全箱 {host.GetInventoryStackCount(host.SecureContainerInventory)}  特殊装备 {host.GetInventoryStackCount(host.SpecialEquipmentInventory)}",
            host.ProtectedColor);
        BuildProtectedPanelContent(protectedPanel.content);
    }

    private void BuildMerchantPage(RectTransform pageRoot)
    {
        ClearChildren(pageRoot);

        PrototypeMerchantCatalog merchantCatalog = host.MerchantCatalog;
        if (merchantCatalog == null || merchantCatalog.Merchants == null || merchantCatalog.Merchants.Count == 0)
        {
            RectTransform emptyCard = CreateCard(pageRoot, true, 0f);
            SetStretch(emptyCard, 0f, 0f, 0f, 0f);
            CreateSectionTitle(emptyCard, "商人");
            CreateBodyText(emptyCard, "未配置商人目录。");
            return;
        }

        RectTransform columns = CreateRectTransform("MerchantColumns", pageRoot);
        SetStretch(columns, 0f, 0f, 0f, 0f);
        HorizontalLayoutGroup layout = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        List<PrototypeMerchantCatalog.MerchantDefinition> displayMerchants = BuildMerchantDisplayOrder(merchantCatalog);
        for (int merchantIndex = 0; merchantIndex < displayMerchants.Count; merchantIndex++)
        {
            PrototypeMerchantCatalog.MerchantDefinition merchant = displayMerchants[merchantIndex];
            if (merchant == null)
            {
                continue;
            }

            bool isFocusedMerchant = host.IsMerchantFocused(merchant.MerchantId);
            Color accent = ResolveMerchantAccent(merchantIndex);
            PanelRefs panel = CreateScrollablePanel(
                columns,
                merchant.DisplayName,
                isFocusedMerchant
                    ? $"当前交互  ·  等级 {merchant.MerchantLevel}  资金 {host.GetAvailableFunds()} {host.GetCurrencyLabel()}"
                    : $"等级 {merchant.MerchantLevel}  资金 {host.GetAvailableFunds()} {host.GetCurrencyLabel()}",
                accent);

            bool drewOffer = false;
            foreach (PrototypeMerchantCatalog.MerchantOfferView offer in merchant.EnumerateOffers())
            {
                if (!offer.IsValid || offer.ItemInstance == null)
                {
                    continue;
                }

                drewOffer = true;
                CreateMerchantOfferCard(panel.content, offer);
            }

            if (!drewOffer)
            {
                CreateEmptyLabel(panel.content, "当前没有可售商品。");
            }

            if (isFocusedMerchant && panel.footer != null)
            {
                CreateButton(panel.footer, "查看全部商人", () =>
                {
                    host.ShowMerchantDirectory();
                    RequestRefresh();
                }, 34f);
            }
        }
    }

    private List<PrototypeMerchantCatalog.MerchantDefinition> BuildMerchantDisplayOrder(PrototypeMerchantCatalog merchantCatalog)
    {
        var orderedMerchants = new List<PrototypeMerchantCatalog.MerchantDefinition>();
        if (merchantCatalog == null || merchantCatalog.Merchants == null)
        {
            return orderedMerchants;
        }

        PrototypeMerchantCatalog.MerchantDefinition focusedMerchant = host.GetFocusedMerchant();
        if (focusedMerchant != null)
        {
            orderedMerchants.Add(focusedMerchant);
        }

        for (int merchantIndex = 0; merchantIndex < merchantCatalog.Merchants.Count; merchantIndex++)
        {
            PrototypeMerchantCatalog.MerchantDefinition merchant = merchantCatalog.Merchants[merchantIndex];
            if (merchant == null || ReferenceEquals(merchant, focusedMerchant))
            {
                continue;
            }

            orderedMerchants.Add(merchant);
        }

        return orderedMerchants;
    }

    private Color ResolveMerchantAccent(int merchantIndex)
    {
        switch (merchantIndex % 4)
        {
            case 1:
                return host.BackpackColor;

            case 2:
                return host.ProtectedColor;

            case 3:
                return host.LockerColor;

            default:
                return host.StashColor;
        }
    }

    private void BuildStashPanelContent(RectTransform content)
    {
        InventoryContainer stashInventory = host.StashInventory;
        if (stashInventory == null || stashInventory.IsEmpty)
        {
            CreateEmptyLabel(content, "空。");
            return;
        }

        for (int index = 0; index < stashInventory.Items.Count; index++)
        {
            ItemInstance item = stashInventory.Items[index];
            if (item == null || !item.IsDefined())
            {
                continue;
            }

            int itemIndex = index;
            List<ButtonSpec> actions = new List<ButtonSpec>();
            if (item.Definition is ArmorDefinition)
            {
                actions.Add(new ButtonSpec("装备", () => inventoryActions.EquipArmorFromInventory(stashInventory, itemIndex, "仓库")));
                actions.Add(new ButtonSpec("装包", () => inventoryActions.MoveItemBetweenInventories(stashInventory, host.RaidBackpackInventory, itemIndex, item.Quantity, "已装入战局背包", "战局背包空间不足，无法放入该堆叠。")));
                actions.Add(new ButtonSpec("出售", () => inventoryActions.SellItemFromInventory(stashInventory, itemIndex, item.Quantity, "已出售护甲")));
            }
            else
            {
                actions.Add(new ButtonSpec("装包", () => inventoryActions.MoveItemBetweenInventories(stashInventory, host.RaidBackpackInventory, itemIndex, item.Quantity, "已装入战局背包", "战局背包空间不足，无法放入该堆叠。")));
                actions.Add(new ButtonSpec("放安全箱", () => inventoryActions.MoveItemBetweenInventories(stashInventory, host.SecureContainerInventory, itemIndex, item.Quantity, "已放入安全箱", "安全箱空间不足，无法放入该堆叠。")));
                actions.Add(new ButtonSpec("放特殊栏", () => inventoryActions.MoveItemBetweenInventories(stashInventory, host.SpecialEquipmentInventory, itemIndex, item.Quantity, "已放入特殊装备栏", "特殊装备栏空间不足，无法放入该堆叠。")));
                if (host.CashDefinition != null && item.Definition != host.CashDefinition)
                {
                    actions.Add(new ButtonSpec("出售", () => inventoryActions.SellItemFromInventory(stashInventory, itemIndex, item.Quantity, "已出售物品")));
                }
            }

            CreateInventoryItemCard(content, item, actions);
        }
    }

    private void BuildBackpackPanelContent(RectTransform footerRoot, RectTransform content)
    {
        InventoryContainer raidBackpackInventory = host.RaidBackpackInventory;
        if (raidBackpackInventory == null || raidBackpackInventory.IsEmpty)
        {
            CreateEmptyLabel(content, "空。");
        }
        else
        {
            for (int index = 0; index < raidBackpackInventory.Items.Count; index++)
            {
                ItemInstance item = raidBackpackInventory.Items[index];
                if (item == null || !item.IsDefined())
                {
                    continue;
                }

                int itemIndex = index;
                List<ButtonSpec> actions = new List<ButtonSpec>();
                if (item.IsWeapon)
                {
                    actions.Add(new ButtonSpec("入库", () => inventoryActions.StoreWeaponItemFromInventory(raidBackpackInventory, itemIndex)));
                }
                else
                {
                    actions.Add(new ButtonSpec("入库", () => inventoryActions.MoveItemBetweenInventories(raidBackpackInventory, host.StashInventory, itemIndex, item.Quantity, "已存入仓库", "仓库空间不足，无法接收该堆叠。")));
                }

                if (item.Definition is ArmorDefinition)
                {
                    actions.Add(new ButtonSpec("装备", () => inventoryActions.EquipArmorFromInventory(raidBackpackInventory, itemIndex, "战局背包")));
                }
                else if (item.IsWeapon && item.WeaponDefinition != null)
                {
                    if (item.WeaponDefinition.IsMeleeWeapon)
                    {
                        actions.Add(new ButtonSpec("装备", () => inventoryActions.EquipWeaponFromInventory(raidBackpackInventory, itemIndex, PrototypeMainMenuController.WeaponSlotType.Melee)));
                    }
                    else
                    {
                        actions.Add(new ButtonSpec("主武器", () => inventoryActions.EquipWeaponFromInventory(raidBackpackInventory, itemIndex, PrototypeMainMenuController.WeaponSlotType.Primary)));
                        actions.Add(new ButtonSpec("副武器", () => inventoryActions.EquipWeaponFromInventory(raidBackpackInventory, itemIndex, PrototypeMainMenuController.WeaponSlotType.Secondary)));
                    }
                }

                CreateInventoryItemCard(content, item, actions);
            }
        }

        if (footerRoot != null)
        {
            CreateButton(footerRoot, "全部入库", () =>
            {
                inventoryActions.StoreAllRaidBackpack();
                RequestRefresh();
            }, 34f);
        }
    }

    private void BuildLockerPanelContent(RectTransform content)
    {
        if (host.WeaponLocker.Count == 0)
        {
            CreateEmptyLabel(content, "当前没有存放的安全武器。");
            return;
        }

        for (int index = 0; index < host.WeaponLocker.Count; index++)
        {
            ItemInstance weapon = host.WeaponLocker[index];
            if (weapon == null || weapon.WeaponDefinition == null)
            {
                continue;
            }

            int lockerIndex = index;
            List<ButtonSpec> actions = new List<ButtonSpec>();
            if (weapon.WeaponDefinition.IsMeleeWeapon)
            {
                actions.Add(new ButtonSpec("装备近战", () => inventoryActions.EquipWeaponFromLocker(lockerIndex, PrototypeMainMenuController.WeaponSlotType.Melee)));
            }
            else
            {
                actions.Add(new ButtonSpec("主武器", () => inventoryActions.EquipWeaponFromLocker(lockerIndex, PrototypeMainMenuController.WeaponSlotType.Primary)));
                actions.Add(new ButtonSpec("副武器", () => inventoryActions.EquipWeaponFromLocker(lockerIndex, PrototypeMainMenuController.WeaponSlotType.Secondary)));
            }

            actions.Add(new ButtonSpec("出售", () => inventoryActions.SellWeaponFromLocker(lockerIndex)));
            CreateInventoryItemCard(content, weapon, actions);
        }
    }

    private void BuildProtectedPanelContent(RectTransform content)
    {
        CreateSubsection(content, "武器栏位");
        CreateWeaponSlotCard(content, "主武器", host.EquippedPrimaryWeapon, PrototypeMainMenuController.WeaponSlotType.Primary, false);
        CreateWeaponSlotCard(content, "副武器", host.EquippedSecondaryWeapon, PrototypeMainMenuController.WeaponSlotType.Secondary, false);
        CreateWeaponSlotCard(content, "近战（保护）", host.EquippedMeleeWeapon, PrototypeMainMenuController.WeaponSlotType.Melee, true);

        CreateSubsection(content, "已装备护甲");
        if (host.EquippedArmor.Count == 0)
        {
            CreateEmptyLabel(content, "未装备护甲。");
        }
        else
        {
            for (int index = 0; index < host.EquippedArmor.Count; index++)
            {
                ArmorInstance armorInstance = host.EquippedArmor[index];
                if (armorInstance == null || armorInstance.Definition == null)
                {
                    continue;
                }

                int armorIndex = index;
                List<ButtonSpec> actions = new List<ButtonSpec>
                {
                    new ButtonSpec("入库", () => inventoryActions.StoreEquippedArmorToStash(armorIndex)),
                    new ButtonSpec("装包", () => inventoryActions.MoveEquippedArmorToBackpack(armorIndex)),
                    new ButtonSpec("出售", () => inventoryActions.SellEquippedArmor(armorIndex))
                };
                CreateArmorCard(content, armorInstance, actions);
            }
        }

        CreateSubsection(content, "安全箱");
        BuildProtectedInventorySection(content, host.SecureContainerInventory);

        CreateSubsection(content, "特殊装备");
        BuildProtectedInventorySection(content, host.SpecialEquipmentInventory);
    }

    private void BuildProtectedInventorySection(RectTransform content, InventoryContainer inventory)
    {
        if (inventory == null || inventory.IsEmpty)
        {
            CreateEmptyLabel(content, "空。");
            return;
        }

        for (int index = 0; index < inventory.Items.Count; index++)
        {
            ItemInstance item = inventory.Items[index];
            if (item == null || !item.IsDefined())
            {
                continue;
            }

            int itemIndex = index;
            List<ButtonSpec> actions = new List<ButtonSpec>
            {
                new ButtonSpec("入库", () => inventoryActions.MoveItemBetweenInventories(inventory, host.StashInventory, itemIndex, item.Quantity, "已存入仓库", "仓库空间不足，无法接收该堆叠。"))
            };
            CreateInventoryItemCard(content, item, actions);
        }
    }

    private void CreateWeaponSlotCard(RectTransform parent, string label, ItemInstance weaponInstance, PrototypeMainMenuController.WeaponSlotType slotType, bool protectedOnDeath)
    {
        RectTransform card = CreateCard(parent, false, 0f);
        string title = weaponInstance != null
            ? $"{label}：{weaponInstance.DisplayName}"
            : $"{label}：空";
        Text titleText = CreateSectionTitle(card, title, 18);
        titleText.color = weaponInstance != null
            ? ItemRarityUtility.GetDisplayColor(weaponInstance.Rarity)
            : new Color(0.82f, 0.86f, 0.9f);

        if (weaponInstance != null)
        {
            CreateBodyText(card, PrototypeMainMenuController.BuildItemInstanceDetail(weaponInstance));
        }
        else
        {
            CreateBodyText(card, "当前栏位未装备武器。", 14, FontStyle.Italic, new Color(0.72f, 0.77f, 0.84f));
        }

        if (protectedOnDeath)
        {
            CreateBodyText(card, "该栏位在战斗死亡后会保留。", 14, FontStyle.Italic, new Color(0.93f, 0.82f, 0.42f));
        }

        if (weaponInstance == null)
        {
            return;
        }

        List<ButtonSpec> actions = new List<ButtonSpec>
        {
            new ButtonSpec("入库", () => inventoryActions.StoreEquippedWeapon(slotType)),
            new ButtonSpec("出售", () => inventoryActions.SellEquippedWeapon(slotType))
        };
        CreateButtonRows(card, actions, 2, 32f);
    }

    private void CreateArmorCard(RectTransform parent, ArmorInstance armorInstance, List<ButtonSpec> actions)
    {
        RectTransform card = CreateCard(parent, false, 0f);
        Text titleText = CreateSectionTitle(card, armorInstance != null ? armorInstance.DisplayName : "未知护甲", 18);
        if (armorInstance != null)
        {
            titleText.color = ItemRarityUtility.GetDisplayColor(armorInstance.Rarity);
            CreateBodyText(card, PrototypeMainMenuController.BuildItemInstanceDetail(ItemInstance.Create(armorInstance)));
        }
        else
        {
            CreateBodyText(card, "护甲数据缺失。", 14, FontStyle.Italic, new Color(0.72f, 0.77f, 0.84f));
        }

        CreateButtonRows(card, actions, 3, 32f);
    }

    private void CreateInventoryItemCard(RectTransform parent, ItemInstance item, List<ButtonSpec> actions)
    {
        RectTransform card = CreateCard(parent, false, 0f);
        string title = item != null && item.Quantity > 1
            ? $"{item.DisplayName} x{item.Quantity}"
            : item != null
                ? item.DisplayName
                : "未知物品";
        Text titleText = CreateSectionTitle(card, title, 18);
        if (item != null)
        {
            titleText.color = ItemRarityUtility.GetDisplayColor(item.Rarity);
            CreateBodyText(card, PrototypeMainMenuController.BuildItemInstanceDetail(item));
        }
        else
        {
            CreateBodyText(card, "物品数据缺失。", 14, FontStyle.Italic, new Color(0.72f, 0.77f, 0.84f));
        }

        CreateButtonRows(card, actions, 3, 32f);
    }

    private void CreateMerchantOfferCard(RectTransform parent, PrototypeMerchantCatalog.MerchantOfferView offer)
    {
        ItemInstance item = offer.ItemInstance;
        if (item == null)
        {
            return;
        }

        RectTransform card = CreateCard(parent, false, 0f);
        string title = item.Quantity > 1 ? $"{item.DisplayName} x{item.Quantity}" : item.DisplayName;
        Text titleText = CreateSectionTitle(card, title, 18);
        titleText.color = ItemRarityUtility.GetDisplayColor(item.Rarity);
        CreateBodyText(card, PrototypeMainMenuController.BuildItemInstanceDetail(item));
        CreateBodyText(card, $"价格 {offer.Price} {host.GetCurrencyLabel()}", 15, FontStyle.Bold, new Color(0.98f, 0.84f, 0.45f));
        CreateButtonRows(card, new List<ButtonSpec>
        {
            new ButtonSpec("购买", () => merchantActions.BuyOffer(offer))
        }, 1, 34f);
    }

    private PanelRefs CreateScrollablePanel(Transform parent, string title, string subtitle, Color accent)
    {
        if (panelTemplate != null)
        {
            PrototypeMainMenuPanelTemplate templateInstance = Instantiate(panelTemplate, parent, false);
            templateInstance.gameObject.name = title;
            ApplyFontToExistingText(templateInstance.transform);
            NormalizeTemplateLayout(templateInstance.transform);

            if (templateInstance.AccentImage != null)
            {
                templateInstance.AccentImage.color = accent;
            }

            if (templateInstance.TitleText != null)
            {
                templateInstance.TitleText.text = title ?? string.Empty;
            }

            if (templateInstance.SubtitleText != null)
            {
                templateInstance.SubtitleText.text = subtitle ?? string.Empty;
                templateInstance.SubtitleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(subtitle));
            }

            if (templateInstance.LayoutElement != null)
            {
                templateInstance.LayoutElement.flexibleWidth = 1f;
                templateInstance.LayoutElement.flexibleHeight = 1f;
                templateInstance.LayoutElement.minWidth = 260f;
            }

            return new PanelRefs
            {
                root = templateInstance.Root,
                content = templateInstance.ContentRoot,
                footer = templateInstance.FooterRoot
            };
        }

        RectTransform root = CreatePanelRoot(title, parent, new Color(0.12f, 0.16f, 0.22f, 0.98f), 260f);
        VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateAccentBar(root, accent);
        CreateSectionTitle(root, title);
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            CreateBodyText(root, subtitle, 13, FontStyle.Normal, new Color(0.75f, 0.81f, 0.88f));
        }

        CreateScrollView(root, out RectTransform content, true);
        return new PanelRefs
        {
            root = root,
            content = content,
            footer = root
        };
    }

    private void CreateButtonRows(RectTransform parent, List<ButtonSpec> specs, int buttonsPerRow, float height, int highlightedIndex = -1)
    {
        if (specs == null || specs.Count == 0)
        {
            return;
        }

        int sanitizedButtonsPerRow = Mathf.Max(1, buttonsPerRow);
        for (int startIndex = 0; startIndex < specs.Count; startIndex += sanitizedButtonsPerRow)
        {
            RectTransform row = CreateRectTransform($"ButtonRow_{startIndex}", parent);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = height;
            rowLayout.flexibleWidth = 1f;

            int endIndex = Mathf.Min(startIndex + sanitizedButtonsPerRow, specs.Count);
            for (int index = startIndex; index < endIndex; index++)
            {
                Button button = CreateButton(row, specs[index].Label, specs[index].OnClick, height);
                LayoutElement buttonLayout = button.gameObject.GetComponent<LayoutElement>();
                if (buttonLayout == null)
                {
                    buttonLayout = button.gameObject.AddComponent<LayoutElement>();
                }

                buttonLayout.flexibleWidth = 1f;
                buttonLayout.preferredHeight = height;

                if (index == highlightedIndex)
                {
                    ApplyButtonColors(
                        button,
                        new Color(0.94f, 0.56f, 0.18f, 1f),
                        new Color(0.98f, 0.66f, 0.28f, 1f),
                        new Color(0.88f, 0.47f, 0.14f, 1f));
                }
            }
        }
    }

    private void CreateSubsection(RectTransform parent, string title)
    {
        CreateSpacer(parent, 4f);
        CreateSectionTitle(parent, title, 18);
    }

    private void CreateEmptyLabel(RectTransform parent, string text)
    {
        CreateBodyText(parent, text, 14, FontStyle.Italic, new Color(0.72f, 0.77f, 0.84f));
    }

    private RectTransform CreateCard(Transform parent, bool flexibleHeight, float minWidth)
    {
        if (cardTemplate != null)
        {
            PrototypeMainMenuCardTemplate templateInstance = Instantiate(cardTemplate, parent, false);
            templateInstance.gameObject.name = "Card";
            ApplyFontToExistingText(templateInstance.transform);
            NormalizeTemplateLayout(templateInstance.transform);

            if (templateInstance.LayoutElement != null)
            {
                templateInstance.LayoutElement.flexibleWidth = 1f;
                templateInstance.LayoutElement.flexibleHeight = flexibleHeight ? 1f : 0f;
                templateInstance.LayoutElement.minWidth = minWidth > 0f ? minWidth : 0f;
            }

            return templateInstance.Root;
        }

        RectTransform card = CreateRectTransform("Card", parent);
        Image background = card.gameObject.AddComponent<Image>();
        background.color = new Color(0.16f, 0.2f, 0.27f, 0.96f);

        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement element = card.gameObject.AddComponent<LayoutElement>();
        element.flexibleWidth = 1f;
        element.flexibleHeight = flexibleHeight ? 1f : 0f;
        if (minWidth > 0f)
        {
            element.minWidth = minWidth;
        }

        return card;
    }

    private RectTransform CreatePanelRoot(string name, Transform parent, Color backgroundColor, float minWidth)
    {
        RectTransform root = CreateRectTransform(name, parent);
        Image background = root.gameObject.AddComponent<Image>();
        background.color = backgroundColor;

        LayoutElement element = root.gameObject.AddComponent<LayoutElement>();
        element.flexibleWidth = 1f;
        element.flexibleHeight = 1f;
        if (minWidth > 0f)
        {
            element.minWidth = minWidth;
        }

        return root;
    }

    private void CreateAccentBar(RectTransform parent, Color accent)
    {
        RectTransform bar = CreateRectTransform("AccentBar", parent);
        Image image = bar.gameObject.AddComponent<Image>();
        image.color = accent;

        LayoutElement layout = bar.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 4f;
        layout.flexibleWidth = 1f;
    }

    private void CreateScrollView(RectTransform parent, out RectTransform content, bool flexibleHeight = false)
    {
        RectTransform scrollRoot = CreateRectTransform("ScrollView", parent);
        if (parent.GetComponent<LayoutGroup>() == null)
        {
            SetStretch(scrollRoot, 0f, 0f, 0f, 0f);
        }
        else
        {
            LayoutElement rootLayout = scrollRoot.gameObject.AddComponent<LayoutElement>();
            rootLayout.flexibleWidth = 1f;
            rootLayout.flexibleHeight = flexibleHeight ? 1f : 0f;
            if (flexibleHeight)
            {
                rootLayout.minHeight = 0f;
            }
        }

        ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 32f;

        RectTransform viewport = CreateRectTransform("Viewport", scrollRoot);
        SetStretch(viewport, 0f, 0f, 0f, 0f);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.015f);
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        content = CreateRectTransform("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(0f, 0f);
        content.offsetMax = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 12f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
    }

    private Text CreateSectionTitle(Transform parent, string text, int fontSize = 22)
    {
        return CreateText(parent, text, fontSize, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
    }

    private Text CreateBodyText(Transform parent, string text, int fontSize = 15, FontStyle fontStyle = FontStyle.Normal, Color? colorOverride = null)
    {
        return CreateText(parent, text, fontSize, fontStyle, colorOverride ?? new Color(0.92f, 0.94f, 0.98f, 1f), TextAnchor.UpperLeft);
    }

    private Text CreateText(Transform parent, string text, int fontSize, FontStyle fontStyle, Color color, TextAnchor anchor)
    {
        RectTransform rect = CreateRectTransform("Text", parent);
        if (parent.GetComponent<LayoutGroup>() == null)
        {
            SetStretch(rect, 0f, 0f, 0f, 0f);
        }
        else
        {
            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
        }

        Text label = rect.gameObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = anchor;
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        label.text = text ?? string.Empty;
        return label;
    }

    private Button CreateButton(Transform parent, string label, Action onClick, float height)
    {
        if (buttonTemplate != null)
        {
            PrototypeMainMenuButtonTemplate templateInstance = Instantiate(buttonTemplate, parent, false);
            templateInstance.gameObject.name = "Button";
            ApplyFontToExistingText(templateInstance.transform);

            if (templateInstance.LabelText != null)
            {
                templateInstance.LabelText.text = label ?? string.Empty;
            }

            if (templateInstance.LayoutElement != null)
            {
                templateInstance.LayoutElement.preferredHeight = height;
                templateInstance.LayoutElement.flexibleWidth = 1f;
            }

            Button templateButton = templateInstance.Button;
            if (templateButton != null)
            {
                templateButton.targetGraphic = templateInstance.BackgroundImage;
                templateButton.transition = Selectable.Transition.ColorTint;
                ApplyButtonColors(
                    templateButton,
                    new Color(0.22f, 0.28f, 0.36f, 1f),
                    new Color(0.31f, 0.38f, 0.47f, 1f),
                    new Color(0.15f, 0.2f, 0.27f, 1f));
                templateButton.onClick.RemoveAllListeners();
                if (onClick != null)
                {
                    templateButton.onClick.AddListener(() =>
                    {
                        onClick();
                        RequestRefresh();
                    });
                }
            }

            return templateButton;
        }

        RectTransform rect = CreateRectTransform("Button", parent);
        Image background = rect.gameObject.AddComponent<Image>();
        background.color = new Color(0.22f, 0.28f, 0.36f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        ApplyButtonColors(
            button,
            new Color(0.22f, 0.28f, 0.36f, 1f),
            new Color(0.31f, 0.38f, 0.47f, 1f),
            new Color(0.15f, 0.2f, 0.27f, 1f));
        if (onClick != null)
        {
            button.onClick.AddListener(() =>
            {
                onClick();
                RequestRefresh();
            });
        }

        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.flexibleWidth = 1f;

        RectTransform textRoot = CreateRectTransform("Label", rect);
        SetStretch(textRoot, 12f, 12f, 6f, 6f);
        Text buttonText = textRoot.gameObject.AddComponent<Text>();
        buttonText.font = uiFont;
        buttonText.fontSize = 15;
        buttonText.fontStyle = FontStyle.Bold;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.supportRichText = false;
        buttonText.horizontalOverflow = HorizontalWrapMode.Wrap;
        buttonText.verticalOverflow = VerticalWrapMode.Overflow;
        buttonText.raycastTarget = false;
        buttonText.text = label ?? string.Empty;

        return button;
    }

    private void ApplyNavigationButtonState(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        ApplyButtonColors(
            button,
            selected ? new Color(0.94f, 0.56f, 0.18f, 1f) : new Color(0.18f, 0.23f, 0.29f, 1f),
            selected ? new Color(0.98f, 0.66f, 0.28f, 1f) : new Color(0.28f, 0.34f, 0.42f, 1f),
            selected ? new Color(0.86f, 0.46f, 0.14f, 1f) : new Color(0.12f, 0.17f, 0.23f, 1f));

        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            buttonText.color = selected ? Color.white : new Color(0.9f, 0.93f, 0.97f, 1f);
        }
    }

    private static void ApplyButtonColors(Button button, Color normal, Color highlighted, Color pressed)
    {
        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.selectedColor = highlighted;
        colors.pressedColor = pressed;
        colors.disabledColor = new Color(normal.r * 0.65f, normal.g * 0.65f, normal.b * 0.65f, 0.7f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        if (button.targetGraphic != null)
        {
            button.targetGraphic.color = normal;
        }
    }

    private void CreateSpacer(Transform parent, float height)
    {
        RectTransform spacer = CreateRectTransform("Spacer", parent);
        LayoutElement layout = spacer.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.flexibleWidth = 1f;
    }

    private static RectTransform CreateRectTransform(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = parent != null ? parent.gameObject.layer : 0;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition3D = Vector3.zero;
        return rect;
    }

    private static void SetStretch(RectTransform rect, float left, float right, float bottom, float top)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void NormalizeTemplateLayout(Transform root)
    {
        if (root == null)
        {
            return;
        }

        VerticalLayoutGroup[] verticalLayouts = root.GetComponentsInChildren<VerticalLayoutGroup>(true);
        for (int index = 0; index < verticalLayouts.Length; index++)
        {
            VerticalLayoutGroup layout = verticalLayouts[index];
            if (layout == null)
            {
                continue;
            }

            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }
    }

    private void EnsureEventSystem()
    {
        EventSystem current = EventSystem.current;
        if (current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            current = eventSystemObject.GetComponent<EventSystem>();
        }

        if (current != null && current.GetComponent<BaseInputModule>() == null)
        {
            current.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private Font ResolveFont()
    {
        Font dynamicFont = Font.CreateDynamicFontFromOSFont(
            new[]
            {
                "Microsoft YaHei UI",
                "Microsoft YaHei",
                "Microsoft JhengHei UI",
                "SimHei",
                "Arial Unicode MS",
                "Arial"
            },
            16);
        if (dynamicFont != null)
        {
            return dynamicFont;
        }

        return ResolveBuiltinFallbackFont();
    }

    private Font ResolveBuiltinFallbackFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            GameObject child = parent.GetChild(index).gameObject;
            if (Application.isPlaying)
            {
                child.SetActive(false);
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }
}
