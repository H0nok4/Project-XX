using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-980)]
[DisallowMultipleComponent]
public sealed class UiWindowService : MonoBehaviour
{
    private static UiWindowService instance;

    private readonly HashSet<IUiManagedElement> registeredElements = new HashSet<IUiManagedElement>();
    private readonly Dictionary<IUiManagedElement, long> visibleOrderMap = new Dictionary<IUiManagedElement, long>();
    private readonly List<IUiManagedElement> sortedBuffer = new List<IUiManagedElement>();

    private long visibleOrderCounter;

    public static UiWindowService Instance => GetOrCreate();
    public static bool HasInstance => instance != null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static UiWindowService GetOrCreate()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<UiWindowService>();
        if (instance != null)
        {
            return instance;
        }

        GameObject serviceObject = new GameObject("UiWindowService");
        instance = serviceObject.AddComponent<UiWindowService>();
        return instance;
    }

    public static bool TryGetExisting(out UiWindowService service)
    {
        if (instance != null)
        {
            service = instance;
            return true;
        }

        instance = FindFirstObjectByType<UiWindowService>();
        service = instance;
        return service != null;
    }

    public void Register(IUiManagedElement element)
    {
        if (!IsElementEligible(element))
        {
            return;
        }

        registeredElements.Add(element);
        if (element.IsManagedElementVisible)
        {
            BringToFront(element);
        }
    }

    public void Unregister(IUiManagedElement element)
    {
        if (element == null)
        {
            return;
        }

        registeredElements.Remove(element);
        visibleOrderMap.Remove(element);
    }

    public void NotifyVisibilityChanged(IUiManagedElement element, bool visible)
    {
        if (!IsElementEligible(element))
        {
            return;
        }

        registeredElements.Add(element);
        if (visible)
        {
            BringToFront(element);
        }
        else
        {
            visibleOrderMap.Remove(element);
        }
    }

    public bool TryHandleSubmit()
    {
        return TryDispatch(static element => element.TryHandleUiSubmit());
    }

    public bool TryHandleCancel()
    {
        return TryDispatch(static element => element.TryHandleUiCancel());
    }

    public bool CloseTopmost()
    {
        return TryHandleCancel();
    }

    private bool TryDispatch(Func<IUiManagedElement, bool> handler)
    {
        sortedBuffer.Clear();
        CollectVisibleElements(sortedBuffer);
        for (int index = 0; index < sortedBuffer.Count; index++)
        {
            IUiManagedElement element = sortedBuffer[index];
            if (element == null)
            {
                continue;
            }

            if (handler(element))
            {
                return true;
            }
        }

        return false;
    }

    private void CollectVisibleElements(List<IUiManagedElement> buffer)
    {
        CleanupInvalidEntries();
        foreach (IUiManagedElement element in registeredElements)
        {
            if (!IsElementActive(element))
            {
                continue;
            }

            buffer.Add(element);
        }

        buffer.Sort(CompareElements);
    }

    private int CompareElements(IUiManagedElement left, IUiManagedElement right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        int layerCompare = right.ManagedLayer.CompareTo(left.ManagedLayer);
        if (layerCompare != 0)
        {
            return layerCompare;
        }

        int priorityCompare = right.ManagedInputPriority.CompareTo(left.ManagedInputPriority);
        if (priorityCompare != 0)
        {
            return priorityCompare;
        }

        long rightOrder = ResolveVisibleOrder(right);
        long leftOrder = ResolveVisibleOrder(left);
        int orderCompare = rightOrder.CompareTo(leftOrder);
        if (orderCompare != 0)
        {
            return orderCompare;
        }

        return string.CompareOrdinal(right.ManagedElementName, left.ManagedElementName);
    }

    private void CleanupInvalidEntries()
    {
        sortedBuffer.Clear();
        foreach (IUiManagedElement element in registeredElements)
        {
            if (IsElementEligible(element))
            {
                continue;
            }

            sortedBuffer.Add(element);
        }

        for (int index = 0; index < sortedBuffer.Count; index++)
        {
            IUiManagedElement invalidElement = sortedBuffer[index];
            registeredElements.Remove(invalidElement);
            visibleOrderMap.Remove(invalidElement);
        }

        sortedBuffer.Clear();
    }

    private void BringToFront(IUiManagedElement element)
    {
        visibleOrderCounter++;
        visibleOrderMap[element] = visibleOrderCounter;
    }

    private long ResolveVisibleOrder(IUiManagedElement element)
    {
        return visibleOrderMap.TryGetValue(element, out long order) ? order : long.MinValue;
    }

    private static bool IsElementEligible(IUiManagedElement element)
    {
        return element != null && element.RegistersWithUiWindowService && element.ManagedOwner != null;
    }

    private static bool IsElementActive(IUiManagedElement element)
    {
        return IsElementEligible(element) && element.IsManagedElementVisible;
    }
}
