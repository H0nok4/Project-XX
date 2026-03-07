using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeFpsInput))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private PrototypeFpsInput fpsInput;
    [SerializeField] private InventoryContainer primaryInventory;
    [SerializeField] private PlayerInteractionState interactionState;

    [Header("Query")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Prompt")]
    [SerializeField] private bool showPrompt = true;
    [SerializeField] private Vector2 promptSize = new Vector2(320f, 42f);

    private InteractionQueryResult currentQuery;
    private GUIStyle promptStyle;

    public InventoryContainer PrimaryInventory => primaryInventory;
    public Camera InteractionCamera => interactionCamera;
    public IInteractable CurrentInteractable => currentQuery.Interactable;
    public bool HasInteractionTarget => currentQuery.HasValue;

    private void Awake()
    {
        ResolveReferences();
        EnsureInventoryWindowController();
    }

    private void Update()
    {
        if (interactionState != null && interactionState.IsUiFocused)
        {
            currentQuery = default;
            return;
        }

        RefreshInteractionQuery();

        if (fpsInput != null && fpsInput.InteractPressedThisFrame && currentQuery.HasValue)
        {
            IInteractable interactable = currentQuery.Interactable;
            if (interactable != null && interactable.CanInteract(this))
            {
                interactable.Interact(this);
                RefreshInteractionQuery();
            }
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
        interactDistance = Mathf.Max(0.5f, interactDistance);
        promptSize.x = Mathf.Max(160f, promptSize.x);
        promptSize.y = Mathf.Max(24f, promptSize.y);
    }

    private void OnGUI()
    {
        if (!showPrompt || !currentQuery.HasValue)
        {
            return;
        }

        if (interactionState != null && interactionState.IsUiFocused)
        {
            return;
        }

        IInteractable interactable = currentQuery.Interactable;
        if (interactable == null || !interactable.CanInteract(this))
        {
            return;
        }

        string prompt = interactable.GetInteractionLabel(this);
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return;
        }

        string bindingDisplay = fpsInput != null ? fpsInput.GetBindingDisplayString("Interact") : string.Empty;
        string promptPrefix = string.IsNullOrWhiteSpace(bindingDisplay) ? "[E]" : $"[{bindingDisplay}]";

        EnsurePromptStyle();

        float x = (Screen.width - promptSize.x) * 0.5f;
        float y = Screen.height - promptSize.y - 36f;
        GUI.Box(new Rect(x, y, promptSize.x, promptSize.y), $"{promptPrefix} {prompt}", promptStyle);
    }

    public void Configure(Camera targetCamera, InventoryContainer inventory = null)
    {
        interactionCamera = targetCamera;
        if (inventory != null)
        {
            primaryInventory = inventory;
        }

        ResolveReferences();
    }

    private void RefreshInteractionQuery()
    {
        currentQuery = default;

        if (interactionCamera == null)
        {
            return;
        }

        Ray ray = new Ray(interactionCamera.transform.position, interactionCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactionMask, triggerInteraction);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        System.Array.Sort(hits, CompareByDistance);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            Transform hitTransform = hit.collider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            IInteractable interactable = ResolveInteractable(hit.collider);
            if (interactable == null)
            {
                continue;
            }

            currentQuery = new InteractionQueryResult(interactable, hit.collider, hit);
            return;
        }
    }

    private void ResolveReferences()
    {
        if (fpsInput == null)
        {
            fpsInput = GetComponent<PrototypeFpsInput>();
        }

        if (interactionCamera == null)
        {
            interactionCamera = GetComponentInChildren<Camera>();
        }

        if (primaryInventory == null)
        {
            primaryInventory = GetComponent<InventoryContainer>();
        }

        if (interactionState == null)
        {
            interactionState = GetComponent<PlayerInteractionState>();
        }
    }

    private void EnsureInventoryWindowController()
    {
        if (GetComponent<PlayerInventoryWindowController>() != null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            gameObject.AddComponent<PlayerInventoryWindowController>();
        }
    }

    private void EnsurePromptStyle()
    {
        if (promptStyle != null)
        {
            return;
        }

        promptStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            normal = { textColor = Color.white }
        };
        promptStyle.padding = new RectOffset(10, 10, 8, 8);
    }

    private static IInteractable ResolveInteractable(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        MonoBehaviour[] localComponents = hitCollider.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in localComponents)
        {
            if (component is IInteractable interactable)
            {
                return interactable;
            }
        }

        MonoBehaviour[] parentComponents = hitCollider.GetComponentsInParent<MonoBehaviour>(true);
        foreach (MonoBehaviour component in parentComponents)
        {
            if (component is IInteractable interactable)
            {
                return interactable;
            }
        }

        return null;
    }

    private static int CompareByDistance(RaycastHit left, RaycastHit right)
    {
        return left.distance.CompareTo(right.distance);
    }
}

[DisallowMultipleComponent]
public class PlayerInteractionState : MonoBehaviour
{
    [SerializeField] private bool uiFocused;
    private readonly HashSet<int> uiFocusOwners = new HashSet<int>();

    public bool IsUiFocused => uiFocused || uiFocusOwners.Count > 0;

    public void SetUiFocused(bool focused)
    {
        uiFocused = focused;
        if (focused)
        {
            uiFocusOwners.Clear();
        }
    }

    public void SetUiFocused(Object owner, bool focused)
    {
        if (owner == null)
        {
            SetUiFocused(focused);
            return;
        }

        uiFocused = false;

        if (focused)
        {
            uiFocusOwners.Add(owner.GetInstanceID());
        }
        else
        {
            uiFocusOwners.Remove(owner.GetInstanceID());
        }
    }
}
