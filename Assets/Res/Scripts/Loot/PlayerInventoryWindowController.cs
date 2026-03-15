using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeFpsInput))]
public class PlayerInventoryWindowController : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PrototypeFpsInput fpsInput;
    [SerializeField] private PlayerInteractionState interactionState;
    [SerializeField] private LootContainerWindowController lootWindowController;
    [SerializeField] private PrototypeUnitVitals playerVitals;

    private bool isOpen;
    private Vector2 scrollPosition;
    private GUIStyle windowStyle;
    private GUIStyle buttonStyle;
    private GUIStyle listStyle;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (playerVitals != null)
        {
            playerVitals.Died += HandlePlayerDied;
        }

        if (IsPlayerDead())
        {
            Close();
        }
    }

    private void OnDisable()
    {
        if (playerVitals != null)
        {
            playerVitals.Died -= HandlePlayerDied;
        }

        Close();
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
            return;
        }

        if (fpsInput != null && fpsInput.ToggleCursorPressedThisFrame)
        {
            Close();
        }
    }

    private void OnGUI()
    {
        if (IsPlayerDead())
        {
            if (IsOpen)
            {
                Close();
            }

            return;
        }

        if (!IsOpen || interactor == null || interactor.PrimaryInventory == null)
        {
            return;
        }

        EnsureStyles();

        InventoryContainer inventory = interactor.PrimaryInventory;
        InventoryContainer secureInventory = interactor.SecureInventory;
        InventoryContainer specialInventory = interactor.SpecialInventory;
        Rect panelRect = new Rect(Screen.width * 0.5f - 340f, Screen.height * 0.5f - 220f, 680f, 440f);
        GUI.Box(panelRect, string.Empty, windowStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - 28f, panelRect.height - 24f));
        GUILayout.Label("Raid Backpack", windowStyle);
        GUILayout.Label(
            $"Backpack {inventory.OccupiedSlots}/{inventory.MaxSlots}  Weight {inventory.CurrentWeight:0.0}/{inventory.MaxWeight:0.0}\nSecure {GetStackCount(secureInventory)}/{GetMaxSlots(secureInventory)}  Special {GetStackCount(specialInventory)}/{GetMaxSlots(specialInventory)}\nPress Tab or Esc to close. Only raid backpack contents can be dropped.",
            windowStyle);

        GUILayout.Space(6f);

        if (inventory.IsEmpty)
        {
            GUILayout.Label("Backpack is empty.", windowStyle);
        }
        else
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(260f));
            for (int index = 0; index < inventory.Items.Count; index++)
            {
                ItemInstance item = inventory.Items[index];
                if (item == null || !item.IsDefined())
                {
                    continue;
                }

                GUILayout.BeginVertical(listStyle);
                GUILayout.Label(item.Quantity > 1 ? $"{item.RichDisplayName} x{item.Quantity}" : item.RichDisplayName, windowStyle);
                GUILayout.Label(GetInventoryEntryDetail(item), windowStyle);
                GUILayout.BeginHorizontal();

                if (item.IsWeapon && GUILayout.Button("Equip", buttonStyle, GUILayout.Width(100f)))
                {
                    EquipWeaponFromInventory(index);
                    GUIUtility.ExitGUI();
                }

                if (item.Quantity > 1 && GUILayout.Button("Drop 1", buttonStyle, GUILayout.Width(100f)))
                {
                    DropItem(index, 1);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button(item.Quantity > 1 ? "Drop Stack" : "Drop", buttonStyle, GUILayout.Width(120f)))
                {
                    DropItem(index, item.Quantity);
                    GUIUtility.ExitGUI();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

        DrawProtectedSection("Secure Container", secureInventory);
        DrawProtectedSection("Special Equipment", specialInventory);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(120f)))
        {
            Close();
            GUIUtility.ExitGUI();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    public void Open()
    {
        ResolveReferences();
        if (interactor == null || interactor.PrimaryInventory == null || IsPlayerDead())
        {
            return;
        }

        if (lootWindowController != null && lootWindowController.IsOpen)
        {
            lootWindowController.Close();
        }

        isOpen = true;
        scrollPosition = Vector2.zero;
        SetUiFocus(true);
    }

    public void Close()
    {
        if (!isOpen && (interactionState == null || !interactionState.IsUiFocused))
        {
            return;
        }

        isOpen = false;
        SetUiFocus(false);
    }

    private void DropItem(int itemIndex, int quantity)
    {
        if (interactor == null || interactor.PrimaryInventory == null)
        {
            return;
        }

        if (!interactor.PrimaryInventory.TryExtractItem(itemIndex, quantity, out ItemInstance extractedItem) || extractedItem == null || !extractedItem.IsDefined())
        {
            return;
        }

        Transform dropOrigin = interactor.InteractionCamera != null ? interactor.InteractionCamera.transform : interactor.transform;
        GroundLootItem.SpawnDroppedItem(dropOrigin, extractedItem);
    }

    private void EquipWeaponFromInventory(int itemIndex)
    {
        if (interactor == null || interactor.PrimaryInventory == null)
        {
            return;
        }

        PrototypeFpsController controller = interactor.GetComponent<PrototypeFpsController>();
        if (controller == null)
        {
            return;
        }

        if (!interactor.PrimaryInventory.TryExtractItem(itemIndex, 1, out ItemInstance extractedItem) || extractedItem == null || !extractedItem.IsWeapon)
        {
            return;
        }

        if (!controller.TryEquipInventoryWeapon(extractedItem, out ItemInstance overflowWeapon))
        {
            interactor.PrimaryInventory.TryAddItemInstance(extractedItem);
            return;
        }

        if (overflowWeapon == null)
        {
            return;
        }

        Transform dropOrigin = interactor.InteractionCamera != null ? interactor.InteractionCamera.transform : interactor.transform;
        GroundLootItem.SpawnDroppedItem(dropOrigin, overflowWeapon);
    }

    private void DrawProtectedSection(string title, InventoryContainer inventory)
    {
        GUILayout.Space(8f);
        GUILayout.Label(title, windowStyle);
        if (inventory == null)
        {
            GUILayout.Label("Unavailable.", windowStyle);
            return;
        }

        if (inventory.IsEmpty)
        {
            GUILayout.Label("Empty.", windowStyle);
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
            GUILayout.Label(item.Quantity > 1 ? $"{item.RichDisplayName} x{item.Quantity}" : item.RichDisplayName, windowStyle);
            GUILayout.Label(GetInventoryEntryDetail(item), windowStyle);
            GUILayout.EndVertical();
        }
    }

    private static string GetInventoryEntryDetail(ItemInstance item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        string detail;
        if (item.IsWeapon && item.WeaponDefinition != null)
        {
            if (item.WeaponDefinition.IsThrowableWeapon)
            {
                detail = $"Throwable  Fuse {item.WeaponDefinition.FuseSeconds:0.0}s  Radius {item.WeaponDefinition.ExplosionRadius:0.0}m  Weight {item.TotalWeight:0.00}";
            }
            else
            {
                detail = item.WeaponDefinition.IsMeleeWeapon
                    ? $"Melee  Weight {item.TotalWeight:0.00}"
                    : $"Ammo {item.MagazineAmmo}/{item.WeaponDefinition.MagazineSize}  Weight {item.TotalWeight:0.00}";
            }
        }
        else if (item.IsArmor)
        {
            detail = $"Durability {item.CurrentDurability:0.0}  Weight {item.TotalWeight:0.00}";
        }
        else
        {
            detail = $"Weight {item.TotalWeight:0.00}";
        }

        string affixSummary = ItemAffixUtility.BuildAffixSummaryRich(item.Affixes);
        if (!string.IsNullOrWhiteSpace(affixSummary))
        {
            detail = string.IsNullOrWhiteSpace(detail) ? affixSummary : $"{detail}\n{affixSummary}";
        }

        string skillSummary = ItemSkillUtility.BuildSkillSummaryRich(item.Skills);
        if (!string.IsNullOrWhiteSpace(skillSummary))
        {
            detail = string.IsNullOrWhiteSpace(detail) ? skillSummary : $"{detail}\n{skillSummary}";
        }

        return detail;
    }

    private static int GetStackCount(InventoryContainer inventory)
    {
        return inventory != null ? inventory.Items.Count : 0;
    }

    private static int GetMaxSlots(InventoryContainer inventory)
    {
        return inventory != null ? inventory.MaxSlots : 0;
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

    private void EnsureStyles()
    {
        if (windowStyle == null)
        {
            windowStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true,
                normal = { textColor = Color.white }
            };
            windowStyle.padding = new RectOffset(12, 12, 10, 10);
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14
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
    }
}
