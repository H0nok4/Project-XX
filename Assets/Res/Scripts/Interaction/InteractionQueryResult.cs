using UnityEngine;

public readonly struct InteractionQueryResult
{
    public readonly IInteractable Interactable;
    public readonly Collider Collider;
    public readonly RaycastHit Hit;

    public bool HasValue => Interactable != null;

    public InteractionQueryResult(IInteractable interactable, Collider collider, RaycastHit hit)
    {
        Interactable = interactable;
        Collider = collider;
        Hit = hit;
    }
}
