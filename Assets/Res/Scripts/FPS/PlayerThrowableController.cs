using UnityEngine;

[DisallowMultipleComponent]
public class PlayerThrowableController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool useHostSettings = true;
    [SerializeField] private float feedbackLifetime = 1.4f;
    [SerializeField] private float throwOriginForwardOffset = 0.3f;
    [SerializeField] private float throwOriginUpOffset = -0.08f;
    [SerializeField] private float projectileScale = 0.16f;
    [SerializeField] private LayerMask explosionMask = Physics.DefaultRaycastLayers;

    private Camera viewCamera;
    private PlayerAimPointResolver aimPointResolver;
    private PrototypeUnitVitals playerVitals;
    private InventoryContainer primaryInventory;
    private PlayerInteractor interactor;
    private PlayerSkillManager skillManager;
    private float nextThrowableUseTime;
    private float feedbackTimer;
    private string feedbackMessage = string.Empty;
    private bool throwableTriggeredThisFrame;

    public string FeedbackMessage => feedbackMessage;
    public bool ThrowableTriggeredThisFrame => throwableTriggeredThisFrame;

    private void Awake()
    {
        ResolveReferences();
        EnsureSettings();
    }

    private void OnValidate()
    {
        EnsureSettings();
    }

    public void SetPlayerDependencies(Camera camera, PrototypeUnitVitals vitals, InventoryContainer inventoryContainer)
    {
        viewCamera = camera;
        playerVitals = vitals;
        primaryInventory = inventoryContainer;
        skillManager = GetComponent<PlayerSkillManager>();
        interactor = GetComponent<PlayerInteractor>();
        aimPointResolver = GetComponent<PlayerAimPointResolver>();
    }

    public void ApplyHostSettings(Camera camera, float hostFeedbackLifetime, PlayerAimPointResolver hostAimPointResolver = null)
    {
        if (useHostSettings)
        {
            viewCamera = camera;
            feedbackLifetime = hostFeedbackLifetime;
        }

        if (hostAimPointResolver != null)
        {
            aimPointResolver = hostAimPointResolver;
        }

        EnsureSettings();
    }

    public void BeginFrame()
    {
        throwableTriggeredThisFrame = false;
    }

    public bool HandleThrowableInput(PrototypeFpsInput fpsInput)
    {
        EnsureReferences();
        if (fpsInput == null || !fpsInput.ThrowThrowablePressedThisFrame || viewCamera == null || playerVitals == null || Time.time < nextThrowableUseTime)
        {
            return false;
        }

        return TryThrowThrowable();
    }

    public void TickFeedback(float deltaTime)
    {
        if (feedbackTimer <= 0f)
        {
            return;
        }

        feedbackTimer -= deltaTime;
        if (feedbackTimer <= 0f)
        {
            feedbackMessage = string.Empty;
        }
    }

    public string BuildHudSummary()
    {
        EnsureReferences();
        if (!TryFindThrowable(out _, out _, out ItemInstance throwableItem, out int availableCount))
        {
            return "投掷物 无 [G]";
        }

        string baseLabel = $"投掷物 {throwableItem.DisplayName} x{Mathf.Max(1, availableCount)} [G]";
        float cooldownRemaining = Mathf.Max(0f, nextThrowableUseTime - Time.time);
        return cooldownRemaining > 0f
            ? $"{baseLabel}  冷却 {cooldownRemaining:0.0}s"
            : baseLabel;
    }

    private bool TryThrowThrowable()
    {
        if (!TryFindThrowable(out InventoryContainer sourceInventory, out int itemIndex, out ItemInstance throwableItem, out _)
            || sourceInventory == null
            || throwableItem == null
            || throwableItem.WeaponDefinition == null)
        {
            SetFeedback("没有可用投掷物");
            return false;
        }

        PrototypeWeaponDefinition weaponDefinition = throwableItem.WeaponDefinition;
        if (!weaponDefinition.IsThrowableWeapon)
        {
            SetFeedback("投掷物配置无效");
            return false;
        }

        if (weaponDefinition.ThrowStaminaCost > 0f
            && (!playerVitals.CanStartStaminaAction(weaponDefinition.ThrowStaminaCost)
                || !playerVitals.TryConsumeStamina(weaponDefinition.ThrowStaminaCost)))
        {
            SetFeedback("体力不足");
            return false;
        }

        if (!sourceInventory.TryExtractItem(itemIndex, 1, out ItemInstance extractedItem) || extractedItem == null)
        {
            SetFeedback("投掷物同步失败");
            return false;
        }

        LaunchThrowable(extractedItem);
        nextThrowableUseTime = Time.time + weaponDefinition.ThrowableCooldown;
        SetFeedback($"已投掷 {weaponDefinition.DisplayName}");
        throwableTriggeredThisFrame = true;
        return true;
    }

    private void LaunchThrowable(ItemInstance throwableItem)
    {
        PrototypeWeaponDefinition definition = throwableItem != null ? throwableItem.WeaponDefinition : null;
        if (definition == null)
        {
            return;
        }

        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = $"{definition.WeaponId}_Projectile";
        projectile.transform.position = ResolveThrowSpawnPosition();
        projectile.transform.localScale = Vector3.one * projectileScale;

        Collider projectileCollider = projectile.GetComponent<Collider>();
        Rigidbody rigidbody = projectile.AddComponent<Rigidbody>();
        rigidbody.mass = 0.35f;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            renderer.material = new Material(shader);
            renderer.material.color = ItemRarityUtility.GetDisplayColor(throwableItem.Rarity);
        }

        IgnoreOwnerCollisions(projectileCollider);

        PrototypeThrowableProjectile throwableProjectile = projectile.AddComponent<PrototypeThrowableProjectile>();
        throwableProjectile.Configure(
            throwableItem,
            BuildThrowableDamageInfo(throwableItem),
            playerVitals,
            skillManager,
            explosionMask);

        Vector3 launchDirection = ResolveThrowableDirection(projectile.transform.position);
        Vector3 launchVelocity = launchDirection * definition.ThrowVelocity + Vector3.up * definition.ThrowUpwardVelocity;
        rigidbody.linearVelocity = launchVelocity;
    }

    private Vector3 ResolveThrowSpawnPosition()
    {
        Transform origin = viewCamera != null ? viewCamera.transform : transform;
        return origin.position + origin.forward * throwOriginForwardOffset + origin.up * throwOriginUpOffset;
    }

    private Vector3 ResolveThrowableDirection(Vector3 origin)
    {
        if (aimPointResolver != null)
        {
            Vector3 resolvedDirection = aimPointResolver.GetDirectionFrom(origin);
            if (resolvedDirection.sqrMagnitude > 0.0001f)
            {
                return resolvedDirection.normalized;
            }
        }

        return viewCamera != null ? viewCamera.transform.forward : transform.forward;
    }

    private PrototypeUnitVitals.DamageInfo BuildThrowableDamageInfo(ItemInstance throwableItem)
    {
        PrototypeWeaponDefinition definition = throwableItem != null ? throwableItem.WeaponDefinition : null;
        ItemAffixSummary affixSummary = throwableItem != null
            ? ItemAffixUtility.BuildSummary(throwableItem.Affixes)
            : ItemAffixSummary.CreateDefault();
        float rarityMultiplier = throwableItem != null ? ItemRarityUtility.GetStatMultiplier(throwableItem.Rarity) : 1f;
        float affixDamageMultiplier = Mathf.Max(0.1f, affixSummary.DamageMultiplier);
        float damage = Mathf.Max(1f, (definition != null ? definition.ExplosionDamage : 40f) * rarityMultiplier * affixDamageMultiplier);
        float armorDamage = Mathf.Max(6f, damage * 0.75f);
        float penetrationPower = (definition != null ? definition.PenetrationPower : 6f) * rarityMultiplier + affixSummary.ArmorPenetrationBonus;

        bool isCrit = affixSummary.CritChance > 0f && Random.value < affixSummary.CritChance;
        if (isCrit)
        {
            damage *= Mathf.Max(1f, affixSummary.CritDamageMultiplier);
        }

        return new PrototypeUnitVitals.DamageInfo
        {
            damage = damage,
            penetrationPower = Mathf.Max(0f, penetrationPower),
            armorDamage = armorDamage,
            lightBleedChance = definition != null ? definition.LightBleedChance : 0f,
            heavyBleedChance = definition != null ? definition.HeavyBleedChance : 0f,
            fractureChance = definition != null ? definition.FractureChance : 0f,
            bypassArmor = false,
            canApplyAfflictions = true,
            sourceUnit = playerVitals,
            sourceDisplayName = gameObject.name,
            sourceEffectDisplayName = isCrit ? "暴击爆炸" : "爆炸"
        };
    }

    private bool TryFindThrowable(
        out InventoryContainer inventory,
        out int itemIndex,
        out ItemInstance itemInstance,
        out int totalCount)
    {
        inventory = null;
        itemIndex = -1;
        itemInstance = null;
        totalCount = 0;

        InventoryContainer specialInventory = interactor != null ? interactor.SpecialInventory : null;
        if (TryFindThrowableInInventory(specialInventory, out itemIndex, out itemInstance, out totalCount))
        {
            inventory = specialInventory;
            return true;
        }

        if (TryFindThrowableInInventory(primaryInventory, out itemIndex, out itemInstance, out totalCount))
        {
            inventory = primaryInventory;
            return true;
        }

        return false;
    }

    private static bool TryFindThrowableInInventory(
        InventoryContainer inventory,
        out int itemIndex,
        out ItemInstance itemInstance,
        out int totalCount)
    {
        itemIndex = -1;
        itemInstance = null;
        totalCount = 0;
        if (inventory == null || inventory.Items == null)
        {
            return false;
        }

        for (int index = 0; index < inventory.Items.Count; index++)
        {
            ItemInstance candidate = inventory.Items[index];
            if (candidate == null || !candidate.IsWeapon || candidate.WeaponDefinition == null || !candidate.WeaponDefinition.IsThrowableWeapon)
            {
                continue;
            }

            totalCount++;
            if (itemIndex < 0)
            {
                itemIndex = index;
                itemInstance = candidate;
            }
        }

        return itemIndex >= 0;
    }

    private void IgnoreOwnerCollisions(Collider projectileCollider)
    {
        if (projectileCollider == null)
        {
            return;
        }

        Collider[] ownerColliders = GetComponentsInChildren<Collider>();
        for (int index = 0; index < ownerColliders.Length; index++)
        {
            Collider ownerCollider = ownerColliders[index];
            if (ownerCollider != null && ownerCollider != projectileCollider)
            {
                Physics.IgnoreCollision(projectileCollider, ownerCollider, true);
            }
        }
    }

    private void SetFeedback(string message)
    {
        feedbackMessage = message ?? string.Empty;
        feedbackTimer = feedbackLifetime;
    }

    private void ResolveReferences()
    {
        if (aimPointResolver == null)
        {
            aimPointResolver = GetComponent<PlayerAimPointResolver>();
        }

        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }

        if (playerVitals == null)
        {
            playerVitals = GetComponent<PrototypeUnitVitals>();
        }

        if (primaryInventory == null)
        {
            primaryInventory = GetComponent<InventoryContainer>();
        }

        if (skillManager == null)
        {
            skillManager = GetComponent<PlayerSkillManager>();
        }

        if (interactor == null)
        {
            interactor = GetComponent<PlayerInteractor>();
        }
    }

    private void EnsureReferences()
    {
        if (viewCamera == null || playerVitals == null || primaryInventory == null || interactor == null)
        {
            ResolveReferences();
        }
    }

    private void EnsureSettings()
    {
        feedbackLifetime = Mathf.Max(0.25f, feedbackLifetime);
        throwOriginForwardOffset = Mathf.Max(0f, throwOriginForwardOffset);
        projectileScale = Mathf.Clamp(projectileScale, 0.08f, 0.5f);
        if (explosionMask.value == 0 || explosionMask.value == ~0)
        {
            explosionMask = Physics.DefaultRaycastLayers;
        }
    }
}
