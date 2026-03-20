using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeFpsInput))]
public class PlayerInventoryWindowController : WindowBase
{
    private const string WindowPrefabResourcePath = "UI/Loot/PlayerInventoryWindow";
    private const string SectionPrefabResourcePath = "UI/Loot/RaidInventorySection";
    private const string SlotCardPrefabResourcePath = "UI/Loot/RaidInventorySlotCard";
    private const string ItemCardPrefabResourcePath = "UI/Loot/RaidInventoryItemCard";

    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PrototypeFpsInput fpsInput;
    [SerializeField] private PlayerInteractionState interactionState;
    [SerializeField] private LootContainerWindowController lootWindowController;
    [SerializeField] private PrototypeUnitVitals playerVitals;
    [SerializeField] private PrototypeRaidEquipmentController raidEquipmentController;

    private bool isOpen;
    private bool contentDirty = true;
    private bool resetBackpackScroll = true;
    private bool resetGearScroll = true;
    private int lastContentHash;
    private TMP_Text summaryText;
    private ScrollRect backpackScrollRect;
    private RectTransform backpackContentRoot;
    private ScrollRect gearScrollRect;
    private RectTransform gearContentRoot;
    private PlayerInventoryWindowTemplate windowView;
    private RaidInventorySectionTemplate sectionPrefab;
    private RaidInventorySlotCardTemplate slotCardPrefab;
    private RaidInventoryItemCardTemplate itemCardPrefab;
    private PrototypeUiToolkit.WindowChrome windowChrome => Chrome;

    public bool IsOpen => isOpen;
    protected override bool VisibleOnAwake => false;
    protected override string WindowName => "PlayerInventoryWindow";
    protected override string WindowTitle => "Raid Inventory";
    protected override string WindowSubtitle => "Drag items between your backpack, equipped slots, special equipment, and the secure container.";
    protected override Vector2 WindowSize => new Vector2(1280f, 720f);

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

        if (fpsInput != null && fpsInput.InventoryTogglePressedThisFrame)
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
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

        RefreshWindowIfNeeded();
    }

    public void Open()
    {
        ResolveReferences();
        if (interactor == null || interactor.PrimaryInventory == null || raidEquipmentController == null || IsPlayerDead())
        {
            return;
        }

        if (lootWindowController != null && lootWindowController.IsOpen)
        {
            lootWindowController.Close();
        }

        isOpen = true;
        resetBackpackScroll = true;
        resetGearScroll = true;
        MarkDirty();
        SetUiFocus(true);
        UpdateWindowVisibility();
        RefreshWindowIfNeeded();
    }

    public void Close()
    {
        if (!isOpen && (interactionState == null || !interactionState.IsUiFocused))
        {
            return;
        }

        CancelActiveDrag();
        isOpen = false;
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
            windowView = chrome.Root.GetComponent<PlayerInventoryWindowTemplate>();
        }

        if (windowView == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing {nameof(PlayerInventoryWindowTemplate)} on instantiated inventory window.", this);
            return;
        }

        summaryText = windowView.SummaryText;
        backpackScrollRect = windowView.BackpackScrollRect;
        backpackContentRoot = windowView.BackpackContentRoot;
        gearScrollRect = windowView.GearScrollRect;
        gearContentRoot = windowView.GearContentRoot;

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
        backpackScrollRect = null;
        backpackContentRoot = null;
        gearScrollRect = null;
        gearContentRoot = null;
    }

    private void RefreshWindowIfNeeded()
    {
        EnsureWindow();
        UpdateWindowVisibility();
        if (!IsOpen || interactor == null || raidEquipmentController == null)
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

        int contentHash = raidEquipmentController.BuildStateHash();
        if (!contentDirty && contentHash == lastContentHash)
        {
            return;
        }

        float backpackScroll = backpackScrollRect != null ? backpackScrollRect.verticalNormalizedPosition : 1f;
        float gearScroll = gearScrollRect != null ? gearScrollRect.verticalNormalizedPosition : 1f;
        lastContentHash = contentHash;
        contentDirty = false;

        PrototypeUiToolkit.ClearChildren(backpackContentRoot);
        PrototypeUiToolkit.ClearChildren(gearContentRoot);

        BuildInventorySection(
            backpackContentRoot,
            "Backpack",
            interactor.PrimaryInventory,
            "Your raid backpack is empty.",
            PrototypeRaidDropTarget.ForInventory(interactor.PrimaryInventory),
            true);

        BuildGearColumn();

        LayoutRebuilder.ForceRebuildLayoutImmediate(windowChrome.Panel);
        if (backpackScrollRect != null)
        {
            backpackScrollRect.verticalNormalizedPosition = resetBackpackScroll ? 1f : Mathf.Clamp01(backpackScroll);
        }

        if (gearScrollRect != null)
        {
            gearScrollRect.verticalNormalizedPosition = resetGearScroll ? 1f : Mathf.Clamp01(gearScroll);
        }

        resetBackpackScroll = false;
        resetGearScroll = false;
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
        InventoryContainer backpack = interactor != null ? interactor.PrimaryInventory : null;
        InventoryContainer secure = interactor != null ? interactor.SecureInventory : null;
        InventoryContainer special = interactor != null ? interactor.SpecialInventory : null;
        string status = raidEquipmentController != null ? raidEquipmentController.StatusMessage : string.Empty;
        string summary =
            $"Backpack {GetStackCount(backpack)}/{GetMaxSlots(backpack)}  Weight {GetWeight(backpack):0.0}/{GetMaxWeight(backpack):0.0}\n" +
            $"Secure {GetStackCount(secure)}/{GetMaxSlots(secure)}  Special {GetStackCount(special)}/{GetMaxSlots(special)}\n" +
            "Drag items between sections. Press Tab or Esc to close.";
        return string.IsNullOrWhiteSpace(status) ? summary : $"{summary}\n{status}";
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

        if (lootWindowController == null)
        {
            lootWindowController = GetComponent<LootContainerWindowController>();
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
            Debug.LogWarning($"[{GetType().Name}] Missing inventory window prefab at Resources/{WindowPrefabResourcePath}.", this);
            return false;
        }

        GameObject instanceObject = Instantiate(prefabAsset, parent, false);
        instanceObject.name = prefabAsset.name;

        windowView = instanceObject.GetComponent<PlayerInventoryWindowTemplate>();
        if (windowView == null || windowView.Root == null)
        {
            Destroy(instanceObject);
            windowView = null;
            Debug.LogWarning($"[{GetType().Name}] Inventory window prefab is missing {nameof(PlayerInventoryWindowTemplate)}.", this);
            return false;
        }

        chrome = windowView.CreateWindowChrome();
        if (chrome == null
            || chrome.Root == null
            || windowView.SummaryText == null
            || windowView.BackpackScrollRect == null
            || windowView.BackpackContentRoot == null
            || windowView.GearScrollRect == null
            || windowView.GearContentRoot == null
            || windowView.CloseButton == null)
        {
            Destroy(instanceObject);
            windowView = null;
            chrome = null;
            Debug.LogWarning($"[{GetType().Name}] Inventory window prefab references are incomplete.", this);
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
