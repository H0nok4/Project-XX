using UnityEngine;

namespace ProjectXX.Domain.Inventory
{
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "ProjectXX/Definitions/Item Definition")]
    public class ProjectXXItemDefinition : ScriptableObject
    {
        [SerializeField] private string definitionId = "item.new";
        [SerializeField] private string displayName = "New Item";
        [SerializeField] private ProjectXXItemCategory category = ProjectXXItemCategory.Misc;
        [SerializeField, Min(1)] private int maxStackSize = 1;
        [SerializeField, Min(1)] private int gridWidth = 1;
        [SerializeField, Min(1)] private int gridHeight = 1;

        public string DefinitionId => string.IsNullOrWhiteSpace(definitionId)
            ? name
            : definitionId.Trim();

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? DefinitionId
            : displayName.Trim();

        public ProjectXXItemCategory Category => category;
        public int MaxStackSize => Mathf.Max(1, maxStackSize);
        public bool IsStackable => MaxStackSize > 1;
        public ProjectXXGridSize GridSize => new ProjectXXGridSize(gridWidth, gridHeight);

        public void Configure(
            string id,
            string label,
            ProjectXXItemCategory itemCategory,
            int stackSize,
            ProjectXXGridSize size)
        {
            definitionId = string.IsNullOrWhiteSpace(id) ? definitionId : id.Trim();
            displayName = string.IsNullOrWhiteSpace(label) ? displayName : label.Trim();
            category = itemCategory;
            maxStackSize = Mathf.Max(1, stackSize);
            gridWidth = Mathf.Max(1, size.Width);
            gridHeight = Mathf.Max(1, size.Height);
        }
    }
}
