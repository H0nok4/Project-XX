using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        public RectTransform root;
        public Text label;
        public Shadow shadow;
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
    private RectTransform worldLayerRoot;

    private void Awake()
    {
        ResolveReferences();
        ClampSettings();
        EnsureWorldLayerRoot();
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

        ClearActiveTexts();
    }

    private void OnValidate()
    {
        ResolveReferences();
        ClampSettings();
    }

    private void LateUpdate()
    {
        UpdateCombatTextUi();
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

        ActiveCombatText entry = new ActiveCombatText
        {
            text = text,
            color = color,
            worldPosition = worldPosition,
            spawnTime = Time.time
        };

        CreateCombatTextUi(entry);
        activeTexts.Add(entry);
    }

    private void UpdateCombatTextUi()
    {
        if (activeTexts.Count == 0)
        {
            return;
        }

        Camera renderCamera = Camera.main;
        if (renderCamera == null)
        {
            SetAllTextsVisible(false);
            return;
        }

        PrototypeRuntimeUiManager manager = PrototypeRuntimeUiManager.GetOrCreate();
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
                DestroyEntry(entry);
                activeTexts.RemoveAt(index);
                continue;
            }

            if (entry.root == null || entry.label == null)
            {
                CreateCombatTextUi(entry);
            }

            float normalizedLifetime = Mathf.Clamp01(elapsed / lifetime);
            Vector3 worldPosition = entry.worldPosition + Vector3.up * (verticalRise * normalizedLifetime);
            Vector3 screenPosition = renderCamera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f)
            {
                PrototypeUiToolkit.SetVisible(entry.root, false);
                continue;
            }

            Color drawColor = entry.color;
            drawColor.a *= 1f - normalizedLifetime;

            PrototypeUiToolkit.SetVisible(entry.root, true);
            PrototypeUiToolkit.SetScreenPosition(manager.CanvasRoot, entry.root, new Vector2(screenPosition.x, screenPosition.y));
            entry.label.text = entry.text;
            entry.label.fontSize = fontSize;
            entry.label.color = drawColor;

            if (entry.shadow != null)
            {
                Color drawShadow = shadowColor;
                drawShadow.a *= drawColor.a;
                entry.shadow.effectColor = drawShadow;
            }
        }
    }

    private void EnsureWorldLayerRoot()
    {
        if (worldLayerRoot != null)
        {
            return;
        }

        worldLayerRoot = PrototypeRuntimeUiManager.GetOrCreate().GetLayerRoot(PrototypeUiLayer.World);
    }

    private void CreateCombatTextUi(ActiveCombatText entry)
    {
        if (entry == null)
        {
            return;
        }

        EnsureWorldLayerRoot();
        PrototypeRuntimeUiManager manager = PrototypeRuntimeUiManager.GetOrCreate();

        if (entry.root == null)
        {
            entry.root = PrototypeUiToolkit.CreateRectTransform("CombatText", worldLayerRoot);
            PrototypeUiToolkit.SetAnchor(
                entry.root,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(132f, 40f));
        }

        if (entry.label == null)
        {
            entry.label = PrototypeUiToolkit.CreateText(
                entry.root,
                manager.RuntimeFont,
                entry.text,
                fontSize,
                FontStyle.Bold,
                entry.color,
                TextAnchor.MiddleCenter,
                false,
                false);
            PrototypeUiToolkit.SetStretch(entry.label.rectTransform, 0f, 0f, 0f, 0f);
            entry.shadow = entry.label.gameObject.AddComponent<Shadow>();
            entry.shadow.effectDistance = new Vector2(1f, -1f);
            entry.shadow.useGraphicAlpha = true;
        }
    }

    private void ClearActiveTexts()
    {
        for (int index = activeTexts.Count - 1; index >= 0; index--)
        {
            DestroyEntry(activeTexts[index]);
        }

        activeTexts.Clear();
    }

    private void SetAllTextsVisible(bool visible)
    {
        for (int index = 0; index < activeTexts.Count; index++)
        {
            ActiveCombatText entry = activeTexts[index];
            if (entry?.root != null)
            {
                PrototypeUiToolkit.SetVisible(entry.root, visible);
            }
        }
    }

    private static void DestroyEntry(ActiveCombatText entry)
    {
        if (entry?.root != null)
        {
            UnityEngine.Object.Destroy(entry.root.gameObject);
            entry.root = null;
            entry.label = null;
            entry.shadow = null;
        }
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

    private void OnDestroy()
    {
        ClearActiveTexts();
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
