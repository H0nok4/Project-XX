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
        public int version = 1;
        public List<ItemStackRecord> stashItems = new List<ItemStackRecord>();
        public List<ItemStackRecord> loadoutItems = new List<ItemStackRecord>();
    }

    private const int CurrentVersion = 1;
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

            records.Add(new ItemStackRecord
            {
                itemId = item.Definition.ItemId,
                quantity = item.Quantity
            });
        }

        return records;
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
        return new ProfileData
        {
            version = CurrentVersion,
            stashItems = CreateRecordsFromPresets(catalog != null ? catalog.DefaultStashItems : null),
            loadoutItems = CreateRecordsFromPresets(catalog != null ? catalog.DefaultLoadoutItems : null)
        };
    }

    private static void SanitizeProfile(ProfileData profile, PrototypeItemCatalog catalog)
    {
        if (profile == null)
        {
            return;
        }

        profile.version = CurrentVersion;
        profile.stashItems = SanitizeRecords(profile.stashItems, catalog);
        profile.loadoutItems = SanitizeRecords(profile.loadoutItems, catalog);
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

    private static List<ItemStackRecord> SanitizeRecords(List<ItemStackRecord> records, PrototypeItemCatalog catalog)
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

            string itemId = definition != null ? definition.ItemId : record.itemId.Trim();
            AddRecordQuantity(sanitized, itemId, record.quantity);
        }

        return sanitized;
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
}
