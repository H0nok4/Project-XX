using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Prototype/Raid/Weapon Definition", fileName = "WeaponDefinition")]
public class PrototypeWeaponDefinition : ItemDefinitionBase
{
    [SerializeField] private string weaponId = "weapon";
    [SerializeField] private string displayName = "Weapon";
    [TextArea]
    [SerializeField] private string description = string.Empty;
    [Min(0f)]
    [SerializeField] private float unitWeight = 3.5f;
    [Range(ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel)]
    [SerializeField] private int itemLevel = ItemDefinition.MinItemLevel;
    [Range(ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel)]
    [SerializeField] private int requiredLevel = ItemDefinition.MinItemLevel;
    [SerializeField] private GameObject firstPersonViewPrefab;
    [SerializeField] private GameObject equippedWorldPrefab;
    [SerializeField] private PrototypeWeaponBehaviorType weaponBehavior = PrototypeWeaponBehaviorType.Firearm;
    [FormerlySerializedAs("meleeWeapon")]
    [SerializeField] private bool legacyMeleeWeapon;
    [SerializeField] private AmmoDefinition ammoDefinition;
    [Min(1f)]
    [SerializeField] private float firearmDamage = 100f;
    [Min(1)]
    [SerializeField] private int magazineSize = 30;
    [Min(0.05f)]
    [SerializeField] private float reloadDuration = 2f;
    [Min(30f)]
    [SerializeField] private float roundsPerMinute = 600f;
    [SerializeField] private PrototypeWeaponFireMode[] fireModes = { PrototypeWeaponFireMode.Auto };
    [Min(1)]
    [SerializeField] private int burstCount = 3;
    [Min(1f)]
    [SerializeField] private float effectiveRange = 65f;
    [Min(0f)]
    [SerializeField] private float spreadAngle = 0.18f;
    [Min(0f)]
    [SerializeField] private float addedImpactForce = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float lightBleedChance = 0.16f;
    [Range(0f, 1f)]
    [SerializeField] private float heavyBleedChance = 0.04f;
    [Range(0f, 1f)]
    [SerializeField] private float fractureChance = 0.08f;
    [Min(0f)]
    [SerializeField] private float penetrationPower = 6f;
    [Min(1f)]
    [SerializeField] private float meleeDamage = 55f;
    [Min(0.5f)]
    [SerializeField] private float meleeRange = 2.2f;
    [Min(0f)]
    [SerializeField] private float meleeRadius = 0.45f;
    [Min(0.05f)]
    [SerializeField] private float meleeCooldown = 0.55f;
    [Min(0.05f)]
    [SerializeField] private float throwableCooldown = 1.1f;
    [Min(0f)]
    [SerializeField] private float throwStaminaCost = 10f;
    [Min(0.5f)]
    [SerializeField] private float throwVelocity = 14f;
    [SerializeField] private float throwUpwardVelocity = 2.35f;
    [Min(0.1f)]
    [SerializeField] private float fuseSeconds = 2.8f;
    [Min(0.5f)]
    [SerializeField] private float explosionRadius = 4.8f;
    [Min(1f)]
    [SerializeField] private float explosionDamage = 120f;
    [Min(0f)]
    [SerializeField] private float explosionForce = 12f;
    [Min(0f)]
    [SerializeField] private float explosionNoiseRadius = 30f;

    public override string ItemId => WeaponId;
    public string WeaponId => string.IsNullOrWhiteSpace(weaponId) ? name : weaponId.Trim();
    public override string DisplayName => string.IsNullOrWhiteSpace(displayName) ? WeaponId : displayName.Trim();
    public override bool HasLevelProgression => true;
    public override string DisplayNameWithLevel => $"{DisplayName} (Lv {ItemLevel})";
    public override string Description => description ?? string.Empty;
    public override float UnitWeight => Mathf.Max(0f, unitWeight);
    public override int ItemLevel => Mathf.Clamp(itemLevel, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel);
    public override Sprite Icon => null;
    public override int MaxStackSize => 1;
    public override int RequiredLevel => Mathf.Clamp(requiredLevel, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel);
    public GameObject FirstPersonViewPrefab => firstPersonViewPrefab;
    public GameObject EquippedWorldPrefab => equippedWorldPrefab != null ? equippedWorldPrefab : firstPersonViewPrefab;
    public PrototypeWeaponBehaviorType WeaponBehavior => ResolveWeaponBehavior();
    public bool IsFirearmWeapon => WeaponBehavior == PrototypeWeaponBehaviorType.Firearm;
    public bool IsMeleeWeapon => WeaponBehavior == PrototypeWeaponBehaviorType.Melee;
    public bool IsThrowableWeapon => WeaponBehavior == PrototypeWeaponBehaviorType.Throwable;
    public AmmoDefinition AmmoDefinition => IsFirearmWeapon ? ammoDefinition : null;
    public int MagazineSize => Mathf.Max(1, magazineSize);
    public float ReloadDuration => Mathf.Max(0.05f, reloadDuration);
    public float RoundsPerMinute => Mathf.Max(30f, roundsPerMinute);
    public float SecondsPerShot => 60f / RoundsPerMinute;
    public int BurstCount => Mathf.Max(1, burstCount);
    public float EffectiveRange => Mathf.Max(1f, effectiveRange);
    public float SpreadAngle => Mathf.Max(0f, spreadAngle);
    public float AddedImpactForce => Mathf.Max(0f, addedImpactForce);
    public float LightBleedChance => Mathf.Clamp01(lightBleedChance);
    public float HeavyBleedChance => Mathf.Clamp01(heavyBleedChance);
    public float FractureChance => Mathf.Clamp01(fractureChance);
    public float FirearmDamage => ItemDefinition.GetScaledValue(Mathf.Max(1f, firearmDamage), ItemLevel);
    public float PenetrationPower => ItemDefinition.GetScaledValue(Mathf.Max(0f, penetrationPower), ItemLevel);
    public float MeleeDamage => ItemDefinition.GetScaledValue(Mathf.Max(1f, meleeDamage), ItemLevel);
    public float MeleeRange => Mathf.Max(0.5f, meleeRange);
    public float MeleeRadius => Mathf.Max(0f, meleeRadius);
    public float MeleeCooldown => Mathf.Max(0.05f, meleeCooldown);
    public float ThrowableCooldown => Mathf.Max(0.05f, throwableCooldown);
    public float ThrowStaminaCost => Mathf.Max(0f, throwStaminaCost);
    public float ThrowVelocity => Mathf.Max(0.5f, throwVelocity);
    public float ThrowUpwardVelocity => throwUpwardVelocity;
    public float FuseSeconds => Mathf.Max(0.1f, fuseSeconds);
    public float ExplosionRadius => Mathf.Max(0.5f, explosionRadius);
    public float ExplosionDamage => ItemDefinition.GetScaledValue(Mathf.Max(1f, explosionDamage), ItemLevel);
    public float ExplosionForce => Mathf.Max(0f, explosionForce);
    public float ExplosionNoiseRadius => Mathf.Max(0f, explosionNoiseRadius);
    public PrototypeWeaponFireMode[] FireModes => fireModes != null && fireModes.Length > 0
        ? fireModes
        : new[] { IsFirearmWeapon ? PrototypeWeaponFireMode.Auto : PrototypeWeaponFireMode.Semi };

    public PrototypeWeaponFireMode GetFireMode(int modeIndex)
    {
        PrototypeWeaponFireMode[] modes = FireModes;
        return modes[Mathf.Clamp(modeIndex, 0, modes.Length - 1)];
    }

    public void SetProgression(int level, int required = ItemDefinition.MinItemLevel)
    {
        itemLevel = Mathf.Clamp(level, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel);
        requiredLevel = Mathf.Clamp(required, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel);
    }

    public int GetValidModeIndex(int requestedIndex)
    {
        return Mathf.Clamp(requestedIndex, 0, FireModes.Length - 1);
    }

    public void ConfigureFirearm(
        string id,
        string nameLabel,
        string weaponDescription,
        float damage,
        AmmoDefinition ammo,
        int magSize,
        float rpm,
        float reloadSeconds,
        float range,
        float spread,
        float extraForce,
        int burstShots,
        params PrototypeWeaponFireMode[] supportedModes)
    {
        weaponId = string.IsNullOrWhiteSpace(id) ? name : id.Trim();
        displayName = string.IsNullOrWhiteSpace(nameLabel) ? weaponId : nameLabel.Trim();
        description = weaponDescription ?? string.Empty;
        unitWeight = Mathf.Max(0f, unitWeight);
        weaponBehavior = PrototypeWeaponBehaviorType.Firearm;
        legacyMeleeWeapon = false;
        ammoDefinition = ammo;
        firearmDamage = Mathf.Max(1f, damage);
        magazineSize = Mathf.Max(1, magSize);
        roundsPerMinute = Mathf.Max(30f, rpm);
        reloadDuration = Mathf.Max(0.05f, reloadSeconds);
        effectiveRange = Mathf.Max(1f, range);
        spreadAngle = Mathf.Max(0f, spread);
        addedImpactForce = Mathf.Max(0f, extraForce);
        burstCount = Mathf.Max(1, burstShots);
        penetrationPower = ammo != null ? ammo.PenetrationPower : penetrationPower;
        SetFireModes(supportedModes);
    }

    public void ConfigureMelee(
        string id,
        string nameLabel,
        string weaponDescription,
        float damage,
        float range,
        float radius,
        float cooldown)
    {
        weaponId = string.IsNullOrWhiteSpace(id) ? name : id.Trim();
        displayName = string.IsNullOrWhiteSpace(nameLabel) ? weaponId : nameLabel.Trim();
        description = weaponDescription ?? string.Empty;
        unitWeight = Mathf.Max(0f, unitWeight);
        weaponBehavior = PrototypeWeaponBehaviorType.Melee;
        legacyMeleeWeapon = true;
        ammoDefinition = null;
        penetrationPower = 6f;
        lightBleedChance = 0.55f;
        heavyBleedChance = 0.18f;
        fractureChance = 0.12f;
        meleeDamage = Mathf.Max(1f, damage);
        meleeRange = Mathf.Max(0.5f, range);
        meleeRadius = Mathf.Max(0f, radius);
        meleeCooldown = Mathf.Max(0.05f, cooldown);
        magazineSize = 1;
        SetFireModes(PrototypeWeaponFireMode.Semi);
    }

    public void ConfigureThrowable(
        string id,
        string nameLabel,
        string weaponDescription,
        float cooldown,
        float staminaCost,
        float velocity,
        float upwardVelocity,
        float fuseDuration,
        float radius,
        float damage,
        float force,
        float noiseRadius)
    {
        weaponId = string.IsNullOrWhiteSpace(id) ? name : id.Trim();
        displayName = string.IsNullOrWhiteSpace(nameLabel) ? weaponId : nameLabel.Trim();
        description = weaponDescription ?? string.Empty;
        unitWeight = Mathf.Max(0f, unitWeight);
        weaponBehavior = PrototypeWeaponBehaviorType.Throwable;
        legacyMeleeWeapon = false;
        ammoDefinition = null;
        magazineSize = 1;
        reloadDuration = 0.05f;
        roundsPerMinute = 60f;
        burstCount = 1;
        effectiveRange = Mathf.Max(1f, radius * 2.5f);
        spreadAngle = 0f;
        addedImpactForce = Mathf.Max(0f, force);
        throwableCooldown = Mathf.Max(0.05f, cooldown);
        throwStaminaCost = Mathf.Max(0f, staminaCost);
        throwVelocity = Mathf.Max(0.5f, velocity);
        throwUpwardVelocity = upwardVelocity;
        fuseSeconds = Mathf.Max(0.1f, fuseDuration);
        explosionRadius = Mathf.Max(0.5f, radius);
        explosionDamage = Mathf.Max(1f, damage);
        explosionForce = Mathf.Max(0f, force);
        explosionNoiseRadius = Mathf.Max(0f, noiseRadius);
        SetFireModes(PrototypeWeaponFireMode.Semi);
    }

    public void SetFireModes(params PrototypeWeaponFireMode[] supportedModes)
    {
        if (supportedModes == null || supportedModes.Length == 0)
        {
            fireModes = new[] { IsFirearmWeapon ? PrototypeWeaponFireMode.Auto : PrototypeWeaponFireMode.Semi };
            return;
        }

        fireModes = new PrototypeWeaponFireMode[supportedModes.Length];
        for (int index = 0; index < supportedModes.Length; index++)
        {
            fireModes[index] = supportedModes[index];
        }
    }

    public void SetFirstPersonViewPrefab(GameObject prefab)
    {
        firstPersonViewPrefab = prefab;
    }

    public void SetEquippedWorldPrefab(GameObject prefab)
    {
        equippedWorldPrefab = prefab;
    }

    private PrototypeWeaponBehaviorType ResolveWeaponBehavior()
    {
        return legacyMeleeWeapon && weaponBehavior == PrototypeWeaponBehaviorType.Firearm
            ? PrototypeWeaponBehaviorType.Melee
            : weaponBehavior;
    }

    private void OnValidate()
    {
        weaponId = string.IsNullOrWhiteSpace(weaponId) ? name : weaponId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? weaponId : displayName.Trim();
        unitWeight = Mathf.Max(0f, unitWeight);
        weaponBehavior = ResolveWeaponBehavior();
        legacyMeleeWeapon = weaponBehavior == PrototypeWeaponBehaviorType.Melee;
        magazineSize = Mathf.Max(1, magazineSize);
        reloadDuration = Mathf.Max(0.05f, reloadDuration);
        roundsPerMinute = Mathf.Max(30f, roundsPerMinute);
        burstCount = Mathf.Max(1, burstCount);
        effectiveRange = Mathf.Max(1f, effectiveRange);
        spreadAngle = Mathf.Max(0f, spreadAngle);
        addedImpactForce = Mathf.Max(0f, addedImpactForce);
        lightBleedChance = Mathf.Clamp01(lightBleedChance);
        heavyBleedChance = Mathf.Clamp01(heavyBleedChance);
        fractureChance = Mathf.Clamp01(fractureChance);
        firearmDamage = Mathf.Max(1f, firearmDamage);
        penetrationPower = Mathf.Max(0f, penetrationPower);
        meleeDamage = Mathf.Max(1f, meleeDamage);
        meleeRange = Mathf.Max(0.5f, meleeRange);
        meleeRadius = Mathf.Max(0f, meleeRadius);
        meleeCooldown = Mathf.Max(0.05f, meleeCooldown);
        throwableCooldown = Mathf.Max(0.05f, throwableCooldown);
        throwStaminaCost = Mathf.Max(0f, throwStaminaCost);
        throwVelocity = Mathf.Max(0.5f, throwVelocity);
        fuseSeconds = Mathf.Max(0.1f, fuseSeconds);
        explosionRadius = Mathf.Max(0.5f, explosionRadius);
        explosionDamage = Mathf.Max(1f, explosionDamage);
        explosionForce = Mathf.Max(0f, explosionForce);
        explosionNoiseRadius = Mathf.Max(0f, explosionNoiseRadius);

        if (!IsFirearmWeapon)
        {
            ammoDefinition = null;
            magazineSize = 1;
        }

        SetFireModes(fireModes);
        itemLevel = Mathf.Clamp(itemLevel, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel);
        requiredLevel = Mathf.Clamp(requiredLevel, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel);
    }
}

public enum PrototypeWeaponBehaviorType
{
    Firearm = 0,
    Melee = 1,
    Throwable = 2
}
