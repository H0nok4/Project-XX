using UnityEngine;

namespace ProjectXX.Infrastructure.Definitions
{
    [CreateAssetMenu(
        fileName = "EnemyDefinition",
        menuName = "ProjectXX/Definitions/Enemy Definition")]
    public sealed class ProjectXXEnemyDefinition : ScriptableObject
    {
        [SerializeField] private string definitionId = "enemy.zombie.basic";
        [SerializeField] private string displayName = "Wanderer";
        [SerializeField] private float maxHealth = 60f;
        [SerializeField] private float contactDamage = 15f;
        [SerializeField] private float detectionDistance = 16f;

        public string DefinitionId => definitionId;
        public string DisplayName => displayName;
        public float MaxHealth => maxHealth;
        public float ContactDamage => contactDamage;
        public float DetectionDistance => detectionDistance;
    }
}
