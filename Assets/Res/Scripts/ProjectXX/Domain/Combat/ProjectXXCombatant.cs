using System;
using UnityEngine;

namespace ProjectXX.Domain.Combat
{
    [AddComponentMenu("ProjectXX/Combat/ProjectXX Combatant")]
    [DisallowMultipleComponent]
    public sealed class ProjectXXCombatant : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private bool dead;
        [SerializeField] private GameObject lastDamageSource;

        public event Action<ProjectXXCombatant> StateChanged;
        public event Action<ProjectXXCombatant, float, GameObject> Damaged;
        public event Action<ProjectXXCombatant, GameObject> Died;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => dead;
        public GameObject LastDamageSource => lastDamageSource;
        public float MissingHealth => Mathf.Max(0f, maxHealth - currentHealth);
        public float HealthRatio => maxHealth > 0f ? currentHealth / maxHealth : 0f;

        private void Awake()
        {
            NormalizeState();
        }

        public void Configure(float newMaxHealth, float newCurrentHealth, bool clearLastDamageSource = false)
        {
            ApplyState(newMaxHealth, newCurrentHealth, clearLastDamageSource ? null : lastDamageSource);
        }

        public void RestoreFull(bool clearLastDamageSource = false)
        {
            ApplyState(maxHealth, maxHealth, clearLastDamageSource ? null : lastDamageSource);
        }

        public bool TrySetCurrentHealth(float newCurrentHealth, bool clearLastDamageSource = false)
        {
            return ApplyState(maxHealth, newCurrentHealth, clearLastDamageSource ? null : lastDamageSource);
        }

        public bool TryApplyHealing(float amount, bool clearLastDamageSource = false)
        {
            if (amount <= 0f || dead || currentHealth >= maxHealth)
            {
                return false;
            }

            return ApplyState(maxHealth, currentHealth + amount, clearLastDamageSource ? null : lastDamageSource);
        }

        public bool TryApplyDamage(float amount, GameObject damageSource)
        {
            if (amount <= 0f || dead)
            {
                return false;
            }

            if (!ProjectXXFactionUtility.CanApplyDamage(damageSource, gameObject))
            {
                return false;
            }

            float previousHealth = currentHealth;
            float resolvedHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
            float appliedDamage = previousHealth - resolvedHealth;
            if (appliedDamage <= 0f)
            {
                return false;
            }

            lastDamageSource = damageSource;
            ProjectXXFactionUtility.RegisterIncomingDamage(gameObject, damageSource);

            bool previousDead = dead;
            currentHealth = resolvedHealth;
            dead = currentHealth <= 0f;

            Damaged?.Invoke(this, appliedDamage, damageSource);
            StateChanged?.Invoke(this);

            if (!previousDead && dead)
            {
                Died?.Invoke(this, damageSource);
            }

            return true;
        }

        private bool ApplyState(float newMaxHealth, float newCurrentHealth, GameObject newLastDamageSource)
        {
            float resolvedMaxHealth = Mathf.Max(1f, newMaxHealth);
            float resolvedCurrentHealth = Mathf.Clamp(newCurrentHealth, 0f, resolvedMaxHealth);
            bool resolvedDead = resolvedCurrentHealth <= 0f;

            bool changed = !Mathf.Approximately(maxHealth, resolvedMaxHealth) ||
                           !Mathf.Approximately(currentHealth, resolvedCurrentHealth) ||
                           dead != resolvedDead ||
                           lastDamageSource != newLastDamageSource;
            if (!changed)
            {
                return false;
            }

            bool previousDead = dead;

            maxHealth = resolvedMaxHealth;
            currentHealth = resolvedCurrentHealth;
            dead = resolvedDead;
            lastDamageSource = newLastDamageSource;

            StateChanged?.Invoke(this);

            if (!previousDead && dead)
            {
                Died?.Invoke(this, lastDamageSource);
            }

            return true;
        }

        private void NormalizeState()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            dead = currentHealth <= 0f;
        }
    }
}
