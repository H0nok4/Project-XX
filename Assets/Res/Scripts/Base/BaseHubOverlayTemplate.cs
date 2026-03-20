using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BaseHubOverlayTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public TMP_Text TitleText => titleText;
    public TMP_Text BodyText => bodyText;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        TMP_Text title,
        TMP_Text body)
    {
        root = rectTransform;
        backgroundImage = background;
        titleText = title;
        bodyText = body;
    }
}
