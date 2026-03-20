using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PrototypeMainMenuButtonTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private LayoutElement layoutElement;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public Button Button => button;
    public TMP_Text LabelText => labelText;
    public LayoutElement LayoutElement => layoutElement;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        Button targetButton,
        TMP_Text label,
        LayoutElement element)
    {
        root = rectTransform;
        backgroundImage = background;
        button = targetButton;
        labelText = label;
        layoutElement = element;
    }
}
