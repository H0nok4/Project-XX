using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeFpsInput))]
public class LootContainerWindowController : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PrototypeFpsInput fpsInput;
    [SerializeField] private PlayerInteractionState interactionState;
    [SerializeField] private PlayerInventoryWindowController inventoryWindowController;
    [SerializeField] private float maxOpenDistance = 3.5f;

    private LootContainer openContainer;
    private GUIStyle windowStyle;
    private GUIStyle buttonStyle;
    private Vector2 scrollPosition;

    public LootContainer OpenContainer => openContainer;
    public bool IsOpen => openContainer != null;

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
        maxOpenDistance = Mathf.Max(1f, maxOpenDistance);
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        if (fpsInput != null && fpsInput.ToggleCursorPressedThisFrame)
        {
            Close();
            return;
        }

        if (openContainer == null || !openContainer.isActiveAndEnabled)
        {
            Close();
            return;
        }

        if (!IsWithinOpenDistance())
        {
            Close();
        }
    }

    private void OnGUI()
    {
        if (!IsOpen || interactor == null || openContainer == null)
        {
            return;
        }

        EnsureStyles();

        InventoryContainer playerInventory = interactor.PrimaryInventory;
        InventoryContainer lootInventory = openContainer.Inventory;
        if (playerInventory == null || lootInventory == null)
        {
            return;
        }

        Rect panelRect = new Rect(Screen.width * 0.5f - 280f, Screen.height * 0.5f - 180f, 560f, 360f);
        GUI.Box(panelRect, string.Empty, windowStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - 28f, panelRect.height - 24f));
        GUILayout.Label($"{openContainer.ContainerLabel}", windowStyle);
        GUILayout.Label(
            $"Loot {lootInventory.Items.Count}/{lootInventory.MaxSlots}  Weight {lootInventory.CurrentWeight:0.0}/{lootInventory.MaxWeight:0.0}\nBackpack {playerInventory.Items.Count}/{playerInventory.MaxSlots}  Weight {playerInventory.CurrentWeight:0.0}/{playerInventory.MaxWeight:0.0}",
            windowStyle);

        GUILayout.Space(6f);

        if (lootInventory.IsEmpty)
        {
            GUILayout.Label("Container is empty.", windowStyle);
        }
        else
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(220f));
            for (int index = 0; index < lootInventory.Items.Count; index++)
            {
                ItemInstance item = lootInventory.Items[index];
                if (item == null || !item.IsDefined())
                {
                    continue;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{item.DisplayName} x{item.Quantity}", windowStyle, GUILayout.Width(320f));

                if (GUILayout.Button("Take", buttonStyle, GUILayout.Width(90f)))
                {
                    lootInventory.TryTransferItemTo(playerInventory, index, item.Quantity, out _);
                    GUIUtility.ExitGUI();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Take All", buttonStyle, GUILayout.Width(120f)))
        {
            TakeAll();
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(120f)))
        {
            Close();
            GUIUtility.ExitGUI();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
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
        if (container == null)
        {
            return;
        }

        ResolveReferences();
        if (inventoryWindowController != null && inventoryWindowController.IsOpen)
        {
            inventoryWindowController.Close();
        }

        openContainer = container;
        scrollPosition = Vector2.zero;
        SetUiFocus(true);
    }

    public void Close()
    {
        if (!IsOpen && (interactionState == null || !interactionState.IsUiFocused))
        {
            return;
        }

        openContainer = null;
        SetUiFocus(false);
    }

    private void TakeAll()
    {
        if (openContainer == null || interactor == null)
        {
            return;
        }

        InventoryContainer lootInventory = openContainer.Inventory;
        InventoryContainer playerInventory = interactor.PrimaryInventory;
        if (lootInventory == null || playerInventory == null)
        {
            return;
        }

        for (int index = lootInventory.Items.Count - 1; index >= 0; index--)
        {
            ItemInstance item = lootInventory.Items[index];
            if (item == null || !item.IsDefined())
            {
                continue;
            }

            lootInventory.TryTransferItemTo(playerInventory, index, item.Quantity, out _);
        }
    }

    private bool IsWithinOpenDistance()
    {
        if (interactor == null || openContainer == null)
        {
            return false;
        }

        Transform targetTransform = openContainer.GetInteractionTransform();
        if (targetTransform == null)
        {
            return false;
        }

        return Vector3.Distance(interactor.transform.position, targetTransform.position) <= maxOpenDistance;
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
    }
}
