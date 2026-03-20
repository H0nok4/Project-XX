using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PrototypeCombatTextEntryTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Shadow shadow;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public TMP_Text LabelText => labelText;
    public Shadow Shadow => shadow;

    public void ConfigureReferences(RectTransform rectTransform, TMP_Text label, Shadow shadowComponent)
    {
        root = rectTransform;
        labelText = label;
        shadow = shadowComponent;
    }
}
