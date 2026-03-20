using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RaidHudView : ViewBase
{
    private const string ViewPrefabResourcePath = "UI/Raid/RaidHud";
    private const float ExtractionWidthFallback = 294f;

    private static RaidHudView instance;

    private RaidHudTemplate viewTemplate;
    private TMP_Text summaryText;
    private RectTransform extractionRoot;
    private TMP_Text extractionText;
    private RectTransform extractionTrack;
    private Image extractionFillImage;

    public static RaidHudView GetOrCreate()
    {
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        instance = FindFirstObjectByType<RaidHudView>();
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        GameObject root = new GameObject("RaidHudView");
        instance = root.AddComponent<RaidHudView>();
        instance.Prepare();
        return instance;
    }

    protected override PrototypeUiLayer Layer => PrototypeUiLayer.Hud;
    protected override bool AddTransparentRootGraphic => false;
    protected override bool VisibleOnAwake => false;
    protected override bool RootGraphicRaycastTarget => false;
    protected override bool ApplyRuntimeFontToChildren => true;
    protected override string ViewName => "RaidHud";

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

        viewTemplate = instanceObject.GetComponent<RaidHudTemplate>();
        if (viewTemplate == null || viewTemplate.Root == null)
        {
            Destroy(instanceObject);
            viewTemplate = null;
            Debug.LogWarning($"[{GetType().Name}] Raid HUD prefab is missing {nameof(RaidHudTemplate)}.", this);
            return null;
        }

        return viewTemplate.Root;
    }

    public void UpdateHud(
        bool visible,
        string summary,
        bool showExtraction,
        string extractionLabel,
        float extractionProgress)
    {
        Prepare();

        if (summaryText != null)
        {
            summaryText.text = summary ?? string.Empty;
        }

        if (extractionRoot != null)
        {
            extractionRoot.gameObject.SetActive(showExtraction);
        }

        if (showExtraction && extractionText != null)
        {
            extractionText.text = extractionLabel ?? string.Empty;
        }

        if (extractionFillImage != null)
        {
            float fillWidth = ResolveTrackWidth(extractionTrack, ExtractionWidthFallback);
            extractionFillImage.rectTransform.sizeDelta = new Vector2(fillWidth * Mathf.Clamp01(extractionProgress), 0f);
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
            viewTemplate = root.GetComponent<RaidHudTemplate>();
        }

        if (viewTemplate == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing {nameof(RaidHudTemplate)} on instantiated view root.", this);
            return;
        }

        summaryText = viewTemplate.SummaryText;
        extractionRoot = viewTemplate.ExtractionRoot;
        extractionText = viewTemplate.ExtractionText;
        extractionTrack = viewTemplate.ExtractionTrack;
        extractionFillImage = viewTemplate.ExtractionFillImage;
        ApplyNonBlockingCanvasGroup();
    }

    protected override void OnViewVisibilityChanged(bool visible)
    {
        base.OnViewVisibilityChanged(visible);
        ApplyNonBlockingCanvasGroup();
    }

    protected override void OnViewRootDestroyed()
    {
        viewTemplate = null;
        summaryText = null;
        extractionRoot = null;
        extractionText = null;
        extractionTrack = null;
        extractionFillImage = null;
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

    private void ApplyNonBlockingCanvasGroup()
    {
        if (RootCanvasGroup == null)
        {
            return;
        }

        RootCanvasGroup.interactable = false;
        RootCanvasGroup.blocksRaycasts = false;
    }

    private static float ResolveTrackWidth(RectTransform track, float fallback)
    {
        if (track == null)
        {
            return fallback;
        }

        float width = track.rect.width;
        if (width <= 0f)
        {
            width = track.sizeDelta.x;
        }

        return width > 0f ? width : fallback;
    }
}
