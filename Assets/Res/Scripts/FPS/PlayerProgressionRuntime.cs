using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerProgressionRuntime : MonoBehaviour
{
    [SerializeField] private PrototypeUnitVitals playerVitals;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private PlayerSkillManager skillManager;
    [SerializeField] private float feedbackLifetime = 2.6f;
    [SerializeField, HideInInspector] private PlayerProgressionData runtimeProgression = new PlayerProgressionData();
    [SerializeField, HideInInspector] private CharacterStatAggregator statAggregator = new CharacterStatAggregator();
    [SerializeField, HideInInspector] private string feedbackMessage = string.Empty;
    [SerializeField, HideInInspector] private float feedbackTimer;

    public PlayerProgressionData RuntimeProgression => runtimeProgression;
    public int PlayerLevel => runtimeProgression != null ? Mathf.Max(1, runtimeProgression.playerLevel) : 1;
    public int CurrentExperience => runtimeProgression != null ? Mathf.Max(0, runtimeProgression.currentExperience) : 0;
    public int ExperienceToNextLevel => PrototypePlayerProgressionUtility.GetExperienceToNextLevel(PlayerLevel);
    public int LifetimeExperience => runtimeProgression != null ? Mathf.Max(0, runtimeProgression.lifetimeExperience) : 0;
    public int KillCount => runtimeProgression != null ? Mathf.Max(0, runtimeProgression.killCount) : 0;
    public int UnspentAttributePoints => runtimeProgression != null ? Mathf.Max(0, runtimeProgression.unspentAttributePoints) : 0;
    public int UnspentSkillPoints => runtimeProgression != null ? Mathf.Max(0, runtimeProgression.unspentSkillPoints) : 0;
    public int UnlockedSkillNodeCount => runtimeProgression != null && runtimeProgression.skillTree != null
        ? runtimeProgression.skillTree.GetUnlockedCount()
        : 0;
    public CharacterStatAggregator StatAggregator => statAggregator;
    public string FeedbackMessage => feedbackMessage;

    private void Awake()
    {
        ResolveReferences();
        EnsureProgressionData();
        RefreshDerivedStats();
    }

    private void OnValidate()
    {
        ResolveReferences();
        feedbackLifetime = Mathf.Max(0.25f, feedbackLifetime);
        EnsureProgressionData();
        RefreshDerivedStats();
    }

    public void SetPlayerDependencies(PrototypeUnitVitals vitals, PlayerWeaponController controller, PlayerSkillManager manager = null)
    {
        playerVitals = vitals;
        weaponController = controller;
        if (manager != null)
        {
            skillManager = manager;
        }

        ResolveReferences();
        EnsureProgressionData();
        RefreshDerivedStats();
    }

    public void Configure(PlayerProgressionData source)
    {
        EnsureProgressionData();
        PrototypePlayerProgressionUtility.Copy(source, runtimeProgression);
        RefreshDerivedStats();
    }

    public void ExportTo(PlayerProgressionData target)
    {
        if (target == null)
        {
            return;
        }

        EnsureProgressionData();
        PrototypePlayerProgressionUtility.Copy(runtimeProgression, target);
    }

    public int AddExperience(int amount, string sourceLabel = "")
    {
        EnsureProgressionData();
        int grantedAmount = PrototypePlayerProgressionUtility.AddExperience(runtimeProgression, amount, out int levelsGained);
        if (grantedAmount <= 0)
        {
            return 0;
        }

        RefreshDerivedStats();

        string normalizedSourceLabel = string.IsNullOrWhiteSpace(sourceLabel) ? string.Empty : sourceLabel.Trim();
        string gainText = string.IsNullOrWhiteSpace(normalizedSourceLabel)
            ? $"获得 {grantedAmount} 经验"
            : $"{normalizedSourceLabel} +{grantedAmount} XP";

        if (levelsGained > 0)
        {
            int earnedAttributePoints = levelsGained * PrototypePlayerProgressionUtility.AttributePointsPerLevel;
            int earnedSkillPoints = levelsGained * PrototypePlayerProgressionUtility.SkillPointsPerLevel;
            SetFeedback($"{gainText}  升到 Lv {PlayerLevel}  +{earnedAttributePoints} 属性点  +{earnedSkillPoints} 技能点");
        }
        else
        {
            SetFeedback(gainText);
        }

        return grantedAmount;
    }

    public void HandleTargetKilled(PrototypeBotController targetBot)
    {
        if (targetBot == null)
        {
            return;
        }

        EnsureProgressionData();
        runtimeProgression.killCount = Mathf.Max(0, runtimeProgression.killCount) + 1;
        QuestEventHub.RaiseKill(targetBot.gameObject.name, targetBot.Archetype.ToString(), targetBot.IsBossProfile);

        int reward = Mathf.Max(0, targetBot.ExperienceReward);
        string sourceLabel = $"击杀 {targetBot.name} (Lv {targetBot.EnemyLevel})";
        if (reward > 0)
        {
            AddExperience(reward, sourceLabel);
        }
        else
        {
            SetFeedback(sourceLabel);
        }
    }

    public void TickFeedback(float deltaTime)
    {
        if (feedbackTimer <= 0f)
        {
            return;
        }

        feedbackTimer -= Mathf.Max(0f, deltaTime);
        if (feedbackTimer <= 0f)
        {
            feedbackTimer = 0f;
            feedbackMessage = string.Empty;
        }
    }

    public string BuildHudSummary()
    {
        return $"等级 {PlayerLevel}  经验 {CurrentExperience}/{ExperienceToNextLevel}  击杀 {KillCount}  属性点 {UnspentAttributePoints}  技能点 {UnspentSkillPoints}  节点 {UnlockedSkillNodeCount}";
    }

    public bool TryAllocateAttributePoint(PlayerAttributeType type, out string errorMessage)
    {
        EnsureProgressionData();
        bool allocated = PrototypePlayerProgressionUtility.TryAllocateAttributePoint(runtimeProgression, type, out errorMessage);
        if (allocated)
        {
            RefreshDerivedStats();
        }

        return allocated;
    }

    public bool TryUnlockSkillNode(string nodeId, out string errorMessage)
    {
        EnsureProgressionData();
        bool unlocked = PrototypePlayerProgressionUtility.TryUnlockSkillNode(runtimeProgression, nodeId, out errorMessage);
        if (unlocked)
        {
            RefreshDerivedStats();
        }

        return unlocked;
    }

    public string BuildDerivedSummary()
    {
        EnsureProgressionData();
        statAggregator ??= new CharacterStatAggregator();
        statAggregator.Rebuild(runtimeProgression, GetActiveWeaponItemInstance(), BuildArmorAffixSummaries());
        return statAggregator.BuildDerivedSummary();
    }

    public void RefreshDerivedStats()
    {
        ApplyCurrentBonuses();
    }

    private void ApplyCurrentBonuses()
    {
        EnsureProgressionData();
        statAggregator ??= new CharacterStatAggregator();
        statAggregator.Rebuild(runtimeProgression, GetActiveWeaponItemInstance(), BuildArmorAffixSummaries());
        PlayerDerivedStats derivedStats = statAggregator.DerivedStats;

        if (playerVitals != null && derivedStats != null)
        {
            playerVitals.ConfigureCharacterBonuses(
                derivedStats.maxHealthBonus,
                derivedStats.maxStaminaBonus,
                derivedStats.healingMultiplier);
            playerVitals.SetCharacterMovementMultiplier(derivedStats.moveSpeedMultiplier);
        }

        if (weaponController != null && derivedStats != null)
        {
            weaponController.SetCharacterCombatModifiers(
                derivedStats.damageMultiplier,
                derivedStats.fireRateMultiplier,
                derivedStats.reloadSpeedMultiplier,
                derivedStats.critChance,
                derivedStats.critDamageMultiplier,
                derivedStats.armorPenetrationBonus,
                derivedStats.spreadMultiplier,
                derivedStats.effectiveRangeMultiplier);
        }

        skillManager?.SetPlayerDependencies(playerVitals, weaponController, this);
    }

    private void SetFeedback(string message)
    {
        feedbackMessage = message ?? string.Empty;
        feedbackTimer = string.IsNullOrWhiteSpace(feedbackMessage) ? 0f : feedbackLifetime;
    }

    private void EnsureProgressionData()
    {
        runtimeProgression ??= new PlayerProgressionData();
        statAggregator ??= new CharacterStatAggregator();
        PrototypePlayerProgressionUtility.Sanitize(runtimeProgression);
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

        if (skillManager == null)
        {
            skillManager = GetComponent<PlayerSkillManager>();
        }
    }

    private ItemInstance GetActiveWeaponItemInstance()
    {
        return weaponController != null ? weaponController.GetActiveItemInstance() : null;
    }

    private IReadOnlyList<ItemAffixSummary> BuildArmorAffixSummaries()
    {
        var summaries = new List<ItemAffixSummary>();
        if (playerVitals == null || playerVitals.EquippedArmor == null)
        {
            return summaries;
        }

        for (int index = 0; index < playerVitals.EquippedArmor.Count; index++)
        {
            PrototypeUnitVitals.ArmorState armorState = playerVitals.EquippedArmor[index];
            if (armorState == null)
            {
                continue;
            }

            summaries.Add(armorState.AffixSummary);
        }

        return summaries;
    }

}
