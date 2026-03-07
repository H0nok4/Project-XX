using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Collider))]
public class PrototypeUnitHitbox : MonoBehaviour
{
    [SerializeField] private PrototypeUnitVitals owner;
    [SerializeField] private string partId = string.Empty;
    [SerializeField] private string passthroughPartId = string.Empty;
    [FormerlySerializedAs("bodyPart")]
    [SerializeField] private PrototypeBodyPartType legacyBodyPart = PrototypeBodyPartType.Torso;

    public PrototypeUnitVitals Owner => owner;
    public string PartId => NormalizePartId(partId);
    public string PassthroughPartId => NormalizePartId(passthroughPartId);

    private void Awake()
    {
        ResolveOwner();
        UpgradeLegacyData();
        EnsureTriggerCollider();
    }

    private void Reset()
    {
        ResolveOwner();
        UpgradeLegacyData();
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        ResolveOwner();
        UpgradeLegacyData();
        EnsureTriggerCollider();
    }

    public void Configure(PrototypeUnitVitals ownerVitals, string targetPartId, string fallbackPartId = "")
    {
        owner = ownerVitals;
        partId = NormalizePartId(targetPartId);
        passthroughPartId = NormalizePartId(fallbackPartId);

        if (string.IsNullOrWhiteSpace(partId) && !string.IsNullOrWhiteSpace(passthroughPartId))
        {
            partId = passthroughPartId;
            passthroughPartId = string.Empty;
        }

        EnsureTriggerCollider();
    }

    public void Configure(PrototypeUnitVitals ownerVitals, PrototypeBodyPartType bodyPartType)
    {
        legacyBodyPart = bodyPartType;
        Configure(ownerVitals, MapLegacyPartId(bodyPartType));
    }

    public void ApplyDamage(float damage)
    {
        ApplyDamage(PrototypeUnitVitals.DamageInfo.CreateDefault(damage));
    }

    public void ApplyDamage(PrototypeUnitVitals.DamageInfo damageInfo)
    {
        if (damageInfo.damage <= 0f)
        {
            return;
        }

        if (owner == null)
        {
            ResolveOwner();
        }

        UpgradeLegacyData();

        if (owner == null)
        {
            return;
        }

        string damagePartId = ResolveDamageTargetPartId();
        if (!string.IsNullOrWhiteSpace(damagePartId))
        {
            owner.ApplyDamage(damagePartId, damageInfo);
        }
    }

    private string ResolveDamageTargetPartId()
    {
        string directPartId = PartId;
        string redirectedPartId = PassthroughPartId;

        if (string.IsNullOrWhiteSpace(directPartId))
        {
            directPartId = MapLegacyPartId(legacyBodyPart);
        }

        if (owner == null)
        {
            return directPartId;
        }

        if (!string.IsNullOrWhiteSpace(directPartId) && owner.HasPart(directPartId))
        {
            if (!owner.IsPartDestroyed(directPartId) || string.IsNullOrWhiteSpace(redirectedPartId))
            {
                return directPartId;
            }

            if (owner.HasPart(redirectedPartId))
            {
                return redirectedPartId;
            }

            return directPartId;
        }

        if (!string.IsNullOrWhiteSpace(redirectedPartId) && owner.HasPart(redirectedPartId))
        {
            return redirectedPartId;
        }

        return directPartId;
    }

    private void ResolveOwner()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<PrototypeUnitVitals>();
        }
    }

    private void UpgradeLegacyData()
    {
        if (string.IsNullOrWhiteSpace(partId))
        {
            partId = MapLegacyPartId(legacyBodyPart);
        }

        partId = NormalizePartId(partId);
        passthroughPartId = NormalizePartId(passthroughPartId);
    }

    private void EnsureTriggerCollider()
    {
        Collider hitboxCollider = GetComponent<Collider>();
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
        }
    }

    private static string NormalizePartId(string candidatePartId)
    {
        return PrototypeUnitDefinition.NormalizePartId(candidatePartId);
    }

    private static string MapLegacyPartId(PrototypeBodyPartType bodyPart)
    {
        switch (bodyPart)
        {
            case PrototypeBodyPartType.Head:
                return "head";
            case PrototypeBodyPartType.Torso:
                return "torso";
            case PrototypeBodyPartType.Legs:
                return "legs";
            default:
                return string.Empty;
        }
    }
}
