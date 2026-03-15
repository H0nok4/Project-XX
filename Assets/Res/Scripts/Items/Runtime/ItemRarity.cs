using UnityEngine;

public enum ItemRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

public static class ItemRarityUtility
{
    public static ItemRarity Sanitize(ItemRarity rarity)
    {
        if (rarity < ItemRarity.Common)
        {
            return ItemRarity.Common;
        }

        if (rarity > ItemRarity.Legendary)
        {
            return ItemRarity.Legendary;
        }

        return rarity;
    }

    public static string GetDisplayName(ItemRarity rarity)
    {
        switch (Sanitize(rarity))
        {
            case ItemRarity.Uncommon:
                return "Uncommon";

            case ItemRarity.Rare:
                return "Rare";

            case ItemRarity.Epic:
                return "Epic";

            case ItemRarity.Legendary:
                return "Legendary";

            default:
                return "Common";
        }
    }

    public static Color GetDisplayColor(ItemRarity rarity)
    {
        switch (Sanitize(rarity))
        {
            case ItemRarity.Uncommon:
                return new Color(0.42f, 0.88f, 0.48f, 1f);

            case ItemRarity.Rare:
                return new Color(0.35f, 0.68f, 1f, 1f);

            case ItemRarity.Epic:
                return new Color(0.78f, 0.48f, 1f, 1f);

            case ItemRarity.Legendary:
                return new Color(1f, 0.72f, 0.24f, 1f);

            default:
                return new Color(0.92f, 0.92f, 0.92f, 1f);
        }
    }

    public static string GetColorHex(ItemRarity rarity)
    {
        return ColorUtility.ToHtmlStringRGB(GetDisplayColor(rarity));
    }

    public static string FormatRichText(string text, ItemRarity rarity)
    {
        return $"<color=#{GetColorHex(rarity)}>{text}</color>";
    }

    public static float GetStatMultiplier(ItemRarity rarity)
    {
        switch (Sanitize(rarity))
        {
            case ItemRarity.Uncommon:
                return 1.1f;

            case ItemRarity.Rare:
                return 1.25f;

            case ItemRarity.Epic:
                return 1.5f;

            case ItemRarity.Legendary:
                return 2f;

            default:
                return 1f;
        }
    }

    public static float ScaleValue(float baseValue, ItemRarity rarity)
    {
        return Mathf.Max(0f, baseValue) * GetStatMultiplier(rarity);
    }

    public static ItemRarity RollWeighted(
        float commonWeight,
        float uncommonWeight,
        float rareWeight,
        float epicWeight,
        float legendaryWeight)
    {
        float sanitizedCommon = Mathf.Max(0f, commonWeight);
        float sanitizedUncommon = Mathf.Max(0f, uncommonWeight);
        float sanitizedRare = Mathf.Max(0f, rareWeight);
        float sanitizedEpic = Mathf.Max(0f, epicWeight);
        float sanitizedLegendary = Mathf.Max(0f, legendaryWeight);
        float totalWeight = sanitizedCommon + sanitizedUncommon + sanitizedRare + sanitizedEpic + sanitizedLegendary;
        if (totalWeight <= 0f)
        {
            return ItemRarity.Common;
        }

        float threshold = Random.value * totalWeight;
        float cumulative = sanitizedCommon;
        if (threshold <= cumulative)
        {
            return ItemRarity.Common;
        }

        cumulative += sanitizedUncommon;
        if (threshold <= cumulative)
        {
            return ItemRarity.Uncommon;
        }

        cumulative += sanitizedRare;
        if (threshold <= cumulative)
        {
            return ItemRarity.Rare;
        }

        cumulative += sanitizedEpic;
        if (threshold <= cumulative)
        {
            return ItemRarity.Epic;
        }

        return ItemRarity.Legendary;
    }

    public static ItemRarity RollWeightedBiased(
        float commonWeight,
        float uncommonWeight,
        float rareWeight,
        float epicWeight,
        float legendaryWeight,
        int rarityBias)
    {
        int sanitizedBias = Mathf.Max(0, rarityBias);
        if (sanitizedBias <= 0)
        {
            return RollWeighted(commonWeight, uncommonWeight, rareWeight, epicWeight, legendaryWeight);
        }

        float commonScale = Mathf.Pow(0.58f, sanitizedBias);
        float uncommonScale = Mathf.Pow(0.82f, sanitizedBias);
        float rareScale = Mathf.Pow(1.22f, sanitizedBias);
        float epicScale = Mathf.Pow(1.55f, sanitizedBias);
        float legendaryScale = Mathf.Pow(1.9f, sanitizedBias);

        return RollWeighted(
            commonWeight * commonScale,
            uncommonWeight * uncommonScale,
            rareWeight * rareScale,
            epicWeight * epicScale,
            legendaryWeight * legendaryScale);
    }
}
