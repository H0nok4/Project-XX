using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StudentIdCardNameEntryWindowTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private RectTransform footerRoot;
    [SerializeField] private TMP_Text fieldLabelText;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_Text inputText;
    [SerializeField] private TMP_Text placeholderText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public RectTransform Panel => panel;
    public TMP_Text TitleText => titleText;
    public TMP_Text SubtitleText => subtitleText;
    public RectTransform BodyRoot => bodyRoot;
    public RectTransform FooterRoot => footerRoot;
    public TMP_Text FieldLabelText => fieldLabelText;
    public TMP_InputField NameInputField => nameInputField;
    public TMP_Text InputText => inputText;
    public TMP_Text PlaceholderText => placeholderText;
    public Button ConfirmButton => confirmButton;
    public Button CancelButton => cancelButton;

    public PrototypeUiToolkit.WindowChrome CreateWindowChrome()
    {
        return new PrototypeUiToolkit.WindowChrome
        {
            Root = Root,
            Panel = panel,
            TitleText = titleText,
            SubtitleText = subtitleText,
            BodyRoot = bodyRoot,
            FooterRoot = footerRoot
        };
    }

    public void ConfigureReferences(
        RectTransform rectTransform,
        RectTransform windowPanel,
        TMP_Text title,
        TMP_Text subtitle,
        RectTransform body,
        RectTransform footer,
        TMP_Text fieldLabel,
        TMP_InputField inputField,
        TMP_Text inputValueText,
        TMP_Text placeholderValueText,
        Button confirm,
        Button cancel)
    {
        root = rectTransform;
        panel = windowPanel;
        titleText = title;
        subtitleText = subtitle;
        bodyRoot = body;
        footerRoot = footer;
        fieldLabelText = fieldLabel;
        nameInputField = inputField;
        inputText = inputValueText;
        placeholderText = placeholderValueText;
        confirmButton = confirm;
        cancelButton = cancel;
    }
}
