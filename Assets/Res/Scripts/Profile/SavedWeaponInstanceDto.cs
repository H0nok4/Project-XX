using System;

[Serializable]
public sealed class SavedWeaponInstanceDto
{
    public string instanceId;
    public string weaponId;
    public int magazineAmmo;
    public float durability = 1f;
}