using System;
using System.Collections.Generic;

[Serializable]
public sealed class SkillNodeDefinition
{
    public string nodeId = string.Empty;
    public string displayName = string.Empty;
    public string description = string.Empty;
    public SkillBranch branch;
    public int requiredPlayerLevel = 1;
    public List<string> prerequisiteNodeIds = new List<string>();
    public List<ModifierDefinition> modifiers = new List<ModifierDefinition>();
}
