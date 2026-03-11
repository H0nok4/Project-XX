using System;
using UnityEngine;

[Serializable]
public class WeaponInstance
{
    [SerializeField] private string instanceId = string.Empty;
    [SerializeField] private PrototypeWeaponDefinition definition;
    [Min(0)]
    [SerializeField] private int magazineAmmo;
    [Min(0f)]
    [SerializeField] private float durability = 1f;

    public string InstanceId => instanceId;
    public PrototypeWeaponDefinition Definition => definition;
    public int MagazineAmmo => magazineAmmo;
    public float Durability => durability;
    public string DisplayName => definition != null ? definition.DisplayNameWithLevel : "Unknown Weapon";

    public static WeaponInstance Create(
        PrototypeWeaponDefinition weaponDefinition,
        int startingAmmo,
        float startingDurability = 1f,
        string instanceIdOverride = null)
    {
        var instance = new WeaponInstance();
        instance.ApplyDefinition(weaponDefinition, startingAmmo, startingDurability, instanceIdOverride);
        return instance;
    }

    public void ApplyDefinition(
        PrototypeWeaponDefinition weaponDefinition,
        int startingAmmo,
        float startingDurability,
        string instanceIdOverride = null)
    {
        definition = weaponDefinition;
        if (definition != null && !definition.IsMeleeWeapon)
        {
            magazineAmmo = Mathf.Clamp(startingAmmo, 0, definition.MagazineSize);
        }
        else
        {
            magazineAmmo = 0;
        }

        durability = Mathf.Max(0f, startingDurability);
        SetInstanceId(instanceIdOverride);
    }

    public void Sanitize()
    {
        if (definition != null && !definition.IsMeleeWeapon)
        {
            magazineAmmo = Mathf.Clamp(magazineAmmo, 0, definition.MagazineSize);
        }
        else
        {
            magazineAmmo = 0;
        }

        durability = Mathf.Max(0f, durability);
        EnsureInstanceId();
    }

    private void SetInstanceId(string desiredId)
    {
        if (!string.IsNullOrWhiteSpace(desiredId))
        {
            instanceId = desiredId.Trim();
        }

        EnsureInstanceId();
    }

    private void EnsureInstanceId()
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            instanceId = Guid.NewGuid().ToString("N");
        }
    }
}