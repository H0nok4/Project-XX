using UnityEngine;

[DisallowMultipleComponent]
public class PrototypeDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private string doorLabel = "Door";
    [SerializeField] private string openVerb = "Open";
    [SerializeField] private string closeVerb = "Close";
    [SerializeField] private Transform hinge;
    [SerializeField] private Collider interactionCollider;
    [SerializeField] private bool startsOpen;
    [SerializeField] private bool locked;
    [SerializeField] private float openAngle = 96f;
    [SerializeField] private float openSpeed = 240f;
    [SerializeField] private bool autoClose;
    [Min(0.1f)]
    [SerializeField] private float autoCloseDelay = 3f;

    private Quaternion closedRotation;
    private Quaternion targetRotation;
    private bool isOpen;
    private float autoCloseTimer;

    private void Awake()
    {
        ResolveReferences();
        InitializeState();
    }

    private void OnValidate()
    {
        ResolveReferences();
        openAngle = Mathf.Clamp(openAngle, -175f, 175f);
        openSpeed = Mathf.Max(1f, openSpeed);
        autoCloseDelay = Mathf.Max(0.1f, autoCloseDelay);
    }

    private void Update()
    {
        if (hinge == null)
        {
            return;
        }

        hinge.localRotation = Quaternion.RotateTowards(hinge.localRotation, targetRotation, openSpeed * Time.deltaTime);

        if (autoClose && isOpen)
        {
            autoCloseTimer -= Time.deltaTime;
            if (autoCloseTimer <= 0f)
            {
                SetOpen(false);
            }
        }
    }

    public void Configure(Transform hingeTransform, Collider interactCollider, float swingAngle = 96f, bool initiallyOpen = false)
    {
        hinge = hingeTransform;
        interactionCollider = interactCollider;
        openAngle = Mathf.Clamp(swingAngle, -175f, 175f);
        startsOpen = initiallyOpen;
        ResolveReferences();
        InitializeState();
    }

    public string GetInteractionLabel(PlayerInteractor interactor)
    {
        if (locked)
        {
            return $"{doorLabel} (Locked)";
        }

        return $"{(isOpen ? closeVerb : openVerb)} {doorLabel}";
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return hinge != null;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (locked)
        {
            return;
        }

        bool openAwayFromInteractor = true;
        if (interactor != null)
        {
            Vector3 toInteractor = interactor.transform.position - hinge.position;
            openAwayFromInteractor = Vector3.Dot(hinge.right, toInteractor) >= 0f;
        }

        SetOpen(!isOpen, openAwayFromInteractor);
    }

    public Transform GetInteractionTransform()
    {
        return interactionCollider != null ? interactionCollider.transform : transform;
    }

    public void SetLocked(bool isLocked)
    {
        locked = isLocked;
    }

    private void SetOpen(bool shouldOpen, bool positiveDirection = true)
    {
        isOpen = shouldOpen;
        if (hinge == null)
        {
            return;
        }

        float signedAngle = positiveDirection ? Mathf.Abs(openAngle) : -Mathf.Abs(openAngle);
        targetRotation = shouldOpen
            ? closedRotation * Quaternion.Euler(0f, signedAngle, 0f)
            : closedRotation;
        autoCloseTimer = shouldOpen ? autoCloseDelay : 0f;
    }

    private void ResolveReferences()
    {
        if (hinge == null)
        {
            hinge = transform;
        }

        if (interactionCollider == null)
        {
            interactionCollider = GetComponentInChildren<Collider>();
        }
    }

    private void InitializeState()
    {
        ResolveReferences();
        closedRotation = hinge != null ? hinge.localRotation : Quaternion.identity;
        targetRotation = closedRotation;
        isOpen = false;

        if (startsOpen)
        {
            SetOpen(true);
            if (hinge != null)
            {
                hinge.localRotation = targetRotation;
            }
        }
        else if (hinge != null)
        {
            hinge.localRotation = closedRotation;
        }
    }
}
