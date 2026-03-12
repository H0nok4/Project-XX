using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Raid/Affix Pool", fileName = "AffixPool")]
public class AffixPool : ScriptableObject
{
    [SerializeField] private List<AffixDefinition> availableAffixes = new List<AffixDefinition>();
    [Header("Affix Count By Rarity")]
    [Min(0)]
    [SerializeField] private int commonAffixCount = 0;
    [Min(0)]
    [SerializeField] private int uncommonAffixCount = 1;
    [Min(0)]
    [SerializeField] private int rareAffixCount = 2;
    [Min(0)]
    [SerializeField] private int epicAffixCount = 3;
    [Min(0)]
    [SerializeField] private int legendaryAffixCount = 4;
    [Header("Value Scaling")]
    [Min(1f)]
    [SerializeField] private float maxLevelMultiplier = 1.6f;
    [Range(0f, 0.5f)]
    [SerializeField] private float rarityValueBonus = 0.12f;
    [Range(0f, 0.5f)]
    [SerializeField] private float tierValueBonus = 0.12f;

    public IReadOnlyList<AffixDefinition> AvailableAffixes => availableAffixes;


    public void ConfigureRuntime(
        List<AffixDefinition> definitions,
        int commonCount,
        int uncommonCount,
        int rareCount,
        int epicCount,
        int legendaryCount,
        float maxLevelMultiplier,
        float rarityValueBonus,
        float tierValueBonus)
    {
        availableAffixes = definitions ?? new List<AffixDefinition>();
        commonAffixCount = Mathf.Max(0, commonCount);
        uncommonAffixCount = Mathf.Max(0, uncommonCount);
        rareAffixCount = Mathf.Max(0, rareCount);
        epicAffixCount = Mathf.Max(0, epicCount);
        legendaryAffixCount = Mathf.Max(0, legendaryCount);
        this.maxLevelMultiplier = Mathf.Max(1f, maxLevelMultiplier);
        this.rarityValueBonus = Mathf.Clamp(rarityValueBonus, 0f, 0.5f);
        this.tierValueBonus = Mathf.Clamp(tierValueBonus, 0f, 0.5f);
    }

    public int GetAffixCount(ItemRarity rarity)
    {
        switch (ItemRarityUtility.Sanitize(rarity))
        {
            case ItemRarity.Uncommon:
                return Mathf.Max(0, uncommonAffixCount);
            case ItemRarity.Rare:
                return Mathf.Max(0, rareAffixCount);
            case ItemRarity.Epic:
                return Mathf.Max(0, epicAffixCount);
            case ItemRarity.Legendary:
                return Mathf.Max(0, legendaryAffixCount);
            default:
                return Mathf.Max(0, commonAffixCount);
        }
    }

    public AffixDefinition FindDefinition(AffixType type)
    {
        if (availableAffixes == null)
        {
            return null;
        }

        for (int index = 0; index < availableAffixes.Count; index++)
        {
            AffixDefinition definition = availableAffixes[index];
            if (definition != null && definition.Type == type)
            {
                return definition;
            }
        }

        return null;
    }

    public List<ItemAffix> GenerateAffixes(ItemRarity rarity, int itemLevel, AffixItemTarget target)
    {
        var results = new List<ItemAffix>();
        GenerateAffixes(rarity, itemLevel, target, results);
        return results;
    }

    public void GenerateAffixes(ItemRarity rarity, int itemLevel, AffixItemTarget target, List<ItemAffix> results)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();
        if (availableAffixes == null || availableAffixes.Count == 0)
        {
            return;
        }

        int affixCount = GetAffixCount(rarity);
        if (affixCount <= 0 || target == AffixItemTarget.None)
        {
            return;
        }

        var candidates = new List<AffixDefinition>();
        for (int index = 0; index < availableAffixes.Count; index++)
        {
            AffixDefinition definition = availableAffixes[index];
            if (definition == null || definition.Weight <= 0f)
            {
                continue;
            }

            if (!definition.SupportsTarget(target))
            {
                continue;
            }

            candidates.Add(definition);
        }

        if (candidates.Count == 0)
        {
            return;
        }

        float levelMultiplier = GetLevelMultiplier(itemLevel);
        float rarityMultiplier = GetRarityMultiplier(rarity);
        var usedTypes = new HashSet<AffixType>();
        var usedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (results.Count < affixCount && candidates.Count > 0)
        {
            float totalWeight = 0f;
            for (int index = 0; index < candidates.Count; index++)
            {
                totalWeight += Mathf.Max(0f, candidates[index].Weight);
            }

            if (totalWeight <= 0f)
            {
                break;
            }

            float roll = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;
            int selectedIndex = 0;
            for (int index = 0; index < candidates.Count; index++)
            {
                cumulative += Mathf.Max(0f, candidates[index].Weight);
                if (roll <= cumulative)
                {
                    selectedIndex = index;
                    break;
                }
            }

            AffixDefinition selected = candidates[selectedIndex];
            if (selected == null)
            {
                candidates.RemoveAt(selectedIndex);
                continue;
            }

            string groupId = string.IsNullOrWhiteSpace(selected.GroupId) ? string.Empty : selected.GroupId.Trim();
            if (!usedTypes.Add(selected.Type) || (!string.IsNullOrEmpty(groupId) && usedGroups.Contains(groupId)))
            {
                candidates.RemoveAt(selectedIndex);
                continue;
            }

            if (!string.IsNullOrEmpty(groupId))
            {
                usedGroups.Add(groupId);
            }

            int tier = UnityEngine.Random.Range(selected.MinTier, selected.MaxTier + 1);
            float baseValue = UnityEngine.Random.Range(Mathf.Min(selected.MinValue, selected.MaxValue), Mathf.Max(selected.MinValue, selected.MaxValue));
            float tierMultiplier = 1f + Mathf.Max(0, tier - 1) * tierValueBonus;
            float value = baseValue * levelMultiplier * rarityMultiplier * tierMultiplier;

            results.Add(new ItemAffix
            {
                type = selected.Type,
                category = selected.Category,
                value = value,
                tier = tier
            });

            for (int index = candidates.Count - 1; index >= 0; index--)
            {
                AffixDefinition candidate = candidates[index];
                if (candidate == null)
                {
                    candidates.RemoveAt(index);
                    continue;
                }

                if (candidate.Type == selected.Type)
                {
                    candidates.RemoveAt(index);
                    continue;
                }

                if (!string.IsNullOrEmpty(groupId) && string.Equals(candidate.GroupId, groupId, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.RemoveAt(index);
                }
            }
        }
    }

    private float GetLevelMultiplier(int itemLevel)
    {
        float clampedLevel = Mathf.Clamp(itemLevel, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel);
        float t = Mathf.InverseLerp(ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel, clampedLevel);
        return Mathf.Lerp(1f, Mathf.Max(1f, maxLevelMultiplier), t);
    }

    private float GetRarityMultiplier(ItemRarity rarity)
    {
        int rarityIndex = (int)ItemRarityUtility.Sanitize(rarity);
        return 1f + rarityIndex * Mathf.Max(0f, rarityValueBonus);
    }

    private void OnValidate()
    {
        maxLevelMultiplier = Mathf.Max(1f, maxLevelMultiplier);
        rarityValueBonus = Mathf.Clamp(rarityValueBonus, 0f, 0.5f);
        tierValueBonus = Mathf.Clamp(tierValueBonus, 0f, 0.5f);

        if (availableAffixes == null)
        {
            availableAffixes = new List<AffixDefinition>();
            return;
        }

        for (int index = availableAffixes.Count - 1; index >= 0; index--)
        {
            if (availableAffixes[index] == null)
            {
                availableAffixes.RemoveAt(index);
            }
        }
    }
}
