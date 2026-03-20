using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class QuestJournalDetailTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text placeholderText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private RectTransform objectivesRoot;
    [SerializeField] private TMP_Text rewardsText;
    [SerializeField] private TMP_Text turnInHintText;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button trackButton;
    [SerializeField] private TMP_Text trackButtonLabel;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public TMP_Text PlaceholderText => placeholderText;
    public TMP_Text TitleText => titleText;
    public TMP_Text StatusText => statusText;
    public TMP_Text DescriptionText => descriptionText;
    public RectTransform ObjectivesRoot => objectivesRoot;
    public TMP_Text RewardsText => rewardsText;
    public TMP_Text TurnInHintText => turnInHintText;
    public Button ClaimButton => claimButton;
    public Button AcceptButton => acceptButton;
    public Button TrackButton => trackButton;
    public TMP_Text TrackButtonLabel => trackButtonLabel;

    public void ConfigureReferences(
        RectTransform rectTransform,
        TMP_Text placeholder,
        TMP_Text title,
        TMP_Text status,
        TMP_Text description,
        RectTransform objectives,
        TMP_Text rewards,
        TMP_Text turnInHint,
        Button claim,
        Button accept,
        Button track,
        TMP_Text trackLabel)
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
