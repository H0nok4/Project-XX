using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PrototypeStatusEffectController : MonoBehaviour
{
    public const string LightBleedEffectId = "light_bleed";
    public const string HeavyBleedEffectId = "heavy_bleed";
    public const string FractureEffectId = "fracture";
    public const string PainkillerEffectId = "painkiller";

    [Serializable]
    public class StatusEffectState
    {
        public string effectId = string.Empty;
        public string displayName = string.Empty;
        public bool isDebuff = true;
        [Min(0f)] public float remainingDuration;
        public bool isPersistent;
        [Min(0f)] public float tickDamagePerSecond;
        [SerializeField, HideInInspector] public string sourceDisplayName = string.Empty;
        [NonSerialized] public PrototypeUnitVitals sourceUnit;

        public void Configure(string id, string label, bool debuff, float durationSeconds, bool persistent, float tickDamage)
        {
            effectId = NormalizeEffectId(id);
            displayName = string.IsNullOrWhiteSpace(label) ? effectId : label.Trim();
            isDebuff = debuff;
            remainingDuration = Mathf.Max(0f, durationSeconds);
            isPersistent = persistent;
            tickDamagePerSecond = Mathf.Max(0f, tickDamage);
        }

        public void Sanitize()
        {
            effectId = NormalizeEffectId(effectId);
            displayName = string.IsNullOrWhiteSpace(displayName) ? effectId : displayName.Trim();
            remainingDuration = Mathf.Max(0f, remainingDuration);
            tickDamagePerSecond = Mathf.Max(0f, tickDamagePerSecond);
        }

        public bool IsExpired => !isPersistent && remainingDuration <= 0f;

        public string ResolveSourceDisplayName()
        {
            if (sourceUnit != null)
            {
                return sourceUnit.gameObject != null ? sourceUnit.gameObject.name : sourceUnit.name;
            }

            return string.IsNullOrWhiteSpace(sourceDisplayName) ? string.Empty : sourceDisplayName.Trim();
        }
    }

    [Header("References")]
    [SerializeField] private PrototypeUnitVitals vitals;
    [SerializeField] private List<StatusEffectState> activeEffects = new List<StatusEffectState>();

    [Header("Built-In Debuffs")]
    [SerializeField] private float lightBleedDamagePerSecond = 1.25f;
    [SerializeField] private float heavyBleedDamagePerSecond = 3.5f;
    [SerializeField] private float heavyBleedMinDamage = 8f;
    [SerializeField] private float lightBleedMinDamage = 3f;
    [SerializeField] private float fractureMinDamage = 7f;
    [SerializeField] private float fractureMoveSpeedMultiplier = 0.72f;
    [SerializeField] private float fractureJumpMultiplier = 0.6f;

    public IReadOnlyList<StatusEffectState> ActiveEffects => activeEffects;
    public bool HasLightBleed => HasEffect(LightBleedEffectId);
    public bool HasHeavyBleed => HasEffect(HeavyBleedEffectId);
    public bool HasAnyBleed => HasLightBleed || HasHeavyBleed;
    public bool HasFracture => HasEffect(FractureEffectId);
    public bool IsPainkillerActive => HasEffect(PainkillerEffectId);
    public float PainkillerRemaining => GetRemainingDuration(PainkillerEffectId);
    public float MovementPenaltyMultiplier => IsPainkillerActive || !HasFracture ? 1f : fractureMoveSpeedMultiplier;
    public float JumpPenaltyMultiplier => IsPainkillerActive || !HasFracture ? 1f : fractureJumpMultiplier;

    private void Awake()
    {
        ResolveVitals();
        SanitizeEffects();
    }

    private void Update()
    {
        if (vitals == null || vitals.IsDead)
        {
            return;
        }

        TickDurations(Time.deltaTime);
        ApplyDamageOverTime(Time.deltaTime);
        RemoveExpiredEffects();
    }

    private void OnValidate()
    {
        ResolveVitals();
        SanitizeEffects();
        lightBleedDamagePerSecond = Mathf.Max(0f, lightBleedDamagePerSecond);
        heavyBleedDamagePerSecond = Mathf.Max(0f, heavyBleedDamagePerSecond);
        heavyBleedMinDamage = Mathf.Max(0f, heavyBleedMinDamage);
        lightBleedMinDamage = Mathf.Max(0f, lightBleedMinDamage);
        fractureMinDamage = Mathf.Max(0f, fractureMinDamage);
        fractureMoveSpeedMultiplier = Mathf.Clamp(fractureMoveSpeedMultiplier, 0.1f, 1f);
        fractureJumpMultiplier = Mathf.Clamp(fractureJumpMultiplier, 0.1f, 1f);
    }

    public void Bind(PrototypeUnitVitals ownerVitals)
    {
        vitals = ownerVitals;
    }

    public void ResetAllEffects()
    {
        activeEffects.Clear();
    }

    public bool HasEffect(string effectId)
    {
        return GetEffect(effectId) != null;
    }

    public float GetRemainingDuration(string effectId)
    {
        StatusEffectState effect = GetEffect(effectId);
        return effect != null && !effect.isPersistent ? effect.remainingDuration : 0f;
    }

    public bool RemoveLightBleeds(int count)
    {
        return count > 0 && RemoveEffect(LightBleedEffectId);
    }

    public bool RemoveHeavyBleeds(int count)
    {
        bool removed = false;
        if (count > 0)
        {
            removed |= RemoveEffect(HeavyBleedEffectId);
        }

        return removed;
    }

    public bool RemoveFractures(int count)
    {
        return count > 0 && RemoveEffect(FractureEffectId);
    }

    public bool ApplyPainkiller(float durationSeconds)
    {
        if (durationSeconds <= 0f)
        {
            return false;
        }

        ApplyOrRefreshTimedEffect(PainkillerEffectId, "Painkiller", false, durationSeconds, 0f);
        return true;
    }

    public void TryApplyCombatDebuffs(
        float lightBleedChance,
        float heavyBleedChance,
        float fractureChance,
        float appliedDamage,
        PrototypeUnitVitals sourceUnit = null,
        string sourceDisplayName = "")
    {
        if (appliedDamage <= 0f)
        {
            return;
        }

        if (appliedDamage >= heavyBleedMinDamage && !HasHeavyBleed && UnityEngine.Random.value < Mathf.Clamp01(heavyBleedChance))
        {
            RemoveEffect(LightBleedEffectId);
            ApplyOrRefreshPersistentEffect(HeavyBleedEffectId, "Heavy Bleed", true, heavyBleedDamagePerSecond, sourceUnit, sourceDisplayName);
        }
        else if (appliedDamage >= lightBleedMinDamage && !HasAnyBleed && UnityEngine.Random.value < Mathf.Clamp01(lightBleedChance))
        {
            ApplyOrRefreshPersistentEffect(LightBleedEffectId, "Light Bleed", true, lightBleedDamagePerSecond, sourceUnit, sourceDisplayName);
        }

        if (appliedDamage >= fractureMinDamage && !HasFracture && UnityEngine.Random.value < Mathf.Clamp01(fractureChance))
        {
            ApplyOrRefreshPersistentEffect(FractureEffectId, "Fracture", true, 0f, sourceUnit, sourceDisplayName);
        }
    }

    public void ApplyOrRefreshTimedEffect(string effectId, string displayName, bool isDebuff, float durationSeconds, float tickDamagePerSecond = 0f)
    {
        ApplyOrRefreshTimedEffect(effectId, displayName, isDebuff, durationSeconds, tickDamagePerSecond, null, string.Empty);
    }

    public void ApplyOrRefreshTimedEffect(
        string effectId,
        string displayName,
        bool isDebuff,
        float durationSeconds,
        float tickDamagePerSecond,
        PrototypeUnitVitals sourceUnit,
        string sourceDisplayName)
    {
        StatusEffectState effect = GetOrCreateEffect(effectId);
        effect.Configure(effectId, displayName, isDebuff, durationSeconds, false, tickDamagePerSecond);
        effect.sourceUnit = sourceUnit;
        effect.sourceDisplayName = string.IsNullOrWhiteSpace(sourceDisplayName) ? effect.sourceDisplayName : sourceDisplayName.Trim();
    }

    public void ApplyOrRefreshPersistentEffect(string effectId, string displayName, bool isDebuff, float tickDamagePerSecond = 0f)
    {
        ApplyOrRefreshPersistentEffect(effectId, displayName, isDebuff, tickDamagePerSecond, null, string.Empty);
    }

    public void ApplyOrRefreshPersistentEffect(
        string effectId,
        string displayName,
        bool isDebuff,
        float tickDamagePerSecond,
        PrototypeUnitVitals sourceUnit,
        string sourceDisplayName)
    {
        StatusEffectState effect = GetOrCreateEffect(effectId);
        effect.Configure(effectId, displayName, isDebuff, 0f, true, tickDamagePerSecond);
        effect.sourceUnit = sourceUnit;
        effect.sourceDisplayName = string.IsNullOrWhiteSpace(sourceDisplayName) ? effect.sourceDisplayName : sourceDisplayName.Trim();
    }

    public bool RemoveEffect(string effectId)
    {
        string normalizedEffectId = NormalizeEffectId(effectId);
        if (string.IsNullOrWhiteSpace(normalizedEffectId))
        {
            return false;
        }

        for (int index = activeEffects.Count - 1; index >= 0; index--)
        {
            StatusEffectState effect = activeEffects[index];
            if (effect == null)
            {
                activeEffects.RemoveAt(index);
                continue;
            }

            if (string.Equals(effect.effectId, normalizedEffectId, StringComparison.OrdinalIgnoreCase))
            {
                activeEffects.RemoveAt(index);
                return true;
            }
        }

        return false;
    }

    private void ResolveVitals()
    {
        if (vitals == null)
        {
            vitals = GetComponent<PrototypeUnitVitals>();
        }
    }

    private void SanitizeEffects()
    {
        if (activeEffects == null)
        {
            activeEffects = new List<StatusEffectState>();
            return;
        }

        var seenEffectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = activeEffects.Count - 1; index >= 0; index--)
        {
            StatusEffectState effect = activeEffects[index];
            if (effect == null)
            {
                activeEffects.RemoveAt(index);
                continue;
            }

            effect.Sanitize();
            if (string.IsNullOrWhiteSpace(effect.effectId) || !seenEffectIds.Add(effect.effectId))
            {
                activeEffects.RemoveAt(index);
            }
        }
    }

    private void TickDurations(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        foreach (StatusEffectState effect in activeEffects)
        {
            if (effect == null || effect.isPersistent)
            {
                continue;
            }

            effect.remainingDuration = Mathf.Max(0f, effect.remainingDuration - deltaTime);
        }
    }

    private void ApplyDamageOverTime(float deltaTime)
    {
        if (deltaTime <= 0f || vitals == null)
        {
            return;
        }

        foreach (StatusEffectState effect in activeEffects)
        {
            if (effect == null || effect.tickDamagePerSecond <= 0f)
            {
                continue;
            }

            float tickDamage = effect.tickDamagePerSecond * deltaTime;
            if (tickDamage <= 0f)
            {
                continue;
            }

            vitals.ApplyGlobalDamage(
                tickDamage,
                effect.sourceUnit,
                effect.ResolveSourceDisplayName(),
                effect.displayName);
        }
    }

    private void RemoveExpiredEffects()
    {
        for (int index = activeEffects.Count - 1; index >= 0; index--)
        {
            StatusEffectState effect = activeEffects[index];
            if (effect == null || effect.IsExpired)
            {
                activeEffects.RemoveAt(index);
            }
        }
    }

    private StatusEffectState GetEffect(string effectId)
    {
        string normalizedEffectId = NormalizeEffectId(effectId);
        if (string.IsNullOrWhiteSpace(normalizedEffectId))
        {
            return null;
        }

        foreach (StatusEffectState effect in activeEffects)
        {
            if (effect != null && string.Equals(effect.effectId, normalizedEffectId, StringComparison.OrdinalIgnoreCase))
            {
                return effect;
            }
        }

        return null;
    }

    private StatusEffectState GetOrCreateEffect(string effectId)
    {
        StatusEffectState effect = GetEffect(effectId);
        if (effect != null)
        {
            return effect;
        }

        effect = new StatusEffectState();
        activeEffects.Add(effect);
        return effect;
    }

    private static string NormalizeEffectId(string effectId)
    {
        return string.IsNullOrWhiteSpace(effectId) ? string.Empty : effectId.Trim().ToLowerInvariant();
    }
}
