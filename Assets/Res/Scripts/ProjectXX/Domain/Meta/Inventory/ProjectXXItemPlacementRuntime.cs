using System;
using UnityEngine;

namespace ProjectXX.Domain.Inventory
{
    [Serializable]
    public sealed class ProjectXXItemPlacementRuntime
    {
        [SerializeField] private ProjectXXItemInstanceRuntime itemInstance;
        [SerializeField] private ProjectXXGridCoord origin;
        [SerializeField] private bool rotated;

        public ProjectXXItemPlacementRuntime(ProjectXXItemInstanceRuntime itemInstance, ProjectXXGridCoord origin, bool rotated)
        {
            this.itemInstance = itemInstance;
            this.origin = origin;
            this.rotated = rotated;
        }

        public ProjectXXItemInstanceRuntime ItemInstance => itemInstance;
        public ProjectXXGridCoord Origin => origin;
        public bool Rotated => rotated;
        public bool IsValid => itemInstance != null && itemInstance.IsValid;

        public ProjectXXGridSize OccupiedSize
        {
            get
            {
                ProjectXXGridSize size = itemInstance != null ? itemInstance.GridSize : ProjectXXGridSize.One;
                return rotated ? size.Rotated : size;
            }
        }

        public void SetPosition(ProjectXXGridCoord value)
        {
            origin = value;
        }

        public void SetRotation(bool value)
        {
            rotated = value;
        }

        public bool Contains(ProjectXXGridCoord coord)
        {
            ProjectXXGridSize size = OccupiedSize;
            return coord.X >= origin.X &&
                   coord.Y >= origin.Y &&
                   coord.X < origin.X + size.Width &&
                   coord.Y < origin.Y + size.Height;
        }
    }
}
