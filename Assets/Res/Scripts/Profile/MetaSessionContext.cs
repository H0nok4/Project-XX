public sealed class MetaSessionContext
{
    public MetaEntryTarget defaultMetaTarget;
    public MetaEntryTarget returnFromRaidTarget;
    public MetaEntryTarget debugEntryTarget;
    public bool debugEntryEnabled;
    public string lastRaidSceneName = string.Empty;
    public MetaEntryTarget lastMetaTarget;
}
