using Akila.FPSFramework;
using ProjectXX.Foundation;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    public sealed class ProjectXXFirstPersonViewBridge : MonoBehaviour
    {
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

            int fpsObjectLayer = -1;
            var compatibilitySettings = ProjectXXCompatibilitySettingsProvider.GetOrDefault();
            if (compatibilitySettings != null)
            {
                compatibilitySettings.TryGetFpsObjectLayer(out fpsObjectLayer);
            }

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
    }
}
