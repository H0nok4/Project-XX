using System;

[Serializable]
public sealed class PlayerProgressionData
{
    public int progressionDataVersion = ProfileSchemaVersion.CurrentProgressionDataVersion;
    public int playerLevel = 1;
}
