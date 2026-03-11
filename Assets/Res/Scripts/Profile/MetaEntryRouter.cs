using UnityEngine;
using UnityEngine.SceneManagement;

public static class MetaEntryRouter
{
    private const string DefaultConfigResourcePath = "MetaEntryRouteConfig";

    private static MetaEntryRouteConfig cachedConfig;
    private static MetaSessionContext session;

    public static bool IsDebugEntryEnabled
    {
        get
        {
            EnsureSession();
            return session.debugEntryEnabled;
        }
    }

    public static void EnterDefaultMeta()
    {
        EnsureSession();
        MetaEntryTarget target = session.debugEntryEnabled ? session.debugEntryTarget : session.defaultMetaTarget;
        LoadMetaTarget(target, ResolveMainMenuSceneName());
    }

    public static void EnterRaid(string raidSceneName)
    {
        EnsureSession();
        string sanitizedSceneName = SanitizeSceneName(raidSceneName);
        if (string.IsNullOrWhiteSpace(sanitizedSceneName))
        {
            Debug.LogWarning("[MetaEntryRouter] Raid scene name was empty; refusing to load.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sanitizedSceneName))
        {
            Debug.LogWarning($"[MetaEntryRouter] Raid scene '{sanitizedSceneName}' is not available.");
            return;
        }

        session.lastRaidSceneName = sanitizedSceneName;
        SceneManager.LoadScene(sanitizedSceneName);
    }

    public static void EnterBaseHub()
    {
        EnsureSession();
        session.baseHubArrivalMode = BaseHubArrivalMode.Default;
        LoadMetaTarget(MetaEntryTarget.BaseScene, ResolveMainMenuSceneName());
    }

    public static void EnterMainMenu()
    {
        EnsureSession();
        LoadMetaTarget(MetaEntryTarget.MainMenu, ResolveMainMenuSceneName());
    }

    public static void ReturnFromRaid(string fallbackSceneName)
    {
        EnsureSession();
        string fallback = ResolveFallbackSceneName(fallbackSceneName);
        LoadMetaTarget(session.returnFromRaidTarget, fallback);
    }

    public static void RecordRaidReturnArrival(RaidGameMode.RaidState raidState)
    {
        EnsureSession();
        session.baseHubArrivalMode = raidState == RaidGameMode.RaidState.Extracted
            ? BaseHubArrivalMode.Departure
            : BaseHubArrivalMode.Respawn;
    }

    public static BaseHubArrivalMode ConsumeBaseHubArrivalMode()
    {
        EnsureSession();
        BaseHubArrivalMode arrivalMode = session.baseHubArrivalMode;
        session.baseHubArrivalMode = BaseHubArrivalMode.Default;
        return arrivalMode;
    }

    private static void EnsureSession()
    {
        if (session == null)
        {
            session = new MetaSessionContext();
        }

        MetaEntryRouteConfig config = GetConfig();
        session.defaultMetaTarget = config.defaultMetaTarget;
        session.returnFromRaidTarget = config.returnFromRaidTarget;
        session.debugEntryTarget = config.debugEntryTarget;
        session.debugEntryEnabled = config.debugEntryEnabled;
    }

    private static MetaEntryRouteConfig GetConfig()
    {
        if (cachedConfig != null)
        {
            return cachedConfig;
        }

        cachedConfig = Resources.Load<MetaEntryRouteConfig>(DefaultConfigResourcePath);
        if (cachedConfig != null)
        {
            return cachedConfig;
        }

        cachedConfig = ScriptableObject.CreateInstance<MetaEntryRouteConfig>();
        cachedConfig.name = "MetaEntryRouteConfig (Runtime)";
        return cachedConfig;
    }

    private static void LoadMetaTarget(MetaEntryTarget target, string fallbackSceneName)
    {
        string targetSceneName = ResolveSceneName(target);
        if (!string.IsNullOrWhiteSpace(targetSceneName) && Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            session.lastMetaTarget = target;
            SceneManager.LoadScene(targetSceneName);
            return;
        }

        string fallback = ResolveFallbackSceneName(fallbackSceneName);
        Debug.LogWarning($"[MetaEntryRouter] Meta target '{target}' not available. Falling back to '{fallback}'.");
        session.lastMetaTarget = MetaEntryTarget.MainMenu;
        SceneManager.LoadScene(fallback);
    }

    private static string ResolveSceneName(MetaEntryTarget target)
    {
        MetaEntryRouteConfig config = GetConfig();
        string sceneName = target == MetaEntryTarget.BaseScene ? config.baseSceneName : config.mainMenuSceneName;
        return SanitizeSceneName(sceneName);
    }

    private static string ResolveMainMenuSceneName()
    {
        MetaEntryRouteConfig config = GetConfig();
        string sceneName = SanitizeSceneName(config.mainMenuSceneName);
        return string.IsNullOrWhiteSpace(sceneName) ? "MainMenu" : sceneName;
    }

    private static string ResolveFallbackSceneName(string fallbackSceneName)
    {
        string sceneName = SanitizeSceneName(fallbackSceneName);
        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            return sceneName;
        }

        return ResolveMainMenuSceneName();
    }

    private static string SanitizeSceneName(string sceneName)
    {
        return string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
    }
}
