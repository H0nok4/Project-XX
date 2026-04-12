using UnityEngine;

[DisallowMultipleComponent]
public abstract class PrefabViewBase<TTemplate> : ViewBase
    where TTemplate : Component
{
    private TTemplate viewTemplate;

    protected TTemplate Template => viewTemplate;

    protected abstract string ViewPrefabResourcePath { get; }
    protected virtual string ViewPrefabId => string.Empty;
    protected virtual bool ApplyRuntimeFontToPrefab => true;
    protected sealed override bool ApplyRuntimeFontToChildren => ApplyRuntimeFontToPrefab;

    protected sealed override RectTransform CreateViewRoot()
    {
        RectTransform parent = UiManager.GetLayerRoot(Layer);
        GameObject prefabAsset = LoadViewPrefab();
        if (prefabAsset == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing view prefab at Resources/{ViewPrefabResourcePath}.", this);
            viewTemplate = null;
            return null;
        }

        GameObject instanceObject = Instantiate(prefabAsset, parent, false);
        instanceObject.name = prefabAsset.name;

        viewTemplate = ResolveTemplateFromInstance(instanceObject);
        if (viewTemplate == null)
        {
            Debug.LogWarning(
                $"[{GetType().Name}] Missing template component {typeof(TTemplate).Name} on instantiated view prefab '{prefabAsset.name}'.",
                this);
            Destroy(instanceObject);
            return null;
        }

        RectTransform root = ResolveViewRoot(viewTemplate);
        if (root == null)
        {
            Debug.LogWarning(
                $"[{GetType().Name}] Failed to resolve a RectTransform root from template {typeof(TTemplate).Name}.",
                this);
            Destroy(instanceObject);
            viewTemplate = null;
            return null;
        }

        OnViewPrefabInstantiated(instanceObject, viewTemplate, root);
        return root;
    }

    protected sealed override void BuildView(RectTransform root)
    {
        if (root == null)
        {
            return;
        }

        if (viewTemplate == null)
        {
            viewTemplate = root.GetComponent<TTemplate>();
        }

        if (viewTemplate == null)
        {
            Debug.LogWarning($"[{GetType().Name}] View root is missing template component {typeof(TTemplate).Name}.", this);
            return;
        }

        BuildPrefabView(viewTemplate, root);
    }

    protected override void OnViewRootDestroyed()
    {
        viewTemplate = null;
        base.OnViewRootDestroyed();
    }

    protected virtual GameObject LoadViewPrefab()
    {
        return UiPrefabRegistry.LoadPrefab(ViewPrefabId, ViewPrefabResourcePath);
    }

    protected virtual TTemplate ResolveTemplateFromInstance(GameObject instanceObject)
    {
        return instanceObject != null ? instanceObject.GetComponent<TTemplate>() : null;
    }

    protected virtual RectTransform ResolveViewRoot(TTemplate template)
    {
        return template != null ? template.transform as RectTransform : null;
    }

    protected virtual void OnViewPrefabInstantiated(GameObject instanceObject, TTemplate template, RectTransform root)
    {
    }

    protected abstract void BuildPrefabView(TTemplate template, RectTransform root);
}
