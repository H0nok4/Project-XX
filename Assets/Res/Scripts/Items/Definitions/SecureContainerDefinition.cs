using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Raid/Secure Container Definition", fileName = "SecureContainerDefinition")]
public class SecureContainerDefinition : ItemDefinition
{
    [Min(1)]
    [SerializeField] private int slotCapacity = 4;
    [Min(0f)]
    [SerializeField] private float maxStoredWeight = 6f;

    public int SlotCapacity => Mathf.Max(1, slotCapacity);
    public float MaxStoredWeight => Mathf.Max(0f, maxStoredWeight);

    public void ConfigureSecureContainer(
        string id,
        string nameLabel,
        string itemDescription,
        float weight,
        int slots,
        float storageWeight,
        Sprite itemIcon = null)
    {
        Configure(id, nameLabel, itemDescription, 1, weight, itemIcon);
        slotCapacity = Mathf.Max(1, slots);
        maxStoredWeight = Mathf.Max(0f, storageWeight);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        slotCapacity = Mathf.Max(1, slotCapacity);
        maxStoredWeight = Mathf.Max(0f, maxStoredWeight);
    }
}
