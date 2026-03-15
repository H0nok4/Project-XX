using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSkillManager : MonoBehaviour
{
    [SerializeField] private PrototypeUnitVitals playerVitals;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField, HideInInspector] private List<ItemSkill> activeSkills = new List<ItemSkill>();
    [SerializeField, HideInInspector] private float battleFrenzyEndTime;
    [SerializeField, HideInInspector] private float perfectDodgeReadyTime;

    private float totalKillHeal;
    private float totalAmmoRecovery;
    private float totalIronBodyReduction;
    private float totalBattleFrenzyBonus;
    private float totalPerfectDodgeChance;
    private float totalBloodlustRatio;
    private float totalUnyieldingReduction;

    public IReadOnlyList<ItemSkill> ActiveSkills => activeSkills;
    public bool HasBattleFrenzyBuff => battleFrenzyEndTime > Time.time;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void SetPlayerDependencies(PrototypeUnitVitals vitals, PlayerWeaponController controller)
    {
        playerVitals = vitals;
        weaponController = controller;
    }

    public void RefreshFromEquipment()
    {
        ResolveReferences();
        activeSkills ??= new List<ItemSkill>();
        activeSkills.Clear();

        CollectSkills(weaponController != null ? weaponController.GetPrimaryItemInstance() : null);
        CollectSkills(weaponController != null ? weaponController.GetSecondaryItemInstance() : null);
        CollectSkills(weaponController != null ? weaponController.GetMeleeItemInstance() : null);

        if (playerVitals != null && playerVitals.EquippedArmor != null)
        {
            for (int index = 0; index < playerVitals.EquippedArmor.Count; index++)
            {
                PrototypeUnitVitals.ArmorState armorState = playerVitals.EquippedArmor[index];
                if (armorState == null || armorState.Skills == null)
                {
                    continue;
                }

                for (int skillIndex = 0; skillIndex < armorState.Skills.Count; skillIndex++)
                {
                    ItemSkill skill = armorState.Skills[skillIndex];
                    if (skill != null)
                    {
                        activeSkills.Add(new ItemSkill(skill));
                    }
                }
            }
        }

        ItemSkillUtility.SanitizeSkills(activeSkills);
        RebuildSkillTotals();
    }

    public PrototypeUnitVitals.DamageInfo AdjustIncomingDamage(string partId, PrototypeUnitVitals.DamageInfo damageInfo)
    {
        if (activeSkills == null || activeSkills.Count == 0)
        {
            return damageInfo;
        }

        if (totalPerfectDodgeChance > 0f
            && Time.time >= perfectDodgeReadyTime
            && damageInfo.damage > 0f
            && Random.value < Mathf.Clamp01(totalPerfectDodgeChance))
        {
            perfectDodgeReadyTime = Time.time + ItemSkillUtility.PerfectDodgeInternalCooldown;
            damageInfo.damage = 0f;
            damageInfo.armorDamage = 0f;
            damageInfo.penetrationPower = 0f;
            damageInfo.canApplyAfflictions = false;
            return damageInfo;
        }

        float incomingReduction = totalIronBodyReduction;
        if (playerVitals != null && playerVitals.TotalHealthNormalized <= ItemSkillUtility.UnyieldingLowHealthThreshold)
        {
            incomingReduction += totalUnyieldingReduction;
        }

        incomingReduction = Mathf.Clamp(incomingReduction, 0f, 0.85f);
        if (incomingReduction <= 0f)
        {
            return damageInfo;
        }

        float multiplier = 1f - incomingReduction;
        damageInfo.damage *= multiplier;
        damageInfo.armorDamage *= multiplier;
        return damageInfo;
    }

    public void HandleDamageResolved(PrototypeUnitVitals targetVitals, PrototypeUnitVitals.DamageInfo damageInfo, bool killedTarget)
    {
        if (targetVitals != null && playerVitals != null && targetVitals != playerVitals && totalBloodlustRatio > 0f && damageInfo.damage > 0f)
        {
            playerVitals.TryRestoreHealthFromSkill(damageInfo.damage * totalBloodlustRatio);
        }

        if (!killedTarget)
        {
            return;
        }

        if (playerVitals != null && totalKillHeal > 0f)
        {
            playerVitals.TryRestoreHealthFromSkill(totalKillHeal);
        }

        if (weaponController != null && totalAmmoRecovery > 0f)
        {
            weaponController.RecoverAmmoToActiveWeapon(Mathf.RoundToInt(totalAmmoRecovery));
        }

        if (totalBattleFrenzyBonus > 0f)
        {
            battleFrenzyEndTime = Mathf.Max(battleFrenzyEndTime, Time.time + ItemSkillUtility.BattleFrenzyBuffDuration);
        }
    }

    public float GetFireRateMultiplier()
    {
        if (!HasBattleFrenzyBuff || totalBattleFrenzyBonus <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp(1f + totalBattleFrenzyBonus, 1f, 3f);
    }

    public float GetReloadSpeedMultiplier()
    {
        return GetFireRateMultiplier();
    }

    public string BuildHudSummary()
    {
        if ((activeSkills == null || activeSkills.Count == 0) && !HasBattleFrenzyBuff)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        if (activeSkills != null && activeSkills.Count > 0)
        {
            builder.Append("Passives ");
            builder.Append(activeSkills.Count);
        }

        if (HasBattleFrenzyBuff)
        {
            if (builder.Length > 0)
            {
                builder.Append("  ");
            }

            builder.Append("Frenzy ");
            builder.Append(Mathf.Max(0f, battleFrenzyEndTime - Time.time).ToString("0.0"));
            builder.Append('s');
        }

        if (totalPerfectDodgeChance > 0f && perfectDodgeReadyTime > Time.time)
        {
            if (builder.Length > 0)
            {
                builder.Append("  ");
            }

            builder.Append("Dodge CD ");
            builder.Append(Mathf.Max(0f, perfectDodgeReadyTime - Time.time).ToString("0.0"));
            builder.Append('s');
        }

        return builder.ToString();
    }

    private void CollectSkills(ItemInstance itemInstance)
    {
        if (itemInstance == null || itemInstance.Skills == null)
        {
            return;
        }

        for (int index = 0; index < itemInstance.Skills.Count; index++)
        {
            ItemSkill skill = itemInstance.Skills[index];
            if (skill != null)
            {
                activeSkills.Add(new ItemSkill(skill));
            }
        }
    }

    private void RebuildSkillTotals()
    {
        totalKillHeal = 0f;
        totalAmmoRecovery = 0f;
        totalIronBodyReduction = 0f;
        totalBattleFrenzyBonus = 0f;
        totalPerfectDodgeChance = 0f;
        totalBloodlustRatio = 0f;
        totalUnyieldingReduction = 0f;

        if (activeSkills == null)
        {
            return;
        }

        for (int index = 0; index < activeSkills.Count; index++)
        {
            ItemSkill skill = activeSkills[index];
            if (skill == null)
            {
                continue;
            }

            switch (skill.type)
            {
                case ItemSkillType.KillHeal:
                    totalKillHeal += skill.value;
                    break;
                case ItemSkillType.AmmoRecovery:
                    totalAmmoRecovery += skill.value;
                    break;
                case ItemSkillType.IronBody:
                    totalIronBodyReduction += skill.value;
                    break;
                case ItemSkillType.BattleFrenzy:
                    totalBattleFrenzyBonus += skill.value;
                    break;
                case ItemSkillType.PerfectDodge:
                    totalPerfectDodgeChance += skill.value;
                    break;
                case ItemSkillType.Bloodlust:
                    totalBloodlustRatio += skill.value;
                    break;
                case ItemSkillType.Unyielding:
                    totalUnyieldingReduction += skill.value;
                    break;
            }
        }

        totalPerfectDodgeChance = Mathf.Clamp01(totalPerfectDodgeChance);
        totalIronBodyReduction = Mathf.Clamp(totalIronBodyReduction, 0f, 0.6f);
        totalUnyieldingReduction = Mathf.Clamp(totalUnyieldingReduction, 0f, 0.6f);
        totalBloodlustRatio = Mathf.Clamp(totalBloodlustRatio, 0f, 0.4f);
    }

    private void ResolveReferences()
    {
        if (playerVitals == null)
        {
            playerVitals = GetComponent<PrototypeUnitVitals>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<PlayerWeaponController>();
        }
    }
}
