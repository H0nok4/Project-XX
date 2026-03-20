using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RaidInventoryItemCardTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Text titleText;
    [SerializeField] private Text detailText;
    [SerializeField] private RectTransform actionRow;
    [SerializeField] private Button actionButton;
    [SerializeField] private Text actionButtonLabel;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public Text TitleText => titleText;
    public Text DetailText => detailText;
    public RectTransform ActionRow => actionRow;
    public Button ActionButton => actionButton;
    public Text ActionButtonLabel => actionButtonLabel;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        Text title,
        Text detail,
        RectTransform actions,
        Button button,
        Text buttonLabel)
    {
        root = rectTransform;
        backgroundImage = background;
        titleText = title;
        detailText = detail;
        actionRow = actions;
        actionButton = button;
        actionButtonLabel = buttonLabel;
    }
}
