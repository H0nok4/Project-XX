using Akila.FPSFramework;
using JUTPS;
using UnityEngine;
using UnityEngine.Events;

namespace ProjectXX.Bridges.JUTPS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(JUHealth))]
    public sealed class JutpsEnemyDamageableAdapter : MonoBehaviour, IDamageable
    {
        private readonly UnityEvent onDeath = new UnityEvent();

        private JUHealth juHealth;

        public bool isDamagableDisabled { get; set; }
        public bool allowDamageableEffects { get; set; } = true;
        public bool DeadConfirmed
        {
            get => juHealth != null && juHealth.IsDead;
            set
            {
            }
        }

        public GameObject DamageSource { get; set; }
        public UnityEvent OnDeath => onDeath;

        public float Health
        {
            get => juHealth != null ? juHealth.Health : 0f;
            set
            {
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
        }

        private void OnEnable()
        {
            if (juHealth != null)
            {
                juHealth.OnDeath.AddListener(HandleDeath);
            }
        }

        private void OnDisable()
        {
            if (juHealth != null)
            {
                juHealth.OnDeath.RemoveListener(HandleDeath);
            }
        }

        public void Damage(float amount, GameObject damageSource)
        {
            if (isDamagableDisabled || juHealth == null || juHealth.IsDead)
            {
                return;
            }

            DamageSource = damageSource;
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
    }
}
