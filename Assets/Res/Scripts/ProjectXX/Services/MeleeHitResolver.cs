using UnityEngine;

namespace ProjectXX.Services
{
    public static class MeleeHitResolver
    {
        public static bool CanResolveHit(Vector3 sourcePosition, Vector3 targetPosition, float range)
        {
            return Vector3.Distance(sourcePosition, targetPosition) <= Mathf.Max(0.1f, range);
        }
    }
}
