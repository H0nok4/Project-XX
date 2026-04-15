using ProjectXX.Domain.Combat;
using UnityEngine;

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

            ApplyTargetSettings();
        }

        public void RefreshTargetSettings()
        {
            ApplyTargetSettings();
        }

        private void ApplyTargetSettings()
        {
            if (deriveFromFactionMember && TryGetComponent(out ProjectXXFactionMember factionMember))
            {
                targetTag = factionMember.Faction == ProjectXXFaction.Player ? "Player" : "Enemy";
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
    }
}
