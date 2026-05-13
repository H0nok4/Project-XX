using JUTPS;
using ProjectXX.Domain.Combat;
using UnityEngine;

namespace JU.SaveLoad
{
    /// <summary>
    /// Load and save data for <see cref="JUHealth"/>.
    /// </summary>
    [RequireComponent(typeof(JUHealth))]
    [AddComponentMenu("JU TPS/Save Load/JU Save Load Health")]
    public class JUSaveLoadHealth : JUSaveLoadComponent
    {
        private JUHealth _health;
        private ProjectXXCombatant _combatant;

        private const string VALUE_KEY = "Health";
        private const string MAX_VALUE_KEY = "Max Health";

        /// <inheritdoc/>
        public JUSaveLoadHealth() : base()
        {
        }

        /// <inheritdoc/>
        protected override void Awake()
        {
            _health = GetComponent<JUHealth>();
            _combatant = GetComponent<ProjectXXCombatant>();

            base.Awake();
        }

        /// <inheritdoc/>
        public override void Save()
        {
            base.Save();

            float currentHealth = _combatant != null ? _combatant.CurrentHealth : _health.Health;
            float maxHealth = _combatant != null ? _combatant.MaxHealth : _health.MaxHealth;
            SetValue(VALUE_KEY, currentHealth);
            SetValue(MAX_VALUE_KEY, maxHealth);
        }

        /// <inheritdoc/>
        public override void Load()
        {
            base.Load();

            float fallbackMaxHealth = _combatant != null ? _combatant.MaxHealth : _health.MaxHealth;
            float loadedMaxHealth = GetValue(MAX_VALUE_KEY, fallbackMaxHealth);
            float loadedHealth = GetValue(VALUE_KEY, loadedMaxHealth);
            _health.SetHealthState(loadedHealth, loadedMaxHealth, true);
        }

        /// <inheritdoc/>
        protected override void OnExitPlayMode()
        {
            base.OnExitPlayMode();

            _health = null;
            _combatant = null;
        }
    }
}
