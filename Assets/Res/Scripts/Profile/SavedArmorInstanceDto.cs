using System;

[Serializable]
public sealed class SavedArmorInstanceDto
{
    public string instanceId;
    public string itemId;
    public ItemRarity rarity = ItemRarity.Common;
    public float currentDurability;
}
