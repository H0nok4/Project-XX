using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RaidInventorySlotCardTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text emptyLabelText;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private TMP_Text capacityText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public TMP_Text TitleText => titleText;
    public TMP_Text EmptyLabelText => emptyLabelText;
    public TMP_Text ItemNameText => itemNameText;
    public TMP_Text DetailText => detailText;
    public TMP_Text CapacityText => capacityText;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        TMP_Text title,
        TMP_Text emptyLabel,
        TMP_Text itemName,
        TMP_Text detail,
        TMP_Text capacity)
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
