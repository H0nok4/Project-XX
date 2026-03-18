using System;
using UnityEngine;

[Serializable]
public sealed class PlayerAttributeSet
{
    public int strength = PrototypePlayerProgressionUtility.DefaultAttributeValue;
    public int endurance = PrototypePlayerProgressionUtility.DefaultAttributeValue;
    public int agility = PrototypePlayerProgressionUtility.DefaultAttributeValue;
    public int perception = PrototypePlayerProgressionUtility.DefaultAttributeValue;
    public int tech = PrototypePlayerProgressionUtility.DefaultAttributeValue;

    public int GetValue(PlayerAttributeType type)
    {
        switch (type)
        {
            case PlayerAttributeType.Strength:
                return strength;
            case PlayerAttributeType.Endurance:
                return endurance;
            case PlayerAttributeType.Agility:
                return agility;
            case PlayerAttributeType.Perception:
                return perception;
            case PlayerAttributeType.Tech:
                return tech;
            default:
                return PrototypePlayerProgressionUtility.DefaultAttributeValue;
        }
    }

    public void SetValue(PlayerAttributeType type, int value)
    {
        int sanitizedValue = Mathf.Clamp(
            value,
            PrototypePlayerProgressionUtility.DefaultAttributeValue,
            PrototypePlayerProgressionUtility.MaxAttributeValue);

        switch (type)
        {
            case PlayerAttributeType.Strength:
                strength = sanitizedValue;
                break;
            case PlayerAttributeType.Endurance:
                endurance = sanitizedValue;
                break;
            case PlayerAttributeType.Agility:
                agility = sanitizedValue;
                break;
            case PlayerAttributeType.Perception:
                perception = sanitizedValue;
                break;
            case PlayerAttributeType.Tech:
                tech = sanitizedValue;
                break;
        }
    }

    public int GetAllocatedPoints()
    {
        int baseline = PrototypePlayerProgressionUtility.DefaultAttributeValue;
        return Mathf.Max(0, strength - baseline)
            + Mathf.Max(0, endurance - baseline)
            + Mathf.Max(0, agility - baseline)
            + Mathf.Max(0, perception - baseline)
            + Mathf.Max(0, tech - baseline);
    }

    public void Sanitize()
    {
        strength = SanitizeValue(strength);
        endurance = SanitizeValue(endurance);
        agility = SanitizeValue(agility);
        perception = SanitizeValue(perception);
        tech = SanitizeValue(tech);
    }

    private static int SanitizeValue(int value)
    {
        return Mathf.Clamp(
            value,
            PrototypePlayerProgressionUtility.DefaultAttributeValue,
            PrototypePlayerProgressionUtility.MaxAttributeValue);
    }
}
