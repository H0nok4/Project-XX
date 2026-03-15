using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class PrototypeThrowableProjectile : MonoBehaviour
{
    [SerializeField] private float fallbackFuseSeconds = 2.5f;
    [SerializeField] private float fallbackExplosionRadius = 4f;
    [SerializeField] private float fallbackExplosionForce = 10f;
    [SerializeField] private float fallbackNoiseRadius = 24f;
    [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;

    private ItemInstance sourceItem;
    private PrototypeWeaponDefinition sourceDefinition;
    private PrototypeUnitVitals.DamageInfo baseDamageInfo;
    private PrototypeUnitVitals ownerVitals;
    private PlayerSkillManager sourceSkillManager;
    private float armTime;
    private bool exploded;

    public void Configure(
        ItemInstance throwableItem,
        PrototypeUnitVitals.DamageInfo damageInfo,
        PrototypeUnitVitals owner,
        PlayerSkillManager skillManager,
        LayerMask explosionMask)
    {
        sourceItem = throwableItem != null ? throwableItem.Clone() : null;
        sourceDefinition = sourceItem != null ? sourceItem.WeaponDefinition : null;
        baseDamageInfo = damageInfo;
        ownerVitals = owner;
        sourceSkillManager = skillManager;
        hitMask = explosionMask.value != 0 ? explosionMask : Physics.DefaultRaycastLayers;
        armTime = Time.time;
        EnsureSettings();
    }

    private void Awake()
    {
        EnsureSettings();
    }

    private void Update()
    {
        if (!exploded && Time.time >= armTime + GetFuseSeconds())
        {
            Explode();
        }
    }

    private void OnCollisionEnter(Collision _)
    {
        if (!exploded && GetFuseSeconds() <= 0.05f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (exploded)
        {
            return;
        }

        exploded = true;
        Vector3 center = transform.position;
        float explosionRadius = GetExplosionRadius();

        if (GetNoiseRadius() > 0f)
        {
            PrototypeCombatNoiseSystem.ReportNoise(center, GetNoiseRadius(), ownerVitals != null ? ownerVitals.gameObject : gameObject);
        }

        Collider[] overlaps = Physics.OverlapSphere(center, explosionRadius, hitMask, QueryTriggerInteraction.Collide);
        var unitTargets = new Dictionary<PrototypeUnitVitals, TargetHit>();
        var breakableTargets = new Dictionary<PrototypeBreakable, TargetHit>();
        var rigidbodies = new HashSet<Rigidbody>();

        for (int index = 0; index < overlaps.Length; index++)
        {
            Collider overlap = overlaps[index];
            if (overlap == null)
            {
                continue;
            }

            Vector3 closestPoint = overlap.ClosestPoint(center);
            float distance = Vector3.Distance(center, closestPoint);
            if (distance > explosionRadius + 0.01f)
            {
                continue;
            }

            if (overlap.attachedRigidbody != null)
            {
                rigidbodies.Add(overlap.attachedRigidbody);
            }

            PrototypeUnitHitbox hitbox = overlap.GetComponent<PrototypeUnitHitbox>() ?? overlap.GetComponentInParent<PrototypeUnitHitbox>();
            if (hitbox != null && hitbox.Owner != null)
            {
                RegisterCloserHit(unitTargets, hitbox.Owner, hitbox, closestPoint, distance);
                continue;
            }

            PrototypeBreakable breakable = overlap.GetComponent<PrototypeBreakable>() ?? overlap.GetComponentInParent<PrototypeBreakable>();
            if (breakable != null)
            {
                RegisterCloserHit(breakableTargets, breakable, null, closestPoint, distance);
            }
        }

        foreach (KeyValuePair<PrototypeUnitVitals, TargetHit> pair in unitTargets)
        {
            PrototypeUnitVitals targetVitals = pair.Key;
            TargetHit hit = pair.Value;
            if (targetVitals == null || hit.UnitHitbox == null)
            {
                continue;
            }

            PrototypeUnitVitals.DamageInfo damageInfo = BuildScaledDamageInfo(hit.Distance, explosionRadius);
            if (damageInfo.damage <= 0f)
            {
                continue;
            }

            bool wasDead = targetVitals.IsDead;
            hit.UnitHitbox.ApplyDamage(damageInfo);
            sourceSkillManager?.HandleDamageResolved(targetVitals, damageInfo, !wasDead && targetVitals.IsDead);
        }

        foreach (KeyValuePair<PrototypeBreakable, TargetHit> pair in breakableTargets)
        {
            PrototypeBreakable breakable = pair.Key;
            TargetHit hit = pair.Value;
            if (breakable == null)
            {
                continue;
            }

            PrototypeUnitVitals.DamageInfo damageInfo = BuildScaledDamageInfo(hit.Distance, explosionRadius);
            if (damageInfo.damage <= 0f)
            {
                continue;
            }

            Vector3 forceDirection = (hit.Point - center).sqrMagnitude > 0.0001f
                ? (hit.Point - center).normalized
                : Vector3.up;
            breakable.ApplyDamage(damageInfo, hit.Point, forceDirection, GetExplosionForce() * Mathf.Clamp01(1f - hit.Distance / explosionRadius));
        }

        foreach (Rigidbody rigidbody in rigidbodies)
        {
            if (rigidbody != null)
            {
                rigidbody.AddExplosionForce(GetExplosionForce(), center, explosionRadius, 0.2f, ForceMode.Impulse);
            }
        }

        SpawnExplosionMarker(center, explosionRadius);
        Destroy(gameObject);
    }

    private PrototypeUnitVitals.DamageInfo BuildScaledDamageInfo(float distance, float explosionRadius)
    {
        float normalizedDistance = explosionRadius > 0.01f ? Mathf.Clamp01(distance / explosionRadius) : 0f;
        float falloff = 1f - normalizedDistance;
        var damageInfo = baseDamageInfo;
        damageInfo.damage = Mathf.Max(0f, baseDamageInfo.damage * falloff);
        damageInfo.armorDamage = Mathf.Max(0f, baseDamageInfo.armorDamage * Mathf.Lerp(0.35f, 1f, falloff));
        damageInfo.penetrationPower = Mathf.Max(0f, baseDamageInfo.penetrationPower * Mathf.Lerp(0.5f, 1f, falloff));
        return damageInfo;
    }

    private float GetFuseSeconds()
    {
        return sourceDefinition != null ? sourceDefinition.FuseSeconds : fallbackFuseSeconds;
    }

    private float GetExplosionRadius()
    {
        return sourceDefinition != null ? sourceDefinition.ExplosionRadius : fallbackExplosionRadius;
    }

    private float GetExplosionForce()
    {
        return sourceDefinition != null ? sourceDefinition.ExplosionForce : fallbackExplosionForce;
    }

    private float GetNoiseRadius()
    {
        return sourceDefinition != null ? sourceDefinition.ExplosionNoiseRadius : fallbackNoiseRadius;
    }

    private void EnsureSettings()
    {
        fallbackFuseSeconds = Mathf.Max(0.05f, fallbackFuseSeconds);
        fallbackExplosionRadius = Mathf.Max(0.5f, fallbackExplosionRadius);
        fallbackExplosionForce = Mathf.Max(0f, fallbackExplosionForce);
        fallbackNoiseRadius = Mathf.Max(0f, fallbackNoiseRadius);
        if (hitMask.value == 0 || hitMask.value == ~0)
        {
            hitMask = Physics.DefaultRaycastLayers;
        }
    }

    private static void RegisterCloserHit<TKey>(
        Dictionary<TKey, TargetHit> targets,
        TKey key,
        PrototypeUnitHitbox unitHitbox,
        Vector3 point,
        float distance)
    {
        if (targets == null || ReferenceEquals(key, null))
        {
            return;
        }

        if (targets.TryGetValue(key, out TargetHit existing) && existing.Distance <= distance)
        {
            return;
        }

        targets[key] = new TargetHit
        {
            UnitHitbox = unitHitbox,
            Point = point,
            Distance = distance
        };
    }

    private static void SpawnExplosionMarker(Vector3 center, float explosionRadius)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "ThrowableExplosionMarker";
        marker.transform.position = center;
        marker.transform.localScale = Vector3.one * Mathf.Max(0.35f, explosionRadius * 0.35f);
        Object.Destroy(marker.GetComponent<Collider>());

        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            renderer.material = new Material(shader);
            renderer.material.color = new Color(1f, 0.48f, 0.14f, 0.85f);
        }

        Object.Destroy(marker, 0.18f);
    }

    private struct TargetHit
    {
        public PrototypeUnitHitbox UnitHitbox;
        public Vector3 Point;
        public float Distance;
    }
}
