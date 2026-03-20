using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerInventoryWindowTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private RectTransform footerRoot;
    [SerializeField] private Text summaryText;
    [SerializeField] private ScrollRect backpackScrollRect;
    [SerializeField] private RectTransform backpackContentRoot;
    [SerializeField] private ScrollRect gearScrollRect;
    [SerializeField] private RectTransform gearContentRoot;
    [SerializeField] private Button closeButton;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public RectTransform Panel => panel;
    public Text TitleText => titleText;
    public Text SubtitleText => subtitleText;
    public RectTransform BodyRoot => bodyRoot;
    public RectTransform FooterRoot => footerRoot;
    public Text SummaryText => summaryText;
    public ScrollRect BackpackScrollRect => backpackScrollRect;
    public RectTransform BackpackContentRoot => backpackContentRoot;
    public ScrollRect GearScrollRect => gearScrollRect;
    public RectTransform GearContentRoot => gearContentRoot;
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
        Text title,
        Text subtitle,
        RectTransform body,
        RectTransform footer,
        Text summary,
        ScrollRect backpackScroll,
        RectTransform backpackContent,
        ScrollRect gearScroll,
        RectTransform gearContent,
        Button close)
    {
        root = rectTransform;
        panel = windowPanel;
        titleText = title;
        subtitleText = subtitle;
        bodyRoot = body;
        footerRoot = footer;
        summaryText = summary;
        backpackScrollRect = backpackScroll;
        backpackContentRoot = backpackContent;
        gearScrollRect = gearScroll;
        gearContentRoot = gearContent;
        closeButton = close;
    }
}
