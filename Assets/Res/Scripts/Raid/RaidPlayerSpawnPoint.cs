using UnityEngine;

[DisallowMultipleComponent]
public class RaidPlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private RaidGameMode raidGameMode;
    [SerializeField] private string spawnLabel = "Player Spawn";

    public string SpawnLabel => string.IsNullOrWhiteSpace(spawnLabel) ? name : spawnLabel.Trim();

    private void OnEnable()
    {
        ResolveReferences();
        raidGameMode?.RegisterPlayerSpawnPoint(this);
    }

    private void OnDisable()
    {
        raidGameMode?.UnregisterPlayerSpawnPoint(this);
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (raidGameMode == null)
        {
            raidGameMode = FindFirstObjectByType<RaidGameMode>();
        }

        spawnLabel = string.IsNullOrWhiteSpace(spawnLabel) ? name : spawnLabel.Trim();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.18f, 0.82f, 1f, 0.9f);
        Vector3 position = transform.position;
        Gizmos.DrawSphere(position + Vector3.up * 0.15f, 0.18f);
        Gizmos.DrawLine(position + Vector3.up * 0.2f, position + transform.forward * 0.85f + Vector3.up * 0.2f);
        Gizmos.DrawWireSphere(position + Vector3.up * 0.9f, 0.35f);
    }
}
