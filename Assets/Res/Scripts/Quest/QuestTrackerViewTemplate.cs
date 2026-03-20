using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class QuestTrackerViewTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private VerticalLayoutGroup layoutGroup;
    [SerializeField] private TMP_Text trackerText;
    [SerializeField] private Button journalButton;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Image BackgroundImage => backgroundImage;
    public VerticalLayoutGroup LayoutGroup => layoutGroup;
    public TMP_Text TrackerText => trackerText;
    public Button JournalButton => journalButton;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image background,
        VerticalLayoutGroup layout,
        TMP_Text summaryText,
        Button openJournalButton)
    {
        root = rectTransform;
        backgroundImage = background;
        layoutGroup = layout;
        trackerText = summaryText;
        journalButton = openJournalButton;
    }
}
