using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PrototypeEncounterDirector : MonoBehaviour
{
    [SerializeField] private Transform combatTarget;
    [SerializeField] private PrototypeUnitDefinition fallbackUnitDefinition;
    [SerializeField] private bool autoDiscoverChildSpawners = true;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private Transform runtimeSpawnRoot;
    [SerializeField] private List<PrototypeEnemySpawnPoint> spawnPoints = new List<PrototypeEnemySpawnPoint>();
    [SerializeField] private List<PrototypeEnemySpawnArea> spawnAreas = new List<PrototypeEnemySpawnArea>();

    private bool hasSpawned;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnAll();
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void Configure(Transform target, PrototypeUnitDefinition unitDefinition = null)
    {
        combatTarget = target;
        if (unitDefinition != null)
        {
            fallbackUnitDefinition = unitDefinition;
        }

        ResolveReferences();
    }

    public void SpawnAll(bool forceRespawn = false)
    {
        if (hasSpawned && !forceRespawn)
        {
            return;
        }

        ResolveReferences();
        if (runtimeSpawnRoot == null)
        {
            GameObject runtimeRootObject = new GameObject("RuntimeEnemySpawns");
            runtimeRootObject.transform.SetParent(transform, false);
            runtimeSpawnRoot = runtimeRootObject.transform;
        }

        if (forceRespawn)
        {
            ClearSpawnedEnemies();
        }

        for (int index = 0; index < spawnPoints.Count; index++)
        {
            if (spawnPoints[index] != null)
            {
                spawnPoints[index].Spawn(combatTarget, runtimeSpawnRoot, fallbackUnitDefinition);
            }
        }

        for (int index = 0; index < spawnAreas.Count; index++)
        {
            if (spawnAreas[index] != null)
            {
                spawnAreas[index].Spawn(combatTarget, runtimeSpawnRoot, fallbackUnitDefinition);
            }
        }

        hasSpawned = true;
    }

    public void ClearSpawnedEnemies()
    {
        if (runtimeSpawnRoot == null)
        {
            return;
        }

        for (int index = runtimeSpawnRoot.childCount - 1; index >= 0; index--)
        {
            Transform child = runtimeSpawnRoot.GetChild(index);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        hasSpawned = false;
    }

    private void ResolveReferences()
    {
        if (combatTarget == null)
        {
            PrototypeFpsController playerController = Object.FindFirstObjectByType<PrototypeFpsController>();
            if (playerController != null)
            {
                combatTarget = playerController.transform;
            }
        }

        if (autoDiscoverChildSpawners)
        {
            spawnPoints = new List<PrototypeEnemySpawnPoint>(GetComponentsInChildren<PrototypeEnemySpawnPoint>(true));
            spawnAreas = new List<PrototypeEnemySpawnArea>(GetComponentsInChildren<PrototypeEnemySpawnArea>(true));
        }
        else
        {
            SanitizeList(spawnPoints);
            SanitizeList(spawnAreas);
        }
    }

    private static void SanitizeList<T>(List<T> components) where T : Component
    {
        if (components == null)
        {
            return;
        }

        for (int index = components.Count - 1; index >= 0; index--)
        {
            if (components[index] == null)
            {
                components.RemoveAt(index);
            }
        }
    }
}
