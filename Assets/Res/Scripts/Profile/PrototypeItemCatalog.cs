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

    [Serializable]
    public sealed class WeaponPreset
    {
        public PrototypeWeaponDefinition definition;
        [Min(1)]
        public int quantity = 1;
    }

    [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();
    [SerializeField] private List<PrototypeWeaponDefinition> weapons = new List<PrototypeWeaponDefinition>();
    [SerializeField] private List<ItemStackPreset> defaultStashItems = new List<ItemStackPreset>();
    [SerializeField] private List<ItemStackPreset> defaultLoadoutItems = new List<ItemStackPreset>();
    [SerializeField] private List<WeaponPreset> defaultStashWeapons = new List<WeaponPreset>();
    [SerializeField] private List<WeaponPreset> defaultSpecialEquipmentWeapons = new List<WeaponPreset>();
    [SerializeField] private PrototypeWeaponDefinition defaultPrimaryWeapon;
    [SerializeField] private PrototypeWeaponDefinition defaultSecondaryWeapon;
    [SerializeField] private PrototypeWeaponDefinition defaultMeleeWeapon;

    private Dictionary<string, ItemDefinition> itemLookup;
    private Dictionary<string, PrototypeWeaponDefinition> weaponLookup;

    public IReadOnlyList<ItemDefinition> Items => items;
    public IReadOnlyList<PrototypeWeaponDefinition> Weapons => weapons;
    public IReadOnlyList<ItemStackPreset> DefaultStashItems => defaultStashItems;
    public IReadOnlyList<ItemStackPreset> DefaultLoadoutItems => defaultLoadoutItems;
    public IReadOnlyList<WeaponPreset> DefaultStashWeapons => defaultStashWeapons;
    public IReadOnlyList<WeaponPreset> DefaultSpecialEquipmentWeapons => defaultSpecialEquipmentWeapons;
    public PrototypeWeaponDefinition DefaultPrimaryWeapon => defaultPrimaryWeapon;
    public PrototypeWeaponDefinition DefaultSecondaryWeapon => defaultSecondaryWeapon;
    public PrototypeWeaponDefinition DefaultMeleeWeapon => defaultMeleeWeapon;

    public IEnumerable<ItemDefinitionBase> EnumerateDefinitions()
    {
        if (items != null)
        {
            for (int index = 0; index < items.Count; index++)
            {
                ItemDefinition definition = items[index];
                if (definition != null)
                {
                    yield return definition;
                }
            }
        }

        if (weapons != null)
        {
            for (int index = 0; index < weapons.Count; index++)
            {
                PrototypeWeaponDefinition definition = weapons[index];
                if (definition != null)
                {
                    yield return definition;
                }
            }
        }
    }

    public ItemDefinitionBase FindDefinitionById(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
        {
            return null;
        }

        ItemDefinition itemDefinition = FindByItemId(definitionId);
        if (itemDefinition != null)
        {
            return itemDefinition;
        }

        return FindWeaponById(definitionId);
    }

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

    public PrototypeWeaponDefinition FindWeaponById(string weaponId)
    {
        if (string.IsNullOrWhiteSpace(weaponId))
        {
            return null;
        }

        EnsureLookup();
        weaponLookup.TryGetValue(weaponId.Trim(), out PrototypeWeaponDefinition definition);
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
        SanitizeWeaponList(weapons);
        SanitizePresetList(defaultStashItems);
        SanitizePresetList(defaultLoadoutItems);
        SanitizeWeaponPresetList(defaultStashWeapons);
        SanitizeWeaponPresetList(defaultSpecialEquipmentWeapons);

        EnsureWeaponRegistered(defaultPrimaryWeapon);
        EnsureWeaponRegistered(defaultSecondaryWeapon);
        EnsureWeaponRegistered(defaultMeleeWeapon);
        EnsurePresetWeaponsRegistered(defaultStashWeapons);
        EnsurePresetWeaponsRegistered(defaultSpecialEquipmentWeapons);

        itemLookup = null;
        weaponLookup = null;
    }

    private void EnsureLookup()
    {
        if (itemLookup != null && weaponLookup != null)
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

        weaponLookup = new Dictionary<string, PrototypeWeaponDefinition>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < weapons.Count; index++)
        {
            PrototypeWeaponDefinition definition = weapons[index];
            if (definition == null)
            {
                continue;
            }

            string weaponId = definition.WeaponId;
            if (!weaponLookup.ContainsKey(weaponId))
            {
                weaponLookup.Add(weaponId, definition);
            }
        }
    }

    private void EnsureWeaponRegistered(PrototypeWeaponDefinition weaponDefinition)
    {
        if (weaponDefinition == null || weapons.Contains(weaponDefinition))
        {
            return;
        }

        weapons.Add(weaponDefinition);
    }

    private void EnsurePresetWeaponsRegistered(IReadOnlyList<WeaponPreset> presets)
    {
        if (presets == null)
        {
            return;
        }

        for (int index = 0; index < presets.Count; index++)
        {
            WeaponPreset preset = presets[index];
            if (preset?.definition != null)
            {
                EnsureWeaponRegistered(preset.definition);
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

    private static void SanitizeWeaponList(List<PrototypeWeaponDefinition> definitions)
    {
        if (definitions == null)
        {
            return;
        }

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = definitions.Count - 1; index >= 0; index--)
        {
            PrototypeWeaponDefinition definition = definitions[index];
            if (definition == null)
            {
                definitions.RemoveAt(index);
                continue;
            }

            string weaponId = definition.WeaponId;
            if (!seenIds.Add(weaponId))
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

    private static void SanitizeWeaponPresetList(List<WeaponPreset> presets)
    {
        if (presets == null)
        {
            return;
        }

        for (int index = presets.Count - 1; index >= 0; index--)
        {
            WeaponPreset preset = presets[index];
            if (preset == null || preset.definition == null)
            {
                presets.RemoveAt(index);
                continue;
            }

            preset.quantity = Mathf.Max(1, preset.quantity);
        }
    }
}
