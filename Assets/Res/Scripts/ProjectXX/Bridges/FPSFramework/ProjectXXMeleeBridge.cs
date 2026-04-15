using Akila.FPSFramework;
using UnityEngine;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXPlayerFacade))]
    public sealed class ProjectXXMeleeBridge : MonoBehaviour
    {
        private ProjectXXPlayerFacade playerFacade;

        public bool HasMeleeWeaponEquipped
        {
            get
            {
                if (playerFacade == null || playerFacade.Inventory == null)
                {
                    return false;
                }

                return playerFacade.Inventory.currentItem is MeleeWeapon;
            }
        }

        private void Awake()
        {
            playerFacade = GetComponent<ProjectXXPlayerFacade>();
        }
    }
}
