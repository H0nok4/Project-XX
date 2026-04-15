using UnityEngine;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    public sealed class ProjectXXCharacterBuffBridge : MonoBehaviour
    {
        [SerializeField] private float outgoingDamageMultiplier = 1f;
        [SerializeField] private float incomingDamageMultiplier = 1f;

        public float OutgoingDamageMultiplier => Mathf.Max(0.1f, outgoingDamageMultiplier);
        public float IncomingDamageMultiplier => Mathf.Max(0.1f, incomingDamageMultiplier);
    }
}
