using UnityEngine;

public abstract class ItemDefinitionBase : ScriptableObject
{
    public abstract string ItemId { get; }
    public abstract string DisplayName { get; }
    public virtual string DisplayNameWithLevel => $"{DisplayName} (Lv {ItemLevel})";
    public virtual string Description => string.Empty;
    public virtual Sprite Icon => null;
    public abstract float UnitWeight { get; }
    public virtual int MaxStackSize => 1;
    public abstract int ItemLevel { get; }
    public abstract int RequiredLevel { get; }
}
