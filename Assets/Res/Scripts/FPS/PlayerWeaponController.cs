using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerWeaponController : MonoBehaviour
{
    private enum WeaponSlot
    {
        Primary = 0,
        Secondary = 1,
        Melee = 2
    }

    private sealed class WeaponRuntime
    {
        public WeaponSlot Slot;
        public PrototypeWeaponDefinition Definition;
        public ItemRarity Rarity;
        public int MagazineAmmo;
        public int FireModeIndex;
        public int PendingBurstShots;
        public float NextAttackTime;
        public float ReloadEndTime;
        public string InstanceId;
        public float Durability;
        public bool IsReloading;
        public List<ItemAffix> Affixes = new List<ItemAffix>();
        public List<ItemSkill> Skills = new List<ItemSkill>();
        public ItemAffixSummary AffixSummary = ItemAffixSummary.CreateDefault();

        public bool IsConfigured => Definition != null;
        public float StatMultiplier => ItemRarityUtility.GetStatMultiplier(Rarity);
        public float FireRateMultiplier => Mathf.Max(0.1f, AffixSummary.FireRateMultiplier);
        public float ReloadSpeedMultiplier => Mathf.Max(0.1f, AffixSummary.ReloadSpeedMultiplier);
        public float SpreadMultiplier => Mathf.Max(0.1f, AffixSummary.SpreadMultiplier);
        public float RangeMultiplier => Mathf.Max(0.1f, AffixSummary.EffectiveRangeMultiplier);
        public PrototypeWeaponFireMode CurrentFireMode => Definition != null
            ? Definition.GetFireMode(FireModeIndex)
            : PrototypeWeaponFireMode.Semi;
    }

    public readonly struct WeaponHudState
    {
        public readonly PrototypeWeaponDefinition Definition;
        public readonly ItemRarity Rarity;
        public readonly PrototypeWeaponFireMode FireMode;
        public readonly int MagazineAmmo;
        public readonly int ReserveAmmo;
        public readonly bool IsReloading;
        public readonly float ReloadEndTime;
        public readonly float NextAttackTime;

        public WeaponHudState(
            PrototypeWeaponDefinition definition,
            ItemRarity rarity,
            PrototypeWeaponFireMode fireMode,
            int magazineAmmo,
            int reserveAmmo,
            bool isReloading,
            float reloadEndTime,
            float nextAttackTime)
        {
            Definition = definition;
            Rarity = ItemRarityUtility.Sanitize(rarity);
            FireMode = fireMode;
            MagazineAmmo = magazineAmmo;
            ReserveAmmo = reserveAmmo;
            IsReloading = isReloading;
            ReloadEndTime = reloadEndTime;
            NextAttackTime = nextAttackTime;
        }
    }

    [Header("Settings")]
    [SerializeField] private bool useHostSettings = true;

    [Header("References")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform primaryViewModel;
    [SerializeField] private Transform secondaryViewModel;
    [SerializeField] private Transform meleeViewModel;

    [Header("Combat")]
    [SerializeField] private PrototypeWeaponDefinition primaryWeapon;
    [SerializeField] private PrototypeWeaponDefinition secondaryWeapon;
    [SerializeField] private PrototypeWeaponDefinition meleeWeapon;
    [SerializeField] private float shootDistance = 60f;
    [SerializeField] private float baseDamage = 42f;
    [SerializeField] private float shootForce = 18f;
    [SerializeField] private float meleeImpactForce = 9f;
    [SerializeField] private float meleeStaminaCost = 22f;
    [SerializeField] private float impactMarkerLifetime = 0.2f;
    [SerializeField] private LayerMask shootMask = Physics.DefaultRaycastLayers;

    [Header("AI Awareness")]
    [SerializeField] private float firearmNoiseRadius = 26f;
    [SerializeField] private float meleeNoiseRadius = 8f;

    private PrototypeUnitVitals playerVitals;
    private InventoryContainer inventory;
    private PlayerSkillManager skillManager;
    private readonly WeaponRuntime primaryRuntime = new WeaponRuntime { Slot = WeaponSlot.Primary };
    private readonly WeaponRuntime secondaryRuntime = new WeaponRuntime { Slot = WeaponSlot.Secondary };
    private readonly WeaponRuntime meleeRuntime = new WeaponRuntime { Slot = WeaponSlot.Melee };
    private WeaponSlot activeWeaponSlot = WeaponSlot.Primary;
    private GameObject primaryViewModelInstance;
    private GameObject secondaryViewModelInstance;
    private GameObject meleeViewModelInstance;
    private PrototypeWeaponDefinition primaryViewModelSource;
    private PrototypeWeaponDefinition secondaryViewModelSource;
    private PrototypeWeaponDefinition meleeViewModelSource;
    private float hitMarkerTimer;
    private float characterDamageMultiplier = 1f;

    public bool ShowHitMarker => hitMarkerTimer > 0f;
    public PrototypeWeaponDefinition EquippedPrimaryWeapon => primaryRuntime.Definition;
    public PrototypeWeaponDefinition EquippedSecondaryWeapon => secondaryRuntime.Definition;
    public PrototypeWeaponDefinition EquippedMeleeWeapon => meleeRuntime.Definition;

    private void Awake()
    {
        ResolveReferences();
        EnsureCombatSettings();
        ResolveViewCamera();
        ResolveMuzzle();
        ResolveViewModels();
    }

    private void OnValidate()
    {
        EnsureCombatSettings();
        ResolveViewCamera();
        ResolveMuzzle();
        ResolveViewModels();
    }

    public void SetPlayerDependencies(PrototypeUnitVitals vitals, InventoryContainer inventoryContainer)
    {
        playerVitals = vitals;
        inventory = inventoryContainer;
        skillManager = GetComponent<PlayerSkillManager>();
        skillManager?.SetPlayerDependencies(playerVitals, this);
    }

    public void ApplyHostSettings(
        Camera hostCamera,
        Transform hostMuzzle,
        Transform hostPrimaryViewModel,
        Transform hostSecondaryViewModel,
        Transform hostMeleeViewModel,
        PrototypeWeaponDefinition hostPrimaryWeapon,
        PrototypeWeaponDefinition hostSecondaryWeapon,
        PrototypeWeaponDefinition hostMeleeWeapon,
        float hostShootDistance,
        float hostBaseDamage,
        float hostShootForce,
        float hostMeleeImpactForce,
        float hostMeleeStaminaCost,
        float hostImpactMarkerLifetime,
        LayerMask hostShootMask,
        float hostFirearmNoiseRadius,
        float hostMeleeNoiseRadius)
    {
        if (useHostSettings)
        {
            viewCamera = hostCamera;
            muzzle = hostMuzzle;
            primaryViewModel = hostPrimaryViewModel;
            secondaryViewModel = hostSecondaryViewModel;
            meleeViewModel = hostMeleeViewModel;
            primaryWeapon = hostPrimaryWeapon;
            secondaryWeapon = hostSecondaryWeapon;
            meleeWeapon = hostMeleeWeapon;
            shootDistance = hostShootDistance;
            baseDamage = hostBaseDamage;
            shootForce = hostShootForce;
            meleeImpactForce = hostMeleeImpactForce;
            meleeStaminaCost = hostMeleeStaminaCost;
            impactMarkerLifetime = hostImpactMarkerLifetime;
            shootMask = hostShootMask;
            firearmNoiseRadius = hostFirearmNoiseRadius;
            meleeNoiseRadius = hostMeleeNoiseRadius;
        }

        ResolveViewCamera();
        ResolveMuzzle();
        EnsureCombatSettings();
        ResolveViewModels();
    }

    public void InitializeRuntime()
    {
        ConfigureRuntimeWeapons();
        skillManager?.RefreshFromEquipment();
    }

    public void SetCharacterDamageMultiplier(float damageMultiplier)
    {
        characterDamageMultiplier = Mathf.Max(0.1f, damageMultiplier);
    }

    public void TickVisuals(float deltaTime)
    {
        if (hitMarkerTimer > 0f)
        {
            hitMarkerTimer -= deltaTime;
        }
    }

    public void ConfigureWeaponLoadout(
        PrototypeWeaponDefinition primaryDefinition,
        PrototypeWeaponDefinition secondaryDefinition,
        PrototypeWeaponDefinition meleeDefinition)
    {
        primaryWeapon = primaryDefinition;
        secondaryWeapon = secondaryDefinition;
        meleeWeapon = meleeDefinition;

        if (Application.isPlaying)
        {
            ConfigureRuntimeWeapons();
        }
    }

    public void ConfigureWeaponLoadout(
        ItemInstance primaryInstance,
        ItemInstance secondaryInstance,
        ItemInstance meleeInstance)
    {
        primaryWeapon = primaryInstance != null ? primaryInstance.WeaponDefinition : null;
        secondaryWeapon = secondaryInstance != null ? secondaryInstance.WeaponDefinition : null;
        meleeWeapon = meleeInstance != null ? meleeInstance.WeaponDefinition : null;

        if (Application.isPlaying)
        {
            ConfigureRuntimeWeapons(primaryInstance, secondaryInstance, meleeInstance);
        }
    }

    public void ConfigureWeaponLoadout(
        WeaponInstance primaryInstance,
        WeaponInstance secondaryInstance,
        WeaponInstance meleeInstance)
    {
        ConfigureWeaponLoadout(
            ItemInstance.Create(primaryInstance),
            ItemInstance.Create(secondaryInstance),
            ItemInstance.Create(meleeInstance));
    }

    public ItemInstance GetPrimaryItemInstance()
    {
        return BuildItemInstance(primaryRuntime);
    }

    public ItemInstance GetSecondaryItemInstance()
    {
        return BuildItemInstance(secondaryRuntime);
    }

    public ItemInstance GetMeleeItemInstance()
    {
        return BuildItemInstance(meleeRuntime);
    }

    public WeaponInstance GetPrimaryWeaponInstance()
    {
        return GetPrimaryItemInstance()?.ToWeaponInstance();
    }

    public WeaponInstance GetSecondaryWeaponInstance()
    {
        return GetSecondaryItemInstance()?.ToWeaponInstance();
    }

    public WeaponInstance GetMeleeWeaponInstance()
    {
        return GetMeleeItemInstance()?.ToWeaponInstance();
    }

    public int RecoverAmmoToActiveWeapon(int rounds)
    {
        WeaponRuntime runtime = GetActiveWeapon();
        if (runtime == null || !runtime.IsConfigured || runtime.Definition == null || runtime.Definition.IsMeleeWeapon || rounds <= 0)
        {
            return 0;
        }

        int recovered = Mathf.Clamp(rounds, 0, runtime.Definition.MagazineSize - runtime.MagazineAmmo);
        runtime.MagazineAmmo += recovered;
        return recovered;
    }

    public string GetSuggestedPickupSlotLabel(PrototypeWeaponDefinition weaponDefinition)
    {
        if (weaponDefinition != null && weaponDefinition.IsThrowableWeapon)
        {
            return "特殊栏";
        }

        return GetSlotDisplayName(ChoosePickupSlot(weaponDefinition));
    }

    public bool PickupWouldReplaceEquippedWeapon(PrototypeWeaponDefinition weaponDefinition)
    {
        if (weaponDefinition != null && weaponDefinition.IsThrowableWeapon)
        {
            return false;
        }

        return GetLootedWeaponAction(weaponDefinition) == LootedWeaponAction.Swap;
    }

    public bool PickupWouldStoreInBackpack(PrototypeWeaponDefinition weaponDefinition)
    {
        if (weaponDefinition != null && weaponDefinition.IsThrowableWeapon)
        {
            return inventory != null && inventory.CanAccept(ItemInstance.Create(weaponDefinition, weaponDefinition.MagazineSize, 1f, null, ItemRarity.Common));
        }

        return GetLootedWeaponAction(weaponDefinition) == LootedWeaponAction.StoreInBackpack;
    }

    public bool TryEquipLootedWeapon(
        PrototypeWeaponDefinition weaponDefinition,
        int startingMagazineAmmo,
        out PrototypeWeaponDefinition droppedWeaponDefinition,
        out int droppedMagazineAmmo)
    {
        droppedWeaponDefinition = null;
        droppedMagazineAmmo = 0;

        bool result = TryEquipLootedWeapon(
            ItemInstance.Create(weaponDefinition, startingMagazineAmmo, 1f, null, ItemRarity.Common),
            out ItemInstance droppedWeapon);
        droppedWeaponDefinition = droppedWeapon != null ? droppedWeapon.WeaponDefinition : null;
        droppedMagazineAmmo = droppedWeapon != null && droppedWeapon.WeaponDefinition != null && !droppedWeapon.WeaponDefinition.IsMeleeWeapon
            ? droppedWeapon.MagazineAmmo
            : 0;
        return result;
    }

    public bool TryEquipLootedWeapon(ItemInstance weaponInstance, out ItemInstance droppedWeapon)
    {
        droppedWeapon = null;
        if (weaponInstance == null || !weaponInstance.IsWeapon || weaponInstance.WeaponDefinition == null)
        {
            return false;
        }

        if (weaponInstance.WeaponDefinition.IsThrowableWeapon)
        {
            return inventory != null && inventory.TryAddItemInstance(weaponInstance.Clone());
        }

        WeaponRuntime runtime = GetWeaponRuntime(ChoosePickupSlot(weaponInstance.WeaponDefinition));
        if (runtime == null)
        {
            return false;
        }

        if (!runtime.IsConfigured)
        {
            return TryForceEquipWeapon(weaponInstance, out droppedWeapon);
        }

        if (inventory != null && inventory.TryAddItemInstance(weaponInstance.Clone()))
        {
            return true;
        }

        return TryForceEquipWeapon(weaponInstance, out droppedWeapon);
    }

    public bool TryEquipLootedWeapon(WeaponInstance weaponInstance, out WeaponInstance droppedWeapon)
    {
        bool equipped = TryEquipLootedWeapon(ItemInstance.Create(weaponInstance), out ItemInstance droppedItem);
        droppedWeapon = droppedItem != null ? droppedItem.ToWeaponInstance() : null;
        return equipped;
    }

    public bool TryEquipInventoryWeapon(ItemInstance itemInstance, out ItemInstance droppedWeapon)
    {
        droppedWeapon = null;
        if (itemInstance == null || !itemInstance.IsWeapon || itemInstance.WeaponDefinition == null)
        {
            return false;
        }

        if (itemInstance.WeaponDefinition.IsThrowableWeapon)
        {
            return false;
        }

        if (!TryForceEquipWeapon(itemInstance, out droppedWeapon))
        {
            return false;
        }

        if (droppedWeapon != null && inventory != null && inventory.TryAddItemInstance(droppedWeapon.Clone()))
        {
            droppedWeapon = null;
        }

        return true;
    }

    public bool TryEquipInventoryWeapon(ItemInstance itemInstance, out WeaponInstance droppedWeapon)
    {
        bool equipped = TryEquipInventoryWeapon(itemInstance, out ItemInstance droppedItem);
        droppedWeapon = droppedItem != null ? droppedItem.ToWeaponInstance() : null;
        return equipped;
    }

    public void HandleWeaponInput(PrototypeFpsInput fpsInput)
    {
        if (fpsInput == null)
        {
            return;
        }

        if (fpsInput.EquipPrimaryPressedThisFrame)
        {
            EquipWeapon(WeaponSlot.Primary);
        }
        else if (fpsInput.EquipSecondaryPressedThisFrame)
        {
            EquipWeapon(WeaponSlot.Secondary);
        }
        else if (fpsInput.EquipMeleePressedThisFrame)
        {
            EquipWeapon(WeaponSlot.Melee);
        }

        WeaponRuntime activeWeapon = GetActiveWeapon();
        if (activeWeapon == null || !activeWeapon.IsConfigured)
        {
            return;
        }

        if (fpsInput.ReloadPressedThisFrame)
        {
            TryStartReload(activeWeapon);
        }

        if (fpsInput.ToggleFireModePressedThisFrame)
        {
            CycleFireMode(activeWeapon);
        }
    }

    public void HandleCombat(PrototypeFpsInput fpsInput)
    {
        if (fpsInput == null || viewCamera == null)
        {
            return;
        }

        WeaponRuntime activeWeapon = GetActiveWeapon();
        if (activeWeapon == null || !activeWeapon.IsConfigured)
        {
            return;
        }

        UpdateReload(activeWeapon);

        if (activeWeapon.IsReloading)
        {
            return;
        }

        if (activeWeapon.Definition.IsMeleeWeapon)
        {
            if (fpsInput.ShootPressedThisFrame && Time.time >= activeWeapon.NextAttackTime)
            {
                PerformMeleeAttack(activeWeapon);
            }

            return;
        }

        if (activeWeapon.PendingBurstShots > 0)
        {
            ProcessBurst(activeWeapon);
            return;
        }

        switch (activeWeapon.CurrentFireMode)
        {
            case PrototypeWeaponFireMode.Auto:
                if (fpsInput.ShootHeld)
                {
                    FireFirearmRound(activeWeapon);
                }
                break;

            case PrototypeWeaponFireMode.Burst:
                if (fpsInput.ShootPressedThisFrame && Time.time >= activeWeapon.NextAttackTime)
                {
                    activeWeapon.PendingBurstShots = activeWeapon.Definition.BurstCount;
                    ProcessBurst(activeWeapon);
                }
                break;

            default:
                if (fpsInput.ShootPressedThisFrame)
                {
                    FireFirearmRound(activeWeapon);
                }
                break;
        }
    }

    public bool TryGetHudState(out WeaponHudState state)
    {
        WeaponRuntime activeWeapon = GetActiveWeapon();
        if (activeWeapon == null || !activeWeapon.IsConfigured)
        {
            state = default;
            return false;
        }

        state = new WeaponHudState(
            activeWeapon.Definition,
            activeWeapon.Rarity,
            activeWeapon.CurrentFireMode,
            activeWeapon.MagazineAmmo,
            GetReserveAmmoCount(activeWeapon),
            activeWeapon.IsReloading,
            activeWeapon.ReloadEndTime,
            activeWeapon.NextAttackTime);
        return true;
    }

    public void RefreshWeaponViewModels()
    {
        EnsureWeaponViewModel(primaryRuntime, primaryViewModel, ref primaryViewModelInstance, ref primaryViewModelSource);
        EnsureWeaponViewModel(secondaryRuntime, secondaryViewModel, ref secondaryViewModelInstance, ref secondaryViewModelSource);
        EnsureWeaponViewModel(meleeRuntime, meleeViewModel, ref meleeViewModelInstance, ref meleeViewModelSource);

        if (primaryViewModel != null)
        {
            primaryViewModel.gameObject.SetActive(activeWeaponSlot == WeaponSlot.Primary);
        }

        if (secondaryViewModel != null)
        {
            secondaryViewModel.gameObject.SetActive(activeWeaponSlot == WeaponSlot.Secondary);
        }

        if (meleeViewModel != null)
        {
            meleeViewModel.gameObject.SetActive(activeWeaponSlot == WeaponSlot.Melee);
        }
    }    private void ProcessBurst(WeaponRuntime runtime)
    {
        while (runtime.PendingBurstShots > 0 && Time.time >= runtime.NextAttackTime)
        {
            if (!FireFirearmRound(runtime))
            {
                runtime.PendingBurstShots = 0;
                return;
            }

            runtime.PendingBurstShots--;
        }
    }

    private bool FireFirearmRound(WeaponRuntime runtime)
    {
        if (runtime == null || !runtime.IsConfigured || runtime.Definition.IsMeleeWeapon)
        {
            return false;
        }

        if (runtime.IsReloading || Time.time < runtime.NextAttackTime)
        {
            return false;
        }

        if (runtime.MagazineAmmo <= 0)
        {
            runtime.PendingBurstShots = 0;
            return false;
        }

        runtime.MagazineAmmo--;
        float secondsPerShot = runtime.Definition.SecondsPerShot / (runtime.FireRateMultiplier * GetPassiveFireRateMultiplier());
        runtime.NextAttackTime = Time.time + Mathf.Max(0.01f, secondsPerShot);
        ReportCombatNoise(muzzle != null ? muzzle.position : viewCamera.transform.position, firearmNoiseRadius);

        AmmoDefinition ammo = runtime.Definition.AmmoDefinition;
        float shotDamage = GetFirearmBaseDamage(runtime, ammo);
        float shotForce = (ammo != null ? ammo.ImpactForce : shootForce) + runtime.Definition.AddedImpactForce;
        float baseRange = runtime.Definition.EffectiveRange > 0f ? runtime.Definition.EffectiveRange : shootDistance;
        float shotRange = baseRange * runtime.RangeMultiplier;
        Vector3 direction = GetSpreadDirection(runtime.Definition.SpreadAngle * runtime.SpreadMultiplier);
        PrototypeUnitVitals.DamageInfo damageInfo = BuildFirearmDamageInfo(runtime, shotDamage, ammo);

        if (TryGetCombatHit(viewCamera.transform.position, direction, shotRange, out RaycastHit hit))
        {
            ResolveCombatHit(hit, damageInfo, shotForce);
        }

        return true;
    }

    private void PerformMeleeAttack(WeaponRuntime runtime)
    {
        if (runtime == null || !runtime.IsConfigured || !runtime.Definition.IsMeleeWeapon)
        {
            return;
        }

        EnsureDependencies();

        if (playerVitals != null && meleeStaminaCost > 0f && (!playerVitals.CanStartStaminaAction(meleeStaminaCost) || !playerVitals.TryConsumeStamina(meleeStaminaCost)))
        {
            return;
        }

        runtime.NextAttackTime = Time.time + runtime.Definition.MeleeCooldown / GetPassiveFireRateMultiplier();
        float meleeRadius = Mathf.Max(meleeNoiseRadius, runtime.Definition.MeleeRange * 3.8f);
        ReportCombatNoise(muzzle != null ? muzzle.position : viewCamera.transform.position, meleeRadius);

        if (TryGetMeleeHit(runtime.Definition.MeleeRange, runtime.Definition.MeleeRadius, out RaycastHit hit))
        {
            ResolveCombatHit(hit, BuildMeleeDamageInfo(runtime), meleeImpactForce);
        }
    }

    private bool TryGetCombatHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit hit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, shootMask, QueryTriggerInteraction.Collide);
        return TrySelectCombatHit(hits, out hit);
    }

    private bool TryGetMeleeHit(float range, float radius, out RaycastHit hit)
    {
        Vector3 origin = viewCamera.transform.position;
        Vector3 direction = viewCamera.transform.forward;
        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, range, shootMask, QueryTriggerInteraction.Collide);
        return TrySelectCombatHit(hits, out hit);
    }

    private bool TrySelectCombatHit(RaycastHit[] hits, out RaycastHit hit)
    {
        if (hits == null || hits.Length == 0)
        {
            hit = default;
            return false;
        }

        Array.Sort(hits, CompareHitDistance);

        foreach (RaycastHit candidate in hits)
        {
            if (candidate.collider == null)
            {
                continue;
            }

            if (candidate.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            PrototypeUnitHitbox unitHitbox = candidate.collider.GetComponent<PrototypeUnitHitbox>();
            if (unitHitbox == null)
            {
                unitHitbox = candidate.collider.GetComponentInParent<PrototypeUnitHitbox>();
            }

            if (candidate.collider.isTrigger && unitHitbox == null)
            {
                continue;
            }

            hit = candidate;
            return true;
        }

        hit = default;
        return false;
    }

    private float GetFirearmBaseDamage(WeaponRuntime runtime, AmmoDefinition ammo)
    {
        PrototypeWeaponDefinition weaponDefinition = runtime != null ? runtime.Definition : null;
        float weaponDamage = weaponDefinition != null ? weaponDefinition.FirearmDamage : Mathf.Max(1f, baseDamage);
        float ammoDamageMultiplier = ammo != null ? ammo.DamageMultiplier : 1f;
        return Mathf.Max(1f, weaponDamage * ammoDamageMultiplier);
    }

    private PrototypeUnitVitals.DamageInfo BuildFirearmDamageInfo(WeaponRuntime runtime, float defaultDamage, AmmoDefinition ammo)
    {
        float rarityMultiplier = runtime != null ? runtime.StatMultiplier : 1f;
        float affixDamageMultiplier = runtime != null ? runtime.AffixSummary.DamageMultiplier : 1f;
        float damage = Mathf.Max(1f, defaultDamage * rarityMultiplier * affixDamageMultiplier * characterDamageMultiplier);
        float armorDamage = (ammo != null ? ammo.ArmorDamage : Mathf.Max(8f, defaultDamage * 0.5f)) * rarityMultiplier * affixDamageMultiplier;
        float penetrationPower = (ammo != null ? ammo.PenetrationPower : runtime.Definition.PenetrationPower) * rarityMultiplier;
        float armorPenetrationBonus = runtime != null ? runtime.AffixSummary.ArmorPenetrationBonus : 0f;
        penetrationPower = Mathf.Max(0f, penetrationPower + armorPenetrationBonus);

        bool isCrit = runtime != null
            && runtime.AffixSummary.CritChance > 0f
            && UnityEngine.Random.value < runtime.AffixSummary.CritChance;
        if (isCrit)
        {
            damage *= Mathf.Max(1f, runtime.AffixSummary.CritDamageMultiplier);
        }

        return new PrototypeUnitVitals.DamageInfo
        {
            damage = damage,
            penetrationPower = penetrationPower,
            armorDamage = armorDamage,
            lightBleedChance = ammo != null ? ammo.LightBleedChance : runtime.Definition.LightBleedChance,
            heavyBleedChance = ammo != null ? ammo.HeavyBleedChance : runtime.Definition.HeavyBleedChance,
            fractureChance = ammo != null ? ammo.FractureChance : runtime.Definition.FractureChance,
            bypassArmor = false,
            canApplyAfflictions = true,
            sourceUnit = playerVitals,
            sourceDisplayName = gameObject.name,
            sourceEffectDisplayName = isCrit ? "暴击" : string.Empty
        };
    }

    private PrototypeUnitVitals.DamageInfo BuildMeleeDamageInfo(WeaponRuntime runtime)
    {
        PrototypeWeaponDefinition weaponDefinition = runtime != null ? runtime.Definition : null;
        float rarityMultiplier = runtime != null ? runtime.StatMultiplier : 1f;
        float affixDamageMultiplier = runtime != null ? runtime.AffixSummary.DamageMultiplier : 1f;
        float damage = (weaponDefinition != null ? weaponDefinition.MeleeDamage : baseDamage) * rarityMultiplier * affixDamageMultiplier * characterDamageMultiplier;
        float armorDamage = (weaponDefinition != null ? Mathf.Max(6f, weaponDefinition.MeleeDamage * 0.28f) : 6f) * rarityMultiplier * affixDamageMultiplier;
        float penetrationPower = (weaponDefinition != null ? weaponDefinition.PenetrationPower : 6f) * rarityMultiplier;
        float armorPenetrationBonus = runtime != null ? runtime.AffixSummary.ArmorPenetrationBonus : 0f;
        penetrationPower = Mathf.Max(0f, penetrationPower + armorPenetrationBonus);

        bool isCrit = runtime != null
            && runtime.AffixSummary.CritChance > 0f
            && UnityEngine.Random.value < runtime.AffixSummary.CritChance;
        if (isCrit)
        {
            damage *= Mathf.Max(1f, runtime.AffixSummary.CritDamageMultiplier);
        }

        return new PrototypeUnitVitals.DamageInfo
        {
            damage = damage,
            penetrationPower = penetrationPower,
            armorDamage = armorDamage,
            lightBleedChance = weaponDefinition != null ? weaponDefinition.LightBleedChance : 0.4f,
            heavyBleedChance = weaponDefinition != null ? weaponDefinition.HeavyBleedChance : 0.1f,
            fractureChance = weaponDefinition != null ? weaponDefinition.FractureChance : 0.12f,
            bypassArmor = false,
            canApplyAfflictions = true,
            sourceUnit = playerVitals,
            sourceDisplayName = gameObject.name,
            sourceEffectDisplayName = isCrit ? "暴击" : string.Empty
        };
    }

    private void ResolveCombatHit(RaycastHit hit, PrototypeUnitVitals.DamageInfo damageInfo, float force)
    {
        hitMarkerTimer = 0.08f;

        PrototypeUnitHitbox unitHitbox = hit.collider.GetComponent<PrototypeUnitHitbox>();
        if (unitHitbox == null)
        {
            unitHitbox = hit.collider.GetComponentInParent<PrototypeUnitHitbox>();
        }

        bool shouldApplyImpactForce = true;
        if (unitHitbox != null)
        {
            PrototypeUnitVitals targetVitals = unitHitbox.Owner;
            bool wasDead = targetVitals != null && targetVitals.IsDead;
            unitHitbox.ApplyDamage(damageInfo);
            skillManager?.HandleDamageResolved(targetVitals, damageInfo, targetVitals != null && !wasDead && targetVitals.IsDead);
            if (targetVitals != null)
            {
                shouldApplyImpactForce = targetVitals.ShouldReceiveImpactForce;
            }
        }
        else
        {
            PrototypeBreakable breakable = hit.collider.GetComponent<PrototypeBreakable>();
            if (breakable == null)
            {
                breakable = hit.collider.GetComponentInParent<PrototypeBreakable>();
            }

            if (breakable != null)
            {
                breakable.ApplyDamage(damageInfo, hit.point, viewCamera.transform.forward, force);
                shouldApplyImpactForce = false;
            }
        }

        if (shouldApplyImpactForce && hit.rigidbody != null && force > 0f)
        {
            hit.rigidbody.AddForce(viewCamera.transform.forward * force, ForceMode.Impulse);
        }

        SpawnImpactMarker(hit);
    }

    private void TryStartReload(WeaponRuntime runtime)
    {
        if (runtime == null || !runtime.IsConfigured || runtime.Definition.IsMeleeWeapon)
        {
            return;
        }

        if (runtime.IsReloading || runtime.MagazineAmmo >= runtime.Definition.MagazineSize)
        {
            return;
        }

        AmmoDefinition ammo = runtime.Definition.AmmoDefinition;
        EnsureDependencies();
        if (inventory == null || ammo == null || inventory.CountItem(ammo) <= 0)
        {
            return;
        }

        runtime.IsReloading = true;
        float reloadDuration = runtime.Definition.ReloadDuration / (runtime.ReloadSpeedMultiplier * GetPassiveReloadSpeedMultiplier());
        runtime.ReloadEndTime = Time.time + Mathf.Max(0.05f, reloadDuration);
        runtime.PendingBurstShots = 0;
    }

    private void UpdateReload(WeaponRuntime runtime)
    {
        if (runtime == null || !runtime.IsReloading)
        {
            return;
        }

        if (Time.time < runtime.ReloadEndTime)
        {
            return;
        }

        runtime.IsReloading = false;

        EnsureDependencies();
        if (inventory == null || runtime.Definition == null || runtime.Definition.AmmoDefinition == null)
        {
            return;
        }

        int roundsNeeded = runtime.Definition.MagazineSize - runtime.MagazineAmmo;
        if (roundsNeeded <= 0)
        {
            return;
        }

        if (inventory.TryRemoveItem(runtime.Definition.AmmoDefinition, roundsNeeded, out int removedQuantity) && removedQuantity > 0)
        {
            runtime.MagazineAmmo = Mathf.Clamp(runtime.MagazineAmmo + removedQuantity, 0, runtime.Definition.MagazineSize);
        }
    }

    private void CycleFireMode(WeaponRuntime runtime)
    {
        if (runtime == null || !runtime.IsConfigured || runtime.Definition.IsMeleeWeapon)
        {
            return;
        }

        PrototypeWeaponFireMode[] fireModes = runtime.Definition.FireModes;
        if (fireModes == null || fireModes.Length <= 1)
        {
            return;
        }

        runtime.FireModeIndex = (runtime.FireModeIndex + 1) % fireModes.Length;
        runtime.PendingBurstShots = 0;
    }

    private void EquipWeapon(WeaponSlot slot)
    {
        WeaponRuntime runtime = GetWeaponRuntime(slot);
        if (runtime == null || !runtime.IsConfigured || activeWeaponSlot == slot)
        {
            return;
        }

        WeaponRuntime currentWeapon = GetActiveWeapon();
        if (currentWeapon != null)
        {
            currentWeapon.IsReloading = false;
            currentWeapon.PendingBurstShots = 0;
        }

        activeWeaponSlot = slot;
        RefreshWeaponViewModels();
    }

    private void ConfigureRuntimeWeapons()
    {
        ConfigureRuntimeWeapons(null, null, null);
    }

    private void ConfigureRuntimeWeapons(
        ItemInstance primaryInstance,
        ItemInstance secondaryInstance,
        ItemInstance meleeInstance)
    {
        SetupWeaponRuntime(
            primaryRuntime,
            primaryWeapon,
            primaryInstance != null ? primaryInstance.MagazineAmmo : -1,
            primaryInstance != null ? primaryInstance.InstanceId : null,
            primaryInstance != null ? primaryInstance.CurrentDurability : -1f,
            primaryInstance != null ? primaryInstance.Rarity : ItemRarity.Common,
            primaryInstance != null ? primaryInstance.Affixes : null,
            primaryInstance != null ? primaryInstance.Skills : null);
        SetupWeaponRuntime(
            secondaryRuntime,
            secondaryWeapon,
            secondaryInstance != null ? secondaryInstance.MagazineAmmo : -1,
            secondaryInstance != null ? secondaryInstance.InstanceId : null,
            secondaryInstance != null ? secondaryInstance.CurrentDurability : -1f,
            secondaryInstance != null ? secondaryInstance.Rarity : ItemRarity.Common,
            secondaryInstance != null ? secondaryInstance.Affixes : null,
            secondaryInstance != null ? secondaryInstance.Skills : null);
        SetupWeaponRuntime(
            meleeRuntime,
            meleeWeapon,
            meleeInstance != null ? meleeInstance.MagazineAmmo : -1,
            meleeInstance != null ? meleeInstance.InstanceId : null,
            meleeInstance != null ? meleeInstance.CurrentDurability : -1f,
            meleeInstance != null ? meleeInstance.Rarity : ItemRarity.Common,
            meleeInstance != null ? meleeInstance.Affixes : null,
            meleeInstance != null ? meleeInstance.Skills : null);
        SelectInitialWeapon();
        RefreshWeaponViewModels();
        skillManager?.RefreshFromEquipment();
    }

    private void SetupWeaponRuntime(

        WeaponRuntime runtime,

        PrototypeWeaponDefinition definition,

        int startingMagazineAmmo = -1,

        string instanceId = null,

        float durability = -1f,

        ItemRarity rarity = ItemRarity.Common,

        IReadOnlyList<ItemAffix> affixes = null,

        IReadOnlyList<ItemSkill> skills = null)

    {
        runtime.Definition = definition;
        runtime.Rarity = ItemRarityUtility.Sanitize(rarity);
        runtime.Affixes = ItemAffixUtility.CloneList(affixes);
        ItemAffixUtility.SanitizeAffixes(runtime.Affixes);
        runtime.Skills = ItemSkillUtility.CloneList(skills);
        ItemSkillUtility.SanitizeSkills(runtime.Skills);
        runtime.AffixSummary = ItemAffixUtility.BuildSummary(runtime.Affixes);
        runtime.PendingBurstShots = 0;
        runtime.IsReloading = false;
        runtime.NextAttackTime = 0f;
        runtime.ReloadEndTime = 0f;
        runtime.FireModeIndex = 0;
        runtime.MagazineAmmo = definition != null && !definition.IsMeleeWeapon
            ? Mathf.Clamp(startingMagazineAmmo >= 0 ? startingMagazineAmmo : definition.MagazineSize, 0, definition.MagazineSize)
            : 0;
        runtime.InstanceId = string.Empty;
        runtime.Durability = 0f;

        if (definition == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(instanceId))
        {
            runtime.InstanceId = instanceId.Trim();
        }

        if (string.IsNullOrWhiteSpace(runtime.InstanceId))
        {
            runtime.InstanceId = Guid.NewGuid().ToString("N");
        }

        runtime.Durability = durability >= 0f ? Mathf.Max(0f, durability) : 1f;
    }

    private void SelectInitialWeapon()
    {
        if (primaryRuntime.IsConfigured)
        {
            activeWeaponSlot = WeaponSlot.Primary;
        }
        else if (secondaryRuntime.IsConfigured)
        {
            activeWeaponSlot = WeaponSlot.Secondary;
        }
        else if (meleeRuntime.IsConfigured)
        {
            activeWeaponSlot = WeaponSlot.Melee;
        }
    }

    private WeaponRuntime GetActiveWeapon()
    {
        return GetWeaponRuntime(activeWeaponSlot);
    }

    private WeaponRuntime GetWeaponRuntime(WeaponSlot slot)
    {
        switch (slot)
        {
            case WeaponSlot.Primary:
                return primaryRuntime;

            case WeaponSlot.Secondary:
                return secondaryRuntime;

            default:
                return meleeRuntime;
        }
    }

    private WeaponSlot ChoosePickupSlot(PrototypeWeaponDefinition weaponDefinition)
    {
        if (weaponDefinition == null)
        {
            return activeWeaponSlot;
        }

        if (weaponDefinition.IsThrowableWeapon)
        {
            return activeWeaponSlot;
        }

        if (weaponDefinition.IsMeleeWeapon)
        {
            return WeaponSlot.Melee;
        }

        if (!primaryRuntime.IsConfigured)
        {
            return WeaponSlot.Primary;
        }

        if (!secondaryRuntime.IsConfigured)
        {
            return WeaponSlot.Secondary;
        }

        if (activeWeaponSlot == WeaponSlot.Primary || activeWeaponSlot == WeaponSlot.Secondary)
        {
            return activeWeaponSlot;
        }

        return WeaponSlot.Primary;
    }

    private string GetSlotDisplayName(WeaponSlot slot)
    {
        switch (slot)
        {
            case WeaponSlot.Primary:
                return "主武器";

            case WeaponSlot.Secondary:
                return "副武器";

            default:
                return "近战";
        }
    }

    private void SetWeaponForSlot(WeaponSlot slot, ItemInstance instance)
    {
        if (instance == null)
        {
            SetWeaponForSlot(slot, (PrototypeWeaponDefinition)null);
            return;
        }

        SetWeaponForSlot(
            slot,
            instance.WeaponDefinition,
            instance.MagazineAmmo,
            instance.InstanceId,
            instance.CurrentDurability,
            instance.Rarity,
            instance.Affixes,
            instance.Skills);
    }

    private void SetWeaponForSlot(WeaponSlot slot, WeaponInstance instance)
    {
        SetWeaponForSlot(slot, ItemInstance.Create(instance));
    }

    private void SetWeaponForSlot(

        WeaponSlot slot,

        PrototypeWeaponDefinition weaponDefinition,

        int startingMagazineAmmo = -1,

        string instanceId = null,

        float durability = -1f,

        ItemRarity rarity = ItemRarity.Common,

        IReadOnlyList<ItemAffix> affixes = null,

        IReadOnlyList<ItemSkill> skills = null)

    {
        switch (slot)
        {
            case WeaponSlot.Primary:
                primaryWeapon = weaponDefinition;
                SetupWeaponRuntime(primaryRuntime, weaponDefinition, startingMagazineAmmo, instanceId, durability, rarity, affixes, skills);
                break;

            case WeaponSlot.Secondary:
                secondaryWeapon = weaponDefinition;
                SetupWeaponRuntime(secondaryRuntime, weaponDefinition, startingMagazineAmmo, instanceId, durability, rarity, affixes, skills);
                break;

            default:
                meleeWeapon = weaponDefinition;
                SetupWeaponRuntime(meleeRuntime, weaponDefinition, startingMagazineAmmo, instanceId, durability, rarity, affixes, skills);
                break;
        }

        if (Application.isPlaying)
        {
            RefreshWeaponViewModels();
            skillManager?.RefreshFromEquipment();
        }
    }

    private ItemInstance BuildItemInstance(WeaponRuntime runtime)
    {
        if (runtime == null || runtime.Definition == null)
        {
            return null;
        }

        return ItemInstance.Create(
            runtime.Definition,
            runtime.MagazineAmmo,
            runtime.Durability,
            runtime.InstanceId,
            runtime.Rarity,
            runtime.Affixes,
            false,
            runtime.Skills,
            false);
    }

    private WeaponInstance BuildWeaponInstance(WeaponRuntime runtime)
    {
        return BuildItemInstance(runtime)?.ToWeaponInstance();
    }

    private float GetPassiveFireRateMultiplier()
    {
        return Mathf.Max(0.1f, skillManager != null ? skillManager.GetFireRateMultiplier() : 1f);
    }

    private float GetPassiveReloadSpeedMultiplier()
    {
        return Mathf.Max(0.1f, skillManager != null ? skillManager.GetReloadSpeedMultiplier() : 1f);
    }

    private int GetReserveAmmoCount(WeaponRuntime runtime)
    {
        EnsureDependencies();
        if (inventory == null || runtime == null || runtime.Definition == null || runtime.Definition.AmmoDefinition == null)
        {
            return 0;
        }

        return inventory.CountItem(runtime.Definition.AmmoDefinition);
    }

    private void ResolveViewCamera()
    {
        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }
    }

    private void ResolveMuzzle()
    {
        if (muzzle == null && viewCamera != null)
        {
            Transform muzzleTransform = viewCamera.transform.Find("Muzzle");
            if (muzzleTransform != null)
            {
                muzzle = muzzleTransform;
            }
        }
    }

    private void ResolveViewModels()
    {
        if (viewCamera == null)
        {
            return;
        }

        primaryViewModel = GetOrCreateViewModelAnchor(primaryViewModel, "WeaponView_Primary");
        secondaryViewModel = GetOrCreateViewModelAnchor(secondaryViewModel, "WeaponView_Secondary");
        meleeViewModel = GetOrCreateViewModelAnchor(meleeViewModel, "WeaponView_Melee");
    }

    private Transform GetOrCreateViewModelAnchor(Transform currentAnchor, string anchorName)
    {
        if (viewCamera == null)
        {
            return currentAnchor;
        }

        if (currentAnchor != null)
        {
            return currentAnchor;
        }

        Transform found = viewCamera.transform.Find(anchorName);
        if (found != null)
        {
            return found;
        }

        GameObject anchorObject = new GameObject(anchorName);
        anchorObject.transform.SetParent(viewCamera.transform, false);
        return anchorObject.transform;
    }

    private void EnsureWeaponViewModel(
        WeaponRuntime runtime,
        Transform anchor,
        ref GameObject currentInstance,
        ref PrototypeWeaponDefinition currentSource)
    {
        if (anchor == null)
        {
            DestroyViewModelInstance(ref currentInstance);
            currentSource = null;
            return;
        }

        PrototypeWeaponDefinition nextSource = runtime != null && runtime.IsConfigured ? runtime.Definition : null;
        GameObject nextPrefab = nextSource != null ? nextSource.FirstPersonViewPrefab : null;
        bool needsRefresh = currentSource != nextSource || currentInstance == null;

        if (!needsRefresh)
        {
            return;
        }

        DestroyViewModelInstance(ref currentInstance);
        ClearViewModelAnchor(anchor);
        currentSource = nextSource;

        if (nextPrefab == null)
        {
            return;
        }

        currentInstance = Instantiate(nextPrefab, anchor, false);
        currentInstance.name = nextPrefab.name;
    }

    private void ClearViewModelAnchor(Transform anchor)
    {
        if (anchor == null)
        {
            return;
        }

        for (int childIndex = anchor.childCount - 1; childIndex >= 0; childIndex--)
        {
            DestroyViewModelObject(anchor.GetChild(childIndex).gameObject);
        }
    }

    private void DestroyViewModelInstance(ref GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        DestroyViewModelObject(instance);
        instance = null;
    }

    private void DestroyViewModelObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
            return;
        }

        DestroyImmediate(target);
    }

    private Vector3 GetSpreadDirection(float spreadAngle)
    {
        Vector3 direction = viewCamera.transform.forward;
        if (spreadAngle <= 0.001f)
        {
            return direction;
        }

        Vector2 spread = UnityEngine.Random.insideUnitCircle * spreadAngle;
        direction = Quaternion.AngleAxis(spread.x, viewCamera.transform.up)
            * Quaternion.AngleAxis(spread.y, viewCamera.transform.right)
            * direction;
        return direction.normalized;
    }

    private static int CompareHitDistance(RaycastHit left, RaycastHit right)
    {
        return left.distance.CompareTo(right.distance);
    }

    private void SpawnImpactMarker(RaycastHit hit)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "ImpactMarker";
        marker.transform.position = hit.point + hit.normal * 0.03f;
        marker.transform.localScale = Vector3.one * 0.12f;

        Destroy(marker.GetComponent<Collider>());

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        markerRenderer.material = new Material(shader);
        markerRenderer.material.color = new Color(1f, 0.4f, 0.1f, 1f);

        Destroy(marker, impactMarkerLifetime);
    }

    private void ReportCombatNoise(float radius)
    {
        ReportCombatNoise(transform.position + Vector3.up * 0.9f, radius);
    }

    private void ReportCombatNoise(Vector3 position, float radius)
    {
        if (radius <= 0f)
        {
            return;
        }

        PrototypeCombatNoiseSystem.ReportNoise(position, radius, gameObject);
    }

    private void EnsureCombatSettings()
    {
        shootDistance = Mathf.Max(shootDistance, 1f);
        baseDamage = Mathf.Max(baseDamage, 1f);
        shootForce = Mathf.Max(shootForce, 0f);
        meleeImpactForce = Mathf.Max(meleeImpactForce, 0f);
        meleeStaminaCost = Mathf.Max(0f, meleeStaminaCost);
        impactMarkerLifetime = Mathf.Max(impactMarkerLifetime, 0.05f);
        firearmNoiseRadius = Mathf.Max(0f, firearmNoiseRadius);
        meleeNoiseRadius = Mathf.Max(0f, meleeNoiseRadius);
        if (shootMask.value == 0 || shootMask.value == ~0)
        {
            shootMask = Physics.DefaultRaycastLayers;
        }
    }

    private void ResolveReferences()
    {
        if (playerVitals == null)
        {
            playerVitals = GetComponent<PrototypeUnitVitals>();
        }

        if (inventory == null)
        {
            inventory = GetComponent<InventoryContainer>();
        }

        if (skillManager == null)
        {
            skillManager = GetComponent<PlayerSkillManager>();
        }

    }

    private void EnsureDependencies()
    {
        if (playerVitals == null || inventory == null || skillManager == null)
        {
            ResolveReferences();
        }
    }

    private enum LootedWeaponAction
    {
        Invalid = 0,
        Equip = 1,
        StoreInBackpack = 2,
        Swap = 3
    }

    private LootedWeaponAction GetLootedWeaponAction(PrototypeWeaponDefinition weaponDefinition)
    {
        if (weaponDefinition == null)
        {
            return LootedWeaponAction.Invalid;
        }

        WeaponRuntime runtime = GetWeaponRuntime(ChoosePickupSlot(weaponDefinition));
        if (runtime == null)
        {
            return LootedWeaponAction.Invalid;
        }

        if (!runtime.IsConfigured)
        {
            return LootedWeaponAction.Equip;
        }

        return CanStoreLootedWeaponInBackpack(weaponDefinition)
            ? LootedWeaponAction.StoreInBackpack
            : LootedWeaponAction.Swap;
    }

    private bool CanStoreLootedWeaponInBackpack(PrototypeWeaponDefinition weaponDefinition)
    {
        EnsureDependencies();
        return inventory != null
            && weaponDefinition != null
            && inventory.CanAccept(ItemInstance.Create(
                weaponDefinition,
                weaponDefinition.IsMeleeWeapon ? 0 : weaponDefinition.MagazineSize,
                1f,
                null,
                ItemRarity.Common));
    }

    private bool TryForceEquipWeapon(ItemInstance weaponInstance, out ItemInstance droppedWeapon)
    {
        droppedWeapon = null;
        if (weaponInstance == null || !weaponInstance.IsWeapon || weaponInstance.WeaponDefinition == null)
        {
            return false;
        }

        WeaponSlot slot = ChoosePickupSlot(weaponInstance.WeaponDefinition);
        WeaponRuntime runtime = GetWeaponRuntime(slot);
        if (runtime == null)
        {
            return false;
        }

        droppedWeapon = BuildItemInstance(runtime);
        SetWeaponForSlot(slot, weaponInstance);
        EquipWeapon(slot);
        return true;
    }

    private bool TryForceEquipWeapon(WeaponInstance weaponInstance, out WeaponInstance droppedWeapon)
    {
        bool equipped = TryForceEquipWeapon(ItemInstance.Create(weaponInstance), out ItemInstance droppedItem);
        droppedWeapon = droppedItem != null ? droppedItem.ToWeaponInstance() : null;
        return equipped;
    }
}
