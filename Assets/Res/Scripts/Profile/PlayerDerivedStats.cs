using System;
using UnityEngine;

[Serializable]
public sealed class PlayerDerivedStats
{
    public float maxHealthBonus;
    public float maxStaminaBonus;
    public float carryWeight = PrototypePlayerProgressionUtility.BaseCarryWeight;
    public float damageMultiplier = 1f;
    public float healingMultiplier = 1f;
    public float reloadSpeedMultiplier = 1f;
    public float fireRateMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;
    public float recoilControlMultiplier = 1f;
    public float interactionRangeMultiplier = 1f;
    public float critChance;
    public float critDamageMultiplier = 1f;
    public float armorPenetrationBonus;
    public float spreadMultiplier = 1f;
    public float effectiveRangeMultiplier = 1f;

    public void Reset()
    {
        maxHealthBonus = 0f;
        maxStaminaBonus = 0f;
        carryWeight = PrototypePlayerProgressionUtility.BaseCarryWeight;
        damageMultiplier = 1f;
        healingMultiplier = 1f;
        reloadSpeedMultiplier = 1f;
        fireRateMultiplier = 1f;
        moveSpeedMultiplier = 1f;
        recoilControlMultiplier = 1f;
        interactionRangeMultiplier = 1f;
        critChance = 0f;
        critDamageMultiplier = 1f;
        armorPenetrationBonus = 0f;
        spreadMultiplier = 1f;
        effectiveRangeMultiplier = 1f;
    }

    public void Sanitize()
    {
        maxHealthBonus = Mathf.Max(0f, maxHealthBonus);
        maxStaminaBonus = Mathf.Max(0f, maxStaminaBonus);
        carryWeight = Mathf.Max(0f, carryWeight);
        damageMultiplier = Mathf.Max(0.1f, damageMultiplier);
        healingMultiplier = Mathf.Max(0.1f, healingMultiplier);
        reloadSpeedMultiplier = Mathf.Clamp(reloadSpeedMultiplier, 0.25f, 3f);
        fireRateMultiplier = Mathf.Clamp(fireRateMultiplier, 0.25f, 3f);
        moveSpeedMultiplier = Mathf.Clamp(moveSpeedMultiplier, 0.4f, 1.8f);
        recoilControlMultiplier = Mathf.Clamp(recoilControlMultiplier, 0.1f, 3f);
        interactionRangeMultiplier = Mathf.Clamp(interactionRangeMultiplier, 0.5f, 3f);
        critChance = Mathf.Clamp01(critChance);
        critDamageMultiplier = Mathf.Clamp(critDamageMultiplier, 1f, 5f);
        armorPenetrationBonus = Mathf.Max(0f, armorPenetrationBonus);
        spreadMultiplier = Mathf.Clamp(spreadMultiplier, 0.1f, 3f);
        effectiveRangeMultiplier = Mathf.Clamp(effectiveRangeMultiplier, 0.5f, 3f);
    }
}
