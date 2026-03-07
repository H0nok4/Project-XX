using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PrototypeUnitVitals))]
public class PrototypeCombatTextController : MonoBehaviour
{
    [Serializable]
    private sealed class ActiveCombatText
    {
        public string text = string.Empty;
        public Color color = Color.white;
        public Vector3 worldPosition;
        public float spawnTime;
    }

    [Header("References")]
    [SerializeField] private PrototypeUnitVitals vitals;

    [Header("Layout")]
    [SerializeField] private Vector3 baseWorldOffset = new Vector3(0f, 0.55f, 0f);
    [SerializeField] private float verticalRise = 0.75f;
    [SerializeField] private float randomHorizontalOffset = 0.18f;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private int fontSize = 19;

    [Header("Colors")]
    [SerializeField] private Color armorDamageColor = new Color(0.74f, 0.74f, 0.74f, 1f);
    [SerializeField] private Color armorBrokenColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private Color healthDamageColor = new Color(0.92f, 0.2f, 0.18f, 1f);
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.8f);

    private readonly List<ActiveCombatText> activeTexts = new List<ActiveCombatText>();
    private GUIStyle textStyle;

    private void Awake()
    {
        ResolveReferences();
        ClampSettings();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (vitals != null)
        {
            vitals.CombatFeedbackGenerated += HandleCombatFeedback;
        }
    }

    private void OnDisable()
    {
        if (vitals != null)
        {
            vitals.CombatFeedbackGenerated -= HandleCombatFeedback;
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
        ClampSettings();
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || activeTexts.Count == 0)
        {
            return;
        }

        Camera renderCamera = Camera.main;
        if (renderCamera == null)
        {
            return;
        }

        EnsureTextStyle();

        float currentTime = Time.time;
        for (int index = activeTexts.Count - 1; index >= 0; index--)
        {
            ActiveCombatText entry = activeTexts[index];
            if (entry == null)
            {
                activeTexts.RemoveAt(index);
                continue;
            }

            float elapsed = currentTime - entry.spawnTime;
            if (elapsed >= lifetime)
            {
                activeTexts.RemoveAt(index);
                continue;
            }

            float normalizedLifetime = Mathf.Clamp01(elapsed / lifetime);
            Vector3 worldPosition = entry.worldPosition + Vector3.up * (verticalRise * normalizedLifetime);
            Vector3 screenPosition = renderCamera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f)
            {
                continue;
            }

            Color drawColor = entry.color;
            drawColor.a *= 1f - normalizedLifetime;

            Rect rect = new Rect(screenPosition.x - 56f, Screen.height - screenPosition.y - 16f, 112f, 32f);
            DrawLabel(rect, entry.text, shadowColor * new Color(1f, 1f, 1f, drawColor.a), new Vector2(1f, 1f));
            DrawLabel(rect, entry.text, drawColor, Vector2.zero);
        }
    }

    private void HandleCombatFeedback(PrototypeUnitVitals.CombatFeedback feedback)
    {
        string text = string.IsNullOrWhiteSpace(feedback.text) ? "-0" : feedback.text;
        Color color = ResolveColor(feedback.kind);
        Vector3 worldPosition = ResolveAnchorWorldPosition(feedback.partId);

        worldPosition += new Vector3(
            UnityEngine.Random.Range(-randomHorizontalOffset, randomHorizontalOffset),
            UnityEngine.Random.Range(0f, randomHorizontalOffset),
            0f);

        activeTexts.Add(new ActiveCombatText
        {
            text = text,
            color = color,
            worldPosition = worldPosition,
            spawnTime = Time.time
        });
    }

    private Vector3 ResolveAnchorWorldPosition(string partId)
    {
        string normalizedPartId = NormalizePartId(partId);
        PrototypeUnitHitbox[] hitboxes = GetComponentsInChildren<PrototypeUnitHitbox>(true);
        foreach (PrototypeUnitHitbox hitbox in hitboxes)
        {
            if (hitbox != null && PartIdEquals(hitbox.PartId, normalizedPartId))
            {
                return hitbox.transform.position + baseWorldOffset;
            }
        }

        return transform.position + baseWorldOffset;
    }

    private Color ResolveColor(PrototypeUnitVitals.CombatFeedbackKind kind)
    {
        switch (kind)
        {
            case PrototypeUnitVitals.CombatFeedbackKind.ArmorDamage:
                return armorDamageColor;
            case PrototypeUnitVitals.CombatFeedbackKind.ArmorBroken:
                return armorBrokenColor;
            default:
                return healthDamageColor;
        }
    }

    private void ResolveReferences()
    {
        if (vitals == null)
        {
            vitals = GetComponent<PrototypeUnitVitals>();
        }
    }

    private void ClampSettings()
    {
        verticalRise = Mathf.Max(0.1f, verticalRise);
        randomHorizontalOffset = Mathf.Max(0f, randomHorizontalOffset);
        lifetime = Mathf.Max(0.1f, lifetime);
        fontSize = Mathf.Clamp(fontSize, 10, 40);
    }

    private void EnsureTextStyle()
    {
        if (textStyle != null)
        {
            textStyle.fontSize = fontSize;
            return;
        }

        textStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize,
            fontStyle = FontStyle.Bold
        };
    }

    private void DrawLabel(Rect rect, string text, Color color, Vector2 offset)
    {
        Rect drawRect = new Rect(rect.x + offset.x, rect.y + offset.y, rect.width, rect.height);
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.Label(drawRect, text, textStyle);
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
