using UnityEngine;

namespace ProjectXX.Domain.Inventory
{
    [CreateAssetMenu(fileName = "ContainerDefinition", menuName = "ProjectXX/Definitions/Container Definition")]
    public sealed class ProjectXXContainerDefinition : ScriptableObject
    {
        [SerializeField] private string definitionId = "container.new";
        [SerializeField] private string displayName = "Container";
        [SerializeField, Min(1)] private int gridWidth = 6;
        [SerializeField, Min(1)] private int gridHeight = 4;

        public string DefinitionId => string.IsNullOrWhiteSpace(definitionId)
            ? name
            : definitionId.Trim();

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? DefinitionId
            : displayName.Trim();

        public ProjectXXGridSize GridSize => new ProjectXXGridSize(gridWidth, gridHeight);
    }
}
