using Akila.FPSFramework;
using JUTPS;
using ProjectXX.Domain.Combat;
using UnityEngine;

namespace ProjectXX.Bridges.FPSFramework
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterInput))]
    [RequireComponent(typeof(CharacterManager))]
    [RequireComponent(typeof(Damageable))]
    [RequireComponent(typeof(ProjectXXCombatant))]
    public sealed class ProjectXXPlayerFacade : MonoBehaviour
    {
        [SerializeField] private string displayName = "Operator";
        [SerializeField] private Damageable damageable;
        [SerializeField] private CharacterManager characterManager;
        [SerializeField] private CharacterInput characterInput;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Inventory inventory;
        [SerializeField] private CameraManager cameraManager;
        [SerializeField] private JUHealth jutpsHealth;
        [SerializeField] private ProjectXXCombatant combatant;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Operator" : displayName.Trim();
        public Damageable Damageable => damageable;
        public CharacterManager CharacterManager => characterManager;
        public CharacterInput CharacterInput => characterInput;
        public CharacterController CharacterController => characterController;
        public Inventory Inventory => inventory;
        public CameraManager CameraManager => cameraManager;
        public JUHealth JutpsHealth => jutpsHealth;
        public ProjectXXCombatant Combatant => combatant;

        private void Awake()
        {
            RefreshReferences();
        }

        public void RefreshReferences()
        {
            damageable = GetComponent<Damageable>();
            characterManager = GetComponent<CharacterManager>();
            characterInput = GetComponent<CharacterInput>();
            characterController = GetComponent<CharacterController>();
            inventory = this.SearchFor<Inventory>(true);
            cameraManager = this.SearchFor<CameraManager>(true);
            jutpsHealth = GetComponent<JUHealth>();
            combatant = GetComponent<ProjectXXCombatant>();
        }

        public bool TryGetCurrentFirearm(out Firearm firearm)
        {
            firearm = inventory != null ? inventory.currentItem as Firearm : null;
            return firearm != null;
        }
    }
}
