using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RaidInventorySectionTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text emptyLabelText;
    [SerializeField] private RectTransform contentRoot;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public TMP_Text TitleText => titleText;
    public TMP_Text SummaryText => summaryText;
    public TMP_Text EmptyLabelText => emptyLabelText;
    public RectTransform ContentRoot => contentRoot;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        TMP_Text title,
        TMP_Text summary,
        TMP_Text emptyLabel,
        RectTransform content)
    {
        root = rectTransform;
        backgroundImage = background;
        titleText = title;
        summaryText = summary;
        emptyLabelText = emptyLabel;
        contentRoot = content;
    }
}
