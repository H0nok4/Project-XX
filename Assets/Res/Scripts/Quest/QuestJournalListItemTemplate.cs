using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class QuestJournalListItemTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Button Button => button;
    public Image BackgroundImage => backgroundImage;
    public TMP_Text TitleText => titleText;
    public TMP_Text StatusText => statusText;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Button targetButton,
        Image background,
        TMP_Text title,
        TMP_Text status)
    {
        root = rectTransform;
        button = targetButton;
        backgroundImage = background;
        titleText = title;
        statusText = status;
    }
}
