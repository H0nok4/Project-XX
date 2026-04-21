using UnityEngine;

namespace ProjectXX.Domain.Combat
{
    public interface IProjectXXTargetFilter
    {
        bool IsValidTarget(Collider collider);
        bool IsValidTarget(GameObject targetObject);
    }
}
