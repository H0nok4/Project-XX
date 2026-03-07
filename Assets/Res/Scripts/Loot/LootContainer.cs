using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(InventoryContainer))]
public class LootContainer : MonoBehaviour, IInteractable
{
    [SerializeField] private string containerLabel = "Supply Crate";
    [SerializeField] private string interactionVerb = "Search";
    [SerializeField] private InventoryContainer inventory;

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

    public string GetInteractionLabel(PlayerInteractor interactor)
    {
        string suffix = inventory != null && inventory.IsEmpty ? " (Empty)" : string.Empty;
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
