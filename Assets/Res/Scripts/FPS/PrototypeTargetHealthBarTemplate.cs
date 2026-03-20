using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PrototypeTargetHealthBarTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text levelLabelText;
    [SerializeField] private RectTransform borderRect;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image fillImage;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public TMP_Text LevelLabelText => levelLabelText;
    public RectTransform BorderRect => borderRect;
    public Image BackgroundImage => backgroundImage;
    public Image FillImage => fillImage;

    public void ConfigureReferences(
        RectTransform rectTransform,
        TMP_Text levelLabel,
        RectTransform border,
        Image background,
        Image fill)
    {
        root = rectTransform;
        levelLabelText = levelLabel;
        borderRect = border;
        backgroundImage = background;
        fillImage = fill;
    }
}
