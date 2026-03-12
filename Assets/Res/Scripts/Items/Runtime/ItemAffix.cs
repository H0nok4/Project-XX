using System;

[Serializable]
public sealed class ItemAffix
{
    public AffixCategory category;
    public AffixType type;
    public float value;
    public int tier = 1;

    public ItemAffix()
    {
    }

    public ItemAffix(ItemAffix source)
    {
        if (source == null)
        {
            return;
        }

        category = source.category;
        type = source.type;
        value = source.value;
        tier = source.tier;
    }
}
