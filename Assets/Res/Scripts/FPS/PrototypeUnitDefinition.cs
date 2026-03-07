using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/FPS/Unit Definition", fileName = "UnitDefinition")]
public class PrototypeUnitDefinition : ScriptableObject
{
    public enum ZeroKillMode
    {
        Never = 0,
        OnAnyDamage = 1,
        OnDirectHitOnly = 2
    }

    [Serializable]
    public class OverflowTarget
    {
        public string partId = string.Empty;
        [Min(0f)] public float weight = 1f;

        public OverflowTarget Clone()
        {
            return new OverflowTarget
            {
                partId = PrototypeUnitDefinition.NormalizePartId(partId),
                weight = Mathf.Max(0f, weight)
            };
        }
    }

    [Serializable]
    public class PartDefinition
    {
        public string partId = "part";
        public string displayName = "Part";
        [Min(1f)] public float maxHealth = 1f;
        [Min(0f)] public float overflowMultiplier = 1f;
        public bool contributesToUnitHealth = true;
        public bool receivesOverflowDamage = true;
        public bool receivesOverflowFollowUpDamage = false;
        public ZeroKillMode zeroKillMode = ZeroKillMode.Never;
        public bool killUnitWhenBlackedAndDamagedAgain = false;
        [Min(0f)] public float blackedFollowUpDamageThreshold = 0f;
        public List<OverflowTarget> overflowTargets = new List<OverflowTarget>();

        public PartDefinition Clone()
        {
            var clone = new PartDefinition
            {
                partId = PrototypeUnitDefinition.NormalizePartId(partId),
                displayName = string.IsNullOrWhiteSpace(displayName) ? PrototypeUnitDefinition.NormalizePartId(partId) : displayName.Trim(),
                maxHealth = Mathf.Max(1f, maxHealth),
                overflowMultiplier = Mathf.Max(0f, overflowMultiplier),
                contributesToUnitHealth = contributesToUnitHealth,
                receivesOverflowDamage = receivesOverflowDamage,
                receivesOverflowFollowUpDamage = receivesOverflowFollowUpDamage,
                zeroKillMode = zeroKillMode,
                killUnitWhenBlackedAndDamagedAgain = killUnitWhenBlackedAndDamagedAgain,
                blackedFollowUpDamageThreshold = Mathf.Max(0f, blackedFollowUpDamageThreshold),
                overflowTargets = new List<OverflowTarget>()
            };

            if (overflowTargets != null)
            {
                foreach (OverflowTarget overflowTarget in overflowTargets)
                {
                    if (overflowTarget == null)
                    {
                        continue;
                    }

                    OverflowTarget clonedTarget = overflowTarget.Clone();
                    if (!string.IsNullOrWhiteSpace(clonedTarget.partId) && clonedTarget.weight > 0f)
                    {
                        clone.overflowTargets.Add(clonedTarget);
                    }
                }
            }

            return clone;
        }
    }

    [SerializeField] private List<PartDefinition> parts = new List<PartDefinition>();
    [SerializeField] private string healthBarAnchorPartId = string.Empty;

    public IReadOnlyList<PartDefinition> Parts => parts;
    public string HealthBarAnchorPartId => healthBarAnchorPartId;

    public PartDefinition GetPart(string partId)
    {
        string normalizedPartId = NormalizePartId(partId);
        if (string.IsNullOrWhiteSpace(normalizedPartId))
        {
            return null;
        }

        foreach (PartDefinition partDefinition in parts)
        {
            if (PartIdEquals(partDefinition.partId, normalizedPartId))
            {
                return partDefinition;
            }
        }

        return null;
    }

    public void SetDefinition(IEnumerable<PartDefinition> sourceParts, string anchorPartId)
    {
        parts = new List<PartDefinition>();

        if (sourceParts != null)
        {
            foreach (PartDefinition sourcePart in sourceParts)
            {
                if (sourcePart == null)
                {
                    continue;
                }

                PartDefinition clonedPart = sourcePart.Clone();
                if (!string.IsNullOrWhiteSpace(clonedPart.partId))
                {
                    parts.Add(clonedPart);
                }
            }
        }

        healthBarAnchorPartId = NormalizePartId(anchorPartId);
        Sanitize();
    }

    private void OnValidate()
    {
        Sanitize();
    }

    private void Sanitize()
    {
        if (parts == null)
        {
            parts = new List<PartDefinition>();
        }

        var seenPartIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = parts.Count - 1; index >= 0; index--)
        {
            PartDefinition partDefinition = parts[index];
            if (partDefinition == null)
            {
                parts.RemoveAt(index);
                continue;
            }

            PartDefinition sanitizedPart = partDefinition.Clone();
            if (string.IsNullOrWhiteSpace(sanitizedPart.partId) || !seenPartIds.Add(sanitizedPart.partId))
            {
                parts.RemoveAt(index);
                continue;
            }

            parts[index] = sanitizedPart;
        }

        healthBarAnchorPartId = NormalizePartId(healthBarAnchorPartId);
        if (string.IsNullOrWhiteSpace(healthBarAnchorPartId) || GetPart(healthBarAnchorPartId) == null)
        {
            healthBarAnchorPartId = parts.Count > 0 ? parts[0].partId : string.Empty;
        }
    }

    private static bool PartIdEquals(string left, string right)
    {
        return string.Equals(NormalizePartId(left), NormalizePartId(right), StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizePartId(string partId)
    {
        return string.IsNullOrWhiteSpace(partId) ? string.Empty : partId.Trim();
    }
}
