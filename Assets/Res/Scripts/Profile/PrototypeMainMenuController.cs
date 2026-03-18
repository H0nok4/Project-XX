using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PrototypeMainMenuController : MonoBehaviour
{
    private const string MenuUiPrefabResourcePath = "UI/PrototypeMainMenuUgui";

    [Serializable]
    private sealed class RaidSceneOption
    {
        public string displayName = "原型战区";
        public string sceneName = "SampleScene";
        [TextArea(2, 4)] public string description = "原型室内战斗区域。";

        public void Sanitize(string fallbackSceneName, int fallbackIndex)
        {
            sceneName = string.IsNullOrWhiteSpace(sceneName)
                ? fallbackSceneName
                : sceneName.Trim();
            displayName = string.IsNullOrWhiteSpace(displayName)
                ? $"战区 {fallbackIndex + 1}"
                : displayName.Trim();
            description = string.IsNullOrWhiteSpace(description)
                ? "进入所选战斗区域。"
                : description.Trim();
        }
    }

    internal enum MenuPage
    {
        Home = 0,
        Warehouse = 1,
        Merchants = 2
    }

    internal enum WeaponSlotType
    {
        Primary = 0,
        Secondary = 1,
        Melee = 2
    }

    internal enum MenuUiMode
    {
        ImmediateGui = 0,
        Ugui = 1
    }

    public enum MetaShellMode
    {
        FullBaseHub = 0,
        DebugShell = 1
    }

    [Header("Profile")]
    [SerializeField] private PrototypeItemCatalog itemCatalog;
    [SerializeField] private PrototypeMerchantCatalog merchantCatalog;
    [SerializeField] private string raidSceneName = "SampleScene";
    [SerializeField] private int stashSlots = 32;
    [SerializeField] private float stashMaxWeight = 0f;
    [FormerlySerializedAs("loadoutSlots")]
    [SerializeField] private int raidBackpackSlots = 12;
    [FormerlySerializedAs("loadoutMaxWeight")]
    [SerializeField] private float raidBackpackMaxWeight = 20f;
    [SerializeField] private int secureContainerSlots = 4;
    [SerializeField] private float secureContainerMaxWeight = 6f;
    [SerializeField] private int specialEquipmentSlots = 4;
    [SerializeField] private float specialEquipmentMaxWeight = 8f;
    [SerializeField] private bool autoSaveOnInventoryChange = true;
    [SerializeField] private bool uiVisible = true;
    [SerializeField] private bool allowBaseHubEntryButton = true;
    [SerializeField] private List<RaidSceneOption> raidSceneOptions = new List<RaidSceneOption>();
    [SerializeField] private int selectedRaidSceneIndex;
    [SerializeField] private string focusedMerchantId = string.Empty;

    [Header("UI")]
    [SerializeField] private MenuUiMode menuUiMode = MenuUiMode.Ugui;
    [SerializeField] private GameObject uguiViewPrefab;
    [SerializeField] private MetaShellMode shellMode = MetaShellMode.FullBaseHub;

    [Header("Scene Dressing")]
    [SerializeField] private Color stashColor = new Color(0.2f, 0.65f, 0.38f, 1f);
    [FormerlySerializedAs("loadoutColor")]
    [SerializeField] private Color backpackColor = new Color(0.75f, 0.26f, 0.22f, 1f);
    [SerializeField] private Color lockerColor = new Color(0.2f, 0.48f, 0.78f, 1f);
    [FormerlySerializedAs("extractedColor")]
    [SerializeField] private Color protectedColor = new Color(0.82f, 0.64f, 0.18f, 1f);

    internal InventoryContainer stashInventory;
    internal InventoryContainer raidBackpackInventory;
    internal InventoryContainer secureContainerInventory;
    internal InventoryContainer specialEquipmentInventory;
    internal PrototypeProfileService.ProfileData profile;
    internal ItemDefinition cashDefinition;
    internal readonly System.Collections.Generic.List<ItemInstance> weaponLocker = new System.Collections.Generic.List<ItemInstance>();
    internal readonly System.Collections.Generic.List<ArmorInstance> equippedArmor = new System.Collections.Generic.List<ArmorInstance>();
    internal ItemInstance equippedPrimaryWeapon;
    internal ItemInstance equippedSecondaryWeapon;
    internal ItemInstance equippedMeleeWeapon;
    internal MenuPage currentPage;
    internal string feedbackMessage = string.Empty;
    internal float feedbackUntilTime;
    internal GUIStyle titleStyle;
    private GUIStyle sectionStyle;
    private GUIStyle bodyStyle;
    private GUIStyle listStyle;
    private GUIStyle buttonStyle;

    private MetaShellPresenter shellPresenter;
    private MetaLoadoutPresenter loadoutPresenter;
    private MetaInventoryPresenter inventoryPresenter;
    private MetaMerchantPresenter merchantPresenter;
    private PrototypeMainMenuUguiView uguiView;
    private MerchantManager merchantManager;
    private BaseFacilityManager facilityManager;

    internal MenuPage CurrentPage
    {
        get => currentPage;
        set
        {
            value = SanitizePageForShell(value);
            if (currentPage == value)
            {
                return;
            }

            currentPage = value;
            RequestUiRefresh();
        }
    }

    internal InventoryContainer StashInventory => stashInventory;
    internal InventoryContainer RaidBackpackInventory => raidBackpackInventory;
    internal InventoryContainer SecureContainerInventory => secureContainerInventory;
    internal InventoryContainer SpecialEquipmentInventory => specialEquipmentInventory;
    internal int PlayerLevel => profile != null && profile.progression != null
        ? Mathf.Max(1, profile.progression.playerLevel)
        : 1;
    internal int PlayerCurrentExperience => profile != null && profile.progression != null
        ? Mathf.Max(0, profile.progression.currentExperience)
        : 0;
    internal int PlayerExperienceToNextLevel => PrototypePlayerProgressionUtility.GetExperienceToNextLevel(PlayerLevel);
    internal int PlayerLifetimeExperience => profile != null && profile.progression != null
        ? Mathf.Max(0, profile.progression.lifetimeExperience)
        : 0;
    internal int PlayerKillCount => profile != null && profile.progression != null
        ? Mathf.Max(0, profile.progression.killCount)
        : 0;
    internal PrototypeItemCatalog ItemCatalog => itemCatalog;
    internal PrototypeMerchantCatalog MerchantCatalog => merchantCatalog;
    internal string FocusedMerchantId => focusedMerchantId;
    internal ItemDefinition CashDefinition => cashDefinition;
    internal System.Collections.Generic.List<ItemInstance> WeaponLocker => weaponLocker;
    internal System.Collections.Generic.List<ArmorInstance> EquippedArmor => equippedArmor;
    internal WorldStateData ProfileWorldState => profile != null ? profile.worldState : null;
    internal bool IsDebugShellMode => shellMode == MetaShellMode.DebugShell;
    internal bool SupportsWarehousePage => !IsDebugShellMode;
    internal bool SupportsMerchantDirectory => !IsDebugShellMode;
    internal bool HasFacilityManager => facilityManager != null && !IsDebugShellMode;
    internal ItemInstance EquippedPrimaryWeapon
    {
        get => equippedPrimaryWeapon;
        set
        {
            if (ReferenceEquals(equippedPrimaryWeapon, value))
            {
                return;
            }

            equippedPrimaryWeapon = value;
            RequestUiRefresh();
        }
    }

    internal ItemInstance EquippedSecondaryWeapon
    {
        get => equippedSecondaryWeapon;
        set
        {
            if (ReferenceEquals(equippedSecondaryWeapon, value))
            {
                return;
            }

            equippedSecondaryWeapon = value;
            RequestUiRefresh();
        }
    }

    internal ItemInstance EquippedMeleeWeapon
    {
        get => equippedMeleeWeapon;
        set
        {
            if (ReferenceEquals(equippedMeleeWeapon, value))
            {
                return;
            }

            equippedMeleeWeapon = value;
            RequestUiRefresh();
        }
    }

    internal GUIStyle TitleStyle => titleStyle;
    internal GUIStyle SectionStyle => sectionStyle;
    internal GUIStyle BodyStyle => bodyStyle;
    internal GUIStyle ListStyle => listStyle;
    internal GUIStyle ButtonStyle => buttonStyle;
    internal bool IsUiVisible => uiVisible;

    internal Color StashColor => stashColor;
    internal Color BackpackColor => backpackColor;
    internal Color LockerColor => lockerColor;
    internal Color ProtectedColor => protectedColor;
    internal string CurrencyLabel => "现金";

    internal string FeedbackMessage => feedbackMessage;
    internal float FeedbackUntilTime => feedbackUntilTime;
    internal bool UsesImmediateGui => false;

    private void EnsurePresenters()
    {
        shellPresenter ??= new MetaShellPresenter(this);
        loadoutPresenter ??= new MetaLoadoutPresenter(this);
        inventoryPresenter ??= new MetaInventoryPresenter(this);
        merchantPresenter ??= new MetaMerchantPresenter(this);
    }

    private void EnsureRuntimeUi()
    {
        if (menuUiMode != MenuUiMode.Ugui)
        {
            menuUiMode = MenuUiMode.Ugui;
        }

        uguiView ??= GetComponentInChildren<PrototypeMainMenuUguiView>(true);
        if (uguiView == null)
        {
            GameObject prefabAsset = uguiViewPrefab != null
                ? uguiViewPrefab
                : Resources.Load<GameObject>(MenuUiPrefabResourcePath);
            if (prefabAsset != null)
            {
                GameObject instance = Instantiate(prefabAsset, transform);
                instance.name = prefabAsset.name;
                uguiView = instance.GetComponent<PrototypeMainMenuUguiView>();
            }
            else
            {
                uguiView = gameObject.AddComponent<PrototypeMainMenuUguiView>();
            }
        }

        uguiView.Initialize(this);
        uguiView.SetViewVisible(uiVisible);
    }

    internal void RequestUiRefresh()
    {
        uguiView?.RequestRefresh();
    }

    private void Awake()
    {
        EnsurePresenters();
        ResolveCatalog();
        EnsureContainers();
        SanitizeRaidSceneOptions();
        LoadProfileIntoContainers();
        if (uiVisible)
        {
            EnsureMenuCursorState();
        }

        EnsureRuntimeUi();
    }

    private void OnEnable()
    {
        if (uiVisible)
        {
            EnsureMenuCursorState();
        }

        EnsureRuntimeUi();
    }

    private void Update()
    {
        if (!uiVisible)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
        {
            EnsureMenuCursorState();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && uiVisible)
        {
            EnsureMenuCursorState();
        }
    }

    private void OnDisable()
    {
        SaveProfileFromContainers();
    }

    private void OnApplicationQuit()
    {
        SaveProfileFromContainers();
    }

    private void OnValidate()
    {
        stashSlots = Mathf.Max(4, stashSlots);
        stashMaxWeight = Mathf.Max(0f, stashMaxWeight);
        raidBackpackSlots = Mathf.Max(4, raidBackpackSlots);
        raidBackpackMaxWeight = Mathf.Max(0f, raidBackpackMaxWeight);
        secureContainerSlots = Mathf.Max(1, secureContainerSlots);
        secureContainerMaxWeight = Mathf.Max(0f, secureContainerMaxWeight);
        specialEquipmentSlots = Mathf.Max(1, specialEquipmentSlots);
        specialEquipmentMaxWeight = Mathf.Max(0f, specialEquipmentMaxWeight);
        ResolveCatalog();
        SanitizeRaidSceneOptions();
    }

    private void LegacyOnGui()
    {
        if (!uiVisible || menuUiMode != MenuUiMode.ImmediateGui)
        {
            return;
        }

        EnsurePresenters();
        MetaMenuStyleUtility.EnsureStyles(ref titleStyle, ref sectionStyle, ref bodyStyle, ref listStyle, ref buttonStyle);
        shellPresenter.DrawBackground();
        shellPresenter.DrawNavigation();

        if (currentPage == MenuPage.Warehouse)
        {
            inventoryPresenter.DrawWarehousePage();
        }
        else if (currentPage == MenuPage.Merchants)
        {
            merchantPresenter.DrawMerchantsPage();
        }
        else
        {
            loadoutPresenter.DrawHomePageCompact();
        }

        shellPresenter.DrawFooter();
    }

    internal int GetAvailableFunds()
    {
        return profile != null ? Mathf.Max(0, profile.funds) : 0;
    }

    internal string GetCurrencyLabel()
    {
        return "现金"; /*
            ? cashDefinition.DisplayName
            : "现金";
        */
    }

    internal string BuildPlayerProgressionSummaryText()
    {
        return $"Level: {PlayerLevel}\nXP: {PlayerCurrentExperience}/{PlayerExperienceToNextLevel}\nLifetime XP: {PlayerLifetimeExperience}  Kills: {PlayerKillCount}";
    }

    internal string BuildPlayerAttributeSummaryText()
    {
        int level = PlayerLevel;
        int vitality = PrototypePlayerProgressionUtility.GetVitality(level);
        int endurance = PrototypePlayerProgressionUtility.GetEndurance(level);
        int combat = PrototypePlayerProgressionUtility.GetCombat(level);
        int medicine = PrototypePlayerProgressionUtility.GetMedicine(level);
        float bonusHealth = PrototypePlayerProgressionUtility.GetHealthBonus(level);
        float bonusStamina = PrototypePlayerProgressionUtility.GetStaminaBonus(level);
        float damageBonusPercent = (PrototypePlayerProgressionUtility.GetDamageMultiplier(level) - 1f) * 100f;
        float healingBonusPercent = (PrototypePlayerProgressionUtility.GetHealingMultiplier(level) - 1f) * 100f;

        var builder = new StringBuilder(160);
        builder.Append($"Vitality {vitality}  Endurance {endurance}\n");
        builder.Append($"Combat {combat}  Medicine {medicine}\n");
        builder.Append($"Health +{Mathf.RoundToInt(bonusHealth)}  Stamina +{Mathf.RoundToInt(bonusStamina)}\n");
        builder.Append($"Damage +{damageBonusPercent:0.#}%  Healing +{healingBonusPercent:0.#}%");
        return builder.ToString();
    }

    internal bool CanReceiveFunds(int amount)
    {
        return amount > 0 && profile != null;
    }

    internal bool TryAddFunds(int amount)
    {
        if (amount <= 0 || profile == null)
        {
            return false;
        }

        long updatedFunds = (long)Mathf.Max(0, profile.funds) + amount;
        profile.funds = updatedFunds > int.MaxValue ? int.MaxValue : (int)updatedFunds;
        return true;
    }

    internal bool TrySpendFunds(int amount, string failureMessage)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (profile == null || GetAvailableFunds() < amount)
        {
            SetFeedback(failureMessage);
            return false;
        }

        profile.funds -= amount;
        return true;
    }

    internal void BeginPanel(Rect rect, string title, Color accent, string subtitle)
    {
        GUI.Box(rect, string.Empty, sectionStyle);

        Color previousColor = GUI.color;
        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x + 16f, rect.y + 18f, 72f, 4f), Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUILayout.BeginArea(new Rect(rect.x + 16f, rect.y + 18f, rect.width - 32f, rect.height - 36f));
        GUILayout.Label(title, sectionStyle);
        GUILayout.Label(subtitle, bodyStyle);
        GUILayout.Space(10f);
    }

    internal void EndPanel()
    {
        GUILayout.EndArea();
    }

    internal void StartRaid()
    {
        SaveProfileFromContainers();
        MetaEntryRouter.EnterRaid(GetSelectedRaidSceneName());
    }

    internal void EnterBaseHub()
    {
        SaveProfileFromContainers();
        MetaEntryRouter.EnterBaseHub();
    }

    internal bool ShouldShowBaseHubEntry()
    {
        return allowBaseHubEntryButton && MetaEntryRouter.IsDebugEntryEnabled;
    }

    private InventoryContainer EnsureContainer(InventoryContainer existing, string objectName, string label, int slots, float maxWeight)
    {
        if (existing == null)
        {
            existing = CreateRuntimeContainer(objectName, label, slots, maxWeight);
        }
        else
        {
            existing.Configure(label, slots, maxWeight);
        }

        return existing;
    }

    private InventoryContainer CreateRuntimeContainer(string objectName, string label, int slots, float maxWeight)
    {
        Transform child = transform.Find(objectName);
        GameObject containerObject = child != null ? child.gameObject : new GameObject(objectName);
        containerObject.transform.SetParent(transform, false);
        containerObject.hideFlags = HideFlags.HideInHierarchy;

        InventoryContainer inventory = containerObject.GetComponent<InventoryContainer>();
        if (inventory == null)
        {
            inventory = containerObject.AddComponent<InventoryContainer>();
        }

        inventory.Configure(label, slots, maxWeight);
        return inventory;
    }

    private void ApplyProfileToContainers(PrototypeProfileService.ProfileData sourceProfile)
    {
        EnsureContainers();
        PrototypeProfileService.PopulateInventoryInstances(stashInventory, sourceProfile != null ? sourceProfile.stashItemInstances : null, itemCatalog);
        PrototypeProfileService.PopulateInventoryInstances(raidBackpackInventory, sourceProfile != null ? sourceProfile.raidBackpackItemInstances : null, itemCatalog);
        PrototypeProfileService.PopulateInventoryInstances(secureContainerInventory, sourceProfile != null ? sourceProfile.secureContainerItemInstances : null, itemCatalog);
        PrototypeProfileService.PopulateInventoryInstances(specialEquipmentInventory, sourceProfile != null ? sourceProfile.specialEquipmentItemInstances : null, itemCatalog);

        weaponLocker.Clear();
        equippedArmor.Clear();
        equippedPrimaryWeapon = null;
        equippedSecondaryWeapon = null;
        equippedMeleeWeapon = null;

        if (sourceProfile == null || itemCatalog == null)
        {
            return;
        }

        if (sourceProfile.stashWeaponInstances != null)
        {
            weaponLocker.AddRange(PrototypeProfileService.ResolveWeaponInstances(sourceProfile.stashWeaponInstances, itemCatalog));
        }

        equippedArmor.AddRange(PrototypeProfileService.ResolveArmorInstances(sourceProfile.equippedArmorInstances, itemCatalog));
        equippedPrimaryWeapon = PrototypeProfileService.ResolveWeaponInstance(sourceProfile.equippedPrimaryWeaponInstance, itemCatalog);
        equippedSecondaryWeapon = PrototypeProfileService.ResolveWeaponInstance(sourceProfile.equippedSecondaryWeaponInstance, itemCatalog);
        equippedMeleeWeapon = PrototypeProfileService.ResolveWeaponInstance(sourceProfile.equippedMeleeWeaponInstance, itemCatalog);
    }

    internal void AutoSaveIfNeeded()
    {
        if (autoSaveOnInventoryChange)
        {
            SaveProfileFromContainers();
            return;
        }

        RequestUiRefresh();
    }

    internal int GetInventoryStackCount(InventoryContainer inventory)
    {
        return inventory != null ? inventory.OccupiedSlots : 0;
    }

    internal static string BuildItemInstanceDetail(ItemInstance item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        string detail;
        if (item.IsWeapon && item.WeaponDefinition != null)
        {
            detail = BuildWeaponDetail(item, item.WeaponDefinition);
        }
        else if (PrototypeRaidInventoryRules.TryGetSecureContainerSpec(item.DefinitionBase, out PrototypeRaidSecureContainerSpec secureSpec))
        {
            detail = $"Secure {secureSpec.SlotCount} slots  Capacity {secureSpec.MaxWeight:0.0}";
        }
        else if (item.Definition is ArmorDefinition armorDefinition)
        {
            detail = BuildArmorDetail(item, armorDefinition);
        }
        else if (item.Definition is MedicalItemDefinition medicalDefinition)
        {
            detail = BuildMedicalDetail(item, medicalDefinition);
        }
        else if (item.Definition is AmmoDefinition ammoDefinition)
        {
            detail = BuildAmmoDetail(item, ammoDefinition);
        }
        else
        {
            detail = $"重量 {item.TotalWeight:0.00}";
        }

        string affixSummary = ItemAffixUtility.BuildAffixSummaryRich(item.Affixes);
        if (!string.IsNullOrWhiteSpace(affixSummary))
        {
            detail = string.IsNullOrWhiteSpace(detail) ? affixSummary : $"{detail}\n{affixSummary}";
        }

        string skillSummary = ItemSkillUtility.BuildSkillSummaryRich(item.Skills);
        if (!string.IsNullOrWhiteSpace(skillSummary))
        {
            detail = string.IsNullOrWhiteSpace(detail) ? skillSummary : $"{detail}\n{skillSummary}";
        }

        return detail;
    }

    private static string BuildWeaponDetail(ItemInstance item, PrototypeWeaponDefinition weaponDefinition)
    {
        if (item == null || weaponDefinition == null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (weaponDefinition.IsThrowableWeapon)
        {
            parts.Add("投掷");
            parts.Add($"爆炸 {weaponDefinition.ExplosionDamage:0}");
            parts.Add($"半径 {weaponDefinition.ExplosionRadius:0.0}m");
        }
        else if (weaponDefinition.IsMeleeWeapon)
        {
            parts.Add("近战");
            parts.Add($"伤害 {weaponDefinition.MeleeDamage:0}");
            parts.Add($"穿深 {weaponDefinition.PenetrationPower:0}");
        }
        else
        {
            AmmoDefinition ammoDefinition = weaponDefinition.AmmoDefinition;
            float ammoMultiplier = ammoDefinition != null ? ammoDefinition.DamageMultiplier : 1f;
            float finalDamage = weaponDefinition.FirearmDamage * ammoMultiplier;
            float penetration = ammoDefinition != null ? ammoDefinition.PenetrationPower : weaponDefinition.PenetrationPower;
            parts.Add($"弹药 {item.MagazineAmmo}/{weaponDefinition.MagazineSize}");
            parts.Add($"武器伤害 {weaponDefinition.FirearmDamage:0}");
            parts.Add($"子弹倍率 {ammoMultiplier:0.00}x");
            parts.Add($"最终伤害 {finalDamage:0}");
            parts.Add($"穿深 {penetration:0}");
        }

        parts.Add($"重量 {item.TotalWeight:0.00}");
        return string.Join("  ", parts);
    }

    private static string BuildArmorDetail(ItemInstance item, ArmorDefinition armorDefinition)
    {
        var parts = new List<string>
        {
            $"耐久 {item.CurrentDurability:0.0}/{armorDefinition.MaxDurability:0.0}",
            $"额外生命 +{armorDefinition.BonusHealth:0}"
        };

        if (armorDefinition.CoveredPartIds != null && armorDefinition.CoveredPartIds.Count > 0)
        {
            parts.Add($"覆盖 {string.Join("/", armorDefinition.CoveredPartIds)}");
        }

        parts.Add($"重量 {item.TotalWeight:0.00}");
        return string.Join("  ", parts);
    }

    private static string BuildMedicalDetail(ItemInstance item, MedicalItemDefinition medicalDefinition)
    {
        var parts = new List<string>();
        if (medicalDefinition.HealAmount > 0f)
        {
            parts.Add($"治疗 {medicalDefinition.HealAmount:0}");
        }

        if (medicalDefinition.HealPercent > 0f)
        {
            parts.Add($"治疗 {medicalDefinition.HealPercent * 100f:0}%最大生命");
        }

        if (medicalDefinition.RemovesLightBleeds > 0)
        {
            parts.Add($"止轻出血 x{medicalDefinition.RemovesLightBleeds}");
        }

        if (medicalDefinition.RemovesHeavyBleeds > 0)
        {
            parts.Add($"止重出血 x{medicalDefinition.RemovesHeavyBleeds}");
        }

        if (medicalDefinition.CuresFractures > 0)
        {
            parts.Add($"治骨折 x{medicalDefinition.CuresFractures}");
        }

        if (medicalDefinition.PainkillerDuration > 0f)
        {
            parts.Add($"止痛 {medicalDefinition.PainkillerDuration:0}s");
        }

        parts.Add($"重量 {item.TotalWeight:0.00}");
        return string.Join("  ", parts);
    }

    private static string BuildAmmoDetail(ItemInstance item, AmmoDefinition ammoDefinition)
    {
        return string.Join("  ", new[]
        {
            $"伤害倍率 {ammoDefinition.DamageMultiplier:0.00}x",
            $"穿深 {ammoDefinition.PenetrationPower:0}",
            $"护甲伤害 {ammoDefinition.ArmorDamage:0}",
            $"重量 {item.TotalWeight:0.00}"
        });
    }

    internal MerchantData GetMerchantData(string merchantId)
    {
        ResolveCatalog();
        EnsureRuntimeStateInitialized();
        PrototypeMerchantCatalog.MerchantDefinition merchant = merchantCatalog != null ? merchantCatalog.GetMerchantById(merchantId) : null;
        return merchantManager != null
            ? merchantManager.GetMerchantData(merchantId, merchant != null ? merchant.MerchantLevel : MerchantData.MinLevel)
            : null;
    }

    internal MerchantManager.SupplyRequest GetMerchantSupplyRequest(string merchantId)
    {
        EnsureRuntimeStateInitialized();
        return merchantManager != null ? merchantManager.GetSupplyRequest(merchantId) : default;
    }

    internal string BuildMerchantPanelSubtitle(PrototypeMerchantCatalog.MerchantDefinition merchant, bool isFocusedMerchant)
    {
        MerchantData data = GetMerchantData(merchant != null ? merchant.MerchantId : null);
        int level = data != null ? data.Level : (merchant != null ? merchant.MerchantLevel : MerchantData.MinLevel);
        string focusPrefix = isFocusedMerchant ? "当前交互  ·  " : string.Empty;
        string reputationLabel = data != null ? GetReputationDisplayName(data.Reputation) : GetReputationDisplayName(ReputationLevel.Neutral);
        return $"{focusPrefix}等级 {level}  ·  信誉 {reputationLabel}  ·  资金 {GetAvailableFunds()} {GetCurrencyLabel()}";
    }

    internal string BuildMerchantProgressDetail(PrototypeMerchantCatalog.MerchantDefinition merchant)
    {
        MerchantData data = GetMerchantData(merchant != null ? merchant.MerchantId : null);
        if (merchant == null || data == null)
        {
            return string.Empty;
        }

        string tradeProgress = data.Level >= MerchantData.MaxLevel
            ? "已达到最高库存等级"
            : $"交易进度 {data.GetTradeProgressIntoCurrentLevel():0}/{data.GetLevelUpRequirement():0}";
        string reputationProgress = data.Reputation == ReputationLevel.Revered
            ? $"信誉 {GetReputationDisplayName(data.Reputation)} · 已达到最高折扣"
            : $"信誉进度 {data.GetReputationProgressIntoCurrentTier():0}/{data.GetReputationRequirementForNextTier():0}";
        int discountPercent = Mathf.RoundToInt((1f - data.GetPriceMultiplier()) * 100f);
        return
            $"库存等级 Lv.{data.Level}  ·  起始等级 Lv.{data.StartingLevel}\n" +
            $"{tradeProgress}\n" +
            $"{reputationProgress}\n" +
            $"当前折扣 {discountPercent}%";
    }

    internal string BuildMerchantSupplyRequestText(string merchantId)
    {
        MerchantManager.SupplyRequest request = GetMerchantSupplyRequest(merchantId);
        if (!request.IsValid)
        {
            return string.Empty;
        }

        int availableCount = CountItemInStorage(request.RequestedItemId);
        string itemLabel = GetItemDisplayName(request.RequestedItemId);
        return $"{request.Title}\n提交 {itemLabel} x{request.RequestedQuantity}\n仓库库存 {availableCount}/{request.RequestedQuantity}  ·  奖励信誉 +{request.ReputationReward}";
    }

    internal bool TryCompleteMerchantSupplyRequest(string merchantId)
    {
        MerchantManager.SupplyRequest request = GetMerchantSupplyRequest(merchantId);
        if (!request.IsValid)
        {
            SetFeedback("当前没有可交付的商人委托。");
            return false;
        }

        if (!TryConsumeStorageItem(request.RequestedItemId, request.RequestedQuantity))
        {
            string itemLabel = GetItemDisplayName(request.RequestedItemId);
            SetFeedback($"仓库库存不足，无法交付 {itemLabel} x{request.RequestedQuantity}。");
            return false;
        }

        EnsureRuntimeStateInitialized();
        MerchantManager.TradeUpdateResult result = merchantManager != null
            ? merchantManager.AddReputation(request.MerchantId, request.ReputationReward)
            : default;
        RefreshMerchantRuntimeInventories(false);

        MerchantData data = GetMerchantData(request.MerchantId);
        string reputationLabel = data != null ? GetReputationDisplayName(data.Reputation) : GetReputationDisplayName(ReputationLevel.Neutral);
        string levelUpSuffix = result.ReputationChanged ? $" 信誉提升至 {reputationLabel}。" : "。";
        SetFeedback($"已完成委托“{request.Title}”，获得信誉 +{request.ReputationReward}。{levelUpSuffix}".Trim());
        AutoSaveIfNeeded();
        return true;
    }

    internal int GetSellPrice(ItemInstance instance)
    {
        ResolveCatalog();
        if (instance == null || !instance.IsDefined() || merchantCatalog == null)
        {
            return 0;
        }

        int basePrice = merchantCatalog.GetSellPrice(instance);
        return Mathf.Max(0, Mathf.RoundToInt(basePrice * GetWorkbenchSellBonusMultiplier()));
    }

    internal void RecordMerchantTrade(string merchantId, int amount)
    {
        if (amount <= 0 || string.IsNullOrWhiteSpace(merchantId))
        {
            return;
        }

        EnsureRuntimeStateInitialized();
        MerchantManager.TradeUpdateResult result = merchantManager != null
            ? merchantManager.RecordTrade(merchantId, amount)
            : default;
        if (result.LevelChanged)
        {
            RefreshMerchantRuntimeInventories(true);
        }
        else
        {
            RefreshMerchantRuntimeInventories(false);
        }
    }

    internal void RecordFocusedMerchantTrade(int amount)
    {
        RecordMerchantTrade(focusedMerchantId, amount);
    }

    internal IReadOnlyList<FacilityData> GetFacilities()
    {
        return facilityManager != null ? facilityManager.GetFacilities() : Array.Empty<FacilityData>();
    }

    internal bool TryUpgradeFacility(FacilityType type)
    {
        return facilityManager != null && facilityManager.UpgradeFacility(type);
    }

    internal string GetFacilityDisplayName(FacilityType type)
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

    internal string BuildFacilityDetail(FacilityData facility)
    {
        if (facility == null)
        {
            return string.Empty;
        }

        string upgradeText = facility.CanUpgrade()
            ? $"下一级花费 {facility.GetUpgradeCost()} {GetCurrencyLabel()}"
            : "已达到最高等级";
        string effectSummary = facilityManager != null ? facilityManager.BuildFacilityEffectSummary(facility.type) : string.Empty;
        return $"等级 Lv.{facility.Level}/{facility.MaxLevelValue}\n{effectSummary}\n{upgradeText}";
    }

    internal int GetWeaponLockerCapacity()
    {
        return facilityManager != null ? facilityManager.GetWeaponLockerCapacity() : 6;
    }

    internal bool TryAddWeaponToLocker(ItemInstance weaponInstance, string failureMessage)
    {
        if (weaponInstance == null || weaponInstance.WeaponDefinition == null)
        {
            return false;
        }

        int capacity = Mathf.Max(1, GetWeaponLockerCapacity());
        if (weaponLocker.Count >= capacity)
        {
            SetFeedback(failureMessage);
            return false;
        }

        weaponLocker.Add(weaponInstance);
        RequestUiRefresh();
        return true;
    }

    internal float GetWorkbenchSellBonusMultiplier()
    {
        return facilityManager != null ? facilityManager.GetWorkbenchSellBonusMultiplier() : 1f;
    }

    internal void RegisterFacilityManager(BaseFacilityManager manager)
    {
        facilityManager = manager;
        ApplyFacilityManagerRuntime(manager);
    }

    internal void ApplyFacilityManagerRuntime(BaseFacilityManager manager)
    {
        facilityManager = manager;
        if (stashInventory != null)
        {
            int effectiveSlots = facilityManager != null ? facilityManager.GetWarehouseSlotCapacity(stashSlots) : stashSlots;
            float effectiveWeight = facilityManager != null ? facilityManager.GetWarehouseWeightCapacity(stashMaxWeight) : stashMaxWeight;
            stashInventory.Configure("仓库", effectiveSlots, effectiveWeight);
        }

        RequestUiRefresh();
    }

    private void EnsureRuntimeStateInitialized()
    {
        if (profile == null)
        {
            return;
        }

        profile.worldState ??= new WorldStateData();
        merchantManager ??= new MerchantManager(profile.worldState);
        merchantManager.EnsureMerchantsFromCatalog(merchantCatalog);
    }

    private void RefreshMerchantRuntimeInventories(bool forceRegenerate)
    {
        if (merchantCatalog == null)
        {
            return;
        }

        if (forceRegenerate)
        {
            merchantCatalog.RegenerateRuntimeInventories(
                merchant => merchantManager != null ? merchantManager.GetEffectiveMerchantLevel(merchant) : merchant.MerchantLevel,
                merchant => merchantManager != null ? merchantManager.GetPriceMultiplier(merchant.MerchantId) : 1f);
            return;
        }

        merchantCatalog.EnsureRuntimeInventories(
            merchant => merchantManager != null ? merchantManager.GetEffectiveMerchantLevel(merchant) : merchant.MerchantLevel,
            merchant => merchantManager != null ? merchantManager.GetPriceMultiplier(merchant.MerchantId) : 1f);
    }

    private void ResolveCatalog()
    {
        if (itemCatalog == null)
        {
            itemCatalog = Resources.Load<PrototypeItemCatalog>("PrototypeItemCatalog");
        }

        if (merchantCatalog == null)
        {
            merchantCatalog = Resources.Load<PrototypeMerchantCatalog>("PrototypeMerchantCatalog");
        }

        if (merchantCatalog == null && itemCatalog != null)
        {
            merchantCatalog = MetaMerchantPresenter.CreateRuntimeMerchantCatalog(itemCatalog);
        }

        cashDefinition = itemCatalog != null ? itemCatalog.FindByItemId("cash_bundle") : null;
        if (merchantCatalog != null)
        {
            RefreshMerchantRuntimeInventories(false);
        }
    }

    private void EnsureContainers()
    {
        stashInventory = EnsureContainer(stashInventory, "Profile_Stash", "仓库", stashSlots, stashMaxWeight);
        raidBackpackInventory = EnsureContainer(raidBackpackInventory, "Profile_RaidBackpack", "战局背包", raidBackpackSlots, raidBackpackMaxWeight);
        secureContainerInventory = EnsureContainer(secureContainerInventory, "Profile_SecureContainer", "安全箱", secureContainerSlots, secureContainerMaxWeight);
        specialEquipmentInventory = EnsureContainer(specialEquipmentInventory, "Profile_SpecialEquipment", "特殊装备", specialEquipmentSlots, specialEquipmentMaxWeight);
        ApplyFacilityManagerRuntime(facilityManager);
    }

    private void LoadProfileIntoContainers()
    {
        profile = PrototypeProfileService.LoadProfile(itemCatalog);
        merchantManager = null;
        EnsureRuntimeStateInitialized();
        ApplyProfileToContainers(profile);
        ApplyFacilityManagerRuntime(facilityManager);
        RefreshMerchantRuntimeInventories(false);
        RequestUiRefresh();
    }

    internal void SaveProfileFromContainers()
    {
        ResolveCatalog();
        EnsureContainers();

        if (profile == null)
        {
            profile = new PrototypeProfileService.ProfileData();
        }

        profile.worldState ??= new WorldStateData();
        EnsureRuntimeStateInitialized();

        DepositStashCurrencyIntoFunds();

        profile.stashItemInstances = PrototypeProfileService.CaptureInventoryInstances(stashInventory);
        profile.raidBackpackItemInstances = PrototypeProfileService.CaptureInventoryInstances(raidBackpackInventory);
        profile.secureContainerItemInstances = PrototypeProfileService.CaptureInventoryInstances(secureContainerInventory);
        profile.specialEquipmentItemInstances = PrototypeProfileService.CaptureInventoryInstances(specialEquipmentInventory);
        profile.equippedArmorInstances = PrototypeProfileService.CaptureArmorInstances(equippedArmor);
        profile.stashWeaponInstances = PrototypeProfileService.CaptureWeaponInstances(weaponLocker);
        profile.raidBackpackWeaponInstances.Clear();
        profile.equippedPrimaryWeaponInstance = PrototypeProfileService.CaptureWeaponInstance(equippedPrimaryWeapon);
        profile.equippedSecondaryWeaponInstance = PrototypeProfileService.CaptureWeaponInstance(equippedSecondaryWeapon);
        profile.equippedMeleeWeaponInstance = PrototypeProfileService.CaptureWeaponInstance(equippedMeleeWeapon);

        profile.stashItems = PrototypeProfileService.CaptureInventory(stashInventory);
        profile.raidBackpackItems = PrototypeProfileService.CaptureInventory(raidBackpackInventory);
        profile.secureContainerItems = PrototypeProfileService.CaptureInventory(secureContainerInventory);
        profile.specialEquipmentItems = PrototypeProfileService.CaptureInventory(specialEquipmentInventory);
        profile.equippedArmorItems = PrototypeProfileService.CaptureArmorDefinitions(equippedArmor);
        profile.stashWeaponIds = PrototypeProfileService.CaptureWeaponIds(weaponLocker);
        profile.equippedPrimaryWeaponId = equippedPrimaryWeapon != null && equippedPrimaryWeapon.WeaponDefinition != null ? equippedPrimaryWeapon.WeaponDefinition.WeaponId : string.Empty;
        profile.equippedSecondaryWeaponId = equippedSecondaryWeapon != null && equippedSecondaryWeapon.WeaponDefinition != null ? equippedSecondaryWeapon.WeaponDefinition.WeaponId : string.Empty;
        profile.equippedMeleeWeaponId = equippedMeleeWeapon != null && equippedMeleeWeapon.WeaponDefinition != null ? equippedMeleeWeapon.WeaponDefinition.WeaponId : string.Empty;
        profile.loadoutItems.Clear();
        profile.extractedItems.Clear();
        PrototypeProfileService.SaveProfile(profile, itemCatalog);
        RequestUiRefresh();
    }

    private void DepositStashCurrencyIntoFunds()
    {
        if (profile == null || stashInventory == null || cashDefinition == null)
        {
            return;
        }

        for (int index = stashInventory.Items.Count - 1; index >= 0; index--)
        {
            ItemInstance item = stashInventory.Items[index];
            if (item == null || !item.IsDefined() || item.Definition != cashDefinition)
            {
                continue;
            }

            int quantity = item.Quantity;
            if (!stashInventory.TryExtractItem(index, quantity, out ItemInstance extractedItem) || extractedItem == null || !extractedItem.IsDefined())
            {
                continue;
            }

            if (!TryAddFunds(extractedItem.Quantity))
            {
                stashInventory.TryAddItemInstance(extractedItem);
            }
        }
    }

    internal void ResetProfile()
    {
        profile = PrototypeProfileService.CreateDefaultProfile(itemCatalog);
        merchantManager = null;
        EnsureRuntimeStateInitialized();
        ApplyProfileToContainers(profile);
        ApplyFacilityManagerRuntime(facilityManager);
        RefreshMerchantRuntimeInventories(true);
        SaveProfileFromContainers();
        SetFeedback("档案已重置为默认配置。");
    }

    internal void SetFeedback(string message)
    {
        feedbackMessage = message ?? string.Empty;
        feedbackUntilTime = Time.time + 2.6f;
        RequestUiRefresh();
    }

    internal void ShowMerchantDirectory()
    {
        if (!SupportsMerchantDirectory)
        {
            SetFeedback("启动壳仅保留跳转入口，请进入基地后使用商人目录。");
            ShowPage(MenuPage.Home);
            return;
        }

        focusedMerchantId = string.Empty;
        ShowPage(MenuPage.Merchants);
    }

    internal bool ShowMerchant(string merchantId, string merchantDisplayName = null)
    {
        if (!SupportsMerchantDirectory)
        {
            SetFeedback("启动壳不承载正式商人业务，请进入基地后交互。");
            ShowPage(MenuPage.Home);
            return false;
        }

        if (!TryFocusMerchant(merchantId))
        {
            ShowMerchantDirectory();
            if (!string.IsNullOrWhiteSpace(merchantDisplayName))
            {
                SetFeedback($"未找到商人：{merchantDisplayName}");
            }

            return false;
        }

        ShowPage(MenuPage.Merchants);
        return true;
    }

    internal bool TryFocusMerchant(string merchantId)
    {
        ResolveCatalog();
        if (merchantCatalog == null || string.IsNullOrWhiteSpace(merchantId))
        {
            focusedMerchantId = string.Empty;
            return false;
        }

        PrototypeMerchantCatalog.MerchantDefinition merchant = merchantCatalog.GetMerchantById(merchantId);
        if (merchant == null)
        {
            focusedMerchantId = string.Empty;
            return false;
        }

        focusedMerchantId = merchant.MerchantId;
        return true;
    }

    internal PrototypeMerchantCatalog.MerchantDefinition GetFocusedMerchant()
    {
        ResolveCatalog();
        return merchantCatalog != null ? merchantCatalog.GetMerchantById(focusedMerchantId) : null;
    }

    internal int GetFocusedMerchantIndex()
    {
        ResolveCatalog();
        return merchantCatalog != null ? merchantCatalog.GetMerchantIndex(focusedMerchantId) : -1;
    }

    internal bool IsMerchantFocused(string merchantId)
    {
        return !string.IsNullOrWhiteSpace(focusedMerchantId)
            && !string.IsNullOrWhiteSpace(merchantId)
            && string.Equals(focusedMerchantId, merchantId.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    internal void ShowPage(MenuPage page)
    {
        currentPage = SanitizePageForShell(page);
        uiVisible = true;
        EnsureMenuCursorState();
        EnsureRuntimeUi();
        RequestUiRefresh();
    }

    internal void HideUi()
    {
        uiVisible = false;
        uguiView?.SetViewVisible(false);
    }

    internal int GetRaidSceneOptionCount()
    {
        SanitizeRaidSceneOptions();
        return raidSceneOptions.Count;
    }

    internal int GetSelectedRaidSceneIndex()
    {
        SanitizeRaidSceneOptions();
        return selectedRaidSceneIndex;
    }

    internal void SelectRaidScene(int index)
    {
        SanitizeRaidSceneOptions();
        if (raidSceneOptions.Count == 0)
        {
            selectedRaidSceneIndex = 0;
            RequestUiRefresh();
            return;
        }

        selectedRaidSceneIndex = Mathf.Clamp(index, 0, raidSceneOptions.Count - 1);
        RequestUiRefresh();
    }

    internal string GetRaidSceneOptionDisplayName(int index)
    {
        RaidSceneOption option = GetRaidSceneOption(index);
        return option != null ? option.displayName : "未配置";
    }

    internal string GetRaidSceneOptionDescription(int index)
    {
        RaidSceneOption option = GetRaidSceneOption(index);
        return option != null ? option.description : "当前没有配置可用的出击地图。";
    }

    internal string GetSelectedRaidSceneDisplayName()
    {
        return GetRaidSceneOptionDisplayName(GetSelectedRaidSceneIndex());
    }

    internal string GetSelectedRaidSceneDescription()
    {
        return GetRaidSceneOptionDescription(GetSelectedRaidSceneIndex());
    }

    internal string BuildHomePageSummaryText()
    {
        string summary =
            $"已选地图：{GetSelectedRaidSceneDisplayName()}\n" +
            $"资金：{GetAvailableFunds()} {GetCurrencyLabel()}\n" +
            $"仓库：物品堆叠 {GetInventoryStackCount(StashInventory)}  武器 {WeaponLocker.Count}\n" +
            $"战局背包：{GetInventoryStackCount(RaidBackpackInventory)}  安全箱：{GetInventoryStackCount(SecureContainerInventory)}\n" +
            $"特殊装备：{GetInventoryStackCount(SpecialEquipmentInventory)}  护甲：{EquippedArmor.Count}\n" +
            $"近战：{(EquippedMeleeWeapon != null ? EquippedMeleeWeapon.DisplayName : "空")}\n" +
            $"主武器：{(EquippedPrimaryWeapon != null ? EquippedPrimaryWeapon.DisplayName : "空")}\n" +
            $"副武器：{(EquippedSecondaryWeapon != null ? EquippedSecondaryWeapon.DisplayName : "空")}";

        if (facilityManager != null && !IsDebugShellMode)
        {
            summary += $"\n设施：仓库 {GetFacilityLevel(FacilityType.Warehouse)} / 武器库 {GetFacilityLevel(FacilityType.Armory)} / 医疗站 {GetFacilityLevel(FacilityType.MedicalStation)} / 工作台 {GetFacilityLevel(FacilityType.Workbench)}";
        }

        return summary;
    }

    private int GetFacilityLevel(FacilityType type)
    {
        IReadOnlyList<FacilityData> facilities = GetFacilities();
        for (int index = 0; index < facilities.Count; index++)
        {
            FacilityData facility = facilities[index];
            if (facility != null && facility.type == type)
            {
                return facility.Level;
            }
        }

        return FacilityData.MinLevel;
    }

    private MenuPage SanitizePageForShell(MenuPage page)
    {
        if (page == MenuPage.Warehouse && !SupportsWarehousePage)
        {
            return MenuPage.Home;
        }

        if (page == MenuPage.Merchants && !SupportsMerchantDirectory)
        {
            return MenuPage.Home;
        }

        return page;
    }

    private int CountItemInStorage(string itemId)
    {
        return CountItemInInventory(stashInventory, itemId);
    }

    private static int CountItemInInventory(InventoryContainer inventory, string itemId)
    {
        if (inventory == null || inventory.Items == null || string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        int total = 0;
        string sanitizedItemId = itemId.Trim();
        for (int index = 0; index < inventory.Items.Count; index++)
        {
            ItemInstance item = inventory.Items[index];
            if (item == null || !item.IsDefined() || item.IsWeapon || item.Definition == null)
            {
                continue;
            }

            if (string.Equals(item.Definition.ItemId, sanitizedItemId, StringComparison.OrdinalIgnoreCase))
            {
                total += Mathf.Max(0, item.Quantity);
            }
        }

        return total;
    }

    private bool TryConsumeStorageItem(string itemId, int quantity)
    {
        return TryConsumeInventoryItem(stashInventory, itemId, quantity);
    }

    private static bool TryConsumeInventoryItem(InventoryContainer inventory, string itemId, int quantity)
    {
        if (inventory == null || quantity <= 0 || string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        string sanitizedItemId = itemId.Trim();
        if (CountItemInInventory(inventory, sanitizedItemId) < quantity)
        {
            return false;
        }

        int remaining = quantity;
        for (int index = inventory.Items.Count - 1; index >= 0 && remaining > 0; index--)
        {
            ItemInstance item = inventory.Items[index];
            if (item == null || !item.IsDefined() || item.IsWeapon || item.Definition == null)
            {
                continue;
            }

            if (!string.Equals(item.Definition.ItemId, sanitizedItemId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int extractQuantity = Mathf.Min(remaining, Mathf.Max(0, item.Quantity));
            if (extractQuantity <= 0 || !inventory.TryExtractItem(index, extractQuantity, out _))
            {
                continue;
            }

            remaining -= extractQuantity;
        }

        return remaining <= 0;
    }

    private string GetItemDisplayName(string itemId)
    {
        if (itemCatalog == null || string.IsNullOrWhiteSpace(itemId))
        {
            return "物资";
        }

        ItemDefinition definition = itemCatalog.FindByItemId(itemId.Trim());
        return definition != null ? definition.DisplayName : itemId.Trim();
    }

    private static string GetReputationDisplayName(ReputationLevel reputationLevel)
    {
        switch (reputationLevel)
        {
            case ReputationLevel.Friendly:
                return "友好";

            case ReputationLevel.Honored:
                return "尊敬";

            case ReputationLevel.Revered:
                return "崇敬";

            default:
                return "中立";
        }
    }

    private static void EnsureMenuCursorState()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private string GetSelectedRaidSceneName()
    {
        RaidSceneOption option = GetRaidSceneOption(GetSelectedRaidSceneIndex());
        if (option != null && !string.IsNullOrWhiteSpace(option.sceneName))
        {
            return option.sceneName;
        }

        return string.IsNullOrWhiteSpace(raidSceneName) ? "SampleScene" : raidSceneName.Trim();
    }

    private RaidSceneOption GetRaidSceneOption(int index)
    {
        SanitizeRaidSceneOptions();
        return index >= 0 && index < raidSceneOptions.Count ? raidSceneOptions[index] : null;
    }

    private void SanitizeRaidSceneOptions()
    {
        string fallbackSceneName = string.IsNullOrWhiteSpace(raidSceneName) ? "SampleScene" : raidSceneName.Trim();
        raidSceneOptions ??= new List<RaidSceneOption>();

        for (int index = raidSceneOptions.Count - 1; index >= 0; index--)
        {
            RaidSceneOption option = raidSceneOptions[index];
            if (option == null)
            {
                raidSceneOptions.RemoveAt(index);
                continue;
            }

            option.Sanitize(fallbackSceneName, index);
        }

        if (raidSceneOptions.Count == 0)
        {
            raidSceneOptions.Add(new RaidSceneOption
            {
                displayName = "原型战区",
                sceneName = fallbackSceneName,
                description = "原型室内战斗区域。"
            });
        }

        selectedRaidSceneIndex = Mathf.Clamp(selectedRaidSceneIndex, 0, raidSceneOptions.Count - 1);
    }
}
