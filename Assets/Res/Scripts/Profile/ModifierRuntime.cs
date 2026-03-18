using System;

[Serializable]
public sealed class ModifierRuntime
{
    public string sourceId = string.Empty;
    public string sourceLabel = string.Empty;
    public string sourceDetail = string.Empty;
    public CharacterStatType statType;
    public ModifierOperation operation = ModifierOperation.Add;
    public float value;
}
