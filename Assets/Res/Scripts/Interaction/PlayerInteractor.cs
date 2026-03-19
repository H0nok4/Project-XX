using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeFpsInput))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private PrototypeFpsInput fpsInput;
    [SerializeField] private InventoryContainer primaryInventory;
    [SerializeField] private InventoryContainer secureInventory;
    [SerializeField] private InventoryContainer specialInventory;
    [SerializeField] private PlayerInteractionState interactionState;

    [Header("Query")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Prompt")]
    [SerializeField] private bool showPrompt = true;
    [SerializeField] private Vector2 promptSize = new Vector2(320f, 42f);
    [SerializeField] private bool autoAddInventoryWindowController = true;

    private InteractionQueryResult currentQuery;
    private RectTransform promptRoot;
    private Text promptText;
    private CanvasGroup promptCanvasGroup;

    public InventoryContainer PrimaryInventory => primaryInventory;
    public InventoryContainer SecureInventory => secureInventory;
    public InventoryContainer SpecialInventory => specialInventory;
    public Camera InteractionCamera => interactionCamera;
    public IInteractable CurrentInteractable => currentQuery.Interactable;
    public bool HasInteractionTarget => currentQuery.HasValue;

    private void Awake()
    {
        ResolveReferences();
        EnsureInventoryWindowController();
        EnsurePromptUi();
    }

    private void Update()
    {
        if (interactionState != null && interactionState.IsUiFocused)
        {
            currentQuery = default;
            UpdatePromptUi();
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

        UpdatePromptUi();
    }

    private void OnValidate()
    {
        ResolveReferences();
        interactDistance = Mathf.Max(0.5f, interactDistance);
        promptSize.x = Mathf.Max(160f, promptSize.x);
        promptSize.y = Mathf.Max(24f, promptSize.y);
    }

    public void Configure(
        Camera targetCamera,
        InventoryContainer inventory = null,
        InventoryContainer secureContainer = null,
        InventoryContainer specialContainer = null)
    {
        interactionCamera = targetCamera;
        if (inventory != null)
        {
            primaryInventory = inventory;
        }

        if (secureContainer != null)
        {
            secureInventory = secureContainer;
        }

        if (specialContainer != null)
        {
            specialInventory = specialContainer;
        }

        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (promptRoot != null)
        {
            Destroy(promptRoot.gameObject);
        }
    }

    private void OnDisable()
    {
        SetPromptVisible(false);
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

        if (secureInventory == null)
        {
            Transform secureTransform = transform.Find("SecureContainer_Runtime");
            if (secureTransform != null)
            {
                secureInventory = secureTransform.GetComponent<InventoryContainer>();
            }
        }

        if (specialInventory == null)
        {
            Transform specialTransform = transform.Find("SpecialEquipment_Runtime");
            if (specialTransform != null)
            {
                specialInventory = specialTransform.GetComponent<InventoryContainer>();
            }
        }

        if (interactionState == null)
        {
            interactionState = GetComponent<PlayerInteractionState>();
        }
    }

    private void EnsureInventoryWindowController()
    {
        if (!autoAddInventoryWindowController)
        {
            return;
        }

        if (GetComponent<PlayerInventoryWindowController>() != null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            gameObject.AddComponent<PlayerInventoryWindowController>();
        }
    }

    private void EnsurePromptUi()
    {
        if (promptRoot != null)
        {
            return;
        }

        PrototypeRuntimeUiManager manager = PrototypeRuntimeUiManager.GetOrCreate();
        RectTransform layerRoot = manager.GetLayerRoot(PrototypeUiLayer.Overlay);
        promptRoot = PrototypeUiToolkit.CreateRectTransform("InteractionPrompt", layerRoot);
        PrototypeUiToolkit.SetAnchor(
            promptRoot,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 36f),
            new Vector2(Mathf.Max(160f, promptSize.x), Mathf.Max(24f, promptSize.y)));

        Image background = promptRoot.gameObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);
        promptCanvasGroup = PrototypeUiToolkit.EnsureCanvasGroup(promptRoot);

        RectTransform labelRoot = PrototypeUiToolkit.CreateRectTransform("Label", promptRoot);
        PrototypeUiToolkit.SetStretch(labelRoot, 12f, 12f, 8f, 8f);
        promptText = labelRoot.gameObject.AddComponent<Text>();
        promptText.font = manager.RuntimeFont;
        promptText.fontSize = 14;
        promptText.fontStyle = FontStyle.Bold;
        promptText.color = Color.white;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.raycastTarget = false;
        promptText.supportRichText = false;
        promptText.horizontalOverflow = HorizontalWrapMode.Wrap;
        promptText.verticalOverflow = VerticalWrapMode.Overflow;
        PrototypeUiToolkit.SetVisible(promptRoot, false);
    }

    private void UpdatePromptUi()
    {
        if (!showPrompt)
        {
            SetPromptVisible(false);
            return;
        }

        EnsurePromptUi();
        if (promptRoot != null)
        {
            promptRoot.sizeDelta = new Vector2(Mathf.Max(160f, promptSize.x), Mathf.Max(24f, promptSize.y));
        }

        if (!currentQuery.HasValue || (interactionState != null && interactionState.IsUiFocused))
        {
            SetPromptVisible(false);
            return;
        }

        IInteractable interactable = currentQuery.Interactable;
        if (interactable == null || !interactable.CanInteract(this))
        {
            SetPromptVisible(false);
            return;
        }

        string prompt = interactable.GetInteractionLabel(this);
        if (string.IsNullOrWhiteSpace(prompt))
        {
            SetPromptVisible(false);
            return;
        }

        string bindingDisplay = fpsInput != null ? fpsInput.GetBindingDisplayString("Interact") : string.Empty;
        string promptPrefix = string.IsNullOrWhiteSpace(bindingDisplay) ? "[E]" : $"[{bindingDisplay}]";
        if (promptText != null)
        {
            promptText.text = $"{promptPrefix} {prompt}";
        }

        SetPromptVisible(true);
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot == null)
        {
            return;
        }

        PrototypeUiToolkit.SetVisible(promptRoot, visible);
        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = visible ? 1f : 0f;
            promptCanvasGroup.interactable = false;
            promptCanvasGroup.blocksRaycasts = false;
        }
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
