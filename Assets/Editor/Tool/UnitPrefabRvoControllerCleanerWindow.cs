#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class UnitPrefabRvoControllerCleanerWindow : EditorWindow
{
    private const string WindowTitle = "Unit RVO Cleaner";
    private const string DefaultPrefabFolder = "Assets/Game/Prefabs/Character";
    private const string TargetTypeFullName = "Pathfinding.RVO.RVOController";
    private const int PreviewLimit = 20;

    [SerializeField] private DefaultAsset _targetFolder;
    [SerializeField] private bool _logEachModifiedPrefab = true;

    private readonly List<string> _lastMatchedPrefabPaths = new List<string>();
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Prefab Tools/Remove Unit RVOController")]
    private static void OpenWindow()
    {
        UnitPrefabRvoControllerCleanerWindow window = GetWindow<UnitPrefabRvoControllerCleanerWindow>(WindowTitle);
        window.minSize = new Vector2(560f, 320f);
        window.Show();
    }

    private void OnEnable()
    {
        if (_targetFolder == null)
        {
            _targetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultPrefabFolder);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Remove RVOController From Unit Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool scans prefabs under the selected folder and removes Pathfinding.RVO.RVOController " +
            "with Unity Editor APIs.\nDefault target folder: " + DefaultPrefabFolder,
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            _targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "Prefab Folder",
                _targetFolder,
                typeof(DefaultAsset),
                false);

            if (GUILayout.Button("Use Default", GUILayout.Width(100f)))
            {
                _targetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultPrefabFolder);
            }
        }

        _logEachModifiedPrefab = EditorGUILayout.ToggleLeft(
            "Log every modified prefab to Console",
            _logEachModifiedPrefab);

        string folderPath = GetFolderPath(_targetFolder);
        if (string.IsNullOrEmpty(folderPath))
        {
            EditorGUILayout.HelpBox("Please assign a valid folder from the Project window.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField("Current Folder", folderPath);
        }

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(folderPath)))
            {
                if (GUILayout.Button("Scan Matching Prefabs", GUILayout.Height(28f)))
                {
                    ScanMatchingPrefabs(folderPath);
                }

                if (GUILayout.Button("Remove RVOController", GUILayout.Height(28f)))
                {
                    RemoveRvoControllers(folderPath);
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Last Scan Result: {_lastMatchedPrefabPaths.Count} prefab(s)", EditorStyles.boldLabel);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        int previewCount = Mathf.Min(PreviewLimit, _lastMatchedPrefabPaths.Count);
        for (int i = 0; i < previewCount; i++)
        {
            EditorGUILayout.SelectableLabel(
                _lastMatchedPrefabPaths[i],
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        if (_lastMatchedPrefabPaths.Count > PreviewLimit)
        {
            EditorGUILayout.LabelField($"...and {_lastMatchedPrefabPaths.Count - PreviewLimit} more.");
        }

        EditorGUILayout.EndScrollView();
    }

    private void ScanMatchingPrefabs(string folderPath)
    {
        if (!TryResolveTargetType(out Type targetType))
        {
            return;
        }

        _lastMatchedPrefabPaths.Clear();
        _lastMatchedPrefabPaths.AddRange(FindMatchingPrefabPaths(folderPath, targetType));

        Debug.Log(
            $"[UnitPrefabRvoControllerCleaner] Scan finished. Folder: {folderPath}, Matches: " +
            $"{_lastMatchedPrefabPaths.Count}.");

        Repaint();
    }

    private void RemoveRvoControllers(string folderPath)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Remove RVOController",
                "Please exit Play Mode before running this tool.",
                "OK");
            return;
        }

        if (!TryResolveTargetType(out Type targetType))
        {
            return;
        }

        List<string> matchingPaths = FindMatchingPrefabPaths(folderPath, targetType);
        _lastMatchedPrefabPaths.Clear();
        _lastMatchedPrefabPaths.AddRange(matchingPaths);

        if (matchingPaths.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Remove RVOController",
                "No prefab with RVOController was found in the selected folder.",
                "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Remove RVOController",
            $"Found {matchingPaths.Count} prefab(s) under:\n{folderPath}\n\nRemove all RVOController components?",
            "Remove",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        int modifiedPrefabCount = 0;
        int removedComponentCount = 0;

        try
        {
            for (int i = 0; i < matchingPaths.Count; i++)
            {
                string prefabPath = matchingPaths[i];
                float progress = (float)(i + 1) / matchingPaths.Count;
                EditorUtility.DisplayProgressBar("Remove RVOController", prefabPath, progress);

                if (!RemoveTargetComponentsFromPrefab(prefabPath, targetType, out int removedCount))
                {
                    continue;
                }

                modifiedPrefabCount++;
                removedComponentCount += removedCount;

                if (_logEachModifiedPrefab)
                {
                    Debug.Log(
                        $"[UnitPrefabRvoControllerCleaner] Removed {removedCount} RVOController component(s): " +
                        prefabPath);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog(
            "Remove RVOController",
            $"Done. Modified {modifiedPrefabCount} prefab(s) and removed {removedComponentCount} component(s).",
            "OK");
    }

    private static bool RemoveTargetComponentsFromPrefab(string prefabPath, Type targetType, out int removedCount)
    {
        removedCount = 0;
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogWarning($"[UnitPrefabRvoControllerCleaner] Failed to load prefab: {prefabPath}");
            return false;
        }

        try
        {
            Component[] components = prefabRoot.GetComponentsInChildren(targetType, true);
            if (components == null || components.Length == 0)
            {
                return false;
            }

            for (int i = components.Length - 1; i >= 0; i--)
            {
                Component component = components[i];
                if (component == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(component);
                removedCount++;
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            return removedCount > 0;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static List<string> FindMatchingPrefabPaths(string folderPath, Type targetType)
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        List<string> results = new List<string>();

        try
        {
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                float progress = (float)(i + 1) / Mathf.Max(1, prefabGuids.Length);
                EditorUtility.DisplayProgressBar("Scan Prefabs", prefabPath, progress);

                if (PrefabContainsTargetComponent(prefabPath, targetType))
                {
                    results.Add(prefabPath);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        results.Sort(StringComparer.Ordinal);
        return results;
    }

    private static bool PrefabContainsTargetComponent(string prefabPath, Type targetType)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogWarning($"[UnitPrefabRvoControllerCleaner] Failed to load prefab: {prefabPath}");
            return false;
        }

        try
        {
            return prefabRoot.GetComponentInChildren(targetType, true) != null;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static string GetFolderPath(DefaultAsset asset)
    {
        if (asset == null)
        {
            return null;
        }

        string path = AssetDatabase.GetAssetPath(asset);
        if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
        {
            return null;
        }

        return path;
    }

    private static bool TryResolveTargetType(out Type targetType)
    {
        targetType = FindType(TargetTypeFullName);
        if (targetType != null && typeof(MonoBehaviour).IsAssignableFrom(targetType))
        {
            return true;
        }

        EditorUtility.DisplayDialog(
            "Remove RVOController",
            "Could not resolve Pathfinding.RVO.RVOController. Please fix compile errors first.",
            "OK");
        return false;
    }

    private static Type FindType(string fullName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types.Where(type => type != null).ToArray();
            }

            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type != null && type.FullName == fullName)
                {
                    return type;
                }
            }
        }

        return null;
    }
}
#endif
