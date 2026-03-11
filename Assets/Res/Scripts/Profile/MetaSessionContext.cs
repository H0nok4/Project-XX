public enum BaseHubArrivalMode
{
    Default = 0,
    Departure = 1,
    Respawn = 2
}

public sealed class MetaSessionContext
{
    public MetaEntryTarget defaultMetaTarget;
    public MetaEntryTarget returnFromRaidTarget;
    public MetaEntryTarget debugEntryTarget;
    public bool debugEntryEnabled;
    public string lastRaidSceneName = string.Empty;
    public MetaEntryTarget lastMetaTarget;
    public BaseHubArrivalMode baseHubArrivalMode = BaseHubArrivalMode.Default;
}
