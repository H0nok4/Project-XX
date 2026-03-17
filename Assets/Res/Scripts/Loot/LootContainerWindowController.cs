using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeFpsInput))]
public class LootContainerWindowController : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PrototypeFpsInput fpsInput;
    [SerializeField] private PlayerInteractionState interactionState;
    [SerializeField] private PlayerInventoryWindowController inventoryWindowController;
    [SerializeField] private PrototypeUnitVitals playerVitals;
    [SerializeField] private PrototypeRaidEquipmentController raidEquipmentController;
    [SerializeField] private float maxOpenDistance = 3.5f;

    private LootContainer openContainer;
    private bool contentDirty = true;
    private bool resetLootScroll = true;
    private bool resetBackpackScroll = true;
    private bool resetGearScroll = true;
    private int lastContentHash;
    private PrototypeUiToolkit.WindowChrome windowChrome;
    private Text summaryText;
    private ScrollRect lootScrollRect;
    private RectTransform lootContentRoot;
    private ScrollRect backpackScrollRect;
    private RectTransform backpackContentRoot;
    private ScrollRect gearScrollRect;
    private RectTransform gearContentRoot;

    public LootContainer OpenContainer => openContainer;
    public bool IsOpen => openContainer != null;

    private void Awake()
    {
        ResolveReferences();
        EnsureWindowUi();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (playerVitals != null)
        {
            playerVitals.Died += HandlePlayerDied;
        }

        if (raidEquipmentController != null)
        {
            raidEquipmentController.Changed += MarkDirty;
        }

        if (IsPlayerDead())
        {
            Close();
        }
        else
        {
            UpdateWindowVisibility();
        }
    }

    private void OnDisable()
    {
        if (playerVitals != null)
        {
            playerVitals.Died -= HandlePlayerDied;
        }

        if (raidEquipmentController != null)
        {
            raidEquipmentController.Changed -= MarkDirty;
        }

        Close();
        UpdateWindowVisibility();
    }

    private void OnValidate()
    {
        ResolveReferences();
        maxOpenDistance = Mathf.Max(1f, maxOpenDistance);
    }

    private void Update()
    {
        if (IsPlayerDead())
        {
            if (IsOpen)
            {
                Close();
            }

            return;
        }

        if (!IsOpen)
        {
            UpdateWindowVisibility();
            return;
        }

        if (fpsInput != null && fpsInput.ToggleCursorPressedThisFrame)
        {
            Close();
            return;
        }

        if (openContainer == null || !openContainer.isActiveAndEnabled || !IsWithinOpenDistance())
        {
            Close();
            return;
        }

        RefreshWindowIfNeeded();
    }

    public void ToggleContainer(LootContainer container)
    {
        if (container == null)
        {
            Close();
            return;
        }

        if (openContainer == container)
        {
            Close();
            return;
        }

        Open(container);
    }

    public void Open(LootContainer container)
    {
        if (container == null || IsPlayerDead())
        {
            return;
        }

        ResolveReferences();
        if (inventoryWindowController != null && inventoryWindowController.IsOpen)
        {
            inventoryWindowController.Close();
        }

        openContainer = container;
        resetLootScroll = true;
        resetBackpackScroll = true;
        resetGearScroll = true;
        MarkDirty();
        SetUiFocus(true);
        UpdateWindowVisibility();
        RefreshWindowIfNeeded();
    }

    public void Close()
    {
        if (!IsOpen && (interactionState == null || !interactionState.IsUiFocused))
        {
            return;
        }

        CancelActiveDrag();
        openContainer = null;
        SetUiFocus(false);
        UpdateWindowVisibility();
    }

    private void EnsureWindowUi()
    {
        if (windowChrome?.Root != null)
        {
            return;
        }

        PrototypeRuntimeUiManager manager = PrototypeRuntimeUiManager.GetOrCreate();
        windowChrome = PrototypeUiToolkit.CreateWindowChrome(
            manager.GetLayerRoot(PrototypeUiLayer.Window),
            manager.RuntimeFont,
            "LootContainerWindow",
            "Loot Container",
            "Drag loot into your backpack, equip slots, special equipment, or the secure container.",
            new Vector2(1540f, 760f));

        VerticalLayoutGroup bodyLayout = windowChrome.BodyRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.spacing = 14f;
        bodyLayout.padding = new RectOffset(0, 0, 0, 0);
        bodyLayout.childAlignment = TextAnchor.UpperLeft;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;

        summaryText = PrototypeUiToolkit.CreateText(
            windowChrome.BodyRoot,
            manager.RuntimeFont,
            string.Empty,
            14,
            FontStyle.Normal,
            new Color(0.92f, 0.94f, 0.98f, 1f),
            TextAnchor.UpperLeft);
        summaryText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform contentRow = PrototypeUiToolkit.CreateRectTransform("ContentRow", windowChrome.BodyRoot);
        HorizontalLayoutGroup contentLayout = contentRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        contentLayout.spacing = 16f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = true;
        LayoutElement contentRowLayout = contentRow.gameObject.AddComponent<LayoutElement>();
        contentRowLayout.flexibleWidth = 1f;
        contentRowLayout.flexibleHeight = 1f;
        contentRowLayout.minHeight = 0f;

        RectTransform lootColumn = CreateColumnRoot(contentRow, 0.38f);
        lootScrollRect = PrototypeUiToolkit.CreateScrollView(lootColumn, out _, out RectTransform lootContent, true);
        lootContentRoot = lootContent;
        PrototypeUiToolkit.SetStretch(lootScrollRect.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        RectTransform backpackColumn = CreateColumnRoot(contentRow, 0.32f);
        backpackScrollRect = PrototypeUiToolkit.CreateScrollView(backpackColumn, out _, out RectTransform backpackContent, true);
        backpackContentRoot = backpackContent;
        PrototypeUiToolkit.SetStretch(backpackScrollRect.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        RectTransform gearColumn = CreateColumnRoot(contentRow, 0.3f);
        gearScrollRect = PrototypeUiToolkit.CreateScrollView(gearColumn, out _, out RectTransform gearContent, true);
        gearContentRoot = gearContent;
        PrototypeUiToolkit.SetStretch(gearScrollRect.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        Button takeAllButton = PrototypeUiToolkit.CreateButton(
            windowChrome.FooterRoot,
            manager.RuntimeFont,
            "Take All",
            TakeAll,
            new Color(0.58f, 0.34f, 0.16f, 1f),
            new Color(0.71f, 0.43f, 0.22f, 1f),
            new Color(0.44f, 0.25f, 0.11f, 1f),
            38f);
        takeAllButton.GetComponent<LayoutElement>().preferredWidth = 150f;

        Button closeButton = PrototypeUiToolkit.CreateButton(
            windowChrome.FooterRoot,
            manager.RuntimeFont,
            "Close",
            Close,
            new Color(0.2f, 0.27f, 0.36f, 0.98f),
            new Color(0.29f, 0.38f, 0.49f, 1f),
            new Color(0.16f, 0.22f, 0.3f, 1f),
            38f);
        closeButton.GetComponent<LayoutElement>().preferredWidth = 140f;

        PrototypeUiToolkit.SetVisible(windowChrome.Root, false);
    }

    private void RefreshWindowIfNeeded()
    {
        EnsureWindowUi();
        UpdateWindowVisibility();
        if (!IsOpen || interactor == null || openContainer == null || raidEquipmentController == null)
        {
            return;
        }

        if (summaryText != null)
        {
            summaryText.text = BuildSummaryText();
        }

        if (HasActiveDrag())
        {
            return;
        }

        int contentHash = raidEquipmentController.BuildStateHash(openContainer);
        if (!contentDirty && contentHash == lastContentHash)
        {
            return;
        }

        float lootScroll = lootScrollRect != null ? lootScrollRect.verticalNormalizedPosition : 1f;
        float backpackScroll = backpackScrollRect != null ? backpackScrollRect.verticalNormalizedPosition : 1f;
        float gearScroll = gearScrollRect != null ? gearScrollRect.verticalNormalizedPosition : 1f;
        lastContentHash = contentHash;
        contentDirty = false;

        if (windowChrome.TitleText != null)
        {
            windowChrome.TitleText.text = openContainer.ContainerLabel;
        }

        PrototypeUiToolkit.ClearChildren(lootContentRoot);
        PrototypeUiToolkit.ClearChildren(backpackContentRoot);
        PrototypeUiToolkit.ClearChildren(gearContentRoot);

        BuildLootColumn();
        BuildBackpackColumn();
        BuildGearColumn();

        LayoutRebuilder.ForceRebuildLayoutImmediate(windowChrome.Panel);
        if (lootScrollRect != null)
        {
            lootScrollRect.verticalNormalizedPosition = resetLootScroll ? 1f : Mathf.Clamp01(lootScroll);
        }

        if (backpackScrollRect != null)
        {
            backpackScrollRect.verticalNormalizedPosition = resetBackpackScroll ? 1f : Mathf.Clamp01(backpackScroll);
        }

        if (gearScrollRect != null)
        {
            gearScrollRect.verticalNormalizedPosition = resetGearScroll ? 1f : Mathf.Clamp01(gearScroll);
        }

        resetLootScroll = false;
        resetBackpackScroll = false;
        resetGearScroll = false;
    }

    private void BuildLootColumn()
    {
        InventoryContainer lootInventory = openContainer != null ? openContainer.Inventory : null;
        PrototypeCorpseLoot corpseLoot = openContainer != null ? openContainer.GetComponent<PrototypeCorpseLoot>() : null;

        if (corpseLoot != null && corpseLoot.HasWeapons)
        {
            BuildCorpseWeaponSection(corpseLoot);
        }

        BuildInventorySection(
            lootContentRoot,
            "Container Items",
            lootInventory,
            "This container is empty.",
            PrototypeRaidDropTarget.ForInventory(lootInventory),
            false);
    }

    private void BuildBackpackColumn()
    {
        BuildInventorySection(
            backpackContentRoot,
            "Backpack",
            interactor != null ? interactor.PrimaryInventory : null,
            "Your raid backpack is empty.",
            PrototypeRaidDropTarget.ForInventory(interactor != null ? interactor.PrimaryInventory : null),
            true);
    }

    private void BuildGearColumn()
    {
        BuildSlotCard(gearContentRoot, PrototypeRaidGearSlotType.PrimaryWeapon, "Drag a firearm here to equip it as your primary weapon.");
        BuildSlotCard(gearContentRoot, PrototypeRaidGearSlotType.SecondaryWeapon, "Drag a firearm here to equip it as your secondary weapon.");
        BuildSlotCard(gearContentRoot, PrototypeRaidGearSlotType.MeleeWeapon, "Drag a melee weapon here to equip it.");
        BuildSlotCard(gearContentRoot, PrototypeRaidGearSlotType.Armor, "Drag torso armor here to equip it.");
        BuildSlotCard(gearContentRoot, PrototypeRaidGearSlotType.Helmet, "Drag head armor here to equip it.");

        BuildInventorySection(
            gearContentRoot,
            "Special Equipment",
            interactor != null ? interactor.SpecialInventory : null,
            "No special equipment stored here.",
            PrototypeRaidDropTarget.ForInventory(interactor != null ? interactor.SpecialInventory : null),
            false);

        BuildSlotCard(gearContentRoot, PrototypeRaidGearSlotType.SecureContainer, "Equip a secure container to unlock its storage capacity.");
        BuildInventorySection(
            gearContentRoot,
            "Secure Storage",
            interactor != null ? interactor.SecureInventory : null,
            raidEquipmentController != null && raidEquipmentController.EquippedSecureContainerItem == null
                ? "No secure container is equipped."
                : "No items stored in the secure container.",
            PrototypeRaidDropTarget.ForInventory(interactor != null ? interactor.SecureInventory : null),
            false);
    }

    private void BuildCorpseWeaponSection(PrototypeCorpseLoot corpseLoot)
    {
        RectTransform section = PrototypeUiToolkit.CreatePanel(
            lootContentRoot,
            "CorpseWeaponsSection",
            new Color(0.11f, 0.15f, 0.2f, 0.98f),
            new RectOffset(14, 14, 14, 14),
            8f);
        PrototypeUiToolkit.CreateText(section, PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont, "Corpse Weapons", 18, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        PrototypeUiToolkit.CreateText(section, PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont, $"Weapons {corpseLoot.Weapons.Count}", 13, FontStyle.Normal, new Color(0.82f, 0.87f, 0.93f, 1f), TextAnchor.UpperLeft);

        for (int index = 0; index < corpseLoot.Weapons.Count; index++)
        {
            PrototypeCorpseLoot.WeaponEntry entry = corpseLoot.GetWeaponEntry(index);
            ItemInstance item = entry != null ? entry.CreateInstance() : null;
            if (item == null)
            {
                continue;
            }

            CreateItemCard(section, item, PrototypeRaidItemLocation.FromCorpseWeapon(corpseLoot, index), false);
        }
    }

    private void BuildInventorySection(
        RectTransform parent,
        string title,
        InventoryContainer inventory,
        string emptyLabel,
        PrototypeRaidDropTarget dropTarget,
        bool allowDropButtons)
    {
        RectTransform section = PrototypeUiToolkit.CreatePanel(
            parent,
            title.Replace(" ", string.Empty) + "Section",
            new Color(0.11f, 0.15f, 0.2f, 0.98f),
            new RectOffset(14, 14, 14, 14),
            8f);
        Image background = section.GetComponent<Image>();
        AddDropZone(section.gameObject, dropTarget, background, new Color(0.11f, 0.15f, 0.2f, 0.98f), new Color(0.17f, 0.24f, 0.33f, 1f));

        PrototypeUiToolkit.CreateText(section, PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont, title, 18, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        string sectionSummary = inventory == null
            ? "Unavailable"
            : $"Stacks {inventory.OccupiedSlots}/{inventory.MaxSlots}  Weight {inventory.CurrentWeight:0.0}/{inventory.MaxWeight:0.0}";
        PrototypeUiToolkit.CreateText(section, PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont, sectionSummary, 13, FontStyle.Normal, new Color(0.82f, 0.87f, 0.93f, 1f), TextAnchor.UpperLeft);

        if (inventory == null || inventory.IsEmpty)
        {
            PrototypeUiToolkit.CreateText(section, PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont, emptyLabel, 14, FontStyle.Italic, new Color(0.7f, 0.76f, 0.84f, 1f), TextAnchor.UpperLeft);
            return;
        }

        for (int index = 0; index < inventory.Items.Count; index++)
        {
            ItemInstance item = inventory.Items[index];
            if (item == null || !item.IsDefined())
            {
                continue;
            }

            CreateItemCard(section, item, PrototypeRaidItemLocation.FromInventory(inventory, item), allowDropButtons);
        }
    }

    private void BuildSlotCard(RectTransform parent, PrototypeRaidGearSlotType gearSlot, string emptyLabel)
    {
        RectTransform card = PrototypeUiToolkit.CreatePanel(
            parent,
            gearSlot + "Slot",
            new Color(0.15f, 0.2f, 0.27f, 0.96f),
            new RectOffset(14, 14, 14, 14),
            6f);
        Image background = card.GetComponent<Image>();
        AddDropZone(
            card.gameObject,
            PrototypeRaidInventoryRules.IsWeaponSlot(gearSlot)
                ? PrototypeRaidDropTarget.ForWeaponSlot(gearSlot)
                : PrototypeRaidInventoryRules.IsArmorSlot(gearSlot)
                    ? PrototypeRaidDropTarget.ForArmorSlot(gearSlot)
                    : PrototypeRaidDropTarget.ForSecureContainerGear(),
            background,
            new Color(0.15f, 0.2f, 0.27f, 0.96f),
            new Color(0.22f, 0.3f, 0.4f, 1f));

        PrototypeUiToolkit.CreateText(card, PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont, PrototypeRaidInventoryRules.GetSlotDisplayName(gearSlot), 17, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);

        ItemInstance equippedItem = raidEquipmentController != null ? raidEquipmentController.GetSlotItem(gearSlot) : null;
        if (equippedItem == null)
        {
            PrototypeUiToolkit.CreateText(card, PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont, emptyLabel, 13, FontStyle.Italic, new Color(0.72f, 0.78f, 0.86f, 1f), TextAnchor.UpperLeft);
            return;
        }

        PrototypeUiToolkit.CreateText(card, PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont, equippedItem.RichDisplayName, 15, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        PrototypeUiToolkit.CreateText(card, PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont, PrototypeMainMenuController.BuildItemInstanceDetail(equippedItem), 13, FontStyle.Normal, new Color(0.9f, 0.93f, 0.97f, 1f), TextAnchor.UpperLeft);

        PrototypeRaidItemLocation source = PrototypeRaidInventoryRules.IsWeaponSlot(gearSlot)
            ? PrototypeRaidItemLocation.FromWeaponSlot(gearSlot)
            : PrototypeRaidInventoryRules.IsArmorSlot(gearSlot)
                ? PrototypeRaidItemLocation.FromArmorSlot(gearSlot)
                : PrototypeRaidItemLocation.FromSecureContainerGear();
        AddDragHandle(card.gameObject, source, equippedItem);

        if (gearSlot == PrototypeRaidGearSlotType.SecureContainer
            && PrototypeRaidInventoryRules.TryGetSecureContainerSpec(equippedItem.DefinitionBase, out PrototypeRaidSecureContainerSpec secureSpec))
        {
            PrototypeUiToolkit.CreateText(
                card,
                PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont,
                $"Slots {secureSpec.SlotCount}  Capacity {secureSpec.MaxWeight:0.0}",
                13,
                FontStyle.Normal,
                new Color(0.78f, 0.84f, 0.92f, 1f),
                TextAnchor.UpperLeft);
        }
    }

    private void CreateItemCard(RectTransform parent, ItemInstance item, PrototypeRaidItemLocation source, bool allowDropButton)
    {
        RectTransform card = PrototypeUiToolkit.CreatePanel(
            parent,
            "ItemCard",
            new Color(0.18f, 0.22f, 0.29f, 0.94f),
            new RectOffset(12, 12, 10, 10),
            6f);
        PrototypeUiToolkit.CreateText(card, PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont, item.Quantity > 1 ? $"{item.RichDisplayName} x{item.Quantity}" : item.RichDisplayName, 16, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        PrototypeUiToolkit.CreateText(card, PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont, PrototypeMainMenuController.BuildItemInstanceDetail(item), 13, FontStyle.Normal, new Color(0.9f, 0.93f, 0.97f, 1f), TextAnchor.UpperLeft);
        AddDragHandle(card.gameObject, source, item);

        if (!allowDropButton || source == null || source.Kind != PrototypeRaidItemLocationKind.InventoryItem || source.Inventory != interactor?.PrimaryInventory)
        {
            return;
        }

        Button dropButton = PrototypeUiToolkit.CreateButton(
            card,
            PrototypeRuntimeUiManager.GetOrCreate().RuntimeFont,
            "Drop",
            () =>
            {
                if (raidEquipmentController != null && raidEquipmentController.TryDropBackpackItem(item.InstanceId, out _))
                {
                    MarkDirty();
                }
            },
            new Color(0.33f, 0.22f, 0.19f, 1f),
            new Color(0.44f, 0.29f, 0.24f, 1f),
            new Color(0.26f, 0.17f, 0.15f, 1f),
            34f);
        LayoutElement dropLayout = dropButton.GetComponent<LayoutElement>();
        dropLayout.preferredWidth = 108f;
        dropLayout.flexibleWidth = 0f;
    }

    private void AddDragHandle(GameObject target, PrototypeRaidItemLocation source, ItemInstance item)
    {
        if (target == null || source == null || item == null || raidEquipmentController == null)
        {
            return;
        }

        PrototypeRaidDragHandle handle = target.GetComponent<PrototypeRaidDragHandle>();
        if (handle == null)
        {
            handle = target.AddComponent<PrototypeRaidDragHandle>();
        }

        handle.Configure(new PrototypeRaidDragPayload
        {
            Controller = raidEquipmentController,
            Source = source,
            Item = item.Clone()
        });
    }

    private void AddDropZone(GameObject target, PrototypeRaidDropTarget dropTarget, Image background, Color normalColor, Color highlightColor)
    {
        if (target == null || dropTarget == null || raidEquipmentController == null)
        {
            return;
        }

        PrototypeRaidDropZone dropZone = target.GetComponent<PrototypeRaidDropZone>();
        if (dropZone == null)
        {
            dropZone = target.AddComponent<PrototypeRaidDropZone>();
        }

        dropZone.Configure(dropTarget, MarkDirty, background, normalColor, highlightColor);
    }

    private string BuildSummaryText()
    {
        InventoryContainer lootInventory = openContainer != null ? openContainer.Inventory : null;
        InventoryContainer backpack = interactor != null ? interactor.PrimaryInventory : null;
        PrototypeCorpseLoot corpseLoot = openContainer != null ? openContainer.GetComponent<PrototypeCorpseLoot>() : null;
        string status = raidEquipmentController != null ? raidEquipmentController.StatusMessage : string.Empty;
        string summary =
            $"Container {GetStackCount(lootInventory)}/{GetMaxSlots(lootInventory)}  Weapons {(corpseLoot != null ? corpseLoot.Weapons.Count : 0)}\n" +
            $"Backpack {GetStackCount(backpack)}/{GetMaxSlots(backpack)}  Weight {GetWeight(backpack):0.0}/{GetMaxWeight(backpack):0.0}\n" +
            "Drag loot into your loadout. Press Esc to close.";
        return string.IsNullOrWhiteSpace(status) ? summary : $"{summary}\n{status}";
    }

    private void TakeAll()
    {
        if (openContainer == null || interactor == null)
        {
            return;
        }

        InventoryContainer lootInventory = openContainer.Inventory;
        InventoryContainer backpack = interactor.PrimaryInventory;
        if (lootInventory != null && backpack != null)
        {
            for (int index = lootInventory.Items.Count - 1; index >= 0; index--)
            {
                ItemInstance item = lootInventory.Items[index];
                if (item == null || !item.IsDefined())
                {
                    continue;
                }

                lootInventory.TryTransferItemTo(backpack, index, item.Quantity, out _);
            }
        }

        PrototypeCorpseLoot corpseLoot = openContainer.GetComponent<PrototypeCorpseLoot>();
        if (corpseLoot != null)
        {
            for (int index = corpseLoot.Weapons.Count - 1; index >= 0; index--)
            {
                corpseLoot.TryTakeWeapon(interactor, index);
            }
        }

        MarkDirty();
    }

    private bool IsWithinOpenDistance()
    {
        if (interactor == null || openContainer == null)
        {
            return false;
        }

        Transform targetTransform = openContainer.GetInteractionTransform();
        return targetTransform != null && Vector3.Distance(interactor.transform.position, targetTransform.position) <= maxOpenDistance;
    }

    private static RectTransform CreateColumnRoot(Transform parent, float flexibleWidth)
    {
        RectTransform root = PrototypeUiToolkit.CreateRectTransform("Column", parent);
        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.05f, 0.07f, 0.1f, 0.36f);
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.flexibleWidth = flexibleWidth;
        layout.flexibleHeight = 1f;
        layout.minHeight = 0f;
        return root;
    }

    private void UpdateWindowVisibility()
    {
        EnsureWindowUi();
        PrototypeUiToolkit.SetVisible(windowChrome.Root, IsOpen && !IsPlayerDead());
    }

    private void MarkDirty()
    {
        contentDirty = true;
    }

    private static bool HasActiveDrag()
    {
        PrototypeRaidDragService dragService = PrototypeRaidDragService.CurrentInstance;
        return dragService != null && dragService.CurrentPayload != null;
    }

    private static void CancelActiveDrag()
    {
        PrototypeRaidDragService.CurrentInstance?.EndDrag();
    }

    private void ResolveReferences()
    {
        if (interactor == null)
        {
            interactor = GetComponent<PlayerInteractor>();
        }

        if (fpsInput == null)
        {
            fpsInput = GetComponent<PrototypeFpsInput>();
        }

        if (interactionState == null)
        {
            interactionState = GetComponent<PlayerInteractionState>();
        }

        if (inventoryWindowController == null)
        {
            inventoryWindowController = GetComponent<PlayerInventoryWindowController>();
        }

        if (playerVitals == null)
        {
            playerVitals = GetComponent<PrototypeUnitVitals>();
        }

        if (raidEquipmentController == null)
        {
            raidEquipmentController = GetComponent<PrototypeRaidEquipmentController>();
        }
    }

    private static int GetStackCount(InventoryContainer inventory)
    {
        return inventory != null ? inventory.OccupiedSlots : 0;
    }

    private static int GetMaxSlots(InventoryContainer inventory)
    {
        return inventory != null ? inventory.MaxSlots : 0;
    }

    private static float GetWeight(InventoryContainer inventory)
    {
        return inventory != null ? inventory.CurrentWeight : 0f;
    }

    private static float GetMaxWeight(InventoryContainer inventory)
    {
        return inventory != null ? inventory.MaxWeight : 0f;
    }

    private void HandlePlayerDied(PrototypeUnitVitals vitals)
    {
        Close();
    }

    private bool IsPlayerDead()
    {
        return playerVitals != null && playerVitals.IsDead;
    }

    private void SetUiFocus(bool focused)
    {
        if (interactionState != null)
        {
            interactionState.SetUiFocused(this, focused);
        }

        bool keepCursorFree = focused || (interactionState != null && interactionState.IsUiFocused);
        Cursor.lockState = keepCursorFree ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = keepCursorFree;
    }

    private void OnDestroy()
    {
        if (windowChrome?.Root != null)
        {
            Destroy(windowChrome.Root.gameObject);
        }
    }
}
