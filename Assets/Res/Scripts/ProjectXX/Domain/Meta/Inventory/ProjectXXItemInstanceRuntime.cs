using System;
using UnityEngine;

namespace ProjectXX.Domain.Inventory
{
    [Serializable]
    public sealed class ProjectXXItemInstanceRuntime
    {
        [SerializeField] private string instanceId;
        [SerializeField] private ProjectXXItemDefinition definition;
        [SerializeField, Min(1)] private int quantity = 1;

        public ProjectXXItemInstanceRuntime()
        {
            instanceId = Guid.NewGuid().ToString("N");
        }

        public ProjectXXItemInstanceRuntime(ProjectXXItemDefinition definition, int quantity = 1, string instanceId = null)
        {
            this.instanceId = string.IsNullOrWhiteSpace(instanceId)
                ? Guid.NewGuid().ToString("N")
                : instanceId.Trim();
            this.definition = definition;
            SetQuantity(quantity);
        }

        public string InstanceId => string.IsNullOrWhiteSpace(instanceId)
            ? string.Empty
            : instanceId.Trim();

        public ProjectXXItemDefinition Definition => definition;
        public int Quantity => Mathf.Clamp(quantity, 1, MaxQuantity);
        public int MaxQuantity => definition != null ? definition.MaxStackSize : Mathf.Max(1, quantity);
        public ProjectXXGridSize GridSize => definition != null ? definition.GridSize : ProjectXXGridSize.One;
        public bool IsValid => definition != null;

        public void SetQuantity(int value)
        {
            quantity = Mathf.Clamp(value, 1, MaxQuantity);
        }

        public bool CanStackWith(ProjectXXItemInstanceRuntime other)
        {
            return other != null &&
                   definition != null &&
                   definition == other.definition &&
                   definition.IsStackable;
        }

        public int AddQuantity(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int accepted = Mathf.Min(amount, MaxQuantity - Quantity);
            quantity = Quantity + accepted;
            return accepted;
        }
    }
}
