using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(InventoryContainer))]
public class LootContainer : MonoBehaviour, IInteractable
{
    [SerializeField] private string containerLabel = "Supply Crate";
    [SerializeField] private string interactionVerb = "Search";
    [SerializeField] private InventoryContainer inventory;
    [SerializeField] private LootTableDefinition lootTable;
    [SerializeField] private bool populateOnFirstOpen = true;
    [SerializeField] private bool populateOnlyWhenEmpty = true;
    [SerializeField] private bool lootGenerated;

    public InventoryContainer Inventory => inventory;
    public string ContainerLabel => string.IsNullOrWhiteSpace(containerLabel) ? name : containerLabel.Trim();

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void Configure(string label, InventoryContainer targetInventory, string verb = "Search")
    {
        containerLabel = string.IsNullOrWhiteSpace(label) ? name : label.Trim();
        interactionVerb = string.IsNullOrWhiteSpace(verb) ? "Search" : verb.Trim();
        inventory = targetInventory;
        ResolveReferences();
    }

    public void ConfigureRandomLoot(LootTableDefinition table, bool generateOnFirstOpen = true, bool onlyWhenEmpty = true)
    {
        lootTable = table;
        populateOnFirstOpen = generateOnFirstOpen;
        populateOnlyWhenEmpty = onlyWhenEmpty;
        lootGenerated = false;
    }

    public string GetInteractionLabel(PlayerInteractor interactor)
    {
        PrototypeCorpseLoot corpseLoot = GetComponent<PrototypeCorpseLoot>();
        bool hasCorpseWeapons = corpseLoot != null && corpseLoot.HasWeapons;
        bool isKnownEmpty = inventory != null
            && inventory.IsEmpty
            && !hasCorpseWeapons
            && (!populateOnFirstOpen || lootGenerated || lootTable == null);
        string suffix = isKnownEmpty ? " (Empty)" : string.Empty;
        return $"{interactionVerb} {ContainerLabel}{suffix}";
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return interactor != null && inventory != null;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        EnsureGeneratedLoot();

        LootContainerWindowController windowController = interactor.GetComponent<LootContainerWindowController>();
        if (windowController == null)
        {
            windowController = interactor.gameObject.AddComponent<LootContainerWindowController>();
        }

        windowController.ToggleContainer(this);
    }

    public Transform GetInteractionTransform()
    {
        return transform;
    }

    public void EnsureGeneratedLoot()
    {
        if (!populateOnFirstOpen || lootGenerated || lootTable == null || inventory == null)
        {
            return;
        }

        if (populateOnlyWhenEmpty && !inventory.IsEmpty)
        {
            lootGenerated = true;
            return;
        }

        var rolls = lootTable.RollLoot();
        for (int index = 0; index < rolls.Count; index++)
        {
            LootTableDefinition.LootRoll roll = rolls[index];
            if (roll.Definition != null && roll.Quantity > 0)
            {
                inventory.TryAddItem(roll.Definition, roll.Quantity, out _);
            }
        }

        lootGenerated = true;
    }

    private void ResolveReferences()
    {
        if (inventory == null)
        {
            inventory = GetComponent<InventoryContainer>();
        }

        containerLabel = string.IsNullOrWhiteSpace(containerLabel) ? name : containerLabel.Trim();
        interactionVerb = string.IsNullOrWhiteSpace(interactionVerb) ? "Search" : interactionVerb.Trim();
    }
}
