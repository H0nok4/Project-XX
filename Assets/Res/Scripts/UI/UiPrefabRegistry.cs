using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UiPrefabRegistry", menuName = "ProjectXX/UI/Prefab Registry")]
public sealed class UiPrefabRegistry : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [SerializeField] private string prefabId = string.Empty;
        [SerializeField] private string resourcePath = string.Empty;
        [SerializeField] private GameObject prefab;

        public string PrefabId => string.IsNullOrWhiteSpace(prefabId) ? string.Empty : prefabId.Trim();
        public string ResourcePath => string.IsNullOrWhiteSpace(resourcePath) ? string.Empty : resourcePath.Trim();
        public GameObject Prefab => prefab;

        public void Configure(string id, string path, GameObject prefabAsset)
        {
            prefabId = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
            resourcePath = string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim();
            prefab = prefabAsset;
        }
    }

    private const string RegistryResourcePath = "UI/UiPrefabRegistry";

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<string, GameObject> prefabsById;
    private Dictionary<string, GameObject> prefabsByPath;

    public IReadOnlyList<Entry> Entries => entries;

    private void OnEnable()
    {
        RebuildLookup();
    }

    private void OnValidate()
    {
        RebuildLookup();
    }

    public static UiPrefabRegistry LoadOrNull()
    {
        return Resources.Load<UiPrefabRegistry>(RegistryResourcePath);
    }

    public static GameObject LoadPrefab(string prefabId, string fallbackResourcePath = null)
    {
        UiPrefabRegistry registry = LoadOrNull();
        if (registry != null)
        {
            if (registry.TryGetPrefabById(prefabId, out GameObject prefabById) && prefabById != null)
            {
                return prefabById;
            }

            if (registry.TryGetPrefabByResourcePath(fallbackResourcePath, out GameObject prefabByPath) && prefabByPath != null)
            {
                return prefabByPath;
            }
        }

        return string.IsNullOrWhiteSpace(fallbackResourcePath)
            ? null
            : Resources.Load<GameObject>(fallbackResourcePath.Trim());
    }

    public bool TryGetPrefabById(string prefabId, out GameObject prefab)
    {
        prefab = null;
        EnsureLookup();
        return !string.IsNullOrWhiteSpace(prefabId) && prefabsById.TryGetValue(prefabId.Trim(), out prefab) && prefab != null;
    }

    public bool TryGetPrefabByResourcePath(string resourcePath, out GameObject prefab)
    {
        prefab = null;
        EnsureLookup();
        return !string.IsNullOrWhiteSpace(resourcePath) && prefabsByPath.TryGetValue(resourcePath.Trim(), out prefab) && prefab != null;
    }

    public void ReplaceEntries(IEnumerable<Entry> newEntries)
    {
        entries.Clear();
        if (newEntries != null)
        {
            entries.AddRange(newEntries);
        }

        RebuildLookup();
    }

    private void EnsureLookup()
    {
        if (prefabsById == null || prefabsByPath == null)
        {
            RebuildLookup();
        }
    }

    private void RebuildLookup()
    {
        prefabsById = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        prefabsByPath = new Dictionary<string, GameObject>(StringComparer.Ordinal);

        for (int index = 0; index < entries.Count; index++)
        {
            Entry entry = entries[index];
            if (entry == null || entry.Prefab == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(entry.PrefabId) && !prefabsById.ContainsKey(entry.PrefabId))
            {
                prefabsById.Add(entry.PrefabId, entry.Prefab);
            }

            if (!string.IsNullOrWhiteSpace(entry.ResourcePath) && !prefabsByPath.ContainsKey(entry.ResourcePath))
            {
                prefabsByPath.Add(entry.ResourcePath, entry.Prefab);
            }
        }
    }
}
