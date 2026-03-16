using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PrototypeMainMenuPanelTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private LayoutElement layoutElement;
    [SerializeField] private Image accentImage;
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private RectTransform footerRoot;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public LayoutElement LayoutElement => layoutElement;
    public Image AccentImage => accentImage;
    public Text TitleText => titleText;
    public Text SubtitleText => subtitleText;
    public ScrollRect ScrollRect => scrollRect;
    public RectTransform ContentRoot => contentRoot;
    public RectTransform FooterRoot => footerRoot;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        LayoutElement element,
        Image accent,
        Text title,
        Text subtitle,
        ScrollRect targetScrollRect,
        RectTransform content,
        RectTransform footer)
    {
        root = rectTransform;
        backgroundImage = background;
        layoutElement = element;
        accentImage = accent;
        titleText = title;
        subtitleText = subtitle;
        scrollRect = targetScrollRect;
        contentRoot = content;
        footerRoot = footer;
    }
}
