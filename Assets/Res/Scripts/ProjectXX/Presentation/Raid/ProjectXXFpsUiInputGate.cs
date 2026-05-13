using Akila.FPSFramework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectXX.Presentation.Raid
{
    [DefaultExecutionOrder(-940)]
    [DisallowMultipleComponent]
    public sealed class ProjectXXFpsUiInputGate : MonoBehaviour
    {
        private static ProjectXXFpsUiInputGate instance;

        private bool blockingInput;
        private bool previousInputActive;
        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            GetOrCreate();
        }

        public static ProjectXXFpsUiInputGate GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<ProjectXXFpsUiInputGate>();
            if (instance != null)
            {
                return instance;
            }

            GameObject gateObject = new GameObject("ProjectXX FPS UI Input Gate");
            DontDestroyOnLoad(gateObject);
            instance = gateObject.AddComponent<ProjectXXFpsUiInputGate>();
            return instance;
        }

        public void RefreshNow()
        {
            RefreshInputState();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            RefreshInputState();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                RestoreInputState(true);
                instance = null;
            }
        }

        private void RefreshInputState()
        {
            bool shouldBlock = UiWindowService.TryGetExisting(out UiWindowService windowService) &&
                               windowService.HasVisibleManagedElement;

            if (shouldBlock)
            {
                ApplyInputBlock();
                return;
            }

            RestoreInputState(false);
        }

        private void ApplyInputBlock()
        {
            if (!blockingInput)
            {
                previousInputActive = FPSFrameworkCore.IsInputActive;
                previousCursorLockMode = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                blockingInput = true;
            }

            FPSFrameworkCore.IsInputActive = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RestoreInputState(bool force)
        {
            if (!blockingInput)
            {
                return;
            }

            if (!force && IsAnyPointerButtonPressed())
            {
                FPSFrameworkCore.IsInputActive = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            FPSFrameworkCore.IsInputActive = previousInputActive;
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
            blockingInput = false;
        }

        private static bool IsAnyPointerButtonPressed()
        {
            Mouse mouse = Mouse.current;
            return mouse != null &&
                   (mouse.leftButton.isPressed ||
                    mouse.rightButton.isPressed ||
                    mouse.middleButton.isPressed ||
                    mouse.forwardButton?.isPressed == true ||
                    mouse.backButton?.isPressed == true);
        }
    }
}
