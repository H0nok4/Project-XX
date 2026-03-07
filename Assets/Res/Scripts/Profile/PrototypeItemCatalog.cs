using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Profile/Item Catalog", fileName = "PrototypeItemCatalog")]
public class PrototypeItemCatalog : ScriptableObject
{
    [Serializable]
    public sealed class ItemStackPreset
    {
        public ItemDefinition definition;
        [Min(1)]
        public int quantity = 1;
    }

    [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();
    [SerializeField] private List<ItemStackPreset> defaultStashItems = new List<ItemStackPreset>();
    [SerializeField] private List<ItemStackPreset> defaultLoadoutItems = new List<ItemStackPreset>();

    private Dictionary<string, ItemDefinition> itemLookup;

    public IReadOnlyList<ItemDefinition> Items => items;
    public IReadOnlyList<ItemStackPreset> DefaultStashItems => defaultStashItems;
    public IReadOnlyList<ItemStackPreset> DefaultLoadoutItems => defaultLoadoutItems;

    public ItemDefinition FindByItemId(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        EnsureLookup();
        itemLookup.TryGetValue(itemId.Trim(), out ItemDefinition definition);
        return definition;
    }

    private void OnEnable()
    {
        EnsureSanitized();
    }

    private void OnValidate()
    {
        EnsureSanitized();
    }

    private void EnsureSanitized()
    {
        SanitizeDefinitionList(items);
        SanitizePresetList(defaultStashItems);
        SanitizePresetList(defaultLoadoutItems);
        itemLookup = null;
    }

    private void EnsureLookup()
    {
        if (itemLookup != null)
        {
            return;
        }

        itemLookup = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < items.Count; index++)
        {
            ItemDefinition definition = items[index];
            if (definition == null)
            {
                continue;
            }

            string itemId = definition.ItemId;
            if (!itemLookup.ContainsKey(itemId))
            {
                itemLookup.Add(itemId, definition);
            }
        }
    }

    private static void SanitizeDefinitionList(List<ItemDefinition> definitions)
    {
        if (definitions == null)
        {
            return;
        }

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = definitions.Count - 1; index >= 0; index--)
        {
            ItemDefinition definition = definitions[index];
            if (definition == null)
            {
                definitions.RemoveAt(index);
                continue;
            }

            string itemId = definition.ItemId;
            if (!seenIds.Add(itemId))
            {
                definitions.RemoveAt(index);
            }
        }
    }

    private static void SanitizePresetList(List<ItemStackPreset> presets)
    {
        if (presets == null)
        {
            return;
        }

        for (int index = presets.Count - 1; index >= 0; index--)
        {
            ItemStackPreset preset = presets[index];
            if (preset == null || preset.definition == null)
            {
                presets.RemoveAt(index);
                continue;
            }

            preset.quantity = Mathf.Max(1, preset.quantity);
        }
    }
}
