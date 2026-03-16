using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PrototypeMainMenuCardTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private VerticalLayoutGroup layoutGroup;
    [SerializeField] private LayoutElement layoutElement;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public VerticalLayoutGroup LayoutGroup => layoutGroup;
    public LayoutElement LayoutElement => layoutElement;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        VerticalLayoutGroup layout,
        LayoutElement element)
    {
        root = rectTransform;
        backgroundImage = background;
        layoutGroup = layout;
        layoutElement = element;
    }
}
