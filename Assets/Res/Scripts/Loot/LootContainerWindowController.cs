using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeFpsInput))]
public class LootContainerWindowController : WindowBase
{
    private const string WindowPrefabResourcePath = "UI/Loot/LootContainerWindow";
    private const string SectionPrefabResourcePath = "UI/Loot/RaidInventorySection";
    private const string SlotCardPrefabResourcePath = "UI/Loot/RaidInventorySlotCard";
    private const string ItemCardPrefabResourcePath = "UI/Loot/RaidInventoryItemCard";

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
    private TMP_Text summaryText;
    private ScrollRect lootScrollRect;
    private RectTransform lootContentRoot;
    private ScrollRect backpackScrollRect;
    private RectTransform backpackContentRoot;
    private ScrollRect gearScrollRect;
    private RectTransform gearContentRoot;
    private LootContainerWindowTemplate windowView;
    private RaidInventorySectionTemplate sectionPrefab;
    private RaidInventorySlotCardTemplate slotCardPrefab;
    private RaidInventoryItemCardTemplate itemCardPrefab;
    private PrototypeUiToolkit.WindowChrome windowChrome => Chrome;

    public LootContainer OpenContainer => openContainer;
    public bool IsOpen => openContainer != null;
    protected override bool VisibleOnAwake => false;
    protected override string WindowName => "LootContainerWindow";
    protected override string WindowTitle => "Loot Container";
    protected override string WindowSubtitle => "Drag loot into your backpack, equip slots, special equipment, or the secure container.";
    protected override Vector2 WindowSize => new Vector2(1540f, 760f);

    protected override void Awake()
    {
        ResolveReferences();
        base.Awake();
    }

    protected override PrototypeUiToolkit.WindowChrome CreateWindowChrome()
    {
        RectTransform parent = UiManager.GetLayerRoot(WindowLayer);
        if (TryInstantiateWindowPrefab(parent, out PrototypeUiToolkit.WindowChrome chrome))
        {
            PrototypeUiToolkit.SetVisible(chrome.Root, false);
            return chrome;
        }

        windowView = null;
        return null;
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

    protected override void BuildWindow(PrototypeUiToolkit.WindowChrome chrome)
    {
        if (chrome == null || chrome.Root == null)
        {
            return;
        }

        if (windowView == null || windowView.Root != chrome.Root)
        {
            windowView = chrome.Root.GetComponent<LootContainerWindowTemplate>();
        }

        if (windowView == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing {nameof(LootContainerWindowTemplate)} on instantiated loot window.", this);
            return;
        }

        summaryText = windowView.SummaryText;
        lootScrollRect = windowView.LootScrollRect;
        lootContentRoot = windowView.LootContentRoot;
        backpackScrollRect = windowView.BackpackScrollRect;
        backpackContentRoot = windowView.BackpackContentRoot;
        gearScrollRect = windowView.GearScrollRect;
        gearContentRoot = windowView.GearContentRoot;

        if (windowView.TakeAllButton != null)
        {
            windowView.TakeAllButton.onClick.RemoveAllListeners();
            windowView.TakeAllButton.onClick.AddListener(TakeAll);
        }

        if (windowView.CloseButton != null)
        {
            windowView.CloseButton.onClick.RemoveAllListeners();
            windowView.CloseButton.onClick.AddListener(Close);
        }
    }

    protected override void OnWindowRootDestroyed()
    {
        windowView = null;
        summaryText = null;
        lootScrollRect = null;
        lootContentRoot = null;
        backpackScrollRect = null;
        backpackContentRoot = null;
        gearScrollRect = null;
        gearContentRoot = null;
    }

    private void RefreshWindowIfNeeded()
    {
        EnsureWindow();
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
        RaidInventorySectionTemplate section = CreateSectionInstance(lootContentRoot, "CorpseWeapons");
        if (section == null)
        {
            return;
        }

        SetText(section.TitleText, "Corpse Weapons");
        SetText(section.SummaryText, $"Weapons {corpseLoot.Weapons.Count}");
        SetActive(section.EmptyLabelText, false);
        SetActive(section.ContentRoot, true);

        for (int index = 0; index < corpseLoot.Weapons.Count; index++)
        {
            PrototypeCorpseLoot.WeaponEntry entry = corpseLoot.GetWeaponEntry(index);
            ItemInstance item = entry != null ? entry.CreateInstance() : null;
            if (item == null)
            {
                continue;
            }

            CreateItemCard(section.ContentRoot, item, PrototypeRaidItemLocation.FromCorpseWeapon(corpseLoot, index), false);
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
        RaidInventorySectionTemplate section = CreateSectionInstance(parent, title);
        if (section == null)
        {
            return;
        }

        AddDropZone(section.Root.gameObject, dropTarget, section.BackgroundImage, new Color(0.11f, 0.15f, 0.2f, 0.98f), new Color(0.17f, 0.24f, 0.33f, 1f));
        SetText(section.TitleText, title);
        SetText(
            section.SummaryText,
            inventory == null
                ? "Unavailable"
                : $"Stacks {inventory.OccupiedSlots}/{inventory.MaxSlots}  Weight {inventory.CurrentWeight:0.0}/{inventory.MaxWeight:0.0}");

        bool hasItems = inventory != null && !inventory.IsEmpty;
        SetActive(section.ContentRoot, hasItems);
        SetActive(section.EmptyLabelText, !hasItems);
        if (!hasItems)
        {
            SetText(section.EmptyLabelText, emptyLabel);
            return;
        }

        for (int index = 0; index < inventory.Items.Count; index++)
        {
            ItemInstance item = inventory.Items[index];
            if (item == null || !item.IsDefined())
            {
                continue;
            }

            CreateItemCard(section.ContentRoot, item, PrototypeRaidItemLocation.FromInventory(inventory, item), allowDropButtons);
        }
    }

    private void BuildSlotCard(RectTransform parent, PrototypeRaidGearSlotType gearSlot, string emptyLabel)
    {
        RaidInventorySlotCardTemplate card = CreateSlotCardInstance(parent, gearSlot + "Slot");
        if (card == null)
        {
            return;
        }

        AddDropZone(
            card.Root.gameObject,
            PrototypeRaidInventoryRules.IsWeaponSlot(gearSlot)
                ? PrototypeRaidDropTarget.ForWeaponSlot(gearSlot)
                : PrototypeRaidInventoryRules.IsArmorSlot(gearSlot)
                    ? PrototypeRaidDropTarget.ForArmorSlot(gearSlot)
                    : PrototypeRaidDropTarget.ForSecureContainerGear(),
            card.BackgroundImage,
            new Color(0.15f, 0.2f, 0.27f, 0.96f),
            new Color(0.22f, 0.3f, 0.4f, 1f));
        SetText(card.TitleText, PrototypeRaidInventoryRules.GetSlotDisplayName(gearSlot));

        ItemInstance equippedItem = raidEquipmentController != null ? raidEquipmentController.GetSlotItem(gearSlot) : null;
        if (equippedItem == null)
        {
            SetActive(card.EmptyLabelText, true);
            SetActive(card.ItemNameText, false);
            SetActive(card.DetailText, false);
            SetActive(card.CapacityText, false);
            SetText(card.EmptyLabelText, emptyLabel);
            return;
        }

        SetActive(card.EmptyLabelText, false);
        SetActive(card.ItemNameText, true);
        SetActive(card.DetailText, true);
        SetText(card.ItemNameText, equippedItem.RichDisplayName);
        SetText(card.DetailText, PrototypeMainMenuController.BuildItemInstanceDetail(equippedItem));

        PrototypeRaidItemLocation source = PrototypeRaidInventoryRules.IsWeaponSlot(gearSlot)
            ? PrototypeRaidItemLocation.FromWeaponSlot(gearSlot)
            : PrototypeRaidInventoryRules.IsArmorSlot(gearSlot)
                ? PrototypeRaidItemLocation.FromArmorSlot(gearSlot)
                : PrototypeRaidItemLocation.FromSecureContainerGear();
        AddDragHandle(card.Root.gameObject, source, equippedItem);

        if (gearSlot == PrototypeRaidGearSlotType.SecureContainer
            && PrototypeRaidInventoryRules.TryGetSecureContainerSpec(equippedItem.DefinitionBase, out PrototypeRaidSecureContainerSpec secureSpec))
        {
            SetActive(card.CapacityText, true);
            SetText(card.CapacityText, $"Slots {secureSpec.SlotCount}  Capacity {secureSpec.MaxWeight:0.0}");
        }
        else
        {
            SetActive(card.CapacityText, false);
        }
    }

    private void CreateItemCard(RectTransform parent, ItemInstance item, PrototypeRaidItemLocation source, bool allowDropButton)
    {
        RaidInventoryItemCardTemplate card = CreateItemCardInstance(parent, "ItemCard");
        if (card == null)
        {
            return;
        }

        SetText(card.TitleText, item.Quantity > 1 ? $"{item.RichDisplayName} x{item.Quantity}" : item.RichDisplayName);
        SetText(card.DetailText, PrototypeMainMenuController.BuildItemInstanceDetail(item));
        AddDragHandle(card.Root.gameObject, source, item);

        bool showDropButton = allowDropButton
            && source != null
            && source.Kind == PrototypeRaidItemLocationKind.InventoryItem
            && source.Inventory == interactor?.PrimaryInventory;
        SetActive(card.ActionRow, showDropButton);
        if (!showDropButton || card.ActionButton == null)
        {
            return;
        }

        if (card.ActionButtonLabel != null)
        {
            card.ActionButtonLabel.text = "Drop";
        }

        card.ActionButton.onClick.RemoveAllListeners();
        card.ActionButton.onClick.AddListener(() =>
        {
            if (raidEquipmentController != null && raidEquipmentController.TryDropBackpackItem(item.InstanceId, out _))
            {
                MarkDirty();
            }
        });
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

    private void UpdateWindowVisibility()
    {
        EnsureWindow();
        SetWindowVisible(IsOpen && !IsPlayerDead());
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

    private bool TryInstantiateWindowPrefab(RectTransform parent, out PrototypeUiToolkit.WindowChrome chrome)
    {
        chrome = null;
        GameObject prefabAsset = Resources.Load<GameObject>(WindowPrefabResourcePath);
        if (prefabAsset == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing loot window prefab at Resources/{WindowPrefabResourcePath}.", this);
            return false;
        }

        GameObject instanceObject = Instantiate(prefabAsset, parent, false);
        instanceObject.name = prefabAsset.name;

        windowView = instanceObject.GetComponent<LootContainerWindowTemplate>();
        if (windowView == null || windowView.Root == null)
        {
            Destroy(instanceObject);
            windowView = null;
            Debug.LogWarning($"[{GetType().Name}] Loot window prefab is missing {nameof(LootContainerWindowTemplate)}.", this);
            return false;
        }

        chrome = windowView.CreateWindowChrome();
        if (chrome == null
            || chrome.Root == null
            || windowView.SummaryText == null
            || windowView.LootScrollRect == null
            || windowView.LootContentRoot == null
            || windowView.BackpackScrollRect == null
            || windowView.BackpackContentRoot == null
            || windowView.GearScrollRect == null
            || windowView.GearContentRoot == null
            || windowView.TakeAllButton == null
            || windowView.CloseButton == null)
        {
            Destroy(instanceObject);
            windowView = null;
            chrome = null;
            Debug.LogWarning($"[{GetType().Name}] Loot window prefab references are incomplete.", this);
            return false;
        }

        PrototypeUiToolkit.ApplyFontRecursively(chrome.Root, RuntimeFont);
        return true;
    }

    private bool EnsureDynamicPrefabsLoaded()
    {
        if (sectionPrefab == null)
        {
            GameObject prefabAsset = Resources.Load<GameObject>(SectionPrefabResourcePath);
            sectionPrefab = prefabAsset != null ? prefabAsset.GetComponent<RaidInventorySectionTemplate>() : null;
        }

        if (slotCardPrefab == null)
        {
            GameObject prefabAsset = Resources.Load<GameObject>(SlotCardPrefabResourcePath);
            slotCardPrefab = prefabAsset != null ? prefabAsset.GetComponent<RaidInventorySlotCardTemplate>() : null;
        }

        if (itemCardPrefab == null)
        {
            GameObject prefabAsset = Resources.Load<GameObject>(ItemCardPrefabResourcePath);
            itemCardPrefab = prefabAsset != null ? prefabAsset.GetComponent<RaidInventoryItemCardTemplate>() : null;
        }

        bool hasAllPrefabs = sectionPrefab != null && slotCardPrefab != null && itemCardPrefab != null;
        if (!hasAllPrefabs)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing one or more loot UI prefabs under Resources/UI/Loot.", this);
        }

        return hasAllPrefabs;
    }

    private RaidInventorySectionTemplate CreateSectionInstance(Transform parent, string objectName)
    {
        if (!EnsureDynamicPrefabsLoaded())
        {
            return null;
        }

        RaidInventorySectionTemplate section = Instantiate(sectionPrefab, parent, false);
        section.gameObject.name = objectName.Replace(" ", string.Empty) + "Section";
        PrototypeUiToolkit.ApplyFontRecursively(section.Root, RuntimeFont);
        PrototypeUiToolkit.ClearChildren(section.ContentRoot);
        return section;
    }

    private RaidInventorySlotCardTemplate CreateSlotCardInstance(Transform parent, string objectName)
    {
        if (!EnsureDynamicPrefabsLoaded())
        {
            return null;
        }

        RaidInventorySlotCardTemplate card = Instantiate(slotCardPrefab, parent, false);
        card.gameObject.name = objectName;
        PrototypeUiToolkit.ApplyFontRecursively(card.Root, RuntimeFont);
        return card;
    }

    private RaidInventoryItemCardTemplate CreateItemCardInstance(Transform parent, string objectName)
    {
        if (!EnsureDynamicPrefabsLoaded())
        {
            return null;
        }

        RaidInventoryItemCardTemplate card = Instantiate(itemCardPrefab, parent, false);
        card.gameObject.name = objectName;
        PrototypeUiToolkit.ApplyFontRecursively(card.Root, RuntimeFont);
        return card;
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null)
        {
            label.text = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }
    }

    private static void SetActive(Component component, bool active)
    {
        if (component != null)
        {
            component.gameObject.SetActive(active);
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

}
