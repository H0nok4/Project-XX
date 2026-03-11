using System;
using UnityEngine;

public static class ProfileMigrationRunner
{
    [Serializable]
    private sealed class ProfileVersionProbe
    {
        public int profileSchemaVersion;
        public int legacyVersion;
        public int version;
    }

    public sealed class ProfileMigrationResult
    {
        public PrototypeProfileService.ProfileData Profile;
        public ProfileDiagnostics.Report Diagnostics;
        public bool Upgraded;

        public bool HasErrors => Diagnostics != null && Diagnostics.HasErrors;
        public bool HasWarnings => Diagnostics != null && Diagnostics.HasWarnings;
    }

    public static ProfileMigrationResult ParseAndUpgrade(string rawJson, PrototypeItemCatalog catalog)
    {
        var report = ProfileDiagnostics.CreateReport("ProfileMigration");
        var result = new ProfileMigrationResult
        {
            Diagnostics = report
        };

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            report.Info("No existing profile payload was found.");
            return result;
        }

        if (!LooksLikePrototypeProfileJson(rawJson))
        {
            report.Error("The profile payload does not contain recognizable profile fields.");
            return result;
        }

        ProfileVersionProbe probe = TryDeserialize<ProfileVersionProbe>(rawJson, report, "version probe");
        if (probe == null)
        {
            return result;
        }

        int detectedLegacyVersion = probe.legacyVersion > 0 ? probe.legacyVersion : probe.version;
        report.Info(
            $"Detected schema={probe.profileSchemaVersion}, legacy={detectedLegacyVersion}, target={ProfileSchemaVersion.CurrentProfileSchemaVersion}.");

        PrototypeProfileService.ProfileData profile = TryDeserialize<PrototypeProfileService.ProfileData>(rawJson, report, "profile payload");
        if (profile == null)
        {
            return result;
        }

        if (probe.profileSchemaVersion <= 0)
        {
            int legacyVersion = detectedLegacyVersion > 0 ? detectedLegacyVersion : ProfileSchemaVersion.CurrentLegacyVersion;
            report.Info($"Migrating legacy profile version {legacyVersion} to schema {ProfileSchemaVersion.CurrentProfileSchemaVersion}.");

            profile.profileSchemaVersion = ProfileSchemaVersion.CurrentProfileSchemaVersion;
            profile.legacyVersion = legacyVersion;
            profile.version = legacyVersion;
            profile.worldState ??= new WorldStateData();
            profile.progression ??= new PlayerProgressionData();

            PrototypeProfileService.ApplyLegacyCompatibilityMigrations(profile, catalog);
            bool instanceMigrated = PrototypeProfileService.ApplyInstanceMigration(profile, catalog);

            report.Info("Applied legacy loadout migration and default shell initialization.");
            if (instanceMigrated)
            {
                report.Info("Migrated definition-based equipment to instance records.");
            }
            result.Upgraded = true;
            result.Profile = profile;
            return result;
        }

        if (probe.profileSchemaVersion > ProfileSchemaVersion.CurrentProfileSchemaVersion)
        {
            report.Warning(
                $"Profile schema {probe.profileSchemaVersion} is newer than this client supports; loading in best-effort mode.");
        }
        else if (probe.profileSchemaVersion < ProfileSchemaVersion.CurrentProfileSchemaVersion)
        {
            report.Info($"Upgrading profile schema {probe.profileSchemaVersion} to {ProfileSchemaVersion.CurrentProfileSchemaVersion}.");
            bool instanceMigrated = PrototypeProfileService.ApplyInstanceMigration(profile, catalog);
            if (instanceMigrated)
            {
                report.Info("Migrated definition-based equipment to instance records.");
            }

            profile.profileSchemaVersion = ProfileSchemaVersion.CurrentProfileSchemaVersion;
            profile.worldState ??= new WorldStateData();
            profile.progression ??= new PlayerProgressionData();
            profile.legacyVersion = profile.legacyVersion > 0 ? profile.legacyVersion : detectedLegacyVersion;
            result.Upgraded = true;
            result.Profile = profile;
            return result;
        }

        profile.profileSchemaVersion = Mathf.Max(profile.profileSchemaVersion, ProfileSchemaVersion.CurrentProfileSchemaVersion);
        profile.worldState ??= new WorldStateData();
        profile.progression ??= new PlayerProgressionData();
        profile.legacyVersion = profile.legacyVersion > 0 ? profile.legacyVersion : detectedLegacyVersion;
        result.Profile = profile;
        return result;
    }

    private static T TryDeserialize<T>(string rawJson, ProfileDiagnostics.Report report, string label)
        where T : class
    {
        try
        {
            T value = JsonUtility.FromJson<T>(rawJson);
            if (value == null)
            {
                report.Error($"Failed to deserialize {label}.");
            }

            return value;
        }
        catch (Exception exception)
        {
            report.Error($"Failed to deserialize {label}: {exception.Message}");
            return null;
        }
    }

    private static bool LooksLikePrototypeProfileJson(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return false;
        }

        return rawJson.Contains("\"profileSchemaVersion\"")
            || rawJson.Contains("\"version\"")
            || rawJson.Contains("\"legacyVersion\"")
            || rawJson.Contains("\"stashItems\"")
            || rawJson.Contains("\"stashWeaponIds\"")
            || rawJson.Contains("\"raidBackpackItems\"")
            || rawJson.Contains("\"equippedPrimaryWeaponId\"")
            || rawJson.Contains("\"stashItemInstances\"")
            || rawJson.Contains("\"stashWeaponInstances\"")
            || rawJson.Contains("\"equippedPrimaryWeaponInstance\"")
            || rawJson.Contains("\"equippedArmorInstances\"");
    }
}
