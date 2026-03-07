using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class ExtractionZone : MonoBehaviour, IInteractable
{
    [SerializeField] private RaidGameMode raidGameMode;
    [SerializeField] private string extractionName = "Extraction";
    [SerializeField] private string interactionLabel = "Extract";
    [SerializeField] private bool requirePlayerInsideVolume = true;
    [SerializeField] private Collider triggerVolume;

    private readonly HashSet<PlayerInteractor> overlappingInteractors = new HashSet<PlayerInteractor>();

    public string ExtractionName => string.IsNullOrWhiteSpace(extractionName) ? name : extractionName.Trim();

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
    }

    private void OnValidate()
    {
        ResolveReferences();
        EnsureTriggerVolume();
    }

    public void Configure(RaidGameMode gameMode, string zoneName, string promptLabel = "Extract", bool requiresInsideVolume = true)
    {
        raidGameMode = gameMode;
        extractionName = string.IsNullOrWhiteSpace(zoneName) ? name : zoneName.Trim();
        interactionLabel = string.IsNullOrWhiteSpace(promptLabel) ? "Extract" : promptLabel.Trim();
        requirePlayerInsideVolume = requiresInsideVolume;
        ResolveReferences();
        EnsureTriggerVolume();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInteractor interactor = other.GetComponentInParent<PlayerInteractor>();
        if (interactor != null)
        {
            overlappingInteractors.Add(interactor);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInteractor interactor = other.GetComponentInParent<PlayerInteractor>();
        if (interactor != null)
        {
            overlappingInteractors.Remove(interactor);
        }
    }

    public string GetInteractionLabel(PlayerInteractor interactor)
    {
        return $"{interactionLabel} {ExtractionName}";
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        if (raidGameMode == null || !raidGameMode.CanExtract(interactor, this))
        {
            return false;
        }

        return !requirePlayerInsideVolume || overlappingInteractors.Contains(interactor);
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (CanInteract(interactor))
        {
            raidGameMode.TryExtract(interactor, this);
        }
    }

    public Transform GetInteractionTransform()
    {
        return transform;
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

        interactionLabel = string.IsNullOrWhiteSpace(interactionLabel) ? "Extract" : interactionLabel.Trim();
        extractionName = string.IsNullOrWhiteSpace(extractionName) ? name : extractionName.Trim();
    }

    private void EnsureTriggerVolume()
    {
        if (triggerVolume != null)
        {
            triggerVolume.isTrigger = true;
        }
    }
}
