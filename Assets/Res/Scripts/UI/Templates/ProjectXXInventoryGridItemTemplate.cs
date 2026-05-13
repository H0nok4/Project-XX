using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ProjectXXInventoryGridItemTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text sizeText;
    [SerializeField] private Button button;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public TMP_Text NameText => nameText;
    public TMP_Text QuantityText => quantityText;
    public TMP_Text SizeText => sizeText;
    public Button Button => button;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image itemBackgroundImage,
        TMP_Text itemNameText,
        TMP_Text itemQuantityText,
        TMP_Text itemSizeText,
        Button itemButton)
    {
        root = rectTransform;
        backgroundImage = itemBackgroundImage;
        nameText = itemNameText;
        quantityText = itemQuantityText;
        sizeText = itemSizeText;
        button = itemButton;
    }
}
