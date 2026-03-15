using System;
using System.Collections.Generic;

[Serializable]
public sealed class SavedArmorInstanceDto
{
    public string instanceId;
    public string itemId;
    public ItemRarity rarity = ItemRarity.Common;
    public float currentDurability;
    public List<ItemAffix> affixes;
    public List<ItemSkill> skills;
}
