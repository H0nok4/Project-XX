using ProjectXX.Bridges.FPSFramework;
using ProjectXX.Domain.Raid;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectXX.Presentation.Raid
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class ProjectXXExtractionPoint : MonoBehaviour
    {
        [SerializeField] private RaidSessionRuntime sessionRuntime;

        private void Reset()
        {
            if (TryGetComponent(out Collider triggerCollider))
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void Awake()
        {
            if (TryGetComponent(out Collider triggerCollider))
            {
                triggerCollider.isTrigger = true;
            }
        }

        public void Configure(RaidSessionRuntime runtime)
        {
            sessionRuntime = runtime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (TryGetPlayer(other, out _))
            {
                GetRuntime().SetPlayerInsideExtractionZone(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (TryGetPlayer(other, out _))
            {
                GetRuntime().SetPlayerInsideExtractionZone(false);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!TryGetPlayer(other, out _))
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                GetRuntime().TryExtract();
            }
        }

        private RaidSessionRuntime GetRuntime()
        {
            if (sessionRuntime == null)
            {
                sessionRuntime = FindFirstObjectByType<RaidSessionRuntime>();
            }

            return sessionRuntime;
        }

        private static bool TryGetPlayer(Collider other, out ProjectXXPlayerFacade playerFacade)
        {
            playerFacade = other.GetComponentInParent<ProjectXXPlayerFacade>();
            return playerFacade != null;
        }
    }
}
