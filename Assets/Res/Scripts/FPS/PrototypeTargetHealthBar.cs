using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeUnitVitals))]
public class PrototypeTargetHealthBar : MonoBehaviour
{
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
    private Text levelLabelText;
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
        uiRoot = PrototypeUiToolkit.CreateRectTransform($"{name}_HealthBar", manager.GetLayerRoot(PrototypeUiLayer.World));
        PrototypeUiToolkit.SetAnchor(uiRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(barSize.x + 24f, barSize.y + 24f));

        levelLabelText = PrototypeUiToolkit.CreateText(uiRoot, manager.RuntimeFont, string.Empty, 11, FontStyle.Bold, levelTextColor, TextAnchor.MiddleCenter);
        PrototypeUiToolkit.SetAnchor(levelLabelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(barSize.x + 36f, 16f));

        Image borderImage = PrototypeUiToolkit.CreateImage(uiRoot, "Border", borderColor);
        PrototypeUiToolkit.SetAnchor(borderImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), barSize);

        Image backgroundImage = PrototypeUiToolkit.CreateImage(borderImage.transform, "Background", backgroundColor);
        PrototypeUiToolkit.SetStretch(backgroundImage.rectTransform, 1f, 1f, 1f, 1f);

        fillImage = PrototypeUiToolkit.CreateImage(backgroundImage.transform, "Fill", highHealthColor);
        fillImage.rectTransform.anchorMin = new Vector2(0f, 0f);
        fillImage.rectTransform.anchorMax = new Vector2(0f, 1f);
        fillImage.rectTransform.pivot = new Vector2(0f, 0.5f);
        fillImage.rectTransform.offsetMin = Vector2.zero;
        fillImage.rectTransform.offsetMax = Vector2.zero;
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
