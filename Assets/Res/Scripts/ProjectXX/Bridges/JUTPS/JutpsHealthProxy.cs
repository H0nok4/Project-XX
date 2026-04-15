using Akila.FPSFramework;
using JUTPS;
using UnityEngine;
using UnityEngine.Events;

namespace ProjectXX.Bridges.JUTPS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(JUHealth))]
    [RequireComponent(typeof(Damageable))]
    public sealed class JutpsHealthProxy : MonoBehaviour
    {
        private JUHealth juHealth;
        private Damageable damageable;

        private void Awake()
        {
            juHealth = GetComponent<JUHealth>();
            damageable = GetComponent<Damageable>();

            juHealth.OnDeath ??= new UnityEvent();
            juHealth.OnDamaged ??= new JUHealth.DamageEvent();

            juHealth.BloodScreenEffect = false;
            juHealth.MaxHealth = Mathf.Max(1f, damageable.maxHealth > 0f ? damageable.maxHealth : damageable.health);
            juHealth.Health = Mathf.Clamp(damageable.health, 0f, juHealth.MaxHealth);
            juHealth.CheckHealthState();
        }

        private void OnEnable()
        {
            juHealth ??= GetComponent<JUHealth>();
            damageable ??= GetComponent<Damageable>();

            if (juHealth == null || damageable == null)
            {
                return;
            }

            juHealth.OnDeath ??= new UnityEvent();
            juHealth.OnDamaged ??= new JUHealth.DamageEvent();

            juHealth.OnDamaged.AddListener(HandleJutpsDamaged);
            juHealth.OnDeath.AddListener(HandleJutpsDeath);
        }

        private void OnDisable()
        {
            juHealth ??= GetComponent<JUHealth>();
            if (juHealth == null)
            {
                return;
            }

            juHealth.OnDamaged.RemoveListener(HandleJutpsDamaged);
            juHealth.OnDeath.RemoveListener(HandleJutpsDeath);
        }

        private void LateUpdate()
        {
            if (juHealth == null || damageable == null)
            {
                return;
            }

            float resolvedMaxHealth = Mathf.Max(1f, damageable.maxHealth > 0f ? damageable.maxHealth : damageable.health);
            if (!Mathf.Approximately(juHealth.MaxHealth, resolvedMaxHealth))
            {
                juHealth.MaxHealth = resolvedMaxHealth;
            }

            if (!Mathf.Approximately(juHealth.Health, damageable.health))
            {
                juHealth.Health = Mathf.Clamp(damageable.health, 0f, juHealth.MaxHealth);
                juHealth.CheckHealthState();
            }
        }

        private void HandleJutpsDamaged(JUHealth.DamageInfo damageInfo)
        {
            if (damageable == null || damageInfo.Damage <= 0f)
            {
                return;
            }

            damageable.Damage(damageInfo.Damage, damageInfo.HitOwner);

            if (damageable.health <= 0f && !damageable.DeadConfirmed)
            {
                damageable.OnDeath?.Invoke();
            }

            ProjectXX.Bridges.FPSFramework.ProjectXXDamageBridge damageBridge =
                GetComponent<ProjectXX.Bridges.FPSFramework.ProjectXXDamageBridge>();
            damageBridge?.ForceSync();
        }

        private void HandleJutpsDeath()
        {
            if (damageable == null)
            {
                return;
            }

            damageable.health = 0f;
        }
    }
}
