using Akila.FPSFramework;
using JUTPS;
using ProjectXX.Domain.Combat;
using UnityEngine;
using UnityEngine.Events;

namespace ProjectXX.Bridges.JUTPS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(JUHealth))]
    [RequireComponent(typeof(ProjectXXCombatant))]
    public sealed class JutpsEnemyDamageableAdapter : MonoBehaviour, IDamageable
    {
        private readonly UnityEvent onDeath = new UnityEvent();

        private JUHealth juHealth;
        private ProjectXXCombatant combatant;

        public bool isDamagableDisabled { get; set; }
        public bool allowDamageableEffects { get; set; } = true;
        public bool DeadConfirmed
        {
            get => combatant != null ? combatant.IsDead : juHealth != null && juHealth.IsDead;
            set
            {
            }
        }

        public GameObject DamageSource { get; set; }
        public UnityEvent OnDeath => onDeath;

        public float Health
        {
            get => combatant != null ? combatant.CurrentHealth : juHealth != null ? juHealth.Health : 0f;
            set
            {
                if (combatant != null)
                {
                    float resolvedMaxHealth = Mathf.Max(1f, Mathf.Max(combatant.MaxHealth, value));
                    combatant.Configure(resolvedMaxHealth, value);
                    return;
                }

                if (juHealth == null)
                {
                    return;
                }

                juHealth.MaxHealth = Mathf.Max(1f, Mathf.Max(juHealth.MaxHealth, value));
                juHealth.Health = Mathf.Clamp(value, 0f, juHealth.MaxHealth);
                juHealth.CheckHealthState();
            }
        }

        private void Awake()
        {
            juHealth = GetComponent<JUHealth>();
            combatant = GetComponent<ProjectXXCombatant>();
        }

        private void OnEnable()
        {
            if (combatant != null)
            {
                combatant.Died += HandleCombatantDeath;
            }
            else if (juHealth != null)
            {
                juHealth.OnDeath.AddListener(HandleDeath);
            }
        }

        private void OnDisable()
        {
            if (combatant != null)
            {
                combatant.Died -= HandleCombatantDeath;
            }
            else if (juHealth != null)
            {
                juHealth.OnDeath.RemoveListener(HandleDeath);
            }
        }

        public void Damage(float amount, GameObject damageSource)
        {
            if (isDamagableDisabled)
            {
                return;
            }

            DamageSource = damageSource;

            if (combatant != null)
            {
                combatant.TryApplyDamage(amount, damageSource);
                return;
            }

            if (juHealth == null || juHealth.IsDead)
            {
                return;
            }

            juHealth.DoDamage(new JUHealth.DamageInfo
            {
                Damage = amount,
                HitPosition = transform.position,
                HitDirection = (transform.position - (damageSource != null ? damageSource.transform.position : transform.position)).normalized,
                HitOriginPosition = damageSource != null ? damageSource.transform.position : transform.position,
                HitOwner = damageSource
            });
        }

        private void HandleDeath()
        {
            onDeath.Invoke();
        }

        private void HandleCombatantDeath(ProjectXXCombatant _, GameObject __)
        {
            HandleDeath();
        }
    }
}
