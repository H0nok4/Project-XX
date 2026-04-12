using UnityEngine;

public interface IUiManagedElement
{
    bool RegistersWithUiWindowService { get; }
    bool IsManagedElementVisible { get; }
    PrototypeUiLayer ManagedLayer { get; }
    int ManagedInputPriority { get; }
    string ManagedElementName { get; }
    Object ManagedOwner { get; }

    bool TryHandleUiSubmit();
    bool TryHandleUiCancel();
}
