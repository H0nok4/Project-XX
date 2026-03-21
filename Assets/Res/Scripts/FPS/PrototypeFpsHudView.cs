using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PrototypeFpsHudView : ViewBase
{
    private const string ViewPrefabResourcePath = "UI/FPS/PrototypeFpsHud";
    private const float StaminaWidthFallback = 274f;

    private static PrototypeFpsHudView instance;

    private PrototypeFpsHudTemplate viewTemplate;
    private TMP_Text crosshairText;
    private RectTransform staminaTrack;
    private Image staminaFillImage;
    private TMP_Text staminaLabelText;
    private TMP_Text weaponInfoText;

    public static PrototypeFpsHudView GetOrCreate()
    {
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        instance = FindFirstObjectByType<PrototypeFpsHudView>();
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        GameObject root = new GameObject("PrototypeFpsHudView");
        instance = root.AddComponent<PrototypeFpsHudView>();
        instance.Prepare();
        return instance;
    }

    protected override PrototypeUiLayer Layer => PrototypeUiLayer.Hud;
    protected override bool AddTransparentRootGraphic => false;
    protected override bool VisibleOnAwake => false;
    protected override bool RootGraphicRaycastTarget => false;
    protected override bool ApplyRuntimeFontToChildren => true;
    protected override string ViewName => "PrototypeFpsHud";

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

        viewTemplate = instanceObject.GetComponent<PrototypeFpsHudTemplate>();
        if (viewTemplate == null || viewTemplate.Root == null)
        {
            Destroy(instanceObject);
            viewTemplate = null;
            Debug.LogWarning($"[{GetType().Name}] FPS HUD prefab is missing {nameof(PrototypeFpsHudTemplate)}.", this);
            return null;
        }

        return viewTemplate.Root;
    }

    public void UpdateHud(
        bool visible,
        bool showCrosshair,
        bool showHitMarker,
        float staminaNormalized,
        Color staminaColor,
        string staminaLabel,
        string weaponInfo)
    {
        Prepare();

        if (crosshairText != null)
        {
            crosshairText.text = showHitMarker ? "X" : (showCrosshair ? "+" : string.Empty);
        }

        if (staminaFillImage != null)
        {
            float fillWidth = ResolveTrackWidth(staminaTrack, StaminaWidthFallback);
            staminaFillImage.rectTransform.sizeDelta = new Vector2(fillWidth * Mathf.Clamp01(staminaNormalized), 0f);
            staminaFillImage.color = staminaColor;
        }

        if (staminaLabelText != null)
        {
            staminaLabelText.text = staminaLabel ?? string.Empty;
        }

        if (weaponInfoText != null)
        {
            weaponInfoText.text = weaponInfo ?? string.Empty;
        }

        SetViewVisible(visible);
    }

    public void SetHudVisible(bool visible)
    {
        Prepare();
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
            viewTemplate = root.GetComponent<PrototypeFpsHudTemplate>();
        }

        if (viewTemplate == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing {nameof(PrototypeFpsHudTemplate)} on instantiated view root.", this);
            return;
        }

        crosshairText = viewTemplate.CrosshairText;
        staminaTrack = viewTemplate.StaminaTrack;
        staminaFillImage = viewTemplate.StaminaFillImage;
        staminaLabelText = viewTemplate.StaminaLabelText;
        weaponInfoText = viewTemplate.WeaponInfoText;
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
        crosshairText = null;
        staminaTrack = null;
        staminaFillImage = null;
        staminaLabelText = null;
        weaponInfoText = null;
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
