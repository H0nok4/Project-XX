using System;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PrototypeFpsInput : MonoBehaviour
{
    private const string DefaultActionsAssetPath = "Assets/InputSystem_Actions.inputactions";

    [Header("Input Asset")]
    [SerializeField] private InputActionAsset actionsAsset;
    [SerializeField] private string actionMapName = "Player";

    [Header("Action Names")]
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string shootActionName = "Attack";
    [SerializeField] private string interactActionName = "Interact";
    [SerializeField] private string inventoryActionName = "Inventory";
    [SerializeField] private string reloadActionName = "Reload";
    [SerializeField] private string toggleFireModeActionName = "ToggleFireMode";
    [SerializeField] private string equipPrimaryActionName = "EquipPrimary";
    [SerializeField] private string equipSecondaryActionName = "EquipSecondary";
    [SerializeField] private string equipMeleeActionName = "EquipMelee";
    [SerializeField] private string quickHealActionName = "QuickHeal";
    [SerializeField] private string stopBleedActionName = "StopBleed";
    [SerializeField] private string splintActionName = "UseSplint";
    [SerializeField] private string painkillerActionName = "UsePainkiller";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string crouchActionName = "Crouch";
    [SerializeField] private string walkActionName = "Sprint";

    [Header("Fallback Bindings")]
    [SerializeField] private string toggleCursorBinding = "<Keyboard>/escape";
    [SerializeField] private string bindingSaveKey = "prototype.fps.bindings";

    private InputActionAsset runtimeActions;
    private InputActionMap runtimeActionMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction shootAction;
    private InputAction interactAction;
    private InputAction inventoryAction;
    private InputAction reloadAction;
    private InputAction toggleFireModeAction;
    private InputAction equipPrimaryAction;
    private InputAction equipSecondaryAction;
    private InputAction equipMeleeAction;
    private InputAction quickHealAction;
    private InputAction stopBleedAction;
    private InputAction splintAction;
    private InputAction painkillerAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction walkAction;
    private InputAction toggleCursorAction;
    private InputActionMap fallbackActionMap;

    public bool IsReady => moveAction != null && lookAction != null && shootAction != null && jumpAction != null;
    public Vector2 Move => moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
    public Vector2 LookDelta => lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
    public bool ShootPressedThisFrame => shootAction?.WasPressedThisFrame() ?? false;
    public bool ShootHeld => shootAction?.IsPressed() ?? false;
    public bool InteractPressedThisFrame => interactAction?.WasPressedThisFrame() ?? false;
    public bool InteractHeld => interactAction?.IsPressed() ?? false;
    public bool InventoryTogglePressedThisFrame => inventoryAction?.WasPressedThisFrame() ?? false;
    public bool ReloadPressedThisFrame => reloadAction?.WasPressedThisFrame() ?? false;
    public bool ToggleFireModePressedThisFrame => toggleFireModeAction?.WasPressedThisFrame() ?? false;
    public bool EquipPrimaryPressedThisFrame => equipPrimaryAction?.WasPressedThisFrame() ?? false;
    public bool EquipSecondaryPressedThisFrame => equipSecondaryAction?.WasPressedThisFrame() ?? false;
    public bool EquipMeleePressedThisFrame => equipMeleeAction?.WasPressedThisFrame() ?? false;
    public bool QuickHealPressedThisFrame => quickHealAction?.WasPressedThisFrame() ?? false;
    public bool StopBleedPressedThisFrame => stopBleedAction?.WasPressedThisFrame() ?? false;
    public bool SplintPressedThisFrame => splintAction?.WasPressedThisFrame() ?? false;
    public bool PainkillerPressedThisFrame => painkillerAction?.WasPressedThisFrame() ?? false;
    public bool JumpPressedThisFrame => jumpAction?.WasPressedThisFrame() ?? false;
    public bool CrouchHeld => crouchAction?.IsPressed() ?? false;
    public bool WalkHeld => walkAction?.IsPressed() ?? false;
    public bool ToggleCursorPressedThisFrame => toggleCursorAction?.WasPressedThisFrame() ?? false;

    private void Awake()
    {
        EnsureActionsConfigured();
    }

    private void OnEnable()
    {
        EnsureActionsConfigured();
        EnableActions();
    }

    private void OnDisable()
    {
        DisableActions();
    }

    private void OnDestroy()
    {
        CleanupRuntimeActions();
    }

    public InputAction GetAction(string actionName)
    {
        if (runtimeActionMap != null)
        {
            InputAction action = runtimeActionMap.FindAction(actionName, false);
            if (action != null)
            {
                return action;
            }
        }

        return toggleCursorAction != null && toggleCursorAction.name == actionName
            ? toggleCursorAction
            : null;
    }

    public string GetBindingDisplayString(string actionName, int bindingIndex = 0)
    {
        InputAction action = GetAction(actionName);
        return action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count
            ? string.Empty
            : action.GetBindingDisplayString(bindingIndex);
    }

    public void SaveBindingOverrides()
    {
        if (runtimeActions == null || string.IsNullOrWhiteSpace(bindingSaveKey))
        {
            return;
        }

        PlayerPrefs.SetString(bindingSaveKey, runtimeActions.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }

    public void LoadBindingOverrides()
    {
        if (runtimeActions == null || string.IsNullOrWhiteSpace(bindingSaveKey) || !PlayerPrefs.HasKey(bindingSaveKey))
        {
            return;
        }

        runtimeActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(bindingSaveKey));
    }

    public void ResetBindingOverrides()
    {
        if (runtimeActions == null)
        {
            return;
        }

        runtimeActions.RemoveAllBindingOverrides();

        if (!string.IsNullOrWhiteSpace(bindingSaveKey))
        {
            PlayerPrefs.DeleteKey(bindingSaveKey);
        }
    }

    public InputActionRebindingExtensions.RebindingOperation StartInteractiveRebind(
        string actionName,
        int bindingIndex,
        Action onComplete = null,
        Action onCancel = null)
    {
        InputAction action = GetAction(actionName);
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            return null;
        }

        DisableActions();

        var rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(operation =>
            {
                operation.Dispose();
                EnableActions();
                onCancel?.Invoke();
            })
            .OnComplete(operation =>
            {
                operation.Dispose();
                SaveBindingOverrides();
                EnableActions();
                onComplete?.Invoke();
            });

        rebindOperation.Start();
        return rebindOperation;
    }

    private void EnsureActionsConfigured()
    {
        if (runtimeActionMap != null)
        {
            return;
        }

        TryAssignDefaultAssetInEditor();
        BuildActions();
    }

    private void BuildActions()
    {
        CleanupRuntimeActions();

        if (actionsAsset != null)
        {
            runtimeActions = Instantiate(actionsAsset);
            runtimeActionMap = runtimeActions.FindActionMap(actionMapName, true);
            moveAction = runtimeActionMap.FindAction(moveActionName, true);
            lookAction = runtimeActionMap.FindAction(lookActionName, true);
            shootAction = runtimeActionMap.FindAction(shootActionName, true);
            interactAction = EnsureAction(runtimeActionMap, interactActionName, "<Keyboard>/e");
            inventoryAction = EnsureAction(runtimeActionMap, inventoryActionName, "<Keyboard>/tab");
            reloadAction = EnsureAction(runtimeActionMap, reloadActionName, "<Keyboard>/r");
            toggleFireModeAction = EnsureAction(runtimeActionMap, toggleFireModeActionName, "<Keyboard>/b");
            equipPrimaryAction = EnsureAction(runtimeActionMap, equipPrimaryActionName, "<Keyboard>/1");
            equipSecondaryAction = EnsureAction(runtimeActionMap, equipSecondaryActionName, "<Keyboard>/2");
            equipMeleeAction = EnsureAction(runtimeActionMap, equipMeleeActionName, "<Keyboard>/3");
            quickHealAction = EnsureAction(runtimeActionMap, quickHealActionName, "<Keyboard>/4");
            stopBleedAction = EnsureAction(runtimeActionMap, stopBleedActionName, "<Keyboard>/5");
            splintAction = EnsureAction(runtimeActionMap, splintActionName, "<Keyboard>/6");
            painkillerAction = EnsureAction(runtimeActionMap, painkillerActionName, "<Keyboard>/7");
            jumpAction = runtimeActionMap.FindAction(jumpActionName, true);
            crouchAction = EnsureAction(runtimeActionMap, crouchActionName, "<Keyboard>/leftCtrl");
            walkAction = EnsureAction(runtimeActionMap, walkActionName, "<Keyboard>/leftShift");
        }
        else
        {
            fallbackActionMap = BuildFallbackActionMap();
            runtimeActionMap = fallbackActionMap;
        }

        toggleCursorAction = new InputAction("ToggleCursor", InputActionType.Button, toggleCursorBinding);
        LoadBindingOverrides();
    }

    private InputActionMap BuildFallbackActionMap()
    {
        var actionMap = new InputActionMap(actionMapName);

        moveAction = actionMap.AddAction(moveActionName, InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction = actionMap.AddAction(lookActionName, InputActionType.Value, "<Mouse>/delta");
        shootAction = actionMap.AddAction(shootActionName, InputActionType.Button, "<Mouse>/leftButton");
        interactAction = actionMap.AddAction(interactActionName, InputActionType.Button, "<Keyboard>/e");
        inventoryAction = actionMap.AddAction(inventoryActionName, InputActionType.Button, "<Keyboard>/tab");
        reloadAction = actionMap.AddAction(reloadActionName, InputActionType.Button, "<Keyboard>/r");
        toggleFireModeAction = actionMap.AddAction(toggleFireModeActionName, InputActionType.Button, "<Keyboard>/b");
        equipPrimaryAction = actionMap.AddAction(equipPrimaryActionName, InputActionType.Button, "<Keyboard>/1");
        equipSecondaryAction = actionMap.AddAction(equipSecondaryActionName, InputActionType.Button, "<Keyboard>/2");
        equipMeleeAction = actionMap.AddAction(equipMeleeActionName, InputActionType.Button, "<Keyboard>/3");
        quickHealAction = actionMap.AddAction(quickHealActionName, InputActionType.Button, "<Keyboard>/4");
        stopBleedAction = actionMap.AddAction(stopBleedActionName, InputActionType.Button, "<Keyboard>/5");
        splintAction = actionMap.AddAction(splintActionName, InputActionType.Button, "<Keyboard>/6");
        painkillerAction = actionMap.AddAction(painkillerActionName, InputActionType.Button, "<Keyboard>/7");
        jumpAction = actionMap.AddAction(jumpActionName, InputActionType.Button, "<Keyboard>/space");
        crouchAction = actionMap.AddAction(crouchActionName, InputActionType.Button, "<Keyboard>/leftCtrl");
        walkAction = actionMap.AddAction(walkActionName, InputActionType.Button, "<Keyboard>/leftShift");

        return actionMap;
    }

    private InputAction EnsureAction(InputActionMap actionMap, string actionName, string defaultBinding)
    {
        InputAction action = actionMap.FindAction(actionName, false) ?? actionMap.AddAction(actionName, InputActionType.Button);

        if (action.bindings.Count == 0 && !string.IsNullOrWhiteSpace(defaultBinding))
        {
            action.AddBinding(defaultBinding);
        }

        return action;
    }

    private void EnableActions()
    {
        runtimeActionMap?.Enable();
        toggleCursorAction?.Enable();
    }

    private void DisableActions()
    {
        runtimeActionMap?.Disable();
        toggleCursorAction?.Disable();
    }

    private void CleanupRuntimeActions()
    {
        toggleCursorAction?.Disable();
        toggleCursorAction?.Dispose();
        toggleCursorAction = null;

        runtimeActionMap = null;
        moveAction = null;
        lookAction = null;
        shootAction = null;
        interactAction = null;
        inventoryAction = null;
        reloadAction = null;
        toggleFireModeAction = null;
        equipPrimaryAction = null;
        equipSecondaryAction = null;
        equipMeleeAction = null;
        quickHealAction = null;
        stopBleedAction = null;
        splintAction = null;
        painkillerAction = null;
        jumpAction = null;
        crouchAction = null;
        walkAction = null;
        fallbackActionMap = null;

        if (runtimeActions == null)
        {
            return;
        }

        runtimeActions.Disable();

        if (Application.isPlaying)
        {
            Destroy(runtimeActions);
        }
        else
        {
            DestroyImmediate(runtimeActions);
        }

        runtimeActions = null;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        TryAssignDefaultAssetInEditor();
    }

    private void OnValidate()
    {
        TryAssignDefaultAssetInEditor();
    }

    private void TryAssignDefaultAssetInEditor()
    {
        if (actionsAsset == null)
        {
            actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(DefaultActionsAssetPath);
        }
    }
#endif
}
