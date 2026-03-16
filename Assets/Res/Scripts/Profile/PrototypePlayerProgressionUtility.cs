using UnityEngine;

public static class PrototypePlayerProgressionUtility
{
    private const int BaseExperienceToNextLevel = 100;
    private const int ExperienceLinearStep = 50;
    private const int ExperienceQuadraticStep = 15;
    private const float HealthBonusPerLevel = 10f;
    private const float StaminaBonusPerLevel = 5f;
    private const float DamageBonusPerLevel = 0.03f;
    private const float HealingBonusPerLevel = 0.025f;

    public static void Sanitize(PlayerProgressionData progression)
    {
        if (progression == null)
        {
            return;
        }

        progression.progressionDataVersion = Mathf.Max(
            progression.progressionDataVersion,
            ProfileSchemaVersion.CurrentProgressionDataVersion);
        progression.playerLevel = Mathf.Max(1, progression.playerLevel);
        progression.currentExperience = Mathf.Max(0, progression.currentExperience);
        progression.lifetimeExperience = Mathf.Max(progression.currentExperience, progression.lifetimeExperience);
        progression.killCount = Mathf.Max(0, progression.killCount);

        int experienceToNext = GetExperienceToNextLevel(progression.playerLevel);
        if (experienceToNext > 0)
        {
            progression.currentExperience = Mathf.Clamp(progression.currentExperience, 0, experienceToNext - 1);
        }
    }

    public static int GetExperienceToNextLevel(int playerLevel)
    {
        int normalizedLevel = Mathf.Max(1, playerLevel);
        int levelIndex = normalizedLevel - 1;
        return BaseExperienceToNextLevel
            + levelIndex * ExperienceLinearStep
            + levelIndex * levelIndex * ExperienceQuadraticStep;
    }

    public static int GetVitality(int playerLevel)
    {
        return 10 + Mathf.Max(0, playerLevel - 1) * 2;
    }

    public static int GetEndurance(int playerLevel)
    {
        return 10 + Mathf.Max(0, playerLevel - 1) * 2;
    }

    public static int GetCombat(int playerLevel)
    {
        return 10 + Mathf.Max(0, playerLevel - 1);
    }

    public static int GetMedicine(int playerLevel)
    {
        return 10 + Mathf.Max(0, playerLevel - 1);
    }

    public static float GetHealthBonus(int playerLevel)
    {
        return Mathf.Max(0, playerLevel - 1) * HealthBonusPerLevel;
    }

    public static float GetStaminaBonus(int playerLevel)
    {
        return Mathf.Max(0, playerLevel - 1) * StaminaBonusPerLevel;
    }

    public static float GetDamageMultiplier(int playerLevel)
    {
        return 1f + Mathf.Max(0, playerLevel - 1) * DamageBonusPerLevel;
    }

    public static float GetHealingMultiplier(int playerLevel)
    {
        return 1f + Mathf.Max(0, playerLevel - 1) * HealingBonusPerLevel;
    }

    public static int GetEnemyBaseLevel(PrototypeEnemyArchetype archetype)
    {
        switch (archetype)
        {
            case PrototypeEnemyArchetype.PoliceZombie:
                return 2;
            case PrototypeEnemyArchetype.SoldierZombie:
                return 4;
            case PrototypeEnemyArchetype.ZombieDog:
                return 2;
            default:
                return 1;
        }
    }

    public static float GetEnemyHealthMultiplier(int enemyLevel, bool bossProfile)
    {
        float multiplier = 1f + Mathf.Max(0, enemyLevel - 1) * 0.16f;
        return bossProfile ? multiplier + 0.35f : multiplier;
    }

    public static float GetEnemyDamageMultiplier(int enemyLevel, bool bossProfile)
    {
        float multiplier = 1f + Mathf.Max(0, enemyLevel - 1) * 0.12f;
        return bossProfile ? multiplier + 0.2f : multiplier;
    }

    public static int GetEnemyExperienceReward(int enemyLevel, PrototypeEnemyArchetype archetype, bool bossProfile)
    {
        int archetypeBonus;
        switch (archetype)
        {
            case PrototypeEnemyArchetype.PoliceZombie:
                archetypeBonus = 8;
                break;
            case PrototypeEnemyArchetype.SoldierZombie:
                archetypeBonus = 18;
                break;
            case PrototypeEnemyArchetype.ZombieDog:
                archetypeBonus = 6;
                break;
            default:
                archetypeBonus = 0;
                break;
        }

        int reward = 28 + Mathf.Max(0, enemyLevel - 1) * 14 + archetypeBonus;
        if (bossProfile)
        {
            reward += 50;
        }

        return Mathf.Max(10, reward);
    }

    public static int AddExperience(PlayerProgressionData progression, int amount, out int levelsGained)
    {
        levelsGained = 0;
        if (progression == null || amount <= 0)
        {
            return 0;
        }

        Sanitize(progression);

        progression.currentExperience += amount;
        progression.lifetimeExperience += amount;

        int experienceToNext = GetExperienceToNextLevel(progression.playerLevel);
        while (experienceToNext > 0 && progression.currentExperience >= experienceToNext)
        {
            progression.currentExperience -= experienceToNext;
            progression.playerLevel++;
            levelsGained++;
            experienceToNext = GetExperienceToNextLevel(progression.playerLevel);
        }

        Sanitize(progression);
        return amount;
    }
}
