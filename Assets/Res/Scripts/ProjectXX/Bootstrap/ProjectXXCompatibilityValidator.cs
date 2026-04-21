using System;
using ProjectXX.Foundation;
using ProjectXX.Infrastructure.Definitions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace ProjectXX.Bootstrap
{
    public static class ProjectXXCompatibilityValidator
    {
        public static bool Validate(ProjectXXCompatibilitySettings settings, UnityEngine.Object context = null)
        {
            if (settings == null)
            {
                ProjectXXLog.Error("ProjectXX compatibility settings are missing.", context);
                return false;
            }

            bool isValid = true;

            isValid &= ValidateTag(settings.PlayerTag, "player tag", context);
            isValid &= ValidateTag(settings.EnemyTag, "enemy tag", context);
            isValid &= ValidateTag(settings.BulletTag, "bullet tag", context);

            isValid &= ValidateLayer(settings.PlayerLayerName, "player layer", context);
            isValid &= ValidateLayer(settings.FpsObjectLayerName, "first-person object layer", context);
            isValid &= ValidateLayer(settings.EnvironmentLayerName, "environment layer", context);

            return isValid;
        }

        private static bool ValidateTag(string tagName, string label, UnityEngine.Object context)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                ProjectXXLog.Error($"ProjectXX compatibility settings are missing {label}.", context);
                return false;
            }

            if (TagExists(tagName))
            {
                return true;
            }

            ProjectXXLog.Error($"Required ProjectXX compatibility {label} '{tagName}' does not exist.", context);
            return false;
        }

        private static bool ValidateLayer(string layerName, string label, UnityEngine.Object context)
        {
            if (string.IsNullOrWhiteSpace(layerName))
            {
                ProjectXXLog.Error($"ProjectXX compatibility settings are missing {label}.", context);
                return false;
            }

            if (LayerMask.NameToLayer(layerName) >= 0)
            {
                return true;
            }

            ProjectXXLog.Error($"Required ProjectXX compatibility {label} '{layerName}' does not exist.", context);
            return false;
        }

        private static bool TagExists(string tagName)
        {
#if UNITY_EDITOR
            return Array.Exists(InternalEditorUtility.tags, candidate => candidate == tagName);
#else
            try
            {
                GameObject.FindGameObjectWithTag(tagName);
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
#endif
        }
    }
}
