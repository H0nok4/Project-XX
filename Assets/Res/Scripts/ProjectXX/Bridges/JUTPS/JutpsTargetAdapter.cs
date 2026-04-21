using ProjectXX.Foundation;
using ProjectXX.Infrastructure.Definitions;
using ProjectXX.Domain.Combat;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectXX.Bridges.JUTPS
{
    [DisallowMultipleComponent]
    public sealed class JutpsTargetAdapter : MonoBehaviour
    {
        [SerializeField] private bool deriveFromFactionMember = true;
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private string playerLayerName = "Player";

        private void Awake()
        {
            ApplyTargetSettings();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            ScheduleTargetSettingsRefresh();
        }

        public void RefreshTargetSettings()
        {
            ApplyTargetSettings();
        }

        private void ApplyTargetSettings()
        {
            ProjectXXCompatibilitySettings compatibilitySettings = ProjectXXCompatibilitySettingsProvider.GetOrDefault();
            if (compatibilitySettings != null)
            {
                playerLayerName = compatibilitySettings.PlayerLayerName;
            }

            if (deriveFromFactionMember && TryGetComponent(out ProjectXXFactionMember factionMember))
            {
                targetTag = compatibilitySettings != null
                    ? compatibilitySettings.ResolveTargetTag(factionMember.Faction)
                    : factionMember.Faction == ProjectXXFaction.Player ? "Player" : "Enemy";
            }

            if (!string.IsNullOrWhiteSpace(targetTag))
            {
                gameObject.tag = targetTag.Trim();
            }

            if (deriveFromFactionMember && TryGetComponent(out ProjectXXFactionMember member) && member.Faction != ProjectXXFaction.Player)
            {
                return;
            }

            int playerLayer = LayerMask.NameToLayer(playerLayerName);
            if (playerLayer >= 0)
            {
                gameObject.layer = playerLayer;
            }
        }

#if UNITY_EDITOR
        private void ScheduleTargetSettingsRefresh()
        {
            EditorApplication.delayCall -= DelayedRefreshTargetSettings;
            EditorApplication.delayCall += DelayedRefreshTargetSettings;
        }

        private void DelayedRefreshTargetSettings()
        {
            if (this == null || gameObject == null)
            {
                return;
            }

            ApplyTargetSettings();
        }
#endif
    }
}
