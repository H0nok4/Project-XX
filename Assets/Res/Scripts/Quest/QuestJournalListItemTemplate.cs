using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class QuestJournalListItemTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Text titleText;
    [SerializeField] private Text statusText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Button Button => button;
    public Image BackgroundImage => backgroundImage;
    public Text TitleText => titleText;
    public Text StatusText => statusText;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Button targetButton,
        Image background,
        Text title,
        Text status)
    {
        root = rectTransform;
        button = targetButton;
        backgroundImage = background;
        titleText = title;
        statusText = status;
    }
}
