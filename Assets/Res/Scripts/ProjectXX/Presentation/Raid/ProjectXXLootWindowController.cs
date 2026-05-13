using System.Collections.Generic;
using ProjectXX.Domain.Inventory;
using ProjectXX.Domain.Raid;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectXX.Presentation.Raid
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXLoadoutRuntime))]
    public sealed class ProjectXXLootWindowController : PrefabWindowBase<ProjectXXLootWindowTemplate>
    {
        private const string LootWindowPrefabId = "ProjectXXLootWindow";
        private const string LootWindowPrefabPath = "UI/Loot/ProjectXXLootWindow";

        [SerializeField] private RaidSessionRuntime sessionRuntime;
        [SerializeField] private ProjectXXLoadoutRuntime loadoutRuntime;
        [SerializeField, Min(24f)] private float gridCellSize = 54f;
        [SerializeField, Min(0f)] private float gridCellSpacing = 4f;
        [SerializeField] private Color containerItemColor = new Color(0.34f, 0.42f, 0.34f, 0.96f);
        [SerializeField] private Color backpackItemColor = new Color(0.28f, 0.36f, 0.48f, 0.96f);

        private readonly List<GameObject> renderedObjects = new List<GameObject>();
        private ProjectXXContainerRuntime activeContainer;

        public bool IsOpen => IsWindowVisible && activeContainer != null;

        public override int ManagedInputPriority => 80;
        protected override string WindowPrefabId => LootWindowPrefabId;
        protected override string WindowPrefabResourcePath => LootWindowPrefabPath;
        protected override bool VisibleOnAwake => false;
        protected override string WindowName => "ProjectXXLootWindow";
        protected override string WindowTitle => "Loot Transfer";
        protected override string WindowSubtitle => "Click occupied items to move them between container and backpack.";
        protected override Vector2 WindowSize => new Vector2(1100f, 720f);

        protected override void Awake()
        {
            base.Awake();
            RefreshReferences();
        }

        private void OnDisable()
        {
            if (IsWindowBuilt)
            {
                Close(false);
            }
        }

        protected override void OnDestroy()
        {
            if (IsWindowBuilt)
            {
                Close(false);
            }

            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsOpen || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                TakeFirstItem();
                return;
            }

            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                StashFirstItem();
            }
        }

        public void Configure(RaidSessionRuntime runtime, ProjectXXLoadoutRuntime loadout)
        {
            sessionRuntime = runtime;
            loadoutRuntime = loadout;
            RefreshReferences();
        }

        public void Open(ProjectXXContainerRuntime containerRuntime)
        {
            RefreshReferences();
            if (containerRuntime == null || loadoutRuntime == null)
            {
                sessionRuntime?.SetInteractionPrompt("Loot window is missing container or backpack runtime.");
                return;
            }

            activeContainer = containerRuntime;
            EnsureWindow();
            BindTemplateCallbacks();
            RefreshWindow();
            SetMessage($"Opened {activeContainer.DisplayName}. Click items, T take first, G stash first, Esc close.");
            ShowWindow();
            ProjectXXFpsUiInputGate.GetOrCreate().RefreshNow();
        }

        public void Close(bool updatePrompt)
        {
            ProjectXXContainerRuntime closedContainer = activeContainer;
            activeContainer = null;

            if (IsWindowBuilt)
            {
                HideWindow();
            }

            ProjectXXFpsUiInputGate.GetOrCreate().RefreshNow();

            if (updatePrompt && closedContainer != null)
            {
                sessionRuntime?.SetInteractionPrompt($"Closed {closedContainer.DisplayName}.");
            }
        }

        public void RefreshReferences()
        {
            if (loadoutRuntime == null)
            {
                loadoutRuntime = GetComponent<ProjectXXLoadoutRuntime>();
            }
        }

        public void TakeFirstItem()
        {
            if (!IsOpen || loadoutRuntime == null)
            {
                return;
            }

            bool moved = ProjectXXInventoryTransferUtility.TryMoveFirst(
                activeContainer.Grid,
                loadoutRuntime.BackpackGrid,
                true,
                out ProjectXXItemPlacementRuntime placement);

            SetMessage(moved
                ? $"Took {DescribePlacement(placement)}."
                : "No fitting backpack space, or container is empty.");
            RefreshWindow();
        }

        public void StashFirstItem()
        {
            if (!IsOpen || loadoutRuntime == null)
            {
                return;
            }

            bool moved = ProjectXXInventoryTransferUtility.TryMoveFirst(
                loadoutRuntime.BackpackGrid,
                activeContainer.Grid,
                true,
                out ProjectXXItemPlacementRuntime placement);

            SetMessage(moved
                ? $"Stashed {DescribePlacement(placement)}."
                : "No fitting container space, or backpack is empty.");
            RefreshWindow();
        }

        public override bool TryHandleUiCancel()
        {
            if (!IsOpen)
            {
                return false;
            }

            Close(true);
            return true;
        }

        protected override PrototypeUiToolkit.WindowChrome CreatePrefabWindowChrome(ProjectXXLootWindowTemplate template)
        {
            return template != null ? template.CreateWindowChrome() : null;
        }

        protected override void BuildPrefabWindow(ProjectXXLootWindowTemplate template, PrototypeUiToolkit.WindowChrome chrome)
        {
            if (template == null || chrome == null || chrome.Root == null)
            {
                return;
            }

            BindTemplateCallbacks();
            ApplyHeaderText(template);
            SetMessage(string.Empty);
        }

        protected override void OnWindowVisibilityChanged(bool visible)
        {
            base.OnWindowVisibilityChanged(visible);
            ProjectXXFpsUiInputGate.GetOrCreate().RefreshNow();
        }

        private void BindTemplateCallbacks()
        {
            if (Template == null)
            {
                return;
            }

            BindButton(Template.TakeFirstButton, TakeFirstItem);
            BindButton(Template.StashFirstButton, StashFirstItem);
            BindButton(Template.CloseButton, () => Close(true));
        }

        private void ApplyHeaderText(ProjectXXLootWindowTemplate template)
        {
            if (template.TitleText != null)
            {
                template.TitleText.text = WindowTitle;
            }

            if (template.SubtitleText != null)
            {
                template.SubtitleText.text = WindowSubtitle;
            }
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void RefreshWindow()
        {
            if (Template == null)
            {
                return;
            }

            ApplyHeaderText(Template);
            RefreshSectionText(
                Template.ContainerTitleText,
                Template.ContainerSummaryText,
                activeContainer != null ? activeContainer.DisplayName : "Container",
                activeContainer != null ? activeContainer.Grid : null);
            RefreshSectionText(
                Template.BackpackTitleText,
                Template.BackpackSummaryText,
                "Backpack",
                loadoutRuntime != null ? loadoutRuntime.BackpackGrid : null);

            RenderGrid(
                activeContainer != null ? activeContainer.Grid : null,
                Template.ContainerGridRoot,
                Template.ContainerCellsRoot,
                Template.ContainerItemsRoot,
                true);
            RenderGrid(
                loadoutRuntime != null ? loadoutRuntime.BackpackGrid : null,
                Template.BackpackGridRoot,
                Template.BackpackCellsRoot,
                Template.BackpackItemsRoot,
                false);
        }

        private void RefreshSectionText(TMP_Text titleText, TMP_Text summaryText, string title, ProjectXXInventoryGridRuntime grid)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (summaryText != null)
            {
                summaryText.text = BuildGridSummary(grid);
            }
        }

        private void RenderGrid(
            ProjectXXInventoryGridRuntime grid,
            RectTransform gridRoot,
            RectTransform cellsRoot,
            RectTransform itemsRoot,
            bool sourceIsContainer)
        {
            if (gridRoot == null || cellsRoot == null || itemsRoot == null || Template == null)
            {
                return;
            }

            ClearRenderedChildren(cellsRoot);
            ClearRenderedChildren(itemsRoot);

            ProjectXXGridSize size = grid != null ? grid.Size : ProjectXXGridSize.One;
            Vector2 gridSize = CalculateGridPixelSize(size);
            gridRoot.sizeDelta = gridSize;
            SetStretch(cellsRoot);
            SetStretch(itemsRoot);

            RectTransform cellTemplate = Template.GridCellTemplate;
            ProjectXXInventoryGridItemTemplate itemTemplate = Template.GridItemTemplate;
            if (cellTemplate == null || itemTemplate == null)
            {
                return;
            }

            for (int y = 0; y < size.Height; y++)
            {
                for (int x = 0; x < size.Width; x++)
                {
                    RectTransform cell = Instantiate(cellTemplate, cellsRoot);
                    cell.name = $"Cell {x},{y}";
                    cell.gameObject.SetActive(true);
                    PlaceGridRect(cell, new ProjectXXGridCoord(x, y), ProjectXXGridSize.One);
                }
            }

            if (grid == null)
            {
                return;
            }

            IReadOnlyList<ProjectXXItemPlacementRuntime> placements = grid.Placements;
            for (int i = 0; i < placements.Count; i++)
            {
                ProjectXXItemPlacementRuntime placement = placements[i];
                if (placement == null || !placement.IsValid)
                {
                    continue;
                }

                ProjectXXInventoryGridItemTemplate item = Instantiate(itemTemplate, itemsRoot);
                item.name = ResolveItemName(placement);
                item.gameObject.SetActive(true);
                PlaceGridRect(item.Root, placement.Origin, placement.OccupiedSize);
                BindItem(item, placement, sourceIsContainer);
            }
        }

        private void BindItem(ProjectXXInventoryGridItemTemplate item, ProjectXXItemPlacementRuntime placement, bool sourceIsContainer)
        {
            if (item == null || placement == null)
            {
                return;
            }

            ProjectXXItemDefinition definition = placement.ItemInstance.Definition;
            string itemName = definition != null ? definition.DisplayName : "Unknown Item";
            ProjectXXGridSize size = placement.OccupiedSize;

            if (item.NameText != null)
            {
                item.NameText.text = itemName;
            }

            if (item.QuantityText != null)
            {
                item.QuantityText.text = placement.ItemInstance.Quantity > 1
                    ? $"x{placement.ItemInstance.Quantity}"
                    : string.Empty;
            }

            if (item.SizeText != null)
            {
                item.SizeText.text = $"{size.Width}x{size.Height}";
            }

            if (item.BackgroundImage != null)
            {
                item.BackgroundImage.color = sourceIsContainer ? containerItemColor : backpackItemColor;
            }

            if (item.Button != null)
            {
                item.Button.onClick.RemoveAllListeners();
                ProjectXXItemPlacementRuntime capturedPlacement = placement;
                bool capturedSourceIsContainer = sourceIsContainer;
                item.Button.onClick.AddListener(() => MovePlacement(capturedSourceIsContainer, capturedPlacement));
            }
        }

        private void MovePlacement(bool sourceIsContainer, ProjectXXItemPlacementRuntime placement)
        {
            if (!IsOpen || placement == null || loadoutRuntime == null || activeContainer == null)
            {
                return;
            }

            ProjectXXInventoryGridRuntime sourceGrid = sourceIsContainer
                ? activeContainer.Grid
                : loadoutRuntime.BackpackGrid;
            ProjectXXInventoryGridRuntime targetGrid = sourceIsContainer
                ? loadoutRuntime.BackpackGrid
                : activeContainer.Grid;

            bool moved = ProjectXXInventoryTransferUtility.TryMove(
                sourceGrid,
                targetGrid,
                placement,
                true,
                out ProjectXXItemPlacementRuntime movedPlacement);

            SetMessage(moved
                ? (sourceIsContainer ? $"Took {DescribePlacement(movedPlacement)}." : $"Stashed {DescribePlacement(movedPlacement)}.")
                : "No fitting grid space for that item.");
            RefreshWindow();
        }

        private void SetMessage(string message)
        {
            string resolvedMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
            if (Template != null && Template.MessageText != null)
            {
                Template.MessageText.text = resolvedMessage;
                Template.MessageText.gameObject.SetActive(!string.IsNullOrEmpty(resolvedMessage));
            }

            sessionRuntime?.SetInteractionPrompt(resolvedMessage);
        }

        private Vector2 CalculateGridPixelSize(ProjectXXGridSize size)
        {
            return new Vector2(
                size.Width * gridCellSize + Mathf.Max(0, size.Width - 1) * gridCellSpacing,
                size.Height * gridCellSize + Mathf.Max(0, size.Height - 1) * gridCellSpacing);
        }

        private void PlaceGridRect(RectTransform rectTransform, ProjectXXGridCoord origin, ProjectXXGridSize size)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(
                origin.X * (gridCellSize + gridCellSpacing),
                -origin.Y * (gridCellSize + gridCellSpacing));
            rectTransform.sizeDelta = CalculateGridPixelSize(size);
        }

        private void ClearRenderedChildren(RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            renderedObjects.Clear();
            for (int i = 0; i < parent.childCount; i++)
            {
                renderedObjects.Add(parent.GetChild(i).gameObject);
            }

            for (int i = 0; i < renderedObjects.Count; i++)
            {
                Destroy(renderedObjects[i]);
            }

            renderedObjects.Clear();
        }

        private static void SetStretch(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static string BuildGridSummary(ProjectXXInventoryGridRuntime grid)
        {
            if (grid == null)
            {
                return "No grid runtime.";
            }

            ProjectXXGridSize size = grid.Size;
            int occupiedArea = 0;
            IReadOnlyList<ProjectXXItemPlacementRuntime> placements = grid.Placements;
            for (int i = 0; i < placements.Count; i++)
            {
                ProjectXXItemPlacementRuntime placement = placements[i];
                if (placement == null || !placement.IsValid)
                {
                    continue;
                }

                occupiedArea += placement.OccupiedSize.Area;
            }

            return $"{size.Width} x {size.Height} grid  |  {occupiedArea}/{size.Area} cells used";
        }

        private static string DescribePlacement(ProjectXXItemPlacementRuntime placement)
        {
            if (placement == null || !placement.IsValid)
            {
                return "item";
            }

            return $"{ResolveItemName(placement)} x{placement.ItemInstance.Quantity}";
        }

        private static string ResolveItemName(ProjectXXItemPlacementRuntime placement)
        {
            ProjectXXItemDefinition definition = placement != null && placement.ItemInstance != null
                ? placement.ItemInstance.Definition
                : null;
            return definition != null ? definition.DisplayName : "item";
        }
    }
}
