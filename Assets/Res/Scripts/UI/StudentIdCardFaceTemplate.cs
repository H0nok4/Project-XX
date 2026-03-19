using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StudentIdCardFaceTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image topBandImage;
    [SerializeField] private Image logoImage;
    [SerializeField] private Image portraitFrameImage;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image sealImage;
    [SerializeField] private Text schoolNameText;
    [SerializeField] private Text cardTitleText;
    [SerializeField] private Text studentNumberLabelText;
    [SerializeField] private Text studentNumberValueText;
    [SerializeField] private Text nameLabelText;
    [SerializeField] private Text nameValueText;
    [SerializeField] private Text birthDateLabelText;
    [SerializeField] private Text birthDateValueText;
    [SerializeField] private Text issueDateLabelText;
    [SerializeField] private Text issueDateValueText;
    [SerializeField] private Text noteText;
    [SerializeField] private Text issuerText;

    public RectTransform Root => root != null ? root : transform as RectTransform;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image topBand,
        Image logo,
        Image portraitFrame,
        Image portrait,
        Image seal,
        Text schoolName,
        Text cardTitle,
        Text studentNumberLabel,
        Text studentNumberValue,
        Text nameLabel,
        Text nameValue,
        Text birthDateLabel,
        Text birthDateValue,
        Text issueDateLabel,
        Text issueDateValue,
        Text note,
        Text issuer)
    {
        root = rectTransform;
        topBandImage = topBand;
        logoImage = logo;
        portraitFrameImage = portraitFrame;
        portraitImage = portrait;
        sealImage = seal;
        schoolNameText = schoolName;
        cardTitleText = cardTitle;
        studentNumberLabelText = studentNumberLabel;
        studentNumberValueText = studentNumberValue;
        nameLabelText = nameLabel;
        nameValueText = nameValue;
        birthDateLabelText = birthDateLabel;
        birthDateValueText = birthDateValue;
        issueDateLabelText = issueDateLabel;
        issueDateValueText = issueDateValue;
        noteText = note;
        issuerText = issuer;
    }

    public void Apply(StudentIdCardContent content, Font font)
    {
        content ??= new StudentIdCardContent();
        content.Sanitize();
        ApplyFont(font);

        if (topBandImage != null)
        {
            topBandImage.color = content.AccentColor;
        }

        if (portraitFrameImage != null)
        {
            portraitFrameImage.color = new Color(0.95f, 0.95f, 0.96f, 1f);
        }

        SetText(schoolNameText, content.SchoolName);
        SetText(cardTitleText, content.CardTitle);
        SetText(studentNumberLabelText, content.StudentNumberLabel);
        SetText(studentNumberValueText, content.StudentNumber);
        SetText(nameLabelText, content.NameLabel);
        SetText(nameValueText, content.FullName);
        SetText(birthDateLabelText, content.BirthDateLabel);
        SetText(birthDateValueText, content.BirthDate);
        SetText(issueDateLabelText, content.IssueDateLabel);
        SetText(issueDateValueText, content.IssueDate);
        SetText(noteText, content.NoteText);
        SetText(issuerText, content.IssuerName);

        SetSprite(logoImage, content.LogoSprite, Color.white, true);
        SetSprite(portraitImage, content.PortraitSprite, Color.white, true);
        SetSprite(sealImage, content.SealSprite, new Color(0.82f, 0.22f, 0.22f, 0.75f), true);

        if (Root != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(Root);
        }
    }

    private void ApplyFont(Font font)
    {
        if (font == null)
        {
            return;
        }

        SetFont(schoolNameText, font);
        SetFont(cardTitleText, font);
        SetFont(studentNumberLabelText, font);
        SetFont(studentNumberValueText, font);
        SetFont(nameLabelText, font);
        SetFont(nameValueText, font);
        SetFont(birthDateLabelText, font);
        SetFont(birthDateValueText, font);
        SetFont(issueDateLabelText, font);
        SetFont(issueDateValueText, font);
        SetFont(noteText, font);
        SetFont(issuerText, font);
    }

    private static void SetFont(Text target, Font font)
    {
        if (target == null || font == null)
        {
            return;
        }

        target.font = font;
    }

    private static void SetText(Text target, string value)
    {
        if (target == null)
        {
            return;
        }

        target.text = value ?? string.Empty;
    }

    private static void SetSprite(Image target, Sprite sprite, Color tint, bool disableWhenMissing)
    {
        if (target == null)
        {
            return;
        }

        target.sprite = sprite;
        target.color = tint;
        target.enabled = !disableWhenMissing || sprite != null;
    }
}
