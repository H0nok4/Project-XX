using ProjectXX.Domain.Combat;
using UnityEngine;

namespace ProjectXX.Bridges.JUTPS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXFactionMember))]
    public sealed class ProjectXXJutpsFactionTargetFilter : MonoBehaviour
    {
        [SerializeField] private bool requireFactionMember = true;

        private ProjectXXFactionMember factionMember;

        private void Awake()
        {
            factionMember = GetComponent<ProjectXXFactionMember>();
        }

        public bool IsValidTarget(Collider collider)
        {
            return collider != null && IsValidTarget(collider.gameObject);
        }

        public bool IsValidTarget(GameObject targetObject)
        {
            factionMember ??= GetComponent<ProjectXXFactionMember>();
            if (factionMember == null || targetObject == null)
            {
                return false;
            }

            ProjectXXFactionMember targetMember = ProjectXXFactionUtility.ResolveMember(targetObject);
            if (targetMember == null)
            {
                return !requireFactionMember;
            }

            if (targetMember == factionMember)
            {
                return false;
            }

            return factionMember.IsHostileTowards(targetMember);
        }
    }
}
