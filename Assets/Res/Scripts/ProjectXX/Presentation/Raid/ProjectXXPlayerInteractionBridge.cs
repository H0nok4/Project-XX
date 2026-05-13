using Akila.FPSFramework;
using ProjectXX.Bridges.FPSFramework;
using ProjectXX.Domain.Interaction;
using ProjectXX.Domain.Inventory;
using ProjectXX.Domain.Raid;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectXX.Presentation.Raid
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXPlayerFacade))]
    public sealed class ProjectXXPlayerInteractionBridge : MonoBehaviour
    {
        [SerializeField] private RaidSessionRuntime sessionRuntime;
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
        [SerializeField] private float resultMessageHoldSeconds = 1.25f;
        [SerializeField] private ProjectXXLootWindowController lootWindowController;

        private ProjectXXPlayerFacade playerFacade;
        private Camera cachedCamera;
        private float resultMessageHoldUntil;

        private void Awake()
        {
            RefreshReferences();
        }

        private void OnDisable()
        {
            sessionRuntime?.ClearInteractionPrompt();
        }

        public void Configure(RaidSessionRuntime runtime)
        {
            sessionRuntime = runtime;
            RefreshReferences();
        }

        public void Configure(RaidSessionRuntime runtime, ProjectXXLootWindowController lootWindow)
        {
            sessionRuntime = runtime;
            lootWindowController = lootWindow;
            RefreshReferences();
        }

        public void RefreshReferences()
        {
            if (playerFacade == null)
            {
                playerFacade = GetComponent<ProjectXXPlayerFacade>();
            }

            playerFacade.RefreshReferences();

            if (lootWindowController == null)
            {
                lootWindowController = GetComponent<ProjectXXLootWindowController>();
            }
        }

        private void Update()
        {
            if (sessionRuntime == null)
            {
                return;
            }

            if (IsAnyManagedUiOpen())
            {
                return;
            }

            Camera playerCamera = ResolveCamera();
            if (playerCamera == null)
            {
                sessionRuntime.ClearInteractionPrompt();
                return;
            }

            IProjectXXInteractable interactable = FindFocusedInteractable(playerCamera);
            bool interactPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

            if (interactable != null && interactable.CanInteract(gameObject))
            {
                if (interactPressed)
                {
                    ProjectXXInteractionResult result = interactable.Interact(gameObject);
                    if (result.Succeeded && TryOpenLootWindow(interactable))
                    {
                        resultMessageHoldUntil = Time.time + resultMessageHoldSeconds;
                        return;
                    }

                    sessionRuntime.SetInteractionPrompt(result.Message);
                    resultMessageHoldUntil = Time.time + resultMessageHoldSeconds;
                    return;
                }

                if (Time.time >= resultMessageHoldUntil)
                {
                    sessionRuntime.SetInteractionPrompt(interactable.InteractionPrompt);
                }

                return;
            }

            if (Time.time >= resultMessageHoldUntil)
            {
                sessionRuntime.ClearInteractionPrompt();
            }
        }

        private Camera ResolveCamera()
        {
            if (cachedCamera != null)
            {
                return cachedCamera;
            }

            CameraManager cameraManager = playerFacade != null ? playerFacade.CameraManager : null;
            if (cameraManager != null && cameraManager.mainCamera != null)
            {
                cachedCamera = cameraManager.mainCamera;
                return cachedCamera;
            }

            cachedCamera = Camera.main;
            return cachedCamera;
        }

        private IProjectXXInteractable FindFocusedInteractable(Camera playerCamera)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionMask, triggerInteraction))
            {
                return null;
            }

            return ResolveInteractable(hit.collider);
        }

        private bool TryOpenLootWindow(IProjectXXInteractable interactable)
        {
            if (lootWindowController == null || interactable is not ProjectXXContainerInteractable containerInteractable)
            {
                return false;
            }

            ProjectXXContainerRuntime containerRuntime = containerInteractable.ContainerRuntime;
            if (containerRuntime == null)
            {
                return false;
            }

            lootWindowController.Open(containerRuntime);
            return true;
        }

        private static bool IsAnyManagedUiOpen()
        {
            return UiWindowService.TryGetExisting(out UiWindowService windowService) &&
                   windowService.HasVisibleManagedElement;
        }

        private static IProjectXXInteractable ResolveInteractable(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return null;
            }

            MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IProjectXXInteractable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }
    }
}
