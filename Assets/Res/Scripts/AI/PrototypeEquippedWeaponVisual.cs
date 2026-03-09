using UnityEngine;

[DisallowMultipleComponent]
public class PrototypeEquippedWeaponVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform weaponAnchor;

    [Header("Offsets")]
    [SerializeField] private Vector3 firearmLocalPosition = new Vector3(-0.18f, 1.1f, -0.22f);
    [SerializeField] private Vector3 firearmLocalEulerAngles = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 meleeLocalPosition = new Vector3(-0.14f, 1.02f, -0.16f);
    [SerializeField] private Vector3 meleeLocalEulerAngles = new Vector3(0f, -18f, 0f);
    [SerializeField] private Vector3 visualLocalScale = Vector3.one;

    private PrototypeWeaponDefinition equippedWeapon;
    private GameObject visualInstance;

    public Transform WeaponAnchor => weaponAnchor;
    public PrototypeWeaponDefinition EquippedWeapon => equippedWeapon;

    private void OnDisable()
    {
        DestroyVisual();
    }

    private void OnValidate()
    {
        visualLocalScale.x = Mathf.Max(0.01f, visualLocalScale.x);
        visualLocalScale.y = Mathf.Max(0.01f, visualLocalScale.y);
        visualLocalScale.z = Mathf.Max(0.01f, visualLocalScale.z);
    }

    public void Configure(Transform anchor)
    {
        weaponAnchor = anchor != null ? anchor : transform;
    }

    public void SetEquippedWeapon(PrototypeWeaponDefinition weaponDefinition)
    {
        if (weaponAnchor == null)
        {
            weaponAnchor = transform;
        }

        GameObject nextPrefab = weaponDefinition != null ? weaponDefinition.EquippedWorldPrefab : null;
        bool prefabChanged = visualInstance == null
            || equippedWeapon != weaponDefinition
            || (nextPrefab != null && visualInstance.name != nextPrefab.name);

        equippedWeapon = weaponDefinition;
        if (!prefabChanged)
        {
            ApplyTransformOffsets();
            return;
        }

        DestroyVisual();
        if (nextPrefab == null)
        {
            return;
        }

        visualInstance = Instantiate(nextPrefab, weaponAnchor, false);
        visualInstance.name = nextPrefab.name;
        ApplyTransformOffsets();
    }

    private void ApplyTransformOffsets()
    {
        if (visualInstance == null)
        {
            return;
        }

        bool isMelee = equippedWeapon != null && equippedWeapon.IsMeleeWeapon;
        visualInstance.transform.localPosition = isMelee ? meleeLocalPosition : firearmLocalPosition;
        visualInstance.transform.localRotation = Quaternion.Euler(isMelee ? meleeLocalEulerAngles : firearmLocalEulerAngles);
        visualInstance.transform.localScale = visualLocalScale;
    }

    private void DestroyVisual()
    {
        if (visualInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(visualInstance);
        }
        else
        {
            DestroyImmediate(visualInstance);
        }

        visualInstance = null;
    }
}
