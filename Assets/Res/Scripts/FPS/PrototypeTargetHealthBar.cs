using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeUnitVitals))]
public class PrototypeTargetHealthBar : MonoBehaviour
{
    private const string HealthBarPrefabResourcePath = "UI/FPS/PrototypeTargetHealthBar";

    [SerializeField] private PrototypeUnitVitals vitals;
    [SerializeField] private PrototypeBotController botController;
    [SerializeField] private Transform anchor;
    [SerializeField] private string anchorPartId = string.Empty;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.34f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(84f, 10f);
    [SerializeField] private bool showWhenDead = true;
    [SerializeField] private Color borderColor = new Color(0f, 0f, 0f, 0.95f);
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
    [SerializeField] private Color lowHealthColor = new Color(0.84f, 0.18f, 0.17f, 1f);
    [SerializeField] private Color highHealthColor = new Color(0.18f, 0.85f, 0.31f, 1f);
    [SerializeField] private Color levelTextColor = new Color(1f, 1f, 1f, 0.96f);

    private RectTransform uiRoot;
    private PrototypeTargetHealthBarTemplate viewTemplate;
    private TMP_Text levelLabelText;
    private RectTransform borderRect;
    private Image backgroundImage;
    private Image fillImage;

    private void Awake()
    {
        ResolveReferences();
        ClampSettings();
        EnsureUi();
    }

    private void Reset()
    {
        ResolveReferences();
        ClampSettings();
    }

    private void OnValidate()
    {
        ResolveReferences();
        ClampSettings();
    }

    public void Configure(PrototypeUnitVitals ownerVitals, Transform anchorTransform, string preferredAnchorPartId = "")
    {
        vitals = ownerVitals;
        anchor = anchorTransform;
        anchorPartId = NormalizePartId(preferredAnchorPartId);

        if (anchor == null)
        {
            anchor = ResolveAnchorTransform();
        }

        ClampSettings();
    }

    private void LateUpdate()
    {
        UpdateHealthBarUi();
    }

    private void ResolveReferences()
    {
        if (vitals == null)
        {
            vitals = GetComponent<PrototypeUnitVitals>();
        }

        if (botController == null)
        {
            botController = GetComponent<PrototypeBotController>();
        }

        anchorPartId = NormalizePartId(anchorPartId);

        if (anchor == null)
        {
            anchor = ResolveAnchorTransform();
        }
    }

    private Transform ResolveAnchorTransform()
    {
        string preferredPartId = anchorPartId;
        if (string.IsNullOrWhiteSpace(preferredPartId) && vitals != null)
        {
            preferredPartId = NormalizePartId(vitals.HealthBarAnchorPartId);
        }

        PrototypeUnitHitbox[] hitboxes = GetComponentsInChildren<PrototypeUnitHitbox>(true);

        if (!string.IsNullOrWhiteSpace(preferredPartId))
        {
            foreach (PrototypeUnitHitbox hitbox in hitboxes)
            {
                if (hitbox != null && PartIdEquals(hitbox.PartId, preferredPartId))
                {
                    return hitbox.transform;
                }
            }
        }

        foreach (PrototypeUnitHitbox hitbox in hitboxes)
        {
            if (hitbox != null)
            {
                return hitbox.transform;
            }
        }

        return transform;
    }

    private void ClampSettings()
    {
        barSize.x = Mathf.Max(barSize.x, 16f);
        barSize.y = Mathf.Max(barSize.y, 4f);
    }

    private void EnsureUi()
    {
        if (uiRoot != null)
        {
            return;
        }

        PrototypeRuntimeUiManager manager = PrototypeRuntimeUiManager.GetOrCreate();
        GameObject prefabAsset = Resources.Load<GameObject>(HealthBarPrefabResourcePath);
        if (prefabAsset == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing target health bar prefab at Resources/{HealthBarPrefabResourcePath}.", this);
            return;
        }

        GameObject instanceObject = Instantiate(prefabAsset, manager.GetLayerRoot(PrototypeUiLayer.World), false);
        instanceObject.name = prefabAsset.name;

        viewTemplate = instanceObject.GetComponent<PrototypeTargetHealthBarTemplate>();
        if (viewTemplate == null || viewTemplate.Root == null || viewTemplate.LevelLabelText == null || viewTemplate.FillImage == null)
        {
            Destroy(instanceObject);
            viewTemplate = null;
            Debug.LogWarning($"[{GetType().Name}] Target health bar prefab is missing {nameof(PrototypeTargetHealthBarTemplate)}.", this);
            return;
        }

        PrototypeUiToolkit.ApplyFontRecursively(viewTemplate.Root, manager.RuntimeFont);
        uiRoot = viewTemplate.Root;
        levelLabelText = viewTemplate.LevelLabelText;
        borderRect = viewTemplate.BorderRect;
        backgroundImage = viewTemplate.BackgroundImage;
        fillImage = viewTemplate.FillImage;
        PrototypeUiToolkit.SetVisible(uiRoot, false);
    }

    private void UpdateHealthBarUi()
    {
        if (vitals == null)
        {
            SetUiVisible(false);
            return;
        }

        EnsureUi();

        if (anchor == null)
        {
            anchor = ResolveAnchorTransform();
        }

        if (!showWhenDead && vitals.IsDead)
        {
            SetUiVisible(false);
            return;
        }

        Camera renderCamera = Camera.main;
        if (renderCamera == null)
        {
            SetUiVisible(false);
            return;
        }

        Transform targetAnchor = anchor != null ? anchor : transform;
        Vector3 screenPosition = renderCamera.WorldToScreenPoint(targetAnchor.position + worldOffset);
        if (screenPosition.z <= 0f)
        {
            SetUiVisible(false);
            return;
        }

        PrototypeRuntimeUiManager manager = PrototypeRuntimeUiManager.GetOrCreate();
        PrototypeUiToolkit.SetScreenPosition(manager.CanvasRoot, uiRoot, new Vector2(screenPosition.x, screenPosition.y));
        uiRoot.sizeDelta = new Vector2(barSize.x + 24f, barSize.y + 24f);
        if (levelLabelText != null)
        {
            levelLabelText.rectTransform.sizeDelta = new Vector2(barSize.x + 36f, 16f);
        }

        if (borderRect != null)
        {
            borderRect.sizeDelta = barSize;
            Image borderImage = borderRect.GetComponent<Image>();
            if (borderImage != null)
            {
                borderImage.color = borderColor;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }
        }

        SetUiVisible(true);

        string levelLabel = BuildLevelLabel();
        if (levelLabelText != null)
        {
            levelLabelText.text = levelLabel ?? string.Empty;
            levelLabelText.color = levelTextColor;
            levelLabelText.gameObject.SetActive(!string.IsNullOrWhiteSpace(levelLabel));
        }

        if (fillImage == null)
        {
            return;
        }

        float fillAmount = vitals.TotalHealthNormalized;
        float innerWidth = Mathf.Max(0f, barSize.x - 2f);
        fillImage.color = Color.Lerp(lowHealthColor, highHealthColor, fillAmount);
        fillImage.rectTransform.sizeDelta = new Vector2(innerWidth * fillAmount, Mathf.Max(0f, barSize.y - 2f));
    }

    private string BuildLevelLabel()
    {
        if (botController == null)
        {
            return string.Empty;
        }

        return botController.IsBossProfile
            ? $"Boss Lv {botController.EnemyLevel}"
            : $"Lv {botController.EnemyLevel}";
    }

    private void SetUiVisible(bool visible)
    {
        PrototypeUiToolkit.SetVisible(uiRoot, visible);
    }

    private void OnDestroy()
    {
        if (uiRoot != null)
        {
            Destroy(uiRoot.gameObject);
        }

        viewTemplate = null;
        levelLabelText = null;
        borderRect = null;
        backgroundImage = null;
        fillImage = null;
    }

    private static string NormalizePartId(string partId)
    {
        return PrototypeUnitDefinition.NormalizePartId(partId);
    }

    private static bool PartIdEquals(string left, string right)
    {
        return string.Equals(NormalizePartId(left), NormalizePartId(right), StringComparison.OrdinalIgnoreCase);
    }
}
