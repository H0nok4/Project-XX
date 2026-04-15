using UnityEngine;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    public sealed class ProjectXXCharacterStatBridge : MonoBehaviour
    {
        [SerializeField] private float healthBonus;

        public float ResolveMaxHealth(float baseMaxHealth)
        {
            return Mathf.Max(1f, baseMaxHealth + healthBonus);
        }
    }
}
