using UnityEngine;
using UnityEngine.InputSystem;

namespace JU.Editor
{
    /// <summary>
    /// Runtime-safe focus utility used by JU UI/input flows.
    /// </summary>
    public static class JUGameFocusUtility
    {
        private sealed class GameFocusTracker : MonoBehaviour
        {
            private const float FocusDelay = 0.3f;
            private float _focusDelayTimer;

            public bool IsFocused { get; private set; }

            private void Awake()
            {
                hideFlags = HideFlags.HideAndDontSave;
                IsFocused = CheckGameFocus();
            }

            private void Update()
            {
                bool focused = CheckGameFocus();
                if (!focused)
                {
                    _focusDelayTimer = 0f;
                    IsFocused = false;
                }
                else
                {
                    if (_focusDelayTimer < FocusDelay)
                    {
                        _focusDelayTimer += Time.unscaledDeltaTime;
                        if (_focusDelayTimer >= FocusDelay)
                            IsFocused = true;
                    }
                }
            }

            private static bool CheckGameFocus()
            {
                if (!Application.isFocused)
                    return false;

                if (Mouse.current == null)
                    return true;

                Vector2 mousePos = Mouse.current.position.value;
                Vector2 screenSize = new Vector2(Screen.width, Screen.height);

                if (mousePos.x < 0 || mousePos.y < 0)
                    return false;

                if (mousePos.x > screenSize.x || mousePos.y > screenSize.y)
                    return false;

                return true;
            }
        }

        private static GameFocusTracker _focusTrackerInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _focusTrackerInstance = null;
        }

        public static bool IsGameFocused
        {
            get
            {
                EnsureFocusTracker();
                return _focusTrackerInstance.IsFocused;
            }
        }

        private static void EnsureFocusTracker()
        {
            if (_focusTrackerInstance != null)
                return;

            GameObject trackerObject = new GameObject("JU Game Focus Tracker");
            trackerObject.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(trackerObject);
            _focusTrackerInstance = trackerObject.AddComponent<GameFocusTracker>();
        }
    }
}
