using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PrototypeEnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private PrototypeEnemySpawnProfile spawnProfile;
    [SerializeField] private List<Transform> patrolWaypoints = new List<Transform>();
    [SerializeField] private bool randomizeYaw = true;

    private PrototypeBotController spawnedBot;

    public PrototypeEnemySpawnProfile SpawnProfile => spawnProfile;
    public PrototypeBotController SpawnedBot => spawnedBot;

    private void OnValidate()
    {
        if (patrolWaypoints == null)
        {
            patrolWaypoints = new List<Transform>();
            return;
        }

        for (int index = patrolWaypoints.Count - 1; index >= 0; index--)
        {
            if (patrolWaypoints[index] == null)
            {
                patrolWaypoints.RemoveAt(index);
            }
        }
    }

    public void Configure(PrototypeEnemySpawnProfile profile, IEnumerable<Transform> waypoints = null, bool randomYaw = true)
    {
        spawnProfile = profile;
        randomizeYaw = randomYaw;
        patrolWaypoints = new List<Transform>();

        if (waypoints == null)
        {
            return;
        }

        foreach (Transform waypoint in waypoints)
        {
            if (waypoint != null)
            {
                patrolWaypoints.Add(waypoint);
            }
        }
    }

    public PrototypeBotController Spawn(Transform combatTarget, Transform spawnRoot, PrototypeUnitDefinition fallbackUnitDefinition = null)
    {
        if (spawnProfile == null)
        {
            return null;
        }

        List<Vector3> patrolPoints = new List<Vector3>();
        if (patrolWaypoints != null && patrolWaypoints.Count > 0)
        {
            for (int index = 0; index < patrolWaypoints.Count; index++)
            {
                if (patrolWaypoints[index] != null)
                {
                    patrolPoints.Add(patrolWaypoints[index].position);
                }
            }
        }
        else
        {
            patrolPoints.Add(transform.position);
        }

        Quaternion rotation = randomizeYaw
            ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
            : transform.rotation;

        spawnedBot = PrototypeEnemyRuntimeFactory.SpawnEnemy(
            spawnProfile,
            combatTarget,
            transform.position,
            rotation,
            spawnRoot,
            patrolPoints,
            fallbackUnitDefinition,
            spawnProfile.DisplayName);

        return spawnedBot;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.86f, 0.2f, 0.18f, 0.35f);
        Gizmos.DrawSphere(transform.position, 0.22f);
        Gizmos.DrawWireSphere(transform.position, 0.4f);

        if (patrolWaypoints == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.68f, 0.2f, 0.8f);
        Vector3 previousPoint = transform.position;
        for (int index = 0; index < patrolWaypoints.Count; index++)
        {
            Transform waypoint = patrolWaypoints[index];
            if (waypoint == null)
            {
                continue;
            }

            Gizmos.DrawLine(previousPoint, waypoint.position);
            Gizmos.DrawSphere(waypoint.position, 0.12f);
            previousPoint = waypoint.position;
        }
    }
}
