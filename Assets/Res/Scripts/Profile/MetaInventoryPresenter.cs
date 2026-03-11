using UnityEngine;

public sealed class MetaInventoryPresenter
{
    private readonly PrototypeMainMenuController host;
    private Vector2 stashScroll;
    private Vector2 backpackScroll;
    private Vector2 lockerScroll;
    private Vector2 protectedScroll;

    public MetaInventoryPresenter(PrototypeMainMenuController host)
    {
        this.host = host;
    }

    public void DrawWarehousePage()
    {
        float panelTop = 140f;
        float panelHeight = Mathf.Max(400f, Screen.height - 220f);
        float panelWidth = Mathf.Max(220f, (Screen.width - 392f) / 4f);
        float stashX = 292f;
        float backpackX = stashX + panelWidth + 16f;
        float lockerX = backpackX + panelWidth + 16f;
        float protectedX = lockerX + panelWidth + 16f;

        DrawStashPanel(new Rect(stashX, panelTop, panelWidth, panelHeight));
        DrawRaidBackpackPanel(new Rect(backpackX, panelTop, panelWidth, panelHeight));
        DrawWeaponLockerPanel(new Rect(lockerX, panelTop, panelWidth, panelHeight));
        DrawProtectedGearPanel(new Rect(protectedX, panelTop, panelWidth, panelHeight));
    }

    private void DrawStashPanel(Rect rect)
    {
        InventoryContainer stashInventory = host.StashInventory;
        host.BeginPanel(rect, "Warehouse Stash", host.StashColor, stashInventory != null
            ? $"Stacks {stashInventory.Items.Count}/{stashInventory.MaxSlots}  Weight {stashInventory.CurrentWeight:0.0}/{stashInventory.MaxWeight:0.0}"
            : "No inventory");

        if (stashInventory == null || stashInventory.IsEmpty)
        {
            GUILayout.Label("Empty.", host.BodyStyle);
        }
        else
        {
            stashScroll = GUILayout.BeginScrollView(stashScroll, GUILayout.Height(rect.height - 130f));
            for (int index = 0; index < stashInventory.Items.Count; index++)
            {
                ItemInstance item = stashInventory.Items[index];
                if (item == null || !item.IsDefined())
                {
                    continue;
                }

                GUILayout.BeginVertical(host.ListStyle);
                GUILayout.Label($"{item.RichDisplayName} x{item.Quantity}", host.BodyStyle);
                GUILayout.Label($"Weight {item.TotalWeight:0.00}", host.BodyStyle);
                GUILayout.BeginHorizontal();
                if (item.Definition is ArmorDefinition)
                {
                    if (GUILayout.Button("Equip", host.ButtonStyle, GUILayout.Width(70f)))
                    {
                        EquipArmorFromInventory(stashInventory, index, "Warehouse");
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Bag", host.ButtonStyle, GUILayout.Width(60f)))
                    {
                        MoveItemBetweenInventories(stashInventory, host.RaidBackpackInventory, index, item.Quantity, "Packed", "Raid backpack has no space for that stack.");
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Sell", host.ButtonStyle, GUILayout.Width(60f)))
                    {
                        SellItemFromInventory(stashInventory, index, item.Quantity, "Sold armor");
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    if (GUILayout.Button("Bag", host.ButtonStyle, GUILayout.Width(56f)))
                    {
                        MoveItemBetweenInventories(stashInventory, host.RaidBackpackInventory, index, item.Quantity, "Packed", "Raid backpack has no space for that stack.");
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Secure", host.ButtonStyle, GUILayout.Width(68f)))
                    {
                        MoveItemBetweenInventories(stashInventory, host.SecureContainerInventory, index, item.Quantity, "Secured", "Secure container has no space for that stack.");
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Special", host.ButtonStyle, GUILayout.Width(68f)))
                    {
                        MoveItemBetweenInventories(stashInventory, host.SpecialEquipmentInventory, index, item.Quantity, "Equipped safely", "Special equipment slots have no space for that stack.");
                        GUIUtility.ExitGUI();
                    }

                    if (host.CashDefinition != null && item.Definition != host.CashDefinition && GUILayout.Button("Sell", host.ButtonStyle, GUILayout.Width(56f)))
                    {
                        SellItemFromInventory(stashInventory, index, item.Quantity, "Sold item");
                        GUIUtility.ExitGUI();
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

        host.EndPanel();
    }

    private void DrawRaidBackpackPanel(Rect rect)
    {
        InventoryContainer raidBackpackInventory = host.RaidBackpackInventory;
        host.BeginPanel(rect, "Raid Backpack", host.BackpackColor, raidBackpackInventory != null
            ? $"Slots {raidBackpackInventory.OccupiedSlots}/{raidBackpackInventory.MaxSlots}  Weight {raidBackpackInventory.CurrentWeight:0.0}/{raidBackpackInventory.MaxWeight:0.0}"
            : "No inventory");

        if (raidBackpackInventory == null || raidBackpackInventory.IsEmpty)
        {
            GUILayout.Label("Empty.", host.BodyStyle);
        }
        else
        {
            backpackScroll = GUILayout.BeginScrollView(backpackScroll, GUILayout.Height(rect.height - 168f));
            for (int index = 0; index < raidBackpackInventory.Items.Count; index++)
            {
                ItemInstance item = raidBackpackInventory.Items[index];
                if (item == null || !item.IsDefined())
                {
                    continue;
                }

                GUILayout.BeginVertical(host.ListStyle);
                GUILayout.Label(item.Quantity > 1 ? $"{item.RichDisplayName} x{item.Quantity}" : item.RichDisplayName, host.BodyStyle);
                GUILayout.Label(GetInventoryEntryDetail(item), host.BodyStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Store", host.ButtonStyle, GUILayout.Width(72f)))
                {
                    if (item.IsWeapon)
                    {
                        StoreWeaponItemFromInventory(raidBackpackInventory, index);
                    }
                    else
                    {
                        MoveItemBetweenInventories(raidBackpackInventory, host.StashInventory, index, item.Quantity, "Stored", "Warehouse has no space for that stack.");
                    }
                    GUIUtility.ExitGUI();
                }

                if (item.Definition is ArmorDefinition && GUILayout.Button("Equip", host.ButtonStyle, GUILayout.Width(72f)))
                {
                    EquipArmorFromInventory(raidBackpackInventory, index, "Raid backpack");
                    GUIUtility.ExitGUI();
                }
                else if (item.IsWeapon && item.WeaponDefinition != null)
                {
                    if (item.WeaponDefinition.IsMeleeWeapon)
                    {
                        if (GUILayout.Button("Equip", host.ButtonStyle, GUILayout.Width(72f)))
                        {
                            EquipWeaponFromInventory(raidBackpackInventory, index, PrototypeMainMenuController.WeaponSlotType.Melee);
                            GUIUtility.ExitGUI();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Primary", host.ButtonStyle, GUILayout.Width(80f)))
                        {
                            EquipWeaponFromInventory(raidBackpackInventory, index, PrototypeMainMenuController.WeaponSlotType.Primary);
                            GUIUtility.ExitGUI();
                        }

                        if (GUILayout.Button("Secondary", host.ButtonStyle, GUILayout.Width(88f)))
                        {
                            EquipWeaponFromInventory(raidBackpackInventory, index, PrototypeMainMenuController.WeaponSlotType.Secondary);
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

        GUILayout.Space(8f);
        if (GUILayout.Button("Store All", host.ButtonStyle, GUILayout.Height(32f)))
        {
            StoreAllRaidBackpack();
            GUIUtility.ExitGUI();
        }

        host.EndPanel();
    }

    private void DrawWeaponLockerPanel(Rect rect)
    {
        host.BeginPanel(rect, "Weapon Locker", host.LockerColor, $"Stored weapons {host.WeaponLocker.Count}");

        if (host.WeaponLocker.Count == 0)
        {
            GUILayout.Label("No safe weapons in storage.", host.BodyStyle);
        }
        else
        {
            lockerScroll = GUILayout.BeginScrollView(lockerScroll, GUILayout.Height(rect.height - 130f));
            for (int index = 0; index < host.WeaponLocker.Count; index++)
            {
                WeaponInstance weapon = host.WeaponLocker[index];
                if (weapon == null || weapon.Definition == null)
                {
                    continue;
                }

                GUILayout.BeginVertical(host.ListStyle);
                GUILayout.Label(weapon.RichDisplayName, host.BodyStyle);
                GUILayout.Label(weapon.Definition.IsMeleeWeapon ? "Melee" : "Firearm", host.BodyStyle);
                if (weapon.Definition.IsMeleeWeapon)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Equip Melee", host.ButtonStyle, GUILayout.Width(108f)))
                    {
                        EquipWeaponFromLocker(index, PrototypeMainMenuController.WeaponSlotType.Melee);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Sell", host.ButtonStyle, GUILayout.Width(64f)))
                    {
                        SellWeaponFromLocker(index);
                        GUIUtility.ExitGUI();
                    }

                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Primary", host.ButtonStyle, GUILayout.Width(90f)))
                    {
                        EquipWeaponFromLocker(index, PrototypeMainMenuController.WeaponSlotType.Primary);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Secondary", host.ButtonStyle, GUILayout.Width(90f)))
                    {
                        EquipWeaponFromLocker(index, PrototypeMainMenuController.WeaponSlotType.Secondary);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Sell", host.ButtonStyle, GUILayout.Width(64f)))
                    {
                        SellWeaponFromLocker(index);
                        GUIUtility.ExitGUI();
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

        host.EndPanel();
    }

    private void DrawProtectedGearPanel(Rect rect)
    {
        string subtitle = $"Armor {host.EquippedArmor.Count}  Secure {host.GetInventoryStackCount(host.SecureContainerInventory)}  Special {host.GetInventoryStackCount(host.SpecialEquipmentInventory)}";
        host.BeginPanel(rect, "Protected Gear", host.ProtectedColor, subtitle);

        protectedScroll = GUILayout.BeginScrollView(protectedScroll, GUILayout.Height(rect.height - 130f));

        DrawWeaponSlotEntry("Primary", host.EquippedPrimaryWeapon, PrototypeMainMenuController.WeaponSlotType.Primary, false);
        DrawWeaponSlotEntry("Secondary", host.EquippedSecondaryWeapon, PrototypeMainMenuController.WeaponSlotType.Secondary, false);
        DrawWeaponSlotEntry("Melee (Safe)", host.EquippedMeleeWeapon, PrototypeMainMenuController.WeaponSlotType.Melee, true);

        GUILayout.Space(8f);
        GUILayout.Label("Equipped Armor", host.BodyStyle);
        if (host.EquippedArmor.Count == 0)
        {
            GUILayout.Label("No armor equipped.", host.BodyStyle);
        }
        else
        {
            for (int index = 0; index < host.EquippedArmor.Count; index++)
            {
                ArmorInstance armorInstance = host.EquippedArmor[index];
                ArmorDefinition armorDefinition = armorInstance != null ? armorInstance.Definition : null;
                if (armorDefinition == null)
                {
                    continue;
                }

                GUILayout.BeginVertical(host.ListStyle);
                GUILayout.Label(armorInstance.RichDisplayName, host.BodyStyle);
                GUILayout.Label($"Durability {armorInstance.CurrentDurability:0}/{armorInstance.MaxDurability:0}", host.BodyStyle);
                GUILayout.Label($"Coverage {string.Join(", ", armorDefinition.CoveredPartIds)}", host.BodyStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Store", host.ButtonStyle, GUILayout.Width(72f)))
                {
                    StoreEquippedArmorToStash(index);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Bag", host.ButtonStyle, GUILayout.Width(64f)))
                {
                    MoveEquippedArmorToBackpack(index);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Sell", host.ButtonStyle, GUILayout.Width(64f)))
                {
                    SellEquippedArmor(index);
                    GUIUtility.ExitGUI();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        DrawProtectedInventorySection("Secure Container", host.SecureContainerInventory);
        DrawProtectedInventorySection("Special Equipment", host.SpecialEquipmentInventory);

        GUILayout.EndScrollView();
        host.EndPanel();
    }

    private void DrawProtectedInventorySection(string title, InventoryContainer inventory)
    {
        GUILayout.Space(8f);
        GUILayout.Label(title, host.BodyStyle);
        if (inventory == null || inventory.IsEmpty)
        {
            GUILayout.Label("Empty.", host.BodyStyle);
            return;
        }

        for (int index = 0; index < inventory.Items.Count; index++)
        {
            ItemInstance item = inventory.Items[index];
            if (item == null || !item.IsDefined())
            {
                continue;
            }

            GUILayout.BeginVertical(host.ListStyle);
            GUILayout.Label($"{item.RichDisplayName} x{item.Quantity}", host.BodyStyle);
            GUILayout.Label($"Weight {item.TotalWeight:0.00}", host.BodyStyle);
            if (GUILayout.Button("Store", host.ButtonStyle, GUILayout.Width(72f)))
            {
                MoveItemBetweenInventories(inventory, host.StashInventory, index, item.Quantity, "Stored", "Warehouse has no space for that stack.");
                GUIUtility.ExitGUI();
            }

            GUILayout.EndVertical();
        }
    }

    private void DrawWeaponSlotEntry(string label, WeaponInstance weaponInstance, PrototypeMainMenuController.WeaponSlotType slotType, bool protectedOnDeath)
    {
        GUILayout.BeginVertical(host.ListStyle);
        GUILayout.Label($"{label}: {(weaponInstance != null ? weaponInstance.RichDisplayName : "Empty")}", host.BodyStyle);
        if (protectedOnDeath)
        {
            GUILayout.Label("This slot survives raid death.", host.BodyStyle);
        }

        if (weaponInstance != null && GUILayout.Button("Store", host.ButtonStyle, GUILayout.Width(120f)))
        {
            StoreEquippedWeapon(slotType);
            GUIUtility.ExitGUI();
        }

        if (weaponInstance != null && GUILayout.Button("Sell", host.ButtonStyle, GUILayout.Width(120f)))
        {
            SellEquippedWeapon(slotType);
            GUIUtility.ExitGUI();
        }

        GUILayout.EndVertical();
    }

    private void MoveItemBetweenInventories(
        InventoryContainer source,
        InventoryContainer destination,
        int itemIndex,
        int quantity,
        string successVerb,
        string failureMessage)
    {
        if (source == null || destination == null || itemIndex < 0 || itemIndex >= source.Items.Count)
        {
            return;
        }

        ItemInstance item = source.Items[itemIndex];
        if (item == null || !item.IsDefined())
        {
            return;
        }

        if (!source.TryTransferItemTo(destination, itemIndex, quantity, out int movedQuantity) || movedQuantity <= 0)
        {
            host.SetFeedback(failureMessage);
            return;
        }

        host.SetFeedback($"{successVerb} {item.DisplayName} x{movedQuantity}.");
        host.AutoSaveIfNeeded();
    }

    private void EquipArmorFromInventory(InventoryContainer source, int itemIndex, string sourceLabel)
    {
        if (source == null || itemIndex < 0 || itemIndex >= source.Items.Count)
        {
            return;
        }

        ItemInstance item = source.Items[itemIndex];
        if (item == null || !(item.Definition is ArmorDefinition armorDefinition))
        {
            return;
        }

        if (host.PlayerLevel < armorDefinition.RequiredLevel)
        {
            host.SetFeedback($"Requires level {armorDefinition.RequiredLevel} to equip {armorDefinition.DisplayNameWithLevel}.");
            return;
        }

        if (!source.TryExtractItem(itemIndex, 1, out ItemInstance extractedItem) || extractedItem == null)
        {
            host.SetFeedback($"Could not equip armor from {sourceLabel.ToLowerInvariant()}.");
            return;
        }

        ArmorInstance armorInstance = extractedItem.ToArmorInstance();
        if (armorInstance == null)
        {
            source.TryAddItemInstance(extractedItem);
            host.SetFeedback($"Could not equip armor from {sourceLabel.ToLowerInvariant()}.");
            return;
        }

        host.EquippedArmor.Add(armorInstance);
        host.SetFeedback($"Equipped {armorInstance.DisplayName}.");
        host.AutoSaveIfNeeded();
    }

    private void StoreEquippedArmorToStash(int armorIndex)
    {
        if (armorIndex < 0 || armorIndex >= host.EquippedArmor.Count)
        {
            return;
        }

        ArmorInstance armorInstance = host.EquippedArmor[armorIndex];
        ArmorDefinition armorDefinition = armorInstance != null ? armorInstance.Definition : null;
        if (armorDefinition == null)
        {
            host.EquippedArmor.RemoveAt(armorIndex);
            host.AutoSaveIfNeeded();
            return;
        }

        if (host.StashInventory == null || !host.StashInventory.TryAddItemInstance(ItemInstance.Create(armorInstance)))
        {
            host.SetFeedback("Warehouse has no space for that armor.");
            return;
        }

        host.EquippedArmor.RemoveAt(armorIndex);
        host.SetFeedback($"Stored {armorDefinition.DisplayNameWithLevel}.");
        host.AutoSaveIfNeeded();
    }

    private void MoveEquippedArmorToBackpack(int armorIndex)
    {
        if (armorIndex < 0 || armorIndex >= host.EquippedArmor.Count)
        {
            return;
        }

        ArmorInstance armorInstance = host.EquippedArmor[armorIndex];
        ArmorDefinition armorDefinition = armorInstance != null ? armorInstance.Definition : null;
        if (armorDefinition == null)
        {
            host.EquippedArmor.RemoveAt(armorIndex);
            host.AutoSaveIfNeeded();
            return;
        }

        if (host.RaidBackpackInventory == null || !host.RaidBackpackInventory.TryAddItemInstance(ItemInstance.Create(armorInstance)))
        {
            host.SetFeedback("Raid backpack has no room for that armor.");
            return;
        }

        host.EquippedArmor.RemoveAt(armorIndex);
        host.SetFeedback($"Moved {armorDefinition.DisplayNameWithLevel} into the raid backpack.");
        host.AutoSaveIfNeeded();
    }

    private void StoreAllRaidBackpack()
    {
        if (host.RaidBackpackInventory == null || host.StashInventory == null)
        {
            return;
        }

        bool movedAnything = false;
        for (int index = host.RaidBackpackInventory.Items.Count - 1; index >= 0; index--)
        {
            ItemInstance item = host.RaidBackpackInventory.Items[index];
            if (item == null || !item.IsDefined())
            {
                continue;
            }

            if (item.IsWeapon)
            {
                if (!host.RaidBackpackInventory.TryExtractItem(index, 1, out ItemInstance extractedItem) || extractedItem == null)
                {
                    continue;
                }

                WeaponInstance weaponInstance = extractedItem.ToWeaponInstance();
                if (weaponInstance == null)
                {
                    host.RaidBackpackInventory.TryAddItemInstance(extractedItem);
                    continue;
                }

                host.WeaponLocker.Add(weaponInstance);
                movedAnything = true;
                continue;
            }

            if (host.RaidBackpackInventory.TryTransferItemTo(host.StashInventory, index, item.Quantity, out _))
            {
                movedAnything = true;
            }
        }

        if (!movedAnything)
        {
            return;
        }

        host.SetFeedback("Stored raid backpack contents.");
        host.AutoSaveIfNeeded();
    }

    private void StoreWeaponItemFromInventory(InventoryContainer source, int itemIndex)
    {
        if (source == null || itemIndex < 0 || itemIndex >= source.Items.Count)
        {
            return;
        }

        if (!source.TryExtractItem(itemIndex, 1, out ItemInstance extractedItem) || extractedItem == null)
        {
            return;
        }

        WeaponInstance weaponInstance = extractedItem.ToWeaponInstance();
        if (weaponInstance == null)
        {
            source.TryAddItemInstance(extractedItem);
            return;
        }

        host.WeaponLocker.Add(weaponInstance);
        host.SetFeedback($"Stored {weaponInstance.DisplayName}.");
        host.AutoSaveIfNeeded();
    }

    private void EquipWeaponFromInventory(InventoryContainer source, int itemIndex, PrototypeMainMenuController.WeaponSlotType slotType)
    {
        if (source == null || itemIndex < 0 || itemIndex >= source.Items.Count)
        {
            return;
        }

        if (!source.TryExtractItem(itemIndex, 1, out ItemInstance extractedItem) || extractedItem == null)
        {
            return;
        }

        WeaponInstance weaponInstance = extractedItem.ToWeaponInstance();
        if (weaponInstance == null || weaponInstance.Definition == null)
        {
            source.TryAddItemInstance(extractedItem);
            return;
        }

        bool expectsMelee = slotType == PrototypeMainMenuController.WeaponSlotType.Melee;
        if (weaponInstance.Definition.IsMeleeWeapon != expectsMelee)
        {
            source.TryAddItemInstance(extractedItem);
            host.SetFeedback(expectsMelee
                ? "Only melee weapons can be equipped in the melee slot."
                : "Melee weapons must be equipped in the melee slot.");
            return;
        }

        if (host.PlayerLevel < weaponInstance.Definition.RequiredLevel)
        {
            source.TryAddItemInstance(extractedItem);
            host.SetFeedback($"Requires level {weaponInstance.Definition.RequiredLevel} to equip {weaponInstance.DisplayName}.");
            return;
        }

        WeaponInstance replacedWeapon = GetEquippedWeapon(slotType);
        if (replacedWeapon != null && !source.TryAddItemInstance(ItemInstance.Create(replacedWeapon)))
        {
            source.TryAddItemInstance(extractedItem);
            host.SetFeedback("Raid backpack has no room for the replaced weapon.");
            return;
        }

        SetEquippedWeapon(slotType, weaponInstance);
        host.SetFeedback($"Equipped {weaponInstance.DisplayName}.");
        host.AutoSaveIfNeeded();
    }

    private void EquipWeaponFromLocker(int lockerIndex, PrototypeMainMenuController.WeaponSlotType slotType)
    {
        if (lockerIndex < 0 || lockerIndex >= host.WeaponLocker.Count)
        {
            return;
        }

        WeaponInstance weaponInstance = host.WeaponLocker[lockerIndex];
        if (weaponInstance == null || weaponInstance.Definition == null)
        {
            host.WeaponLocker.RemoveAt(lockerIndex);
            host.AutoSaveIfNeeded();
            return;
        }

        bool expectsMelee = slotType == PrototypeMainMenuController.WeaponSlotType.Melee;
        if (weaponInstance.Definition.IsMeleeWeapon != expectsMelee)
        {
            host.SetFeedback(expectsMelee
                ? "Only melee weapons can be equipped in the melee slot."
                : "Melee weapons must be equipped in the melee slot.");
            return;
        }

        if (host.PlayerLevel < weaponInstance.Definition.RequiredLevel)
        {
            host.SetFeedback($"Requires level {weaponInstance.Definition.RequiredLevel} to equip {weaponInstance.DisplayName}.");
            return;
        }

        host.WeaponLocker.RemoveAt(lockerIndex);
        WeaponInstance replacedWeapon = GetEquippedWeapon(slotType);
        if (replacedWeapon != null)
        {
            host.WeaponLocker.Add(replacedWeapon);
        }

        SetEquippedWeapon(slotType, weaponInstance);
        host.SetFeedback($"Equipped {weaponInstance.DisplayName}.");
        host.AutoSaveIfNeeded();
    }

    private void StoreEquippedWeapon(PrototypeMainMenuController.WeaponSlotType slotType)
    {
        WeaponInstance weaponInstance = GetEquippedWeapon(slotType);
        if (weaponInstance == null)
        {
            return;
        }

        host.WeaponLocker.Add(weaponInstance);
        SetEquippedWeapon(slotType, null);
        host.SetFeedback($"Stored {weaponInstance.DisplayName}.");
        host.AutoSaveIfNeeded();
    }

    private WeaponInstance GetEquippedWeapon(PrototypeMainMenuController.WeaponSlotType slotType)
    {
        switch (slotType)
        {
            case PrototypeMainMenuController.WeaponSlotType.Primary:
                return host.EquippedPrimaryWeapon;

            case PrototypeMainMenuController.WeaponSlotType.Secondary:
                return host.EquippedSecondaryWeapon;

            default:
                return host.EquippedMeleeWeapon;
        }
    }

    private void SetEquippedWeapon(PrototypeMainMenuController.WeaponSlotType slotType, WeaponInstance weaponInstance)
    {
        switch (slotType)
        {
            case PrototypeMainMenuController.WeaponSlotType.Primary:
                host.EquippedPrimaryWeapon = weaponInstance;
                break;

            case PrototypeMainMenuController.WeaponSlotType.Secondary:
                host.EquippedSecondaryWeapon = weaponInstance;
                break;

            default:
                host.EquippedMeleeWeapon = weaponInstance;
                break;
        }
    }

    private static string GetInventoryEntryDetail(ItemInstance item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        if (item.IsWeapon && item.WeaponDefinition != null)
        {
            return item.WeaponDefinition.IsMeleeWeapon
                ? $"Melee  Weight {item.TotalWeight:0.00}"
                : $"Ammo {item.MagazineAmmo}/{item.WeaponDefinition.MagazineSize}  Weight {item.TotalWeight:0.00}";
        }

        if (item.IsArmor)
        {
            return $"Durability {item.CurrentDurability:0.0}  Weight {item.TotalWeight:0.00}";
        }

        return $"Weight {item.TotalWeight:0.00}";
    }

    private void SellItemFromInventory(InventoryContainer source, int itemIndex, int quantity, string successPrefix)
    {
        PrototypeMerchantCatalog merchantCatalog = host.MerchantCatalog;
        if (source == null || merchantCatalog == null || itemIndex < 0 || itemIndex >= source.Items.Count)
        {
            return;
        }

        ItemInstance item = source.Items[itemIndex];
        if (item == null || !item.IsDefined() || item.IsWeapon || item.Definition == null || item.Definition == host.CashDefinition)
        {
            return;
        }

        int sellPrice = merchantCatalog.GetSellPrice(item.Definition, quantity);
        if (sellPrice <= 0)
        {
            return;
        }

        if (!source.TryExtractItem(itemIndex, quantity, out ItemInstance extractedItem) || extractedItem == null || !extractedItem.IsDefined())
        {
            return;
        }

        if (!host.TryAddFunds(sellPrice))
        {
            source.TryAddItemInstance(extractedItem);
            host.SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        host.SetFeedback($"{successPrefix}: {extractedItem.DisplayName} x{extractedItem.Quantity} for {sellPrice}.");
        host.AutoSaveIfNeeded();
    }

    private void SellWeaponFromLocker(int lockerIndex)
    {
        PrototypeMerchantCatalog merchantCatalog = host.MerchantCatalog;
        if (merchantCatalog == null || lockerIndex < 0 || lockerIndex >= host.WeaponLocker.Count)
        {
            return;
        }

        WeaponInstance weaponInstance = host.WeaponLocker[lockerIndex];
        PrototypeWeaponDefinition weaponDefinition = weaponInstance != null ? weaponInstance.Definition : null;
        int sellPrice = merchantCatalog.GetSellPrice(weaponDefinition);
        if (weaponDefinition == null || sellPrice <= 0 || !host.CanReceiveFunds(sellPrice))
        {
            host.SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        host.WeaponLocker.RemoveAt(lockerIndex);
        if (!host.TryAddFunds(sellPrice))
        {
            host.WeaponLocker.Insert(Mathf.Clamp(lockerIndex, 0, host.WeaponLocker.Count), weaponInstance);
            host.SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        host.SetFeedback($"Sold {weaponDefinition.DisplayNameWithLevel} for {sellPrice}.");
        host.AutoSaveIfNeeded();
    }

    private void SellEquippedWeapon(PrototypeMainMenuController.WeaponSlotType slotType)
    {
        PrototypeMerchantCatalog merchantCatalog = host.MerchantCatalog;
        if (merchantCatalog == null)
        {
            return;
        }

        WeaponInstance weaponInstance = GetEquippedWeapon(slotType);
        PrototypeWeaponDefinition weaponDefinition = weaponInstance != null ? weaponInstance.Definition : null;
        int sellPrice = merchantCatalog.GetSellPrice(weaponDefinition);
        if (weaponDefinition == null || sellPrice <= 0 || !host.CanReceiveFunds(sellPrice))
        {
            host.SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        SetEquippedWeapon(slotType, null);
        if (!host.TryAddFunds(sellPrice))
        {
            SetEquippedWeapon(slotType, weaponInstance);
            host.SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        host.SetFeedback($"Sold {weaponDefinition.DisplayNameWithLevel} for {sellPrice}.");
        host.AutoSaveIfNeeded();
    }

    private void SellEquippedArmor(int armorIndex)
    {
        PrototypeMerchantCatalog merchantCatalog = host.MerchantCatalog;
        if (merchantCatalog == null || armorIndex < 0 || armorIndex >= host.EquippedArmor.Count)
        {
            return;
        }

        ArmorInstance armorInstance = host.EquippedArmor[armorIndex];
        ArmorDefinition armorDefinition = armorInstance != null ? armorInstance.Definition : null;
        int sellPrice = merchantCatalog.GetSellPrice(armorDefinition, 1);
        if (armorDefinition == null || sellPrice <= 0 || !host.CanReceiveFunds(sellPrice))
        {
            host.SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        host.EquippedArmor.RemoveAt(armorIndex);
        if (!host.TryAddFunds(sellPrice))
        {
            host.EquippedArmor.Insert(Mathf.Clamp(armorIndex, 0, host.EquippedArmor.Count), armorInstance);
            host.SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        host.SetFeedback($"Sold {armorDefinition.DisplayNameWithLevel} for {sellPrice}.");
        host.AutoSaveIfNeeded();
    }
}
