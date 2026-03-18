using System;

[Serializable]
public sealed class PlayerProgressionData
{
    public int progressionDataVersion = ProfileSchemaVersion.CurrentProgressionDataVersion;
    public int playerLevel = 1;
    public int currentExperience;
    public int lifetimeExperience;
    public int killCount;
    public int unspentAttributePoints;
    public int unspentSkillPoints;
    public PlayerAttributeSet attributeSet = new PlayerAttributeSet();
    public PlayerSkillTree skillTree = new PlayerSkillTree();
}
