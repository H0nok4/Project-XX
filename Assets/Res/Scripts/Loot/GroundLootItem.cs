using UnityEngine;

[DisallowMultipleComponent]
public class GroundLootItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition itemDefinition;
    [Min(1)]
    [SerializeField] private int quantity = 1;
    [SerializeField] private string interactionVerb = "Pick Up";
    [SerializeField] private bool destroyWhenCollected = true;

    public ItemDefinition ItemDefinition => itemDefinition;
    public int Quantity => quantity;

    private void OnValidate()
    {
        quantity = Mathf.Max(1, quantity);
        interactionVerb = string.IsNullOrWhiteSpace(interactionVerb) ? "Pick Up" : interactionVerb.Trim();
    }

    public void Configure(ItemDefinition definition, int amount, string verb = "Pick Up")
    {
        itemDefinition = definition;
        quantity = Mathf.Max(1, amount);
        interactionVerb = string.IsNullOrWhiteSpace(verb) ? "Pick Up" : verb.Trim();
    }

    public string GetInteractionLabel(PlayerInteractor interactor)
    {
        string itemName = itemDefinition != null ? itemDefinition.DisplayNameWithLevel : "Unknown Item";
        return $"{interactionVerb} {itemName} x{quantity}";
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return interactor != null
            && interactor.PrimaryInventory != null
            && itemDefinition != null
            && quantity > 0
            && interactor.PrimaryInventory.GetAddableQuantity(itemDefinition, quantity) > 0;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        InventoryContainer inventory = interactor.PrimaryInventory;
        if (!inventory.TryAddItem(itemDefinition, quantity, out int addedQuantity) || addedQuantity <= 0)
        {
            return;
        }

        quantity = Mathf.Max(0, quantity - addedQuantity);
        if (quantity <= 0 && destroyWhenCollected)
        {
            Destroy(gameObject);
        }
    }

    public static GroundLootItem SpawnDroppedItem(Transform dropOrigin, ItemDefinition definition, int amount, string verb = "Pick Up")
    {
        if (dropOrigin == null || definition == null || amount <= 0)
        {
            return null;
        }

        GameObject pickupObject = CreatePickupObject(
            $"Dropped_{definition.ItemId}",
            ResolveDropPosition(dropOrigin),
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
            GetDropScale(definition, amount),
            definition,
            amount,
            verb);

        Rigidbody body = pickupObject.AddComponent<Rigidbody>();
        body.mass = Mathf.Clamp(definition.UnitWeight * Mathf.Max(amount, 1), 0.2f, 5f);
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.AddForce((dropOrigin.forward.normalized + Vector3.up * 0.25f) * 2.4f, ForceMode.VelocityChange);

        return pickupObject.GetComponent<GroundLootItem>();
    }

    public static GroundLootItem SpawnScenePickup(
        Vector3 worldPosition,
        ItemDefinition definition,
        int amount,
        string verb = "Pick Up",
        Transform parent = null)
    {
        if (definition == null || amount <= 0)
        {
            return null;
        }

        GameObject pickupObject = CreatePickupObject(
            $"Spawned_{definition.ItemId}",
            worldPosition,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
            GetDropScale(definition, amount),
            definition,
            amount,
            verb);

        if (parent != null)
        {
            pickupObject.transform.SetParent(parent, true);
        }

        return pickupObject.GetComponent<GroundLootItem>();
    }

    public Transform GetInteractionTransform()
    {
        return transform;
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

    private static Vector3 GetDropScale(ItemDefinition definition, int amount)
    {
        float stackFactor = Mathf.Clamp01(Mathf.Log10(Mathf.Max(1, amount) + 1f));
        float weightFactor = Mathf.Clamp01(definition != null ? definition.UnitWeight : 0f);
        float size = Mathf.Lerp(0.16f, 0.32f, Mathf.Max(stackFactor, weightFactor));
        return new Vector3(size, size * 0.55f, size);
    }

    private static GameObject CreatePickupObject(
        string objectName,
        Vector3 worldPosition,
        Quaternion worldRotation,
        Vector3 localScale,
        ItemDefinition definition,
        int amount,
        string verb)
    {
        GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pickupObject.name = objectName;
        pickupObject.transform.position = worldPosition;
        pickupObject.transform.rotation = worldRotation;
        pickupObject.transform.localScale = localScale;

        Renderer renderer = pickupObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            renderer.material = new Material(shader);
            renderer.material.color = new Color(0.91f, 0.79f, 0.29f, 1f);
        }

        GroundLootItem groundLoot = pickupObject.AddComponent<GroundLootItem>();
        groundLoot.Configure(definition, amount, verb);
        return pickupObject;
    }
}
