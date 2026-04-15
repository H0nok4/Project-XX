using UnityEngine;

namespace ProjectXX.Services
{
    public static class MeleeStaggerService
    {
        public static Vector3 ResolveKnockback(Vector3 sourcePosition, Vector3 targetPosition, float strength)
        {
            Vector3 direction = (targetPosition - sourcePosition).normalized;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.forward;
            }

            return direction * Mathf.Max(0f, strength);
        }
    }
}
