using Akila.FPSFramework;
using JUTPS;
using UnityEngine;

namespace ProjectXX.Bridges.JUTPS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(JUHealth))]
    [RequireComponent(typeof(Damageable))]
    public sealed class JutpsHealthProxy : MonoBehaviour
    {
        private void Awake()
        {
            enabled = false;
        }
    }
}
