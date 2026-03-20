using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RaidInventorySlotCardTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Text titleText;
    [SerializeField] private Text emptyLabelText;
    [SerializeField] private Text itemNameText;
    [SerializeField] private Text detailText;
    [SerializeField] private Text capacityText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public Text TitleText => titleText;
    public Text EmptyLabelText => emptyLabelText;
    public Text ItemNameText => itemNameText;
    public Text DetailText => detailText;
    public Text CapacityText => capacityText;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        Text title,
        Text emptyLabel,
        Text itemName,
        Text detail,
        Text capacity)
    {
        root = rectTransform;
        backgroundImage = background;
        titleText = title;
        emptyLabelText = emptyLabel;
        itemNameText = itemName;
        detailText = detail;
        capacityText = capacity;
    }
}
