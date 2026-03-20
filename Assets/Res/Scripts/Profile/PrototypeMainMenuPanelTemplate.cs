using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PrototypeMainMenuPanelTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private LayoutElement layoutElement;
    [SerializeField] private Image accentImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private RectTransform footerRoot;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public LayoutElement LayoutElement => layoutElement;
    public Image AccentImage => accentImage;
    public TMP_Text TitleText => titleText;
    public TMP_Text SubtitleText => subtitleText;
    public ScrollRect ScrollRect => scrollRect;
    public RectTransform ContentRoot => contentRoot;
    public RectTransform FooterRoot => footerRoot;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        LayoutElement element,
        Image accent,
        TMP_Text title,
        TMP_Text subtitle,
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
