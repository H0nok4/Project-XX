using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RaidHudTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private RectTransform extractionRoot;
    [SerializeField] private TMP_Text extractionText;
    [SerializeField] private RectTransform extractionTrack;
    [SerializeField] private Image extractionFillImage;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public TMP_Text SummaryText => summaryText;
    public RectTransform ExtractionRoot => extractionRoot;
    public TMP_Text ExtractionText => extractionText;
    public RectTransform ExtractionTrack => extractionTrack;
    public Image ExtractionFillImage => extractionFillImage;

    public void ConfigureReferences(
        RectTransform rectTransform,
        TMP_Text summary,
        RectTransform extraction,
        TMP_Text extractionLabel,
        RectTransform track,
        Image fillImage)
    {
        root = rectTransform;
        summaryText = summary;
        extractionRoot = extraction;
        extractionText = extractionLabel;
        extractionTrack = track;
        extractionFillImage = fillImage;
    }
}
