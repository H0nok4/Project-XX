using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class ExtractionZone : MonoBehaviour
{
    [SerializeField] private RaidGameMode raidGameMode;
    [SerializeField] private string extractionName = "Extraction";
    [SerializeField] private float extractionDurationSeconds = 4f;
    [SerializeField] private bool requirePlayerInsideVolume = true;
    [SerializeField] private Collider triggerVolume;

    private readonly HashSet<PlayerInteractor> overlappingInteractors = new HashSet<PlayerInteractor>();
    private PlayerInteractor extractingInteractor;
    private float extractionRemainingSeconds;

    public string ExtractionName => string.IsNullOrWhiteSpace(extractionName) ? name : extractionName.Trim();
    public bool HasActiveExtraction => extractingInteractor != null;
    public float ExtractionDurationSeconds => Mathf.Max(0.1f, extractionDurationSeconds);
    public float ExtractionRemainingSeconds => HasActiveExtraction ? Mathf.Max(0f, extractionRemainingSeconds) : 0f;
    public float ExtractionProgressNormalized => HasActiveExtraction
        ? Mathf.Clamp01(1f - (ExtractionRemainingSeconds / ExtractionDurationSeconds))
        : 0f;

    private void Awake()
    {
        ResolveReferences();
        EnsureTriggerVolume();
    }

    private void OnEnable()
    {
        ResolveReferences();
        raidGameMode?.RegisterExtractionZone(this);
    }

    private void OnDisable()
    {
        raidGameMode?.UnregisterExtractionZone(this);
        overlappingInteractors.Clear();
        CancelExtraction();
    }

    private void OnValidate()
    {
        ResolveReferences();
        EnsureTriggerVolume();
        extractionDurationSeconds = Mathf.Max(0.1f, extractionDurationSeconds);
    }

    public void Configure(RaidGameMode gameMode, string zoneName, string promptLabel = "Extract", bool requiresInsideVolume = true)
    {
        raidGameMode = gameMode;
        extractionName = string.IsNullOrWhiteSpace(zoneName) ? name : zoneName.Trim();
        requirePlayerInsideVolume = requiresInsideVolume;
        ResolveReferences();
        EnsureTriggerVolume();
    }

    private void Update()
    {
        if (HasActiveExtraction)
        {
            if (!CanContinueExtraction(extractingInteractor))
            {
                CancelExtraction();
                return;
            }

            extractionRemainingSeconds = Mathf.Max(0f, extractionRemainingSeconds - Time.deltaTime);
            if (extractionRemainingSeconds <= 0f)
            {
                PlayerInteractor completedInteractor = extractingInteractor;
                CancelExtraction();
                raidGameMode?.TryExtract(completedInteractor, this);
            }

            return;
        }

        foreach (PlayerInteractor interactor in overlappingInteractors)
        {
            if (CanContinueExtraction(interactor))
            {
                StartExtraction(interactor);
                break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInteractor interactor = other.GetComponentInParent<PlayerInteractor>();
        if (interactor != null)
        {
            if (overlappingInteractors.Add(interactor) && CanContinueExtraction(interactor))
            {
                StartExtraction(interactor);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInteractor interactor = other.GetComponentInParent<PlayerInteractor>();
        if (interactor != null)
        {
            overlappingInteractors.Remove(interactor);
            if (interactor == extractingInteractor)
            {
                CancelExtraction();
            }
        }
    }

    private void ResolveReferences()
    {
        if (raidGameMode == null)
        {
            raidGameMode = FindFirstObjectByType<RaidGameMode>();
        }

        if (triggerVolume == null)
        {
            triggerVolume = GetComponent<Collider>();
        }

        extractionName = string.IsNullOrWhiteSpace(extractionName) ? name : extractionName.Trim();
    }

    private void EnsureTriggerVolume()
    {
        if (triggerVolume != null)
        {
            triggerVolume.isTrigger = true;
        }
    }

    private bool CanContinueExtraction(PlayerInteractor interactor)
    {
        if (interactor == null || raidGameMode == null || !raidGameMode.CanExtract(interactor, this))
        {
            return false;
        }

        return !requirePlayerInsideVolume || overlappingInteractors.Contains(interactor);
    }

    private void StartExtraction(PlayerInteractor interactor)
    {
        if (interactor == null)
        {
            return;
        }

        extractingInteractor = interactor;
        extractionRemainingSeconds = ExtractionDurationSeconds;
    }

    private void CancelExtraction()
    {
        extractingInteractor = null;
        extractionRemainingSeconds = 0f;
    }
}
