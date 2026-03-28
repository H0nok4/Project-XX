using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMedicalController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool useHostSettings = true;
    [SerializeField] private float medicalUseCooldown = 0.28f;
    [SerializeField] private float medicalFeedbackLifetime = 1.4f;

    private PrototypeUnitVitals playerVitals;
    private InventoryContainer inventory;
    private float nextMedicalUseTime;
    private float medicalFeedbackTimer;
    private string medicalFeedbackMessage = string.Empty;
    private bool medicalTriggeredThisFrame;

    public string FeedbackMessage => medicalFeedbackMessage;
    public bool MedicalTriggeredThisFrame => medicalTriggeredThisFrame;

    private void Awake()
    {
        ResolveReferences();
        EnsureMedicalSettings();
    }

    private void OnValidate()
    {
        EnsureMedicalSettings();
    }

    public void SetPlayerDependencies(PrototypeUnitVitals vitals, InventoryContainer inventoryContainer)
    {
        playerVitals = vitals;
        inventory = inventoryContainer;
    }

    public void ApplyHostSettings(float hostMedicalUseCooldown, float hostMedicalFeedbackLifetime)
    {
        if (useHostSettings)
        {
            medicalUseCooldown = hostMedicalUseCooldown;
            medicalFeedbackLifetime = hostMedicalFeedbackLifetime;
        }

        EnsureMedicalSettings();
    }

    public void BeginFrame()
    {
        medicalTriggeredThisFrame = false;
    }

    public bool HandleMedicalInput(PrototypeFpsInput fpsInput)
    {
        EnsureReferences();

        if (fpsInput == null || playerVitals == null || inventory == null || Time.time < nextMedicalUseTime)
        {
            return false;
        }

        bool usedMedical;
        if (fpsInput.StopBleedPressedThisFrame)
        {
            usedMedical = TryUseBleedTreatment();
            medicalTriggeredThisFrame |= usedMedical;
            return usedMedical;
        }

        if (fpsInput.SplintPressedThisFrame)
        {
            usedMedical = TryUseSplint();
            medicalTriggeredThisFrame |= usedMedical;
            return usedMedical;
        }

        if (fpsInput.PainkillerPressedThisFrame)
        {
            usedMedical = TryUsePainkiller();
            medicalTriggeredThisFrame |= usedMedical;
            return usedMedical;
        }

        if (fpsInput.QuickHealPressedThisFrame)
        {
            usedMedical = TryUseQuickHeal();
            medicalTriggeredThisFrame |= usedMedical;
            return usedMedical;
        }

        return false;
    }

    public void TickFeedback(float deltaTime)
    {
        if (medicalFeedbackTimer <= 0f)
        {
            return;
        }

        medicalFeedbackTimer -= deltaTime;
        if (medicalFeedbackTimer <= 0f)
        {
            medicalFeedbackMessage = string.Empty;
        }
    }

    private bool TryUseQuickHeal()
    {
        bool needsHealing = playerVitals.TotalCurrentHealth < playerVitals.TotalMaxHealth - 0.5f;
        if (!needsHealing && !playerVitals.HasAnyBleed && !playerVitals.HasFracture)
        {
            SetMedicalFeedback("当前无需治疗");
            return false;
        }

        return TryUseMedicalItem(
            medical => medical.HasHealing
                || (playerVitals.HasHeavyBleed && medical.RemovesHeavyBleeds > 0)
                || (playerVitals.HasLightBleed && medical.RemovesLightBleeds > 0)
                || (playerVitals.HasFracture && medical.CuresFractures > 0)
                || medical.PainkillerDuration > 0f,
            medical =>
            {
                float healingValue = GetHealingValue(medical);
                float score = healingValue * 0.6f;
                if (needsHealing)
                {
                    score += healingValue;
                }

                if (playerVitals.HasHeavyBleed)
                {
                    score += medical.RemovesHeavyBleeds * 150f;
                }

                if (playerVitals.HasLightBleed)
                {
                    score += medical.RemovesLightBleeds * 60f;
                }

                if (playerVitals.HasFracture)
                {
                    score += medical.CuresFractures * 120f;
                }

                score += medical.PainkillerDuration * 0.5f;
                return score;
            },
            "没有可用治疗物品");
    }

    private bool TryUseBleedTreatment()
    {
        if (!playerVitals.HasAnyBleed)
        {
            SetMedicalFeedback("当前没有出血");
            return false;
        }

        bool hasHeavyBleed = playerVitals.HasHeavyBleed;
        return TryUseMedicalItem(
            medical => hasHeavyBleed ? medical.RemovesHeavyBleeds > 0 : medical.RemovesLightBleeds > 0 || medical.RemovesHeavyBleeds > 0,
            medical =>
            {
                float score = 0f;
                if (hasHeavyBleed)
                {
                    score += medical.RemovesHeavyBleeds * 200f;
                    score += medical.RemovesLightBleeds * 25f;
                }
                else
                {
                    score += medical.RemovesLightBleeds * 120f;
                    score += medical.RemovesHeavyBleeds * 80f;
                }

                score += GetHealingValue(medical) * 0.1f;
                return score;
            },
            "没有止血物品");
    }

    private bool TryUseSplint()
    {
        if (!playerVitals.HasFracture)
        {
            SetMedicalFeedback("当前没有骨折");
            return false;
        }

        return TryUseMedicalItem(
            medical => medical.CuresFractures > 0,
            medical => medical.CuresFractures * 100f + medical.PainkillerDuration * 0.1f,
            "没有夹板");
    }

    private bool TryUsePainkiller()
    {
        if (playerVitals.IsPainkillerActive && playerVitals.PainkillerRemaining > 8f)
        {
            SetMedicalFeedback("止痛效果仍在持续");
            return false;
        }

        return TryUseMedicalItem(
            medical => medical.PainkillerDuration > 0f,
            medical => medical.PainkillerDuration + GetHealingValue(medical) * 0.1f,
            "没有止痛药");
    }

    private float GetHealingValue(MedicalItemDefinition medical)
    {
        return medical != null && playerVitals != null ? medical.GetHealingAmount(playerVitals.TotalMaxHealth) * playerVitals.MedicalEffectivenessMultiplier : 0f;
    }

    private bool TryUseMedicalItem(
        Func<MedicalItemDefinition, bool> predicate,
        Func<MedicalItemDefinition, float> scoreSelector,
        string missingItemMessage)
    {
        MedicalItemDefinition bestMedicalItem = FindBestMedicalItem(predicate, scoreSelector);
        if (bestMedicalItem == null)
        {
            SetMedicalFeedback(missingItemMessage);
            return false;
        }

        if (!playerVitals.TryUseMedicalItem(bestMedicalItem))
        {
            SetMedicalFeedback("当前无需使用该治疗物品");
            return false;
        }

        if (!inventory.TryRemoveItem(bestMedicalItem, 1, out int removedQuantity) || removedQuantity <= 0)
        {
            SetMedicalFeedback("治疗物品同步失败");
            return false;
        }

        nextMedicalUseTime = Time.time + medicalUseCooldown;
        SetMedicalFeedback($"已使用 {bestMedicalItem.DisplayName}");
        return true;
    }

    private MedicalItemDefinition FindBestMedicalItem(
        Func<MedicalItemDefinition, bool> predicate,
        Func<MedicalItemDefinition, float> scoreSelector)
    {
        if (inventory == null || predicate == null || scoreSelector == null)
        {
            return null;
        }

        MedicalItemDefinition bestMedicalItem = null;
        float bestScore = float.NegativeInfinity;

        foreach (ItemInstance item in inventory.Items)
        {
            if (item == null || !(item.Definition is MedicalItemDefinition medicalItem) || item.Quantity <= 0)
            {
                continue;
            }

            if (!predicate(medicalItem))
            {
                continue;
            }

            float score = scoreSelector(medicalItem);
            if (score > bestScore)
            {
                bestScore = score;
                bestMedicalItem = medicalItem;
            }
        }

        return bestMedicalItem;
    }

    private void SetMedicalFeedback(string message)
    {
        medicalFeedbackMessage = message ?? string.Empty;
        medicalFeedbackTimer = medicalFeedbackLifetime;
    }

    private void EnsureMedicalSettings()
    {
        medicalUseCooldown = Mathf.Max(0.05f, medicalUseCooldown);
        medicalFeedbackLifetime = Mathf.Max(0.25f, medicalFeedbackLifetime);
    }

    private void ResolveReferences()
    {
        if (playerVitals == null)
        {
            playerVitals = GetComponent<PrototypeUnitVitals>();
        }

        if (inventory == null)
        {
            inventory = GetComponent<InventoryContainer>();
        }
    }

    private void EnsureReferences()
    {
        if (playerVitals == null || inventory == null)
        {
            ResolveReferences();
        }
    }
}
