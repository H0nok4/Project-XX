using System;
using UnityEngine;

[Serializable]
public sealed class FacilityData
{
    public const int MinLevel = 1;
    public const int MaxLevel = 5;

    public FacilityType type;
    [Range(MinLevel, MaxLevel)]
    public int level = MinLevel;
    [Range(MinLevel, MaxLevel)]
    public int maxLevel = MaxLevel;

    public int Level => Mathf.Clamp(level, MinLevel, MaxLevel);
    public int MaxLevelValue => Mathf.Clamp(maxLevel, MinLevel, MaxLevel);

    public void Sanitize(FacilityType facilityType)
    {
        type = facilityType;
        maxLevel = Mathf.Clamp(maxLevel, MinLevel, MaxLevel);
        level = Mathf.Clamp(level, MinLevel, maxLevel);
    }

    public int GetUpgradeCost()
    {
        return Level >= MaxLevelValue ? 0 : Level * 5000;
    }

    public bool CanUpgrade()
    {
        return Level < MaxLevelValue;
    }

    public bool Upgrade()
    {
        if (!CanUpgrade())
        {
            return false;
        }

        level = Mathf.Clamp(level + 1, MinLevel, MaxLevelValue);
        return true;
    }
}
