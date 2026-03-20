using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RaidReturnButtonView : ViewBase
{
    private const string ViewPrefabResourcePath = "UI/Raid/RaidReturnButton";

    private static RaidReturnButtonView instance;

    private RaidReturnButtonTemplate viewTemplate;
    private Button returnButton;
    private TMP_Text labelText;

    public static RaidReturnButtonView GetOrCreate()
    {
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        instance = FindFirstObjectByType<RaidReturnButtonView>();
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        GameObject root = new GameObject("RaidReturnButtonView");
        instance = root.AddComponent<RaidReturnButtonView>();
        instance.Prepare();
        return instance;
    }

    protected override PrototypeUiLayer Layer => PrototypeUiLayer.Modal;
    protected override bool AddTransparentRootGraphic => false;
    protected override bool VisibleOnAwake => false;
    protected override bool RootGraphicRaycastTarget => false;
    protected override bool ApplyRuntimeFontToChildren => true;
    protected override string ViewName => "RaidReturnButton";

    protected override RectTransform CreateViewRoot()
    {
        RectTransform parent = UiManager.GetLayerRoot(Layer);
        GameObject prefabAsset = Resources.Load<GameObject>(ViewPrefabResourcePath);
        if (prefabAsset == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing view prefab at Resources/{ViewPrefabResourcePath}.", this);
            return null;
        }

        GameObject instanceObject = Instantiate(prefabAsset, parent, false);
        instanceObject.name = prefabAsset.name;

        viewTemplate = instanceObject.GetComponent<RaidReturnButtonTemplate>();
        if (viewTemplate == null || viewTemplate.Root == null)
        {
            Destroy(instanceObject);
            viewTemplate = null;
            Debug.LogWarning($"[{GetType().Name}] Raid return button prefab is missing {nameof(RaidReturnButtonTemplate)}.", this);
            return null;
        }

        return viewTemplate.Root;
    }

    public void Configure(Action onClick, string label, bool visible)
    {
        Prepare();
        if (labelText != null)
        {
            labelText.text = string.IsNullOrWhiteSpace(label) ? "Return To Menu" : label.Trim();
        }

        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                returnButton.onClick.AddListener(() => onClick());
            }
        }

        SetViewVisible(visible);
    }

    protected override void BuildView(RectTransform root)
    {
        if (root == null)
        {
            return;
        }

        if (viewTemplate == null || viewTemplate.Root != root)
        {
            viewTemplate = root.GetComponent<RaidReturnButtonTemplate>();
        }

        if (viewTemplate == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing {nameof(RaidReturnButtonTemplate)} on instantiated view root.", this);
            return;
        }

        returnButton = viewTemplate.Button;
        labelText = viewTemplate.LabelText;
    }

    protected override void OnViewRootDestroyed()
    {
        viewTemplate = null;
        returnButton = null;
        labelText = null;
        base.OnViewRootDestroyed();
    }

    protected override void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        base.OnDestroy();
    }

    private void Prepare()
    {
        EnsureView();
    }
}
