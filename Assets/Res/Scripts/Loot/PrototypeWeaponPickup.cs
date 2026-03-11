using UnityEngine;

[DisallowMultipleComponent]
public class PrototypeWeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private PrototypeWeaponDefinition weaponDefinition;
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;
    [Min(0)]
    [SerializeField] private int magazineAmmo;
    [Min(0f)]
    [SerializeField] private float durability = 1f;
    [SerializeField] private string interactionVerb = "Take";
    [SerializeField] private bool destroyWhenCollected = true;

    public PrototypeWeaponDefinition WeaponDefinition => weaponDefinition;
    public ItemRarity Rarity => ItemRarityUtility.Sanitize(rarity);
    public float Durability => Mathf.Max(0f, durability);
    public int MagazineAmmo => weaponDefinition != null && !weaponDefinition.IsMeleeWeapon
        ? Mathf.Clamp(magazineAmmo, 0, weaponDefinition.MagazineSize)
        : 0;

    private void OnValidate()
    {
        interactionVerb = string.IsNullOrWhiteSpace(interactionVerb) ? "Take" : interactionVerb.Trim();
        if (weaponDefinition != null && !weaponDefinition.IsMeleeWeapon)
        {
            magazineAmmo = Mathf.Clamp(magazineAmmo, 0, weaponDefinition.MagazineSize);
        }
        else
        {
            magazineAmmo = 0;
        }

        rarity = ItemRarityUtility.Sanitize(rarity);
        durability = Mathf.Max(0f, durability);
        RefreshVisuals();
    }

    public void Configure(PrototypeWeaponDefinition definition, int loadedAmmo = -1, string verb = "Take")
    {
        int startingAmmo = definition != null && !definition.IsMeleeWeapon
            ? (loadedAmmo >= 0 ? loadedAmmo : definition.MagazineSize)
            : 0;
        Configure(WeaponInstance.Create(definition, startingAmmo, 1f), verb);
    }

    public void Configure(WeaponInstance instance, string verb = "Take")
    {
        weaponDefinition = instance != null ? instance.Definition : null;
        rarity = instance != null ? instance.Rarity : ItemRarity.Common;
        durability = instance != null ? instance.Durability : 1f;
        interactionVerb = string.IsNullOrWhiteSpace(verb) ? "Take" : verb.Trim();

        if (weaponDefinition != null && !weaponDefinition.IsMeleeWeapon)
        {
            int defaultAmmo = weaponDefinition.MagazineSize;
            int loadedAmmo = instance != null ? instance.MagazineAmmo : defaultAmmo;
            magazineAmmo = Mathf.Clamp(loadedAmmo, 0, weaponDefinition.MagazineSize);
        }
        else
        {
            magazineAmmo = 0;
        }

        RefreshVisuals();
    }

    public string GetInteractionLabel(PlayerInteractor interactor)
    {
        if (weaponDefinition == null)
        {
            return "Take Weapon";
        }

        PrototypeFpsController controller = interactor != null ? interactor.GetComponent<PrototypeFpsController>() : null;
        bool storeInBackpack = controller != null && controller.PickupWouldStoreInBackpack(weaponDefinition);
        string slotLabel = storeInBackpack
            ? "Backpack"
            : controller != null ? controller.GetSuggestedPickupSlotLabel(weaponDefinition) : (weaponDefinition.IsMeleeWeapon ? "Melee" : "Primary");
        string actionVerb = storeInBackpack
            ? "Pack"
            : controller != null && controller.PickupWouldReplaceEquippedWeapon(weaponDefinition) ? "Swap" : interactionVerb;
        string displayName = $"{weaponDefinition.DisplayNameWithLevel} [{ItemRarityUtility.GetDisplayName(Rarity)}]";

        if (weaponDefinition.IsMeleeWeapon)
        {
            return $"{actionVerb} {slotLabel}: {displayName}";
        }

        return $"{actionVerb} {slotLabel}: {displayName} [{MagazineAmmo}/{weaponDefinition.MagazineSize}]";
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return interactor != null
            && weaponDefinition != null
            && interactor.GetComponent<PrototypeFpsController>() != null;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        PrototypeFpsController controller = interactor.GetComponent<PrototypeFpsController>();
        WeaponInstance weaponInstance = CreateWeaponInstance();
        if (controller == null
            || weaponInstance == null
            || !controller.TryEquipLootedWeapon(weaponInstance, out WeaponInstance droppedWeapon))
        {
            return;
        }

        if (droppedWeapon != null)
        {
            Transform dropOrigin = interactor.InteractionCamera != null ? interactor.InteractionCamera.transform : interactor.transform;
            SpawnDroppedWeapon(dropOrigin, droppedWeapon);
        }

        if (destroyWhenCollected)
        {
            Destroy(gameObject);
        }
    }

    public Transform GetInteractionTransform()
    {
        return transform;
    }

    public static PrototypeWeaponPickup SpawnDroppedWeapon(
        Transform dropOrigin,
        PrototypeWeaponDefinition definition,
        int loadedAmmo = -1,
        string verb = "Take")
    {
        WeaponInstance instance = definition != null
            ? WeaponInstance.Create(definition, loadedAmmo >= 0 ? loadedAmmo : (definition.IsMeleeWeapon ? 0 : definition.MagazineSize))
            : null;
        return SpawnDroppedWeapon(dropOrigin, instance, verb);
    }

    public static PrototypeWeaponPickup SpawnDroppedWeapon(
        Transform dropOrigin,
        WeaponInstance instance,
        string verb = "Take")
    {
        if (dropOrigin == null || instance == null || instance.Definition == null)
        {
            return null;
        }

        GameObject pickupObject = CreatePickupObject(
            $"DroppedWeapon_{instance.Definition.WeaponId}",
            ResolveDropPosition(dropOrigin),
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
            GetDropScale(instance.Definition),
            instance,
            verb);

        Rigidbody body = pickupObject.AddComponent<Rigidbody>();
        body.mass = instance.Definition.IsMeleeWeapon ? 0.7f : 1.5f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.AddForce((dropOrigin.forward.normalized + Vector3.up * 0.24f) * 2.1f, ForceMode.VelocityChange);

        return pickupObject.GetComponent<PrototypeWeaponPickup>();
    }

    public static PrototypeWeaponPickup SpawnScenePickup(
        Vector3 worldPosition,
        PrototypeWeaponDefinition definition,
        int loadedAmmo = -1,
        string verb = "Take",
        Transform parent = null)
    {
        WeaponInstance instance = definition != null
            ? WeaponInstance.Create(definition, loadedAmmo >= 0 ? loadedAmmo : (definition.IsMeleeWeapon ? 0 : definition.MagazineSize))
            : null;
        return SpawnScenePickup(worldPosition, instance, verb, parent);
    }

    public static PrototypeWeaponPickup SpawnScenePickup(
        Vector3 worldPosition,
        WeaponInstance instance,
        string verb = "Take",
        Transform parent = null)
    {
        if (instance == null || instance.Definition == null)
        {
            return null;
        }

        GameObject pickupObject = CreatePickupObject(
            $"SpawnedWeapon_{instance.Definition.WeaponId}",
            worldPosition,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
            GetDropScale(instance.Definition),
            instance,
            verb);

        if (parent != null)
        {
            pickupObject.transform.SetParent(parent, true);
        }

        return pickupObject.GetComponent<PrototypeWeaponPickup>();
    }

    private static Vector3 ResolveDropPosition(Transform dropOrigin)
    {
        Vector3 spawnOrigin = dropOrigin.position + dropOrigin.forward * 0.85f + Vector3.up * 0.2f;
        Vector3 probeOrigin = spawnOrigin + Vector3.up * 1.2f;
        if (Physics.Raycast(probeOrigin, Vector3.down, out RaycastHit hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * 0.12f;
        }

        return spawnOrigin;
    }

    private static Vector3 GetDropScale(PrototypeWeaponDefinition definition)
    {
        if (definition == null)
        {
            return new Vector3(0.36f, 0.1f, 0.18f);
        }

        return definition.IsMeleeWeapon
            ? new Vector3(0.14f, 0.52f, 0.14f)
            : new Vector3(0.48f, 0.1f, 0.18f);
    }

    private static GameObject CreatePickupObject(
        string objectName,
        Vector3 worldPosition,
        Quaternion worldRotation,
        Vector3 localScale,
        WeaponInstance instance,
        string verb)
    {
        PrototypeWeaponDefinition definition = instance != null ? instance.Definition : null;
        PrimitiveType primitiveType = definition != null && definition.IsMeleeWeapon ? PrimitiveType.Capsule : PrimitiveType.Cube;
        GameObject pickupObject = GameObject.CreatePrimitive(primitiveType);
        pickupObject.name = objectName;
        pickupObject.transform.position = worldPosition;
        pickupObject.transform.rotation = worldRotation;
        pickupObject.transform.localScale = localScale;

        Renderer renderer = pickupObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            renderer.material = new Material(shader);
            renderer.material.color = ItemRarityUtility.GetDisplayColor(instance != null ? instance.Rarity : ItemRarity.Common);
        }

        PrototypeWeaponPickup pickup = pickupObject.AddComponent<PrototypeWeaponPickup>();
        pickup.Configure(instance, verb);
        return pickupObject;
    }

    private WeaponInstance CreateWeaponInstance()
    {
        if (weaponDefinition == null)
        {
            return null;
        }

        return WeaponInstance.Create(weaponDefinition, MagazineAmmo, Durability, null, Rarity);
    }

    private void RefreshVisuals()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            renderer.material = material;
        }

        material.color = ItemRarityUtility.GetDisplayColor(Rarity);
    }
}
