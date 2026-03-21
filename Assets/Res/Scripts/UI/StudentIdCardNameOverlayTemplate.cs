using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StudentIdCardNameOverlayTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private TMP_Text nameValueText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Canvas WorldCanvas => worldCanvas;
    public TMP_Text NameValueText => nameValueText;

    public void ConfigureReferences(RectTransform rectTransform, Canvas canvasComponent, TMP_Text nameText)
    {
        root = rectTransform;
        worldCanvas = canvasComponent;
        nameValueText = nameText;
    }

    public void Apply(string fullName, Font font)
    {
        if (nameValueText == null)
        {
            return;
        }

        if (font != null)
        {
            PrototypeUiToolkit.ApplyTmpFont(nameValueText, font);
        }

        string sanitizedName = string.IsNullOrWhiteSpace(fullName) ? string.Empty : fullName.Trim();
        bool hasName = !string.IsNullOrEmpty(sanitizedName);
        nameValueText.gameObject.SetActive(hasName);
        nameValueText.text = sanitizedName;

        RectTransform rectTransform = Root;
        if (rectTransform != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
}
