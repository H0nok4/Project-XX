using Akila.FPSFramework;
using JUTPS;
using JUTPS.GameSettings;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    public sealed class ProjectXXFirstPersonViewBridge : MonoBehaviour
    {
        [SerializeField] private bool disableConflictingSceneObjects = true;
        [SerializeField] private bool disableFrameworkPlayerCard = true;
        [SerializeField] private bool enforceFirstPersonCameraStack = true;
        [SerializeField] private string mainCameraName = "Main Camera";
        [SerializeField] private string overlayCameraName = "Overlay Camera";

        private bool sanitizedScenePresentation;

        private void LateUpdate()
        {
            if (sanitizedScenePresentation)
            {
                return;
            }

            SanitizeScenePresentation();
            sanitizedScenePresentation = true;
        }

        private void SanitizeScenePresentation()
        {
            if (enforceFirstPersonCameraStack)
            {
                EnsureFirstPersonCameraStack();
            }

            if (disableConflictingSceneObjects)
            {
                DisableObjectByName("JUTPS Default User Interface");
                DisableObjectByName("ThirdPerson Camera Controller");

                foreach (JUPauseGame pauseGame in FindObjectsByType<JUPauseGame>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    pauseGame.enabled = false;
                }

                foreach (JUGameSettings gameSettings in FindObjectsByType<JUGameSettings>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    gameSettings.enabled = false;
                }

                foreach (JUGameManager gameManager in FindObjectsByType<JUGameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    gameManager.enabled = false;
                }
            }

            if (disableFrameworkPlayerCard && UIManager.Instance != null && UIManager.Instance.PlayerCard != null)
            {
                UIManager.Instance.PlayerCard.gameObject.SetActive(false);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void EnsureFirstPersonCameraStack()
        {
            Camera mainCamera = FindCamera(mainCameraName);
            Camera overlayCamera = FindCamera(overlayCameraName);
            if (mainCamera == null || overlayCamera == null)
            {
                return;
            }

            UniversalAdditionalCameraData mainCameraData = mainCamera.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData overlayCameraData = overlayCamera.GetUniversalAdditionalCameraData();
            if (mainCameraData == null || overlayCameraData == null)
            {
                return;
            }

            mainCamera.depth = 0f;
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            mainCameraData.renderType = CameraRenderType.Base;

            overlayCamera.depth = 1f;
            overlayCamera.clearFlags = CameraClearFlags.Depth;
            overlayCameraData.renderType = CameraRenderType.Overlay;

            int fpsObjectLayer = LayerMask.NameToLayer("FPS Object");
            if (fpsObjectLayer >= 0)
            {
                int overlayMask = 1 << fpsObjectLayer;
                overlayCamera.cullingMask = overlayMask;
                mainCamera.cullingMask &= ~overlayMask;
            }

            mainCameraData.cameraStack.Clear();
            if (!mainCameraData.cameraStack.Contains(overlayCamera))
            {
                mainCameraData.cameraStack.Add(overlayCamera);
            }
        }

        private Camera FindCamera(string cameraName)
        {
            Camera[] cameras = GetComponentsInChildren<Camera>(true);
            foreach (Camera cameraComponent in cameras)
            {
                if (cameraComponent.name == cameraName)
                {
                    return cameraComponent;
                }
            }

            return null;
        }

        private static void DisableObjectByName(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
            {
                target.SetActive(false);
            }
        }
    }
}
