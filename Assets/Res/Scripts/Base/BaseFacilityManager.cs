using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BaseFacilityManager : MonoBehaviour
{
    private const int BaseWeaponLockerCapacity = 6;
    private const int WeaponLockerCapacityPerLevel = 2;
    private const int WarehouseSlotsPerLevel = 8;
    private const float WarehouseWeightPerLevel = 12f;
    private const float WorkbenchSellBonusPerLevel = 0.05f;

    [SerializeField] private PrototypeMainMenuController menuController;
    [SerializeField] private BaseHubDirector director;
    [SerializeField] private PrototypeItemCatalog itemCatalog;
    [SerializeField] private bool grantRecoverySuppliesOnRespawn = true;

    private readonly Dictionary<FacilityType, FacilityData> facilityLookup = new Dictionary<FacilityType, FacilityData>();

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        RefreshFacilityCache();
        ApplyFacilityEffects();
        menuController?.RegisterFacilityManager(this);
    }

    public FacilityData GetFacility(FacilityType type)
    {
        RefreshFacilityCache();
        return facilityLookup.TryGetValue(type, out FacilityData data) ? data : null;
    }

    public IReadOnlyList<FacilityData> GetFacilities()
    {
        RefreshFacilityCache();
        var facilities = new List<FacilityData>(facilityLookup.Count);
        foreach (FacilityData data in facilityLookup.Values)
        {
            if (data != null)
            {
                facilities.Add(data);
            }
        }

        facilities.Sort((left, right) => left.type.CompareTo(right.type));
        return facilities;
    }

    public bool UpgradeFacility(FacilityType type)
    {
        ResolveReferences();
        RefreshFacilityCache();

        if (menuController == null)
        {
            return false;
        }

        FacilityData data = GetFacility(type);
        if (data == null)
        {
            menuController.SetFeedback("未找到对应设施。");
            return false;
        }

        if (!data.CanUpgrade())
        {
            menuController.SetFeedback($"{GetFacilityDisplayName(type)}已达到最高等级。");
            return false;
        }

        int upgradeCost = data.GetUpgradeCost();
        if (!menuController.TrySpendFunds(upgradeCost, $"资金不足，无法升级{GetFacilityDisplayName(type)}。"))
        {
            return false;
        }

        data.Upgrade();
        ApplyFacilityEffects();
        menuController.SetFeedback($"{GetFacilityDisplayName(type)}升级到 Lv.{data.Level}。");
        menuController.AutoSaveIfNeeded();
        return true;
    }

    public int GetWarehouseSlotCapacity(int baseSlots)
    {
        FacilityData warehouse = GetFacility(FacilityType.Warehouse);
        return Mathf.Max(baseSlots, baseSlots + Mathf.Max(0, (warehouse != null ? warehouse.Level : 1) - 1) * WarehouseSlotsPerLevel);
    }

    public float GetWarehouseWeightCapacity(float baseWeight)
    {
        if (baseWeight <= 0f)
        {
            return baseWeight;
        }

        FacilityData warehouse = GetFacility(FacilityType.Warehouse);
        return baseWeight + Mathf.Max(0, (warehouse != null ? warehouse.Level : 1) - 1) * WarehouseWeightPerLevel;
    }

    public int GetWeaponLockerCapacity()
    {
        FacilityData armory = GetFacility(FacilityType.Armory);
        return BaseWeaponLockerCapacity + Mathf.Max(0, (armory != null ? armory.Level : 1) - 1) * WeaponLockerCapacityPerLevel;
    }

    public float GetWorkbenchSellBonusMultiplier()
    {
        FacilityData workbench = GetFacility(FacilityType.Workbench);
        return 1f + Mathf.Max(0, (workbench != null ? workbench.Level : 1) - 1) * WorkbenchSellBonusPerLevel;
    }

    public string BuildFacilityEffectSummary(FacilityType type)
    {
        FacilityData data = GetFacility(type);
        int level = data != null ? data.Level : 1;

        switch (type)
        {
            case FacilityType.Warehouse:
                return $"仓库容量提升：+{Mathf.Max(0, level - 1) * WarehouseSlotsPerLevel} 格";

            case FacilityType.Armory:
                return $"武器柜容量：{GetWeaponLockerCapacity()} 把";

            case FacilityType.MedicalStation:
                return $"撤离失败后补给：{GetMedicalSupplyGrantCount(level)} 件恢复物资";

            case FacilityType.Workbench:
                return $"装备出售加成：+{(GetWorkbenchSellBonusMultiplier() - 1f) * 100f:0}%";

            default:
                return string.Empty;
        }
    }

    public void HandleArrival(BaseHubArrivalMode arrivalMode)
    {
        if (arrivalMode != BaseHubArrivalMode.Respawn || !grantRecoverySuppliesOnRespawn)
        {
            return;
        }

        ResolveReferences();
        RefreshFacilityCache();

        if (menuController == null || menuController.StashInventory == null || itemCatalog == null)
        {
            return;
        }

        int supplyCount = GetMedicalSupplyGrantCount(GetFacility(FacilityType.MedicalStation)?.Level ?? 1);
        if (supplyCount <= 0)
        {
            return;
        }

        ItemDefinition bandage = itemCatalog.FindByItemId("bandage_roll");
        if (bandage == null)
        {
            return;
        }

        int granted = 0;
        for (int index = 0; index < supplyCount; index++)
        {
            if (!menuController.StashInventory.TryAddItemInstance(ItemInstance.Create(bandage, 1, null, ItemRarity.Common)))
            {
                break;
            }

            granted++;
        }

        if (granted > 0)
        {
            menuController.SetFeedback($"医疗站为你补给了 {granted} 份绷带。");
            menuController.AutoSaveIfNeeded();
        }
    }

    private void ApplyFacilityEffects()
    {
        ResolveReferences();
        RefreshFacilityCache();
        menuController?.ApplyFacilityManagerRuntime(this);
    }

    private void RefreshFacilityCache()
    {
        facilityLookup.Clear();

        if (menuController?.ProfileWorldState == null)
        {
            return;
        }

        menuController.ProfileWorldState.baseFacilities ??= new List<FacilityData>();
        EnsureFacilityEntry(menuController.ProfileWorldState.baseFacilities, FacilityType.Warehouse);
        EnsureFacilityEntry(menuController.ProfileWorldState.baseFacilities, FacilityType.Armory);
        EnsureFacilityEntry(menuController.ProfileWorldState.baseFacilities, FacilityType.MedicalStation);
        EnsureFacilityEntry(menuController.ProfileWorldState.baseFacilities, FacilityType.Workbench);

        for (int index = 0; index < menuController.ProfileWorldState.baseFacilities.Count; index++)
        {
            FacilityData data = menuController.ProfileWorldState.baseFacilities[index];
            if (data == null)
            {
                continue;
            }

            data.Sanitize(data.type);
            facilityLookup[data.type] = data;
        }
    }

    private void ResolveReferences()
    {
        if (menuController == null)
        {
            menuController = FindFirstObjectByType<PrototypeMainMenuController>();
        }

        if (director == null)
        {
            director = FindFirstObjectByType<BaseHubDirector>();
        }

        if (itemCatalog == null)
        {
            itemCatalog = Resources.Load<PrototypeItemCatalog>("PrototypeItemCatalog");
        }
    }

    private static void EnsureFacilityEntry(List<FacilityData> facilities, FacilityType type)
    {
        for (int index = 0; index < facilities.Count; index++)
        {
            FacilityData data = facilities[index];
            if (data != null && data.type == type)
            {
                data.Sanitize(type);
                return;
            }
        }

        var created = new FacilityData
        {
            type = type,
            level = FacilityData.MinLevel,
            maxLevel = FacilityData.MaxLevel
        };
        created.Sanitize(type);
        facilities.Add(created);
    }

    private static int GetMedicalSupplyGrantCount(int level)
    {
        return Mathf.Clamp(level, FacilityData.MinLevel, FacilityData.MaxLevel);
    }

    private static string GetFacilityDisplayName(FacilityType type)
    {
        switch (type)
        {
            case FacilityType.Warehouse:
                return "仓库";

            case FacilityType.Armory:
                return "武器库";

            case FacilityType.MedicalStation:
                return "医疗站";

            case FacilityType.Workbench:
                return "工作台";

            default:
                return type.ToString();
        }
    }
}
