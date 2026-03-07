using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeFpsInput))]
public class PlayerInventoryWindowController : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PrototypeFpsInput fpsInput;
    [SerializeField] private PlayerInteractionState interactionState;
    [SerializeField] private LootContainerWindowController lootWindowController;

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

    private void OnDisable()
    {
        Close();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void Update()
    {
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
        if (!IsOpen || interactor == null || interactor.PrimaryInventory == null)
        {
            return;
        }

        EnsureStyles();

        InventoryContainer inventory = interactor.PrimaryInventory;
        Rect panelRect = new Rect(Screen.width * 0.5f - 310f, Screen.height * 0.5f - 190f, 620f, 380f);
        GUI.Box(panelRect, string.Empty, windowStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - 28f, panelRect.height - 24f));
        GUILayout.Label("Raid Backpack", windowStyle);
        GUILayout.Label(
            $"Slots {inventory.Items.Count}/{inventory.MaxSlots}  Weight {inventory.CurrentWeight:0.0}/{inventory.MaxWeight:0.0}\nPress Tab or Esc to close. Drop items to spawn them at your feet.",
            windowStyle);

        GUILayout.Space(6f);

        if (inventory.IsEmpty)
        {
            GUILayout.Label("Backpack is empty.", windowStyle);
        }
        else
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(240f));
            for (int index = 0; index < inventory.Items.Count; index++)
            {
                ItemInstance item = inventory.Items[index];
                if (item == null || !item.IsDefined())
                {
                    continue;
                }

                GUILayout.BeginVertical(listStyle);
                GUILayout.Label($"{item.DisplayName} x{item.Quantity}", windowStyle);
                GUILayout.Label($"Weight {item.TotalWeight:0.00}", windowStyle);
                GUILayout.BeginHorizontal();

                if (item.Quantity > 1 && GUILayout.Button("Drop 1", buttonStyle, GUILayout.Width(100f)))
                {
                    DropItem(index, 1);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Drop Stack", buttonStyle, GUILayout.Width(120f)))
                {
                    DropItem(index, item.Quantity);
                    GUIUtility.ExitGUI();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

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
        if (interactor == null || interactor.PrimaryInventory == null)
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
        GroundLootItem.SpawnDroppedItem(dropOrigin, extractedItem.Definition, extractedItem.Quantity);
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
