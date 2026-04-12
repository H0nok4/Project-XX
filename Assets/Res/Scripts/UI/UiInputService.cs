using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-970)]
[DisallowMultipleComponent]
public sealed class UiInputService : MonoBehaviour
{
    private static UiInputService instance;

    [SerializeField] private bool routeSubmitToUiWindowService = true;
    [SerializeField] private bool routeCancelToUiWindowService = true;
    [SerializeField] private bool supportKeyboard = true;
    [SerializeField] private bool supportGamepad = true;

    public static UiInputService Instance => GetOrCreate();

    public event Action SubmitPressed;
    public event Action CancelPressed;
    public event Action UnhandledSubmitPressed;
    public event Action UnhandledCancelPressed;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static UiInputService GetOrCreate()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<UiInputService>();
        if (instance != null)
        {
            return instance;
        }

        GameObject serviceObject = new GameObject("UiInputService");
        instance = serviceObject.AddComponent<UiInputService>();
        return instance;
    }

    private void Update()
    {
        if (WasSubmitPressedThisFrame())
        {
            HandleSubmitPressed();
        }

        if (WasCancelPressedThisFrame())
        {
            HandleCancelPressed();
        }
    }

    private bool WasSubmitPressedThisFrame()
    {
        bool keyboardPressed = supportKeyboard && Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
        bool gamepadPressed = supportGamepad && Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame;
        return keyboardPressed || gamepadPressed;
    }

    private bool WasCancelPressedThisFrame()
    {
        bool keyboardPressed = supportKeyboard && Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame;
        bool gamepadPressed = supportGamepad && Gamepad.current != null &&
            Gamepad.current.buttonEast.wasPressedThisFrame;
        return keyboardPressed || gamepadPressed;
    }

    private void HandleSubmitPressed()
    {
        SubmitPressed?.Invoke();
        bool handled = routeSubmitToUiWindowService && UiWindowService.GetOrCreate().TryHandleSubmit();
        if (!handled)
        {
            UnhandledSubmitPressed?.Invoke();
        }
    }

    private void HandleCancelPressed()
    {
        CancelPressed?.Invoke();
        bool handled = routeCancelToUiWindowService && UiWindowService.GetOrCreate().TryHandleCancel();
        if (!handled)
        {
            UnhandledCancelPressed?.Invoke();
        }
    }
}
