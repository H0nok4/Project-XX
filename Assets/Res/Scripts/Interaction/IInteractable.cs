using UnityEngine;

public interface IInteractable
{
    string GetInteractionLabel(PlayerInteractor interactor);
    bool CanInteract(PlayerInteractor interactor);
    void Interact(PlayerInteractor interactor);
    Transform GetInteractionTransform();
}
