using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class QuestJournalViewTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private RectTransform footerRoot;
    [SerializeField] private ScrollRect listScrollRect;
    [SerializeField] private RectTransform listContentRoot;
    [SerializeField] private ScrollRect detailScrollRect;
    [SerializeField] private RectTransform detailContentRoot;
    [SerializeField] private Button closeButton;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public RectTransform Panel => panel;
    public Text TitleText => titleText;
    public Text SubtitleText => subtitleText;
    public RectTransform BodyRoot => bodyRoot;
    public RectTransform FooterRoot => footerRoot;
    public ScrollRect ListScrollRect => listScrollRect;
    public RectTransform ListContentRoot => listContentRoot;
    public ScrollRect DetailScrollRect => detailScrollRect;
    public RectTransform DetailContentRoot => detailContentRoot;
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
        ScrollRect listScroll,
        RectTransform listContent,
        ScrollRect detailScroll,
        RectTransform detailContent,
        Button close)
    {
        root = rectTransform;
        panel = windowPanel;
        titleText = title;
        subtitleText = subtitle;
        bodyRoot = body;
        footerRoot = footer;
        listScrollRect = listScroll;
        listContentRoot = listContent;
        detailScrollRect = detailScroll;
        detailContentRoot = detailContent;
        closeButton = close;
    }
}
