using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeUnitVitals))]
public class PrototypeTargetHealthBar : MonoBehaviour
{
    [SerializeField] private PrototypeUnitVitals vitals;
    [SerializeField] private Transform anchor;
    [SerializeField] private string anchorPartId = string.Empty;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.34f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(84f, 10f);
    [SerializeField] private bool showWhenDead = true;
    [SerializeField] private Color borderColor = new Color(0f, 0f, 0f, 0.95f);
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
    [SerializeField] private Color lowHealthColor = new Color(0.84f, 0.18f, 0.17f, 1f);
    [SerializeField] private Color highHealthColor = new Color(0.18f, 0.85f, 0.31f, 1f);

    private static Texture2D pixelTexture;

    private void Awake()
    {
        ResolveReferences();
        ClampSettings();
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

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || vitals == null)
        {
            return;
        }

        if (anchor == null)
        {
            anchor = ResolveAnchorTransform();
        }

        if (!showWhenDead && vitals.IsDead)
        {
            return;
        }

        Camera renderCamera = Camera.main;
        if (renderCamera == null)
        {
            return;
        }

        Transform targetAnchor = anchor != null ? anchor : transform;
        Vector3 screenPosition = renderCamera.WorldToScreenPoint(targetAnchor.position + worldOffset);
        if (screenPosition.z <= 0f)
        {
            return;
        }

        float width = barSize.x;
        float height = barSize.y;
        float top = Screen.height - screenPosition.y;
        Rect outerRect = new Rect(screenPosition.x - width * 0.5f, top - height * 0.5f, width, height);
        Rect innerRect = new Rect(outerRect.x + 1f, outerRect.y + 1f, outerRect.width - 2f, outerRect.height - 2f);

        DrawRect(outerRect, borderColor);
        DrawRect(innerRect, backgroundColor);

        float fillAmount = vitals.TotalHealthNormalized;
        if (fillAmount <= 0f)
        {
            return;
        }

        Rect fillRect = new Rect(innerRect.x, innerRect.y, innerRect.width * fillAmount, innerRect.height);
        Color fillColor = Color.Lerp(lowHealthColor, highHealthColor, fillAmount);
        DrawRect(fillRect, fillColor);
    }

    private void ResolveReferences()
    {
        if (vitals == null)
        {
            vitals = GetComponent<PrototypeUnitVitals>();
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

    private static void DrawRect(Rect rect, Color color)
    {
        if (pixelTexture == null)
        {
            pixelTexture = Texture2D.whiteTexture;
        }

        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pixelTexture);
        GUI.color = previousColor;
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
