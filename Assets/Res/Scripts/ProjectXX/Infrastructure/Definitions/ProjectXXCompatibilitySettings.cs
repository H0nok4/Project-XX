using ProjectXX.Domain.Combat;
using UnityEngine;

namespace ProjectXX.Infrastructure.Definitions
{
    [CreateAssetMenu(
        fileName = "ProjectXXCompatibilitySettings",
        menuName = "ProjectXX/Definitions/Compatibility Settings")]
    public sealed class ProjectXXCompatibilitySettings : ScriptableObject
    {
        [Header("Tags")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private string enemyTag = "Enemy";
        [SerializeField] private string bulletTag = "Bullet";

        [Header("Layers")]
        [SerializeField] private string playerLayerName = "Player";
        [SerializeField] private string fpsObjectLayerName = "FPS Object";
        [SerializeField] private string environmentLayerName = "Enviroment";

        public string PlayerTag => string.IsNullOrWhiteSpace(playerTag) ? "Player" : playerTag.Trim();
        public string EnemyTag => string.IsNullOrWhiteSpace(enemyTag) ? "Enemy" : enemyTag.Trim();
        public string BulletTag => string.IsNullOrWhiteSpace(bulletTag) ? "Bullet" : bulletTag.Trim();
        public string PlayerLayerName => string.IsNullOrWhiteSpace(playerLayerName) ? "Player" : playerLayerName.Trim();
        public string FpsObjectLayerName => string.IsNullOrWhiteSpace(fpsObjectLayerName) ? "FPS Object" : fpsObjectLayerName.Trim();
        public string EnvironmentLayerName => string.IsNullOrWhiteSpace(environmentLayerName) ? "Enviroment" : environmentLayerName.Trim();

        public string ResolveTargetTag(ProjectXXFaction faction)
        {
            return faction == ProjectXXFaction.Player ? PlayerTag : EnemyTag;
        }

        public string[] CreateBroadTargetTags()
        {
            return new[] { PlayerTag, EnemyTag };
        }

        public string[] CreateExtraTargetLayers()
        {
            return new[] { PlayerLayerName };
        }

        public string[] CreateGroundLayers()
        {
            return new[] { EnvironmentLayerName };
        }

        public bool TryGetPlayerLayer(out int layer)
        {
            return TryGetLayer(PlayerLayerName, out layer);
        }

        public bool TryGetFpsObjectLayer(out int layer)
        {
            return TryGetLayer(FpsObjectLayerName, out layer);
        }

        public bool TryGetEnvironmentLayer(out int layer)
        {
            return TryGetLayer(EnvironmentLayerName, out layer);
        }

        private static bool TryGetLayer(string layerName, out int layer)
        {
            layer = LayerMask.NameToLayer(layerName);
            return layer >= 0;
        }
    }
}
