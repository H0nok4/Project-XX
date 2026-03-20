using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RaidDragGhostTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text labelText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public CanvasGroup CanvasGroup => canvasGroup;
    public TMP_Text LabelText => labelText;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        CanvasGroup group,
        TMP_Text label)
    {
        root = rectTransform;
        backgroundImage = background;
        canvasGroup = group;
        labelText = label;
    }
}
