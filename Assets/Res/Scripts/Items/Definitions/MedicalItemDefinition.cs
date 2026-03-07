using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Raid/Medical Definition", fileName = "MedicalDefinition")]
public class MedicalItemDefinition : ItemDefinition
{
    [Header("Treatment")]
    [Min(0f)]
    [SerializeField] private float healAmount = 0f;
    [Min(0)]
    [SerializeField] private int removesLightBleeds = 0;
    [Min(0)]
    [SerializeField] private int removesHeavyBleeds = 0;
    [Min(0)]
    [SerializeField] private int curesFractures = 0;
    [Min(0f)]
    [SerializeField] private float painkillerDuration = 0f;

    public float HealAmount => Mathf.Max(0f, healAmount);
    public int RemovesLightBleeds => Mathf.Max(0, removesLightBleeds);
    public int RemovesHeavyBleeds => Mathf.Max(0, removesHeavyBleeds);
    public int CuresFractures => Mathf.Max(0, curesFractures);
    public float PainkillerDuration => Mathf.Max(0f, painkillerDuration);

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
        Sprite itemIcon = null)
    {
        Configure(id, nameLabel, itemDescription, stackSize, weight, itemIcon);
        healAmount = Mathf.Max(0f, healing);
        removesLightBleeds = Mathf.Max(0, lightBleedCures);
        removesHeavyBleeds = Mathf.Max(0, heavyBleedCures);
        curesFractures = Mathf.Max(0, fractureCures);
        painkillerDuration = Mathf.Max(0f, painkillerSeconds);
    }

    private void OnValidate()
    {
        healAmount = Mathf.Max(0f, healAmount);
        removesLightBleeds = Mathf.Max(0, removesLightBleeds);
        removesHeavyBleeds = Mathf.Max(0, removesHeavyBleeds);
        curesFractures = Mathf.Max(0, curesFractures);
        painkillerDuration = Mathf.Max(0f, painkillerDuration);
    }
}
