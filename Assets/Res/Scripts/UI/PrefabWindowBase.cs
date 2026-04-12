using UnityEngine;

[DisallowMultipleComponent]
public abstract class PrefabWindowBase<TTemplate> : WindowBase
    where TTemplate : Component
{
    private TTemplate windowTemplate;

    protected TTemplate Template => windowTemplate;

    protected abstract string WindowPrefabResourcePath { get; }
    protected virtual string WindowPrefabId => string.Empty;
    protected virtual bool ApplyRuntimeFontToPrefab => true;
    protected sealed override bool ApplyRuntimeFontToChildren => ApplyRuntimeFontToPrefab;
    protected virtual bool FallbackToGeneratedWindowChrome => false;

    protected sealed override PrototypeUiToolkit.WindowChrome CreateWindowChrome()
    {
        RectTransform parent = UiManager.GetLayerRoot(WindowLayer);
        if (TryInstantiateWindowPrefab(parent, out PrototypeUiToolkit.WindowChrome chrome))
        {
            return chrome;
        }

        windowTemplate = null;
        return FallbackToGeneratedWindowChrome ? base.CreateWindowChrome() : null;
    }

    protected sealed override void BuildWindow(PrototypeUiToolkit.WindowChrome chrome)
    {
        if (chrome == null || chrome.Root == null)
        {
            return;
        }

        if (windowTemplate == null)
        {
            windowTemplate = chrome.Root.GetComponent<TTemplate>();
        }

        if (windowTemplate == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Window root is missing template component {typeof(TTemplate).Name}.", this);
            return;
        }

        BuildPrefabWindow(windowTemplate, chrome);
    }

    protected override void OnWindowRootDestroyed()
    {
        windowTemplate = null;
        base.OnWindowRootDestroyed();
    }

    protected virtual GameObject LoadWindowPrefab()
    {
        return UiPrefabRegistry.LoadPrefab(WindowPrefabId, WindowPrefabResourcePath);
    }

    protected virtual TTemplate ResolveTemplateFromInstance(GameObject instanceObject)
    {
        return instanceObject != null ? instanceObject.GetComponent<TTemplate>() : null;
    }

    protected virtual void OnWindowPrefabInstantiated(GameObject instanceObject, TTemplate template, PrototypeUiToolkit.WindowChrome chrome)
    {
    }

    protected abstract PrototypeUiToolkit.WindowChrome CreatePrefabWindowChrome(TTemplate template);
    protected abstract void BuildPrefabWindow(TTemplate template, PrototypeUiToolkit.WindowChrome chrome);

    private bool TryInstantiateWindowPrefab(RectTransform parent, out PrototypeUiToolkit.WindowChrome chrome)
    {
        chrome = null;
        GameObject prefabAsset = LoadWindowPrefab();
        if (prefabAsset == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing window prefab at Resources/{WindowPrefabResourcePath}.", this);
            return false;
        }

        GameObject instanceObject = Instantiate(prefabAsset, parent, false);
        instanceObject.name = prefabAsset.name;

        windowTemplate = ResolveTemplateFromInstance(instanceObject);
        if (windowTemplate == null)
        {
            Debug.LogWarning(
                $"[{GetType().Name}] Missing template component {typeof(TTemplate).Name} on instantiated window prefab '{prefabAsset.name}'.",
                this);
            Destroy(instanceObject);
            return false;
        }

        chrome = CreatePrefabWindowChrome(windowTemplate);
        if (chrome == null || chrome.Root == null)
        {
            Debug.LogWarning(
                $"[{GetType().Name}] Failed to build WindowChrome from template {typeof(TTemplate).Name}.",
                this);
            Destroy(instanceObject);
            windowTemplate = null;
            chrome = null;
            return false;
        }

        OnWindowPrefabInstantiated(instanceObject, windowTemplate, chrome);
        return true;
    }
}
