using System;
using System.Collections.Generic;

[Serializable]
public sealed class WorldStateData
{
    [Serializable]
    public sealed class QuestChainStageRecord
    {
        public string chainId = string.Empty;
        public string stageId = string.Empty;
    }

    public int worldStateVersion = ProfileSchemaVersion.CurrentWorldStateVersion;
    public List<string> unlockedRaidMerchantIds = new List<string>();
    public List<string> unlockedRaidNpcIds = new List<string>();
    public List<QuestChainStageRecord> questChainStages = new List<QuestChainStageRecord>();
    public List<string> storyFlags = new List<string>();
    public List<MerchantData> merchantProgress = new List<MerchantData>();
    public List<FacilityData> baseFacilities = new List<FacilityData>();
}
