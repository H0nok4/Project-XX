using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DialogueWindowViewTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private RectTransform footerRoot;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private RectTransform optionsRoot;
    [SerializeField] private TMP_Text emptyOptionsText;
    [SerializeField] private Button closeButton;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public RectTransform Panel => panel;
    public TMP_Text TitleText => titleText;
    public TMP_Text SubtitleText => subtitleText;
    public RectTransform BodyRoot => bodyRoot;
    public RectTransform FooterRoot => footerRoot;
    public TMP_Text SpeakerText => speakerText;
    public TMP_Text DialogueText => dialogueText;
    public RectTransform OptionsRoot => optionsRoot;
    public TMP_Text EmptyOptionsText => emptyOptionsText;
    public Button CloseButton => closeButton;

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
        TMP_Text speaker,
        TMP_Text dialogue,
        RectTransform options,
        TMP_Text emptyOptions,
        Button close)
    {
        root = rectTransform;
        panel = windowPanel;
        titleText = title;
        subtitleText = subtitle;
        bodyRoot = body;
        footerRoot = footer;
        speakerText = speaker;
        dialogueText = dialogue;
        optionsRoot = options;
        emptyOptionsText = emptyOptions;
        closeButton = close;
    }
}
