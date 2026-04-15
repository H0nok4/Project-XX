using Akila.FPSFramework;
using UnityEngine;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    public sealed class ProjectXXAmmoResolver : MonoBehaviour
    {
        public int ResolveMagazineAmmo(Firearm firearm)
        {
            return firearm != null ? Mathf.Max(0, firearm.remainingAmmoCount) : 0;
        }

        public int ResolveReserveAmmo(Firearm firearm)
        {
            return firearm != null ? Mathf.Max(0, firearm.remainingAmmoTypeCount) : 0;
        }
    }
}
