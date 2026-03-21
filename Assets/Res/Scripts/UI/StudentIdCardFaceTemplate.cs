using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StudentIdCardFaceTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private bool overridePrefabFontsAtRuntime;
    [SerializeField] private Image topBandImage;
    [SerializeField] private Image logoImage;
    [SerializeField] private Image portraitFrameImage;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image sealImage;
    [SerializeField] private TMP_Text schoolNameText;
    [SerializeField] private TMP_Text cardTitleText;
    [SerializeField] private TMP_Text studentNumberLabelText;
    [SerializeField] private TMP_Text studentNumberValueText;
    [SerializeField] private TMP_Text nameLabelText;
    [SerializeField] private TMP_Text nameValueText;
    [SerializeField] private TMP_Text birthDateLabelText;
    [SerializeField] private TMP_Text birthDateValueText;
    [SerializeField] private TMP_Text issueDateLabelText;
    [SerializeField] private TMP_Text issueDateValueText;
    [SerializeField] private TMP_Text noteText;
    [SerializeField] private TMP_Text issuerText;

    public RectTransform Root => root != null ? root : transform as RectTransform;

    public void ConfigureReferences(
        RectTransform rectTransform,
        Image topBand,
        Image logo,
        Image portraitFrame,
        Image portrait,
        Image seal,
        TMP_Text schoolName,
        TMP_Text cardTitle,
        TMP_Text studentNumberLabel,
        TMP_Text studentNumberValue,
        TMP_Text nameLabel,
        TMP_Text nameValue,
        TMP_Text birthDateLabel,
        TMP_Text birthDateValue,
        TMP_Text issueDateLabel,
        TMP_Text issueDateValue,
        TMP_Text note,
        TMP_Text issuer)
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
        if (overridePrefabFontsAtRuntime)
        {
            ApplyFont(font);
        }

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

    private static void SetFont(TMP_Text target, Font font)
    {
        if (target == null)
        {
            return;
        }

        PrototypeUiToolkit.ApplyTmpFont(target, font);
    }

    private static void SetText(TMP_Text target, string value)
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
