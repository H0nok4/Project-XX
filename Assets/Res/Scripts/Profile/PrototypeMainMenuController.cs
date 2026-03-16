using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PrototypeMainMenuController : MonoBehaviour
{
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

    internal MenuPage CurrentPage
    {
        get => currentPage;
        set => currentPage = value;
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
    internal ItemDefinition CashDefinition => cashDefinition;
    internal System.Collections.Generic.List<ItemInstance> WeaponLocker => weaponLocker;
    internal System.Collections.Generic.List<ArmorInstance> EquippedArmor => equippedArmor;
    internal ItemInstance EquippedPrimaryWeapon
    {
        get => equippedPrimaryWeapon;
        set => equippedPrimaryWeapon = value;
    }

    internal ItemInstance EquippedSecondaryWeapon
    {
        get => equippedSecondaryWeapon;
        set => equippedSecondaryWeapon = value;
    }

    internal ItemInstance EquippedMeleeWeapon
    {
        get => equippedMeleeWeapon;
        set => equippedMeleeWeapon = value;
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

    internal string FeedbackMessage => feedbackMessage;
    internal float FeedbackUntilTime => feedbackUntilTime;

    private void EnsurePresenters()
    {
        shellPresenter ??= new MetaShellPresenter(this);
        loadoutPresenter ??= new MetaLoadoutPresenter(this);
        inventoryPresenter ??= new MetaInventoryPresenter(this);
        merchantPresenter ??= new MetaMerchantPresenter(this);
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
    }

    private void OnEnable()
    {
        if (uiVisible)
        {
            EnsureMenuCursorState();
        }
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

    private void OnGUI()
    {
        if (!uiVisible)
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
        return cashDefinition != null && stashInventory != null ? stashInventory.CountItem(cashDefinition) : 0;
    }

    internal string GetCurrencyLabel()
    {
        return cashDefinition != null && !string.IsNullOrWhiteSpace(cashDefinition.DisplayName)
            ? cashDefinition.DisplayName
            : "现金";
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
        return amount > 0
            && cashDefinition != null
            && stashInventory != null
            && stashInventory.GetAddableQuantity(cashDefinition, amount) >= amount;
    }

    internal bool TryAddFunds(int amount)
    {
        return amount > 0
            && cashDefinition != null
            && stashInventory != null
            && stashInventory.TryAddItem(cashDefinition, amount, out int addedQuantity)
            && addedQuantity >= amount;
    }

    internal bool TrySpendFunds(int amount, string failureMessage)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (cashDefinition == null || stashInventory == null || stashInventory.CountItem(cashDefinition) < amount)
        {
            SetFeedback(failureMessage);
            return false;
        }

        if (!stashInventory.TryRemoveItem(cashDefinition, amount, out int removedQuantity) || removedQuantity < amount)
        {
            if (removedQuantity > 0)
            {
                stashInventory.TryAddItem(cashDefinition, removedQuantity, out _);
            }

            SetFeedback(failureMessage);
            return false;
        }

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
        }
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

        merchantCatalog?.EnsureRuntimeInventories();

        cashDefinition = itemCatalog != null ? itemCatalog.FindByItemId("cash_bundle") : null;
    }

    private void EnsureContainers()
    {
        stashInventory = EnsureContainer(stashInventory, "Profile_Stash", "仓库", stashSlots, stashMaxWeight);
        raidBackpackInventory = EnsureContainer(raidBackpackInventory, "Profile_RaidBackpack", "战局背包", raidBackpackSlots, raidBackpackMaxWeight);
        secureContainerInventory = EnsureContainer(secureContainerInventory, "Profile_SecureContainer", "安全箱", secureContainerSlots, secureContainerMaxWeight);
        specialEquipmentInventory = EnsureContainer(specialEquipmentInventory, "Profile_SpecialEquipment", "特殊装备", specialEquipmentSlots, specialEquipmentMaxWeight);
    }

    private void LoadProfileIntoContainers()
    {
        profile = PrototypeProfileService.LoadProfile(itemCatalog);
        ApplyProfileToContainers(profile);
    }

    internal void SaveProfileFromContainers()
    {
        ResolveCatalog();
        EnsureContainers();

        if (profile == null)
        {
            profile = new PrototypeProfileService.ProfileData();
        }

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
    }

    internal void ResetProfile()
    {
        profile = PrototypeProfileService.CreateDefaultProfile(itemCatalog);
        ApplyProfileToContainers(profile);
        SaveProfileFromContainers();
        SetFeedback("档案已重置为默认配置。");
    }

    internal void SetFeedback(string message)
    {
        feedbackMessage = message ?? string.Empty;
        feedbackUntilTime = Time.time + 2.6f;
    }

    internal void ShowPage(MenuPage page)
    {
        currentPage = page;
        uiVisible = true;
        EnsureMenuCursorState();
    }

    internal void HideUi()
    {
        uiVisible = false;
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
            return;
        }

        selectedRaidSceneIndex = Mathf.Clamp(index, 0, raidSceneOptions.Count - 1);
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
