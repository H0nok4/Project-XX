using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/AI/Enemy Spawn Profile", fileName = "EnemySpawnProfile")]
public class PrototypeEnemySpawnProfile : ScriptableObject
{
    [SerializeField] private string enemyId = "enemy_profile";
    [SerializeField] private string displayName = "Enemy";
    [SerializeField] private PrototypeEnemyArchetype archetype = PrototypeEnemyArchetype.RegularZombie;
    [SerializeField] private PrototypeUnitDefinition unitDefinition;
    [SerializeField] private PrototypeWeaponDefinition primaryWeapon;
    [SerializeField] private LootTableDefinition carriedLootTable;
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Vector3 localScale = new Vector3(0.9f, 1.1f, 0.9f);
    [SerializeField] private List<ArmorDefinition> armorLoadout = new List<ArmorDefinition>();

    public string EnemyId => string.IsNullOrWhiteSpace(enemyId) ? name : enemyId.Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? EnemyId : displayName.Trim();
    public PrototypeEnemyArchetype Archetype => archetype;
    public PrototypeUnitDefinition UnitDefinition => unitDefinition;
    public PrototypeWeaponDefinition PrimaryWeapon => primaryWeapon;
    public LootTableDefinition CarriedLootTable => carriedLootTable;
    public Material BodyMaterial => bodyMaterial;
    public Vector3 LocalScale => localScale;
    public IReadOnlyList<ArmorDefinition> ArmorLoadout => armorLoadout;

    public void Configure(
        string id,
        string label,
        PrototypeEnemyArchetype enemyArchetype,
        PrototypeUnitDefinition definition,
        PrototypeWeaponDefinition weapon,
        Material material,
        Vector3 scale,
        params ArmorDefinition[] armor)
    {
        enemyId = string.IsNullOrWhiteSpace(id) ? name : id.Trim();
        displayName = string.IsNullOrWhiteSpace(label) ? enemyId : label.Trim();
        archetype = enemyArchetype;
        unitDefinition = definition;
        primaryWeapon = weapon;
        bodyMaterial = material;
        localScale = SanitizeScale(scale);
        armorLoadout = new List<ArmorDefinition>();

        if (armor == null)
        {
            return;
        }

        for (int index = 0; index < armor.Length; index++)
        {
            if (armor[index] != null)
            {
                armorLoadout.Add(armor[index]);
            }
        }
    }

    public void SetCarriedLootTable(LootTableDefinition lootTable)
    {
        carriedLootTable = lootTable;
    }

    private void OnValidate()
    {
        enemyId = string.IsNullOrWhiteSpace(enemyId) ? name : enemyId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? EnemyId : displayName.Trim();
        localScale = SanitizeScale(localScale);

        if (armorLoadout == null)
        {
            armorLoadout = new List<ArmorDefinition>();
            return;
        }

        for (int index = armorLoadout.Count - 1; index >= 0; index--)
        {
            if (armorLoadout[index] == null)
            {
                armorLoadout.RemoveAt(index);
            }
        }
    }

    private static Vector3 SanitizeScale(Vector3 scale)
    {
        scale.x = Mathf.Max(0.2f, scale.x);
        scale.y = Mathf.Max(0.2f, scale.y);
        scale.z = Mathf.Max(0.2f, scale.z);
        return scale;
    }
}
