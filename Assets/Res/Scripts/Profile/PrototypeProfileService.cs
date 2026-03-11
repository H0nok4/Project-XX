using System;
using System.Collections.Generic;
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
        public int profileSchemaVersion = ProfileSchemaVersion.CurrentProfileSchemaVersion;
        public int legacyVersion = ProfileSchemaVersion.CurrentLegacyVersion;
        public int version = ProfileSchemaVersion.CurrentLegacyVersion;
        public WorldStateData worldState = new WorldStateData();
        public PlayerProgressionData progression = new PlayerProgressionData();
        public List<SavedItemInstanceDto> stashItemInstances = new List<SavedItemInstanceDto>();
        public List<SavedItemInstanceDto> raidBackpackItemInstances = new List<SavedItemInstanceDto>();
        public List<SavedItemInstanceDto> secureContainerItemInstances = new List<SavedItemInstanceDto>();
        public List<SavedItemInstanceDto> specialEquipmentItemInstances = new List<SavedItemInstanceDto>();
        public List<SavedArmorInstanceDto> equippedArmorInstances = new List<SavedArmorInstanceDto>();
        public List<SavedWeaponInstanceDto> stashWeaponInstances = new List<SavedWeaponInstanceDto>();
        public SavedWeaponInstanceDto equippedPrimaryWeaponInstance;
        public SavedWeaponInstanceDto equippedSecondaryWeaponInstance;
        public SavedWeaponInstanceDto equippedMeleeWeaponInstance;
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
            if (record == null || record.quantity <= 0)
            {
                continue;
            }

            ItemDefinition definition = catalog.FindByItemId(record.itemId);
            if (definition == null)
            {
                continue;
            }

            int remainingQuantity = record.quantity;
            bool usedRecordId = false;
            while (remainingQuantity > 0)
            {
                int stackSize = Mathf.Min(remainingQuantity, definition.MaxStackSize);
                string instanceId = !usedRecordId ? record.instanceId : null;
                ItemInstance instance = ItemInstance.Create(definition, stackSize, instanceId);
                if (!inventory.TryAddItemInstance(instance))
                {
                    break;
                }

                usedRecordId = true;
                remainingQuantity -= stackSize;
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
            if (item == null || !item.IsDefined() || item.Quantity <= 0)
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
            records.Add(new SavedItemInstanceDto
            {
                instanceId = item.InstanceId,
                itemId = item.Definition.ItemId,
                quantity = item.Quantity
            });
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
                currentDurability = armorInstance.CurrentDurability
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
                currentDurability = armorState.currentDurability
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

            ArmorInstance instance = ArmorInstance.Create(armorDefinition, record.currentDurability, record.instanceId);
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

    public static List<string> CaptureWeaponIds(IEnumerable<WeaponInstance> weaponInstances)
    {
        var weaponIds = new List<string>();
        if (weaponInstances == null)
        {
            return weaponIds;
        }

        foreach (WeaponInstance weaponInstance in weaponInstances)
        {
            if (weaponInstance == null || weaponInstance.Definition == null)
            {
                continue;
            }

            AddWeaponId(weaponIds, weaponInstance.Definition.WeaponId);
        }

        return weaponIds;
    }

    public static List<SavedWeaponInstanceDto> CaptureWeaponInstances(IEnumerable<WeaponInstance> weaponInstances)
    {
        var records = new List<SavedWeaponInstanceDto>();
        if (weaponInstances == null)
        {
            return records;
        }

        foreach (WeaponInstance weaponInstance in weaponInstances)
        {
            SavedWeaponInstanceDto record = CaptureWeaponInstance(weaponInstance);
            if (record != null)
            {
                records.Add(record);
            }
        }

        return records;
    }

    public static SavedWeaponInstanceDto CaptureWeaponInstance(WeaponInstance weaponInstance)
    {
        if (weaponInstance == null || weaponInstance.Definition == null)
        {
            return null;
        }

        return new SavedWeaponInstanceDto
        {
            instanceId = EnsureInstanceId(weaponInstance.InstanceId),
            weaponId = weaponInstance.Definition.WeaponId,
            magazineAmmo = weaponInstance.MagazineAmmo,
            durability = weaponInstance.Durability
        };
    }

    public static WeaponInstance ResolveWeaponInstance(SavedWeaponInstanceDto record, PrototypeItemCatalog catalog)
    {
        if (record == null)
        {
            return null;
        }

        PrototypeWeaponDefinition definition = catalog != null ? catalog.FindWeaponById(record.weaponId) : null;
        if (catalog != null && definition == null)
        {
            return null;
        }

        return WeaponInstance.Create(definition, record.magazineAmmo, record.durability, record.instanceId);
    }

    public static List<WeaponInstance> ResolveWeaponInstances(List<SavedWeaponInstanceDto> records, PrototypeItemCatalog catalog)
    {
        var weaponInstances = new List<WeaponInstance>();
        if (records == null || catalog == null)
        {
            return weaponInstances;
        }

        for (int index = 0; index < records.Count; index++)
        {
            WeaponInstance instance = ResolveWeaponInstance(records[index], catalog);
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
            profileSchemaVersion = ProfileSchemaVersion.CurrentProfileSchemaVersion,
            legacyVersion = ProfileSchemaVersion.CurrentLegacyVersion,
            version = ProfileSchemaVersion.CurrentLegacyVersion,
            worldState = new WorldStateData(),
            progression = new PlayerProgressionData(),
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
        ApplyInstanceMigration(profile, catalog);
        return profile;
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

        profile.stashWeaponInstances ??= new List<SavedWeaponInstanceDto>();
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
        profile.stashWeaponInstances ??= new List<SavedWeaponInstanceDto>();
        profile.stashItems ??= new List<ItemStackRecord>();
        profile.loadoutItems ??= new List<ItemStackRecord>();
        profile.extractedItems ??= new List<ItemStackRecord>();
        profile.raidBackpackItems ??= new List<ItemStackRecord>();
        profile.secureContainerItems ??= new List<ItemStackRecord>();
        profile.specialEquipmentItems ??= new List<ItemStackRecord>();
        profile.equippedArmorItems ??= new List<ItemStackRecord>();
        profile.stashWeaponIds ??= new List<string>();

        profile.worldState.worldStateVersion = Mathf.Max(
            profile.worldState.worldStateVersion,
            ProfileSchemaVersion.CurrentWorldStateVersion);
        profile.progression.progressionDataVersion = Mathf.Max(
            profile.progression.progressionDataVersion,
            ProfileSchemaVersion.CurrentProgressionDataVersion);
        profile.progression.playerLevel = Mathf.Max(1, profile.progression.playerLevel);
        profile.worldState.unlockedRaidMerchantIds ??= new List<string>();
        profile.worldState.unlockedRaidNpcIds ??= new List<string>();
        profile.worldState.questChainStages ??= new List<WorldStateData.QuestChainStageRecord>();
        profile.worldState.storyFlags ??= new List<string>();

        profile.stashItemInstances = SanitizeItemInstanceRecords(profile.stashItemInstances, catalog);
        profile.raidBackpackItemInstances = SanitizeItemInstanceRecords(profile.raidBackpackItemInstances, catalog);
        profile.secureContainerItemInstances = SanitizeItemInstanceRecords(profile.secureContainerItemInstances, catalog);
        profile.specialEquipmentItemInstances = SanitizeItemInstanceRecords(profile.specialEquipmentItemInstances, catalog);
        profile.equippedArmorInstances = SanitizeArmorInstanceRecords(profile.equippedArmorInstances, catalog);
        profile.stashWeaponInstances = SanitizeWeaponInstanceRecords(profile.stashWeaponInstances, catalog);
        profile.equippedPrimaryWeaponInstance = SanitizeWeaponInstance(profile.equippedPrimaryWeaponInstance, catalog);
        profile.equippedSecondaryWeaponInstance = SanitizeWeaponInstance(profile.equippedSecondaryWeaponInstance, catalog);
        profile.equippedMeleeWeaponInstance = SanitizeWeaponInstance(profile.equippedMeleeWeaponInstance, catalog);

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
                    quantity = stackSize
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

        return new SavedArmorInstanceDto
        {
            instanceId = EnsureInstanceId(record.instanceId),
            itemId = itemId,
            currentDurability = clampedDurability
        };
    }

    private static List<SavedWeaponInstanceDto> SanitizeWeaponInstanceRecords(
        List<SavedWeaponInstanceDto> records,
        PrototypeItemCatalog catalog)
    {
        var sanitized = new List<SavedWeaponInstanceDto>();
        if (records == null)
        {
            return sanitized;
        }

        for (int index = 0; index < records.Count; index++)
        {
            SavedWeaponInstanceDto record = records[index];
            SavedWeaponInstanceDto sanitizedRecord = SanitizeWeaponInstance(record, catalog);
            if (sanitizedRecord != null)
            {
                sanitized.Add(sanitizedRecord);
            }
        }

        return sanitized;
    }

    private static SavedWeaponInstanceDto SanitizeWeaponInstance(SavedWeaponInstanceDto record, PrototypeItemCatalog catalog)
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

        return new SavedWeaponInstanceDto
        {
            instanceId = EnsureInstanceId(record.instanceId),
            weaponId = weaponId,
            magazineAmmo = ammo,
            durability = Mathf.Max(0f, record.durability)
        };
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
            int maxStackSize = definition != null ? definition.MaxStackSize : Mathf.Max(1, record.quantity);
            int remainingQuantity = record.quantity;
            while (remainingQuantity > 0)
            {
                int stackSize = Mathf.Clamp(remainingQuantity, 1, maxStackSize);
                instances.Add(new SavedItemInstanceDto
                {
                    instanceId = Guid.NewGuid().ToString("N"),
                    itemId = itemId,
                    quantity = stackSize
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
                    currentDurability = durability
                });
            }
        }

        return instances;
    }

    private static List<SavedWeaponInstanceDto> CreateWeaponInstanceDtosFromIds(
        List<string> weaponIds,
        PrototypeItemCatalog catalog)
    {
        var instances = new List<SavedWeaponInstanceDto>();
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
            SavedWeaponInstanceDto instance = CreateWeaponInstanceDto(definition, weaponId);
            if (instance != null)
            {
                instances.Add(instance);
            }
        }

        return instances;
    }

    private static SavedWeaponInstanceDto CreateWeaponInstanceDtoFromId(string weaponId, PrototypeItemCatalog catalog)
    {
        string sanitizedId = SanitizeWeaponId(weaponId, catalog);
        if (string.IsNullOrWhiteSpace(sanitizedId))
        {
            return null;
        }

        PrototypeWeaponDefinition definition = catalog != null ? catalog.FindWeaponById(sanitizedId) : null;
        return CreateWeaponInstanceDto(definition, sanitizedId);
    }

    private static SavedWeaponInstanceDto CreateWeaponInstanceDto(PrototypeWeaponDefinition definition, string fallbackWeaponId)
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

        return new SavedWeaponInstanceDto
        {
            instanceId = Guid.NewGuid().ToString("N"),
            weaponId = weaponId.Trim(),
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
