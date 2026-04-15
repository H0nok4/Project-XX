using UnityEngine;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    public sealed class ProjectXXWeaponDurabilityBridge : MonoBehaviour
    {
        [SerializeField] [Range(0f, 1f)] private float normalizedDurability = 1f;

        public float NormalizedDurability => normalizedDurability;

        public void SetNormalizedDurability(float value)
        {
            normalizedDurability = Mathf.Clamp01(value);
        }
    }
}
