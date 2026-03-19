using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public sealed class RaidScenePlayerBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private bool spawnOnAwake = true;
    [SerializeField] private bool useRaidSpawnPoints;

    public GameObject PlayerPrefab => playerPrefab;

    private void Awake()
    {
        if (!Application.isPlaying || !spawnOnAwake)
        {
            return;
        }

        EnsurePlayerSpawned();
    }

    public PrototypeFpsController EnsurePlayerSpawned()
    {
        PrototypeFpsController existingPlayer = FindExistingPlayerInScene();
        if (existingPlayer != null)
        {
            return existingPlayer;
        }

        if (playerPrefab == null)
        {
            Debug.LogWarning($"[RaidScenePlayerBootstrap] Missing player prefab on '{name}'.", this);
            return null;
        }

        Pose spawnPose = ResolveSpawnPose();
        GameObject instance = Instantiate(playerPrefab, spawnPose.position, spawnPose.rotation);
        instance.name = playerPrefab.name;

        if (instance.scene != gameObject.scene)
        {
            SceneManager.MoveGameObjectToScene(instance, gameObject.scene);
        }

        return instance.GetComponent<PrototypeFpsController>();
    }

    private PrototypeFpsController FindExistingPlayerInScene()
    {
        PrototypeFpsController[] players = FindObjectsByType<PrototypeFpsController>(FindObjectsSortMode.None);
        for (int index = 0; index < players.Length; index++)
        {
            PrototypeFpsController player = players[index];
            if (player != null && player.gameObject.scene == gameObject.scene)
            {
                return player;
            }
        }

        return null;
    }

    private Pose ResolveSpawnPose()
    {
        if (!useRaidSpawnPoints)
        {
            return new Pose(transform.position, transform.rotation);
        }

        List<RaidPlayerSpawnPoint> sceneSpawnPoints = GetSceneSpawnPoints();
        if (sceneSpawnPoints.Count == 0)
        {
            return new Pose(transform.position, transform.rotation);
        }

        sceneSpawnPoints.Sort(static (left, right) =>
            string.CompareOrdinal(left != null ? left.name : string.Empty, right != null ? right.name : string.Empty));

        Transform spawnTransform = sceneSpawnPoints[0].transform;
        return new Pose(spawnTransform.position, spawnTransform.rotation);
    }

    private List<RaidPlayerSpawnPoint> GetSceneSpawnPoints()
    {
        RaidPlayerSpawnPoint[] spawnPoints = FindObjectsByType<RaidPlayerSpawnPoint>(FindObjectsSortMode.None);
        var sceneSpawnPoints = new List<RaidPlayerSpawnPoint>(spawnPoints.Length);

        for (int index = 0; index < spawnPoints.Length; index++)
        {
            RaidPlayerSpawnPoint spawnPoint = spawnPoints[index];
            if (spawnPoint == null || spawnPoint.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            sceneSpawnPoints.Add(spawnPoint);
        }

        return sceneSpawnPoints;
    }
}
