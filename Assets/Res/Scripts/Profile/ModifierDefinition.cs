using System;

public enum CharacterStatType
{
    MaxHealthBonus = 0,
    MaxStaminaBonus = 1,
    CarryWeight = 2,
    DamageMultiplier = 3,
    HealingMultiplier = 4,
    ReloadSpeedMultiplier = 5,
    FireRateMultiplier = 6,
    MoveSpeedMultiplier = 7,
    RecoilControlMultiplier = 8,
    InteractionRangeMultiplier = 9,
    CritChance = 10,
    CritDamageMultiplier = 11,
    ArmorPenetrationBonus = 12,
    SpreadMultiplier = 13,
    EffectiveRangeMultiplier = 14
}

public enum ModifierOperation
{
    Add = 0,
    Multiply = 1
}

[Serializable]
public sealed class ModifierDefinition
{
    public CharacterStatType statType;
    public ModifierOperation operation = ModifierOperation.Add;
    public float value;
    public string description = string.Empty;
}
