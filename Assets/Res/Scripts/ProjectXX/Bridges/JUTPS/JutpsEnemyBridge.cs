using Akila.FPSFramework;
using JU.CharacterSystem.AI;
using JUTPS;
using ProjectXX.Bridges.Combat;
using ProjectXX.Domain.Combat;
using ProjectXX.Domain.Raid;
using ProjectXX.Foundation;
using ProjectXX.Infrastructure.Definitions;
using UnityEngine;

namespace ProjectXX.Bridges.JUTPS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(JUHealth))]
    [RequireComponent(typeof(ProjectXXCombatant))]
    [RequireComponent(typeof(ProjectXXCombatantSync))]
    [RequireComponent(typeof(ProjectXXFactionMember))]
    [RequireComponent(typeof(JutpsTargetAdapter))]
    [RequireComponent(typeof(ProjectXXJutpsFactionBridge))]
    public sealed class JutpsEnemyBridge : MonoBehaviour
    {
        [SerializeField] private RaidSessionRuntime sessionRuntime;
        [SerializeField] private ProjectXXEnemyDefinition enemyDefinition;
        [SerializeField] private bool forceSimpleNavigation = true;
        [SerializeField] private bool useCompatibilityGroundLayers = true;
        [SerializeField] private string[] additionalGroundLayers = System.Array.Empty<string>();
        [SerializeField] private float groundSnapProbeHeight = 1.5f;
        [SerializeField] private float groundSnapDistance = 4f;

        private JUHealth juHealth;
        private JUCharacterController characterController;
        private JUFootPlacement footPlacement;
        private JU_AI_Zombie zombieAi;
        private Damager[] damagers;
        private ProjectXXCombatant combatant;
        private bool registered;
        private bool deathReported;

        private void Awake()
        {
            juHealth = GetComponent<JUHealth>();
            characterController = GetComponent<JUCharacterController>();
            footPlacement = GetComponent<JUFootPlacement>();
            zombieAi = GetComponent<JU_AI_Zombie>();
            damagers = GetComponentsInChildren<Damager>(true);
            combatant = GetComponent<ProjectXXCombatant>();

            ApplyDefinition();
        }

        private void OnEnable()
        {
            if (combatant != null)
            {
                combatant.Died += HandleCombatantDeath;
            }
        }

        private void Start()
        {
            RegisterWithSession();
        }

        private void OnDisable()
        {
            if (combatant != null)
            {
                combatant.Died -= HandleCombatantDeath;
            }
        }

        public void Configure(RaidSessionRuntime runtime, ProjectXXEnemyDefinition definition)
        {
            sessionRuntime = runtime;
            enemyDefinition = definition;
            ApplyDefinition();
            RegisterWithSession();
        }

        private void ApplyDefinition()
        {
            ConfigureFaction();

            if (juHealth == null || enemyDefinition == null)
            {
                EnsureCombatant();
                ApplyNavigationMode();
                ConfigureDamageableHitDecals();
                ConfigureGroundingMasks();
                SnapToGround();

                return;
            }

            EnsureCombatant();
            combatant.Configure(enemyDefinition.MaxHealth, enemyDefinition.MaxHealth, true);

            ApplyNavigationMode();
            ConfigureDamageableHitDecals();
            ConfigureGroundingMasks();
            SnapToGround();

            if (zombieAi != null)
            {
                zombieAi.FieldOfView.Distance = enemyDefinition.DetectionDistance;
            }

            for (int i = 0; i < damagers.Length; i++)
            {
                damagers[i].Damage = enemyDefinition.ContactDamage;
            }
        }

        private void ConfigureDamageableHitDecals()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                {
                    continue;
                }

                CustomDecal customDecal = collider.GetComponent<CustomDecal>();
                if (customDecal == null)
                {
                    customDecal = collider.gameObject.AddComponent<CustomDecal>();
                }

                // Living targets should not inherit environment bullet-hole decals.
                customDecal.decalVFX = null;
                customDecal.parent = true;
                customDecal.lifeTime = 0.5f;
            }
        }

        private void ConfigureFaction()
        {
            ProjectXXFactionMember factionMember = GetComponent<ProjectXXFactionMember>();
            if (factionMember == null)
            {
                ProjectXXLog.Error("JutpsEnemyBridge requires ProjectXXFactionMember on the enemy prefab.", this);
                return;
            }

            factionMember.SetFaction(ProjectXXFaction.Enemy);

            JutpsTargetAdapter targetAdapter = GetComponent<JutpsTargetAdapter>();
            if (targetAdapter == null)
            {
                ProjectXXLog.Error("JutpsEnemyBridge requires JutpsTargetAdapter on the enemy prefab.", this);
                return;
            }

            targetAdapter.RefreshTargetSettings();

            ProjectXXJutpsFactionBridge factionBridge = GetComponent<ProjectXXJutpsFactionBridge>();
            if (factionBridge == null)
            {
                ProjectXXLog.Error("JutpsEnemyBridge requires ProjectXXJutpsFactionBridge on the enemy prefab.", this);
                return;
            }

            factionBridge.Refresh();
        }

        private void EnsureCombatant()
        {
            combatant ??= GetComponent<ProjectXXCombatant>();
            if (combatant == null)
            {
                ProjectXXLog.Error("JutpsEnemyBridge requires ProjectXXCombatant on the enemy prefab.", this);
            }

            if (GetComponent<ProjectXXCombatantSync>() == null)
            {
                ProjectXXLog.Error("JutpsEnemyBridge requires ProjectXXCombatantSync on the enemy prefab.", this);
            }
        }

        private void ApplyNavigationMode()
        {
            if (zombieAi == null)
            {
                return;
            }

            zombieAi.NavigationSettings.Mode = forceSimpleNavigation
                ? JUCharacterAIBase.NavigationModes.Simple
                : zombieAi.NavigationSettings.Mode;
        }

        private void ConfigureGroundingMasks()
        {
            int compatibilityMask = BuildLayerMask(ResolveGroundLayers());
            if (compatibilityMask == 0)
            {
                return;
            }

            if (characterController != null)
            {
                characterController.WhatIsGround = characterController.WhatIsGround.value | compatibilityMask;
                characterController.WhatIsWall = characterController.WhatIsWall.value | compatibilityMask;
                characterController.StepCorrectionMask = characterController.StepCorrectionMask.value | compatibilityMask;
            }

            footPlacement ??= GetComponent<JUFootPlacement>();
            if (footPlacement != null)
            {
                footPlacement.GroundLayers = footPlacement.GroundLayers.value | compatibilityMask;
            }
        }

        private void SnapToGround()
        {
            int snapMask = BuildLayerMask(ResolveGroundLayers());
            if (snapMask == 0)
            {
                return;
            }

            Vector3 rayOrigin = transform.position + Vector3.up * groundSnapProbeHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundSnapProbeHeight + groundSnapDistance, snapMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 position = transform.position;
                position.y = hit.point.y;
                transform.position = position;
            }
        }

        private void RegisterWithSession()
        {
            if (registered || sessionRuntime == null)
            {
                return;
            }

            sessionRuntime.RegisterEnemy(gameObject.GetInstanceID());
            registered = true;
        }

        private void HandleDeath()
        {
            if (deathReported)
            {
                return;
            }

            deathReported = true;
            if (sessionRuntime != null)
            {
                sessionRuntime.NotifyEnemyKilled(gameObject.GetInstanceID());
            }

            ProjectXXLog.Info($"Enemy defeated: {gameObject.name}", this);
        }

        private void HandleCombatantDeath(ProjectXXCombatant _, GameObject __)
        {
            HandleDeath();
        }

        private string[] ResolveGroundLayers()
        {
            if (useCompatibilityGroundLayers)
            {
                ProjectXXCompatibilitySettings compatibilitySettings = ProjectXXCompatibilitySettingsProvider.GetOrDefault();
                if (compatibilitySettings != null)
                {
                    return compatibilitySettings.CreateGroundLayers();
                }
            }

            return additionalGroundLayers ?? System.Array.Empty<string>();
        }

        private static int BuildLayerMask(string[] layerNames)
        {
            if (layerNames == null)
            {
                return 0;
            }

            int layerMask = 0;
            for (int i = 0; i < layerNames.Length; i++)
            {
                string layerName = layerNames[i];
                if (string.IsNullOrWhiteSpace(layerName))
                {
                    continue;
                }

                int layer = LayerMask.NameToLayer(layerName.Trim());
                if (layer >= 0)
                {
                    layerMask |= 1 << layer;
                }
            }

            return layerMask;
        }
    }
}
