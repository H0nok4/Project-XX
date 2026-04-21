using Akila.FPSFramework;
using JUTPS;
using ProjectXX.Domain.Combat;
using UnityEngine;

namespace ProjectXX.Bridges.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXCombatant))]
    public sealed class ProjectXXCombatantSync : MonoBehaviour
    {
        [SerializeField] private ProjectXXCombatant combatant;
        [SerializeField] private Damageable damageable;
        [SerializeField] private JUHealth juHealth;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();

            if (combatant != null)
            {
                combatant.StateChanged += HandleStateChanged;
            }

            SyncNow();
        }

        private void OnDisable()
        {
            if (combatant != null)
            {
                combatant.StateChanged -= HandleStateChanged;
            }
        }

        [ContextMenu("Sync Now")]
        public void SyncNow()
        {
            CacheReferences();

            if (combatant == null)
            {
                return;
            }

            if (damageable != null)
            {
                damageable.maxHealth = combatant.MaxHealth;
                damageable.health = combatant.CurrentHealth;
                damageable.DamageSource = combatant.LastDamageSource;

                if (!combatant.IsDead)
                {
                    damageable.DeadConfirmed = false;
                }
            }

            if (juHealth != null)
            {
                juHealth.MaxHealth = combatant.MaxHealth;
                juHealth.Health = combatant.CurrentHealth;
                juHealth.CheckHealthState();
            }
        }

        private void HandleStateChanged(ProjectXXCombatant changedCombatant)
        {
            if (changedCombatant != combatant)
            {
                return;
            }

            SyncNow();
        }

        private void CacheReferences()
        {
            combatant = GetComponent<ProjectXXCombatant>();
            damageable = GetComponent<Damageable>();
            juHealth = GetComponent<JUHealth>();
        }
    }
}
