using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/AI/Enemy Spawn Profile", fileName = "EnemySpawnProfile")]
public class PrototypeEnemySpawnProfile : ScriptableObject
{
    [SerializeField] private string enemyId = "enemy_profile";
    [SerializeField] private string displayName = "Enemy";
    [SerializeField] private PrototypeEnemyArchetype archetype = PrototypeEnemyArchetype.RegularZombie;
    [SerializeField] private PrototypeUnitDefinition unitDefinition;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private PrototypeWeaponDefinition primaryWeapon;
    [SerializeField] private List<PrototypeWeaponDefinition> weaponPool = new List<PrototypeWeaponDefinition>();
    [Header("Primary Weapon Rarity")]
    [Min(0f)]
    [SerializeField] private float primaryWeaponCommonWeight = 60f;
    [Min(0f)]
    [SerializeField] private float primaryWeaponUncommonWeight = 25f;
    [Min(0f)]
    [SerializeField] private float primaryWeaponRareWeight = 10f;
    [Min(0f)]
    [SerializeField] private float primaryWeaponEpicWeight = 4f;
    [Min(0f)]
    [SerializeField] private float primaryWeaponLegendaryWeight = 1f;
    [Header("Boss Loot")]
    [SerializeField] private bool bossProfile;
    [Range(0, 3)]
    [SerializeField] private int bossLootRarityBias = 1;
    [Range(0, 3)]
    [SerializeField] private int bossPrimaryWeaponRarityBias = 1;
    [Min(0)]
    [SerializeField] private int bossBonusLootRolls = 1;
    [Min(0)]
    [SerializeField] private int bossItemLevelBonus = 2;
    [SerializeField] private LootTableDefinition carriedLootTable;
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Vector3 localScale = new Vector3(0.9f, 1.1f, 0.9f);
    [SerializeField] private List<ArmorDefinition> armorLoadout = new List<ArmorDefinition>();

    public string EnemyId => string.IsNullOrWhiteSpace(enemyId) ? name : enemyId.Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? EnemyId : displayName.Trim();
    public PrototypeEnemyArchetype Archetype => archetype;
    public PrototypeUnitDefinition UnitDefinition => unitDefinition;
    public GameObject EnemyPrefab => enemyPrefab;
    public PrototypeWeaponDefinition PrimaryWeapon => primaryWeapon;
    public IReadOnlyList<PrototypeWeaponDefinition> WeaponPool => weaponPool;
    public float PrimaryWeaponCommonWeight => Mathf.Max(0f, primaryWeaponCommonWeight);
    public float PrimaryWeaponUncommonWeight => Mathf.Max(0f, primaryWeaponUncommonWeight);
    public float PrimaryWeaponRareWeight => Mathf.Max(0f, primaryWeaponRareWeight);
    public float PrimaryWeaponEpicWeight => Mathf.Max(0f, primaryWeaponEpicWeight);
    public float PrimaryWeaponLegendaryWeight => Mathf.Max(0f, primaryWeaponLegendaryWeight);
    public bool IsBossProfile => bossProfile;
    public int BossLootRarityBias => bossProfile ? Mathf.Clamp(bossLootRarityBias, 0, 3) : 0;
    public int BossPrimaryWeaponRarityBias => bossProfile ? Mathf.Clamp(bossPrimaryWeaponRarityBias, 0, 3) : 0;
    public int BossBonusLootRolls => bossProfile ? Mathf.Max(0, bossBonusLootRolls) : 0;
    public int BossItemLevelBonus => bossProfile ? Mathf.Max(0, bossItemLevelBonus) : 0;
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

    public void SetEnemyPrefab(GameObject prefab)
    {
        enemyPrefab = prefab;
    }

    public void SetWeaponPool(params PrototypeWeaponDefinition[] weapons)
    {
        weaponPool = new List<PrototypeWeaponDefinition>();
        if (weapons == null)
        {
            return;
        }

        for (int index = 0; index < weapons.Length; index++)
        {
            if (weapons[index] != null)
            {
                weaponPool.Add(weapons[index]);
            }
        }
    }

    public PrototypeWeaponDefinition ResolvePrimaryWeapon()
    {
        if (weaponPool != null && weaponPool.Count > 0)
        {
            int startIndex = Random.Range(0, weaponPool.Count);
            for (int offset = 0; offset < weaponPool.Count; offset++)
            {
                PrototypeWeaponDefinition candidate = weaponPool[(startIndex + offset) % weaponPool.Count];
                if (candidate != null)
                {
                    return candidate;
                }
            }
        }

        return primaryWeapon;
    }

    public ItemRarity RollPrimaryWeaponRarity(int additionalRarityBias = 0)
    {
        return ItemRarityUtility.RollWeightedBiased(
            PrimaryWeaponCommonWeight,
            PrimaryWeaponUncommonWeight,
            PrimaryWeaponRareWeight,
            PrimaryWeaponEpicWeight,
            PrimaryWeaponLegendaryWeight,
            BossPrimaryWeaponRarityBias + Mathf.Max(0, additionalRarityBias));
    }

    public void SetPrimaryWeaponRarityWeights(
        float commonWeight,
        float uncommonWeight,
        float rareWeight,
        float epicWeight,
        float legendaryWeight)
    {
        primaryWeaponCommonWeight = Mathf.Max(0f, commonWeight);
        primaryWeaponUncommonWeight = Mathf.Max(0f, uncommonWeight);
        primaryWeaponRareWeight = Mathf.Max(0f, rareWeight);
        primaryWeaponEpicWeight = Mathf.Max(0f, epicWeight);
        primaryWeaponLegendaryWeight = Mathf.Max(0f, legendaryWeight);
    }

    public void SetBossLootProfile(
        bool isBoss,
        int lootRarityBias = 1,
        int primaryWeaponRarityBias = 1,
        int bonusLootRolls = 1,
        int itemLevelBonus = 2)
    {
        bossProfile = isBoss;
        bossLootRarityBias = Mathf.Clamp(lootRarityBias, 0, 3);
        bossPrimaryWeaponRarityBias = Mathf.Clamp(primaryWeaponRarityBias, 0, 3);
        bossBonusLootRolls = Mathf.Max(0, bonusLootRolls);
        bossItemLevelBonus = Mathf.Max(0, itemLevelBonus);
    }

    private void OnValidate()
    {
        enemyId = string.IsNullOrWhiteSpace(enemyId) ? name : enemyId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? EnemyId : displayName.Trim();
        localScale = SanitizeScale(localScale);
        primaryWeaponCommonWeight = Mathf.Max(0f, primaryWeaponCommonWeight);
        primaryWeaponUncommonWeight = Mathf.Max(0f, primaryWeaponUncommonWeight);
        primaryWeaponRareWeight = Mathf.Max(0f, primaryWeaponRareWeight);
        primaryWeaponEpicWeight = Mathf.Max(0f, primaryWeaponEpicWeight);
        primaryWeaponLegendaryWeight = Mathf.Max(0f, primaryWeaponLegendaryWeight);
        bossLootRarityBias = Mathf.Clamp(bossLootRarityBias, 0, 3);
        bossPrimaryWeaponRarityBias = Mathf.Clamp(bossPrimaryWeaponRarityBias, 0, 3);
        bossBonusLootRolls = Mathf.Max(0, bossBonusLootRolls);
        bossItemLevelBonus = Mathf.Max(0, bossItemLevelBonus);

        if (armorLoadout == null)
        {
            armorLoadout = new List<ArmorDefinition>();
        }
        else
        {
            for (int index = armorLoadout.Count - 1; index >= 0; index--)
            {
                if (armorLoadout[index] == null)
                {
                    armorLoadout.RemoveAt(index);
                }
            }
        }

        if (weaponPool == null)
        {
            weaponPool = new List<PrototypeWeaponDefinition>();
            return;
        }

        for (int index = weaponPool.Count - 1; index >= 0; index--)
        {
            if (weaponPool[index] == null)
            {
                weaponPool.RemoveAt(index);
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
