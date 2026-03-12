using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Raid/Affix Definition", fileName = "AffixDefinition")]
public class AffixDefinition : ScriptableObject
{
    [SerializeField] private AffixType type = AffixType.DamageBonus;
    [SerializeField] private AffixCategory category = AffixCategory.Offensive;
    [SerializeField] private AffixValueKind valueKind = AffixValueKind.Percent;
    [SerializeField] private string displayName = "Damage Bonus";
    [SerializeField] private float minValue = 0.05f;
    [SerializeField] private float maxValue = 0.25f;
    [Min(1)]
    [SerializeField] private int minTier = 1;
    [Min(1)]
    [SerializeField] private int maxTier = 3;
    [Min(0f)]
    [SerializeField] private float weight = 1f;
    [SerializeField] private AffixItemTarget allowedTargets = AffixItemTarget.Any;
    [SerializeField] private string groupId = string.Empty;

    public AffixType Type => type;
    public AffixCategory Category => category;
    public AffixValueKind ValueKind => valueKind;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? type.ToString() : displayName.Trim();
    public float MinValue => minValue;
    public float MaxValue => maxValue;
    public int MinTier => Mathf.Max(1, minTier);
    public int MaxTier => Mathf.Max(MinTier, maxTier);
    public float Weight => Mathf.Max(0f, weight);
    public AffixItemTarget AllowedTargets => allowedTargets;
    public string GroupId => groupId;

    public bool SupportsTarget(AffixItemTarget target)
    {
        return (allowedTargets & target) != 0;
    }


    public void ConfigureRuntime(
        AffixType type,
        AffixCategory category,
        AffixValueKind valueKind,
        string displayName,
        float minValue,
        float maxValue,
        int minTier,
        int maxTier,
        float weight,
        AffixItemTarget allowedTargets,
        string groupId)
    {
        this.type = type;
        this.category = category;
        this.valueKind = valueKind;
        this.displayName = displayName;
        this.minValue = minValue;
        this.maxValue = maxValue;
        this.minTier = minTier;
        this.maxTier = maxTier;
        this.weight = weight;
        this.allowedTargets = allowedTargets;
        this.groupId = groupId ?? string.Empty;
        OnValidate();
    }

    private void OnValidate()
    {
        minTier = Mathf.Max(1, minTier);
        maxTier = Mathf.Max(minTier, maxTier);
        weight = Mathf.Max(0f, weight);
        if (maxValue < minValue)
        {
            float swap = minValue;
            minValue = maxValue;
            maxValue = swap;
        }

        displayName = string.IsNullOrWhiteSpace(displayName) ? type.ToString() : displayName.Trim();
        groupId = groupId ?? string.Empty;
    }
}
