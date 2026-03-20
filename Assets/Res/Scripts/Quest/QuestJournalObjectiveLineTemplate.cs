using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class QuestJournalObjectiveLineTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Text labelText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public Text LabelText => labelText;

    public void ConfigureReferences(RectTransform rectTransform, Text label)
    {
        root = rectTransform;
        labelText = label;
    }
}
