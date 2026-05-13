using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ProjectXXLootWindowTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private RectTransform footerRoot;
    [SerializeField] private TMP_Text containerTitleText;
    [SerializeField] private TMP_Text containerSummaryText;
    [SerializeField] private TMP_Text backpackTitleText;
    [SerializeField] private TMP_Text backpackSummaryText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private RectTransform containerGridRoot;
    [SerializeField] private RectTransform containerCellsRoot;
    [SerializeField] private RectTransform containerItemsRoot;
    [SerializeField] private RectTransform backpackGridRoot;
    [SerializeField] private RectTransform backpackCellsRoot;
    [SerializeField] private RectTransform backpackItemsRoot;
    [SerializeField] private RectTransform gridCellTemplate;
    [SerializeField] private ProjectXXInventoryGridItemTemplate gridItemTemplate;
    [SerializeField] private Button takeFirstButton;
    [SerializeField] private Button stashFirstButton;
    [SerializeField] private Button closeButton;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public RectTransform Panel => panel;
    public TMP_Text TitleText => titleText;
    public TMP_Text SubtitleText => subtitleText;
    public RectTransform BodyRoot => bodyRoot;
    public RectTransform FooterRoot => footerRoot;
    public TMP_Text ContainerTitleText => containerTitleText;
    public TMP_Text ContainerSummaryText => containerSummaryText;
    public TMP_Text BackpackTitleText => backpackTitleText;
    public TMP_Text BackpackSummaryText => backpackSummaryText;
    public TMP_Text MessageText => messageText;
    public RectTransform ContainerGridRoot => containerGridRoot;
    public RectTransform ContainerCellsRoot => containerCellsRoot;
    public RectTransform ContainerItemsRoot => containerItemsRoot;
    public RectTransform BackpackGridRoot => backpackGridRoot;
    public RectTransform BackpackCellsRoot => backpackCellsRoot;
    public RectTransform BackpackItemsRoot => backpackItemsRoot;
    public RectTransform GridCellTemplate => gridCellTemplate;
    public ProjectXXInventoryGridItemTemplate GridItemTemplate => gridItemTemplate;
    public Button TakeFirstButton => takeFirstButton;
    public Button StashFirstButton => stashFirstButton;
    public Button CloseButton => closeButton;

    public PrototypeUiToolkit.WindowChrome CreateWindowChrome()
    {
        return new PrototypeUiToolkit.WindowChrome
        {
            Root = Root,
            Panel = panel,
            TitleText = titleText,
            SubtitleText = subtitleText,
            BodyRoot = bodyRoot,
            FooterRoot = footerRoot
        };
    }

    public void ConfigureReferences(
        RectTransform rectTransform,
        RectTransform windowPanel,
        TMP_Text title,
        TMP_Text subtitle,
        RectTransform body,
        RectTransform footer,
        TMP_Text containerTitle,
        TMP_Text containerSummary,
        TMP_Text backpackTitle,
        TMP_Text backpackSummary,
        TMP_Text message,
        RectTransform containerGrid,
        RectTransform containerCells,
        RectTransform containerItems,
        RectTransform backpackGrid,
        RectTransform backpackCells,
        RectTransform backpackItems,
        RectTransform cellTemplate,
        ProjectXXInventoryGridItemTemplate itemTemplate,
        Button takeButton,
        Button stashButton,
        Button closeWindowButton)
    {
        root = rectTransform;
        panel = windowPanel;
        titleText = title;
        subtitleText = subtitle;
        bodyRoot = body;
        footerRoot = footer;
        containerTitleText = containerTitle;
        containerSummaryText = containerSummary;
        backpackTitleText = backpackTitle;
        backpackSummaryText = backpackSummary;
        messageText = message;
        containerGridRoot = containerGrid;
        containerCellsRoot = containerCells;
        containerItemsRoot = containerItems;
        backpackGridRoot = backpackGrid;
        backpackCellsRoot = backpackCells;
        backpackItemsRoot = backpackItems;
        gridCellTemplate = cellTemplate;
        gridItemTemplate = itemTemplate;
        takeFirstButton = takeButton;
        stashFirstButton = stashButton;
        closeButton = closeWindowButton;
    }
}
