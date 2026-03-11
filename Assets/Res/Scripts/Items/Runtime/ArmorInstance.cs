using System;
using UnityEngine;

[Serializable]
public class ArmorInstance
{
    [SerializeField] private string instanceId = string.Empty;
    [SerializeField] private ArmorDefinition definition;
    [Min(0f)]
    [SerializeField] private float currentDurability = 1f;

    public string InstanceId => instanceId;
    public ArmorDefinition Definition => definition;
    public float CurrentDurability => currentDurability;
    public float MaxDurability => definition != null ? definition.MaxDurability : Mathf.Max(1f, currentDurability);
    public string DisplayName => definition != null ? definition.DisplayNameWithLevel : "Unknown Armor";

    public static ArmorInstance Create(ArmorDefinition armorDefinition, float durability, string instanceIdOverride = null)
    {
        var instance = new ArmorInstance();
        instance.ApplyDefinition(armorDefinition, durability, instanceIdOverride);
        return instance;
    }

    public void ApplyDefinition(ArmorDefinition armorDefinition, float durability, string instanceIdOverride = null)
    {
        definition = armorDefinition;
        float maxDurability = definition != null ? definition.MaxDurability : Mathf.Max(1f, durability);
        currentDurability = Mathf.Clamp(durability, 0f, maxDurability);
        SetInstanceId(instanceIdOverride);
    }

    public void Sanitize()
    {
        float maxDurability = definition != null ? definition.MaxDurability : Mathf.Max(1f, currentDurability);
        currentDurability = Mathf.Clamp(currentDurability, 0f, maxDurability);
        EnsureInstanceId();
    }

    private void SetInstanceId(string desiredId)
    {
        if (!string.IsNullOrWhiteSpace(desiredId))
        {
            instanceId = desiredId.Trim();
        }

        EnsureInstanceId();
    }

    private void EnsureInstanceId()
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            instanceId = Guid.NewGuid().ToString("N");
        }
    }
}