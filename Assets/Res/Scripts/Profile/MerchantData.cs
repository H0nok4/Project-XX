using System;
using UnityEngine;

[Serializable]
public sealed class MerchantData
{
    public const int MinLevel = 1;
    public const int MaxLevel = 5;

    public string merchantId = string.Empty;
    [Range(MinLevel, MaxLevel)]
    public int startingLevel = MinLevel;
    [Range(MinLevel, MaxLevel)]
    public int level = MinLevel;
    public int totalTradeAmount;
    public ReputationLevel reputation = ReputationLevel.Neutral;
    public int reputationPoints;

    public string MerchantId => string.IsNullOrWhiteSpace(merchantId) ? string.Empty : merchantId.Trim();
    public int StartingLevel => Mathf.Clamp(startingLevel, MinLevel, MaxLevel);
    public int Level => Mathf.Clamp(level, StartingLevel, MaxLevel);
    public int TotalTradeAmount => Mathf.Max(0, totalTradeAmount);
    public int ReputationPoints => Mathf.Max(0, reputationPoints);
    public ReputationLevel Reputation => SanitizeReputation(reputation);

    public void Sanitize(string fallbackMerchantId, int defaultStartingLevel)
    {
        merchantId = string.IsNullOrWhiteSpace(merchantId)
            ? fallbackMerchantId ?? string.Empty
            : merchantId.Trim();
        startingLevel = Mathf.Clamp(defaultStartingLevel, MinLevel, MaxLevel);
        level = Mathf.Clamp(level, startingLevel, MaxLevel);
        totalTradeAmount = Mathf.Max(0, totalTradeAmount);
        reputation = SanitizeReputation(reputation);
        reputationPoints = Mathf.Max(0, reputationPoints);
        UpdateReputationLevelFromPoints();
    }

    public int GetLevelUpRequirement()
    {
        return Level >= MaxLevel ? 0 : Level * 10000;
    }

    public int GetTradeAmountAtCurrentLevelStart()
    {
        int total = 0;
        for (int currentLevel = StartingLevel; currentLevel < Level; currentLevel++)
        {
            total += currentLevel * 10000;
        }

        return total;
    }

    public int GetTradeProgressIntoCurrentLevel()
    {
        if (Level >= MaxLevel)
        {
            return 0;
        }

        return Mathf.Clamp(TotalTradeAmount - GetTradeAmountAtCurrentLevelStart(), 0, GetLevelUpRequirement());
    }

    public float GetTradeProgressNormalized()
    {
        int requirement = GetLevelUpRequirement();
        if (requirement <= 0)
        {
            return 1f;
        }

        return Mathf.Clamp01((float)GetTradeProgressIntoCurrentLevel() / requirement);
    }

    public bool AddTradeAmount(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        totalTradeAmount += amount;
        return CheckLevelUp();
    }

    public int GetCurrentReputationThreshold()
    {
        switch (Reputation)
        {
            case ReputationLevel.Friendly:
                return 100;

            case ReputationLevel.Honored:
                return 300;

            case ReputationLevel.Revered:
                return 650;

            default:
                return 0;
        }
    }

    public int GetNextReputationThreshold()
    {
        switch (Reputation)
        {
            case ReputationLevel.Neutral:
                return 100;

            case ReputationLevel.Friendly:
                return 300;

            case ReputationLevel.Honored:
                return 650;

            default:
                return 650;
        }
    }

    public int GetReputationProgressIntoCurrentTier()
    {
        if (Reputation == ReputationLevel.Revered)
        {
            return ReputationPoints;
        }

        return Mathf.Clamp(ReputationPoints - GetCurrentReputationThreshold(), 0, GetReputationRequirementForNextTier());
    }

    public int GetReputationRequirementForNextTier()
    {
        if (Reputation == ReputationLevel.Revered)
        {
            return 0;
        }

        return Mathf.Max(1, GetNextReputationThreshold() - GetCurrentReputationThreshold());
    }

    public float GetReputationProgressNormalized()
    {
        int requirement = GetReputationRequirementForNextTier();
        if (requirement <= 0)
        {
            return 1f;
        }

        return Mathf.Clamp01((float)GetReputationProgressIntoCurrentTier() / requirement);
    }

    public float GetPriceMultiplier()
    {
        switch (Reputation)
        {
            case ReputationLevel.Friendly:
                return 0.95f;

            case ReputationLevel.Honored:
                return 0.9f;

            case ReputationLevel.Revered:
                return 0.85f;

            default:
                return 1f;
        }
    }

    public bool AddReputation(int points)
    {
        if (points <= 0)
        {
            return false;
        }

        reputationPoints += points;
        return UpdateReputationLevelFromPoints();
    }

    private bool CheckLevelUp()
    {
        bool leveledUp = false;
        while (level < MaxLevel && TotalTradeAmount >= GetTradeRequirementForLevel(level + 1))
        {
            level++;
            leveledUp = true;
        }

        return leveledUp;
    }

    private int GetTradeRequirementForLevel(int targetLevel)
    {
        int sanitizedTargetLevel = Mathf.Clamp(targetLevel, StartingLevel, MaxLevel);
        int total = 0;
        for (int currentLevel = StartingLevel; currentLevel < sanitizedTargetLevel; currentLevel++)
        {
            total += currentLevel * 10000;
        }

        return total;
    }

    private bool UpdateReputationLevelFromPoints()
    {
        ReputationLevel previousLevel = reputation;
        if (reputationPoints >= 650)
        {
            reputation = ReputationLevel.Revered;
        }
        else if (reputationPoints >= 300)
        {
            reputation = ReputationLevel.Honored;
        }
        else if (reputationPoints >= 100)
        {
            reputation = ReputationLevel.Friendly;
        }
        else
        {
            reputation = ReputationLevel.Neutral;
        }

        return previousLevel != reputation;
    }

    private static ReputationLevel SanitizeReputation(ReputationLevel value)
    {
        if (value < ReputationLevel.Neutral || value > ReputationLevel.Revered)
        {
            return ReputationLevel.Neutral;
        }

        return value;
    }
}
