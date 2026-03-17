using UnityEngine;

public enum BaseHubMerchantSpotType
{
    Weapon = 0,
    Armor = 1,
    Medical = 2,
    General = 3
}

[DisallowMultipleComponent]
public sealed class BaseHubMerchantSpot : MonoBehaviour
{
    [SerializeField] private BaseHubMerchantSpotType spotType = BaseHubMerchantSpotType.Weapon;
    [SerializeField] private string merchantId = "weapons_trader";
    [SerializeField] private string merchantName = "武器商人";
    [TextArea(1, 3)]
    [SerializeField] private string previewDescription = "出售武器、弹药和基础配件。";
    [SerializeField] private Transform standAnchor;

    public BaseHubMerchantSpotType SpotType => spotType;
    public string MerchantId => string.IsNullOrWhiteSpace(merchantId) ? string.Empty : merchantId.Trim();
    public string MerchantName => string.IsNullOrWhiteSpace(merchantName) ? "商人" : merchantName.Trim();
    public string PreviewDescription => string.IsNullOrWhiteSpace(previewDescription) ? string.Empty : previewDescription.Trim();
    public Transform StandAnchor => standAnchor != null ? standAnchor : transform;

    private void Reset()
    {
        standAnchor = transform;
    }

    private void OnValidate()
    {
        standAnchor ??= transform;
        merchantId = merchantId ?? string.Empty;
        merchantName = merchantName ?? string.Empty;
        previewDescription = previewDescription ?? string.Empty;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform anchor = StandAnchor;
        Vector3 anchorPosition = anchor != null ? anchor.position : transform.position;
        Gizmos.color = GetGizmoColor(spotType);
        Gizmos.DrawWireCube(anchorPosition + Vector3.up * 0.9f, new Vector3(0.7f, 1.8f, 0.7f));
        Gizmos.DrawRay(anchorPosition + Vector3.up * 1.2f, StandAnchor.forward * 1.2f);
    }

    private static Color GetGizmoColor(BaseHubMerchantSpotType type)
    {
        switch (type)
        {
            case BaseHubMerchantSpotType.Armor:
                return new Color(0.2f, 0.74f, 0.96f, 0.8f);

            case BaseHubMerchantSpotType.Medical:
                return new Color(0.52f, 0.9f, 0.64f, 0.8f);

            case BaseHubMerchantSpotType.General:
                return new Color(0.95f, 0.86f, 0.46f, 0.8f);

            default:
                return new Color(0.92f, 0.52f, 0.18f, 0.8f);
        }
    }
#endif
}
