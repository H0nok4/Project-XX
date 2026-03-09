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

        PrototypeWeaponDefinition equippedWeapon = profile.ResolvePrimaryWeapon();
        GameObject enemyObject = CreateEnemyObject(profile, position, rotation, parent, overrideName);
        if (enemyObject == null)
        {
            return null;
        }

        enemyObject.transform.localScale = profile.LocalScale;
        ConfigureEnemyLayers(enemyObject.transform);
        ApplyBodyMaterial(enemyObject, profile.BodyMaterial);
        EnsureSolidCollider(enemyObject);

        Rigidbody rigidbody = GetOrAddComponent<Rigidbody>(enemyObject);
        rigidbody.mass = 1.6f;
        rigidbody.angularDamping = 1.8f;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        PrototypeUnitVitals vitals = GetOrAddComponent<PrototypeUnitVitals>(enemyObject);
        vitals.SetUnitDefinition(unitDefinition);
        vitals.SetAllowImpactForceWhenAlive(false);

        PrototypeStatusEffectController statusEffects = GetOrAddComponent<PrototypeStatusEffectController>(enemyObject);
        statusEffects.Bind(vitals);
        GetOrAddComponent<PrototypeCombatTextController>(enemyObject);

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
        EnsureDefaultHitboxes(enemyObject.transform, vitals);

        PrototypeTargetHealthBar targetHealthBar = GetOrAddComponent<PrototypeTargetHealthBar>(enemyObject);
        targetHealthBar.Configure(vitals, null, unitDefinition.HealthBarAnchorPartId);

        PrototypeBotController botController = GetOrAddComponent<PrototypeBotController>(enemyObject);
        botController.Configure(
            combatTarget,
            profile.Archetype,
            equippedWeapon,
            patrolPoints != null ? new List<Vector3>(patrolPoints).ToArray() : null);
        botController.SetCarriedLootTable(profile.CarriedLootTable);

        PrototypeEquippedWeaponVisual equippedWeaponVisual = GetOrAddComponent<PrototypeEquippedWeaponVisual>(enemyObject);
        equippedWeaponVisual.Configure(FindOrCreateWeaponVisualAnchor(enemyObject.transform));
        equippedWeaponVisual.SetEquippedWeapon(equippedWeapon);

        return botController;
    }

    private static GameObject CreateEnemyObject(
        PrototypeEnemySpawnProfile profile,
        Vector3 position,
        Quaternion rotation,
        Transform parent,
        string overrideName)
    {
        GameObject enemyObject;
        if (profile.EnemyPrefab != null)
        {
            enemyObject = Object.Instantiate(profile.EnemyPrefab, position, rotation, parent);
        }
        else
        {
            enemyObject = CreateFallbackEnemyObject(parent);
            enemyObject.transform.SetPositionAndRotation(position, rotation);
        }

        enemyObject.name = string.IsNullOrWhiteSpace(overrideName) ? profile.DisplayName : overrideName.Trim();
        return enemyObject;
    }

    private static GameObject CreateFallbackEnemyObject(Transform parent)
    {
        GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyObject.transform.SetParent(parent, false);
        enemyObject.layer = IgnoreRaycastLayer;
        return enemyObject;
    }

    private static void EnsureSolidCollider(GameObject enemyObject)
    {
        Collider[] colliders = enemyObject.GetComponentsInChildren<Collider>(true);
        for (int index = 0; index < colliders.Length; index++)
        {
            if (colliders[index] != null && !colliders[index].isTrigger)
            {
                return;
            }
        }

        CapsuleCollider capsuleCollider = enemyObject.GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            capsuleCollider = enemyObject.AddComponent<CapsuleCollider>();
        }

        capsuleCollider.isTrigger = false;
        capsuleCollider.height = 2f;
        capsuleCollider.radius = 0.35f;
        capsuleCollider.center = new Vector3(0f, 1f, 0f);
    }

    private static void ApplyBodyMaterial(GameObject enemyObject, Material bodyMaterial)
    {
        if (enemyObject == null || bodyMaterial == null)
        {
            return;
        }

        Renderer[] renderers = enemyObject.GetComponentsInChildren<Renderer>(true);
        for (int index = 0; index < renderers.Length; index++)
        {
            if (renderers[index] != null)
            {
                renderers[index].sharedMaterial = bodyMaterial;
            }
        }
    }

    private static void ConfigureEnemyLayers(Transform root)
    {
        if (root == null)
        {
            return;
        }

        SetEnemyLayerRecursive(root);
    }

    private static void SetEnemyLayerRecursive(Transform current)
    {
        if (current == null)
        {
            return;
        }

        bool isHitbox = current.GetComponent<PrototypeUnitHitbox>() != null || current.name.StartsWith("Hitbox_");
        current.gameObject.layer = isHitbox ? 0 : IgnoreRaycastLayer;

        for (int childIndex = 0; childIndex < current.childCount; childIndex++)
        {
            SetEnemyLayerRecursive(current.GetChild(childIndex));
        }
    }

    private static void EnsureDefaultHitboxes(Transform parent, PrototypeUnitVitals vitals)
    {
        EnsureBodyHitbox(parent, vitals, "Hitbox_Head", "head", new Vector3(0f, 0.82f, 0f), new Vector3(0.42f, 0.42f, 0.42f));
        EnsureBodyHitbox(parent, vitals, "Hitbox_Torso", "torso", new Vector3(0f, 0.08f, 0f), new Vector3(0.72f, 0.86f, 0.52f));
        EnsureBodyHitbox(parent, vitals, "Hitbox_Legs", "legs", new Vector3(0f, -0.72f, 0f), new Vector3(0.64f, 0.88f, 0.48f));
    }

    private static void EnsureBodyHitbox(
        Transform parent,
        PrototypeUnitVitals vitals,
        string name,
        string partId,
        Vector3 localPosition,
        Vector3 colliderSize,
        string passthroughPartId = "")
    {
        Transform hitboxTransform = parent.Find(name);
        if (hitboxTransform == null)
        {
            GameObject hitboxObject = new GameObject(name);
            hitboxTransform = hitboxObject.transform;
            hitboxTransform.SetParent(parent, false);
            hitboxTransform.localPosition = localPosition;
            hitboxTransform.localRotation = Quaternion.identity;
        }

        hitboxTransform.gameObject.layer = 0;

        BoxCollider collider = hitboxTransform.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = hitboxTransform.gameObject.AddComponent<BoxCollider>();
            collider.size = colliderSize;
        }

        collider.isTrigger = true;
        if (collider.size == Vector3.zero)
        {
            collider.size = colliderSize;
        }

        PrototypeUnitHitbox unitHitbox = hitboxTransform.GetComponent<PrototypeUnitHitbox>();
        if (unitHitbox == null)
        {
            unitHitbox = hitboxTransform.gameObject.AddComponent<PrototypeUnitHitbox>();
        }

        unitHitbox.Configure(vitals, partId, passthroughPartId);
    }

    private static Transform FindOrCreateWeaponVisualAnchor(Transform parent)
    {
        Transform anchor = parent.Find("WeaponVisualAnchor");
        if (anchor != null)
        {
            return anchor;
        }

        GameObject anchorObject = new GameObject("WeaponVisualAnchor");
        anchorObject.transform.SetParent(parent, false);
        anchorObject.transform.localPosition = Vector3.zero;
        anchorObject.transform.localRotation = Quaternion.identity;
        anchorObject.layer = IgnoreRaycastLayer;
        return anchorObject.transform;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
