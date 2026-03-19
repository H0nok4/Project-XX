using System;
using UnityEngine;

[Serializable]
public sealed class StudentIdCardContent
{
    [SerializeField] private string schoolName = "私立星昇高等学校";
    [SerializeField] private string cardTitle = "学生证";
    [SerializeField] private string studentNumberLabel = "学籍编号";
    [SerializeField] private string studentNumber = "170009220";
    [SerializeField] private string nameLabel = "姓名";
    [SerializeField] private string fullName = "星野 玲";
    [SerializeField] private string birthDateLabel = "生年月日";
    [SerializeField] private string birthDate = "2006年 04月 01日";
    [SerializeField] private string issueDateLabel = "发行日";
    [SerializeField] private string issueDate = "2026年 04月 01日";
    [TextArea(2, 4)]
    [SerializeField] private string noteText = "上记之人为本校在册学生。";
    [SerializeField] private string issuerName = "私立星昇高等学校";
    [SerializeField] private Sprite portraitSprite;
    [SerializeField] private Sprite logoSprite;
    [SerializeField] private Sprite sealSprite;
    [SerializeField] private Color accentColor = new Color(0.49f, 0.91f, 0.94f, 1f);

    public string SchoolName
    {
        get => schoolName;
        set => schoolName = value;
    }

    public string CardTitle
    {
        get => cardTitle;
        set => cardTitle = value;
    }

    public string StudentNumberLabel
    {
        get => studentNumberLabel;
        set => studentNumberLabel = value;
    }

    public string StudentNumber
    {
        get => studentNumber;
        set => studentNumber = value;
    }

    public string NameLabel
    {
        get => nameLabel;
        set => nameLabel = value;
    }

    public string FullName
    {
        get => fullName;
        set => fullName = value;
    }

    public string BirthDateLabel
    {
        get => birthDateLabel;
        set => birthDateLabel = value;
    }

    public string BirthDate
    {
        get => birthDate;
        set => birthDate = value;
    }

    public string IssueDateLabel
    {
        get => issueDateLabel;
        set => issueDateLabel = value;
    }

    public string IssueDate
    {
        get => issueDate;
        set => issueDate = value;
    }

    public string NoteText
    {
        get => noteText;
        set => noteText = value;
    }

    public string IssuerName
    {
        get => issuerName;
        set => issuerName = value;
    }

    public Sprite PortraitSprite
    {
        get => portraitSprite;
        set => portraitSprite = value;
    }

    public Sprite LogoSprite
    {
        get => logoSprite;
        set => logoSprite = value;
    }

    public Sprite SealSprite
    {
        get => sealSprite;
        set => sealSprite = value;
    }

    public Color AccentColor
    {
        get => accentColor;
        set => accentColor = value;
    }

    public void Sanitize()
    {
        schoolName = TrimOrDefault(schoolName, "私立星昇高等学校");
        cardTitle = TrimOrDefault(cardTitle, "学生证");
        studentNumberLabel = TrimOrDefault(studentNumberLabel, "学籍编号");
        studentNumber = TrimOrDefault(studentNumber, "170009220");
        nameLabel = TrimOrDefault(nameLabel, "姓名");
        fullName = fullName != null ? fullName.Trim() : string.Empty;
        birthDateLabel = TrimOrDefault(birthDateLabel, "生年月日");
        birthDate = TrimOrDefault(birthDate, "2006年 04月 01日");
        issueDateLabel = TrimOrDefault(issueDateLabel, "发行日");
        issueDate = TrimOrDefault(issueDate, "2026年 04月 01日");
        noteText = TrimOrDefault(noteText, "上记之人为本校在册学生。");
        issuerName = TrimOrDefault(issuerName, "私立星昇高等学校");
        accentColor.a = 1f;
    }

    private static string TrimOrDefault(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
