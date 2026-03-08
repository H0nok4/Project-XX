using System.Collections.Generic;
using UnityEngine;

public static class PrototypeEnemyRuntimeFactory
{
    private const int IgnoreRaycastLayer = 2;

    public static PrototypeBotController SpawnEnemy(
        PrototypeEnemySpawnProfile profile,
        Transform combatTarget,
        Vector3 position,
        Quaternion rotation,
        Transform parent,
        IList<Vector3> patrolPoints,
        PrototypeUnitDefinition fallbackUnitDefinition = null,
        string overrideName = null)
    {
        if (profile == null)
        {
            return null;
        }

        PrototypeUnitDefinition unitDefinition = profile.UnitDefinition != null ? profile.UnitDefinition : fallbackUnitDefinition;
        if (unitDefinition == null)
        {
            return null;
        }

        GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyObject.name = string.IsNullOrWhiteSpace(overrideName) ? profile.DisplayName : overrideName.Trim();
        enemyObject.transform.SetParent(parent, false);
        enemyObject.transform.position = position;
        enemyObject.transform.rotation = rotation;
        enemyObject.transform.localScale = profile.LocalScale;
        enemyObject.layer = IgnoreRaycastLayer;

        Renderer renderer = enemyObject.GetComponent<Renderer>();
        if (renderer != null && profile.BodyMaterial != null)
        {
            renderer.sharedMaterial = profile.BodyMaterial;
        }

        Rigidbody rigidbody = enemyObject.AddComponent<Rigidbody>();
        rigidbody.mass = 1.6f;
        rigidbody.angularDamping = 1.8f;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        PrototypeUnitVitals vitals = enemyObject.AddComponent<PrototypeUnitVitals>();
        vitals.SetUnitDefinition(unitDefinition);

        PrototypeStatusEffectController statusEffects = GetOrAddComponent<PrototypeStatusEffectController>(enemyObject);
        statusEffects.Bind(vitals);
        GetOrAddComponent<PrototypeCombatTextController>(enemyObject);
        vitals.SetAllowImpactForceWhenAlive(false);

        List<ArmorDefinition> armorLoadout = new List<ArmorDefinition>();
        IReadOnlyList<ArmorDefinition> configuredArmor = profile.ArmorLoadout;
        for (int index = 0; index < configuredArmor.Count; index++)
        {
            if (configuredArmor[index] != null)
            {
                armorLoadout.Add(configuredArmor[index]);
            }
        }

        vitals.SetArmorLoadout(armorLoadout.ToArray());
        CreateDefaultHitboxes(enemyObject.transform, vitals);

        PrototypeTargetHealthBar targetHealthBar = enemyObject.AddComponent<PrototypeTargetHealthBar>();
        targetHealthBar.Configure(vitals, null, unitDefinition.HealthBarAnchorPartId);

        PrototypeBotController botController = enemyObject.AddComponent<PrototypeBotController>();
        botController.Configure(
            combatTarget,
            profile.Archetype,
            profile.PrimaryWeapon,
            patrolPoints != null ? new List<Vector3>(patrolPoints).ToArray() : null);

        return botController;
    }

    private static void CreateDefaultHitboxes(Transform parent, PrototypeUnitVitals vitals)
    {
        CreateBodyHitbox(parent, vitals, "Hitbox_Head", "head", new Vector3(0f, 0.82f, 0f), new Vector3(0.42f, 0.42f, 0.42f));
        CreateBodyHitbox(parent, vitals, "Hitbox_Torso", "torso", new Vector3(0f, 0.08f, 0f), new Vector3(0.72f, 0.86f, 0.52f));
        CreateBodyHitbox(parent, vitals, "Hitbox_Legs", "legs", new Vector3(0f, -0.72f, 0f), new Vector3(0.64f, 0.88f, 0.48f));
    }

    private static void CreateBodyHitbox(
        Transform parent,
        PrototypeUnitVitals vitals,
        string name,
        string partId,
        Vector3 localPosition,
        Vector3 colliderSize,
        string passthroughPartId = "")
    {
        GameObject hitbox = new GameObject(name);
        hitbox.transform.SetParent(parent, false);
        hitbox.transform.localPosition = localPosition;
        hitbox.transform.localRotation = Quaternion.identity;

        BoxCollider collider = hitbox.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = colliderSize;

        PrototypeUnitHitbox unitHitbox = hitbox.AddComponent<PrototypeUnitHitbox>();
        unitHitbox.Configure(vitals, partId, passthroughPartId);
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
