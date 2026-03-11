using UnityEngine;

[DisallowMultipleComponent]
public sealed class BaseHubTerminalInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private BaseHubDirector director;
    [SerializeField] private BaseHubInteractionKind interactionKind = BaseHubInteractionKind.Deploy;
    [SerializeField] private string interactionLabelOverride = string.Empty;

    private void Awake()
    {
        ResolveDirector();
    }

    private void OnValidate()
    {
        ResolveDirector();
    }

    public string GetInteractionLabel(PlayerInteractor interactor)
    {
        if (!string.IsNullOrWhiteSpace(interactionLabelOverride))
        {
            return interactionLabelOverride.Trim();
        }

        return interactionKind == BaseHubInteractionKind.Warehouse
            ? "打开仓库"
            : "打开出击终端";
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        ResolveDirector();
        return director != null;
    }

    public void Interact(PlayerInteractor interactor)
    {
        director?.OpenInteraction(interactionKind);
    }

    public Transform GetInteractionTransform()
    {
        return transform;
    }

    private void ResolveDirector()
    {
        if (director == null)
        {
            director = FindFirstObjectByType<BaseHubDirector>();
        }
    }
}
