using System;
using System.IO;
using UnityEngine;

public static class ProfileFileGateway
{
    private const string SaveFileName = "prototype_profile.json";
    private const string BackupDirectoryName = "ProfileBackups";

    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
    public static string BackupDirectoryPath => Path.Combine(Application.persistentDataPath, BackupDirectoryName);

    public static bool TryReadRawJson(out string rawJson, out string errorMessage)
    {
        rawJson = string.Empty;
        errorMessage = string.Empty;

        if (!File.Exists(SavePath))
        {
            return false;
        }

        try
        {
            rawJson = File.ReadAllText(SavePath);
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    public static bool TryWriteJson(string rawJson, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            EnsureDirectory(Path.GetDirectoryName(SavePath));

            string tempPath = SavePath + ".tmp";
            File.WriteAllText(tempPath, rawJson ?? string.Empty);
            File.Copy(tempPath, SavePath, true);
            File.Delete(tempPath);
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    public static bool TryWriteBackup(string rawJson, out string backupPath, out string errorMessage)
    {
        backupPath = string.Empty;
        errorMessage = string.Empty;

        try
        {
            EnsureDirectory(BackupDirectoryPath);
            backupPath = Path.Combine(
                BackupDirectoryPath,
                $"prototype_profile.backup.{CreateTimestamp()}.json");
            File.WriteAllText(backupPath, rawJson ?? string.Empty);
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    public static bool TryWriteMigrationLog(string logText, out string logPath, out string errorMessage)
    {
        logPath = string.Empty;
        errorMessage = string.Empty;

        try
        {
            EnsureDirectory(BackupDirectoryPath);
            logPath = Path.Combine(
                BackupDirectoryPath,
                $"prototype_profile.migration.{CreateTimestamp()}.log");
            File.WriteAllText(logPath, logText ?? string.Empty);
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private static void EnsureDirectory(string directoryPath)
    {
        if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    private static string CreateTimestamp()
    {
        return DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
    }
}
