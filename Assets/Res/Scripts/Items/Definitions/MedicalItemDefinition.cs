using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Raid/Medical Definition", fileName = "MedicalDefinition")]
public class MedicalItemDefinition : ItemDefinition
{
    [Header("Treatment")]
    [Min(0f)]
    [SerializeField] private float healAmount = 0f;
    [Min(0f)]
    [SerializeField] private float healPercent = 0f;
    [Min(0)]
    [SerializeField] private int removesLightBleeds = 0;
    [Min(0)]
    [SerializeField] private int removesHeavyBleeds = 0;
    [Min(0)]
    [SerializeField] private int curesFractures = 0;
    [Min(0f)]
    [SerializeField] private float painkillerDuration = 0f;

    public float HealAmount => Mathf.Max(0f, healAmount);
    public float HealPercent => NormalizePercent(healPercent);
    public bool HasHealing => HealAmount > 0f || HealPercent > 0f;
    public int RemovesLightBleeds => Mathf.Max(0, removesLightBleeds);
    public int RemovesHeavyBleeds => Mathf.Max(0, removesHeavyBleeds);
    public int CuresFractures => Mathf.Max(0, curesFractures);
    public float PainkillerDuration => Mathf.Max(0f, painkillerDuration);

    public float GetHealingAmount(float maxHealth)
    {
        float percentHealing = Mathf.Max(0f, maxHealth) * HealPercent;
        return HealAmount + percentHealing;
    }

    public void ConfigureMedical(
        string id,
        string nameLabel,
        string itemDescription,
        int stackSize,
        float weight,
        float healing,
        int lightBleedCures,
        int heavyBleedCures,
        int fractureCures,
        float painkillerSeconds,
        float healingPercent = 0f,
        Sprite itemIcon = null)
    {
        Configure(id, nameLabel, itemDescription, stackSize, weight, itemIcon);
        healAmount = Mathf.Max(0f, healing);
        healPercent = NormalizePercent(healingPercent);
        removesLightBleeds = Mathf.Max(0, lightBleedCures);
        removesHeavyBleeds = Mathf.Max(0, heavyBleedCures);
        curesFractures = Mathf.Max(0, fractureCures);
        painkillerDuration = Mathf.Max(0f, painkillerSeconds);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        healAmount = Mathf.Max(0f, healAmount);
        healPercent = NormalizePercent(healPercent);
        removesLightBleeds = Mathf.Max(0, removesLightBleeds);
        removesHeavyBleeds = Mathf.Max(0, removesHeavyBleeds);
        curesFractures = Mathf.Max(0, curesFractures);
        painkillerDuration = Mathf.Max(0f, painkillerDuration);
    }

    private static float NormalizePercent(float rawPercent)
    {
        if (rawPercent <= 0f)
        {
            return 0f;
        }

        return rawPercent > 1f ? Mathf.Clamp01(rawPercent * 0.01f) : Mathf.Clamp01(rawPercent);
    }
}
