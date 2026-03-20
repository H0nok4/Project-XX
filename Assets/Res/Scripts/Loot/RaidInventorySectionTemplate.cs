using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RaidInventorySectionTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Text titleText;
    [SerializeField] private Text summaryText;
    [SerializeField] private Text emptyLabelText;
    [SerializeField] private RectTransform contentRoot;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public Text TitleText => titleText;
    public Text SummaryText => summaryText;
    public Text EmptyLabelText => emptyLabelText;
    public RectTransform ContentRoot => contentRoot;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        Text title,
        Text summary,
        Text emptyLabel,
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
