using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GroundLootSpawnPoint : MonoBehaviour
{
    [SerializeField] private LootTableDefinition lootTable;
    [SerializeField] private bool spawnOnStart = true;
    [Min(0f)]
    [SerializeField] private float scatterRadius = 0.6f;
    [Min(0.05f)]
    [SerializeField] private float spawnHeightOffset = 0.12f;
    [SerializeField] private string pickupVerb = "Pick Up";
    [SerializeField] private bool clearPreviousLoot = true;
    [SerializeField] private Transform pickupParent;

    private readonly List<GroundLootItem> spawnedItems = new List<GroundLootItem>();
    private bool hasSpawnedLoot;

    public LootTableDefinition LootTable => lootTable;
    public bool HasSpawnedLoot => hasSpawnedLoot;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnLoot();
        }
    }

    private void OnValidate()
    {
        scatterRadius = Mathf.Max(0f, scatterRadius);
        spawnHeightOffset = Mathf.Max(0.05f, spawnHeightOffset);
        pickupVerb = string.IsNullOrWhiteSpace(pickupVerb) ? "Pick Up" : pickupVerb.Trim();
    }

    public void Configure(LootTableDefinition table, float radius = 0.6f, string verb = "Pick Up")
    {
        lootTable = table;
        scatterRadius = Mathf.Max(0f, radius);
        pickupVerb = string.IsNullOrWhiteSpace(verb) ? "Pick Up" : verb.Trim();
    }

    public void SpawnLoot()
    {
        if (lootTable == null)
        {
            return;
        }

        if (clearPreviousLoot)
        {
            ClearSpawnedLoot();
        }

        List<LootTableDefinition.LootRoll> rolls = lootTable.RollLoot();
        for (int index = 0; index < rolls.Count; index++)
        {
            LootTableDefinition.LootRoll roll = rolls[index];
            if (roll.Definition == null || roll.Quantity <= 0)
            {
                continue;
            }

            Vector3 spawnPosition = ResolveSpawnPosition(index);
            GroundLootItem pickup = GroundLootItem.SpawnScenePickup(
                spawnPosition,
                roll.Definition,
                roll.Quantity,
                pickupVerb,
                pickupParent);

            if (pickup != null)
            {
                spawnedItems.Add(pickup);
            }
        }

        hasSpawnedLoot = true;
    }

    public void ClearSpawnedLoot()
    {
        for (int index = spawnedItems.Count - 1; index >= 0; index--)
        {
            GroundLootItem pickup = spawnedItems[index];
            if (pickup == null)
            {
                spawnedItems.RemoveAt(index);
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(pickup.gameObject);
            }
            else
            {
                DestroyImmediate(pickup.gameObject);
            }
        }

        spawnedItems.Clear();
        hasSpawnedLoot = false;
    }

    private Vector3 ResolveSpawnPosition(int index)
    {
        Vector2 scatter = scatterRadius > 0f
            ? Random.insideUnitCircle * scatterRadius
            : Vector2.zero;

        Vector3 candidate = transform.position + new Vector3(scatter.x, 0f, scatter.y);
        Vector3 rayOrigin = candidate + Vector3.up * 2f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            candidate = hit.point;
        }

        return candidate + Vector3.up * spawnHeightOffset;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.98f, 0.82f, 0.22f, 0.35f);
        Gizmos.DrawSphere(transform.position, 0.12f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.08f, scatterRadius));
    }
}
