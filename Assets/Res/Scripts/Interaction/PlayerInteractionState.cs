using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInteractionState : MonoBehaviour
{
    [SerializeField] private bool uiFocused;
    private readonly HashSet<int> uiFocusOwners = new HashSet<int>();

    public bool IsUiFocused => uiFocused || uiFocusOwners.Count > 0;

    public void SetUiFocused(bool focused)
    {
        uiFocused = focused;
        if (focused)
        {
            uiFocusOwners.Clear();
        }
    }

    public void SetUiFocused(Object owner, bool focused)
    {
        if (owner == null)
        {
            SetUiFocused(focused);
            return;
        }

        uiFocused = false;

        if (focused)
        {
            uiFocusOwners.Add(owner.GetInstanceID());
        }
        else
        {
            uiFocusOwners.Remove(owner.GetInstanceID());
        }
    }
}
