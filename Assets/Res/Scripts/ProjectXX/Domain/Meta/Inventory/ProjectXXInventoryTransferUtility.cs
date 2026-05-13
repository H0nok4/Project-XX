namespace ProjectXX.Domain.Inventory
{
    public static class ProjectXXInventoryTransferUtility
    {
        public static bool TryMoveFirst(
            ProjectXXInventoryGridRuntime sourceGrid,
            ProjectXXInventoryGridRuntime targetGrid,
            bool allowRotation,
            out ProjectXXItemPlacementRuntime movedPlacement)
        {
            movedPlacement = null;
            if (sourceGrid == null || targetGrid == null || ReferenceEquals(sourceGrid, targetGrid))
            {
                return false;
            }

            System.Collections.Generic.IReadOnlyList<ProjectXXItemPlacementRuntime> placements = sourceGrid.Placements;
            for (int i = 0; i < placements.Count; i++)
            {
                ProjectXXItemPlacementRuntime placement = placements[i];
                if (placement == null || !placement.IsValid)
                {
                    continue;
                }

                if (TryMove(sourceGrid, targetGrid, placement, allowRotation, out movedPlacement))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryMove(
            ProjectXXInventoryGridRuntime sourceGrid,
            ProjectXXInventoryGridRuntime targetGrid,
            ProjectXXItemPlacementRuntime sourcePlacement,
            bool allowRotation,
            out ProjectXXItemPlacementRuntime movedPlacement)
        {
            movedPlacement = null;
            if (sourceGrid == null ||
                targetGrid == null ||
                ReferenceEquals(sourceGrid, targetGrid) ||
                sourcePlacement == null ||
                !sourcePlacement.IsValid)
            {
                return false;
            }

            if (!targetGrid.TryAddFirstFit(sourcePlacement.ItemInstance, allowRotation, out movedPlacement))
            {
                return false;
            }

            if (sourceGrid.Remove(sourcePlacement))
            {
                return true;
            }

            targetGrid.Remove(movedPlacement);
            movedPlacement = null;
            return false;
        }
    }
}
