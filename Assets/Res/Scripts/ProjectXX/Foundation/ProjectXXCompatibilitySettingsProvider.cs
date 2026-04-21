using ProjectXX.Infrastructure.Definitions;
using UnityEngine;

namespace ProjectXX.Foundation
{
    public static class ProjectXXCompatibilitySettingsProvider
    {
        private const string ResourcePath = "ProjectXXCompatibilitySettings";

        private static ProjectXXCompatibilitySettings cachedSettings;
        private static bool missingAssetWarningLogged;

        public static ProjectXXCompatibilitySettings GetOrDefault()
        {
            if (cachedSettings != null)
            {
                return cachedSettings;
            }

            cachedSettings = Resources.Load<ProjectXXCompatibilitySettings>(ResourcePath);
            if (cachedSettings != null)
            {
                return cachedSettings;
            }

            cachedSettings = ScriptableObject.CreateInstance<ProjectXXCompatibilitySettings>();
            cachedSettings.hideFlags = HideFlags.DontSave;

            if (!missingAssetWarningLogged)
            {
                missingAssetWarningLogged = true;
                ProjectXXLog.Warning(
                    "Missing Resources asset 'ProjectXXCompatibilitySettings'. Falling back to in-memory defaults until the asset is created.");
            }

            return cachedSettings;
        }

        public static void ResetCache()
        {
            cachedSettings = null;
            missingAssetWarningLogged = false;
        }
    }
}
