using UnityEngine;

namespace ProjectXX.Domain.Interaction
{
    public interface IProjectXXInteractable
    {
        string InteractionPrompt { get; }

        bool CanInteract(GameObject interactor);

        ProjectXXInteractionResult Interact(GameObject interactor);
    }
}
