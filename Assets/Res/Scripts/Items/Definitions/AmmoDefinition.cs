using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Raid/Ammo Definition", fileName = "AmmoDefinition")]
public class AmmoDefinition : ItemDefinition
{
    [Header("Ballistics")]
    [SerializeField] private float directDamage = 24f;
    [SerializeField] private float impactForce = 16f;
    [SerializeField] private float penetrationPower = 24f;
    [SerializeField] private float armorDamage = 18f;
    [Range(0f, 1f)]
    [SerializeField] private float lightBleedChance = 0.18f;
    [Range(0f, 1f)]
    [SerializeField] private float heavyBleedChance = 0.05f;
    [Range(0f, 1f)]
    [SerializeField] private float fractureChance = 0.08f;

    public float DirectDamage => Mathf.Max(1f, directDamage);
    public float ImpactForce => Mathf.Max(0f, impactForce);
    public float PenetrationPower => Mathf.Max(0f, penetrationPower);
    public float ArmorDamage => Mathf.Max(0f, armorDamage);
    public float LightBleedChance => Mathf.Clamp01(lightBleedChance);
    public float HeavyBleedChance => Mathf.Clamp01(heavyBleedChance);
    public float FractureChance => Mathf.Clamp01(fractureChance);

    public void ConfigureAmmo(
        string id,
        string nameLabel,
        string itemDescription,
        int stackSize,
        float weight,
        float damage,
        float force,
        float penetration,
        float armorDamageValue,
        float lightBleed,
        float heavyBleed,
        float fracture,
        Sprite itemIcon = null)
    {
        Configure(id, nameLabel, itemDescription, stackSize, weight, itemIcon);
        directDamage = Mathf.Max(1f, damage);
        impactForce = Mathf.Max(0f, force);
        penetrationPower = Mathf.Max(0f, penetration);
        armorDamage = Mathf.Max(0f, armorDamageValue);
        lightBleedChance = Mathf.Clamp01(lightBleed);
        heavyBleedChance = Mathf.Clamp01(heavyBleed);
        fractureChance = Mathf.Clamp01(fracture);
    }

    private void OnValidate()
    {
        directDamage = Mathf.Max(1f, directDamage);
        impactForce = Mathf.Max(0f, impactForce);
        penetrationPower = Mathf.Max(0f, penetrationPower);
        armorDamage = Mathf.Max(0f, armorDamage);
        lightBleedChance = Mathf.Clamp01(lightBleedChance);
        heavyBleedChance = Mathf.Clamp01(heavyBleedChance);
        fractureChance = Mathf.Clamp01(fractureChance);
    }
}
