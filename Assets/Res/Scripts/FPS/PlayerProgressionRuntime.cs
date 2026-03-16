using UnityEngine;

[DisallowMultipleComponent]
public class PlayerProgressionRuntime : MonoBehaviour
{
    [SerializeField] private PrototypeUnitVitals playerVitals;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private PlayerSkillManager skillManager;
    [SerializeField] private float feedbackLifetime = 2.6f;
    [SerializeField, HideInInspector] private PlayerProgressionData runtimeProgression = new PlayerProgressionData();
    [SerializeField, HideInInspector] private string feedbackMessage = string.Empty;
    [SerializeField, HideInInspector] private float feedbackTimer;

    public PlayerProgressionData RuntimeProgression => runtimeProgression;
    public int PlayerLevel => runtimeProgression != null ? Mathf.Max(1, runtimeProgression.playerLevel) : 1;
    public int CurrentExperience => runtimeProgression != null ? Mathf.Max(0, runtimeProgression.currentExperience) : 0;
    public int ExperienceToNextLevel => PrototypePlayerProgressionUtility.GetExperienceToNextLevel(PlayerLevel);
    public int LifetimeExperience => runtimeProgression != null ? Mathf.Max(0, runtimeProgression.lifetimeExperience) : 0;
    public int KillCount => runtimeProgression != null ? Mathf.Max(0, runtimeProgression.killCount) : 0;
    public string FeedbackMessage => feedbackMessage;

    private void Awake()
    {
        ResolveReferences();
        EnsureProgressionData();
        ApplyCurrentBonuses();
    }

    private void OnValidate()
    {
        ResolveReferences();
        feedbackLifetime = Mathf.Max(0.25f, feedbackLifetime);
        EnsureProgressionData();
        ApplyCurrentBonuses();
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
        ApplyCurrentBonuses();
    }

    public void Configure(PlayerProgressionData source)
    {
        EnsureProgressionData();
        CopyProgression(source, runtimeProgression);
        ApplyCurrentBonuses();
    }

    public void ExportTo(PlayerProgressionData target)
    {
        if (target == null)
        {
            return;
        }

        EnsureProgressionData();
        CopyProgression(runtimeProgression, target);
    }

    public int AddExperience(int amount, string sourceLabel = "")
    {
        EnsureProgressionData();
        int grantedAmount = PrototypePlayerProgressionUtility.AddExperience(runtimeProgression, amount, out int levelsGained);
        if (grantedAmount <= 0)
        {
            return 0;
        }

        ApplyCurrentBonuses();

        string normalizedSourceLabel = string.IsNullOrWhiteSpace(sourceLabel) ? string.Empty : sourceLabel.Trim();
        string gainText = string.IsNullOrWhiteSpace(normalizedSourceLabel)
            ? $"获得 {grantedAmount} 经验"
            : $"{normalizedSourceLabel} +{grantedAmount} XP";

        if (levelsGained > 0)
        {
            SetFeedback($"{gainText}  升到 Lv {PlayerLevel}");
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
        return $"等级 {PlayerLevel}  经验 {CurrentExperience}/{ExperienceToNextLevel}  击杀 {KillCount}";
    }

    private void ApplyCurrentBonuses()
    {
        EnsureProgressionData();

        float healthBonus = PrototypePlayerProgressionUtility.GetHealthBonus(PlayerLevel);
        float staminaBonus = PrototypePlayerProgressionUtility.GetStaminaBonus(PlayerLevel);
        float damageMultiplier = PrototypePlayerProgressionUtility.GetDamageMultiplier(PlayerLevel);
        float healingMultiplier = PrototypePlayerProgressionUtility.GetHealingMultiplier(PlayerLevel);

        if (playerVitals != null)
        {
            playerVitals.ConfigureCharacterBonuses(healthBonus, staminaBonus, healingMultiplier);
        }

        weaponController?.SetCharacterDamageMultiplier(damageMultiplier);
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

    private static void CopyProgression(PlayerProgressionData source, PlayerProgressionData target)
    {
        if (target == null)
        {
            return;
        }

        if (source == null)
        {
            target.progressionDataVersion = ProfileSchemaVersion.CurrentProgressionDataVersion;
            target.playerLevel = 1;
            target.currentExperience = 0;
            target.lifetimeExperience = 0;
            target.killCount = 0;
            return;
        }

        target.progressionDataVersion = source.progressionDataVersion;
        target.playerLevel = source.playerLevel;
        target.currentExperience = source.currentExperience;
        target.lifetimeExperience = source.lifetimeExperience;
        target.killCount = source.killCount;
        PrototypePlayerProgressionUtility.Sanitize(target);
    }
}