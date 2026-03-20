using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InteractionPromptTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text promptText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public CanvasGroup CanvasGroup => canvasGroup;
    public TMP_Text PromptText => promptText;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        CanvasGroup group,
        TMP_Text label)
    {
        root = rectTransform;
        backgroundImage = background;
        canvasGroup = group;
        promptText = label;
    }
}
