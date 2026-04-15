using Akila.FPSFramework;
using JU.CharacterSystem.AI;
using JUTPS;
using ProjectXX.Domain.Combat;
using ProjectXX.Domain.Raid;
using ProjectXX.Foundation;
using ProjectXX.Infrastructure.Definitions;
using UnityEngine;

namespace ProjectXX.Bridges.JUTPS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(JUHealth))]
    public sealed class JutpsEnemyBridge : MonoBehaviour
    {
        [SerializeField] private RaidSessionRuntime sessionRuntime;
        [SerializeField] private ProjectXXEnemyDefinition enemyDefinition;
        [SerializeField] private bool forceSimpleNavigation = true;
        [SerializeField] private string[] additionalGroundLayers = { "Enviroment" };
        [SerializeField] private float groundSnapProbeHeight = 1.5f;
        [SerializeField] private float groundSnapDistance = 4f;

        private JUHealth juHealth;
        private JUCharacterController characterController;
        private JU_AI_Zombie zombieAi;
        private Damager[] damagers;
        private bool registered;
        private bool deathReported;

        private void Awake()
        {
            juHealth = GetComponent<JUHealth>();
            characterController = GetComponent<JUCharacterController>();
            zombieAi = GetComponent<JU_AI_Zombie>();
            damagers = GetComponentsInChildren<Damager>(true);

            ApplyDefinition();
        }

        private void OnEnable()
        {
            if (juHealth != null)
            {
                juHealth.OnDeath.AddListener(HandleDeath);
            }
        }

        private void Start()
        {
            if (sessionRuntime == null)
            {
                sessionRuntime = FindFirstObjectByType<RaidSessionRuntime>();
            }

            RegisterWithSession();
        }

        private void OnDisable()
        {
            if (juHealth != null)
            {
                juHealth.OnDeath.RemoveListener(HandleDeath);
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
                ApplyNavigationMode();
                ConfigureDamageableHitDecals();
                ConfigureGroundingMasks();
                SnapToGround();

                return;
            }

            juHealth.MaxHealth = enemyDefinition.MaxHealth;
            juHealth.Health = enemyDefinition.MaxHealth;
            juHealth.CheckHealthState();

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
                factionMember = gameObject.AddComponent<ProjectXXFactionMember>();
            }

            factionMember.SetFaction(ProjectXXFaction.Enemy);

            JutpsTargetAdapter targetAdapter = GetComponent<JutpsTargetAdapter>();
            if (targetAdapter == null)
            {
                targetAdapter = gameObject.AddComponent<JutpsTargetAdapter>();
            }

            targetAdapter.RefreshTargetSettings();

            ProjectXXJutpsFactionBridge factionBridge = GetComponent<ProjectXXJutpsFactionBridge>();
            if (factionBridge == null)
            {
                factionBridge = gameObject.AddComponent<ProjectXXJutpsFactionBridge>();
            }

            factionBridge.Refresh();
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
            if (characterController == null)
            {
                return;
            }

            int groundMask = characterController.WhatIsGround.value;
            int wallMask = characterController.WhatIsWall.value;
            int stepMask = characterController.StepCorrectionMask.value;
            bool updated = false;

            for (int i = 0; i < additionalGroundLayers.Length; i++)
            {
                string layerName = additionalGroundLayers[i];
                if (string.IsNullOrWhiteSpace(layerName))
                {
                    continue;
                }

                int layer = LayerMask.NameToLayer(layerName);
                if (layer < 0)
                {
                    continue;
                }

                int layerMask = 1 << layer;
                if ((groundMask & layerMask) == 0)
                {
                    groundMask |= layerMask;
                    updated = true;
                }

                if ((wallMask & layerMask) == 0)
                {
                    wallMask |= layerMask;
                    updated = true;
                }

                if ((stepMask & layerMask) == 0)
                {
                    stepMask |= layerMask;
                    updated = true;
                }
            }

            if (!updated)
            {
                return;
            }

            characterController.WhatIsGround = groundMask;
            characterController.WhatIsWall = wallMask;
            characterController.StepCorrectionMask = stepMask;
        }

        private void SnapToGround()
        {
            if (characterController == null || additionalGroundLayers == null || additionalGroundLayers.Length == 0)
            {
                return;
            }

            int snapMask = 0;
            for (int i = 0; i < additionalGroundLayers.Length; i++)
            {
                int layer = LayerMask.NameToLayer(additionalGroundLayers[i]);
                if (layer >= 0)
                {
                    snapMask |= 1 << layer;
                }
            }

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
    }
}
