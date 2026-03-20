using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class QuestJournalObjectiveLineTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text labelText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public TMP_Text LabelText => labelText;

    public void ConfigureReferences(RectTransform rectTransform, TMP_Text label)
    {
        root = rectTransform;
        labelText = label;
    }
}
