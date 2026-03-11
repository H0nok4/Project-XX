using UnityEngine;

public enum MetaEntryTarget
{
    MainMenu = 0,
    BaseScene = 1
}

[CreateAssetMenu(menuName = "Project-XX/Meta Entry Route Config", fileName = "MetaEntryRouteConfig")]
public sealed class MetaEntryRouteConfig : ScriptableObject
{
    public MetaEntryTarget defaultMetaTarget = MetaEntryTarget.BaseScene;
    public MetaEntryTarget returnFromRaidTarget = MetaEntryTarget.BaseScene;
    public MetaEntryTarget debugEntryTarget = MetaEntryTarget.MainMenu;
    public bool debugEntryEnabled = true;
    public string mainMenuSceneName = "MainMenu";
    public string baseSceneName = "BaseScene";
}
