using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PrototypeMainMenuController : MonoBehaviour
{
    private enum MenuPage
    {
        Home = 0,
        Warehouse = 1,
        Merchants = 2
    }

    private enum WeaponSlotType
    {
        Primary = 0,
        Secondary = 1,
        Melee = 2
    }

    [Header("Profile")]
    [SerializeField] private PrototypeItemCatalog itemCatalog;
    [SerializeField] private PrototypeMerchantCatalog merchantCatalog;
    [SerializeField] private string raidSceneName = "SampleScene";
    [SerializeField] private int stashSlots = 32;
    [SerializeField] private float stashMaxWeight = 0f;
    [FormerlySerializedAs("loadoutSlots")]
    [SerializeField] private int raidBackpackSlots = 12;
    [FormerlySerializedAs("loadoutMaxWeight")]
    [SerializeField] private float raidBackpackMaxWeight = 20f;
    [SerializeField] private int secureContainerSlots = 4;
    [SerializeField] private float secureContainerMaxWeight = 6f;
    [SerializeField] private int specialEquipmentSlots = 4;
    [SerializeField] private float specialEquipmentMaxWeight = 8f;
    [SerializeField] private bool autoSaveOnInventoryChange = true;

    [Header("Scene Dressing")]
    [SerializeField] private Color stashColor = new Color(0.2f, 0.65f, 0.38f, 1f);
    [FormerlySerializedAs("loadoutColor")]
    [SerializeField] private Color backpackColor = new Color(0.75f, 0.26f, 0.22f, 1f);
    [SerializeField] private Color lockerColor = new Color(0.2f, 0.48f, 0.78f, 1f);
    [FormerlySerializedAs("extractedColor")]
    [SerializeField] private Color protectedColor = new Color(0.82f, 0.64f, 0.18f, 1f);

    private InventoryContainer stashInventory;
    private InventoryContainer raidBackpackInventory;
    private InventoryContainer secureContainerInventory;
    private InventoryContainer specialEquipmentInventory;
    private PrototypeProfileService.ProfileData profile;
    private ItemDefinition cashDefinition;
    private readonly System.Collections.Generic.List<PrototypeWeaponDefinition> weaponLocker = new System.Collections.Generic.List<PrototypeWeaponDefinition>();
    private readonly System.Collections.Generic.List<ArmorDefinition> equippedArmor = new System.Collections.Generic.List<ArmorDefinition>();
    private PrototypeWeaponDefinition equippedPrimaryWeapon;
    private PrototypeWeaponDefinition equippedSecondaryWeapon;
    private PrototypeWeaponDefinition equippedMeleeWeapon;
    private MenuPage currentPage;
    private Vector2 stashScroll;
    private Vector2 backpackScroll;
    private Vector2 lockerScroll;
    private Vector2 protectedScroll;
    private Vector2 weaponMerchantScroll;
    private Vector2 medicalMerchantScroll;
    private Vector2 armorMerchantScroll;
    private string feedbackMessage = string.Empty;
    private float feedbackUntilTime;
    private GUIStyle titleStyle;
    private GUIStyle sectionStyle;
    private GUIStyle bodyStyle;
    private GUIStyle listStyle;
    private GUIStyle buttonStyle;

    private void Awake()
    {
        ResolveCatalog();
        EnsureContainers();
        LoadProfileIntoContainers();
    }

    private void OnDisable()
    {
        SaveProfileFromContainers();
    }

    private void OnApplicationQuit()
    {
        SaveProfileFromContainers();
    }

    private void OnValidate()
    {
        stashSlots = Mathf.Max(4, stashSlots);
        stashMaxWeight = Mathf.Max(0f, stashMaxWeight);
        raidBackpackSlots = Mathf.Max(4, raidBackpackSlots);
        raidBackpackMaxWeight = Mathf.Max(0f, raidBackpackMaxWeight);
        secureContainerSlots = Mathf.Max(1, secureContainerSlots);
        secureContainerMaxWeight = Mathf.Max(0f, secureContainerMaxWeight);
        specialEquipmentSlots = Mathf.Max(1, specialEquipmentSlots);
        specialEquipmentMaxWeight = Mathf.Max(0f, specialEquipmentMaxWeight);
        ResolveCatalog();
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawBackground();
        DrawNavigation();

        if (currentPage == MenuPage.Warehouse)
        {
            DrawWarehousePage();
        }
        else if (currentPage == MenuPage.Merchants)
        {
            DrawMerchantsPage();
        }
        else
        {
            DrawHomePage();
        }

        DrawFooter();
    }

    private void DrawBackground()
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.94f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUI.Label(new Rect(40f, 32f, 640f, 48f), "Project-XX", titleStyle);
        GUI.Label(new Rect(44f, 88f, 560f, 28f), "Single-player raid prototype", bodyStyle);
    }

    private void DrawNavigation()
    {
        Rect navRect = new Rect(40f, 140f, 220f, 300f);
        GUI.Box(navRect, string.Empty, sectionStyle);

        GUILayout.BeginArea(new Rect(navRect.x + 16f, navRect.y + 16f, navRect.width - 32f, navRect.height - 32f));
        GUILayout.Label("Operations", sectionStyle);
        GUILayout.Space(12f);

        if (GUILayout.Button("Deploy", buttonStyle, GUILayout.Height(42f)))
        {
            currentPage = MenuPage.Home;
        }

        if (GUILayout.Button("Warehouse", buttonStyle, GUILayout.Height(42f)))
        {
            currentPage = MenuPage.Warehouse;
        }

        if (GUILayout.Button("Merchants", buttonStyle, GUILayout.Height(42f)))
        {
            currentPage = MenuPage.Merchants;
        }

        GUILayout.Space(10f);
        if (GUILayout.Button("Save Profile", buttonStyle, GUILayout.Height(34f)))
        {
            SaveProfileFromContainers();
            SetFeedback("Profile saved.");
        }

        if (GUILayout.Button("Reset Profile", buttonStyle, GUILayout.Height(34f)))
        {
            ResetProfile();
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Quit", buttonStyle, GUILayout.Height(34f)))
        {
            SaveProfileFromContainers();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        GUILayout.EndArea();
    }

    private void DrawHomePage()
    {
        Rect panelRect = new Rect(292f, 140f, Mathf.Max(560f, Screen.width - 336f), 380f);
        GUI.Box(panelRect, string.Empty, sectionStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 18f, panelRect.y + 16f, panelRect.width - 36f, panelRect.height - 32f));
        GUILayout.Label("Ready Room", sectionStyle);
        GUILayout.Space(10f);
        GUILayout.Label(
            "Warehouse items and weapon locker weapons are safe. The raid backpack, equipped firearms, and armor are risky. The melee slot, secure container, and special equipment slots are protected and survive raid death.",
            bodyStyle);

        GUILayout.Space(18f);
        GUILayout.Label($"可用资金：{GetAvailableFunds()} {GetCurrencyLabel()}", bodyStyle);
        GUILayout.Space(8f);
        GUILayout.Label(BuildSummaryText(), bodyStyle);

        GUILayout.Space(20f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Enter Battle", buttonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            StartRaid();
        }

        if (GUILayout.Button("Open Warehouse", buttonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            currentPage = MenuPage.Warehouse;
        }

        if (GUILayout.Button("Visit Merchants", buttonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            currentPage = MenuPage.Merchants;
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawWarehousePage()
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

    private void DrawMerchantsPage()
    {
        if (merchantCatalog == null || merchantCatalog.Merchants == null || merchantCatalog.Merchants.Count == 0)
        {
            Rect emptyRect = new Rect(292f, 140f, Mathf.Max(560f, Screen.width - 336f), 220f);
            BeginPanel(emptyRect, "商人", lockerColor, $"资金 {GetAvailableFunds()} {GetCurrencyLabel()}");
            GUILayout.Label("No merchant catalog configured.", bodyStyle);
            EndPanel();
            return;
        }

        float panelTop = 140f;
        float panelHeight = Mathf.Max(400f, Screen.height - 220f);
        float panelWidth = Mathf.Max(250f, (Screen.width - 356f) / 3f);
        float firstX = 292f;

        DrawMerchantPanel(new Rect(firstX, panelTop, panelWidth, panelHeight), 0, stashColor, ref weaponMerchantScroll);
        if (merchantCatalog.Merchants.Count > 1)
        {
            DrawMerchantPanel(new Rect(firstX + panelWidth + 16f, panelTop, panelWidth, panelHeight), 1, backpackColor, ref medicalMerchantScroll);
        }

        if (merchantCatalog.Merchants.Count > 2)
        {
            DrawMerchantPanel(new Rect(firstX + (panelWidth + 16f) * 2f, panelTop, panelWidth, panelHeight), 2, protectedColor, ref armorMerchantScroll);
        }
    }

    private void DrawFooter()
    {
        if (string.IsNullOrWhiteSpace(feedbackMessage) || Time.time > feedbackUntilTime)
        {
            return;
        }

        GUI.Label(new Rect(44f, Screen.height - 42f, Mathf.Max(900f, Screen.width - 88f), 24f), feedbackMessage, bodyStyle);
    }

    private void DrawStashPanel(Rect rect)
    {
        BeginPanel(rect, "Warehouse Stash", stashColor, stashInventory != null
            ? $"Stacks {stashInventory.Items.Count}/{stashInventory.MaxSlots}  Weight {stashInventory.CurrentWeight:0.0}/{stashInventory.MaxWeight:0.0}"
            : "No inventory");

        if (stashInventory == null || stashInventory.IsEmpty)
        {
            GUILayout.Label("Empty.", bodyStyle);
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

                GUILayout.BeginVertical(listStyle);
                GUILayout.Label($"{item.DisplayName} x{item.Quantity}", bodyStyle);
                GUILayout.Label($"Weight {item.TotalWeight:0.00}", bodyStyle);
                GUILayout.BeginHorizontal();
                if (item.Definition is ArmorDefinition)
                {
                    if (GUILayout.Button("Equip", buttonStyle, GUILayout.Width(70f)))
                    {
                        EquipArmorFromInventory(stashInventory, index, "Warehouse");
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Bag", buttonStyle, GUILayout.Width(60f)))
                    {
                        MoveItemBetweenInventories(stashInventory, raidBackpackInventory, index, item.Quantity, "Packed", "Raid backpack has no space for that stack.");
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Sell", buttonStyle, GUILayout.Width(60f)))
                    {
                        SellItemFromInventory(stashInventory, index, item.Quantity, "Sold armor");
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    if (GUILayout.Button("Bag", buttonStyle, GUILayout.Width(56f)))
                    {
                        MoveItemBetweenInventories(stashInventory, raidBackpackInventory, index, item.Quantity, "Packed", "Raid backpack has no space for that stack.");
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Secure", buttonStyle, GUILayout.Width(68f)))
                    {
                        MoveItemBetweenInventories(stashInventory, secureContainerInventory, index, item.Quantity, "Secured", "Secure container has no space for that stack.");
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Special", buttonStyle, GUILayout.Width(68f)))
                    {
                        MoveItemBetweenInventories(stashInventory, specialEquipmentInventory, index, item.Quantity, "Equipped safely", "Special equipment slots have no space for that stack.");
                        GUIUtility.ExitGUI();
                    }

                    if (cashDefinition != null && item.Definition != cashDefinition && GUILayout.Button("Sell", buttonStyle, GUILayout.Width(56f)))
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

        EndPanel();
    }

    private void DrawRaidBackpackPanel(Rect rect)
    {
        BeginPanel(rect, "Raid Backpack", backpackColor, raidBackpackInventory != null
            ? $"Stacks {raidBackpackInventory.Items.Count}/{raidBackpackInventory.MaxSlots}  Weight {raidBackpackInventory.CurrentWeight:0.0}/{raidBackpackInventory.MaxWeight:0.0}"
            : "No inventory");

        if (raidBackpackInventory == null || raidBackpackInventory.IsEmpty)
        {
            GUILayout.Label("Empty.", bodyStyle);
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

                GUILayout.BeginVertical(listStyle);
                GUILayout.Label($"{item.DisplayName} x{item.Quantity}", bodyStyle);
                GUILayout.Label($"Weight {item.TotalWeight:0.00}", bodyStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Store", buttonStyle, GUILayout.Width(72f)))
                {
                    MoveItemBetweenInventories(raidBackpackInventory, stashInventory, index, item.Quantity, "Stored", "Warehouse has no space for that stack.");
                    GUIUtility.ExitGUI();
                }

                if (item.Definition is ArmorDefinition && GUILayout.Button("Equip", buttonStyle, GUILayout.Width(72f)))
                {
                    EquipArmorFromInventory(raidBackpackInventory, index, "Raid backpack");
                    GUIUtility.ExitGUI();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

        GUILayout.Space(8f);
        if (GUILayout.Button("Store All", buttonStyle, GUILayout.Height(32f)))
        {
            StoreAllRaidBackpack();
            GUIUtility.ExitGUI();
        }

        EndPanel();
    }

    private void DrawWeaponLockerPanel(Rect rect)
    {
        BeginPanel(rect, "Weapon Locker", lockerColor, $"Stored weapons {weaponLocker.Count}");

        if (weaponLocker.Count == 0)
        {
            GUILayout.Label("No safe weapons in storage.", bodyStyle);
        }
        else
        {
            lockerScroll = GUILayout.BeginScrollView(lockerScroll, GUILayout.Height(rect.height - 130f));
            for (int index = 0; index < weaponLocker.Count; index++)
            {
                PrototypeWeaponDefinition weapon = weaponLocker[index];
                if (weapon == null)
                {
                    continue;
                }

                GUILayout.BeginVertical(listStyle);
                GUILayout.Label(weapon.DisplayName, bodyStyle);
                GUILayout.Label(weapon.IsMeleeWeapon ? "Melee" : "Firearm", bodyStyle);
                if (weapon.IsMeleeWeapon)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Equip Melee", buttonStyle, GUILayout.Width(108f)))
                    {
                        EquipWeaponFromLocker(index, WeaponSlotType.Melee);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Sell", buttonStyle, GUILayout.Width(64f)))
                    {
                        SellWeaponFromLocker(index);
                        GUIUtility.ExitGUI();
                    }

                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Primary", buttonStyle, GUILayout.Width(90f)))
                    {
                        EquipWeaponFromLocker(index, WeaponSlotType.Primary);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Secondary", buttonStyle, GUILayout.Width(90f)))
                    {
                        EquipWeaponFromLocker(index, WeaponSlotType.Secondary);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Sell", buttonStyle, GUILayout.Width(64f)))
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

        EndPanel();
    }

    private void DrawProtectedGearPanel(Rect rect)
    {
        string subtitle =
            $"Armor {equippedArmor.Count}  Secure {GetInventoryStackCount(secureContainerInventory)}  Special {GetInventoryStackCount(specialEquipmentInventory)}";
        BeginPanel(rect, "Protected Gear", protectedColor, subtitle);

        protectedScroll = GUILayout.BeginScrollView(protectedScroll, GUILayout.Height(rect.height - 130f));

        DrawWeaponSlotEntry("Primary", equippedPrimaryWeapon, WeaponSlotType.Primary, false);
        DrawWeaponSlotEntry("Secondary", equippedSecondaryWeapon, WeaponSlotType.Secondary, false);
        DrawWeaponSlotEntry("Melee (Safe)", equippedMeleeWeapon, WeaponSlotType.Melee, true);

        GUILayout.Space(8f);
        GUILayout.Label("Equipped Armor", bodyStyle);
        if (equippedArmor.Count == 0)
        {
            GUILayout.Label("No armor equipped.", bodyStyle);
        }
        else
        {
            for (int index = 0; index < equippedArmor.Count; index++)
            {
                ArmorDefinition armorDefinition = equippedArmor[index];
                if (armorDefinition == null)
                {
                    continue;
                }

                GUILayout.BeginVertical(listStyle);
                GUILayout.Label(armorDefinition.DisplayName, bodyStyle);
                GUILayout.Label($"Coverage {string.Join(", ", armorDefinition.CoveredPartIds)}", bodyStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Store", buttonStyle, GUILayout.Width(72f)))
                {
                    StoreEquippedArmorToStash(index);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Bag", buttonStyle, GUILayout.Width(64f)))
                {
                    MoveEquippedArmorToBackpack(index);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Sell", buttonStyle, GUILayout.Width(64f)))
                {
                    SellEquippedArmor(index);
                    GUIUtility.ExitGUI();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        DrawProtectedInventorySection("Secure Container", secureContainerInventory);
        DrawProtectedInventorySection("Special Equipment", specialEquipmentInventory);

        GUILayout.EndScrollView();
        EndPanel();
    }

    private void DrawProtectedInventorySection(string title, InventoryContainer inventory)
    {
        GUILayout.Space(8f);
        GUILayout.Label(title, bodyStyle);
        if (inventory == null || inventory.IsEmpty)
        {
            GUILayout.Label("Empty.", bodyStyle);
            return;
        }

        for (int index = 0; index < inventory.Items.Count; index++)
        {
            ItemInstance item = inventory.Items[index];
            if (item == null || !item.IsDefined())
            {
                continue;
            }

            GUILayout.BeginVertical(listStyle);
            GUILayout.Label($"{item.DisplayName} x{item.Quantity}", bodyStyle);
            GUILayout.Label($"Weight {item.TotalWeight:0.00}", bodyStyle);
            if (GUILayout.Button("Store", buttonStyle, GUILayout.Width(72f)))
            {
                MoveItemBetweenInventories(inventory, stashInventory, index, item.Quantity, "Stored", "Warehouse has no space for that stack.");
                GUIUtility.ExitGUI();
            }

            GUILayout.EndVertical();
        }
    }

    private void DrawWeaponSlotEntry(string label, PrototypeWeaponDefinition weaponDefinition, WeaponSlotType slotType, bool protectedOnDeath)
    {
        GUILayout.BeginVertical(listStyle);
        GUILayout.Label($"{label}: {(weaponDefinition != null ? weaponDefinition.DisplayName : "Empty")}", bodyStyle);
        if (protectedOnDeath)
        {
            GUILayout.Label("This slot survives raid death.", bodyStyle);
        }

        if (weaponDefinition != null && GUILayout.Button("Store", buttonStyle, GUILayout.Width(120f)))
        {
            StoreEquippedWeapon(slotType);
            GUIUtility.ExitGUI();
        }

        if (weaponDefinition != null && GUILayout.Button("Sell", buttonStyle, GUILayout.Width(120f)))
        {
            SellEquippedWeapon(slotType);
            GUIUtility.ExitGUI();
        }

        GUILayout.EndVertical();
    }

    private void DrawMerchantPanel(Rect rect, int merchantIndex, Color accent, ref Vector2 scroll)
    {
        if (merchantCatalog == null || merchantCatalog.Merchants == null || merchantIndex < 0 || merchantIndex >= merchantCatalog.Merchants.Count)
        {
            return;
        }

        PrototypeMerchantCatalog.MerchantDefinition merchant = merchantCatalog.Merchants[merchantIndex];
        if (merchant == null)
        {
            return;
        }

        BeginPanel(rect, merchant.displayName, accent, $"资金 {GetAvailableFunds()} {GetCurrencyLabel()}");
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(rect.height - 130f));

        if (merchant.weaponOffers != null && merchant.weaponOffers.Count > 0)
        {
            GUILayout.Label("Weapons", bodyStyle);
            for (int index = 0; index < merchant.weaponOffers.Count; index++)
            {
                PrototypeMerchantCatalog.WeaponOffer offer = merchant.weaponOffers[index];
                if (offer?.definition == null || offer.price <= 0)
                {
                    continue;
                }

                GUILayout.BeginVertical(listStyle);
                GUILayout.Label(offer.definition.DisplayName, bodyStyle);
                GUILayout.Label($"{(offer.definition.IsMeleeWeapon ? "Melee" : "Firearm")}  Price {offer.price}", bodyStyle);
                if (GUILayout.Button("Buy", buttonStyle, GUILayout.Width(96f)))
                {
                    BuyWeaponOffer(offer);
                    GUIUtility.ExitGUI();
                }

                GUILayout.EndVertical();
            }

            GUILayout.Space(8f);
        }

        if (merchant.itemOffers != null && merchant.itemOffers.Count > 0)
        {
            GUILayout.Label("Supplies", bodyStyle);
            for (int index = 0; index < merchant.itemOffers.Count; index++)
            {
                PrototypeMerchantCatalog.ItemOffer offer = merchant.itemOffers[index];
                if (offer?.definition == null || offer.quantity <= 0 || offer.price <= 0)
                {
                    continue;
                }

                GUILayout.BeginVertical(listStyle);
                GUILayout.Label($"{offer.definition.DisplayName} x{offer.quantity}", bodyStyle);
                GUILayout.Label($"Price {offer.price}", bodyStyle);
                if (GUILayout.Button("Buy", buttonStyle, GUILayout.Width(96f)))
                {
                    BuyItemOffer(offer);
                    GUIUtility.ExitGUI();
                }

                GUILayout.EndVertical();
            }
        }

        GUILayout.EndScrollView();
        EndPanel();
    }

    private void BuyItemOffer(PrototypeMerchantCatalog.ItemOffer offer)
    {
        if (offer?.definition == null || offer.quantity <= 0 || offer.price <= 0 || stashInventory == null)
        {
            return;
        }

        if (!TrySpendFunds(offer.price, "Not enough cash in warehouse."))
        {
            return;
        }

        if (!stashInventory.TryAddItem(offer.definition, offer.quantity, out int addedQuantity) || addedQuantity < offer.quantity)
        {
            TryAddFunds(offer.price);
            SetFeedback("Warehouse has no space for that purchase.");
            return;
        }

        SetFeedback($"Bought {offer.definition.DisplayName} x{offer.quantity}.");
        AutoSaveIfNeeded();
    }

    private void BuyWeaponOffer(PrototypeMerchantCatalog.WeaponOffer offer)
    {
        if (offer?.definition == null || offer.price <= 0)
        {
            return;
        }

        if (!TrySpendFunds(offer.price, "Not enough cash in warehouse."))
        {
            return;
        }

        weaponLocker.Add(offer.definition);
        SetFeedback($"Bought {offer.definition.DisplayName}.");
        AutoSaveIfNeeded();
    }

    private void SellItemFromInventory(InventoryContainer source, int itemIndex, int quantity, string successPrefix)
    {
        if (source == null || merchantCatalog == null || itemIndex < 0 || itemIndex >= source.Items.Count)
        {
            return;
        }

        ItemInstance item = source.Items[itemIndex];
        if (item == null || !item.IsDefined() || item.Definition == cashDefinition)
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

        if (!TryAddFunds(sellPrice))
        {
            source.TryAddItem(extractedItem.Definition, extractedItem.Quantity, out _);
            SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        SetFeedback($"{successPrefix}: {extractedItem.DisplayName} x{extractedItem.Quantity} for {sellPrice}.");
        AutoSaveIfNeeded();
    }

    private void SellWeaponFromLocker(int lockerIndex)
    {
        if (merchantCatalog == null || lockerIndex < 0 || lockerIndex >= weaponLocker.Count)
        {
            return;
        }

        PrototypeWeaponDefinition weaponDefinition = weaponLocker[lockerIndex];
        int sellPrice = merchantCatalog.GetSellPrice(weaponDefinition);
        if (weaponDefinition == null || sellPrice <= 0 || !CanReceiveFunds(sellPrice))
        {
            SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        weaponLocker.RemoveAt(lockerIndex);
        if (!TryAddFunds(sellPrice))
        {
            weaponLocker.Insert(Mathf.Clamp(lockerIndex, 0, weaponLocker.Count), weaponDefinition);
            SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        SetFeedback($"Sold {weaponDefinition.DisplayName} for {sellPrice}.");
        AutoSaveIfNeeded();
    }

    private void SellEquippedWeapon(WeaponSlotType slotType)
    {
        if (merchantCatalog == null)
        {
            return;
        }

        PrototypeWeaponDefinition weaponDefinition = GetEquippedWeapon(slotType);
        int sellPrice = merchantCatalog.GetSellPrice(weaponDefinition);
        if (weaponDefinition == null || sellPrice <= 0 || !CanReceiveFunds(sellPrice))
        {
            SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        SetEquippedWeapon(slotType, null);
        if (!TryAddFunds(sellPrice))
        {
            SetEquippedWeapon(slotType, weaponDefinition);
            SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        SetFeedback($"Sold {weaponDefinition.DisplayName} for {sellPrice}.");
        AutoSaveIfNeeded();
    }

    private void SellEquippedArmor(int armorIndex)
    {
        if (merchantCatalog == null || armorIndex < 0 || armorIndex >= equippedArmor.Count)
        {
            return;
        }

        ArmorDefinition armorDefinition = equippedArmor[armorIndex];
        int sellPrice = merchantCatalog.GetSellPrice(armorDefinition, 1);
        if (armorDefinition == null || sellPrice <= 0 || !CanReceiveFunds(sellPrice))
        {
            SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        equippedArmor.RemoveAt(armorIndex);
        if (!TryAddFunds(sellPrice))
        {
            equippedArmor.Insert(Mathf.Clamp(armorIndex, 0, equippedArmor.Count), armorDefinition);
            SetFeedback("Warehouse has no room for the payment.");
            return;
        }

        SetFeedback($"Sold {armorDefinition.DisplayName} for {sellPrice}.");
        AutoSaveIfNeeded();
    }

    private int GetAvailableFunds()
    {
        return cashDefinition != null && stashInventory != null ? stashInventory.CountItem(cashDefinition) : 0;
    }

    private string GetCurrencyLabel()
    {
        return cashDefinition != null && !string.IsNullOrWhiteSpace(cashDefinition.DisplayName)
            ? cashDefinition.DisplayName
            : "现金";
    }

    private bool CanReceiveFunds(int amount)
    {
        return amount > 0
            && cashDefinition != null
            && stashInventory != null
            && stashInventory.GetAddableQuantity(cashDefinition, amount) >= amount;
    }

    private bool TryAddFunds(int amount)
    {
        return amount > 0
            && cashDefinition != null
            && stashInventory != null
            && stashInventory.TryAddItem(cashDefinition, amount, out int addedQuantity)
            && addedQuantity >= amount;
    }

    private bool TrySpendFunds(int amount, string failureMessage)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (cashDefinition == null || stashInventory == null || stashInventory.CountItem(cashDefinition) < amount)
        {
            SetFeedback(failureMessage);
            return false;
        }

        if (!stashInventory.TryRemoveItem(cashDefinition, amount, out int removedQuantity) || removedQuantity < amount)
        {
            if (removedQuantity > 0)
            {
                stashInventory.TryAddItem(cashDefinition, removedQuantity, out _);
            }

            SetFeedback(failureMessage);
            return false;
        }

        return true;
    }

    private void BeginPanel(Rect rect, string title, Color accent, string subtitle)
    {
        GUI.Box(rect, string.Empty, sectionStyle);

        Color previousColor = GUI.color;
        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x + 16f, rect.y + 18f, 72f, 4f), Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUILayout.BeginArea(new Rect(rect.x + 16f, rect.y + 18f, rect.width - 32f, rect.height - 36f));
        GUILayout.Label(title, sectionStyle);
        GUILayout.Label(subtitle, bodyStyle);
        GUILayout.Space(10f);
    }

    private void EndPanel()
    {
        GUILayout.EndArea();
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
            SetFeedback(failureMessage);
            return;
        }

        SetFeedback($"{successVerb} {item.DisplayName} x{movedQuantity}.");
        AutoSaveIfNeeded();
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

        if (!source.TryExtractItem(itemIndex, 1, out _))
        {
            SetFeedback($"Could not equip armor from {sourceLabel.ToLowerInvariant()}.");
            return;
        }

        equippedArmor.Add(armorDefinition);
        SetFeedback($"Equipped {armorDefinition.DisplayName}.");
        AutoSaveIfNeeded();
    }

    private void StoreEquippedArmorToStash(int armorIndex)
    {
        if (armorIndex < 0 || armorIndex >= equippedArmor.Count)
        {
            return;
        }

        ArmorDefinition armorDefinition = equippedArmor[armorIndex];
        if (armorDefinition == null)
        {
            equippedArmor.RemoveAt(armorIndex);
            AutoSaveIfNeeded();
            return;
        }

        if (stashInventory == null || !stashInventory.TryAddItem(armorDefinition, 1, out int addedQuantity) || addedQuantity <= 0)
        {
            SetFeedback("Warehouse has no space for that armor.");
            return;
        }

        equippedArmor.RemoveAt(armorIndex);
        SetFeedback($"Stored {armorDefinition.DisplayName}.");
        AutoSaveIfNeeded();
    }

    private void MoveEquippedArmorToBackpack(int armorIndex)
    {
        if (armorIndex < 0 || armorIndex >= equippedArmor.Count)
        {
            return;
        }

        ArmorDefinition armorDefinition = equippedArmor[armorIndex];
        if (armorDefinition == null)
        {
            equippedArmor.RemoveAt(armorIndex);
            AutoSaveIfNeeded();
            return;
        }

        if (raidBackpackInventory == null || !raidBackpackInventory.TryAddItem(armorDefinition, 1, out int addedQuantity) || addedQuantity <= 0)
        {
            SetFeedback("Raid backpack has no room for that armor.");
            return;
        }

        equippedArmor.RemoveAt(armorIndex);
        SetFeedback($"Moved {armorDefinition.DisplayName} into the raid backpack.");
        AutoSaveIfNeeded();
    }

    private void StoreAllRaidBackpack()
    {
        if (raidBackpackInventory == null || stashInventory == null || raidBackpackInventory.IsEmpty)
        {
            return;
        }

        for (int index = raidBackpackInventory.Items.Count - 1; index >= 0; index--)
        {
            ItemInstance item = raidBackpackInventory.Items[index];
            if (item == null || !item.IsDefined())
            {
                continue;
            }

            raidBackpackInventory.TryTransferItemTo(stashInventory, index, item.Quantity, out _);
        }

        SetFeedback("Stored raid backpack items.");
        AutoSaveIfNeeded();
    }

    private void EquipWeaponFromLocker(int lockerIndex, WeaponSlotType slotType)
    {
        if (lockerIndex < 0 || lockerIndex >= weaponLocker.Count)
        {
            return;
        }

        PrototypeWeaponDefinition weaponDefinition = weaponLocker[lockerIndex];
        if (weaponDefinition == null)
        {
            weaponLocker.RemoveAt(lockerIndex);
            AutoSaveIfNeeded();
            return;
        }

        bool expectsMelee = slotType == WeaponSlotType.Melee;
        if (weaponDefinition.IsMeleeWeapon != expectsMelee)
        {
            SetFeedback(expectsMelee
                ? "Only melee weapons can be equipped in the melee slot."
                : "Melee weapons must be equipped in the melee slot.");
            return;
        }

        weaponLocker.RemoveAt(lockerIndex);
        PrototypeWeaponDefinition replacedWeapon = GetEquippedWeapon(slotType);
        if (replacedWeapon != null)
        {
            weaponLocker.Add(replacedWeapon);
        }

        SetEquippedWeapon(slotType, weaponDefinition);
        SetFeedback($"Equipped {weaponDefinition.DisplayName}.");
        AutoSaveIfNeeded();
    }

    private void StoreEquippedWeapon(WeaponSlotType slotType)
    {
        PrototypeWeaponDefinition weaponDefinition = GetEquippedWeapon(slotType);
        if (weaponDefinition == null)
        {
            return;
        }

        weaponLocker.Add(weaponDefinition);
        SetEquippedWeapon(slotType, null);
        SetFeedback($"Stored {weaponDefinition.DisplayName}.");
        AutoSaveIfNeeded();
    }

    private PrototypeWeaponDefinition GetEquippedWeapon(WeaponSlotType slotType)
    {
        switch (slotType)
        {
            case WeaponSlotType.Primary:
                return equippedPrimaryWeapon;

            case WeaponSlotType.Secondary:
                return equippedSecondaryWeapon;

            default:
                return equippedMeleeWeapon;
        }
    }

    private void SetEquippedWeapon(WeaponSlotType slotType, PrototypeWeaponDefinition weaponDefinition)
    {
        switch (slotType)
        {
            case WeaponSlotType.Primary:
                equippedPrimaryWeapon = weaponDefinition;
                break;

            case WeaponSlotType.Secondary:
                equippedSecondaryWeapon = weaponDefinition;
                break;

            default:
                equippedMeleeWeapon = weaponDefinition;
                break;
        }
    }

    private void StartRaid()
    {
        SaveProfileFromContainers();
        SceneManager.LoadScene(raidSceneName);
    }

    private InventoryContainer EnsureContainer(InventoryContainer existing, string objectName, string label, int slots, float maxWeight)
    {
        if (existing == null)
        {
            existing = CreateRuntimeContainer(objectName, label, slots, maxWeight);
        }
        else
        {
            existing.Configure(label, slots, maxWeight);
        }

        return existing;
    }

    private InventoryContainer CreateRuntimeContainer(string objectName, string label, int slots, float maxWeight)
    {
        Transform child = transform.Find(objectName);
        GameObject containerObject = child != null ? child.gameObject : new GameObject(objectName);
        containerObject.transform.SetParent(transform, false);
        containerObject.hideFlags = HideFlags.HideInHierarchy;

        InventoryContainer inventory = containerObject.GetComponent<InventoryContainer>();
        if (inventory == null)
        {
            inventory = containerObject.AddComponent<InventoryContainer>();
        }

        inventory.Configure(label, slots, maxWeight);
        return inventory;
    }

    private void ApplyProfileToContainers(PrototypeProfileService.ProfileData sourceProfile)
    {
        EnsureContainers();
        PrototypeProfileService.PopulateInventory(stashInventory, sourceProfile != null ? sourceProfile.stashItems : null, itemCatalog);
        PrototypeProfileService.PopulateInventory(raidBackpackInventory, sourceProfile != null ? sourceProfile.raidBackpackItems : null, itemCatalog);
        PrototypeProfileService.PopulateInventory(secureContainerInventory, sourceProfile != null ? sourceProfile.secureContainerItems : null, itemCatalog);
        PrototypeProfileService.PopulateInventory(specialEquipmentInventory, sourceProfile != null ? sourceProfile.specialEquipmentItems : null, itemCatalog);

        weaponLocker.Clear();
        equippedArmor.Clear();
        equippedPrimaryWeapon = null;
        equippedSecondaryWeapon = null;
        equippedMeleeWeapon = null;

        if (sourceProfile == null || itemCatalog == null)
        {
            return;
        }

        if (sourceProfile.stashWeaponIds != null)
        {
            for (int index = 0; index < sourceProfile.stashWeaponIds.Count; index++)
            {
                PrototypeWeaponDefinition weaponDefinition = itemCatalog.FindWeaponById(sourceProfile.stashWeaponIds[index]);
                if (weaponDefinition != null)
                {
                    weaponLocker.Add(weaponDefinition);
                }
            }
        }

        equippedArmor.AddRange(PrototypeProfileService.ResolveArmorDefinitions(sourceProfile.equippedArmorItems, itemCatalog));
        equippedPrimaryWeapon = itemCatalog.FindWeaponById(sourceProfile.equippedPrimaryWeaponId);
        equippedSecondaryWeapon = itemCatalog.FindWeaponById(sourceProfile.equippedSecondaryWeaponId);
        equippedMeleeWeapon = itemCatalog.FindWeaponById(sourceProfile.equippedMeleeWeaponId);
    }

    private void AutoSaveIfNeeded()
    {
        if (autoSaveOnInventoryChange)
        {
            SaveProfileFromContainers();
        }
    }

    private string BuildSummaryText()
    {
        return
            $"资金：{GetAvailableFunds()} {GetCurrencyLabel()}\n" +
            $"Warehouse item stacks: {GetInventoryStackCount(stashInventory)}\n" +
            $"Warehouse weapons: {weaponLocker.Count}\n" +
            $"Raid backpack stacks: {GetInventoryStackCount(raidBackpackInventory)}\n" +
            $"Secure container stacks: {GetInventoryStackCount(secureContainerInventory)}\n" +
            $"Special equipment stacks: {GetInventoryStackCount(specialEquipmentInventory)}\n" +
            $"Protected melee slot: {(equippedMeleeWeapon != null ? equippedMeleeWeapon.DisplayName : "Empty")}\n" +
            $"Primary: {(equippedPrimaryWeapon != null ? equippedPrimaryWeapon.DisplayName : "Empty")}\n" +
            $"Secondary: {(equippedSecondaryWeapon != null ? equippedSecondaryWeapon.DisplayName : "Empty")}\n" +
            $"Armor pieces: {equippedArmor.Count}\n" +
            $"Profile file: {PrototypeProfileService.SavePath}";
    }

    private static int GetInventoryStackCount(InventoryContainer inventory)
    {
        return inventory != null ? inventory.Items.Count : 0;
    }

    private void ResolveCatalog()
    {
        if (itemCatalog == null)
        {
            itemCatalog = Resources.Load<PrototypeItemCatalog>("PrototypeItemCatalog");
        }

        if (merchantCatalog == null)
        {
            merchantCatalog = Resources.Load<PrototypeMerchantCatalog>("PrototypeMerchantCatalog");
        }

        if (merchantCatalog == null && itemCatalog != null)
        {
            merchantCatalog = CreateRuntimeMerchantCatalog();
        }

        cashDefinition = itemCatalog != null ? itemCatalog.FindByItemId("cash_bundle") : null;
    }

    private PrototypeMerchantCatalog CreateRuntimeMerchantCatalog()
    {
        PrototypeMerchantCatalog runtimeCatalog = ScriptableObject.CreateInstance<PrototypeMerchantCatalog>();
        runtimeCatalog.hideFlags = HideFlags.HideAndDontSave;

        ItemDefinition rifleAmmo = itemCatalog.FindByItemId("rifle_ammo");
        ItemDefinition pistolAmmo = itemCatalog.FindByItemId("pistol_ammo");
        ItemDefinition medkit = itemCatalog.FindByItemId("field_medkit");
        ItemDefinition bandage = itemCatalog.FindByItemId("bandage_roll");
        ItemDefinition tourniquet = itemCatalog.FindByItemId("tourniquet");
        ItemDefinition splint = itemCatalog.FindByItemId("field_splint");
        ItemDefinition painkiller = itemCatalog.FindByItemId("painkillers");
        ItemDefinition helmet = itemCatalog.FindByItemId("helmet_alpha");
        ItemDefinition rig = itemCatalog.FindByItemId("armored_rig");
        PrototypeWeaponDefinition carbine = itemCatalog.FindWeaponById("carbine_alpha");
        PrototypeWeaponDefinition sidearm = itemCatalog.FindWeaponById("sidearm_9mm");
        PrototypeWeaponDefinition knife = itemCatalog.FindWeaponById("combat_knife");

        runtimeCatalog.Configure(
            0.55f,
            0.6f,
            new PrototypeMerchantCatalog.MerchantDefinition
            {
                merchantId = "weapons_trader",
                displayName = "武器商人",
                itemOffers = new System.Collections.Generic.List<PrototypeMerchantCatalog.ItemOffer>
                {
                    CreateItemOffer(rifleAmmo, 30, 6),
                    CreateItemOffer(pistolAmmo, 24, 4)
                },
                weaponOffers = new System.Collections.Generic.List<PrototypeMerchantCatalog.WeaponOffer>
                {
                    CreateWeaponOffer(carbine, 24),
                    CreateWeaponOffer(sidearm, 16),
                    CreateWeaponOffer(knife, 10)
                }
            },
            new PrototypeMerchantCatalog.MerchantDefinition
            {
                merchantId = "medical_vendor",
                displayName = "药品商人",
                itemOffers = new System.Collections.Generic.List<PrototypeMerchantCatalog.ItemOffer>
                {
                    CreateItemOffer(medkit, 1, 10),
                    CreateItemOffer(bandage, 1, 3),
                    CreateItemOffer(tourniquet, 1, 5),
                    CreateItemOffer(splint, 1, 4),
                    CreateItemOffer(painkiller, 1, 4)
                }
            },
            new PrototypeMerchantCatalog.MerchantDefinition
            {
                merchantId = "armor_vendor",
                displayName = "护甲商人",
                itemOffers = new System.Collections.Generic.List<PrototypeMerchantCatalog.ItemOffer>
                {
                    CreateItemOffer(helmet, 1, 14),
                    CreateItemOffer(rig, 1, 20)
                }
            });

        return runtimeCatalog;
    }

    private static PrototypeMerchantCatalog.ItemOffer CreateItemOffer(ItemDefinition definition, int quantity, int price)
    {
        if (definition == null || quantity <= 0 || price <= 0)
        {
            return null;
        }

        return new PrototypeMerchantCatalog.ItemOffer
        {
            definition = definition,
            quantity = quantity,
            price = price
        };
    }

    private static PrototypeMerchantCatalog.WeaponOffer CreateWeaponOffer(PrototypeWeaponDefinition definition, int price)
    {
        if (definition == null || price <= 0)
        {
            return null;
        }

        return new PrototypeMerchantCatalog.WeaponOffer
        {
            definition = definition,
            price = price
        };
    }

    private void EnsureContainers()
    {
        stashInventory = EnsureContainer(stashInventory, "Profile_Stash", "Warehouse", stashSlots, stashMaxWeight);
        raidBackpackInventory = EnsureContainer(raidBackpackInventory, "Profile_RaidBackpack", "Raid Backpack", raidBackpackSlots, raidBackpackMaxWeight);
        secureContainerInventory = EnsureContainer(secureContainerInventory, "Profile_SecureContainer", "Secure Container", secureContainerSlots, secureContainerMaxWeight);
        specialEquipmentInventory = EnsureContainer(specialEquipmentInventory, "Profile_SpecialEquipment", "Special Equipment", specialEquipmentSlots, specialEquipmentMaxWeight);
    }

    private void LoadProfileIntoContainers()
    {
        profile = PrototypeProfileService.LoadProfile(itemCatalog);
        ApplyProfileToContainers(profile);
    }

    private void SaveProfileFromContainers()
    {
        ResolveCatalog();
        EnsureContainers();

        if (profile == null)
        {
            profile = new PrototypeProfileService.ProfileData();
        }

        profile.stashItems = PrototypeProfileService.CaptureInventory(stashInventory);
        profile.raidBackpackItems = PrototypeProfileService.CaptureInventory(raidBackpackInventory);
        profile.secureContainerItems = PrototypeProfileService.CaptureInventory(secureContainerInventory);
        profile.specialEquipmentItems = PrototypeProfileService.CaptureInventory(specialEquipmentInventory);
        profile.equippedArmorItems = PrototypeProfileService.CaptureDefinitions(equippedArmor);
        profile.stashWeaponIds = PrototypeProfileService.CaptureWeaponIds(weaponLocker);
        profile.equippedPrimaryWeaponId = equippedPrimaryWeapon != null ? equippedPrimaryWeapon.WeaponId : string.Empty;
        profile.equippedSecondaryWeaponId = equippedSecondaryWeapon != null ? equippedSecondaryWeapon.WeaponId : string.Empty;
        profile.equippedMeleeWeaponId = equippedMeleeWeapon != null ? equippedMeleeWeapon.WeaponId : string.Empty;
        profile.loadoutItems.Clear();
        profile.extractedItems.Clear();
        PrototypeProfileService.SaveProfile(profile, itemCatalog);
    }

    private void ResetProfile()
    {
        profile = PrototypeProfileService.CreateDefaultProfile(itemCatalog);
        ApplyProfileToContainers(profile);
        SaveProfileFromContainers();
        SetFeedback("Profile reset to defaults.");
    }

    private void SetFeedback(string message)
    {
        feedbackMessage = message ?? string.Empty;
        feedbackUntilTime = Time.time + 2.6f;
    }

    private void EnsureStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (sectionStyle == null)
        {
            sectionStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };
            sectionStyle.padding = new RectOffset(14, 14, 12, 12);
        }

        if (bodyStyle == null)
        {
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.94f, 0.98f, 1f) }
            };
        }

        if (listStyle == null)
        {
            listStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft
            };
            listStyle.padding = new RectOffset(10, 10, 8, 8);
            listStyle.margin = new RectOffset(0, 0, 0, 8);
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14
            };
        }
    }
}
