using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RaidReturnButtonTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Button Button => button;
    public TMP_Text LabelText => labelText;

    public void ConfigureReferences(RectTransform rectTransform, Button buttonComponent, TMP_Text label)
    {
        root = rectTransform;
        button = buttonComponent;
        labelText = label;
    }
}
