using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PrototypeMainMenuController : MonoBehaviour
{
    private enum MenuPage
    {
        Home = 0,
        Warehouse = 1
    }

    private enum WeaponSlotType
    {
        Primary = 0,
        Secondary = 1,
        Melee = 2
    }

    [Header("Profile")]
    [SerializeField] private PrototypeItemCatalog itemCatalog;
    [SerializeField] private string raidSceneName = "SampleScene";
    [SerializeField] private int stashSlots = 32;
    [SerializeField] private float stashMaxWeight = 0f;
    [FormerlySerializedAs("loadoutSlots")]
    [SerializeField] private int raidBackpackSlots = 12;
    [FormerlySerializedAs("loadoutMaxWeight")]
    [SerializeField] private float raidBackpackMaxWeight = 20f;
    [SerializeField] private bool autoSaveOnInventoryChange = true;

    [Header("Scene Dressing")]
    [SerializeField] private Color stashColor = new Color(0.2f, 0.65f, 0.38f, 1f);
    [FormerlySerializedAs("loadoutColor")]
    [SerializeField] private Color backpackColor = new Color(0.75f, 0.26f, 0.22f, 1f);
    [SerializeField] private Color lockerColor = new Color(0.2f, 0.48f, 0.78f, 1f);
    [FormerlySerializedAs("extractedColor")]
    [SerializeField] private Color gearColor = new Color(0.82f, 0.64f, 0.18f, 1f);

    private InventoryContainer stashInventory;
    private InventoryContainer raidBackpackInventory;
    private PrototypeProfileService.ProfileData profile;
    private readonly List<PrototypeWeaponDefinition> weaponLocker = new List<PrototypeWeaponDefinition>();
    private readonly List<ArmorDefinition> equippedArmor = new List<ArmorDefinition>();
    private PrototypeWeaponDefinition equippedPrimaryWeapon;
    private PrototypeWeaponDefinition equippedSecondaryWeapon;
    private PrototypeWeaponDefinition equippedMeleeWeapon;
    private MenuPage currentPage;
    private Vector2 stashScroll;
    private Vector2 backpackScroll;
    private Vector2 lockerScroll;
    private Vector2 gearScroll;
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
        Rect navRect = new Rect(40f, 140f, 220f, 250f);
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
        Rect panelRect = new Rect(292f, 140f, Mathf.Max(560f, Screen.width - 336f), 360f);
        GUI.Box(panelRect, string.Empty, sectionStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 18f, panelRect.y + 16f, panelRect.width - 36f, panelRect.height - 32f));
        GUILayout.Label("Ready Room", sectionStyle);
        GUILayout.Space(10f);
        GUILayout.Label(
            "Items in the warehouse are safe. Weapons, armor, and consumables moved into the combat rig or raid backpack are at risk: extracting keeps them in your combat kit, but dying wipes the entire raid kit until you rebuild it from the warehouse.",
            bodyStyle);

        GUILayout.Space(18f);
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

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawWarehousePage()
    {
        float panelTop = 140f;
        float panelHeight = Mathf.Max(380f, Screen.height - 220f);
        float panelWidth = Mathf.Max(220f, (Screen.width - 392f) / 4f);
        float stashX = 292f;
        float backpackX = stashX + panelWidth + 16f;
        float lockerX = backpackX + panelWidth + 16f;
        float gearX = lockerX + panelWidth + 16f;

        DrawStashPanel(new Rect(stashX, panelTop, panelWidth, panelHeight));
        DrawRaidBackpackPanel(new Rect(backpackX, panelTop, panelWidth, panelHeight));
        DrawWeaponLockerPanel(new Rect(lockerX, panelTop, panelWidth, panelHeight));
        DrawEquippedGearPanel(new Rect(gearX, panelTop, panelWidth, panelHeight));
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
                    if (GUILayout.Button("Equip", buttonStyle, GUILayout.Width(80f)))
                    {
                        EquipArmorFromInventory(stashInventory, index, "Warehouse");
                        GUIUtility.ExitGUI();
                    }
                }

                if (GUILayout.Button("To Bag", buttonStyle, GUILayout.Width(80f)))
                {
                    MoveItemBetweenInventories(stashInventory, raidBackpackInventory, index, item.Quantity, "Packed", "Raid backpack has no space for that stack.");
                    GUIUtility.ExitGUI();
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
                if (GUILayout.Button("Store", buttonStyle, GUILayout.Width(80f)))
                {
                    MoveItemBetweenInventories(raidBackpackInventory, stashInventory, index, item.Quantity, "Stored", "Warehouse has no space for that stack.");
                    GUIUtility.ExitGUI();
                }

                if (item.Definition is ArmorDefinition && GUILayout.Button("Equip", buttonStyle, GUILayout.Width(80f)))
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
                    if (GUILayout.Button("Equip Melee", buttonStyle, GUILayout.Width(140f)))
                    {
                        EquipWeaponFromLocker(index, WeaponSlotType.Melee);
                        GUIUtility.ExitGUI();
                    }
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

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

        EndPanel();
    }

    private void DrawEquippedGearPanel(Rect rect)
    {
        BeginPanel(rect, "Combat Gear", gearColor, $"Armor {equippedArmor.Count}  Weapons {GetEquippedWeaponCount()}");

        gearScroll = GUILayout.BeginScrollView(gearScroll, GUILayout.Height(rect.height - 130f));
        DrawWeaponSlotEntry("Primary", equippedPrimaryWeapon, WeaponSlotType.Primary);
        DrawWeaponSlotEntry("Secondary", equippedSecondaryWeapon, WeaponSlotType.Secondary);
        DrawWeaponSlotEntry("Melee", equippedMeleeWeapon, WeaponSlotType.Melee);

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
                if (GUILayout.Button("Store", buttonStyle, GUILayout.Width(80f)))
                {
                    StoreEquippedArmorToStash(index);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("To Bag", buttonStyle, GUILayout.Width(80f)))
                {
                    MoveEquippedArmorToBackpack(index);
                    GUIUtility.ExitGUI();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        GUILayout.EndScrollView();
        EndPanel();
    }

    private void DrawWeaponSlotEntry(string label, PrototypeWeaponDefinition weaponDefinition, WeaponSlotType slotType)
    {
        GUILayout.BeginVertical(listStyle);
        GUILayout.Label($"{label}: {(weaponDefinition != null ? weaponDefinition.DisplayName : "Empty")}", bodyStyle);
        if (weaponDefinition != null)
        {
            if (GUILayout.Button("Store", buttonStyle, GUILayout.Width(120f)))
            {
                StoreEquippedWeapon(slotType);
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawFooter()
    {
        if (string.IsNullOrWhiteSpace(feedbackMessage) || Time.time > feedbackUntilTime)
        {
            return;
        }

        GUI.Label(new Rect(44f, Screen.height - 42f, Mathf.Max(900f, Screen.width - 88f), 24f), feedbackMessage, bodyStyle);
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

        SetFeedback("Stored all raid backpack items.");
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

    private int GetEquippedWeaponCount()
    {
        int count = 0;
        if (equippedPrimaryWeapon != null)
        {
            count++;
        }

        if (equippedSecondaryWeapon != null)
        {
            count++;
        }

        if (equippedMeleeWeapon != null)
        {
            count++;
        }

        return count;
    }

    private void StartRaid()
    {
        SaveProfileFromContainers();
        SceneManager.LoadScene(raidSceneName);
    }

    private void ResetProfile()
    {
        profile = PrototypeProfileService.CreateDefaultProfile(itemCatalog);
        ApplyProfileToContainers(profile);
        SaveProfileFromContainers();
        SetFeedback("Profile reset to defaults.");
    }

    private void ResolveCatalog()
    {
        if (itemCatalog == null)
        {
            itemCatalog = Resources.Load<PrototypeItemCatalog>("PrototypeItemCatalog");
        }
    }

    private void EnsureContainers()
    {
        if (stashInventory == null)
        {
            stashInventory = CreateRuntimeContainer("Profile_Stash", "Warehouse", stashSlots, stashMaxWeight);
        }
        else
        {
            stashInventory.Configure("Warehouse", stashSlots, stashMaxWeight);
        }

        if (raidBackpackInventory == null)
        {
            raidBackpackInventory = CreateRuntimeContainer("Profile_RaidBackpack", "Raid Backpack", raidBackpackSlots, raidBackpackMaxWeight);
        }
        else
        {
            raidBackpackInventory.Configure("Raid Backpack", raidBackpackSlots, raidBackpackMaxWeight);
        }
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

    private void LoadProfileIntoContainers()
    {
        profile = PrototypeProfileService.LoadProfile(itemCatalog);
        ApplyProfileToContainers(profile);
    }

    private void ApplyProfileToContainers(PrototypeProfileService.ProfileData sourceProfile)
    {
        EnsureContainers();
        PrototypeProfileService.PopulateInventory(stashInventory, sourceProfile != null ? sourceProfile.stashItems : null, itemCatalog);
        PrototypeProfileService.PopulateInventory(raidBackpackInventory, sourceProfile != null ? sourceProfile.raidBackpackItems : null, itemCatalog);

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
        profile.equippedArmorItems = PrototypeProfileService.CaptureDefinitions(equippedArmor);
        profile.stashWeaponIds = PrototypeProfileService.CaptureWeaponIds(weaponLocker);
        profile.equippedPrimaryWeaponId = equippedPrimaryWeapon != null ? equippedPrimaryWeapon.WeaponId : string.Empty;
        profile.equippedSecondaryWeaponId = equippedSecondaryWeapon != null ? equippedSecondaryWeapon.WeaponId : string.Empty;
        profile.equippedMeleeWeaponId = equippedMeleeWeapon != null ? equippedMeleeWeapon.WeaponId : string.Empty;
        profile.loadoutItems.Clear();
        profile.extractedItems.Clear();
        PrototypeProfileService.SaveProfile(profile, itemCatalog);
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
        int stashStacks = stashInventory != null ? stashInventory.Items.Count : 0;
        int backpackStacks = raidBackpackInventory != null ? raidBackpackInventory.Items.Count : 0;
        int warehouseWeapons = weaponLocker.Count;
        float backpackWeight = raidBackpackInventory != null ? raidBackpackInventory.CurrentWeight : 0f;
        float backpackCapacity = raidBackpackInventory != null ? raidBackpackInventory.MaxWeight : 0f;

        return
            $"Warehouse item stacks: {stashStacks}\n" +
            $"Warehouse weapons: {warehouseWeapons}\n" +
            $"Raid backpack stacks: {backpackStacks}\n" +
            $"Raid backpack weight: {backpackWeight:0.0}/{backpackCapacity:0.0}\n" +
            $"Primary: {(equippedPrimaryWeapon != null ? equippedPrimaryWeapon.DisplayName : "Empty")}\n" +
            $"Secondary: {(equippedSecondaryWeapon != null ? equippedSecondaryWeapon.DisplayName : "Empty")}\n" +
            $"Melee: {(equippedMeleeWeapon != null ? equippedMeleeWeapon.DisplayName : "Empty")}\n" +
            $"Armor pieces: {equippedArmor.Count}\n" +
            $"Profile file: {PrototypeProfileService.SavePath}";
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
