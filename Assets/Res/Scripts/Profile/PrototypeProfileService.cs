using System;
using System.Collections.Generic;
using UnityEngine;

public static class PrototypeProfileService
{
    private const string CashBundleItemId = "cash_bundle";

    [Serializable]
    public sealed class ItemStackRecord
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public sealed class ProfileData
    {
        public int profileSchemaVersion = ProfileSchemaVersion.CurrentProfileSchemaVersion;
        public int legacyVersion = ProfileSchemaVersion.CurrentLegacyVersion;
        public int version = ProfileSchemaVersion.CurrentLegacyVersion;
        public WorldStateData worldState = new WorldStateData();
        public PlayerProgressionData progression = new PlayerProgressionData();
        public int funds;
        public List<SavedItemInstanceDto> stashItemInstances = new List<SavedItemInstanceDto>();
        public List<SavedItemInstanceDto> raidBackpackItemInstances = new List<SavedItemInstanceDto>();
        public List<SavedItemInstanceDto> secureContainerItemInstances = new List<SavedItemInstanceDto>();
        public List<SavedItemInstanceDto> specialEquipmentItemInstances = new List<SavedItemInstanceDto>();
        public List<SavedArmorInstanceDto> equippedArmorInstances = new List<SavedArmorInstanceDto>();
        public List<SavedItemInstanceDto> stashWeaponInstances = new List<SavedItemInstanceDto>();
        public List<SavedItemInstanceDto> raidBackpackWeaponInstances = new List<SavedItemInstanceDto>();
        public SavedItemInstanceDto equippedSecureContainerInstance;
        public SavedItemInstanceDto equippedPrimaryWeaponInstance;
        public SavedItemInstanceDto equippedSecondaryWeaponInstance;
        public SavedItemInstanceDto equippedMeleeWeaponInstance;
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

    public static string SavePath => ProfileFileGateway.SavePath;

    public static ProfileData LoadProfile(PrototypeItemCatalog catalog)
    {
        if (!ProfileFileGateway.TryReadRawJson(out string rawJson, out string readError))
        {
            if (!string.IsNullOrWhiteSpace(readError))
            {
                Debug.LogWarning($"Failed to read prototype profile: {readError}");
            }

            return CreateDefaultProfile(catalog);
        }

        ProfileMigrationRunner.ProfileMigrationResult migration = ProfileMigrationRunner.ParseAndUpgrade(rawJson, catalog);
        ProfileData profile = migration.Profile;
        if (profile == null)
        {
            PersistDiagnostics(migration.Diagnostics, migration.Upgraded || migration.HasErrors || migration.HasWarnings);
            return CreateDefaultProfile(catalog);
        }

        SanitizeProfile(profile, catalog);

        if (migration.Upgraded)
        {
            if (ProfileFileGateway.TryWriteBackup(rawJson, out string backupPath, out string backupError))
            {
                migration.Diagnostics?.Info($"Created pre-migration backup at {backupPath}.");
                Debug.Log($"[PrototypeProfileService] Backed up legacy profile to {backupPath}");
            }
            else if (!string.IsNullOrWhiteSpace(backupError))
            {
                migration.Diagnostics?.Warning($"Failed to create migration backup: {backupError}");
                Debug.LogWarning($"Failed to back up legacy profile before migration: {backupError}");
            }

            if (TrySaveProfileInternal(profile, catalog, out _))
            {
                migration.Diagnostics?.Info("Rewrote migrated profile using the current schema-aware save path.");
            }
            else
            {
                migration.Diagnostics?.Warning("Failed to persist the migrated profile back to disk.");
            }
        }

        PersistDiagnostics(migration.Diagnostics, migration.Upgraded || migration.HasErrors || migration.HasWarnings);
        return profile;
    }

    public static void SaveProfile(ProfileData profile, PrototypeItemCatalog catalog)
    {
        TrySaveProfileInternal(profile, catalog, out _);
    }

    private static bool TrySaveProfileInternal(
        ProfileData profile,
        PrototypeItemCatalog catalog,
        out ProfileDiagnostics.Report saveReport)
    {
        saveReport = null;
        if (profile == null)
        {
            return false;
        }

        saveReport = ProfileDiagnostics.ValidateBeforeSave(profile);
        SanitizeProfile(profile, catalog);

        if (saveReport.HasErrors)
        {
            PersistDiagnostics(saveReport, true);
            return false;
        }

        if (!ProfileFileGateway.TryWriteJson(JsonUtility.ToJson(profile, true), out string writeError))
        {
            Debug.LogError($"Failed to save prototype profile: {writeError}");
            return false;
        }

        if (saveReport.HasWarnings)
        {
            PersistDiagnostics(saveReport, true);
        }

        return true;
    }

    private static void PersistDiagnostics(ProfileDiagnostics.Report report, bool writeLogFile)
    {
        if (report == null || !report.HasEntries)
        {
            return;
        }

        ProfileDiagnostics.FlushToConsole(report);
        if (!writeLogFile)
        {
            return;
        }

        if (!ProfileFileGateway.TryWriteMigrationLog(report.BuildText(), out string logPath, out string logError))
        {
            Debug.LogWarning($"Failed to persist profile diagnostics log: {logError}");
            return;
        }

        Debug.Log($"[PrototypeProfileService] Wrote profile diagnostics to {logPath}");
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

    public static void PopulateInventoryInstances(InventoryContainer inventory, List<SavedItemInstanceDto> records, PrototypeItemCatalog catalog)
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
            SavedItemInstanceDto record = records[index];
            if (record == null)
            {
                continue;
            }

            if (!TryCreateInventoryItemInstance(record, catalog, out ItemInstance instance) || instance == null)
            {
                continue;
            }

            if (!inventory.TryAddItemInstance(instance))
            {
                break;
            }
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
            if (item == null || !item.IsDefined() || item.Quantity <= 0 || item.IsWeapon || item.Definition == null)
            {
                continue;
            }

            AddRecordQuantity(records, item.Definition.ItemId, item.Quantity);
        }

        return records;
    }

    public static List<SavedItemInstanceDto> CaptureInventoryInstances(InventoryContainer inventory)
    {
        var records = new List<SavedItemInstanceDto>();
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

            item.Sanitize();
            SavedItemInstanceDto record = CreateInventoryItemInstanceRecord(item);
            if (record != null)
            {
                records.Add(record);
            }
        }

        return records;
    }

    public static ItemInstance ResolveItemInstance(SavedItemInstanceDto record, PrototypeItemCatalog catalog)
    {
        return TryCreateInventoryItemInstance(record, catalog, out ItemInstance instance) ? instance : null;
    }

    public static SavedItemInstanceDto CaptureItemInstance(ItemInstance item)
    {
        return CreateInventoryItemInstanceRecord(item);
    }

    private static bool TryCreateInventoryItemInstance(
        SavedItemInstanceDto record,
        PrototypeItemCatalog catalog,
        out ItemInstance instance)
    {
        instance = null;
        if (record == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(record.weaponId))
        {
            PrototypeWeaponDefinition weaponDefinition = catalog != null ? catalog.FindWeaponById(record.weaponId) : null;
            if (catalog != null && weaponDefinition == null)
            {
                return false;
            }

            string instanceId = EnsureInstanceId(record.instanceId);
            string weaponId = weaponDefinition != null ? weaponDefinition.WeaponId : record.weaponId.Trim();
            int magazineAmmo = weaponDefinition != null && !weaponDefinition.IsMeleeWeapon
                ? Mathf.Clamp(record.magazineAmmo, 0, weaponDefinition.MagazineSize)
                : 0;
            float durability = Mathf.Max(0f, record.durability >= 0f ? record.durability : 1f);
            PrototypeWeaponDefinition resolvedDefinition = weaponDefinition;
            if (resolvedDefinition == null && catalog != null)
            {
                resolvedDefinition = catalog.FindWeaponById(weaponId);
            }

            instance = resolvedDefinition != null
                ? ItemInstance.Create(resolvedDefinition, magazineAmmo, durability, instanceId, record.rarity, record.affixes, false, record.skills, false)
                : null;
            return instance != null;
        }

        if (string.IsNullOrWhiteSpace(record.itemId) || record.quantity <= 0)
        {
            return false;
        }

        ItemDefinition definition = catalog != null ? catalog.FindByItemId(record.itemId) : null;
        if (catalog != null && definition == null)
        {
            return false;
        }

        if (definition is ArmorDefinition armorDefinition)
        {
            float durability = GetStoredArmorDurability(armorDefinition, record.rarity, record.durability);
            instance = ItemInstance.Create(armorDefinition, durability, EnsureInstanceId(record.instanceId), record.rarity, record.affixes, false, record.skills, false);
            return true;
        }

        instance = ItemInstance.Create(definition, Mathf.Max(1, record.quantity), EnsureInstanceId(record.instanceId), record.rarity, record.affixes, false, record.skills, false);
        return instance != null;
    }

    private static SavedItemInstanceDto CreateInventoryItemInstanceRecord(ItemInstance item)
    {
        if (item == null || !item.IsDefined())
        {
            return null;
        }

        if (item.IsWeapon && item.WeaponDefinition != null)
        {
            return new SavedItemInstanceDto
            {
                instanceId = item.InstanceId,
                weaponId = item.WeaponDefinition.WeaponId,
                rarity = item.Rarity,
                quantity = 1,
                magazineAmmo = item.MagazineAmmo,
                durability = Mathf.Max(0f, item.CurrentDurability),
                affixes = ItemAffixUtility.CloneList(item.Affixes),
                skills = ItemSkillUtility.CloneList(item.Skills)
            };
        }

        if (item.Definition == null)
        {
            return null;
        }

        return new SavedItemInstanceDto
        {
            instanceId = item.InstanceId,
            itemId = item.Definition.ItemId,
            rarity = item.Rarity,
            quantity = item.IsArmor ? 1 : item.Quantity,
            durability = item.IsArmor ? Mathf.Max(0f, item.CurrentDurability) : -1f,
            affixes = ItemAffixUtility.CloneList(item.Affixes),
            skills = ItemSkillUtility.CloneList(item.Skills)
        };
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

    public static List<ItemStackRecord> CaptureArmorDefinitions(IEnumerable<ArmorInstance> armorInstances)
    {
        var records = new List<ItemStackRecord>();
        if (armorInstances == null)
        {
            return records;
        }

        foreach (ArmorInstance armorInstance in armorInstances)
        {
            if (armorInstance == null || armorInstance.Definition == null)
            {
                continue;
            }

            AddRecordQuantity(records, armorInstance.Definition.ItemId, 1);
        }

        return records;
    }

    public static List<ItemStackRecord> CaptureArmorDefinitions(IEnumerable<PrototypeUnitVitals.ArmorState> armorStates)
    {
        var records = new List<ItemStackRecord>();
        if (armorStates == null)
        {
            return records;
        }

        foreach (PrototypeUnitVitals.ArmorState armorState in armorStates)
        {
            if (armorState == null || armorState.definition == null)
            {
                continue;
            }

            AddRecordQuantity(records, armorState.definition.ItemId, 1);
        }

        return records;
    }

    public static List<SavedArmorInstanceDto> CaptureArmorInstances(IEnumerable<ArmorInstance> armorInstances)
    {
        var records = new List<SavedArmorInstanceDto>();
        if (armorInstances == null)
        {
            return records;
        }

        foreach (ArmorInstance armorInstance in armorInstances)
        {
            if (armorInstance == null || armorInstance.Definition == null)
            {
                continue;
            }

                records.Add(new SavedArmorInstanceDto
                {
                    instanceId = EnsureInstanceId(armorInstance.InstanceId),
                    itemId = armorInstance.Definition.ItemId,
                    rarity = armorInstance.Rarity,
                    currentDurability = armorInstance.CurrentDurability,
                    affixes = ItemAffixUtility.CloneList(armorInstance.Affixes),
                    skills = ItemSkillUtility.CloneList(armorInstance.Skills)
                });
        }

        return records;
    }

    public static List<SavedArmorInstanceDto> CaptureArmorInstances(IEnumerable<PrototypeUnitVitals.ArmorState> armorStates)
    {
        var records = new List<SavedArmorInstanceDto>();
        if (armorStates == null)
        {
            return records;
        }

        foreach (PrototypeUnitVitals.ArmorState armorState in armorStates)
        {
            if (armorState == null || armorState.definition == null)
            {
                continue;
            }

                records.Add(new SavedArmorInstanceDto
                {
                    instanceId = EnsureInstanceId(armorState.instanceId),
                    itemId = armorState.definition.ItemId,
                    rarity = armorState.Rarity,
                    currentDurability = armorState.currentDurability,
                    affixes = ItemAffixUtility.CloneList(armorState.affixes),
                    skills = ItemSkillUtility.CloneList(armorState.skills)
                });
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

    public static List<ArmorInstance> ResolveArmorInstances(List<SavedArmorInstanceDto> records, PrototypeItemCatalog catalog)
    {
        var armorInstances = new List<ArmorInstance>();
        if (records == null || catalog == null)
        {
            return armorInstances;
        }

        for (int index = 0; index < records.Count; index++)
        {
            SavedArmorInstanceDto record = records[index];
            if (record == null)
            {
                continue;
            }

            if (!(catalog.FindByItemId(record.itemId) is ArmorDefinition armorDefinition))
            {
                continue;
            }

            ArmorInstance instance = ArmorInstance.Create(armorDefinition, record.currentDurability, record.instanceId, record.rarity, record.affixes, false, record.skills, false);
            armorInstances.Add(instance);
        }

        return armorInstances;
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

    public static List<string> CaptureWeaponIds(IEnumerable<ItemInstance> weaponInstances)
    {
        var weaponIds = new List<string>();
        if (weaponInstances == null)
        {
            return weaponIds;
        }

        foreach (ItemInstance weaponInstance in weaponInstances)
        {
            if (weaponInstance == null || !weaponInstance.IsWeapon || weaponInstance.WeaponDefinition == null)
            {
                continue;
            }

            AddWeaponId(weaponIds, weaponInstance.WeaponDefinition.WeaponId);
        }

        return weaponIds;
    }

    public static List<SavedItemInstanceDto> CaptureWeaponInstances(IEnumerable<ItemInstance> weaponInstances)
    {
        var records = new List<SavedItemInstanceDto>();
        if (weaponInstances == null)
        {
            return records;
        }

        foreach (ItemInstance weaponInstance in weaponInstances)
        {
            SavedItemInstanceDto record = CaptureWeaponInstance(weaponInstance);
            if (record != null)
            {
                records.Add(record);
            }
        }

        return records;
    }

    public static SavedItemInstanceDto CaptureWeaponInstance(ItemInstance weaponInstance)
    {
        if (weaponInstance == null || !weaponInstance.IsWeapon || weaponInstance.WeaponDefinition == null)
        {
            return null;
        }

        weaponInstance.Sanitize();
        return CreateInventoryItemInstanceRecord(weaponInstance);
    }

    public static ItemInstance ResolveWeaponInstance(SavedItemInstanceDto record, PrototypeItemCatalog catalog)
    {
        if (record == null)
        {
            return null;
        }

        if (!TryCreateInventoryItemInstance(record, catalog, out ItemInstance instance))
        {
            return null;
        }

        return instance != null && instance.IsWeapon ? instance : null;
    }

    public static List<ItemInstance> ResolveWeaponInstances(List<SavedItemInstanceDto> records, PrototypeItemCatalog catalog)
    {
        var weaponInstances = new List<ItemInstance>();
        if (records == null || catalog == null)
        {
            return weaponInstances;
        }

        for (int index = 0; index < records.Count; index++)
        {
            ItemInstance instance = ResolveWeaponInstance(records[index], catalog);
            if (instance != null)
            {
                weaponInstances.Add(instance);
            }
        }

        return weaponInstances;
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
            if (item == null || !item.IsDefined() || item.Quantity <= 0 || item.IsWeapon || item.Definition == null)
            {
                continue;
            }

            if (IsCashBundleItemId(item.Definition.ItemId))
            {
                AddFunds(profile, item.Quantity);
                continue;
            }

            AddRecordQuantity(profile.stashItems, item.Definition.ItemId, item.Quantity);
        }
    }

    public static ProfileData CreateDefaultProfile(PrototypeItemCatalog catalog)
    {
        var profile = new ProfileData
        {
            profileSchemaVersion = ProfileSchemaVersion.CurrentProfileSchemaVersion,
            legacyVersion = ProfileSchemaVersion.CurrentLegacyVersion,
            version = ProfileSchemaVersion.CurrentLegacyVersion,
            worldState = new WorldStateData(),
            progression = new PlayerProgressionData(),
            funds = 0,
            specialEquipmentItemInstances = CreateInventoryWeaponInstanceDtosFromPresets(catalog != null ? catalog.DefaultSpecialEquipmentWeapons : null),
            stashItems = CreateRecordsFromPresets(catalog != null ? catalog.DefaultStashItems : null),
            loadoutItems = new List<ItemStackRecord>(),
            extractedItems = new List<ItemStackRecord>(),
            raidBackpackItems = new List<ItemStackRecord>(),
            secureContainerItems = new List<ItemStackRecord>(),
            specialEquipmentItems = new List<ItemStackRecord>(),
            equippedArmorItems = new List<ItemStackRecord>(),
            stashWeaponIds = CreateWeaponIdsFromPresets(catalog != null ? catalog.DefaultStashWeapons : null),
            raidBackpackWeaponInstances = new List<SavedItemInstanceDto>(),
            equippedSecureContainerInstance = CaptureItemInstance(PrototypeRaidInventoryRules.CreateDefaultSecureContainerInstance(catalog)),
            equippedPrimaryWeaponId = catalog != null && catalog.DefaultPrimaryWeapon != null ? catalog.DefaultPrimaryWeapon.WeaponId : string.Empty,
            equippedSecondaryWeaponId = catalog != null && catalog.DefaultSecondaryWeapon != null ? catalog.DefaultSecondaryWeapon.WeaponId : string.Empty,
            equippedMeleeWeaponId = catalog != null && catalog.DefaultMeleeWeapon != null ? catalog.DefaultMeleeWeapon.WeaponId : string.Empty
        };

        SplitRecordsForRiskLoadout(
            CreateRecordsFromPresets(catalog != null ? catalog.DefaultLoadoutItems : null),
            profile.equippedArmorItems,
            profile.raidBackpackItems,
            catalog);
        ApplyInstanceMigration(profile, catalog);
        MigrateStashCurrencyToFunds(profile);
        return profile;
    }

    private static List<SavedItemInstanceDto> CreateInventoryWeaponInstanceDtosFromPresets(IReadOnlyList<PrototypeItemCatalog.WeaponPreset> presets)
    {
        var records = new List<SavedItemInstanceDto>();
        if (presets == null)
        {
            return records;
        }

        for (int presetIndex = 0; presetIndex < presets.Count; presetIndex++)
        {
            PrototypeItemCatalog.WeaponPreset preset = presets[presetIndex];
            if (preset?.definition == null)
            {
                continue;
            }

            int quantity = Mathf.Max(1, preset.quantity);
            for (int instanceIndex = 0; instanceIndex < quantity; instanceIndex++)
            {
                ItemInstance instance = ItemInstance.Create(
                    preset.definition,
                    preset.definition.IsMeleeWeapon ? 0 : preset.definition.MagazineSize,
                    1f,
                    null,
                    ItemRarity.Common,
                    null,
                    false,
                    null,
                    false);
                SavedItemInstanceDto record = CreateInventoryItemInstanceRecord(instance);
                if (record != null)
                {
                    records.Add(record);
                }
            }
        }

        return records;
    }

    internal static void ApplyLegacyCompatibilityMigrations(ProfileData profile, PrototypeItemCatalog catalog)
    {
        if (profile == null)
        {
            return;
        }

        profile.equippedArmorItems ??= new List<ItemStackRecord>();
        profile.raidBackpackItems ??= new List<ItemStackRecord>();
        profile.stashWeaponIds ??= new List<string>();

        SplitRecordsForRiskLoadout(profile.loadoutItems, profile.equippedArmorItems, profile.raidBackpackItems, catalog);
        AppendSanitizedRecords(profile.raidBackpackItems, profile.extractedItems, catalog);

        bool hasAnyWeaponConfigured =
            !string.IsNullOrWhiteSpace(profile.equippedPrimaryWeaponId)
            || !string.IsNullOrWhiteSpace(profile.equippedSecondaryWeaponId)
            || !string.IsNullOrWhiteSpace(profile.equippedMeleeWeaponId)
            || profile.stashWeaponIds.Count > 0;

        if (hasAnyWeaponConfigured || catalog == null)
        {
            return;
        }

        profile.stashWeaponIds = CreateWeaponIdsFromPresets(catalog.DefaultStashWeapons);
        profile.equippedPrimaryWeaponId = catalog.DefaultPrimaryWeapon != null ? catalog.DefaultPrimaryWeapon.WeaponId : string.Empty;
        profile.equippedSecondaryWeaponId = catalog.DefaultSecondaryWeapon != null ? catalog.DefaultSecondaryWeapon.WeaponId : string.Empty;
        profile.equippedMeleeWeaponId = catalog.DefaultMeleeWeapon != null ? catalog.DefaultMeleeWeapon.WeaponId : string.Empty;
    }

    internal static bool ApplyInstanceMigration(ProfileData profile, PrototypeItemCatalog catalog)
    {
        if (profile == null)
        {
            return false;
        }

        bool migrated = false;

        profile.stashItemInstances ??= new List<SavedItemInstanceDto>();
        if (profile.stashItemInstances.Count == 0 && profile.stashItems != null && profile.stashItems.Count > 0)
        {
            profile.stashItemInstances = CreateItemInstanceDtosFromRecords(profile.stashItems, catalog);
            migrated |= profile.stashItemInstances.Count > 0;
        }

        profile.raidBackpackItemInstances ??= new List<SavedItemInstanceDto>();
        if (profile.raidBackpackItemInstances.Count == 0 && profile.raidBackpackItems != null && profile.raidBackpackItems.Count > 0)
        {
            profile.raidBackpackItemInstances = CreateItemInstanceDtosFromRecords(profile.raidBackpackItems, catalog);
            migrated |= profile.raidBackpackItemInstances.Count > 0;
        }

        profile.secureContainerItemInstances ??= new List<SavedItemInstanceDto>();
        if (profile.secureContainerItemInstances.Count == 0 && profile.secureContainerItems != null && profile.secureContainerItems.Count > 0)
        {
            profile.secureContainerItemInstances = CreateItemInstanceDtosFromRecords(profile.secureContainerItems, catalog);
            migrated |= profile.secureContainerItemInstances.Count > 0;
        }

        profile.specialEquipmentItemInstances ??= new List<SavedItemInstanceDto>();
        if (profile.specialEquipmentItemInstances.Count == 0 && profile.specialEquipmentItems != null && profile.specialEquipmentItems.Count > 0)
        {
            profile.specialEquipmentItemInstances = CreateItemInstanceDtosFromRecords(profile.specialEquipmentItems, catalog);
            migrated |= profile.specialEquipmentItemInstances.Count > 0;
        }

        profile.equippedArmorInstances ??= new List<SavedArmorInstanceDto>();
        if (profile.equippedArmorInstances.Count == 0 && profile.equippedArmorItems != null && profile.equippedArmorItems.Count > 0)
        {
            profile.equippedArmorInstances = CreateArmorInstanceDtosFromRecords(profile.equippedArmorItems, catalog);
            migrated |= profile.equippedArmorInstances.Count > 0;
        }

        profile.stashWeaponInstances ??= new List<SavedItemInstanceDto>();
        profile.raidBackpackWeaponInstances ??= new List<SavedItemInstanceDto>();
        if (profile.stashWeaponInstances.Count == 0 && profile.stashWeaponIds != null && profile.stashWeaponIds.Count > 0)
        {
            profile.stashWeaponInstances = CreateWeaponInstanceDtosFromIds(profile.stashWeaponIds, catalog);
            migrated |= profile.stashWeaponInstances.Count > 0;
        }

        if (profile.equippedPrimaryWeaponInstance == null && !string.IsNullOrWhiteSpace(profile.equippedPrimaryWeaponId))
        {
            profile.equippedPrimaryWeaponInstance = CreateWeaponInstanceDtoFromId(profile.equippedPrimaryWeaponId, catalog);
            migrated |= profile.equippedPrimaryWeaponInstance != null;
        }

        if (profile.equippedSecondaryWeaponInstance == null && !string.IsNullOrWhiteSpace(profile.equippedSecondaryWeaponId))
        {
            profile.equippedSecondaryWeaponInstance = CreateWeaponInstanceDtoFromId(profile.equippedSecondaryWeaponId, catalog);
            migrated |= profile.equippedSecondaryWeaponInstance != null;
        }

        if (profile.equippedMeleeWeaponInstance == null && !string.IsNullOrWhiteSpace(profile.equippedMeleeWeaponId))
        {
            profile.equippedMeleeWeaponInstance = CreateWeaponInstanceDtoFromId(profile.equippedMeleeWeaponId, catalog);
            migrated |= profile.equippedMeleeWeaponInstance != null;
        }

        return migrated;
    }

    private static void SanitizeProfile(ProfileData profile, PrototypeItemCatalog catalog)
    {
        if (profile == null)
        {
            return;
        }

        profile.profileSchemaVersion = Mathf.Max(profile.profileSchemaVersion, ProfileSchemaVersion.CurrentProfileSchemaVersion);
        profile.legacyVersion = profile.legacyVersion > 0 ? profile.legacyVersion : ProfileSchemaVersion.CurrentLegacyVersion;
        profile.version = profile.legacyVersion;
        profile.worldState ??= new WorldStateData();
        profile.progression ??= new PlayerProgressionData();
        profile.stashItemInstances ??= new List<SavedItemInstanceDto>();
        profile.raidBackpackItemInstances ??= new List<SavedItemInstanceDto>();
        profile.secureContainerItemInstances ??= new List<SavedItemInstanceDto>();
        profile.specialEquipmentItemInstances ??= new List<SavedItemInstanceDto>();
        profile.equippedArmorInstances ??= new List<SavedArmorInstanceDto>();
        profile.stashWeaponInstances ??= new List<SavedItemInstanceDto>();
        profile.raidBackpackWeaponInstances ??= new List<SavedItemInstanceDto>();
        profile.stashItems ??= new List<ItemStackRecord>();
        profile.loadoutItems ??= new List<ItemStackRecord>();
        profile.extractedItems ??= new List<ItemStackRecord>();
        profile.raidBackpackItems ??= new List<ItemStackRecord>();
        profile.secureContainerItems ??= new List<ItemStackRecord>();
        profile.specialEquipmentItems ??= new List<ItemStackRecord>();
        profile.equippedArmorItems ??= new List<ItemStackRecord>();
        profile.stashWeaponIds ??= new List<string>();
        profile.funds = Mathf.Max(0, profile.funds);

        profile.worldState.worldStateVersion = Mathf.Max(
            profile.worldState.worldStateVersion,
            ProfileSchemaVersion.CurrentWorldStateVersion);
        PrototypePlayerProgressionUtility.Sanitize(profile.progression);
        profile.worldState.unlockedRaidMerchantIds ??= new List<string>();
        profile.worldState.unlockedRaidNpcIds ??= new List<string>();
        profile.worldState.questChainStages ??= new List<WorldStateData.QuestChainStageRecord>();
        profile.worldState.storyFlags ??= new List<string>();

        if (profile.raidBackpackWeaponInstances != null && profile.raidBackpackWeaponInstances.Count > 0)
        {
            MergeLegacyBackpackWeaponInstances(profile);
        }

        MigrateStashCurrencyToFunds(profile);
        profile.stashItemInstances = SanitizeItemInstanceRecords(profile.stashItemInstances, catalog);
        profile.raidBackpackItemInstances = SanitizeItemInstanceRecords(profile.raidBackpackItemInstances, catalog);
        profile.secureContainerItemInstances = SanitizeItemInstanceRecords(profile.secureContainerItemInstances, catalog);
        profile.specialEquipmentItemInstances = SanitizeItemInstanceRecords(profile.specialEquipmentItemInstances, catalog);
        profile.equippedArmorInstances = SanitizeArmorInstanceRecords(profile.equippedArmorInstances, catalog);
        profile.stashWeaponInstances = SanitizeWeaponInstanceRecords(profile.stashWeaponInstances, catalog);
        profile.raidBackpackWeaponInstances = SanitizeWeaponInstanceRecords(profile.raidBackpackWeaponInstances, catalog);
        profile.equippedSecureContainerInstance = SanitizeGenericItemInstance(
            profile.equippedSecureContainerInstance,
            catalog,
            PrototypeRaidInventoryRules.IsSecureContainerItem);
        profile.equippedPrimaryWeaponInstance = SanitizeWeaponInstance(profile.equippedPrimaryWeaponInstance, catalog);
        profile.equippedSecondaryWeaponInstance = SanitizeWeaponInstance(profile.equippedSecondaryWeaponInstance, catalog);
        profile.equippedMeleeWeaponInstance = SanitizeWeaponInstance(profile.equippedMeleeWeaponInstance, catalog);

        if (profile.equippedSecureContainerInstance == null)
        {
            profile.equippedSecureContainerInstance = CaptureItemInstance(PrototypeRaidInventoryRules.CreateDefaultSecureContainerInstance(catalog));
        }

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

    private static void MigrateStashCurrencyToFunds(ProfileData profile)
    {
        if (profile == null)
        {
            return;
        }

        profile.funds = Mathf.Max(0, profile.funds);

        int cashFromInstances = CountCashItemInstances(profile.stashItemInstances);
        int cashFromRecords = CountCashRecords(profile.stashItems);
        AddFunds(profile, Mathf.Max(cashFromInstances, cashFromRecords));

        RemoveCashItemInstances(profile.stashItemInstances);
        RemoveCashRecords(profile.stashItems);
    }

    private static int CountCashRecords(List<ItemStackRecord> records)
    {
        if (records == null)
        {
            return 0;
        }

        int total = 0;
        for (int index = 0; index < records.Count; index++)
        {
            ItemStackRecord record = records[index];
            if (record == null || record.quantity <= 0 || !IsCashBundleItemId(record.itemId))
            {
                continue;
            }

            total += record.quantity;
        }

        return total;
    }

    private static int CountCashItemInstances(List<SavedItemInstanceDto> records)
    {
        if (records == null)
        {
            return 0;
        }

        int total = 0;
        for (int index = 0; index < records.Count; index++)
        {
            SavedItemInstanceDto record = records[index];
            if (record == null || record.quantity <= 0 || !IsCashBundleItemId(record.itemId))
            {
                continue;
            }

            total += record.quantity;
        }

        return total;
    }

    private static void RemoveCashRecords(List<ItemStackRecord> records)
    {
        if (records == null)
        {
            return;
        }

        for (int index = records.Count - 1; index >= 0; index--)
        {
            ItemStackRecord record = records[index];
            if (record == null || !IsCashBundleItemId(record.itemId))
            {
                continue;
            }

            records.RemoveAt(index);
        }
    }

    private static void RemoveCashItemInstances(List<SavedItemInstanceDto> records)
    {
        if (records == null)
        {
            return;
        }

        for (int index = records.Count - 1; index >= 0; index--)
        {
            SavedItemInstanceDto record = records[index];
            if (record == null || !IsCashBundleItemId(record.itemId))
            {
                continue;
            }

            records.RemoveAt(index);
        }
    }

    private static bool IsCashBundleItemId(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId)
            && string.Equals(itemId.Trim(), CashBundleItemId, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddFunds(ProfileData profile, int amount)
    {
        if (profile == null || amount <= 0)
        {
            return;
        }

        long updatedFunds = (long)Mathf.Max(0, profile.funds) + amount;
        profile.funds = updatedFunds > int.MaxValue ? int.MaxValue : (int)updatedFunds;
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

    private static string EnsureInstanceId(string instanceId)
    {
        if (!string.IsNullOrWhiteSpace(instanceId))
        {
            return instanceId.Trim();
        }

        return Guid.NewGuid().ToString("N");
    }

    private static void MergeLegacyBackpackWeaponInstances(ProfileData profile)
    {
        if (profile == null || profile.raidBackpackWeaponInstances == null || profile.raidBackpackWeaponInstances.Count == 0)
        {
            return;
        }

        profile.raidBackpackItemInstances ??= new List<SavedItemInstanceDto>();
        for (int index = 0; index < profile.raidBackpackWeaponInstances.Count; index++)
        {
            SavedItemInstanceDto weaponRecord = profile.raidBackpackWeaponInstances[index];
            if (weaponRecord == null || string.IsNullOrWhiteSpace(weaponRecord.weaponId))
            {
                continue;
            }

            profile.raidBackpackItemInstances.Add(new SavedItemInstanceDto
            {
                instanceId = weaponRecord.instanceId,
                weaponId = weaponRecord.weaponId,
                rarity = weaponRecord.rarity,
                quantity = 1,
                magazineAmmo = weaponRecord.magazineAmmo,
                durability = weaponRecord.durability,
                affixes = ItemAffixUtility.CloneList(weaponRecord.affixes),
                skills = ItemSkillUtility.CloneList(weaponRecord.skills)
            });
        }

        profile.raidBackpackWeaponInstances.Clear();
    }

    private static List<SavedItemInstanceDto> SanitizeItemInstanceRecords(
        List<SavedItemInstanceDto> records,
        PrototypeItemCatalog catalog)
    {
        var sanitized = new List<SavedItemInstanceDto>();
        if (records == null)
        {
            return sanitized;
        }

        for (int index = 0; index < records.Count; index++)
        {
            SavedItemInstanceDto record = records[index];
            if (record == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(record.weaponId))
            {
                SavedItemInstanceDto sanitizedWeaponRecord = SanitizeWeaponInstance(new SavedItemInstanceDto
                {
                    instanceId = record.instanceId,
                    weaponId = record.weaponId,
                    rarity = record.rarity,
                    magazineAmmo = record.magazineAmmo,
                    durability = record.durability,
                    affixes = ItemAffixUtility.CloneList(record.affixes),
                    skills = ItemSkillUtility.CloneList(record.skills)
                }, catalog);

                if (sanitizedWeaponRecord != null)
                {
                    sanitized.Add(new SavedItemInstanceDto
                    {
                        instanceId = sanitizedWeaponRecord.instanceId,
                        weaponId = sanitizedWeaponRecord.weaponId,
                        rarity = sanitizedWeaponRecord.rarity,
                        quantity = 1,
                        magazineAmmo = sanitizedWeaponRecord.magazineAmmo,
                        durability = sanitizedWeaponRecord.durability,
                        affixes = ItemAffixUtility.CloneList(sanitizedWeaponRecord.affixes),
                        skills = ItemSkillUtility.CloneList(sanitizedWeaponRecord.skills)
                    });
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(record.itemId) || record.quantity <= 0)
            {
                continue;
            }

            ItemDefinition definition = catalog != null ? catalog.FindByItemId(record.itemId) : null;
            if (catalog != null && definition == null)
            {
                continue;
            }

            string itemId = definition != null ? definition.ItemId : record.itemId.Trim();
            if (definition is ArmorDefinition armorDefinition)
            {
                sanitized.Add(new SavedItemInstanceDto
                {
                    instanceId = EnsureInstanceId(record.instanceId),
                    itemId = itemId,
                    rarity = ItemRarityUtility.Sanitize(record.rarity),
                    quantity = 1,
                    durability = GetStoredArmorDurability(armorDefinition, record.rarity, record.durability),
                    affixes = ItemAffixUtility.CloneList(record.affixes),
                    skills = ItemSkillUtility.CloneList(record.skills)
                });
                continue;
            }

            int maxStackSize = definition != null ? definition.MaxStackSize : Mathf.Max(1, record.quantity);
            int remainingQuantity = record.quantity;
            bool usedRecordId = false;
            while (remainingQuantity > 0)
            {
                int stackSize = Mathf.Clamp(remainingQuantity, 1, maxStackSize);
                string instanceId = usedRecordId ? Guid.NewGuid().ToString("N") : EnsureInstanceId(record.instanceId);
                sanitized.Add(new SavedItemInstanceDto
                {
                    instanceId = instanceId,
                    itemId = itemId,
                    rarity = ItemRarityUtility.Sanitize(record.rarity),
                    quantity = stackSize,
                    durability = -1f
                });

                usedRecordId = true;
                remainingQuantity -= stackSize;
            }
        }

        return sanitized;
    }

    private static List<SavedArmorInstanceDto> SanitizeArmorInstanceRecords(
        List<SavedArmorInstanceDto> records,
        PrototypeItemCatalog catalog)
    {
        var sanitized = new List<SavedArmorInstanceDto>();
        if (records == null)
        {
            return sanitized;
        }

        for (int index = 0; index < records.Count; index++)
        {
            SavedArmorInstanceDto record = records[index];
            SavedArmorInstanceDto sanitizedRecord = SanitizeArmorInstance(record, catalog);
            if (sanitizedRecord != null)
            {
                sanitized.Add(sanitizedRecord);
            }
        }

        return sanitized;
    }

    private static SavedArmorInstanceDto SanitizeArmorInstance(SavedArmorInstanceDto record, PrototypeItemCatalog catalog)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.itemId))
        {
            return null;
        }

        ItemDefinition definition = catalog != null ? catalog.FindByItemId(record.itemId) : null;
        if (catalog != null && !(definition is ArmorDefinition))
        {
            return null;
        }

        ArmorDefinition armorDefinition = definition as ArmorDefinition;
        float maxDurability = armorDefinition != null ? armorDefinition.MaxDurability : Mathf.Max(1f, record.currentDurability);
        float clampedDurability = Mathf.Clamp(record.currentDurability, 0f, maxDurability);
        string itemId = armorDefinition != null ? armorDefinition.ItemId : record.itemId.Trim();
        List<ItemAffix> affixes = ItemAffixUtility.CloneList(record.affixes);
        ItemAffixUtility.SanitizeAffixes(affixes);
        List<ItemSkill> skills = ItemSkillUtility.CloneList(record.skills);
        ItemSkillUtility.SanitizeSkills(skills);

        return new SavedArmorInstanceDto
        {
            instanceId = EnsureInstanceId(record.instanceId),
            itemId = itemId,
            rarity = ItemRarityUtility.Sanitize(record.rarity),
            currentDurability = clampedDurability,
            affixes = affixes,
            skills = skills
        };
    }

    private static float GetStoredArmorDurability(ArmorDefinition armorDefinition, ItemRarity rarity, float storedDurability)
    {
        float maxDurability = armorDefinition != null
            ? Mathf.Max(1f, ItemRarityUtility.ScaleValue(armorDefinition.MaxDurability, rarity))
            : Mathf.Max(1f, storedDurability);

        if (storedDurability < 0f)
        {
            return maxDurability;
        }

        return Mathf.Clamp(storedDurability, 0f, maxDurability);
    }

    private static List<SavedItemInstanceDto> SanitizeWeaponInstanceRecords(
        List<SavedItemInstanceDto> records,
        PrototypeItemCatalog catalog)
    {
        var sanitized = new List<SavedItemInstanceDto>();
        if (records == null)
        {
            return sanitized;
        }

        for (int index = 0; index < records.Count; index++)
        {
            SavedItemInstanceDto record = records[index];
            SavedItemInstanceDto sanitizedRecord = SanitizeWeaponInstance(record, catalog);
            if (sanitizedRecord != null)
            {
                sanitized.Add(sanitizedRecord);
            }
        }

        return sanitized;
    }

    private static SavedItemInstanceDto SanitizeWeaponInstance(SavedItemInstanceDto record, PrototypeItemCatalog catalog)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.weaponId))
        {
            return null;
        }

        PrototypeWeaponDefinition definition = catalog != null ? catalog.FindWeaponById(record.weaponId) : null;
        if (catalog != null && definition == null)
        {
            return null;
        }

        string weaponId = definition != null ? definition.WeaponId : record.weaponId.Trim();
        int ammo = Mathf.Max(0, record.magazineAmmo);
        if (definition != null)
        {
            ammo = definition.IsMeleeWeapon ? 0 : Mathf.Clamp(record.magazineAmmo, 0, definition.MagazineSize);
        }

        List<ItemAffix> affixes = ItemAffixUtility.CloneList(record.affixes);
        ItemAffixUtility.SanitizeAffixes(affixes);
        List<ItemSkill> skills = ItemSkillUtility.CloneList(record.skills);
        ItemSkillUtility.SanitizeSkills(skills);

        return new SavedItemInstanceDto
        {
            instanceId = EnsureInstanceId(record.instanceId),
            weaponId = weaponId,
            rarity = ItemRarityUtility.Sanitize(record.rarity),
            quantity = 1,
            magazineAmmo = ammo,
            durability = Mathf.Max(0f, record.durability),
            affixes = affixes,
            skills = skills
        };
    }

    private static SavedItemInstanceDto SanitizeGenericItemInstance(
        SavedItemInstanceDto record,
        PrototypeItemCatalog catalog,
        Predicate<ItemInstance> predicate = null)
    {
        ItemInstance instance = ResolveItemInstance(record, catalog);
        if (instance == null)
        {
            return null;
        }

        if (predicate != null && !predicate(instance))
        {
            return null;
        }

        return CaptureItemInstance(instance);
    }

    private static List<SavedItemInstanceDto> CreateItemInstanceDtosFromRecords(
        List<ItemStackRecord> records,
        PrototypeItemCatalog catalog)
    {
        var instances = new List<SavedItemInstanceDto>();
        if (records == null)
        {
            return instances;
        }

        for (int index = 0; index < records.Count; index++)
        {
            ItemStackRecord record = records[index];
            if (record == null || record.quantity <= 0 || string.IsNullOrWhiteSpace(record.itemId))
            {
                continue;
            }

            ItemDefinition definition = catalog != null ? catalog.FindByItemId(record.itemId) : null;
            if (catalog != null && definition == null)
            {
                continue;
            }

            string itemId = definition != null ? definition.ItemId : record.itemId.Trim();
            if (definition is ArmorDefinition armorDefinition)
            {
                int count = Mathf.Max(1, record.quantity);
                for (int instanceIndex = 0; instanceIndex < count; instanceIndex++)
                {
                    instances.Add(new SavedItemInstanceDto
                    {
                        instanceId = Guid.NewGuid().ToString("N"),
                        itemId = itemId,
                        rarity = ItemRarity.Common,
                        quantity = 1,
                        durability = GetStoredArmorDurability(armorDefinition, ItemRarity.Common, -1f)
                    });
                }

                continue;
            }

            int maxStackSize = definition != null ? definition.MaxStackSize : Mathf.Max(1, record.quantity);
            int remainingQuantity = record.quantity;
            while (remainingQuantity > 0)
            {
                int stackSize = Mathf.Clamp(remainingQuantity, 1, maxStackSize);
                instances.Add(new SavedItemInstanceDto
                {
                    instanceId = Guid.NewGuid().ToString("N"),
                    itemId = itemId,
                    rarity = ItemRarity.Common,
                    quantity = stackSize,
                    durability = -1f
                });

                remainingQuantity -= stackSize;
            }
        }

        return instances;
    }

    private static List<SavedArmorInstanceDto> CreateArmorInstanceDtosFromRecords(
        List<ItemStackRecord> records,
        PrototypeItemCatalog catalog)
    {
        var instances = new List<SavedArmorInstanceDto>();
        if (records == null)
        {
            return instances;
        }

        for (int index = 0; index < records.Count; index++)
        {
            ItemStackRecord record = records[index];
            if (record == null || record.quantity <= 0 || string.IsNullOrWhiteSpace(record.itemId))
            {
                continue;
            }

            ArmorDefinition armorDefinition = catalog != null ? catalog.FindByItemId(record.itemId) as ArmorDefinition : null;
            if (catalog != null && armorDefinition == null)
            {
                continue;
            }

            string itemId = armorDefinition != null ? armorDefinition.ItemId : record.itemId.Trim();
            float durability = armorDefinition != null ? armorDefinition.MaxDurability : 1f;
            int count = Mathf.Max(1, record.quantity);
            for (int instanceIndex = 0; instanceIndex < count; instanceIndex++)
            {
                instances.Add(new SavedArmorInstanceDto
                {
                    instanceId = Guid.NewGuid().ToString("N"),
                    itemId = itemId,
                    rarity = ItemRarity.Common,
                    currentDurability = durability
                });
            }
        }

        return instances;
    }

    private static List<SavedItemInstanceDto> CreateWeaponInstanceDtosFromIds(
        List<string> weaponIds,
        PrototypeItemCatalog catalog)
    {
        var instances = new List<SavedItemInstanceDto>();
        if (weaponIds == null)
        {
            return instances;
        }

        for (int index = 0; index < weaponIds.Count; index++)
        {
            string weaponId = SanitizeWeaponId(weaponIds[index], catalog);
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                continue;
            }

            PrototypeWeaponDefinition definition = catalog != null ? catalog.FindWeaponById(weaponId) : null;
            SavedItemInstanceDto instance = CreateWeaponInstanceDto(definition, weaponId);
            if (instance != null)
            {
                instances.Add(instance);
            }
        }

        return instances;
    }

    private static SavedItemInstanceDto CreateWeaponInstanceDtoFromId(string weaponId, PrototypeItemCatalog catalog)
    {
        string sanitizedId = SanitizeWeaponId(weaponId, catalog);
        if (string.IsNullOrWhiteSpace(sanitizedId))
        {
            return null;
        }

        PrototypeWeaponDefinition definition = catalog != null ? catalog.FindWeaponById(sanitizedId) : null;
        return CreateWeaponInstanceDto(definition, sanitizedId);
    }

    private static SavedItemInstanceDto CreateWeaponInstanceDto(PrototypeWeaponDefinition definition, string fallbackWeaponId)
    {
        string weaponId = definition != null ? definition.WeaponId : fallbackWeaponId;
        if (string.IsNullOrWhiteSpace(weaponId))
        {
            return null;
        }

        int magazineAmmo = 0;
        if (definition != null && !definition.IsMeleeWeapon)
        {
            magazineAmmo = definition.MagazineSize;
        }

        return new SavedItemInstanceDto
        {
            instanceId = Guid.NewGuid().ToString("N"),
            weaponId = weaponId.Trim(),
            rarity = ItemRarity.Common,
            quantity = 1,
            magazineAmmo = magazineAmmo,
            durability = 1f
        };
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
