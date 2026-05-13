using ProjectXX.Domain.Inventory;
using UnityEngine;

namespace ProjectXX.Domain.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXContainerRuntime))]
    public sealed class ProjectXXContainerInteractable : MonoBehaviour, IProjectXXInteractable
    {
        [SerializeField] private ProjectXXContainerRuntime containerRuntime;
        [SerializeField] private string interactionVerb = "Open";

        public ProjectXXContainerRuntime ContainerRuntime => containerRuntime;

        public string InteractionPrompt
        {
            get
            {
                string verb = string.IsNullOrWhiteSpace(interactionVerb) ? "Open" : interactionVerb.Trim();
                string targetName = containerRuntime != null ? containerRuntime.DisplayName : name;
                return $"Press [E] to {verb} {targetName}";
            }
        }

        private void Awake()
        {
            RefreshReferences();
        }

        private void Reset()
        {
            RefreshReferences();
        }

        public bool CanInteract(GameObject interactor)
        {
            return isActiveAndEnabled && interactor != null && containerRuntime != null;
        }

        public ProjectXXInteractionResult Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return ProjectXXInteractionResult.Failed("Container is not available.");
            }

            return ProjectXXInteractionResult.Success($"Opened {containerRuntime.DisplayName}.");
        }

        public void RefreshReferences()
        {
            if (containerRuntime == null)
            {
                containerRuntime = GetComponent<ProjectXXContainerRuntime>();
            }
        }
    }
}
