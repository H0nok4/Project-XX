using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectXX.Domain.Inventory
{
    [Serializable]
    public sealed class ProjectXXInventoryGridRuntime
    {
        [SerializeField, Min(1)] private int width = 6;
        [SerializeField, Min(1)] private int height = 4;
        [SerializeField] private List<ProjectXXItemPlacementRuntime> placements = new List<ProjectXXItemPlacementRuntime>();

        public ProjectXXInventoryGridRuntime()
        {
        }

        public ProjectXXInventoryGridRuntime(int width, int height)
        {
            Configure(new ProjectXXGridSize(width, height), true);
        }

        public ProjectXXGridSize Size => new ProjectXXGridSize(width, height);
        public IReadOnlyList<ProjectXXItemPlacementRuntime> Placements => EnsurePlacements();

        public void Configure(ProjectXXGridSize size, bool clearContents)
        {
            width = size.Width;
            height = size.Height;

            if (clearContents)
            {
                EnsurePlacements().Clear();
            }
        }

        public bool TryAdd(ProjectXXItemInstanceRuntime itemInstance, ProjectXXGridCoord origin, bool rotated, out ProjectXXItemPlacementRuntime placement)
        {
            placement = null;
            if (!CanPlace(itemInstance, origin, rotated))
            {
                return false;
            }

            placement = new ProjectXXItemPlacementRuntime(itemInstance, origin, rotated);
            EnsurePlacements().Add(placement);
            return true;
        }

        public bool TryAddFirstFit(ProjectXXItemInstanceRuntime itemInstance, bool allowRotation, out ProjectXXItemPlacementRuntime placement)
        {
            placement = null;
            if (!TryFindFirstFit(itemInstance, allowRotation, out ProjectXXGridCoord origin, out bool rotated))
            {
                return false;
            }

            return TryAdd(itemInstance, origin, rotated, out placement);
        }

        public bool TryFindFirstFit(ProjectXXItemInstanceRuntime itemInstance, bool allowRotation, out ProjectXXGridCoord origin, out bool rotated)
        {
            origin = default;
            rotated = false;

            if (itemInstance == null || !itemInstance.IsValid)
            {
                return false;
            }

            for (int y = 0; y < Size.Height; y++)
            {
                for (int x = 0; x < Size.Width; x++)
                {
                    ProjectXXGridCoord candidate = new ProjectXXGridCoord(x, y);
                    if (CanPlace(itemInstance, candidate, false))
                    {
                        origin = candidate;
                        return true;
                    }

                    if (allowRotation &&
                        itemInstance.GridSize.Width != itemInstance.GridSize.Height &&
                        CanPlace(itemInstance, candidate, true))
                    {
                        origin = candidate;
                        rotated = true;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryMove(ProjectXXItemPlacementRuntime placement, ProjectXXGridCoord origin, bool rotated)
        {
            if (placement == null || !CanPlace(placement.ItemInstance, origin, rotated, placement))
            {
                return false;
            }

            placement.SetPosition(origin);
            placement.SetRotation(rotated);
            return true;
        }

        public bool Remove(ProjectXXItemPlacementRuntime placement)
        {
            return placement != null && EnsurePlacements().Remove(placement);
        }

        public ProjectXXItemPlacementRuntime FindAt(ProjectXXGridCoord coord)
        {
            List<ProjectXXItemPlacementRuntime> currentPlacements = EnsurePlacements();
            for (int i = 0; i < currentPlacements.Count; i++)
            {
                ProjectXXItemPlacementRuntime placement = currentPlacements[i];
                if (placement != null && placement.Contains(coord))
                {
                    return placement;
                }
            }

            return null;
        }

        public bool CanPlace(ProjectXXItemInstanceRuntime itemInstance, ProjectXXGridCoord origin, bool rotated, ProjectXXItemPlacementRuntime ignoredPlacement = null)
        {
            if (itemInstance == null || !itemInstance.IsValid)
            {
                return false;
            }

            ProjectXXGridSize itemSize = rotated ? itemInstance.GridSize.Rotated : itemInstance.GridSize;
            if (!ContainsRect(origin, itemSize))
            {
                return false;
            }

            List<ProjectXXItemPlacementRuntime> currentPlacements = EnsurePlacements();
            for (int i = 0; i < currentPlacements.Count; i++)
            {
                ProjectXXItemPlacementRuntime placement = currentPlacements[i];
                if (placement == null || placement == ignoredPlacement)
                {
                    continue;
                }

                if (Overlaps(origin, itemSize, placement.Origin, placement.OccupiedSize))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ContainsRect(ProjectXXGridCoord origin, ProjectXXGridSize size)
        {
            return origin.X >= 0 &&
                   origin.Y >= 0 &&
                   origin.X + size.Width <= Size.Width &&
                   origin.Y + size.Height <= Size.Height;
        }

        private static bool Overlaps(ProjectXXGridCoord aOrigin, ProjectXXGridSize aSize, ProjectXXGridCoord bOrigin, ProjectXXGridSize bSize)
        {
            return aOrigin.X < bOrigin.X + bSize.Width &&
                   aOrigin.X + aSize.Width > bOrigin.X &&
                   aOrigin.Y < bOrigin.Y + bSize.Height &&
                   aOrigin.Y + aSize.Height > bOrigin.Y;
        }

        private List<ProjectXXItemPlacementRuntime> EnsurePlacements()
        {
            placements ??= new List<ProjectXXItemPlacementRuntime>();
            return placements;
        }
    }
}
