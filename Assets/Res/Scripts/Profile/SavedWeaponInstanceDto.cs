using System;
using System.Collections.Generic;

[Serializable]
public sealed class SavedWeaponInstanceDto
{
    public string instanceId;
    public string weaponId;
    public ItemRarity rarity = ItemRarity.Common;
    public int magazineAmmo;
    public float durability = 1f;
    public List<ItemAffix> affixes;
    public List<ItemSkill> skills;
}
