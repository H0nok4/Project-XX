using System;

[Serializable]
public sealed class PlayerProgressionData
{
    public int progressionDataVersion = ProfileSchemaVersion.CurrentProgressionDataVersion;
    public int playerLevel = 1;
    public int currentExperience;
    public int lifetimeExperience;
    public int killCount;
}
