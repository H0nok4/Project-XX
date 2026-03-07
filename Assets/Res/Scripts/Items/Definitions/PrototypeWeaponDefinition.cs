using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Raid/Weapon Definition", fileName = "WeaponDefinition")]
public class PrototypeWeaponDefinition : ScriptableObject
{
    [SerializeField] private string weaponId = "weapon";
    [SerializeField] private string displayName = "Weapon";
    [TextArea]
    [SerializeField] private string description = string.Empty;
    [SerializeField] private bool meleeWeapon;
    [SerializeField] private AmmoDefinition ammoDefinition;
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

    public string WeaponId => string.IsNullOrWhiteSpace(weaponId) ? name : weaponId.Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? WeaponId : displayName.Trim();
    public string Description => description ?? string.Empty;
    public bool IsMeleeWeapon => meleeWeapon;
    public AmmoDefinition AmmoDefinition => ammoDefinition;
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
    public float PenetrationPower => Mathf.Max(0f, penetrationPower);
    public float MeleeDamage => Mathf.Max(1f, meleeDamage);
    public float MeleeRange => Mathf.Max(0.5f, meleeRange);
    public float MeleeRadius => Mathf.Max(0f, meleeRadius);
    public float MeleeCooldown => Mathf.Max(0.05f, meleeCooldown);
    public PrototypeWeaponFireMode[] FireModes => fireModes != null && fireModes.Length > 0
        ? fireModes
        : new[] { meleeWeapon ? PrototypeWeaponFireMode.Semi : PrototypeWeaponFireMode.Auto };

    public PrototypeWeaponFireMode GetFireMode(int modeIndex)
    {
        PrototypeWeaponFireMode[] modes = FireModes;
        return modes[Mathf.Clamp(modeIndex, 0, modes.Length - 1)];
    }

    public int GetValidModeIndex(int requestedIndex)
    {
        return Mathf.Clamp(requestedIndex, 0, FireModes.Length - 1);
    }

    public void ConfigureFirearm(
        string id,
        string nameLabel,
        string weaponDescription,
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
        meleeWeapon = false;
        ammoDefinition = ammo;
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
        meleeWeapon = true;
        ammoDefinition = null;
        penetrationPower = 6f;
        lightBleedChance = 0.55f;
        heavyBleedChance = 0.18f;
        fractureChance = 0.12f;
        meleeDamage = Mathf.Max(1f, damage);
        meleeRange = Mathf.Max(0.5f, range);
        meleeRadius = Mathf.Max(0f, radius);
        meleeCooldown = Mathf.Max(0.05f, cooldown);
        SetFireModes(PrototypeWeaponFireMode.Semi);
    }

    public void SetFireModes(params PrototypeWeaponFireMode[] supportedModes)
    {
        if (supportedModes == null || supportedModes.Length == 0)
        {
            fireModes = new[] { meleeWeapon ? PrototypeWeaponFireMode.Semi : PrototypeWeaponFireMode.Auto };
            return;
        }

        fireModes = new PrototypeWeaponFireMode[supportedModes.Length];
        for (int index = 0; index < supportedModes.Length; index++)
        {
            fireModes[index] = supportedModes[index];
        }
    }

    private void OnValidate()
    {
        weaponId = string.IsNullOrWhiteSpace(weaponId) ? name : weaponId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? weaponId : displayName.Trim();
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
        penetrationPower = Mathf.Max(0f, penetrationPower);
        meleeDamage = Mathf.Max(1f, meleeDamage);
        meleeRange = Mathf.Max(0.5f, meleeRange);
        meleeRadius = Mathf.Max(0f, meleeRadius);
        meleeCooldown = Mathf.Max(0.05f, meleeCooldown);
        SetFireModes(fireModes);
    }
}
