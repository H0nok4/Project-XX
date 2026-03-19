using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StudentIdCardNameEntryWindowTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private RectTransform footerRoot;
    [SerializeField] private Text fieldLabelText;
    [SerializeField] private InputField nameInputField;
    [SerializeField] private Text inputText;
    [SerializeField] private Text placeholderText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public RectTransform Panel => panel;
    public Text TitleText => titleText;
    public Text SubtitleText => subtitleText;
    public RectTransform BodyRoot => bodyRoot;
    public RectTransform FooterRoot => footerRoot;
    public Text FieldLabelText => fieldLabelText;
    public InputField NameInputField => nameInputField;
    public Text InputText => inputText;
    public Text PlaceholderText => placeholderText;
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
        Text title,
        Text subtitle,
        RectTransform body,
        RectTransform footer,
        Text fieldLabel,
        InputField inputField,
        Text inputValueText,
        Text placeholderValueText,
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
