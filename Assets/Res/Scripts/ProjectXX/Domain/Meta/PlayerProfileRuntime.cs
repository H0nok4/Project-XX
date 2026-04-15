using System;
using UnityEngine;

namespace ProjectXX.Domain.Meta
{
    [Serializable]
    public sealed class PlayerProfileRuntime
    {
        [SerializeField] private string profileId = "debug-profile";
        [SerializeField] private string displayName = "Operator";
        [SerializeField] private float baseMaxHealth = 100f;

        public string ProfileId => profileId;
        public string DisplayName => displayName;
        public float BaseMaxHealth => baseMaxHealth;
    }
}
