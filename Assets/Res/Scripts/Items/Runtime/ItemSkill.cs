using System;
using UnityEngine;

[Serializable]
public sealed class ItemSkill
{
    public ItemSkillType type = ItemSkillType.KillHeal;
    [Min(0f)] public float value;
    [SerializeField] private string description = string.Empty;

    public string Description
    {
        get => string.IsNullOrWhiteSpace(description)
            ? ItemSkillUtility.BuildDescription(type, value)
            : description.Trim();
        set => description = value ?? string.Empty;
    }

    public ItemSkill()
    {
    }

    public ItemSkill(ItemSkill other)
    {
        if (other == null)
        {
            return;
        }

        type = other.type;
        value = other.value;
        description = other.description;
    }

    public void Sanitize()
    {
        value = Mathf.Max(0f, value);
        Description = Description;
    }
}
