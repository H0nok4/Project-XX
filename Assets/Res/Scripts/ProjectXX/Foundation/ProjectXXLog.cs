using UnityEngine;

namespace ProjectXX.Foundation
{
    public static class ProjectXXLog
    {
        private const string Prefix = "[ProjectXX]";

        public static void Info(string message, Object context = null)
        {
            Debug.Log($"{Prefix} {message}", context);
        }

        public static void Warning(string message, Object context = null)
        {
            Debug.LogWarning($"{Prefix} {message}", context);
        }

        public static void Error(string message, Object context = null)
        {
            Debug.LogError($"{Prefix} {message}", context);
        }
    }
}
