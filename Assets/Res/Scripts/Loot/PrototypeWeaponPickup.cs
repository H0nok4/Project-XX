using UnityEngine;

[DisallowMultipleComponent]
public class PrototypeWeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private PrototypeWeaponDefinition weaponDefinition;
    [Min(0)]
    [SerializeField] private int magazineAmmo;
    [SerializeField] private string interactionVerb = "Take";
    [SerializeField] private bool destroyWhenCollected = true;

    public PrototypeWeaponDefinition WeaponDefinition => weaponDefinition;
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
    }

    public void Configure(PrototypeWeaponDefinition definition, int loadedAmmo = -1, string verb = "Take")
    {
        weaponDefinition = definition;
        interactionVerb = string.IsNullOrWhiteSpace(verb) ? "Take" : verb.Trim();

        if (weaponDefinition != null && !weaponDefinition.IsMeleeWeapon)
        {
            int defaultAmmo = weaponDefinition.MagazineSize;
            magazineAmmo = Mathf.Clamp(loadedAmmo >= 0 ? loadedAmmo : defaultAmmo, 0, weaponDefinition.MagazineSize);
        }
        else
        {
            magazineAmmo = 0;
        }
    }

    public string GetInteractionLabel(PlayerInteractor interactor)
    {
        if (weaponDefinition == null)
        {
            return "Take Weapon";
        }

        PrototypeFpsController controller = interactor != null ? interactor.GetComponent<PrototypeFpsController>() : null;
        string slotLabel = controller != null ? controller.GetSuggestedPickupSlotLabel(weaponDefinition) : (weaponDefinition.IsMeleeWeapon ? "Melee" : "Primary");
        string actionVerb = controller != null && controller.PickupWouldReplaceEquippedWeapon(weaponDefinition) ? "Swap" : interactionVerb;

        if (weaponDefinition.IsMeleeWeapon)
        {
            return $"{actionVerb} {slotLabel}: {weaponDefinition.DisplayName}";
        }

        return $"{actionVerb} {slotLabel}: {weaponDefinition.DisplayName} [{MagazineAmmo}/{weaponDefinition.MagazineSize}]";
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
        if (controller == null
            || !controller.TryEquipLootedWeapon(weaponDefinition, MagazineAmmo, out PrototypeWeaponDefinition droppedWeapon, out int droppedMagazineAmmo))
        {
            return;
        }

        if (droppedWeapon != null)
        {
            Transform dropOrigin = interactor.InteractionCamera != null ? interactor.InteractionCamera.transform : interactor.transform;
            SpawnDroppedWeapon(dropOrigin, droppedWeapon, droppedMagazineAmmo);
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
        if (dropOrigin == null || definition == null)
        {
            return null;
        }

        GameObject pickupObject = CreatePickupObject(
            $"DroppedWeapon_{definition.WeaponId}",
            ResolveDropPosition(dropOrigin),
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
            GetDropScale(definition),
            definition,
            loadedAmmo,
            verb);

        Rigidbody body = pickupObject.AddComponent<Rigidbody>();
        body.mass = definition.IsMeleeWeapon ? 0.7f : 1.5f;
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
        if (definition == null)
        {
            return null;
        }

        GameObject pickupObject = CreatePickupObject(
            $"SpawnedWeapon_{definition.WeaponId}",
            worldPosition,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
            GetDropScale(definition),
            definition,
            loadedAmmo,
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
        PrototypeWeaponDefinition definition,
        int loadedAmmo,
        string verb)
    {
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
            renderer.material.color = definition != null && definition.IsMeleeWeapon
                ? new Color(0.78f, 0.78f, 0.82f, 1f)
                : new Color(0.34f, 0.58f, 0.92f, 1f);
        }

        PrototypeWeaponPickup pickup = pickupObject.AddComponent<PrototypeWeaponPickup>();
        pickup.Configure(definition, loadedAmmo, verb);
        return pickupObject;
    }
}
