using System;

public enum AffixCategory
{
    Offensive = 0,
    Defensive = 1,
    Mobility = 2,
    Survival = 3,
    Special = 4
}

[Flags]
public enum AffixItemTarget
{
    None = 0,
    Weapon = 1 << 0,
    Armor = 1 << 1,
    Any = Weapon | Armor
}

public enum AffixValueKind
{
    Flat = 0,
    Percent = 1
}

public enum AffixType
{
    // Offensive
    DamageBonus = 0,
    CritChance = 1,
    CritDamage = 2,
    ArmorPenetration = 3,
    FireRate = 4,
    Accuracy = 5,
    EffectiveRange = 6,

    // Defensive
    ArmorBonus = 10,
    ArmorLevel = 11,
    DurabilityBonus = 12,
    DamageReduction = 13,
    BodyPartProtection = 14,

    // Mobility
    MoveSpeed = 20,
    ReloadSpeed = 21,
    SwapSpeed = 22,
    AimSpeed = 23,
    WeightReduction = 24,

    // Survival
    HealthBonus = 30,
    StaminaBonus = 31,
    StaminaRegen = 32,
    HealingBonus = 33,
    StatusResistance = 34,

    // Special
    ExperienceBonus = 40,
    LootFind = 41
}
