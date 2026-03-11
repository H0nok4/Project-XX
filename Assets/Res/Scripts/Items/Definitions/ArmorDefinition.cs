using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Raid/Armor Definition", fileName = "ArmorDefinition")]
public class ArmorDefinition : ItemDefinition
{
    [SerializeField] private List<string> coveredPartIds = new List<string>();
    [Min(1)]
    [SerializeField] private int armorClass = 3;
    [Min(1f)]
    [SerializeField] private float maxDurability = 60f;
    [Range(0f, 1f)]
    [SerializeField] private float bluntDamageMultiplier = 0.18f;
    [Min(0f)]
    [SerializeField] private float blockedDurabilityLossMultiplier = 1.1f;
    [Min(0f)]
    [SerializeField] private float penetratedDurabilityLossMultiplier = 1.45f;
    [Range(0f, 1f)]
    [SerializeField] private float bleedProtection = 0.45f;
    [Range(0f, 1f)]
    [SerializeField] private float fractureProtection = 0.35f;

    public IReadOnlyList<string> CoveredPartIds => coveredPartIds;
    public int ArmorClass => Mathf.Max(1, armorClass);
    public float MaxDurability => GetScaledValue(Mathf.Max(1f, maxDurability));
    public float BluntDamageMultiplier => Mathf.Clamp01(bluntDamageMultiplier);
    public float BlockedDurabilityLossMultiplier => Mathf.Max(0f, blockedDurabilityLossMultiplier);
    public float PenetratedDurabilityLossMultiplier => Mathf.Max(0f, penetratedDurabilityLossMultiplier);
    public float BleedProtection => Mathf.Clamp01(bleedProtection);
    public float FractureProtection => Mathf.Clamp01(fractureProtection);

    public void ConfigureArmor(
        string id,
        string nameLabel,
        string itemDescription,
        float weight,
        int armorLevel,
        float durability,
        float bluntDamage,
        float blockedLoss,
        float penetratedLoss,
        float bleedGuard,
        float fractureGuard,
        params string[] coverage)
    {
        Configure(id, nameLabel, itemDescription, 1, weight);
        armorClass = Mathf.Max(1, armorLevel);
        maxDurability = Mathf.Max(1f, durability);
        bluntDamageMultiplier = Mathf.Clamp01(bluntDamage);
        blockedDurabilityLossMultiplier = Mathf.Max(0f, blockedLoss);
        penetratedDurabilityLossMultiplier = Mathf.Max(0f, penetratedLoss);
        bleedProtection = Mathf.Clamp01(bleedGuard);
        fractureProtection = Mathf.Clamp01(fractureGuard);
        coveredPartIds = new List<string>();

        if (coverage != null)
        {
            foreach (string coveredPartId in coverage)
            {
                string normalizedPartId = NormalizePartId(coveredPartId);
                if (!string.IsNullOrWhiteSpace(normalizedPartId) && !coveredPartIds.Contains(normalizedPartId))
                {
                    coveredPartIds.Add(normalizedPartId);
                }
            }
        }
    }

    public bool CoversPart(string partId)
    {
        string normalizedPartId = NormalizePartId(partId);
        if (string.IsNullOrWhiteSpace(normalizedPartId))
        {
            return false;
        }

        foreach (string coveredPartId in coveredPartIds)
        {
            if (string.Equals(NormalizePartId(coveredPartId), normalizedPartId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void OnValidate()
    {
        armorClass = Mathf.Max(1, armorClass);
        maxDurability = Mathf.Max(1f, maxDurability);
        bluntDamageMultiplier = Mathf.Clamp01(bluntDamageMultiplier);
        blockedDurabilityLossMultiplier = Mathf.Max(0f, blockedDurabilityLossMultiplier);
        penetratedDurabilityLossMultiplier = Mathf.Max(0f, penetratedDurabilityLossMultiplier);
        bleedProtection = Mathf.Clamp01(bleedProtection);
        fractureProtection = Mathf.Clamp01(fractureProtection);

        if (coveredPartIds == null)
        {
            coveredPartIds = new List<string>();
            return;
        }

        var seenPartIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = coveredPartIds.Count - 1; index >= 0; index--)
        {
            string normalizedPartId = NormalizePartId(coveredPartIds[index]);
            if (string.IsNullOrWhiteSpace(normalizedPartId) || !seenPartIds.Add(normalizedPartId))
            {
                coveredPartIds.RemoveAt(index);
                continue;
            }

            coveredPartIds[index] = normalizedPartId;
        }
    }

    private static string NormalizePartId(string partId)
    {
        return PrototypeUnitDefinition.NormalizePartId(partId);
    }
}
