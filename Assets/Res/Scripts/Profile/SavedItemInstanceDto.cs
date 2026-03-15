using System;
using System.Collections.Generic;

[Serializable]
public sealed class SavedItemInstanceDto
{
    public string instanceId;
    public string itemId;
    public string weaponId;
    public ItemRarity rarity = ItemRarity.Common;
    public int quantity;
    public int magazineAmmo;
    public float durability = -1f;
    public List<ItemAffix> affixes;
    public List<ItemSkill> skills;
}
