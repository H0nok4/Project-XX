using JU.CharacterSystem.AI;
using ProjectXX.Domain.Combat;
using UnityEngine;

namespace ProjectXX.Bridges.JUTPS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectXXFactionMember))]
    public sealed class ProjectXXJutpsFactionBridge : MonoBehaviour
    {
        [SerializeField] private string[] broadTargetTags = { "Player", "Enemy" };
        [SerializeField] private string[] extraTargetLayers = { "Player" };

        private ProjectXXJutpsFactionTargetFilter targetFilter;
        private JU_AI_Zombie zombieAi;
        private JU_AI_PatrolCharacter patrolAi;
        private JutpsTargetAdapter targetAdapter;

        private void Awake()
        {
            Refresh();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            Refresh();
        }

        public void Refresh()
        {
            targetFilter = GetOrAdd<ProjectXXJutpsFactionTargetFilter>();
            zombieAi = GetComponent<JU_AI_Zombie>();
            patrolAi = GetComponent<JU_AI_PatrolCharacter>();
            targetAdapter = GetComponent<JutpsTargetAdapter>();

            targetAdapter?.RefreshTargetSettings();

            if (zombieAi != null)
            {
                ApplyFieldOfViewSettings(zombieAi.FieldOfView);
            }

            if (patrolAi != null)
            {
                ApplyFieldOfViewSettings(patrolAi.FieldOfView);
            }
        }

        private void ApplyFieldOfViewSettings(FieldOfView fieldOfView)
        {
            if (fieldOfView == null)
            {
                return;
            }

            fieldOfView.TargetTags = broadTargetTags;

            int targetMask = 1 << gameObject.layer;
            for (int i = 0; i < extraTargetLayers.Length; i++)
            {
                string layerName = extraTargetLayers[i];
                if (string.IsNullOrWhiteSpace(layerName))
                {
                    continue;
                }

                int layer = LayerMask.NameToLayer(layerName.Trim());
                if (layer >= 0)
                {
                    targetMask |= 1 << layer;
                }
            }

            fieldOfView.TargetsLayer = targetMask;
        }

        private T GetOrAdd<T>()
            where T : Component
        {
            if (TryGetComponent(out T component))
            {
                return component;
            }

            return gameObject.AddComponent<T>();
        }
    }
}
