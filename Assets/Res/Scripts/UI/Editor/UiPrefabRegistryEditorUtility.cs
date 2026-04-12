#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UiPrefabRegistryEditorUtility
{
    private const string RegistryAssetPath = "Assets/Resources/UI/UiPrefabRegistry.asset";
    private const string UiPrefabRoot = "Assets/Resources/UI";
    private const string ResourcesRoot = "Assets/Resources/";

    [MenuItem("Tools/ProjectXX/UI/Rebuild Prefab Registry")]
    public static void RebuildPrefabRegistryMenu()
    {
        UiPrefabRegistry registry = RebuildPrefabRegistry();
        if (registry != null)
        {
            Debug.Log($"[UiPrefabRegistry] Rebuilt registry with {registry.Entries.Count} entries at '{RegistryAssetPath}'.");
        }
    }

    [MenuItem("Tools/ProjectXX/UI/Validate Prefab Registry")]
    public static void ValidatePrefabRegistryMenu()
    {
        ValidatePrefabRegistry();
    }

    [MenuItem("Tools/ProjectXX/UI/Validate Runtime UI Prefabs")]
    public static void ValidateRuntimeUiPrefabsMenu()
    {
        ValidateRuntimeUiPrefabs();
    }

    public static UiPrefabRegistry RebuildPrefabRegistry()
    {
        EnsureParentFolderExists();

        UiPrefabRegistry registry = LoadOrCreateRegistry();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { UiPrefabRoot });
        Array.Sort(prefabGuids, StringComparer.Ordinal);

        List<UiPrefabRegistry.Entry> rebuiltEntries = new List<UiPrefabRegistry.Entry>();
        HashSet<string> usedIds = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < prefabGuids.Length; index++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(prefabGuids[index]);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                continue;
            }

            string resourcePath = ConvertAssetPathToResourcePath(assetPath);
            string prefabId = Path.GetFileNameWithoutExtension(assetPath);
            if (usedIds.Contains(prefabId))
            {
                prefabId = resourcePath;
            }

            usedIds.Add(prefabId);

            UiPrefabRegistry.Entry entry = new UiPrefabRegistry.Entry();
            entry.Configure(prefabId, resourcePath, prefab);
            rebuiltEntries.Add(entry);
        }

        registry.ReplaceEntries(rebuiltEntries);
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return registry;
    }

    public static void ValidatePrefabRegistry()
    {
        UiPrefabRegistry registry = AssetDatabase.LoadAssetAtPath<UiPrefabRegistry>(RegistryAssetPath);
        if (registry == null)
        {
            Debug.LogError($"[UiPrefabRegistry] Missing registry asset at '{RegistryAssetPath}'. Use Tools/ProjectXX/UI/Rebuild Prefab Registry first.");
            return;
        }

        int errorCount = 0;
        HashSet<string> usedIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> usedPaths = new HashSet<string>(StringComparer.Ordinal);

        IReadOnlyList<UiPrefabRegistry.Entry> entries = registry.Entries;
        for (int index = 0; index < entries.Count; index++)
        {
            UiPrefabRegistry.Entry entry = entries[index];
            if (entry == null)
            {
                Debug.LogError($"[UiPrefabRegistry] Entry {index} is null.", registry);
                errorCount++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.PrefabId))
            {
                Debug.LogError($"[UiPrefabRegistry] Entry {index} has empty prefab id.", registry);
                errorCount++;
            }
            else if (!usedIds.Add(entry.PrefabId))
            {
                Debug.LogError($"[UiPrefabRegistry] Duplicate prefab id '{entry.PrefabId}'.", registry);
                errorCount++;
            }

            if (string.IsNullOrWhiteSpace(entry.ResourcePath))
            {
                Debug.LogError($"[UiPrefabRegistry] Entry '{entry.PrefabId}' has empty resource path.", registry);
                errorCount++;
            }
            else if (!usedPaths.Add(entry.ResourcePath))
            {
                Debug.LogError($"[UiPrefabRegistry] Duplicate resource path '{entry.ResourcePath}'.", registry);
                errorCount++;
            }

            if (entry.Prefab == null)
            {
                Debug.LogError($"[UiPrefabRegistry] Entry '{entry.PrefabId}' has no prefab assigned.", registry);
                errorCount++;
                continue;
            }

            string assetPath = AssetDatabase.GetAssetPath(entry.Prefab);
            string expectedPath = ConvertAssetPathToResourcePath(assetPath);
            if (!string.Equals(expectedPath, entry.ResourcePath, StringComparison.Ordinal))
            {
                Debug.LogError(
                    $"[UiPrefabRegistry] Entry '{entry.PrefabId}' resource path mismatch. Registry='{entry.ResourcePath}', Actual='{expectedPath}'.",
                    entry.Prefab);
                errorCount++;
            }
        }

        if (errorCount == 0)
        {
            Debug.Log($"[UiPrefabRegistry] Validation succeeded for {entries.Count} entries.", registry);
        }
        else
        {
            Debug.LogError($"[UiPrefabRegistry] Validation failed with {errorCount} error(s).", registry);
        }
    }

    public static void ValidateRuntimeUiPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { UiPrefabRoot });
        Array.Sort(prefabGuids, StringComparer.Ordinal);

        int errorCount = 0;
        int warningCount = 0;
        int validatedCount = 0;

        for (int index = 0; index < prefabGuids.Length; index++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(prefabGuids[index]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                continue;
            }

            validatedCount++;
            errorCount += ValidateSinglePrefab(prefab, assetPath, ref warningCount);
        }

        if (errorCount == 0)
        {
            Debug.Log($"[UiValidation] Validated {validatedCount} UI prefab(s). Warnings: {warningCount}, Errors: 0.");
        }
        else
        {
            Debug.LogError($"[UiValidation] Validated {validatedCount} UI prefab(s). Warnings: {warningCount}, Errors: {errorCount}.");
        }
    }

    private static int ValidateSinglePrefab(GameObject prefab, string assetPath, ref int warningCount)
    {
        int errorCount = 0;

        if (prefab.transform is not RectTransform)
        {
            Debug.LogError($"[UiValidation] Prefab '{assetPath}' root must be a RectTransform.", prefab);
            errorCount++;
        }

        foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
            {
                Debug.LogError($"[UiValidation] Prefab '{assetPath}' contains missing script references on '{child.name}'.", prefab);
                errorCount++;
            }
        }

        Button[] buttons = prefab.GetComponentsInChildren<Button>(true);
        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            if (button != null && button.targetGraphic == null)
            {
                Debug.LogError($"[UiValidation] Button '{button.name}' in '{assetPath}' is missing targetGraphic.", prefab);
                errorCount++;
            }
        }

        ScrollRect[] scrollRects = prefab.GetComponentsInChildren<ScrollRect>(true);
        for (int index = 0; index < scrollRects.Length; index++)
        {
            ScrollRect scrollRect = scrollRects[index];
            if (scrollRect == null)
            {
                continue;
            }

            if (scrollRect.viewport == null)
            {
                Debug.LogError($"[UiValidation] ScrollRect '{scrollRect.name}' in '{assetPath}' is missing viewport.", prefab);
                errorCount++;
            }

            if (scrollRect.content == null)
            {
                Debug.LogError($"[UiValidation] ScrollRect '{scrollRect.name}' in '{assetPath}' is missing content.", prefab);
                errorCount++;
            }
        }

        TMP_InputField[] inputFields = prefab.GetComponentsInChildren<TMP_InputField>(true);
        for (int index = 0; index < inputFields.Length; index++)
        {
            TMP_InputField inputField = inputFields[index];
            if (inputField == null)
            {
                continue;
            }

            if (inputField.textComponent == null)
            {
                Debug.LogError($"[UiValidation] TMP_InputField '{inputField.name}' in '{assetPath}' is missing textComponent.", prefab);
                errorCount++;
            }
        }

        UiVirtualList[] virtualLists = prefab.GetComponentsInChildren<UiVirtualList>(true);
        for (int index = 0; index < virtualLists.Length; index++)
        {
            UiVirtualList virtualList = virtualLists[index];
            if (virtualList == null)
            {
                continue;
            }

            if (virtualList.ScrollRect == null)
            {
                Debug.LogError($"[UiValidation] UiVirtualList '{virtualList.name}' in '{assetPath}' is missing ScrollRect reference.", prefab);
                errorCount++;
            }

            if (virtualList.ContentRoot == null)
            {
                Debug.LogError($"[UiValidation] UiVirtualList '{virtualList.name}' in '{assetPath}' is missing ContentRoot reference.", prefab);
                errorCount++;
            }

            if (virtualList.ItemPrefab == null)
            {
                Debug.LogError($"[UiValidation] UiVirtualList '{virtualList.name}' in '{assetPath}' is missing ItemPrefab reference.", prefab);
                errorCount++;
            }
        }

        bool hasTemplate = false;
        MonoBehaviour[] behaviours = prefab.GetComponents<MonoBehaviour>();
        for (int index = 0; index < behaviours.Length; index++)
        {
            MonoBehaviour behaviour = behaviours[index];
            if (behaviour == null)
            {
                continue;
            }

            if (behaviour.GetType().Name.EndsWith("Template", StringComparison.Ordinal))
            {
                hasTemplate = true;
                break;
            }
        }

        if (!hasTemplate)
        {
            Debug.LogWarning($"[UiValidation] Prefab '{assetPath}' has no root-level *Template component. This may be intentional, but formal runtime UI prefabs should usually have one.", prefab);
            warningCount++;
        }

        return errorCount;
    }

    private static UiPrefabRegistry LoadOrCreateRegistry()
    {
        UiPrefabRegistry registry = AssetDatabase.LoadAssetAtPath<UiPrefabRegistry>(RegistryAssetPath);
        if (registry != null)
        {
            return registry;
        }

        registry = ScriptableObject.CreateInstance<UiPrefabRegistry>();
        AssetDatabase.CreateAsset(registry, RegistryAssetPath);
        AssetDatabase.SaveAssets();
        return registry;
    }

    private static void EnsureParentFolderExists()
    {
        if (AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            return;
        }

        AssetDatabase.CreateFolder("Assets", "Resources");
    }

    private static string ConvertAssetPathToResourcePath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.StartsWith(ResourcesRoot, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        string relativePath = assetPath.Substring(ResourcesRoot.Length);
        return Path.ChangeExtension(relativePath, null).Replace('\\', '/');
    }
}
#endif
