using UnityEngine;

namespace ProjectXX.Domain.Combat
{
    public static class ProjectXXFactionUtility
    {
        public static ProjectXXFactionMember ResolveMember(GameObject source)
        {
            if (source == null)
            {
                return null;
            }

            if (source.TryGetComponent(out ProjectXXFactionMember directMember))
            {
                return directMember;
            }

            ProjectXXFactionMember parentMember = source.GetComponentInParent<ProjectXXFactionMember>();
            if (parentMember != null)
            {
                return parentMember;
            }

            return source.GetComponentInChildren<ProjectXXFactionMember>();
        }

        public static ProjectXXFactionDisposition GetBaseDisposition(ProjectXXFaction source, ProjectXXFaction target)
        {
            if (source == target)
            {
                return ProjectXXFactionDisposition.Allied;
            }

            if ((source == ProjectXXFaction.Player && target == ProjectXXFaction.FriendlyNpc) ||
                (source == ProjectXXFaction.FriendlyNpc && target == ProjectXXFaction.Player))
            {
                return ProjectXXFactionDisposition.Allied;
            }

            if ((source == ProjectXXFaction.Player && target == ProjectXXFaction.Enemy) ||
                (source == ProjectXXFaction.Enemy && target == ProjectXXFaction.Player) ||
                (source == ProjectXXFaction.FriendlyNpc && target == ProjectXXFaction.Enemy) ||
                (source == ProjectXXFaction.Enemy && target == ProjectXXFaction.FriendlyNpc))
            {
                return ProjectXXFactionDisposition.Hostile;
            }

            return ProjectXXFactionDisposition.Neutral;
        }

        public static bool IsDefaultHostile(ProjectXXFaction observer, ProjectXXFaction target)
        {
            return observer switch
            {
                ProjectXXFaction.Player => target == ProjectXXFaction.Enemy,
                ProjectXXFaction.FriendlyNpc => target == ProjectXXFaction.Enemy,
                ProjectXXFaction.NeutralNpc => false,
                ProjectXXFaction.Enemy => target == ProjectXXFaction.Player || target == ProjectXXFaction.FriendlyNpc,
                _ => false
            };
        }

        public static bool CanApplyDamage(GameObject source, GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            ProjectXXFactionMember sourceMember = ResolveMember(source);
            ProjectXXFactionMember targetMember = ResolveMember(target);

            if (sourceMember == null || targetMember == null)
            {
                return true;
            }

            if (sourceMember == targetMember)
            {
                return false;
            }

            if (sourceMember.IsHostileTowards(targetMember) || targetMember.IsHostileTowards(sourceMember))
            {
                return true;
            }

            return GetBaseDisposition(sourceMember.Faction, targetMember.Faction) != ProjectXXFactionDisposition.Allied;
        }

        public static void RegisterIncomingDamage(GameObject target, GameObject source)
        {
            ProjectXXFactionMember sourceMember = ResolveMember(source);
            ProjectXXFactionMember targetMember = ResolveMember(target);

            if (sourceMember == null || targetMember == null || sourceMember == targetMember)
            {
                return;
            }

            targetMember.RegisterDamageSource(sourceMember);
        }
    }
}
