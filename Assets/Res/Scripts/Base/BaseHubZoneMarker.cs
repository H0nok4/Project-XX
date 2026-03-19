using UnityEngine;

public enum BaseHubZoneType
{
    Arrival = 0,
    ReadyRoom = 1,
    Warehouse = 2,
    Merchants = 3,
    Missions = 4,
    Recovery = 5
}

[DisallowMultipleComponent]
public sealed class BaseHubZoneMarker : MonoBehaviour
{
    [SerializeField] private BaseHubZoneType zoneType = BaseHubZoneType.Arrival;
    [SerializeField] private string zoneName = "中枢大厅";
    [TextArea(2, 4)]
    [SerializeField] private string zoneSummary = "基地主通路和功能分发区。";
    [SerializeField] private string questExploreLocationId = string.Empty;
    [SerializeField] private float guidanceRadius = 5f;

    public BaseHubZoneType ZoneType => zoneType;
    public string ZoneName => string.IsNullOrWhiteSpace(zoneName) ? zoneType.ToString() : zoneName.Trim();
    public string ZoneSummary => string.IsNullOrWhiteSpace(zoneSummary) ? string.Empty : zoneSummary.Trim();
    public string QuestExploreLocationId => ResolveQuestExploreLocationId();
    public float GuidanceRadius => Mathf.Max(1f, guidanceRadius);

    public float GetPlanarDistanceSqr(Vector3 worldPosition)
    {
        Vector3 delta = worldPosition - transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude;
    }

    public bool Contains(Vector3 worldPosition)
    {
        float radius = GuidanceRadius;
        return GetPlanarDistanceSqr(worldPosition) <= radius * radius;
    }

    private void OnValidate()
    {
        guidanceRadius = Mathf.Max(1f, guidanceRadius);
        zoneName = zoneName ?? string.Empty;
        zoneSummary = zoneSummary ?? string.Empty;
        questExploreLocationId = questExploreLocationId ?? string.Empty;
    }

    private string ResolveQuestExploreLocationId()
    {
        if (!string.IsNullOrWhiteSpace(questExploreLocationId))
        {
            return questExploreLocationId.Trim();
        }

        switch (zoneType)
        {
            case BaseHubZoneType.Warehouse:
                return "base_warehouse";

            default:
                return string.Empty;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = GetGizmoColor(zoneType);
        Vector3 center = transform.position + Vector3.up * 0.05f;
        Gizmos.DrawWireSphere(center, GuidanceRadius);
        Gizmos.DrawSphere(center, 0.12f);
    }

    private static Color GetGizmoColor(BaseHubZoneType type)
    {
        switch (type)
        {
            case BaseHubZoneType.ReadyRoom:
                return new Color(0.22f, 0.74f, 0.96f, 0.8f);

            case BaseHubZoneType.Warehouse:
                return new Color(0.26f, 0.68f, 0.42f, 0.8f);

            case BaseHubZoneType.Merchants:
                return new Color(0.92f, 0.56f, 0.18f, 0.8f);

            case BaseHubZoneType.Missions:
                return new Color(0.76f, 0.42f, 0.9f, 0.8f);

            case BaseHubZoneType.Recovery:
                return new Color(0.84f, 0.92f, 0.98f, 0.8f);

            default:
                return new Color(0.92f, 0.9f, 0.52f, 0.8f);
        }
    }
#endif
}
