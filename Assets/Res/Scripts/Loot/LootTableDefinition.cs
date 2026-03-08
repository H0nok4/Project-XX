using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Raid/Loot Table", fileName = "LootTable")]
public class LootTableDefinition : ScriptableObject
{
    [Serializable]
    public sealed class LootEntry
    {
        public ItemDefinition itemDefinition;
        [Min(1)]
        public int minQuantity = 1;
        [Min(1)]
        public int maxQuantity = 1;
        [Min(0f)]
        public float weight = 1f;
    }

    public readonly struct LootRoll
    {
        public LootRoll(ItemDefinition definition, int quantity)
        {
            Definition = definition;
            Quantity = Mathf.Max(1, quantity);
        }

        public ItemDefinition Definition { get; }
        public int Quantity { get; }
    }

    [SerializeField] private string tableId = "loot_table";
    [SerializeField] private string displayName = "Loot Table";
    [Min(0)]
    [SerializeField] private int minRolls = 1;
    [Min(0)]
    [SerializeField] private int maxRolls = 3;
    [SerializeField] private bool allowDuplicateRolls = true;
    [SerializeField] private List<LootEntry> entries = new List<LootEntry>();

    public string TableId => string.IsNullOrWhiteSpace(tableId) ? name : tableId.Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? TableId : displayName.Trim();
    public IReadOnlyList<LootEntry> Entries => entries;
    public int MinRolls => Mathf.Max(0, minRolls);
    public int MaxRolls => Mathf.Max(MinRolls, maxRolls);
    public bool AllowDuplicateRolls => allowDuplicateRolls;

    public List<LootRoll> RollLoot()
    {
        var results = new List<LootRoll>();
        RollInto(results);
        return results;
    }

    public void RollInto(List<LootRoll> results)
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
        float totalWeight = 0f;

        for (int index = 0; index < entries.Count; index++)
        {
            LootEntry entry = entries[index];
            if (entry == null || entry.itemDefinition == null || entry.weight <= 0f)
            {
                continue;
            }

            entry.minQuantity = Mathf.Max(1, entry.minQuantity);
            entry.maxQuantity = Mathf.Max(entry.minQuantity, entry.maxQuantity);
            candidateEntries.Add(entry);
            totalWeight += entry.weight;
        }

        if (candidateEntries.Count == 0 || totalWeight <= 0f)
        {
            return;
        }

        int rollCount = UnityEngine.Random.Range(MinRolls, MaxRolls + 1);
        for (int rollIndex = 0; rollIndex < rollCount && candidateEntries.Count > 0; rollIndex++)
        {
            int selectedIndex = SelectWeightedEntryIndex(candidateEntries, totalWeight);
            LootEntry selectedEntry = candidateEntries[selectedIndex];
            int quantity = UnityEngine.Random.Range(selectedEntry.minQuantity, selectedEntry.maxQuantity + 1);
            results.Add(new LootRoll(selectedEntry.itemDefinition, quantity));

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
            if (source == null || source.itemDefinition == null)
            {
                continue;
            }

            entries.Add(new LootEntry
            {
                itemDefinition = source.itemDefinition,
                minQuantity = Mathf.Max(1, source.minQuantity),
                maxQuantity = Mathf.Max(Mathf.Max(1, source.minQuantity), source.maxQuantity),
                weight = Mathf.Max(0f, source.weight)
            });
        }
    }

    private void OnValidate()
    {
        tableId = string.IsNullOrWhiteSpace(tableId) ? name : tableId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? TableId : displayName.Trim();
        minRolls = Mathf.Max(0, minRolls);
        maxRolls = Mathf.Max(minRolls, maxRolls);

        if (entries == null)
        {
            entries = new List<LootEntry>();
            return;
        }

        for (int index = entries.Count - 1; index >= 0; index--)
        {
            LootEntry entry = entries[index];
            if (entry == null || entry.itemDefinition == null)
            {
                entries.RemoveAt(index);
                continue;
            }

            entry.minQuantity = Mathf.Max(1, entry.minQuantity);
            entry.maxQuantity = Mathf.Max(entry.minQuantity, entry.maxQuantity);
            entry.weight = Mathf.Max(0f, entry.weight);
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
}
