using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RaidInventoryItemCardTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private RectTransform actionRow;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonLabel;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public TMP_Text TitleText => titleText;
    public TMP_Text DetailText => detailText;
    public RectTransform ActionRow => actionRow;
    public Button ActionButton => actionButton;
    public TMP_Text ActionButtonLabel => actionButtonLabel;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        TMP_Text title,
        TMP_Text detail,
        RectTransform actions,
        Button button,
        TMP_Text buttonLabel)
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
