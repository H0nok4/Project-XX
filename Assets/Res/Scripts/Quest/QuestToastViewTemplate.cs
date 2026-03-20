using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class QuestToastViewTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private VerticalLayoutGroup layoutGroup;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text messageText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public VerticalLayoutGroup LayoutGroup => layoutGroup;
    public CanvasGroup CanvasGroup => canvasGroup;
    public TMP_Text MessageText => messageText;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        VerticalLayoutGroup layout,
        CanvasGroup targetCanvasGroup,
        TMP_Text label)
    {
        root = rectTransform;
        backgroundImage = background;
        layoutGroup = layout;
        canvasGroup = targetCanvasGroup;
        messageText = label;
    }
}
