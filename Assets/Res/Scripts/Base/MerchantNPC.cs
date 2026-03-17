using UnityEngine;

[DisallowMultipleComponent]
public sealed class MerchantNPC : MonoBehaviour, IInteractable
{
    [SerializeField] private BaseHubMerchantSpot merchantSpot;
    [SerializeField] private MerchantUIManager uiManager;
    [SerializeField] private string merchantIdOverride = string.Empty;
    [SerializeField] private string merchantNameOverride = string.Empty;
    [SerializeField] private string interactionLabelOverride = string.Empty;
    [TextArea(1, 3)]
    [SerializeField] private string greetingLine = "商人抬头看了你一眼，示意你可以开始交易。";
    [SerializeField] private float interactionRange = 3f;

    public string MerchantId
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(merchantIdOverride))
            {
                return merchantIdOverride.Trim();
            }

            return merchantSpot != null ? merchantSpot.MerchantId : string.Empty;
        }
    }

    public string MerchantName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(merchantNameOverride))
            {
                return merchantNameOverride.Trim();
            }

            return merchantSpot != null ? merchantSpot.MerchantName : "商人";
        }
    }

    public string GreetingLine => string.IsNullOrWhiteSpace(greetingLine) ? string.Empty : greetingLine.Trim();
    public BaseHubMerchantSpotType MerchantType => merchantSpot != null ? merchantSpot.SpotType : BaseHubMerchantSpotType.General;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
        interactionRange = Mathf.Max(1.25f, interactionRange);
        merchantIdOverride ??= string.Empty;
        merchantNameOverride ??= string.Empty;
        interactionLabelOverride ??= string.Empty;
        greetingLine ??= string.Empty;
    }

    public string GetInteractionLabel(PlayerInteractor interactor)
    {
        if (!string.IsNullOrWhiteSpace(interactionLabelOverride))
        {
            return interactionLabelOverride.Trim();
        }

        return $"与{MerchantName}交易";
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        ResolveReferences();
        if (uiManager == null || string.IsNullOrWhiteSpace(MerchantId))
        {
            return false;
        }

        if (interactor == null)
        {
            return true;
        }

        Transform interactionTransform = GetInteractionTransform();
        Transform interactorTransform = interactor.transform;
        if (interactionTransform == null || interactorTransform == null)
        {
            return true;
        }

        Vector3 planarDelta = interactorTransform.position - interactionTransform.position;
        planarDelta.y = 0f;
        return planarDelta.sqrMagnitude <= interactionRange * interactionRange;
    }

    public void Interact(PlayerInteractor interactor)
    {
        ResolveReferences();
        uiManager?.OpenMerchant(this);
    }

    public Transform GetInteractionTransform()
    {
        return merchantSpot != null ? merchantSpot.StandAnchor : transform;
    }

    private void ResolveReferences()
    {
        if (merchantSpot == null)
        {
            merchantSpot = GetComponent<BaseHubMerchantSpot>();
        }

        if (uiManager == null)
        {
            uiManager = MerchantUIManager.Instance;
            if (uiManager == null)
            {
                uiManager = FindFirstObjectByType<MerchantUIManager>();
            }
        }
    }
}
