using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PrototypeEnemySpawnArea : MonoBehaviour
{
    [SerializeField] private List<PrototypeEnemySpawnProfile> spawnProfiles = new List<PrototypeEnemySpawnProfile>();
    [Min(1)]
    [SerializeField] private int minSpawnCount = 1;
    [Min(1)]
    [SerializeField] private int maxSpawnCount = 3;
    [SerializeField] private Vector3 areaSize = new Vector3(6f, 2f, 6f);
    [Min(0.5f)]
    [SerializeField] private float patrolRadius = 3f;
    [Min(1)]
    [SerializeField] private int patrolPointsPerSpawn = 3;
    [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;
    [Min(0.5f)]
    [SerializeField] private float groundProbeHeight = 4f;

    public IReadOnlyList<PrototypeEnemySpawnProfile> SpawnProfiles => spawnProfiles;

    private void OnValidate()
    {
        minSpawnCount = Mathf.Max(1, minSpawnCount);
        maxSpawnCount = Mathf.Max(minSpawnCount, maxSpawnCount);
        patrolRadius = Mathf.Max(0.5f, patrolRadius);
        patrolPointsPerSpawn = Mathf.Max(1, patrolPointsPerSpawn);
        areaSize.x = Mathf.Max(0.5f, areaSize.x);
        areaSize.y = Mathf.Max(0.5f, areaSize.y);
        areaSize.z = Mathf.Max(0.5f, areaSize.z);

        if (spawnProfiles == null)
        {
            spawnProfiles = new List<PrototypeEnemySpawnProfile>();
            return;
        }

        for (int index = spawnProfiles.Count - 1; index >= 0; index--)
        {
            if (spawnProfiles[index] == null)
            {
                spawnProfiles.RemoveAt(index);
            }
        }
    }

    public void Configure(IEnumerable<PrototypeEnemySpawnProfile> profiles, int minimumCount, int maximumCount, Vector3 size, float patrolRange = 3f, int generatedPatrolPoints = 3)
    {
        spawnProfiles = new List<PrototypeEnemySpawnProfile>();
        if (profiles != null)
        {
            foreach (PrototypeEnemySpawnProfile profile in profiles)
            {
                if (profile != null)
                {
                    spawnProfiles.Add(profile);
                }
            }
        }

        minSpawnCount = Mathf.Max(1, minimumCount);
        maxSpawnCount = Mathf.Max(minSpawnCount, maximumCount);
        areaSize = new Vector3(
            Mathf.Max(0.5f, size.x),
            Mathf.Max(0.5f, size.y),
            Mathf.Max(0.5f, size.z));
        patrolRadius = Mathf.Max(0.5f, patrolRange);
        patrolPointsPerSpawn = Mathf.Max(1, generatedPatrolPoints);
    }

    public List<PrototypeBotController> Spawn(Transform combatTarget, Transform spawnRoot, PrototypeUnitDefinition fallbackUnitDefinition = null)
    {
        var spawnedBots = new List<PrototypeBotController>();
        if (spawnProfiles == null || spawnProfiles.Count == 0)
        {
            return spawnedBots;
        }

        int spawnCount = Random.Range(minSpawnCount, maxSpawnCount + 1);
        for (int index = 0; index < spawnCount; index++)
        {
            PrototypeEnemySpawnProfile profile = spawnProfiles[Random.Range(0, spawnProfiles.Count)];
            if (profile == null)
            {
                continue;
            }

            Vector3 spawnPosition = ResolveSpawnPosition();
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            List<Vector3> patrolPoints = BuildPatrolPoints(spawnPosition);
            PrototypeBotController bot = PrototypeEnemyRuntimeFactory.SpawnEnemy(
                profile,
                combatTarget,
                spawnPosition,
                rotation,
                spawnRoot,
                patrolPoints,
                fallbackUnitDefinition,
                $"{profile.DisplayName}_{index + 1}");

            if (bot != null)
            {
                spawnedBots.Add(bot);
            }
        }

        return spawnedBots;
    }

    private Vector3 ResolveSpawnPosition()
    {
        Vector3 extents = areaSize * 0.5f;
        Vector3 localOffset = new Vector3(
            Random.Range(-extents.x, extents.x),
            0f,
            Random.Range(-extents.z, extents.z));
        Vector3 worldPosition = transform.TransformPoint(localOffset);
        Vector3 rayOrigin = worldPosition + Vector3.up * Mathf.Max(groundProbeHeight, extents.y + 1f);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundProbeHeight * 2f + extents.y, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return worldPosition;
    }

    private List<Vector3> BuildPatrolPoints(Vector3 origin)
    {
        var patrolPoints = new List<Vector3> { origin };
        for (int index = 0; index < patrolPointsPerSpawn - 1; index++)
        {
            Vector2 offset = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = origin + new Vector3(offset.x, 0f, offset.y);
            Vector3 rayOrigin = candidate + Vector3.up * groundProbeHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundProbeHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                candidate = hit.point;
            }

            patrolPoints.Add(candidate);
        }

        return patrolPoints;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.78f, 0.2f, 0.18f, 0.2f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, areaSize);
        Gizmos.DrawWireCube(Vector3.zero, areaSize);
    }
}
