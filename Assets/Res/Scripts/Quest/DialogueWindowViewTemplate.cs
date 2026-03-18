using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DialogueWindowViewTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private RectTransform footerRoot;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private RectTransform optionsRoot;
    [SerializeField] private Button closeButton;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public RectTransform Panel => panel;
    public Text TitleText => titleText;
    public Text SubtitleText => subtitleText;
    public RectTransform BodyRoot => bodyRoot;
    public RectTransform FooterRoot => footerRoot;
    public Text SpeakerText => speakerText;
    public Text DialogueText => dialogueText;
    public RectTransform OptionsRoot => optionsRoot;
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
        Text title,
        Text subtitle,
        RectTransform body,
        RectTransform footer,
        Text speaker,
        Text dialogue,
        RectTransform options,
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
        closeButton = close;
    }
}
