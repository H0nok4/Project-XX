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
        host.BeginPanel(rect, "仓库储藏", host.StashColor, stashInventory != null
            ? $"堆叠 {stashInventory.Items.Count}/{stashInventory.MaxSlots}  重量 {stashInventory.CurrentWeight:0.0}/{stashInventory.MaxWeight:0.0}"
            : "未配置容器");

        if (stashInventory == null || stashInventory.IsEmpty)
        {
            GUILayout.Label("空。", host.BodyStyle);
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
                GUILayout.Label(GetInventoryEntryDetail(item), host.BodyStyle);
                GUILayout.BeginHorizontal();
                if (item.Definition is ArmorDefinition)
                {
                    if (GUILayout.Button("装备", host.ButtonStyle, GUILayout.Width(70f)))
                    {
                        EquipArmorFromInventory(stashInventory, index, "仓库");
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("装包", host.ButtonStyle, GUILayout.Width(60f)))
                    {
                        MoveItemBetweenInventories(stashInventory, host.RaidBackpackInventory, index, item.Quantity, "已装入战局背包", "战局背包空间不足，无法放入该堆叠。");
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("出售", host.ButtonStyle, GUILayout.Width(60f)))
                    {
                        SellItemFromInventory(stashInventory, index, item.Quantity, "已出售护甲");
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    if (GUILayout.Button("装包", host.ButtonStyle, GUILayout.Width(56f)))
                    {
                        MoveItemBetweenInventories(stashInventory, host.RaidBackpackInventory, index, item.Quantity, "已装入战局背包", "战局背包空间不足，无法放入该堆叠。");
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("放安全箱", host.ButtonStyle, GUILayout.Width(68f)))
                    {
                        MoveItemBetweenInventories(stashInventory, host.SecureContainerInventory, index, item.Quantity, "已放入安全箱", "安全箱空间不足，无法放入该堆叠。");
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("放特殊栏", host.ButtonStyle, GUILayout.Width(68f)))
                    {
                        MoveItemBetweenInventories(stashInventory, host.SpecialEquipmentInventory, index, item.Quantity, "已放入特殊装备栏", "特殊装备栏空间不足，无法放入该堆叠。");
                        GUIUtility.ExitGUI();
                    }

                    if (host.CashDefinition != null && item.Definition != host.CashDefinition && GUILayout.Button("出售", host.ButtonStyle, GUILayout.Width(56f)))
                    {
                        SellItemFromInventory(stashInventory, index, item.Quantity, "已出售物品");
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
        host.BeginPanel(rect, "战局背包", host.BackpackColor, raidBackpackInventory != null
            ? $"格位 {raidBackpackInventory.OccupiedSlots}/{raidBackpackInventory.MaxSlots}  重量 {raidBackpackInventory.CurrentWeight:0.0}/{raidBackpackInventory.MaxWeight:0.0}"
            : "未配置容器");

        if (raidBackpackInventory == null || raidBackpackInventory.IsEmpty)
        {
            GUILayout.Label("空。", host.BodyStyle);
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
                if (GUILayout.Button("入库", host.ButtonStyle, GUILayout.Width(72f)))
                {
                    if (item.IsWeapon)
                    {
                        StoreWeaponItemFromInventory(raidBackpackInventory, index);
                    }
                    else
                    {
                        MoveItemBetweenInventories(raidBackpackInventory, host.StashInventory, index, item.Quantity, "已存入仓库", "仓库空间不足，无法接收该堆叠。");
                    }

                    GUIUtility.ExitGUI();
                }

                if (item.Definition is ArmorDefinition && GUILayout.Button("装备", host.ButtonStyle, GUILayout.Width(72f)))
                {
                    EquipArmorFromInventory(raidBackpackInventory, index, "战局背包");
                    GUIUtility.ExitGUI();
                }
                else if (item.IsWeapon && item.WeaponDefinition != null)
                {
                    if (item.WeaponDefinition.IsMeleeWeapon)
                    {
                        if (GUILayout.Button("装备", host.ButtonStyle, GUILayout.Width(72f)))
                        {
                            EquipWeaponFromInventory(raidBackpackInventory, index, PrototypeMainMenuController.WeaponSlotType.Melee);
                            GUIUtility.ExitGUI();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("主武器", host.ButtonStyle, GUILayout.Width(80f)))
                        {
                            EquipWeaponFromInventory(raidBackpackInventory, index, PrototypeMainMenuController.WeaponSlotType.Primary);
                            GUIUtility.ExitGUI();
                        }

                        if (GUILayout.Button("副武器", host.ButtonStyle, GUILayout.Width(88f)))
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
        if (GUILayout.Button("全部入库", host.ButtonStyle, GUILayout.Height(32f)))
        {
            StoreAllRaidBackpack();
            GUIUtility.ExitGUI();
        }

        host.EndPanel();
    }

    private void DrawWeaponLockerPanel(Rect rect)
    {
        host.BeginPanel(rect, "武器柜", host.LockerColor, $"已存武器 {host.WeaponLocker.Count}");

        if (host.WeaponLocker.Count == 0)
        {
            GUILayout.Label("当前没有存放的安全武器。", host.BodyStyle);
        }
        else
        {
            lockerScroll = GUILayout.BeginScrollView(lockerScroll, GUILayout.Height(rect.height - 130f));
            for (int index = 0; index < host.WeaponLocker.Count; index++)
            {
                ItemInstance weapon = host.WeaponLocker[index];
                if (weapon == null || weapon.WeaponDefinition == null)
                {
                    continue;
                }

                GUILayout.BeginVertical(host.ListStyle);
                GUILayout.Label(weapon.RichDisplayName, host.BodyStyle);
                GUILayout.Label(PrototypeMainMenuController.BuildItemInstanceDetail(weapon), host.BodyStyle);
                if (weapon.WeaponDefinition.IsMeleeWeapon)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("装备近战", host.ButtonStyle, GUILayout.Width(108f)))
                    {
                        EquipWeaponFromLocker(index, PrototypeMainMenuController.WeaponSlotType.Melee);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("出售", host.ButtonStyle, GUILayout.Width(64f)))
                    {
                        SellWeaponFromLocker(index);
                        GUIUtility.ExitGUI();
                    }

                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("主武器", host.ButtonStyle, GUILayout.Width(90f)))
                    {
                        EquipWeaponFromLocker(index, PrototypeMainMenuController.WeaponSlotType.Primary);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("副武器", host.ButtonStyle, GUILayout.Width(90f)))
                    {
                        EquipWeaponFromLocker(index, PrototypeMainMenuController.WeaponSlotType.Secondary);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("出售", host.ButtonStyle, GUILayout.Width(64f)))
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
        string subtitle = $"护甲 {host.EquippedArmor.Count}  安全箱 {host.GetInventoryStackCount(host.SecureContainerInventory)}  特殊装备 {host.GetInventoryStackCount(host.SpecialEquipmentInventory)}";
        host.BeginPanel(rect, "保护装备", host.ProtectedColor, subtitle);

        protectedScroll = GUILayout.BeginScrollView(protectedScroll, GUILayout.Height(rect.height - 130f));

        DrawWeaponSlotEntry("主武器", host.EquippedPrimaryWeapon, PrototypeMainMenuController.WeaponSlotType.Primary, false);
        DrawWeaponSlotEntry("副武器", host.EquippedSecondaryWeapon, PrototypeMainMenuController.WeaponSlotType.Secondary, false);
        DrawWeaponSlotEntry("近战（保护）", host.EquippedMeleeWeapon, PrototypeMainMenuController.WeaponSlotType.Melee, true);

        GUILayout.Space(8f);
        GUILayout.Label("已装备护甲", host.BodyStyle);
        if (host.EquippedArmor.Count == 0)
        {
            GUILayout.Label("未装备护甲。", host.BodyStyle);
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
                GUILayout.Label(PrototypeMainMenuController.BuildItemInstanceDetail(ItemInstance.Create(armorInstance)), host.BodyStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("入库", host.ButtonStyle, GUILayout.Width(72f)))
                {
                    StoreEquippedArmorToStash(index);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("装包", host.ButtonStyle, GUILayout.Width(64f)))
                {
                    MoveEquippedArmorToBackpack(index);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("出售", host.ButtonStyle, GUILayout.Width(64f)))
                {
                    SellEquippedArmor(index);
                    GUIUtility.ExitGUI();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        DrawProtectedInventorySection("安全箱", host.SecureContainerInventory);
        DrawProtectedInventorySection("特殊装备", host.SpecialEquipmentInventory);

        GUILayout.EndScrollView();
        host.EndPanel();
    }

    private void DrawProtectedInventorySection(string title, InventoryContainer inventory)
    {
        GUILayout.Space(8f);
        GUILayout.Label(title, host.BodyStyle);
        if (inventory == null || inventory.IsEmpty)
        {
            GUILayout.Label("空。", host.BodyStyle);
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
            GUILayout.Label(GetInventoryEntryDetail(item), host.BodyStyle);
            if (GUILayout.Button("入库", host.ButtonStyle, GUILayout.Width(72f)))
            {
                MoveItemBetweenInventories(inventory, host.StashInventory, index, item.Quantity, "已存入仓库", "仓库空间不足，无法接收该堆叠。");
                GUIUtility.ExitGUI();
            }

            GUILayout.EndVertical();
        }
    }

    private void DrawWeaponSlotEntry(string label, ItemInstance weaponInstance, PrototypeMainMenuController.WeaponSlotType slotType, bool protectedOnDeath)
    {
        GUILayout.BeginVertical(host.ListStyle);
        GUILayout.Label($"{label}：{(weaponInstance != null ? weaponInstance.RichDisplayName : "空")}", host.BodyStyle);
        if (weaponInstance != null)
        {
            GUILayout.Label(PrototypeMainMenuController.BuildItemInstanceDetail(weaponInstance), host.BodyStyle);
        }
        if (protectedOnDeath)
        {
            GUILayout.Label("该栏位在战斗死亡后会保留。", host.BodyStyle);
        }

        if (weaponInstance != null && GUILayout.Button("入库", host.ButtonStyle, GUILayout.Width(120f)))
        {
            StoreEquippedWeapon(slotType);
            GUIUtility.ExitGUI();
        }

        if (weaponInstance != null && GUILayout.Button("出售", host.ButtonStyle, GUILayout.Width(120f)))
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

        host.SetFeedback($"{successVerb} {item.DisplayName} x{movedQuantity}。");
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
            host.SetFeedback($"需要等级 {armorDefinition.RequiredLevel} 才能装备 {armorDefinition.DisplayNameWithLevel}。");
            return;
        }

        if (!source.TryExtractItem(itemIndex, 1, out ItemInstance extractedItem) || extractedItem == null)
        {
            host.SetFeedback($"无法从{sourceLabel}装备护甲。");
            return;
        }

        ArmorInstance armorInstance = extractedItem.ToArmorInstance();
        if (armorInstance == null)
        {
            source.TryAddItemInstance(extractedItem);
            host.SetFeedback($"无法从{sourceLabel}装备护甲。");
            return;
        }

        host.EquippedArmor.Add(armorInstance);
        host.SetFeedback($"已装备 {armorInstance.DisplayName}。");
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
            host.SetFeedback("仓库空间不足，无法存放这件护甲。");
            return;
        }

        host.EquippedArmor.RemoveAt(armorIndex);
        host.SetFeedback($"已将 {armorDefinition.DisplayNameWithLevel} 存入仓库。");
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
            host.SetFeedback("战局背包空间不足，无法放入这件护甲。");
            return;
        }

        host.EquippedArmor.RemoveAt(armorIndex);
        host.SetFeedback($"已将 {armorDefinition.DisplayNameWithLevel} 放入战局背包。");
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

                ItemInstance weaponInstance = extractedItem;
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

        host.SetFeedback("已将战局背包内容全部存入仓库。");
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

        ItemInstance weaponInstance = extractedItem;
        if (weaponInstance == null)
        {
            source.TryAddItemInstance(extractedItem);
            return;
        }

        host.WeaponLocker.Add(weaponInstance);
        host.SetFeedback($"已将 {weaponInstance.DisplayName} 存入武器柜。");
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

        ItemInstance weaponInstance = extractedItem;
        if (weaponInstance == null || weaponInstance.WeaponDefinition == null)
        {
            source.TryAddItemInstance(extractedItem);
            return;
        }

        bool expectsMelee = slotType == PrototypeMainMenuController.WeaponSlotType.Melee;
        if (weaponInstance.WeaponDefinition.IsMeleeWeapon != expectsMelee)
        {
            source.TryAddItemInstance(extractedItem);
            host.SetFeedback(expectsMelee
                ? "只有近战武器可以装备到近战槽。"
                : "近战武器必须装备到近战槽。");
            return;
        }

        if (host.PlayerLevel < weaponInstance.WeaponDefinition.RequiredLevel)
        {
            source.TryAddItemInstance(extractedItem);
            host.SetFeedback($"需要等级 {weaponInstance.WeaponDefinition.RequiredLevel} 才能装备 {weaponInstance.DisplayName}。");
            return;
        }

        ItemInstance replacedWeapon = GetEquippedWeapon(slotType);
        if (replacedWeapon != null && !source.TryAddItemInstance(replacedWeapon.Clone()))
        {
            source.TryAddItemInstance(extractedItem);
            host.SetFeedback("战局背包空间不足，无法放入被替换的武器。");
            return;
        }

        SetEquippedWeapon(slotType, weaponInstance);
        host.SetFeedback($"已装备 {weaponInstance.DisplayName}。");
        host.AutoSaveIfNeeded();
    }

    private void EquipWeaponFromLocker(int lockerIndex, PrototypeMainMenuController.WeaponSlotType slotType)
    {
        if (lockerIndex < 0 || lockerIndex >= host.WeaponLocker.Count)
        {
            return;
        }

        ItemInstance weaponInstance = host.WeaponLocker[lockerIndex];
        if (weaponInstance == null || weaponInstance.WeaponDefinition == null)
        {
            host.WeaponLocker.RemoveAt(lockerIndex);
            host.AutoSaveIfNeeded();
            return;
        }

        bool expectsMelee = slotType == PrototypeMainMenuController.WeaponSlotType.Melee;
        if (weaponInstance.WeaponDefinition.IsMeleeWeapon != expectsMelee)
        {
            host.SetFeedback(expectsMelee
                ? "只有近战武器可以装备到近战槽。"
                : "近战武器必须装备到近战槽。");
            return;
        }

        if (host.PlayerLevel < weaponInstance.WeaponDefinition.RequiredLevel)
        {
            host.SetFeedback($"需要等级 {weaponInstance.WeaponDefinition.RequiredLevel} 才能装备 {weaponInstance.DisplayName}。");
            return;
        }

        host.WeaponLocker.RemoveAt(lockerIndex);
        ItemInstance replacedWeapon = GetEquippedWeapon(slotType);
        if (replacedWeapon != null)
        {
            host.WeaponLocker.Add(replacedWeapon);
        }

        SetEquippedWeapon(slotType, weaponInstance);
        host.SetFeedback($"已装备 {weaponInstance.DisplayName}。");
        host.AutoSaveIfNeeded();
    }

    private void StoreEquippedWeapon(PrototypeMainMenuController.WeaponSlotType slotType)
    {
        ItemInstance weaponInstance = GetEquippedWeapon(slotType);
        if (weaponInstance == null)
        {
            return;
        }

        host.WeaponLocker.Add(weaponInstance);
        SetEquippedWeapon(slotType, null);
        host.SetFeedback($"已将 {weaponInstance.DisplayName} 存入武器柜。");
        host.AutoSaveIfNeeded();
    }

    private ItemInstance GetEquippedWeapon(PrototypeMainMenuController.WeaponSlotType slotType)
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

    private void SetEquippedWeapon(PrototypeMainMenuController.WeaponSlotType slotType, ItemInstance weaponInstance)
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
        return PrototypeMainMenuController.BuildItemInstanceDetail(item);
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

        int sellPrice = merchantCatalog.GetSellPrice(item.CloneWithQuantity(quantity));
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
            host.SetFeedback("仓库空间不足，无法接收这笔付款。");
            return;
        }

        host.SetFeedback($"{successPrefix}：{extractedItem.DisplayName} x{extractedItem.Quantity}，获得 {sellPrice}。");
        host.AutoSaveIfNeeded();
    }

    private void SellWeaponFromLocker(int lockerIndex)
    {
        PrototypeMerchantCatalog merchantCatalog = host.MerchantCatalog;
        if (merchantCatalog == null || lockerIndex < 0 || lockerIndex >= host.WeaponLocker.Count)
        {
            return;
        }

        ItemInstance weaponInstance = host.WeaponLocker[lockerIndex];
        PrototypeWeaponDefinition weaponDefinition = weaponInstance != null ? weaponInstance.WeaponDefinition : null;
        int sellPrice = merchantCatalog.GetSellPrice(weaponInstance);
        if (weaponDefinition == null || sellPrice <= 0 || !host.CanReceiveFunds(sellPrice))
        {
            host.SetFeedback("仓库空间不足，无法接收这笔付款。");
            return;
        }

        host.WeaponLocker.RemoveAt(lockerIndex);
        if (!host.TryAddFunds(sellPrice))
        {
            host.WeaponLocker.Insert(Mathf.Clamp(lockerIndex, 0, host.WeaponLocker.Count), weaponInstance);
            host.SetFeedback("仓库空间不足，无法接收这笔付款。");
            return;
        }

        host.SetFeedback($"已出售 {weaponDefinition.DisplayNameWithLevel}，获得 {sellPrice}。");
        host.AutoSaveIfNeeded();
    }

    private void SellEquippedWeapon(PrototypeMainMenuController.WeaponSlotType slotType)
    {
        PrototypeMerchantCatalog merchantCatalog = host.MerchantCatalog;
        if (merchantCatalog == null)
        {
            return;
        }

        ItemInstance weaponInstance = GetEquippedWeapon(slotType);
        PrototypeWeaponDefinition weaponDefinition = weaponInstance != null ? weaponInstance.WeaponDefinition : null;
        int sellPrice = merchantCatalog.GetSellPrice(weaponInstance);
        if (weaponDefinition == null || sellPrice <= 0 || !host.CanReceiveFunds(sellPrice))
        {
            host.SetFeedback("仓库空间不足，无法接收这笔付款。");
            return;
        }

        SetEquippedWeapon(slotType, null);
        if (!host.TryAddFunds(sellPrice))
        {
            SetEquippedWeapon(slotType, weaponInstance);
            host.SetFeedback("仓库空间不足，无法接收这笔付款。");
            return;
        }

        host.SetFeedback($"已出售 {weaponDefinition.DisplayNameWithLevel}，获得 {sellPrice}。");
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
        int sellPrice = merchantCatalog.GetSellPrice(ItemInstance.Create(armorInstance));
        if (armorDefinition == null || sellPrice <= 0 || !host.CanReceiveFunds(sellPrice))
        {
            host.SetFeedback("仓库空间不足，无法接收这笔付款。");
            return;
        }

        host.EquippedArmor.RemoveAt(armorIndex);
        if (!host.TryAddFunds(sellPrice))
        {
            host.EquippedArmor.Insert(Mathf.Clamp(armorIndex, 0, host.EquippedArmor.Count), armorInstance);
            host.SetFeedback("仓库空间不足，无法接收这笔付款。");
            return;
        }

        host.SetFeedback($"已出售 {armorDefinition.DisplayNameWithLevel}，获得 {sellPrice}。");
        host.AutoSaveIfNeeded();
    }
}
