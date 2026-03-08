using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class PrototypeProfileService
{
    [Serializable]
    public sealed class ItemStackRecord
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public sealed class ProfileData
    {
        public int version = CurrentVersion;
        public List<ItemStackRecord> stashItems = new List<ItemStackRecord>();
        public List<ItemStackRecord> loadoutItems = new List<ItemStackRecord>();
        public List<ItemStackRecord> extractedItems = new List<ItemStackRecord>();
        public List<ItemStackRecord> raidBackpackItems = new List<ItemStackRecord>();
        public List<ItemStackRecord> secureContainerItems = new List<ItemStackRecord>();
        public List<ItemStackRecord> specialEquipmentItems = new List<ItemStackRecord>();
        public List<ItemStackRecord> equippedArmorItems = new List<ItemStackRecord>();
        public List<string> stashWeaponIds = new List<string>();
        public string equippedPrimaryWeaponId = string.Empty;
        public string equippedSecondaryWeaponId = string.Empty;
        public string equippedMeleeWeaponId = string.Empty;
    }

    private const int CurrentVersion = 2;
    private const string SaveFileName = "prototype_profile.json";

    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static ProfileData LoadProfile(PrototypeItemCatalog catalog)
    {
        ProfileData profile = null;
        if (File.Exists(SavePath))
        {
            try
            {
                profile = JsonUtility.FromJson<ProfileData>(File.ReadAllText(SavePath));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load prototype profile: {exception.Message}");
            }
        }

        if (profile == null)
        {
            profile = CreateDefaultProfile(catalog);
        }

        SanitizeProfile(profile, catalog);
        return profile;
    }

    public static void SaveProfile(ProfileData profile, PrototypeItemCatalog catalog)
    {
        if (profile == null)
        {
            return;
        }

        SanitizeProfile(profile, catalog);

        string directory = Path.GetDirectoryName(SavePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(profile, true));
    }

    public static void PopulateInventory(InventoryContainer inventory, List<ItemStackRecord> records, PrototypeItemCatalog catalog)
    {
        if (inventory == null)
        {
            return;
        }

        inventory.Clear();
        if (records == null || catalog == null)
        {
            return;
        }

        for (int index = 0; index < records.Count; index++)
        {
            ItemStackRecord record = records[index];
            if (record == null || record.quantity <= 0)
            {
                continue;
            }

            ItemDefinition definition = catalog.FindByItemId(record.itemId);
            if (definition == null)
            {
                continue;
            }

            inventory.TryAddItem(definition, record.quantity, out _);
        }
    }

    public static List<ItemStackRecord> CaptureInventory(InventoryContainer inventory)
    {
        var records = new List<ItemStackRecord>();
        if (inventory == null || inventory.Items == null)
        {
            return records;
        }

        for (int index = 0; index < inventory.Items.Count; index++)
        {
            ItemInstance item = inventory.Items[index];
            if (item == null || !item.IsDefined() || item.Quantity <= 0)
            {
                continue;
            }

            AddRecordQuantity(records, item.Definition.ItemId, item.Quantity);
        }

        return records;
    }

    public static List<ItemStackRecord> CaptureDefinitions(IEnumerable<ItemDefinition> definitions)
    {
        var records = new List<ItemStackRecord>();
        if (definitions == null)
        {
            return records;
        }

        foreach (ItemDefinition definition in definitions)
        {
            if (definition == null)
            {
                continue;
            }

            AddRecordQuantity(records, definition.ItemId, 1);
        }

        return records;
    }

    public static List<ArmorDefinition> ResolveArmorDefinitions(List<ItemStackRecord> records, PrototypeItemCatalog catalog)
    {
        var armorDefinitions = new List<ArmorDefinition>();
        if (records == null || catalog == null)
        {
            return armorDefinitions;
        }

        for (int index = 0; index < records.Count; index++)
        {
            ItemStackRecord record = records[index];
            if (record == null || record.quantity <= 0)
            {
                continue;
            }

            if (!(catalog.FindByItemId(record.itemId) is ArmorDefinition armorDefinition))
            {
                continue;
            }

            for (int count = 0; count < record.quantity; count++)
            {
                armorDefinitions.Add(armorDefinition);
            }
        }

        return armorDefinitions;
    }

    public static List<string> CaptureWeaponIds(IEnumerable<PrototypeWeaponDefinition> weaponDefinitions)
    {
        var weaponIds = new List<string>();
        if (weaponDefinitions == null)
        {
            return weaponIds;
        }

        foreach (PrototypeWeaponDefinition weaponDefinition in weaponDefinitions)
        {
            AddWeaponId(weaponIds, weaponDefinition != null ? weaponDefinition.WeaponId : null);
        }

        return weaponIds;
    }

    public static void MergeInventoryIntoStash(ProfileData profile, InventoryContainer inventory)
    {
        if (profile == null || inventory == null)
        {
            return;
        }

        if (profile.stashItems == null)
        {
            profile.stashItems = new List<ItemStackRecord>();
        }

        for (int index = 0; index < inventory.Items.Count; index++)
        {
            ItemInstance item = inventory.Items[index];
            if (item == null || !item.IsDefined() || item.Quantity <= 0)
            {
                continue;
            }

            AddRecordQuantity(profile.stashItems, item.Definition.ItemId, item.Quantity);
        }
    }

    public static ProfileData CreateDefaultProfile(PrototypeItemCatalog catalog)
    {
        var profile = new ProfileData
        {
            version = CurrentVersion,
            stashItems = CreateRecordsFromPresets(catalog != null ? catalog.DefaultStashItems : null),
            loadoutItems = new List<ItemStackRecord>(),
            extractedItems = new List<ItemStackRecord>(),
            raidBackpackItems = new List<ItemStackRecord>(),
            secureContainerItems = new List<ItemStackRecord>(),
            specialEquipmentItems = new List<ItemStackRecord>(),
            equippedArmorItems = new List<ItemStackRecord>(),
            stashWeaponIds = CreateWeaponIdsFromPresets(catalog != null ? catalog.DefaultStashWeapons : null),
            equippedPrimaryWeaponId = catalog != null && catalog.DefaultPrimaryWeapon != null ? catalog.DefaultPrimaryWeapon.WeaponId : string.Empty,
            equippedSecondaryWeaponId = catalog != null && catalog.DefaultSecondaryWeapon != null ? catalog.DefaultSecondaryWeapon.WeaponId : string.Empty,
            equippedMeleeWeaponId = catalog != null && catalog.DefaultMeleeWeapon != null ? catalog.DefaultMeleeWeapon.WeaponId : string.Empty
        };

        SplitRecordsForRiskLoadout(
            CreateRecordsFromPresets(catalog != null ? catalog.DefaultLoadoutItems : null),
            profile.equippedArmorItems,
            profile.raidBackpackItems,
            catalog);
        return profile;
    }

    private static void SanitizeProfile(ProfileData profile, PrototypeItemCatalog catalog)
    {
        if (profile == null)
        {
            return;
        }

        profile.stashItems ??= new List<ItemStackRecord>();
        profile.loadoutItems ??= new List<ItemStackRecord>();
        profile.extractedItems ??= new List<ItemStackRecord>();
        profile.raidBackpackItems ??= new List<ItemStackRecord>();
        profile.secureContainerItems ??= new List<ItemStackRecord>();
        profile.specialEquipmentItems ??= new List<ItemStackRecord>();
        profile.equippedArmorItems ??= new List<ItemStackRecord>();
        profile.stashWeaponIds ??= new List<string>();

        MigrateLegacyFields(profile, catalog);

        profile.version = CurrentVersion;
        profile.stashItems = SanitizeRecords(profile.stashItems, catalog);
        profile.raidBackpackItems = SanitizeRecords(profile.raidBackpackItems, catalog);
        profile.secureContainerItems = SanitizeRecords(profile.secureContainerItems, catalog);
        profile.specialEquipmentItems = SanitizeRecords(profile.specialEquipmentItems, catalog);
        profile.equippedArmorItems = SanitizeRecords(profile.equippedArmorItems, catalog, definition => definition is ArmorDefinition);
        profile.stashWeaponIds = SanitizeWeaponIds(profile.stashWeaponIds, catalog);
        profile.equippedPrimaryWeaponId = SanitizeWeaponId(profile.equippedPrimaryWeaponId, catalog);
        profile.equippedSecondaryWeaponId = SanitizeWeaponId(profile.equippedSecondaryWeaponId, catalog);
        profile.equippedMeleeWeaponId = SanitizeWeaponId(profile.equippedMeleeWeaponId, catalog);
        profile.loadoutItems = new List<ItemStackRecord>();
        profile.extractedItems = new List<ItemStackRecord>();
    }

    private static void MigrateLegacyFields(ProfileData profile, PrototypeItemCatalog catalog)
    {
        if (profile == null)
        {
            return;
        }

        SplitRecordsForRiskLoadout(profile.loadoutItems, profile.equippedArmorItems, profile.raidBackpackItems, catalog);
        AppendSanitizedRecords(profile.raidBackpackItems, profile.extractedItems, catalog);

        bool hasAnyWeaponConfigured =
            !string.IsNullOrWhiteSpace(profile.equippedPrimaryWeaponId)
            || !string.IsNullOrWhiteSpace(profile.equippedSecondaryWeaponId)
            || !string.IsNullOrWhiteSpace(profile.equippedMeleeWeaponId)
            || (profile.stashWeaponIds != null && profile.stashWeaponIds.Count > 0);

        if (hasAnyWeaponConfigured || catalog == null)
        {
            return;
        }

        profile.stashWeaponIds = CreateWeaponIdsFromPresets(catalog.DefaultStashWeapons);
        profile.equippedPrimaryWeaponId = catalog.DefaultPrimaryWeapon != null ? catalog.DefaultPrimaryWeapon.WeaponId : string.Empty;
        profile.equippedSecondaryWeaponId = catalog.DefaultSecondaryWeapon != null ? catalog.DefaultSecondaryWeapon.WeaponId : string.Empty;
        profile.equippedMeleeWeaponId = catalog.DefaultMeleeWeapon != null ? catalog.DefaultMeleeWeapon.WeaponId : string.Empty;
    }

    private static void SplitRecordsForRiskLoadout(
        IReadOnlyList<ItemStackRecord> sourceRecords,
        List<ItemStackRecord> armorRecords,
        List<ItemStackRecord> backpackRecords,
        PrototypeItemCatalog catalog)
    {
        if (sourceRecords == null || sourceRecords.Count == 0)
        {
            return;
        }

        armorRecords ??= new List<ItemStackRecord>();
        backpackRecords ??= new List<ItemStackRecord>();

        for (int index = 0; index < sourceRecords.Count; index++)
        {
            ItemStackRecord record = sourceRecords[index];
            if (record == null || string.IsNullOrWhiteSpace(record.itemId) || record.quantity <= 0)
            {
                continue;
            }

            ItemDefinition definition = catalog != null ? catalog.FindByItemId(record.itemId) : null;
            if (catalog != null && definition == null)
            {
                continue;
            }

            List<ItemStackRecord> targetRecords = definition is ArmorDefinition ? armorRecords : backpackRecords;
            AddRecordQuantity(targetRecords, definition != null ? definition.ItemId : record.itemId, record.quantity);
        }
    }

    private static void AppendSanitizedRecords(
        List<ItemStackRecord> targetRecords,
        IReadOnlyList<ItemStackRecord> sourceRecords,
        PrototypeItemCatalog catalog)
    {
        if (targetRecords == null || sourceRecords == null)
        {
            return;
        }

        for (int index = 0; index < sourceRecords.Count; index++)
        {
            ItemStackRecord record = sourceRecords[index];
            if (record == null || string.IsNullOrWhiteSpace(record.itemId) || record.quantity <= 0)
            {
                continue;
            }

            ItemDefinition definition = catalog != null ? catalog.FindByItemId(record.itemId) : null;
            if (catalog != null && definition == null)
            {
                continue;
            }

            AddRecordQuantity(targetRecords, definition != null ? definition.ItemId : record.itemId, record.quantity);
        }
    }

    private static List<ItemStackRecord> CreateRecordsFromPresets(IReadOnlyList<PrototypeItemCatalog.ItemStackPreset> presets)
    {
        var records = new List<ItemStackRecord>();
        if (presets == null)
        {
            return records;
        }

        for (int index = 0; index < presets.Count; index++)
        {
            PrototypeItemCatalog.ItemStackPreset preset = presets[index];
            if (preset == null || preset.definition == null)
            {
                continue;
            }

            AddRecordQuantity(records, preset.definition.ItemId, preset.quantity);
        }

        return records;
    }

    private static List<string> CreateWeaponIdsFromPresets(IReadOnlyList<PrototypeItemCatalog.WeaponPreset> presets)
    {
        var weaponIds = new List<string>();
        if (presets == null)
        {
            return weaponIds;
        }

        for (int index = 0; index < presets.Count; index++)
        {
            PrototypeItemCatalog.WeaponPreset preset = presets[index];
            if (preset?.definition == null)
            {
                continue;
            }

            for (int count = 0; count < Mathf.Max(1, preset.quantity); count++)
            {
                AddWeaponId(weaponIds, preset.definition.WeaponId);
            }
        }

        return weaponIds;
    }

    private static List<ItemStackRecord> SanitizeRecords(
        List<ItemStackRecord> records,
        PrototypeItemCatalog catalog,
        Predicate<ItemDefinition> predicate = null)
    {
        var sanitized = new List<ItemStackRecord>();
        if (records == null)
        {
            return sanitized;
        }

        for (int index = 0; index < records.Count; index++)
        {
            ItemStackRecord record = records[index];
            if (record == null || string.IsNullOrWhiteSpace(record.itemId) || record.quantity <= 0)
            {
                continue;
            }

            ItemDefinition definition = catalog != null ? catalog.FindByItemId(record.itemId) : null;
            if (catalog != null && definition == null)
            {
                continue;
            }

            if (predicate != null && definition != null && !predicate(definition))
            {
                continue;
            }

            string itemId = definition != null ? definition.ItemId : record.itemId.Trim();
            AddRecordQuantity(sanitized, itemId, record.quantity);
        }

        return sanitized;
    }

    private static List<string> SanitizeWeaponIds(List<string> weaponIds, PrototypeItemCatalog catalog)
    {
        var sanitized = new List<string>();
        if (weaponIds == null)
        {
            return sanitized;
        }

        for (int index = 0; index < weaponIds.Count; index++)
        {
            string sanitizedId = SanitizeWeaponId(weaponIds[index], catalog);
            if (!string.IsNullOrWhiteSpace(sanitizedId))
            {
                sanitized.Add(sanitizedId);
            }
        }

        return sanitized;
    }

    private static string SanitizeWeaponId(string weaponId, PrototypeItemCatalog catalog)
    {
        if (string.IsNullOrWhiteSpace(weaponId))
        {
            return string.Empty;
        }

        PrototypeWeaponDefinition definition = catalog != null ? catalog.FindWeaponById(weaponId) : null;
        if (catalog != null && definition == null)
        {
            return string.Empty;
        }

        return definition != null ? definition.WeaponId : weaponId.Trim();
    }

    private static void AddRecordQuantity(List<ItemStackRecord> records, string itemId, int quantity)
    {
        if (records == null || string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return;
        }

        for (int index = 0; index < records.Count; index++)
        {
            ItemStackRecord record = records[index];
            if (record != null && string.Equals(record.itemId, itemId, StringComparison.OrdinalIgnoreCase))
            {
                record.quantity += quantity;
                return;
            }
        }

        records.Add(new ItemStackRecord
        {
            itemId = itemId,
            quantity = quantity
        });
    }

    private static void AddWeaponId(List<string> weaponIds, string weaponId)
    {
        if (weaponIds == null || string.IsNullOrWhiteSpace(weaponId))
        {
            return;
        }

        weaponIds.Add(weaponId.Trim());
    }
}
