using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Raid/Loot Table", fileName = "LootTable")]
public class LootTableDefinition : ScriptableObject
{
    [Serializable]
    public sealed class LootEntry
    {
        public ItemDefinitionBase definition;
        public ItemDefinition itemDefinition;
        public PrototypeWeaponDefinition weaponDefinition;
        [Min(1)]
        public int minQuantity = 1;
        [Min(1)]
        public int maxQuantity = 1;
        public bool rollRarity;
        [Min(0f)]
        public float weight = 1f;

        public ItemDefinitionBase DefinitionBase => ResolveDefinition();
        public bool IsWeapon => ResolveDefinition() is PrototypeWeaponDefinition;
        public bool IsLevelSensitive
        {
            get
            {
                ItemDefinitionBase resolvedDefinition = ResolveDefinition();
                return rollRarity || resolvedDefinition is PrototypeWeaponDefinition || resolvedDefinition is ArmorDefinition;
            }
        }

        public bool IsValid => ResolveDefinition() != null;

        public void Sanitize()
        {
            minQuantity = Mathf.Max(1, minQuantity);
            maxQuantity = Mathf.Max(minQuantity, maxQuantity);
            weight = Mathf.Max(0f, weight);

            definition ??= ResolveDefinition();
            if (definition is PrototypeWeaponDefinition)
            {
                minQuantity = 1;
                maxQuantity = 1;
            }
        }

        public ItemInstance CreateInstance(ItemRarity rarity)
        {
            ItemDefinitionBase resolvedDefinition = ResolveDefinition();
            if (resolvedDefinition == null)
            {
                return null;
            }

            int quantity = resolvedDefinition is PrototypeWeaponDefinition
                ? 1
                : UnityEngine.Random.Range(Mathf.Max(1, minQuantity), Mathf.Max(Mathf.Max(1, minQuantity), maxQuantity) + 1);
            return ItemInstance.Create(resolvedDefinition, quantity, rarity);
        }

        private ItemDefinitionBase ResolveDefinition()
        {
            if (definition != null)
            {
                return definition;
            }

            if (weaponDefinition != null)
            {
                return weaponDefinition;
            }

            return itemDefinition;
        }
    }

    public readonly struct LootRoll
    {
        public LootRoll(ItemInstance instance)
        {
            Instance = instance;
        }

        public ItemInstance Instance { get; }
        public ItemDefinitionBase DefinitionBase => Instance != null ? Instance.DefinitionBase : null;
        public ItemDefinition Definition => Instance != null ? Instance.Definition : null;
        public PrototypeWeaponDefinition WeaponDefinition => Instance != null ? Instance.WeaponDefinition : null;
        public int Quantity => Instance != null ? Instance.Quantity : 0;
        public ItemRarity Rarity => Instance != null ? Instance.Rarity : ItemRarity.Common;
        public bool IsWeapon => Instance != null && Instance.IsWeapon;
        public bool IsValid => Instance != null && Instance.IsDefined() && Quantity > 0;
    }

    public readonly struct LootGenerationContext
    {
        public LootGenerationContext(int minItemLevel, int maxItemLevel, int rarityBias = 0, int bonusRolls = 0)
        {
            if (minItemLevel > 0 || maxItemLevel > 0)
            {
                int normalizedMin = minItemLevel > 0
                    ? Mathf.Clamp(minItemLevel, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel)
                    : ItemDefinition.MinItemLevel;
                int normalizedMax = maxItemLevel > 0
                    ? Mathf.Clamp(maxItemLevel, normalizedMin, ItemDefinition.MaxItemLevel)
                    : ItemDefinition.MaxItemLevel;

                MinItemLevel = normalizedMin;
                MaxItemLevel = normalizedMax;
            }
            else
            {
                MinItemLevel = 0;
                MaxItemLevel = 0;
            }

            RarityBias = Mathf.Max(0, rarityBias);
            BonusRolls = Mathf.Max(0, bonusRolls);
        }

        public int MinItemLevel { get; }
        public int MaxItemLevel { get; }
        public int RarityBias { get; }
        public int BonusRolls { get; }
        public bool HasItemLevelRange => MinItemLevel > 0 && MaxItemLevel >= MinItemLevel;

        public LootGenerationContext WithBonuses(int itemLevelBonus = 0, int rarityBiasBonus = 0, int bonusRollsBonus = 0)
        {
            int minItemLevel = MinItemLevel;
            int maxItemLevel = MaxItemLevel;

            if (HasItemLevelRange && itemLevelBonus != 0)
            {
                minItemLevel = Mathf.Clamp(MinItemLevel + itemLevelBonus, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel);
                maxItemLevel = Mathf.Clamp(MaxItemLevel + itemLevelBonus, minItemLevel, ItemDefinition.MaxItemLevel);
            }

            return new LootGenerationContext(
                minItemLevel,
                maxItemLevel,
                RarityBias + rarityBiasBonus,
                BonusRolls + bonusRollsBonus);
        }
    }

    [SerializeField] private string tableId = "loot_table";
    [SerializeField] private string displayName = "Loot Table";
    [Min(0)]
    [SerializeField] private int minRolls = 1;
    [Min(0)]
    [SerializeField] private int maxRolls = 3;
    [SerializeField] private bool allowDuplicateRolls = true;
    [Header("Rarity Weights")]
    [Min(0f)]
    [SerializeField] private float commonWeight = 60f;
    [Min(0f)]
    [SerializeField] private float uncommonWeight = 25f;
    [Min(0f)]
    [SerializeField] private float rareWeight = 10f;
    [Min(0f)]
    [SerializeField] private float epicWeight = 4f;
    [Min(0f)]
    [SerializeField] private float legendaryWeight = 1f;
    [SerializeField] private List<LootEntry> entries = new List<LootEntry>();

    public string TableId => string.IsNullOrWhiteSpace(tableId) ? name : tableId.Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? TableId : displayName.Trim();
    public IReadOnlyList<LootEntry> Entries => entries;
    public int MinRolls => Mathf.Max(0, minRolls);
    public int MaxRolls => Mathf.Max(MinRolls, maxRolls);
    public bool AllowDuplicateRolls => allowDuplicateRolls;

    public List<LootRoll> RollLoot()
    {
        return RollLoot(default);
    }

    public List<LootRoll> RollLoot(LootGenerationContext context)
    {
        var results = new List<LootRoll>();
        RollInto(results, context);
        return results;
    }

    public void RollInto(List<LootRoll> results)
    {
        RollInto(results, default);
    }

    public void RollInto(List<LootRoll> results, LootGenerationContext context)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();
        if (entries == null || entries.Count == 0)
        {
            return;
        }

        List<LootEntry> candidateEntries = new List<LootEntry>();
        float totalWeight;
        CollectCandidateEntries(candidateEntries, context, out totalWeight);

        if (candidateEntries.Count == 0 || totalWeight <= 0f)
        {
            return;
        }

        int minimumRolls = Mathf.Max(0, MinRolls + context.BonusRolls);
        int maximumRolls = Mathf.Max(minimumRolls, MaxRolls + context.BonusRolls);
        int rollCount = UnityEngine.Random.Range(minimumRolls, maximumRolls + 1);
        for (int rollIndex = 0; rollIndex < rollCount && candidateEntries.Count > 0; rollIndex++)
        {
            int selectedIndex = SelectWeightedEntryIndex(candidateEntries, totalWeight);
            LootEntry selectedEntry = candidateEntries[selectedIndex];
            ItemRarity rarity = selectedEntry.rollRarity
                ? ItemRarityUtility.RollWeightedBiased(commonWeight, uncommonWeight, rareWeight, epicWeight, legendaryWeight, context.RarityBias)
                : ItemRarity.Common;
            ItemInstance instance = selectedEntry.CreateInstance(rarity);
            if (instance != null && instance.IsDefined() && instance.Quantity > 0)
            {
                results.Add(new LootRoll(instance));
            }

            if (!allowDuplicateRolls)
            {
                totalWeight -= Mathf.Max(0f, selectedEntry.weight);
                candidateEntries.RemoveAt(selectedIndex);
                if (totalWeight <= 0f)
                {
                    break;
                }
            }
        }
    }

    public void Configure(string id, string nameLabel, int minimumRolls, int maximumRolls, bool allowDuplicates, params LootEntry[] lootEntries)
    {
        tableId = string.IsNullOrWhiteSpace(id) ? name : id.Trim();
        displayName = string.IsNullOrWhiteSpace(nameLabel) ? tableId : nameLabel.Trim();
        minRolls = Mathf.Max(0, minimumRolls);
        maxRolls = Mathf.Max(minRolls, maximumRolls);
        allowDuplicateRolls = allowDuplicates;
        entries = new List<LootEntry>();

        if (lootEntries == null)
        {
            return;
        }

        for (int index = 0; index < lootEntries.Length; index++)
        {
            LootEntry source = lootEntries[index];
            if (source == null || !source.IsValid)
            {
                continue;
            }

            ItemDefinitionBase resolvedDefinition = source.DefinitionBase;
            var entry = new LootEntry
            {
                definition = resolvedDefinition,
                itemDefinition = resolvedDefinition as ItemDefinition,
                weaponDefinition = resolvedDefinition as PrototypeWeaponDefinition,
                minQuantity = Mathf.Max(1, source.minQuantity),
                maxQuantity = Mathf.Max(Mathf.Max(1, source.minQuantity), source.maxQuantity),
                rollRarity = source.rollRarity,
                weight = Mathf.Max(0f, source.weight)
            };
            entry.Sanitize();
            if (entry.IsValid)
            {
                entries.Add(entry);
            }
        }
    }

    private void OnValidate()
    {
        tableId = string.IsNullOrWhiteSpace(tableId) ? name : tableId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? TableId : displayName.Trim();
        minRolls = Mathf.Max(0, minRolls);
        maxRolls = Mathf.Max(minRolls, maxRolls);
        commonWeight = Mathf.Max(0f, commonWeight);
        uncommonWeight = Mathf.Max(0f, uncommonWeight);
        rareWeight = Mathf.Max(0f, rareWeight);
        epicWeight = Mathf.Max(0f, epicWeight);
        legendaryWeight = Mathf.Max(0f, legendaryWeight);

        if (entries == null)
        {
            entries = new List<LootEntry>();
            return;
        }

        for (int index = entries.Count - 1; index >= 0; index--)
        {
            LootEntry entry = entries[index];
            if (entry == null)
            {
                entries.RemoveAt(index);
                continue;
            }

            entry.Sanitize();
            if (!entry.IsValid)
            {
                entries.RemoveAt(index);
            }
        }
    }

    private static int SelectWeightedEntryIndex(List<LootEntry> candidates, float totalWeight)
    {
        float threshold = UnityEngine.Random.value * Mathf.Max(totalWeight, 0.0001f);
        float cumulativeWeight = 0f;

        for (int index = 0; index < candidates.Count; index++)
        {
            cumulativeWeight += Mathf.Max(0f, candidates[index].weight);
            if (threshold <= cumulativeWeight)
            {
                return index;
            }
        }

        return Mathf.Max(0, candidates.Count - 1);
    }

    private void CollectCandidateEntries(List<LootEntry> candidateEntries, LootGenerationContext context, out float totalWeight)
    {
        totalWeight = 0f;
        bool useItemLevelRange = context.HasItemLevelRange;
        int matchedLevelSensitiveEntries = 0;
        List<LootEntry> fallbackLevelEntries = useItemLevelRange ? new List<LootEntry>() : null;

        for (int index = 0; index < entries.Count; index++)
        {
            LootEntry entry = entries[index];
            if (entry == null)
            {
                continue;
            }

            entry.Sanitize();
            if (!entry.IsValid || entry.weight <= 0f)
            {
                continue;
            }

            if (useItemLevelRange && entry.IsLevelSensitive && !IsEntryWithinLevelRange(entry, context))
            {
                fallbackLevelEntries.Add(entry);
                continue;
            }

            if (entry.IsLevelSensitive)
            {
                matchedLevelSensitiveEntries++;
            }

            candidateEntries.Add(entry);
            totalWeight += entry.weight;
        }

        if (!useItemLevelRange || matchedLevelSensitiveEntries > 0 || fallbackLevelEntries == null || fallbackLevelEntries.Count == 0)
        {
            return;
        }

        for (int index = 0; index < fallbackLevelEntries.Count; index++)
        {
            LootEntry fallbackEntry = fallbackLevelEntries[index];
            candidateEntries.Add(fallbackEntry);
            totalWeight += fallbackEntry.weight;
        }
    }

    private static bool IsEntryWithinLevelRange(LootEntry entry, LootGenerationContext context)
    {
        if (entry == null || !entry.IsValid || !context.HasItemLevelRange)
        {
            return true;
        }

        ItemDefinitionBase definition = entry.DefinitionBase;
        if (definition == null)
        {
            return false;
        }

        int itemLevel = Mathf.Clamp(definition.ItemLevel, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel);
        return itemLevel >= context.MinItemLevel && itemLevel <= context.MaxItemLevel;
    }
}
