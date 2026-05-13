using Akila.FPSFramework;
using JUTPS;
using ProjectXX.Domain.Combat;
using UnityEngine;
using UnityEngine.Events;

namespace ProjectXX.Bridges.JUTPS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXCombatant))]
    public sealed class JutpsEnemyDamageableAdapter : MonoBehaviour, IDamageable
    {
        private readonly UnityEvent onDeath = new UnityEvent();

        private ProjectXXCombatant combatant;

        public bool isDamagableDisabled { get; set; }
        public bool allowDamageableEffects { get; set; } = true;
        public bool DeadConfirmed
        {
            get => combatant != null && combatant.IsDead;
            set
            {
            }
        }

        public GameObject DamageSource { get; set; }
        public UnityEvent OnDeath => onDeath;

        public float Health
        {
            get => combatant != null ? combatant.CurrentHealth : 0f;
            set
            {
                if (combatant == null)
                {
                    return;
                }

                float resolvedMaxHealth = Mathf.Max(1f, Mathf.Max(combatant.MaxHealth, value));
                combatant.Configure(resolvedMaxHealth, value);
            }
        }

        private void Awake()
        {
            combatant = GetComponent<ProjectXXCombatant>();
        }

        private void OnEnable()
        {
            if (combatant != null)
            {
                combatant.Died += HandleCombatantDeath;
            }
        }

        private void OnDisable()
        {
            if (combatant != null)
            {
                combatant.Died -= HandleCombatantDeath;
            }
        }

        public void Damage(float amount, GameObject damageSource)
        {
            if (isDamagableDisabled || combatant == null)
            {
                return;
            }

            DamageSource = damageSource;
            combatant.TryApplyDamage(amount, damageSource);
        }

        private void HandleCombatantDeath(ProjectXXCombatant _, GameObject __)
        {
            onDeath.Invoke();
        }
    }
}
