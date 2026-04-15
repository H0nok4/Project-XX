using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectXX.Domain.Combat
{
    [DisallowMultipleComponent]
    public sealed class ProjectXXFactionMember : MonoBehaviour
    {
        [SerializeField] private ProjectXXFaction faction = ProjectXXFaction.Enemy;
        [SerializeField] private ProjectXXFactionRetaliationMode retaliationMode = ProjectXXFactionRetaliationMode.DamageSourceFaction;
        [SerializeField] private ProjectXXFaction[] extraHostileFactions = Array.Empty<ProjectXXFaction>();

        private readonly HashSet<ProjectXXFaction> runtimeHostileFactions = new HashSet<ProjectXXFaction>();
        private readonly HashSet<ProjectXXFaction> extraHostileLookup = new HashSet<ProjectXXFaction>();

        public ProjectXXFaction Faction => faction;
        public ProjectXXFactionRetaliationMode RetaliationMode => retaliationMode;

        public event Action<ProjectXXFaction> OnFactionBecameHostile;

        private void Awake()
        {
            RebuildLookup();
        }

        private void OnValidate()
        {
            RebuildLookup();
        }

        public void SetFaction(ProjectXXFaction value, bool clearRuntimeHostility = false)
        {
            faction = value;

            if (clearRuntimeHostility)
            {
                runtimeHostileFactions.Clear();
            }
        }

        public bool IsHostileTowards(ProjectXXFactionMember other)
        {
            if (other == null || other == this)
            {
                return false;
            }

            return IsHostileTowards(other.Faction);
        }

        public bool IsHostileTowards(ProjectXXFaction otherFaction)
        {
            return runtimeHostileFactions.Contains(otherFaction) ||
                   extraHostileLookup.Contains(otherFaction) ||
                   ProjectXXFactionUtility.IsDefaultHostile(faction, otherFaction);
        }

        public void RegisterDamageSource(ProjectXXFactionMember sourceMember)
        {
            if (sourceMember == null || sourceMember == this)
            {
                return;
            }

            if (retaliationMode != ProjectXXFactionRetaliationMode.DamageSourceFaction)
            {
                return;
            }

            SetRuntimeHostility(sourceMember.Faction);
        }

        public void ClearRuntimeHostility()
        {
            runtimeHostileFactions.Clear();
        }

        private void SetRuntimeHostility(ProjectXXFaction targetFaction)
        {
            if (targetFaction == faction)
            {
                return;
            }

            if (runtimeHostileFactions.Add(targetFaction))
            {
                OnFactionBecameHostile?.Invoke(targetFaction);
            }
        }

        private void RebuildLookup()
        {
            extraHostileLookup.Clear();

            if (extraHostileFactions == null)
            {
                return;
            }

            for (int i = 0; i < extraHostileFactions.Length; i++)
            {
                extraHostileLookup.Add(extraHostileFactions[i]);
            }
        }
    }
}
