using UnityEngine;

[DisallowMultipleComponent]
public class GroundLootItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemInstance itemInstance;
    [SerializeField] private string interactionVerb = "拾取";
    [SerializeField] private bool destroyWhenCollected = true;

    public ItemDefinitionBase DefinitionBase => itemInstance != null ? itemInstance.DefinitionBase : null;
    public ItemDefinition ItemDefinition => itemInstance != null ? itemInstance.Definition : null;
    public PrototypeWeaponDefinition WeaponDefinition => itemInstance != null ? itemInstance.WeaponDefinition : null;
    public ItemRarity Rarity => itemInstance != null ? itemInstance.Rarity : ItemRarity.Common;
    public int Quantity => itemInstance != null ? itemInstance.Quantity : 0;

    private void OnValidate()
    {
        itemInstance?.Sanitize();
        interactionVerb = ResolveInteractionVerb(itemInstance, interactionVerb);
        RefreshVisuals();
    }

    public void Configure(ItemDefinition definition, int amount, string verb = null)
    {
        Configure(ItemInstance.Create(definition, amount), verb);
    }

    public void Configure(ItemDefinitionBase definition, int amount, ItemRarity rarity = ItemRarity.Common, string verb = null)
    {
        Configure(ItemInstance.Create(definition, amount, rarity), verb);
    }

    public void Configure(WeaponInstance instance, string verb = null)
    {
        Configure(ItemInstance.Create(instance), verb);
    }

    public void Configure(ItemInstance instance, string verb = null)
    {
        itemInstance = instance != null ? instance.Clone() : null;
        interactionVerb = ResolveInteractionVerb(itemInstance, verb);
        RefreshVisuals();
    }

    public string GetInteractionLabel(PlayerInteractor interactor)
    {
        if (itemInstance == null || !itemInstance.IsDefined())
        {
            return "拾取物品";
        }

        if (itemInstance.IsWeapon && itemInstance.WeaponDefinition != null)
        {
            return GetWeaponInteractionLabel(interactor, itemInstance.WeaponDefinition);
        }

        string itemName = itemInstance.DisplayName;
        return $"{LocalizeActionVerb(interactionVerb)} {itemName} x{Quantity}";
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        if (interactor == null || itemInstance == null || !itemInstance.IsDefined() || Quantity <= 0)
        {
            return false;
        }

        if (itemInstance.IsWeapon)
        {
            PrototypeFpsController controller = interactor.GetComponent<PrototypeFpsController>();
            if (controller != null)
            {
                return true;
            }

            return interactor.PrimaryInventory != null && interactor.PrimaryInventory.CanAccept(itemInstance);
        }

        if (interactor.PrimaryInventory == null)
        {
            return false;
        }

        if (itemInstance.HasInstanceState || itemInstance.MaxStackSize <= 1)
        {
            return interactor.PrimaryInventory.CanAccept(itemInstance);
        }

        return itemInstance.Definition != null
            && interactor.PrimaryInventory.GetAddableQuantity(itemInstance.Definition, Quantity, itemInstance.Rarity) > 0;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        if (itemInstance != null && itemInstance.IsWeapon)
        {
            TryCollectWeapon(interactor);
            return;
        }

        InventoryContainer inventory = interactor.PrimaryInventory;
        if (inventory == null)
        {
            return;
        }

        if (itemInstance.HasInstanceState || itemInstance.MaxStackSize <= 1)
        {
            if (!inventory.TryAddItemInstance(itemInstance.Clone()))
            {
                return;
            }

            HandleCollected();
            return;
        }

        if (!inventory.TryAddItem(itemInstance.Definition, Quantity, itemInstance.Rarity, out int addedQuantity) || addedQuantity <= 0)
        {
            return;
        }

        int remainingQuantity = Mathf.Max(0, Quantity - addedQuantity);
        if (remainingQuantity <= 0)
        {
            HandleCollected();
            return;
        }

        itemInstance.SetQuantity(remainingQuantity);
        if (!destroyWhenCollected)
        {
            RefreshVisuals();
        }
    }

    public static GroundLootItem SpawnDroppedItem(Transform dropOrigin, ItemDefinition definition, int amount, string verb = null)
    {
        return SpawnDroppedItem(dropOrigin, ItemInstance.Create(definition, amount), verb);
    }

    public static GroundLootItem SpawnDroppedItem(
        Transform dropOrigin,
        ItemDefinitionBase definition,
        int amount,
        ItemRarity rarity = ItemRarity.Common,
        string verb = null)
    {
        return SpawnDroppedItem(dropOrigin, ItemInstance.Create(definition, amount, rarity), verb);
    }

    public static GroundLootItem SpawnDroppedItem(Transform dropOrigin, WeaponInstance instance, string verb = null)
    {
        return SpawnDroppedItem(dropOrigin, ItemInstance.Create(instance), verb);
    }

    public static GroundLootItem SpawnDroppedItem(Transform dropOrigin, ItemInstance instance, string verb = null)
    {
        if (dropOrigin == null || instance == null || !instance.IsDefined() || instance.Quantity <= 0)
        {
            return null;
        }

        GameObject pickupObject = CreatePickupObject(
            $"Dropped_{GetPickupId(instance)}",
            ResolveDropPosition(dropOrigin),
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
            GetDropScale(instance),
            instance,
            verb);

        Rigidbody body = pickupObject.AddComponent<Rigidbody>();
        body.mass = Mathf.Clamp(GetDropMass(instance), 0.2f, 5f);
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.AddForce((dropOrigin.forward.normalized + Vector3.up * 0.25f) * 2.4f, ForceMode.VelocityChange);

        return pickupObject.GetComponent<GroundLootItem>();
    }

    public static GroundLootItem SpawnScenePickup(
        Vector3 worldPosition,
        ItemDefinition definition,
        int amount,
        string verb = null,
        Transform parent = null)
    {
        return SpawnScenePickup(worldPosition, ItemInstance.Create(definition, amount), verb, parent);
    }

    public static GroundLootItem SpawnScenePickup(
        Vector3 worldPosition,
        ItemDefinitionBase definition,
        int amount,
        ItemRarity rarity = ItemRarity.Common,
        string verb = null,
        Transform parent = null)
    {
        return SpawnScenePickup(worldPosition, ItemInstance.Create(definition, amount, rarity), verb, parent);
    }

    public static GroundLootItem SpawnScenePickup(
        Vector3 worldPosition,
        WeaponInstance instance,
        string verb = null,
        Transform parent = null)
    {
        return SpawnScenePickup(worldPosition, ItemInstance.Create(instance), verb, parent);
    }

    public static GroundLootItem SpawnScenePickup(
        Vector3 worldPosition,
        ItemInstance instance,
        string verb = null,
        Transform parent = null)
    {
        if (instance == null || !instance.IsDefined() || instance.Quantity <= 0)
        {
            return null;
        }

        GameObject pickupObject = CreatePickupObject(
            $"Spawned_{GetPickupId(instance)}",
            worldPosition,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
            GetDropScale(instance),
            instance,
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

    private static string ResolveInteractionVerb(ItemInstance instance, string verb)
    {
        if (!string.IsNullOrWhiteSpace(verb))
        {
            return LocalizeActionVerb(verb);
        }

        return instance != null && instance.IsWeapon ? "拿取" : "拾取";
    }

    private bool TryCollectWeapon(PlayerInteractor interactor)
    {
        ItemInstance weaponInstance = itemInstance != null ? itemInstance.Clone() : null;
        if (weaponInstance == null || !weaponInstance.IsWeapon)
        {
            return false;
        }

        if (weaponInstance.WeaponDefinition != null && weaponInstance.WeaponDefinition.IsThrowableWeapon)
        {
            InventoryContainer specialInventory = interactor != null ? interactor.SpecialInventory : null;
            if (specialInventory != null && specialInventory.TryAddItemInstance(weaponInstance))
            {
                HandleCollected();
                return true;
            }

            InventoryContainer fallbackInventory = interactor != null ? interactor.PrimaryInventory : null;
            if (fallbackInventory != null && fallbackInventory.TryAddItemInstance(itemInstance.Clone()))
            {
                HandleCollected();
                return true;
            }

            return false;
        }

        PrototypeFpsController controller = interactor != null ? interactor.GetComponent<PrototypeFpsController>() : null;
        if (controller != null && controller.TryEquipLootedWeapon(weaponInstance, out ItemInstance droppedWeapon))
        {
            if (droppedWeapon != null)
            {
                Transform dropOrigin = interactor.InteractionCamera != null ? interactor.InteractionCamera.transform : interactor.transform;
                SpawnDroppedItem(dropOrigin, droppedWeapon, interactionVerb);
            }

            HandleCollected();
            return true;
        }

        InventoryContainer inventory = interactor != null ? interactor.PrimaryInventory : null;
        if (inventory == null || !inventory.TryAddItemInstance(itemInstance.Clone()))
        {
            return false;
        }

        HandleCollected();
        return true;
    }

    private string GetWeaponInteractionLabel(PlayerInteractor interactor, PrototypeWeaponDefinition weaponDefinition)
    {
        string displayName = itemInstance != null ? itemInstance.DisplayName : weaponDefinition.DisplayNameWithLevel;
        if (weaponDefinition.IsThrowableWeapon)
        {
            InventoryContainer specialInventory = interactor != null ? interactor.SpecialInventory : null;
            bool goesToSpecial = specialInventory != null && specialInventory.CanAccept(itemInstance != null ? itemInstance.Clone() : ItemInstance.Create(weaponDefinition, 1));
            return $"收纳 {LocalizeSlotLabel(goesToSpecial ? "Special" : "Backpack")}: {displayName}";
        }

        PrototypeFpsController controller = interactor != null ? interactor.GetComponent<PrototypeFpsController>() : null;
        bool storeInBackpack = controller != null && controller.PickupWouldStoreInBackpack(weaponDefinition);
        string slotLabel = storeInBackpack
            ? "背包"
            : controller != null
                ? controller.GetSuggestedPickupSlotLabel(weaponDefinition)
                : (weaponDefinition.IsMeleeWeapon ? "近战" : "主武器");
        slotLabel = LocalizeSlotLabel(slotLabel);
        string actionVerb = storeInBackpack
            ? "收纳"
            : controller != null && controller.PickupWouldReplaceEquippedWeapon(weaponDefinition)
                ? "替换"
                : LocalizeActionVerb(interactionVerb);

        if (weaponDefinition.IsMeleeWeapon)
        {
            return $"{actionVerb} {slotLabel}: {displayName}";
        }

        int loadedAmmo = itemInstance != null ? itemInstance.MagazineAmmo : 0;
        return $"{actionVerb} {slotLabel}: {displayName} [{loadedAmmo}/{weaponDefinition.MagazineSize}]";
    }

    public static string LocalizeActionVerb(string verb)
    {
        if (string.IsNullOrWhiteSpace(verb))
        {
            return "拿取";
        }

        switch (verb.Trim().ToLowerInvariant())
        {
            case "take":
                return "拿取";
            case "pick up":
            case "pickup":
                return "拾取";
            case "pack":
                return "收纳";
            case "swap":
                return "替换";
            default:
                return verb.Trim();
        }
    }

    public static string LocalizeSlotLabel(string slotLabel)
    {
        if (string.IsNullOrWhiteSpace(slotLabel))
        {
            return string.Empty;
        }

        switch (slotLabel.Trim().ToLowerInvariant())
        {
            case "backpack":
                return "背包";
            case "special":
                return "特殊栏";
            case "primary":
                return "主武器";
            case "secondary":
                return "副武器";
            case "melee":
                return "近战";
            default:
                return slotLabel.Trim();
        }
    }

    private void HandleCollected()
    {
        if (destroyWhenCollected)
        {
            Destroy(gameObject);
            return;
        }

        itemInstance = null;
        RefreshVisuals();
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

    private static Vector3 GetDropScale(ItemInstance instance)
    {
        if (instance == null)
        {
            return new Vector3(0.16f, 0.09f, 0.16f);
        }

        if (instance.IsWeapon && instance.WeaponDefinition != null)
        {
            if (instance.WeaponDefinition.IsThrowableWeapon)
            {
                return new Vector3(0.18f, 0.18f, 0.18f);
            }

            return instance.WeaponDefinition.IsMeleeWeapon
                ? new Vector3(0.14f, 0.52f, 0.14f)
                : new Vector3(0.48f, 0.1f, 0.18f);
        }

        float stackFactor = Mathf.Clamp01(Mathf.Log10(Mathf.Max(1, instance.Quantity) + 1f));
        float weightFactor = Mathf.Clamp01(instance.Definition != null ? instance.Definition.UnitWeight : 0f);
        float size = Mathf.Lerp(0.16f, 0.32f, Mathf.Max(stackFactor, weightFactor));
        return new Vector3(size, size * 0.55f, size);
    }

    private static float GetDropMass(ItemInstance instance)
    {
        if (instance == null)
        {
            return 0.2f;
        }

        if (instance.IsWeapon && instance.WeaponDefinition != null)
        {
            if (instance.WeaponDefinition.IsThrowableWeapon)
            {
                return 0.5f;
            }

            return instance.WeaponDefinition.IsMeleeWeapon ? 0.7f : 1.5f;
        }

        return GetUnitWeight(instance) * Mathf.Max(instance.Quantity, 1);
    }

    private static float GetUnitWeight(ItemInstance instance)
    {
        if (instance == null)
        {
            return 0f;
        }

        if (instance.IsWeapon && instance.WeaponDefinition != null)
        {
            return instance.WeaponDefinition.UnitWeight;
        }

        return instance.Definition != null ? instance.Definition.UnitWeight : 0f;
    }

    private static string GetPickupId(ItemInstance instance)
    {
        if (instance == null)
        {
            return "item";
        }

        if (instance.IsWeapon && instance.WeaponDefinition != null)
        {
            return instance.WeaponDefinition.WeaponId;
        }

        return instance.Definition != null ? instance.Definition.ItemId : "item";
    }

    private static PrimitiveType GetPrimitiveType(ItemInstance instance)
    {
        return instance != null && instance.IsWeapon && instance.WeaponDefinition != null && instance.WeaponDefinition.IsMeleeWeapon
            ? PrimitiveType.Capsule
            : PrimitiveType.Cube;
    }

    private static GameObject CreatePickupObject(
        string objectName,
        Vector3 worldPosition,
        Quaternion worldRotation,
        Vector3 localScale,
        ItemInstance instance,
        string verb)
    {
        GameObject pickupObject = GameObject.CreatePrimitive(GetPrimitiveType(instance));
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

        GroundLootItem groundLoot = pickupObject.AddComponent<GroundLootItem>();
        groundLoot.Configure(instance, verb);
        return pickupObject;
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

        Color color = itemInstance != null && itemInstance.IsDefined()
            ? ItemRarityUtility.GetDisplayColor(Rarity)
            : new Color(0.35f, 0.35f, 0.35f, 1f);
        material.color = color;
    }
}
