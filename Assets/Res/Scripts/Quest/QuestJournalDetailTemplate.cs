using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class QuestJournalDetailTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Text placeholderText;
    [SerializeField] private Text titleText;
    [SerializeField] private Text statusText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private RectTransform objectivesRoot;
    [SerializeField] private Text rewardsText;
    [SerializeField] private Text turnInHintText;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button trackButton;
    [SerializeField] private Text trackButtonLabel;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Text PlaceholderText => placeholderText;
    public Text TitleText => titleText;
    public Text StatusText => statusText;
    public Text DescriptionText => descriptionText;
    public RectTransform ObjectivesRoot => objectivesRoot;
    public Text RewardsText => rewardsText;
    public Text TurnInHintText => turnInHintText;
    public Button ClaimButton => claimButton;
    public Button AcceptButton => acceptButton;
    public Button TrackButton => trackButton;
    public Text TrackButtonLabel => trackButtonLabel;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Text placeholder,
        Text title,
        Text status,
        Text description,
        RectTransform objectives,
        Text rewards,
        Text turnInHint,
        Button claim,
        Button accept,
        Button track,
        Text trackLabel)
    {
        root = rectTransform;
        placeholderText = placeholder;
        titleText = title;
        statusText = status;
        descriptionText = description;
        objectivesRoot = objectives;
        rewardsText = rewards;
        turnInHintText = turnInHint;
        claimButton = claim;
        acceptButton = accept;
        trackButton = track;
        trackButtonLabel = trackLabel;
    }
}
