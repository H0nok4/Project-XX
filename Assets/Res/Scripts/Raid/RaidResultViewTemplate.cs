using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RaidResultViewTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public TMP_Text TitleText => titleText;
    public TMP_Text BodyText => bodyText;

    public void ConfigureReferences(RectTransform rectTransform, TMP_Text title, TMP_Text body)
    {
        root = rectTransform;
        titleText = title;
        bodyText = body;
    }
}
