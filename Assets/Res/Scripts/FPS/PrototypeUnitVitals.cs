using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class PrototypeUnitVitals : MonoBehaviour
{
    private const float HealthEpsilon = 0.001f;
    private const string FallbackHeadPartId = "head";
    private const string FallbackTorsoPartId = "torso";
    private const string FallbackLegsPartId = "legs";

    private enum DamageOrigin
    {
        DirectHit = 0,
        OverflowShare = 1,
        StatusEffect = 2
    }

    [Serializable]
    public struct DamageInfo
    {
        [Min(0f)] public float damage;
        [Min(0f)] public float penetrationPower;
        [Min(0f)] public float armorDamage;
        [Range(0f, 1f)] public float lightBleedChance;
        [Range(0f, 1f)] public float heavyBleedChance;
        [Range(0f, 1f)] public float fractureChance;
        public bool bypassArmor;
        public bool canApplyAfflictions;
        public PrototypeUnitVitals sourceUnit;
        public string sourceDisplayName;
        public string sourceEffectDisplayName;

        public static DamageInfo CreateDefault(float amount)
        {
            return new DamageInfo
            {
                damage = Mathf.Max(0f, amount),
                penetrationPower = 0f,
                armorDamage = 0f,
                lightBleedChance = 0f,
                heavyBleedChance = 0f,
                fractureChance = 0f,
                bypassArmor = false,
                canApplyAfflictions = false,
                sourceUnit = null,
                sourceDisplayName = string.Empty,
                sourceEffectDisplayName = string.Empty
            };
        }
    }

    [Serializable]
    public struct DamageSourceSnapshot
    {
        public string sourceDisplayName;
        public string effectDisplayName;
        public string partId;
        public bool viaStatusEffect;
    }

    public enum CombatFeedbackKind
    {
        ArmorDamage = 0,
        HealthDamage = 1,
        ArmorBroken = 2
    }

    [Serializable]
    public struct CombatFeedback
    {
        public CombatFeedbackKind kind;
        public string partId;
        public float amount;
        public string text;

        public static CombatFeedback CreateArmorDamage(string targetPartId, float durabilityDamage)
        {
            float clampedAmount = Mathf.Max(0f, durabilityDamage);
            return new CombatFeedback
            {
                kind = CombatFeedbackKind.ArmorDamage,
                partId = NormalizePartId(targetPartId),
                amount = clampedAmount,
                text = $"-{Mathf.RoundToInt(clampedAmount)}"
            };
        }

        public static CombatFeedback CreateHealthDamage(string targetPartId, float healthDamage)
        {
            float clampedAmount = Mathf.Max(0f, healthDamage);
            return new CombatFeedback
            {
                kind = CombatFeedbackKind.HealthDamage,
                partId = NormalizePartId(targetPartId),
                amount = clampedAmount,
                text = $"-{Mathf.RoundToInt(clampedAmount)}"
            };
        }

        public static CombatFeedback CreateArmorBroken(string targetPartId)
        {
            return new CombatFeedback
            {
                kind = CombatFeedbackKind.ArmorBroken,
                partId = NormalizePartId(targetPartId),
                amount = 0f,
                text = "-护甲损坏"
            };
        }
    }

        [Serializable]
    public class ArmorState
    {
        public ArmorDefinition definition;
        public string instanceId = string.Empty;
        public string displayName = string.Empty;
        public ItemRarity rarity = ItemRarity.Common;
        [Min(1f)] public float maxDurability = 1f;
        [Min(0f)] public float currentDurability = 1f;
        public List<ItemAffix> affixes = new List<ItemAffix>();
        public List<ItemSkill> skills = new List<ItemSkill>();
        private ItemAffixSummary affixSummary = ItemAffixSummary.CreateDefault();

        public ItemRarity Rarity => ItemRarityUtility.Sanitize(rarity);
        public float StatMultiplier => ItemRarityUtility.GetStatMultiplier(Rarity);
        public ItemAffixSummary AffixSummary => affixSummary;
        public IReadOnlyList<ItemSkill> Skills => skills;
        public float DamageReduction => affixSummary.DamageReduction;
        public float MoveSpeedMultiplier => affixSummary.MoveSpeedMultiplier;
        public float EffectiveArmorClass => definition != null
            ? definition.ArmorClass * StatMultiplier + affixSummary.ArmorClassBonus
            : 0f;
        public float EffectiveBleedProtection => definition != null
            ? Mathf.Clamp01(definition.BleedProtection * Mathf.Lerp(1f, 1.35f, StatMultiplier - 1f))
            : 0f;
        public float EffectiveFractureProtection => definition != null
            ? Mathf.Clamp01(definition.FractureProtection * Mathf.Lerp(1f, 1.35f, StatMultiplier - 1f))
            : 0f;
        public float BonusHealth => definition != null ? ItemRarityUtility.ScaleValue(definition.BonusHealth, Rarity) : 0f;

        public void ApplyDefinition(
            ArmorDefinition armorDefinition,
            float preservedDurability,
            string desiredInstanceId = null,
            ItemRarity itemRarity = ItemRarity.Common,
            IReadOnlyList<ItemAffix> affixesOverride = null,
            IReadOnlyList<ItemSkill> skillsOverride = null)
        {
            definition = armorDefinition;
            rarity = ItemRarityUtility.Sanitize(itemRarity);
            displayName = armorDefinition != null
                ? $"{armorDefinition.DisplayNameWithLevel} [{ItemRarityUtility.GetDisplayName(Rarity)}]"
                : string.Empty;
            affixes = affixesOverride != null ? ItemAffixUtility.CloneList(affixesOverride) : new List<ItemAffix>();
            ItemAffixUtility.SanitizeAffixes(affixes);
            skills = skillsOverride != null ? ItemSkillUtility.CloneList(skillsOverride) : new List<ItemSkill>();
            ItemSkillUtility.SanitizeSkills(skills);
            affixSummary = ItemAffixUtility.BuildSummary(affixes);
            maxDurability = armorDefinition != null
                ? Mathf.Max(1f, ItemRarityUtility.ScaleValue(armorDefinition.MaxDurability, Rarity) * affixSummary.DurabilityMultiplier)
                : 1f;
            currentDurability = Mathf.Clamp(preservedDurability, 0f, maxDurability);

            if (!string.IsNullOrWhiteSpace(desiredInstanceId))
            {
                instanceId = desiredInstanceId.Trim();
            }

            EnsureInstanceId();
        }

        public void Sanitize()
        {
            if (affixes == null)
            {
                affixes = new List<ItemAffix>();
            }

            if (skills == null)
            {
                skills = new List<ItemSkill>();
            }

            ItemAffixUtility.SanitizeAffixes(affixes);
            ItemSkillUtility.SanitizeSkills(skills);
            affixSummary = ItemAffixUtility.BuildSummary(affixes);
            rarity = ItemRarityUtility.Sanitize(rarity);
            displayName = string.IsNullOrWhiteSpace(displayName) && definition != null
                ? $"{definition.DisplayNameWithLevel} [{ItemRarityUtility.GetDisplayName(Rarity)}]"
                : displayName;
            maxDurability = definition != null
                ? Mathf.Max(1f, ItemRarityUtility.ScaleValue(definition.MaxDurability, Rarity) * affixSummary.DurabilityMultiplier)
                : Mathf.Max(1f, maxDurability);
            currentDurability = Mathf.Clamp(currentDurability, 0f, maxDurability);
            EnsureInstanceId();
        }

        private void EnsureInstanceId()
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                instanceId = Guid.NewGuid().ToString("N");
            }
        }

        public bool CoversPart(string partId)
        {
            return definition != null && currentDurability > HealthEpsilon && definition.CoversPart(partId);
        }

        public float DurabilityNormalized => maxDurability > HealthEpsilon ? Mathf.Clamp01(currentDurability / maxDurability) : 0f;
    }

    [Serializable]
    public class PartAfflictionState
    {
        public string partId = string.Empty;
        public bool lightBleeding;
        public bool heavyBleeding;
        public bool fractured;

        public void Sanitize()
        {
            partId = NormalizePartId(partId);
        }
    }

    [Serializable]
    public class OverflowRouteState
    {
        public string partId = string.Empty;
        [Min(0f)] public float weight = 1f;

        public void ApplyDefinition(PrototypeUnitDefinition.OverflowTarget definition)
        {
            partId = NormalizePartId(definition != null ? definition.partId : string.Empty);
            weight = Mathf.Max(0f, definition != null ? definition.weight : 0f);
        }

        public void Sanitize()
        {
            partId = NormalizePartId(partId);
            weight = Mathf.Max(0f, weight);
        }
    }

    [Serializable]
    public class PartState
    {
        [FormerlySerializedAs("bodyPart")]
        public PrototypeBodyPartType legacyBodyPart = PrototypeBodyPartType.Torso;
        public string partId = string.Empty;
        public string displayName = string.Empty;
        [Min(1f)] public float baseMaxHealth = 1f;
        [Min(0f)] public float armorBonusHealth = 0f;
        [Min(1f)] public float maxHealth = 1f;
        [Min(0f)] public float currentHealth = 1f;
        [Min(0f)] public float overflowMultiplier = 1f;
        public bool contributesToUnitHealth = true;
        public bool receivesOverflowDamage = true;
        public bool receivesOverflowFollowUpDamage = false;
        public PrototypeUnitDefinition.ZeroKillMode zeroKillMode = PrototypeUnitDefinition.ZeroKillMode.Never;
        public bool killUnitWhenBlackedAndDamagedAgain = false;
        [Min(0f)] public float blackedFollowUpDamageThreshold = 0f;
        public List<OverflowRouteState> overflowTargets = new List<OverflowRouteState>();

        public void ApplyDefinition(PrototypeUnitDefinition.PartDefinition definition, float preservedCurrentHealth)
        {
            partId = NormalizePartId(definition != null ? definition.partId : string.Empty);
            displayName = string.IsNullOrWhiteSpace(definition != null ? definition.displayName : string.Empty)
                ? partId
                : definition.displayName.Trim();
            legacyBodyPart = MapLegacyBodyPart(partId);
            baseMaxHealth = Mathf.Max(1f, definition != null ? definition.maxHealth : 1f);
            armorBonusHealth = 0f;
            maxHealth = baseMaxHealth;
            currentHealth = Mathf.Clamp(preservedCurrentHealth, 0f, maxHealth);
            overflowMultiplier = Mathf.Max(0f, definition != null ? definition.overflowMultiplier : 0f);
            contributesToUnitHealth = definition != null && definition.contributesToUnitHealth;
            receivesOverflowDamage = definition == null || definition.receivesOverflowDamage;
            receivesOverflowFollowUpDamage = definition != null && definition.receivesOverflowFollowUpDamage;
            zeroKillMode = definition != null ? definition.zeroKillMode : PrototypeUnitDefinition.ZeroKillMode.Never;
            killUnitWhenBlackedAndDamagedAgain = definition != null && definition.killUnitWhenBlackedAndDamagedAgain;
            blackedFollowUpDamageThreshold = Mathf.Max(0f, definition != null ? definition.blackedFollowUpDamageThreshold : 0f);

            overflowTargets = new List<OverflowRouteState>();
            if (definition != null && definition.overflowTargets != null)
            {
                foreach (PrototypeUnitDefinition.OverflowTarget overflowTarget in definition.overflowTargets)
                {
                    if (overflowTarget == null)
                    {
                        continue;
                    }

                    var runtimeRoute = new OverflowRouteState();
                    runtimeRoute.ApplyDefinition(overflowTarget);
                    if (!string.IsNullOrWhiteSpace(runtimeRoute.partId) && runtimeRoute.weight > 0f)
                    {
                        overflowTargets.Add(runtimeRoute);
                    }
                }
            }
        }

        public void Sanitize()
        {
            if (string.IsNullOrWhiteSpace(partId))
            {
                partId = MapLegacyPartId(legacyBodyPart);
            }

            partId = NormalizePartId(partId);
            displayName = string.IsNullOrWhiteSpace(displayName) ? partId : displayName.Trim();
            baseMaxHealth = Mathf.Max(1f, baseMaxHealth > HealthEpsilon ? baseMaxHealth : maxHealth);
            armorBonusHealth = Mathf.Max(0f, armorBonusHealth);
            maxHealth = Mathf.Max(1f, baseMaxHealth + armorBonusHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            overflowMultiplier = Mathf.Max(0f, overflowMultiplier);
            blackedFollowUpDamageThreshold = Mathf.Max(0f, blackedFollowUpDamageThreshold);

            if (overflowTargets == null)
            {
                overflowTargets = new List<OverflowRouteState>();
                return;
            }

            for (int index = overflowTargets.Count - 1; index >= 0; index--)
            {
                OverflowRouteState overflowRoute = overflowTargets[index];
                if (overflowRoute == null)
                {
                    overflowTargets.RemoveAt(index);
                    continue;
                }

                overflowRoute.Sanitize();
                if (string.IsNullOrWhiteSpace(overflowRoute.partId) || overflowRoute.weight <= 0f)
                {
                    overflowTargets.RemoveAt(index);
                }
            }
        }
    }

    private struct PendingDamage
    {
        public string partId;
        public float amount;
        public DamageOrigin origin;
        public float penetrationPower;
        public float armorDamage;
        public float lightBleedChance;
        public float heavyBleedChance;
        public float fractureChance;
        public bool bypassArmor;
        public bool canApplyAfflictions;
        public PrototypeUnitVitals sourceUnit;
        public string sourceDisplayName;
        public string sourceEffectDisplayName;

        public PendingDamage(string partId, float amount, DamageOrigin origin)
        {
            this.partId = NormalizePartId(partId);
            this.amount = amount;
            this.origin = origin;
            penetrationPower = 0f;
            armorDamage = 0f;
            lightBleedChance = 0f;
            heavyBleedChance = 0f;
            fractureChance = 0f;
            bypassArmor = origin != DamageOrigin.DirectHit;
            canApplyAfflictions = false;
            sourceUnit = null;
            sourceDisplayName = string.Empty;
            sourceEffectDisplayName = string.Empty;
        }

        public PendingDamage(string partId, DamageInfo damageInfo, DamageOrigin origin)
        {
            this.partId = NormalizePartId(partId);
            amount = Mathf.Max(0f, damageInfo.damage);
            this.origin = origin;
            penetrationPower = Mathf.Max(0f, damageInfo.penetrationPower);
            armorDamage = Mathf.Max(0f, damageInfo.armorDamage);
            lightBleedChance = Mathf.Clamp01(damageInfo.lightBleedChance);
            heavyBleedChance = Mathf.Clamp01(damageInfo.heavyBleedChance);
            fractureChance = Mathf.Clamp01(damageInfo.fractureChance);
            bypassArmor = damageInfo.bypassArmor || origin != DamageOrigin.DirectHit;
            canApplyAfflictions = damageInfo.canApplyAfflictions && origin == DamageOrigin.DirectHit;
            sourceUnit = damageInfo.sourceUnit;
            sourceDisplayName = damageInfo.sourceDisplayName;
            sourceEffectDisplayName = damageInfo.sourceEffectDisplayName;
        }
    }

    [Header("Tarkov-Inspired Health")]
    [SerializeField] private PrototypeUnitDefinition unitDefinition;
    [SerializeField] private List<PartState> bodyParts = new List<PartState>();
    [Header("Armor Setup")]
    [SerializeField] private List<ArmorDefinition> armorLoadout = new List<ArmorDefinition>();
    [Header("Stamina")]
    [Min(1f)]
    [SerializeField] private float maxStamina = 100f;
    [Min(0f)]
    [SerializeField] private float currentStamina = 100f;
    [Min(0f)]
    [SerializeField] private float staminaRecoveryPerSecond = 24f;
    [Range(0f, 1f)]
    [SerializeField] private float staminaActionThresholdNormalized = 0.05f;
    [Min(0f)]
    [SerializeField] private float staminaRecoveryDelay = 0.45f;
    [Min(0f)]
    [SerializeField] private float exhaustionRecoveryDelay = 1.4f;
    [Header("Character Bonuses")]
    [Min(0f)]
    [SerializeField] private float characterBonusHealth;
    [Min(0f)]
    [SerializeField] private float characterBonusStamina;
    [Min(0.1f)]
    [SerializeField] private float medicalEffectivenessMultiplier = 1f;
    [Min(0.1f)]
    [SerializeField] private float characterMoveSpeedMultiplier = 1f;
    [Header("Runtime State")]
    [SerializeField, HideInInspector] private List<ArmorState> equippedArmor = new List<ArmorState>();
    [SerializeField] private PrototypeStatusEffectController statusEffects;
    [SerializeField] private PlayerSkillManager skillManager;
    [SerializeField] private bool bootstrapStatusEffects = true;
    [SerializeField] private bool allowImpactForceWhenAlive = true;
    [SerializeField, HideInInspector] private DamageSourceSnapshot lastDamageSource;
    [SerializeField] private UnityEvent onDied = new UnityEvent();

    private readonly Queue<PendingDamage> pendingDamage = new Queue<PendingDamage>();
    private float equipmentMoveSpeedMultiplier = 1f;
    private bool isApplyingDamage;
    private float staminaRecoveryBlockedTimer;
    [NonSerialized] private PrototypeUnitVitals lastDamageSourceUnit;

    public event Action<PrototypeUnitVitals> Died;
    public event Action<CombatFeedback> CombatFeedbackGenerated;

    public PrototypeUnitDefinition UnitDefinition => unitDefinition;
    public IReadOnlyList<PartState> BodyParts => bodyParts;
    public IReadOnlyList<ArmorDefinition> ArmorLoadout => armorLoadout;
    public IReadOnlyList<ArmorState> EquippedArmor => equippedArmor;
    public PrototypeStatusEffectController StatusEffects => statusEffects;
    public bool IsDead { get; private set; }
    public bool ShouldReceiveImpactForce => IsDead || allowImpactForceWhenAlive;
    public string HealthBarAnchorPartId => ResolveHealthBarAnchorPartId();
    public bool HasLightBleed => statusEffects != null && statusEffects.HasLightBleed;
    public bool HasHeavyBleed => statusEffects != null && statusEffects.HasHeavyBleed;
    public bool HasAnyBleed => HasLightBleed || HasHeavyBleed;
    public bool HasFracture => statusEffects != null && statusEffects.HasFracture;
    public bool IsPainkillerActive => statusEffects != null && statusEffects.IsPainkillerActive;
    public float PainkillerRemaining => statusEffects != null ? statusEffects.PainkillerRemaining : 0f;
    public float MovementPenaltyMultiplier => (statusEffects != null ? statusEffects.MovementPenaltyMultiplier : 1f)
        * EquipmentMoveSpeedMultiplier
        * Mathf.Clamp(characterMoveSpeedMultiplier, 0.4f, 1.8f);
    public float EquipmentMoveSpeedMultiplier => equipmentMoveSpeedMultiplier;
    public float JumpPenaltyMultiplier => statusEffects != null ? statusEffects.JumpPenaltyMultiplier : 1f;
    public float MedicalEffectivenessMultiplier => Mathf.Max(0.1f, medicalEffectivenessMultiplier);
    public DamageSourceSnapshot LastDamageSource => lastDamageSource;
    public PrototypeUnitVitals LastDamageSourceUnit => lastDamageSourceUnit;
    public float MaxStamina => Mathf.Max(1f, maxStamina + characterBonusStamina);
    public float CurrentStamina => Mathf.Clamp(currentStamina, 0f, MaxStamina);
    public float StaminaNormalized => MaxStamina > HealthEpsilon ? Mathf.Clamp01(CurrentStamina / MaxStamina) : 0f;
    public float StaminaActionThresholdNormalized => Mathf.Clamp01(staminaActionThresholdNormalized);
    public bool IsExhausted => CurrentStamina <= HealthEpsilon;
    public bool IsBelowStaminaActionThreshold => StaminaNormalized + HealthEpsilon < StaminaActionThresholdNormalized;
    public bool IsStaminaRecoveryBlocked => staminaRecoveryBlockedTimer > HealthEpsilon;
    public float StaminaRecoveryBlockedRemaining => Mathf.Max(0f, staminaRecoveryBlockedTimer);

    public float TotalCurrentHealth
    {
        get
        {
            float total = 0f;
            foreach (PartState state in bodyParts)
            {
                if (state.contributesToUnitHealth)
                {
                    total += state.currentHealth;
                }
            }

            return total;
        }
    }

    public float TotalMaxHealth
    {
        get
        {
            float total = 0f;
            foreach (PartState state in bodyParts)
            {
                if (state.contributesToUnitHealth)
                {
                    total += state.maxHealth;
                }
            }

            return total;
        }
    }

    public float TotalHealthNormalized
    {
        get
        {
            float totalMaxHealth = TotalMaxHealth;
            if (totalMaxHealth <= HealthEpsilon)
            {
                return 0f;
            }

            return Mathf.Clamp01(TotalCurrentHealth / totalMaxHealth);
        }
    }

    private void Reset()
    {
        ResolveRuntimeDependencies();
        EnsureBodyPartSetup(true);
    }

    private void Awake()
    {
        ResolveRuntimeDependencies();
        EnsureBodyPartSetup(true);
    }

    private void Update()
    {
        UpdateStamina(Time.deltaTime);
    }

    private void OnValidate()
    {
        ResolveRuntimeDependencies();
        EnsureBodyPartSetup(false);
    }

    [ContextMenu("Reset Health To Full")]
    public void ResetHealthToFull()
    {
        EnsureBodyPartSetup(true);
    }

    public void SetUnitDefinition(PrototypeUnitDefinition definition, bool resetHealth = true)
    {
        unitDefinition = definition;
        EnsureBodyPartSetup(resetHealth);
    }

    public void SetArmorLoadout(params ArmorDefinition[] armorDefinitions)
    {
        armorLoadout = CopyArmorLoadoutDefinitions(armorDefinitions);
        SyncArmorLoadout(armorLoadout, true);
        SanitizeArmor();
        skillManager?.RefreshFromEquipment();
    }

    public void SetArmorInstances(System.Collections.Generic.IEnumerable<ArmorInstance> armorInstances)
    {
        armorLoadout = new System.Collections.Generic.List<ArmorDefinition>();
        equippedArmor = new System.Collections.Generic.List<ArmorState>();

        if (armorInstances != null)
        {
            foreach (ArmorInstance armorInstance in armorInstances)
            {
                if (armorInstance == null || armorInstance.Definition == null)
                {
                    continue;
                }

                armorLoadout.Add(armorInstance.Definition);
                var armorState = new ArmorState();
                armorState.ApplyDefinition(armorInstance.Definition, armorInstance.CurrentDurability, armorInstance.InstanceId, armorInstance.Rarity, armorInstance.Affixes, armorInstance.Skills);
                equippedArmor.Add(armorState);
            }
        }

        SanitizeArmorLoadout();
        SanitizeArmor();
        skillManager?.RefreshFromEquipment();
    }

    public bool TryConsumeStamina(float amount)
    {
        float requiredAmount = Mathf.Max(0f, amount);
        if (requiredAmount <= HealthEpsilon)
        {
            return true;
        }

        if (CurrentStamina + HealthEpsilon < requiredAmount)
        {
            return false;
        }

        SpendStamina(requiredAmount);
        return true;
    }

    public bool CanStartStaminaAction(float requiredAmount = 0f)
    {
        if (CurrentStamina <= HealthEpsilon || IsBelowStaminaActionThreshold)
        {
            return false;
        }

        float staminaRequirement = Mathf.Max(0f, requiredAmount);
        return staminaRequirement <= HealthEpsilon || CurrentStamina + HealthEpsilon >= staminaRequirement;
    }

    public float DrainStamina(float amount)
    {
        float requestedAmount = Mathf.Max(0f, amount);
        if (requestedAmount <= HealthEpsilon || CurrentStamina <= HealthEpsilon)
        {
            return 0f;
        }

        float drained = Mathf.Min(CurrentStamina, requestedAmount);
        SpendStamina(drained);
        return drained;
    }

    public void ApplyDamage(string partId, float damage)
    {
        ApplyDamage(partId, DamageInfo.CreateDefault(damage));
    }

    public void ApplyDamage(PrototypeBodyPartType bodyPart, float damage)
    {
        ApplyDamage(MapLegacyPartId(bodyPart), damage);
    }

    public void ApplyDamage(string partId, DamageInfo damageInfo)
    {
        if (IsDead || damageInfo.damage <= 0f)
        {
            return;
        }

        EnsureBodyPartSetup(false);
        if (skillManager != null)
        {
            damageInfo = skillManager.AdjustIncomingDamage(partId, damageInfo);
            if (damageInfo.damage <= 0f && damageInfo.armorDamage <= 0f)
            {
                return;
            }
        }

        pendingDamage.Enqueue(new PendingDamage(partId, damageInfo, DamageOrigin.DirectHit));
        FlushPendingDamage();
    }

    public void ApplyDamage(PrototypeBodyPartType bodyPart, DamageInfo damageInfo)
    {
        ApplyDamage(MapLegacyPartId(bodyPart), damageInfo);
    }

    public float GetCurrentHealth(string partId)
    {
        PartState state = GetBodyPartState(partId);
        return state != null ? state.currentHealth : 0f;
    }

    public float GetCurrentHealth(PrototypeBodyPartType bodyPart)
    {
        return GetCurrentHealth(MapLegacyPartId(bodyPart));
    }

    public bool HasPart(string partId)
    {
        return GetBodyPartState(partId) != null;
    }

    public bool IsPartDestroyed(string partId)
    {
        PartState state = GetBodyPartState(partId);
        return state == null || state.currentHealth <= HealthEpsilon;
    }

    public float GetArmorDurabilityNormalized(string partId)
    {
        ArmorState armorState = GetCoveringArmor(partId);
        return armorState != null ? armorState.DurabilityNormalized : 0f;
    }

    public void SetAllowImpactForceWhenAlive(bool allow)
    {
        allowImpactForceWhenAlive = allow;
    }

    public void ConfigureCharacterBonuses(float healthBonus, float staminaBonus, float healingMultiplier, bool resetResourcesToFull = false)
    {
        characterBonusHealth = Mathf.Max(0f, healthBonus);
        characterBonusStamina = Mathf.Max(0f, staminaBonus);
        medicalEffectivenessMultiplier = Mathf.Max(0.1f, healingMultiplier);
        EnsureBodyPartSetup(resetResourcesToFull);
    }

    public void SetCharacterMovementMultiplier(float moveSpeedMultiplier)
    {
        characterMoveSpeedMultiplier = Mathf.Clamp(moveSpeedMultiplier, 0.4f, 1.8f);
    }

    /// <summary>
    /// 按比例调整所有身体部分的当前血量，保持整体血量比例不变
    /// </summary>
    public void AdjustCurrentHealthByRatio(float healthRatio)
    {
        if (bodyParts == null || bodyParts.Count == 0)
        {
            return;
        }

        healthRatio = Mathf.Clamp01(healthRatio);
        
        for (int index = 0; index < bodyParts.Count; index++)
        {
            PartState state = bodyParts[index];
            if (state != null && state.contributesToUnitHealth && state.maxHealth > HealthEpsilon)
            {
                state.currentHealth = state.maxHealth * healthRatio;
            }
        }
    }

    public bool TryUseMedicalItem(MedicalItemDefinition medicalItem)
    {
        if (medicalItem == null)
        {
            return false;
        }

        EnsureBodyPartSetup(false);
        ResolveStatusEffects(false);

        bool used = false;
        if (medicalItem.RemovesHeavyBleeds > 0 && statusEffects != null)
        {
            used |= statusEffects.RemoveHeavyBleeds(medicalItem.RemovesHeavyBleeds);
        }

        if (medicalItem.RemovesLightBleeds > 0 && statusEffects != null)
        {
            used |= statusEffects.RemoveLightBleeds(medicalItem.RemovesLightBleeds);
        }

        if (medicalItem.CuresFractures > 0 && statusEffects != null)
        {
            used |= statusEffects.RemoveFractures(medicalItem.CuresFractures);
        }

        float healingAmount = medicalItem.GetHealingAmount(TotalMaxHealth) * MedicalEffectivenessMultiplier;
        if (healingAmount > HealthEpsilon)
        {
            used |= RestoreHealth(healingAmount);
        }

        if (medicalItem.PainkillerDuration > HealthEpsilon && statusEffects != null)
        {
            used |= statusEffects.ApplyPainkiller(medicalItem.PainkillerDuration);
        }

        return used;
    }

    public bool TryRestoreHealthFromSkill(float amount)
    {
        return RestoreHealth(amount);
    }

    public void ApplyGlobalDamage(float damage)
    {
        ApplyGlobalDamage(damage, null, string.Empty, string.Empty);
    }

    public void ApplyGlobalDamage(float damage, PrototypeUnitVitals sourceUnit, string sourceDisplayName, string sourceEffectDisplayName)
    {
        if (IsDead || damage <= HealthEpsilon)
        {
            return;
        }

        EnsureBodyPartSetup(false);

        var recipients = new List<PartState>();
        foreach (PartState state in bodyParts)
        {
            if (state != null && state.contributesToUnitHealth && state.currentHealth > HealthEpsilon)
            {
                recipients.Add(state);
            }
        }

        if (recipients.Count == 0)
        {
            return;
        }

        float sharedDamage = damage / recipients.Count;
        foreach (PartState state in recipients)
        {
            var damageInfo = DamageInfo.CreateDefault(sharedDamage);
            damageInfo.bypassArmor = true;
            damageInfo.sourceUnit = sourceUnit;
            damageInfo.sourceDisplayName = sourceDisplayName;
            damageInfo.sourceEffectDisplayName = sourceEffectDisplayName;
            pendingDamage.Enqueue(new PendingDamage(state.partId, damageInfo, DamageOrigin.StatusEffect));
        }

        FlushPendingDamage();
    }

    public string GetLastDamageSourceSummary()
    {
        string sourceName = ResolveDamageSourceName(lastDamageSourceUnit, lastDamageSource.sourceDisplayName);
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return string.Empty;
        }

        if (lastDamageSource.viaStatusEffect && !string.IsNullOrWhiteSpace(lastDamageSource.effectDisplayName))
        {
            return $"{sourceName} via {lastDamageSource.effectDisplayName}";
        }

        return sourceName;
    }

    private void ProcessDamageChunk(PendingDamage chunk)
    {
        if (chunk.amount <= 0f || string.IsNullOrWhiteSpace(chunk.partId))
        {
            return;
        }

        PartState state = GetBodyPartState(chunk.partId);
        if (state == null)
        {
            return;
        }

        float finalDamage = chunk.amount;
        bool penetratedArmor = true;
        ArmorState blockingArmor = null;
        float armorDamageApplied = 0f;
        bool armorBroken = false;

        if (chunk.origin == DamageOrigin.DirectHit && !chunk.bypassArmor)
        {
            finalDamage = ResolveArmorMitigation(state, chunk, out blockingArmor, out penetratedArmor, out armorDamageApplied, out armorBroken);
            if (finalDamage <= HealthEpsilon && blockingArmor == null)
            {
                return;
            }

            if (armorDamageApplied > HealthEpsilon)
            {
                EmitCombatFeedback(CombatFeedback.CreateArmorDamage(state.partId, armorDamageApplied), chunk.origin);
            }

            if (armorBroken)
            {
                EmitCombatFeedback(CombatFeedback.CreateArmorBroken(state.partId), chunk.origin);
            }
        }

        if (ShouldTriggerBlackedFollowUpDeath(state, finalDamage, chunk.origin))
        {
            RecordDamageSource(chunk, state.partId);
            KillAllBodyParts();
            return;
        }

        float damageAppliedToHealth = 0f;
        float overflowDamage = finalDamage;

        if (state.currentHealth > HealthEpsilon)
        {
            float absorbedDamage = Mathf.Min(state.currentHealth, overflowDamage);
            state.currentHealth = Mathf.Max(state.currentHealth - absorbedDamage, 0f);
            overflowDamage -= absorbedDamage;
            damageAppliedToHealth += absorbedDamage;

            if (absorbedDamage > HealthEpsilon)
            {
                RecordDamageSource(chunk, state.partId);
                EmitCombatFeedback(CombatFeedback.CreateHealthDamage(state.partId, absorbedDamage), chunk.origin);
            }

            if (state.currentHealth <= HealthEpsilon && ShouldTriggerZeroKill(state, chunk.origin))
            {
                RecordDamageSource(chunk, state.partId);
                KillAllBodyParts();
                return;
            }
        }

        if (chunk.canApplyAfflictions && damageAppliedToHealth > HealthEpsilon)
        {
            TryApplyAfflictions(state.partId, chunk, penetratedArmor, blockingArmor, damageAppliedToHealth);
        }

        if (overflowDamage <= HealthEpsilon || state.overflowMultiplier <= HealthEpsilon)
        {
            return;
        }

        float distributedDamage = overflowDamage * state.overflowMultiplier;
        DistributeOverflowDamage(state, chunk, distributedDamage);
    }

    private void DistributeOverflowDamage(PartState sourceState, PendingDamage sourceChunk, float damage)
    {
        if (damage <= HealthEpsilon || sourceState == null)
        {
            return;
        }

        var recipientParts = new List<PartState>();
        var recipientWeights = new List<float>();
        float totalWeight = 0f;

        if (sourceState.overflowTargets != null && sourceState.overflowTargets.Count > 0)
        {
            foreach (OverflowRouteState overflowRoute in sourceState.overflowTargets)
            {
                if (overflowRoute == null || overflowRoute.weight <= 0f)
                {
                    continue;
                }

                PartState targetState = GetBodyPartState(overflowRoute.partId);
                if (!CanReceiveOverflowDamage(targetState, sourceState.partId))
                {
                    continue;
                }

                recipientParts.Add(targetState);
                recipientWeights.Add(overflowRoute.weight);
                totalWeight += overflowRoute.weight;
            }
        }
        else
        {
            foreach (PartState candidateState in bodyParts)
            {
                if (!CanReceiveOverflowDamage(candidateState, sourceState.partId))
                {
                    continue;
                }

                recipientParts.Add(candidateState);
                recipientWeights.Add(1f);
                totalWeight += 1f;
            }
        }

        if (totalWeight <= HealthEpsilon)
        {
            return;
        }

        for (int index = 0; index < recipientParts.Count; index++)
        {
            float sharedDamage = damage * (recipientWeights[index] / totalWeight);
            PendingDamage overflowChunk = new PendingDamage(recipientParts[index].partId, sharedDamage, DamageOrigin.OverflowShare)
            {
                sourceUnit = sourceChunk.sourceUnit,
                sourceDisplayName = sourceChunk.sourceDisplayName,
                sourceEffectDisplayName = sourceChunk.sourceEffectDisplayName
            };
            pendingDamage.Enqueue(overflowChunk);
        }
    }

    private float ResolveArmorMitigation(
        PartState targetPart,
        PendingDamage chunk,
        out ArmorState blockingArmor,
        out bool penetratedArmor,
        out float armorDamageApplied,
        out bool armorBroken)
    {
        blockingArmor = GetCoveringArmor(targetPart != null ? targetPart.partId : string.Empty);
        penetratedArmor = true;
        armorDamageApplied = 0f;
        armorBroken = false;

        if (targetPart == null || blockingArmor == null || blockingArmor.definition == null)
        {
            return chunk.amount;
        }

        float damageReduction = Mathf.Clamp01(blockingArmor.DamageReduction);
        float durabilityRatio = blockingArmor.DurabilityNormalized;
        float effectiveArmorStrength = blockingArmor.EffectiveArmorClass * 10f * Mathf.Lerp(0.45f, 1f, durabilityRatio);
        float penetrationRatio = chunk.penetrationPower / Mathf.Max(1f, effectiveArmorStrength);
        float penetrationChance = Mathf.Clamp01(Mathf.InverseLerp(0.58f, 1.12f, penetrationRatio));
        penetratedArmor = penetrationRatio >= 1f || UnityEngine.Random.value < penetrationChance;

        float durabilityLossMultiplier = penetratedArmor
            ? blockingArmor.definition.PenetratedDurabilityLossMultiplier
            : blockingArmor.definition.BlockedDurabilityLossMultiplier;
        float durabilityLoss = Mathf.Max(chunk.armorDamage, chunk.amount * 0.35f) * durabilityLossMultiplier;
        float previousDurability = blockingArmor.currentDurability;
        blockingArmor.currentDurability = Mathf.Max(0f, blockingArmor.currentDurability - durabilityLoss);
        armorDamageApplied = Mathf.Max(0f, previousDurability - blockingArmor.currentDurability);
        armorBroken = previousDurability > HealthEpsilon && blockingArmor.currentDurability <= HealthEpsilon;

        if (penetratedArmor)
        {
            float penetrationDamageFactor = Mathf.Lerp(0.45f, 1f, penetrationChance);
            return chunk.amount * penetrationDamageFactor * (1f - damageReduction);
        }

        float bluntFactor = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(penetrationRatio));
        return chunk.amount * blockingArmor.definition.BluntDamageMultiplier * bluntFactor * (1f - damageReduction);
    }

    private void TryApplyAfflictions(string partId, PendingDamage chunk, bool penetratedArmor, ArmorState blockingArmor, float damageAppliedToHealth)
    {
        ResolveStatusEffects(false);
        if (statusEffects == null)
        {
            return;
        }

        float armorBleedProtection = blockingArmor != null && blockingArmor.definition != null
            ? blockingArmor.EffectiveBleedProtection
            : 0f;
        float armorFractureProtection = blockingArmor != null && blockingArmor.definition != null
            ? blockingArmor.EffectiveFractureProtection
            : 0f;
        float mitigationFactor = penetratedArmor ? 0.5f : 1f;

        statusEffects.TryApplyCombatDebuffs(
            chunk.lightBleedChance * (1f - armorBleedProtection * mitigationFactor),
            chunk.heavyBleedChance * (1f - armorBleedProtection * mitigationFactor),
            chunk.fractureChance * (1f - armorFractureProtection * mitigationFactor),
            damageAppliedToHealth,
            chunk.sourceUnit,
            ResolveDamageSourceName(chunk.sourceUnit, chunk.sourceDisplayName));
    }

    private bool CanReceiveOverflowDamage(PartState state, string sourcePartId)
    {
        if (state == null || PartIdEquals(state.partId, sourcePartId))
        {
            return false;
        }

        if (state.currentHealth > HealthEpsilon)
        {
            return state.receivesOverflowDamage;
        }

        return state.receivesOverflowFollowUpDamage;
    }

    private bool ShouldTriggerZeroKill(PartState state, DamageOrigin origin)
    {
        switch (state.zeroKillMode)
        {
            case PrototypeUnitDefinition.ZeroKillMode.OnAnyDamage:
                return true;
            case PrototypeUnitDefinition.ZeroKillMode.OnDirectHitOnly:
                return origin == DamageOrigin.DirectHit;
            default:
                return false;
        }
    }

    private bool ShouldTriggerBlackedFollowUpDeath(PartState state, float damage, DamageOrigin origin)
    {
        return state != null
            && state.killUnitWhenBlackedAndDamagedAgain
            && state.currentHealth <= HealthEpsilon
            && origin != DamageOrigin.StatusEffect
            && damage > state.blackedFollowUpDamageThreshold;
    }

    private void RecordDamageSource(PendingDamage chunk, string partId)
    {
        string sourceName = ResolveDamageSourceName(chunk.sourceUnit, chunk.sourceDisplayName);
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return;
        }

        lastDamageSourceUnit = chunk.sourceUnit;
        lastDamageSource = new DamageSourceSnapshot
        {
            sourceDisplayName = sourceName,
            effectDisplayName = chunk.origin == DamageOrigin.StatusEffect
                ? chunk.sourceEffectDisplayName
                : string.Empty,
            partId = NormalizePartId(partId),
            viaStatusEffect = chunk.origin == DamageOrigin.StatusEffect
        };
    }

    private static string ResolveDamageSourceName(PrototypeUnitVitals sourceUnit, string fallbackName)
    {
        if (sourceUnit != null)
        {
            return sourceUnit.gameObject != null ? sourceUnit.gameObject.name : sourceUnit.name;
        }

        return string.IsNullOrWhiteSpace(fallbackName) ? string.Empty : fallbackName.Trim();
    }

    private void KillAllBodyParts()
    {
        foreach (PartState state in bodyParts)
        {
            state.currentHealth = 0f;
        }

        pendingDamage.Clear();
        TryTriggerDeath();
    }

    private void TryTriggerDeath()
    {
        if (IsDead)
        {
            return;
        }

        bool hasVitalParts = false;
        foreach (PartState state in bodyParts)
        {
            if (!state.contributesToUnitHealth)
            {
                continue;
            }

            hasVitalParts = true;
            if (state.currentHealth > HealthEpsilon)
            {
                return;
            }
        }

        if (!hasVitalParts)
        {
            return;
        }

        IsDead = true;
        onDied.Invoke();
        Died?.Invoke(this);
    }

    private void EmitCombatFeedback(CombatFeedback feedback, DamageOrigin origin)
    {
        if (origin != DamageOrigin.DirectHit)
        {
            return;
        }

        CombatFeedbackGenerated?.Invoke(feedback);
    }

    private void FlushPendingDamage()
    {
        if (isApplyingDamage)
        {
            return;
        }

        isApplyingDamage = true;

        while (pendingDamage.Count > 0 && !IsDead)
        {
            PendingDamage chunk = pendingDamage.Dequeue();
            ProcessDamageChunk(chunk);
        }

        pendingDamage.Clear();
        isApplyingDamage = false;
        TryTriggerDeath();
    }

    private PartState GetBodyPartState(string partId)
    {
        string normalizedPartId = NormalizePartId(partId);
        if (string.IsNullOrWhiteSpace(normalizedPartId))
        {
            return null;
        }

        foreach (PartState state in bodyParts)
        {
            if (PartIdEquals(state.partId, normalizedPartId))
            {
                return state;
            }
        }

        return null;
    }

    private ArmorState GetCoveringArmor(string partId)
    {
        string normalizedPartId = NormalizePartId(partId);
        if (string.IsNullOrWhiteSpace(normalizedPartId))
        {
            return null;
        }

        ArmorState bestArmor = null;
        foreach (ArmorState armorState in equippedArmor)
        {
            if (armorState == null || !armorState.CoversPart(normalizedPartId))
            {
                continue;
            }

            if (bestArmor == null
                || armorState.EffectiveArmorClass > bestArmor.EffectiveArmorClass
                || (armorState.definition == bestArmor.definition && armorState.currentDurability > bestArmor.currentDurability))
            {
                bestArmor = armorState;
            }
        }

        return bestArmor;
    }

    private void EnsureBodyPartSetup(bool resetHealth)
    {
        if (bodyParts == null)
        {
            bodyParts = new List<PartState>();
        }

        if (equippedArmor == null)
        {
            equippedArmor = new List<ArmorState>();
        }

        if (armorLoadout == null)
        {
            armorLoadout = new List<ArmorDefinition>();
        }

        if (unitDefinition != null && unitDefinition.Parts.Count > 0)
        {
            SyncFromDefinition(resetHealth);
        }
        else if (bodyParts.Count == 0)
        {
            BuildFallbackHumanoidParts();
        }

        SanitizeBodyParts();
        MigrateLegacyArmorLoadoutIfNeeded();
        SanitizeArmorLoadout();
        SyncArmorLoadout(armorLoadout, resetHealth);
        SanitizeArmor();
        SanitizeStamina(resetHealth);
        ResolveStatusEffects(resetHealth);

        if (resetHealth)
        {
            ResetPartHealthToFull();
            ResetStaminaToFull();
        }
        else
        {
            IsDead = !HasLivingVitalPart();
        }
    }

    private void MigrateLegacyArmorLoadoutIfNeeded()
    {
        if (armorLoadout != null && armorLoadout.Count > 0)
        {
            return;
        }

        if (equippedArmor == null || equippedArmor.Count == 0)
        {
            return;
        }

        armorLoadout = new List<ArmorDefinition>();
        foreach (ArmorState armorState in equippedArmor)
        {
            if (armorState?.definition != null)
            {
                armorLoadout.Add(armorState.definition);
            }
        }
    }

    private void SyncFromDefinition(bool resetHealth)
    {
        var preservedHealthByPartId = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (PartState existingState in bodyParts)
        {
            string normalizedPartId = NormalizePartId(existingState.partId);
            if (!string.IsNullOrWhiteSpace(normalizedPartId))
            {
                preservedHealthByPartId[normalizedPartId] = existingState.currentHealth;
            }
        }

        var updatedStates = new List<PartState>();
        foreach (PrototypeUnitDefinition.PartDefinition partDefinition in unitDefinition.Parts)
        {
            if (partDefinition == null || string.IsNullOrWhiteSpace(partDefinition.partId))
            {
                continue;
            }

            float currentHealth = partDefinition.maxHealth;
            if (!resetHealth && preservedHealthByPartId.TryGetValue(partDefinition.partId, out float preservedHealth))
            {
                currentHealth = preservedHealth;
            }

            var runtimeState = new PartState();
            runtimeState.ApplyDefinition(partDefinition, currentHealth);
            updatedStates.Add(runtimeState);
        }

        bodyParts = updatedStates;
    }

    private void SyncArmorLoadout(IEnumerable<ArmorDefinition> armorDefinitions, bool resetDurability)
    {
        if (equippedArmor == null)
        {
            equippedArmor = new List<ArmorState>();
        }

        var preservedDurability = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (ArmorState existingArmor in equippedArmor)
        {
            if (existingArmor == null || existingArmor.definition == null)
            {
                continue;
            }

            preservedDurability[existingArmor.definition.ItemId] = existingArmor.currentDurability;
        }

        var updatedArmor = new List<ArmorState>();
        if (armorDefinitions != null)
        {
            foreach (ArmorDefinition armorDefinition in armorDefinitions)
            {
                if (armorDefinition == null)
                {
                    continue;
                }

                float currentDurability = armorDefinition.MaxDurability;
                if (!resetDurability && preservedDurability.TryGetValue(armorDefinition.ItemId, out float preservedValue))
                {
                    currentDurability = preservedValue;
                }

                var armorState = new ArmorState();
                armorState.ApplyDefinition(armorDefinition, currentDurability, null, ItemRarity.Common, null);
                updatedArmor.Add(armorState);
            }
        }

        equippedArmor = updatedArmor;
    }

    private void ApplyArmorHealthBonuses(bool resetHealth)
    {
        if (bodyParts == null || bodyParts.Count == 0)
        {
            return;
        }

        var bonusHealthByPartId = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        if (equippedArmor != null)
        {
            for (int armorIndex = 0; armorIndex < equippedArmor.Count; armorIndex++)
            {
                ArmorState armorState = equippedArmor[armorIndex];
                if (armorState == null || armorState.definition == null || armorState.BonusHealth <= HealthEpsilon)
                {
                    continue;
                }

                IReadOnlyList<string> coveredPartIds = armorState.definition.CoveredPartIds;
                if (coveredPartIds == null || coveredPartIds.Count == 0)
                {
                    continue;
                }

                float sharedBonus = armorState.BonusHealth / coveredPartIds.Count;
                for (int partIndex = 0; partIndex < coveredPartIds.Count; partIndex++)
                {
                    string coveredPartId = NormalizePartId(coveredPartIds[partIndex]);
                    if (string.IsNullOrWhiteSpace(coveredPartId))
                    {
                        continue;
                    }

                    bonusHealthByPartId.TryGetValue(coveredPartId, out float existingBonus);
                    bonusHealthByPartId[coveredPartId] = existingBonus + sharedBonus;
                }
            }
        }

        int vitalPartCount = 0;
        for (int partIndex = 0; partIndex < bodyParts.Count; partIndex++)
        {
            PartState state = bodyParts[partIndex];
            if (state != null && state.contributesToUnitHealth)
            {
                vitalPartCount++;
            }
        }

        float sharedCharacterBonus = vitalPartCount > 0
            ? Mathf.Max(0f, characterBonusHealth) / vitalPartCount
            : 0f;

        for (int partIndex = 0; partIndex < bodyParts.Count; partIndex++)
        {
            PartState state = bodyParts[partIndex];
            if (state == null)
            {
                continue;
            }

            string normalizedPartId = NormalizePartId(state.partId);
            float bonusHealth = 0f;
            if (!string.IsNullOrWhiteSpace(normalizedPartId))
            {
                bonusHealthByPartId.TryGetValue(normalizedPartId, out bonusHealth);
            }

            if (state.contributesToUnitHealth)
            {
                bonusHealth += sharedCharacterBonus;
            }

            state.baseMaxHealth = Mathf.Max(1f, state.baseMaxHealth > HealthEpsilon ? state.baseMaxHealth : state.maxHealth);
            state.armorBonusHealth = Mathf.Max(0f, bonusHealth);
            state.maxHealth = Mathf.Max(1f, state.baseMaxHealth + state.armorBonusHealth);
            state.currentHealth = resetHealth ? state.maxHealth : Mathf.Clamp(state.currentHealth, 0f, state.maxHealth);
        }
    }

    private void RecalculateEquipmentModifiers()
    {
        // 护甲词条的移速收益已经并入 CharacterStatAggregator，这里保留 1x 基线，
        // 避免装备词条和成长汇总在局内被重复计算。
        equipmentMoveSpeedMultiplier = 1f;
    }

    private void SanitizeArmorLoadout()
    {
        armorLoadout = CopyArmorLoadoutDefinitions(armorLoadout);
    }

    private void SyncAfflictionStates(bool resetStatuses)
    {
        ResolveStatusEffects(resetStatuses);
    }

    private void ApplyAfflictionDamage(float deltaTime)
    {
        ResolveStatusEffects(false);
    }

    private bool RemoveBleeds(bool heavy, int count)
    {
        if (statusEffects == null || count <= 0)
        {
            return false;
        }

        return heavy ? statusEffects.RemoveHeavyBleeds(count) : statusEffects.RemoveLightBleeds(count);
    }

    private bool CureFractures(int count)
    {
        return statusEffects != null && count > 0 && statusEffects.RemoveFractures(count);
    }

    private bool RestoreHealth(float amount)
    {
        if (amount <= HealthEpsilon || bodyParts == null || bodyParts.Count == 0)
        {
            return false;
        }

        var healableParts = new List<PartState>();
        foreach (PartState state in bodyParts)
        {
            if (state == null
                || !state.contributesToUnitHealth
                || state.currentHealth <= HealthEpsilon
                || state.currentHealth >= state.maxHealth - HealthEpsilon)
            {
                continue;
            }

            healableParts.Add(state);
        }

        if (healableParts.Count == 0)
        {
            return false;
        }

        healableParts.Sort((left, right) =>
        {
            float leftRatio = left.maxHealth > HealthEpsilon ? left.currentHealth / left.maxHealth : 1f;
            float rightRatio = right.maxHealth > HealthEpsilon ? right.currentHealth / right.maxHealth : 1f;
            return leftRatio.CompareTo(rightRatio);
        });

        float remaining = amount;
        bool healedAny = false;
        foreach (PartState state in healableParts)
        {
            if (remaining <= HealthEpsilon)
            {
                break;
            }

            float missingHealth = state.maxHealth - state.currentHealth;
            if (missingHealth <= HealthEpsilon)
            {
                continue;
            }

            float restored = Mathf.Min(missingHealth, remaining);
            state.currentHealth = Mathf.Clamp(state.currentHealth + restored, 0f, state.maxHealth);
            remaining -= restored;
            healedAny = true;
        }

        if (healedAny)
        {
            IsDead = false;
        }

        return healedAny;
    }

    private bool HasAnyAffliction(Func<PartAfflictionState, bool> predicate)
    {
        return false;
    }

    private bool CanPartFracture(string partId)
    {
        return !string.IsNullOrWhiteSpace(NormalizePartId(partId));
    }

    private void SanitizeArmor()
    {
        if (equippedArmor == null)
        {
            equippedArmor = new List<ArmorState>();
            return;
        }

        var seenArmorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = equippedArmor.Count - 1; index >= 0; index--)
        {
            ArmorState armorState = equippedArmor[index];
            if (armorState == null || armorState.definition == null)
            {
                equippedArmor.RemoveAt(index);
                continue;
            }

            armorState.Sanitize();
            string armorId = armorState.definition.ItemId;
            if (string.IsNullOrWhiteSpace(armorId) || !seenArmorIds.Add(armorId))
            {
                equippedArmor.RemoveAt(index);
            }
        }

        ApplyArmorHealthBonuses(false);
        RecalculateEquipmentModifiers();
    }

    private void SanitizeStamina(bool resetToFull)
    {
        maxStamina = Mathf.Max(1f, maxStamina);
        characterBonusHealth = Mathf.Max(0f, characterBonusHealth);
        characterBonusStamina = Mathf.Max(0f, characterBonusStamina);
        medicalEffectivenessMultiplier = Mathf.Max(0.1f, medicalEffectivenessMultiplier);
        characterMoveSpeedMultiplier = Mathf.Clamp(characterMoveSpeedMultiplier, 0.4f, 1.8f);
        staminaRecoveryPerSecond = Mathf.Max(0f, staminaRecoveryPerSecond);
        staminaActionThresholdNormalized = Mathf.Clamp01(staminaActionThresholdNormalized);
        staminaRecoveryDelay = Mathf.Max(0f, staminaRecoveryDelay);
        exhaustionRecoveryDelay = Mathf.Max(0f, exhaustionRecoveryDelay);
        staminaRecoveryBlockedTimer = Mathf.Max(0f, staminaRecoveryBlockedTimer);
        currentStamina = resetToFull ? MaxStamina : Mathf.Clamp(currentStamina, 0f, MaxStamina);
    }

    private void ResetStaminaToFull()
    {
        currentStamina = MaxStamina;
        staminaRecoveryBlockedTimer = 0f;
    }

    private void SpendStamina(float amount)
    {
        float staminaCost = Mathf.Max(0f, amount);
        if (staminaCost <= HealthEpsilon)
        {
            return;
        }

        currentStamina = Mathf.Clamp(currentStamina - staminaCost, 0f, MaxStamina);
        if (currentStamina <= HealthEpsilon)
        {
            currentStamina = 0f;
            staminaRecoveryBlockedTimer = exhaustionRecoveryDelay;
        }
        else
        {
            staminaRecoveryBlockedTimer = staminaRecoveryDelay;
        }
    }

    private void UpdateStamina(float deltaTime)
    {
        if (deltaTime <= 0f || IsDead)
        {
            return;
        }

        if (staminaRecoveryBlockedTimer > 0f)
        {
            staminaRecoveryBlockedTimer = Mathf.Max(0f, staminaRecoveryBlockedTimer - deltaTime);
            return;
        }

        if (currentStamina >= MaxStamina - HealthEpsilon)
        {
            currentStamina = MaxStamina;
            return;
        }

        currentStamina = Mathf.Min(MaxStamina, currentStamina + staminaRecoveryPerSecond * deltaTime);
    }

    private static List<ArmorDefinition> CopyArmorLoadoutDefinitions(IEnumerable<ArmorDefinition> armorDefinitions)
    {
        var sanitizedDefinitions = new List<ArmorDefinition>();
        if (armorDefinitions == null)
        {
            return sanitizedDefinitions;
        }

        var seenArmorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ArmorDefinition armorDefinition in armorDefinitions)
        {
            if (armorDefinition == null)
            {
                continue;
            }

            string armorId = string.IsNullOrWhiteSpace(armorDefinition.ItemId)
                ? armorDefinition.name
                : armorDefinition.ItemId;

            if (!seenArmorIds.Add(armorId))
            {
                continue;
            }

            sanitizedDefinitions.Add(armorDefinition);
        }

        return sanitizedDefinitions;
    }

    private void BuildFallbackHumanoidParts()
    {
        bodyParts = new List<PartState>
        {
            CreateFallbackPart(
                FallbackHeadPartId,
                "头部",
                35f,
                1f,
                true,
                true,
                true,
                PrototypeUnitDefinition.ZeroKillMode.OnDirectHitOnly,
                true,
                0f),
            CreateFallbackPart(
                FallbackTorsoPartId,
                "躯体",
                155f,
                1.05f,
                true,
                true,
                false,
                PrototypeUnitDefinition.ZeroKillMode.Never,
                false,
                0f),
            CreateFallbackPart(
                FallbackLegsPartId,
                "腿部",
                130f,
                0.7f,
                true,
                true,
                false,
                PrototypeUnitDefinition.ZeroKillMode.Never,
                false,
                0f)
        };
    }

    private PartState CreateFallbackPart(
        string partId,
        string displayName,
        float maxHealth,
        float overflowMultiplier,
        bool contributesToUnitHealth,
        bool receivesOverflowDamage,
        bool receivesOverflowFollowUpDamage,
        PrototypeUnitDefinition.ZeroKillMode zeroKillMode,
        bool killUnitWhenBlackedAndDamagedAgain,
        float blackedFollowUpDamageThreshold)
    {
        return new PartState
        {
            legacyBodyPart = MapLegacyBodyPart(partId),
            partId = NormalizePartId(partId),
            displayName = displayName,
            baseMaxHealth = maxHealth,
            armorBonusHealth = 0f,
            maxHealth = maxHealth,
            currentHealth = maxHealth,
            overflowMultiplier = overflowMultiplier,
            contributesToUnitHealth = contributesToUnitHealth,
            receivesOverflowDamage = receivesOverflowDamage,
            receivesOverflowFollowUpDamage = receivesOverflowFollowUpDamage,
            zeroKillMode = zeroKillMode,
            killUnitWhenBlackedAndDamagedAgain = killUnitWhenBlackedAndDamagedAgain,
            blackedFollowUpDamageThreshold = blackedFollowUpDamageThreshold,
            overflowTargets = new List<OverflowRouteState>()
        };
    }

    private void SanitizeBodyParts()
    {
        var seenPartIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = bodyParts.Count - 1; index >= 0; index--)
        {
            PartState state = bodyParts[index];
            if (state == null)
            {
                bodyParts.RemoveAt(index);
                continue;
            }

            state.Sanitize();
            if (string.IsNullOrWhiteSpace(state.partId) || !seenPartIds.Add(state.partId))
            {
                bodyParts.RemoveAt(index);
            }
        }
    }

    private void ResetPartHealthToFull()
    {
        foreach (PartState state in bodyParts)
        {
            state.currentHealth = state.maxHealth;
        }

        if (statusEffects != null)
        {
            statusEffects.ResetAllEffects();
        }

        lastDamageSource = default;
        lastDamageSourceUnit = null;
        pendingDamage.Clear();
        isApplyingDamage = false;
        IsDead = false;
    }

    private bool HasLivingVitalPart()
    {
        bool hasVitalParts = false;
        foreach (PartState state in bodyParts)
        {
            if (!state.contributesToUnitHealth)
            {
                continue;
            }

            hasVitalParts = true;
            if (state.currentHealth > HealthEpsilon)
            {
                return true;
            }
        }

        return !hasVitalParts;
    }

    private string ResolveHealthBarAnchorPartId()
    {
        string preferredPartId = unitDefinition != null
            ? NormalizePartId(unitDefinition.HealthBarAnchorPartId)
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(preferredPartId) && GetBodyPartState(preferredPartId) != null)
        {
            return preferredPartId;
        }

        foreach (PartState state in bodyParts)
        {
            if (state.contributesToUnitHealth)
            {
                return state.partId;
            }
        }

        return bodyParts.Count > 0 ? bodyParts[0].partId : string.Empty;
    }

    private void ResolveStatusEffects(bool resetStatuses)
    {
        if (statusEffects == null)
        {
            statusEffects = GetComponent<PrototypeStatusEffectController>();

            if (statusEffects == null && bootstrapStatusEffects && Application.isPlaying)
            {
                statusEffects = gameObject.AddComponent<PrototypeStatusEffectController>();
            }
        }

        if (statusEffects == null)
        {
            return;
        }

        statusEffects.Bind(this);
        if (resetStatuses)
        {
            statusEffects.ResetAllEffects();
        }
    }

    private void ResolveRuntimeDependencies()
    {
        if (skillManager == null)
        {
            skillManager = GetComponent<PlayerSkillManager>();
        }
    }

    private static string NormalizePartId(string partId)
    {
        return PrototypeUnitDefinition.NormalizePartId(partId);
    }

    private static bool PartIdEquals(string left, string right)
    {
        return string.Equals(NormalizePartId(left), NormalizePartId(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string MapLegacyPartId(PrototypeBodyPartType bodyPart)
    {
        switch (bodyPart)
        {
            case PrototypeBodyPartType.Head:
                return FallbackHeadPartId;
            case PrototypeBodyPartType.Torso:
                return FallbackTorsoPartId;
            case PrototypeBodyPartType.Legs:
                return FallbackLegsPartId;
            default:
                return string.Empty;
        }
    }

    private static PrototypeBodyPartType MapLegacyBodyPart(string partId)
    {
        switch (NormalizePartId(partId))
        {
            case FallbackHeadPartId:
                return PrototypeBodyPartType.Head;
            case FallbackTorsoPartId:
                return PrototypeBodyPartType.Torso;
            case FallbackLegsPartId:
                return PrototypeBodyPartType.Legs;
            default:
                return PrototypeBodyPartType.Torso;
        }
    }
}
