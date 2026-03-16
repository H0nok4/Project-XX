using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeFpsInput))]
public class PlayerInventoryWindowController : MonoBehaviour
{
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
    private PrototypeUiToolkit.WindowChrome windowChrome;
    private Text summaryText;
    private ScrollRect backpackScrollRect;
    private RectTransform backpackContentRoot;
    private ScrollRect gearScrollRect;
    private RectTransform gearContentRoot;

    public bool IsOpen => isOpen;

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

        isOpen = false;
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
            "PlayerInventoryWindow",
            "Raid Inventory",
            "Drag items between your backpack, equipped slots, special equipment, and the secure container.",
            new Vector2(1280f, 720f));

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
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = true;
        LayoutElement contentRowLayout = contentRow.gameObject.AddComponent<LayoutElement>();
        contentRowLayout.flexibleWidth = 1f;
        contentRowLayout.flexibleHeight = 1f;
        contentRowLayout.minHeight = 0f;

        RectTransform backpackColumn = CreateColumnRoot(contentRow, 0.56f);
        backpackScrollRect = PrototypeUiToolkit.CreateScrollView(backpackColumn, out _, out RectTransform backpackContent, true);
        backpackContentRoot = backpackContent;
        PrototypeUiToolkit.SetStretch(backpackScrollRect.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        RectTransform gearColumn = CreateColumnRoot(contentRow, 0.44f);
        gearScrollRect = PrototypeUiToolkit.CreateScrollView(gearColumn, out _, out RectTransform gearContent, true);
        gearContentRoot = gearContent;
        PrototypeUiToolkit.SetStretch(gearScrollRect.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        Button closeButton = PrototypeUiToolkit.CreateButton(
            windowChrome.FooterRoot,
            manager.RuntimeFont,
            "Close",
            Close,
            new Color(0.2f, 0.27f, 0.36f, 0.98f),
            new Color(0.29f, 0.38f, 0.49f, 1f),
            new Color(0.16f, 0.22f, 0.3f, 1f),
            38f);
        LayoutElement closeLayout = closeButton.GetComponent<LayoutElement>();
        closeLayout.preferredWidth = 150f;
        closeLayout.flexibleWidth = 0f;

        PrototypeUiToolkit.SetVisible(windowChrome.Root, false);
    }

    private void RefreshWindowIfNeeded()
    {
        EnsureWindowUi();
        UpdateWindowVisibility();
        if (!IsOpen || interactor == null || raidEquipmentController == null)
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

        if (summaryText != null)
        {
            summaryText.text = BuildSummaryText();
        }

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
