using UnityEngine;

namespace ProjectXX.Domain.Inventory
{
    [DisallowMultipleComponent]
    public sealed class ProjectXXContainerRuntime : MonoBehaviour
    {
        [SerializeField] private ProjectXXContainerDefinition definition;
        [SerializeField] private string containerId = "container.new";
        [SerializeField] private string displayName = "Container";
        [SerializeField] private ProjectXXInventoryGridRuntime grid = new ProjectXXInventoryGridRuntime(6, 4);

        public ProjectXXContainerDefinition Definition => definition;
        public string ContainerId => definition != null ? definition.DefinitionId : string.IsNullOrWhiteSpace(containerId) ? name : containerId.Trim();
        public string DisplayName => definition != null ? definition.DisplayName : string.IsNullOrWhiteSpace(displayName) ? ContainerId : displayName.Trim();
        public ProjectXXInventoryGridRuntime Grid => grid ??= new ProjectXXInventoryGridRuntime(6, 4);

        private void Awake()
        {
            ApplyDefinition(false);
        }

        public void Configure(string id, string label, ProjectXXGridSize size, bool clearContents)
        {
            definition = null;
            containerId = string.IsNullOrWhiteSpace(id) ? containerId : id.Trim();
            displayName = string.IsNullOrWhiteSpace(label) ? displayName : label.Trim();
            Grid.Configure(size, clearContents);
        }

        public void SetDefinition(ProjectXXContainerDefinition containerDefinition, bool clearContents)
        {
            definition = containerDefinition;
            ApplyDefinition(clearContents);
        }

        public bool TryAdd(ProjectXXItemDefinition definition, int quantity, ProjectXXGridCoord origin, bool rotated, out ProjectXXItemPlacementRuntime placement)
        {
            placement = null;
            if (definition == null)
            {
                return false;
            }

            return Grid.TryAdd(new ProjectXXItemInstanceRuntime(definition, quantity), origin, rotated, out placement);
        }

        public bool TryAddFirstFit(ProjectXXItemDefinition definition, int quantity, bool allowRotation, out ProjectXXItemPlacementRuntime placement)
        {
            placement = null;
            if (definition == null)
            {
                return false;
            }

            return Grid.TryAddFirstFit(new ProjectXXItemInstanceRuntime(definition, quantity), allowRotation, out placement);
        }

        private void ApplyDefinition(bool clearContents)
        {
            if (definition == null)
            {
                return;
            }

            Grid.Configure(definition.GridSize, clearContents);
        }
    }
}
