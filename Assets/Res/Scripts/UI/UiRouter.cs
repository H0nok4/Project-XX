using System;
using UnityEngine;

[DefaultExecutionOrder(-960)]
[DisallowMultipleComponent]
public sealed class UiRouter : MonoBehaviour
{
    private static UiRouter instance;

    public static UiRouter Instance => GetOrCreate();

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

    public static UiRouter GetOrCreate()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<UiRouter>();
        if (instance != null)
        {
            return instance;
        }

        GameObject routerObject = new GameObject("UiRouter");
        instance = routerObject.AddComponent<UiRouter>();
        return instance;
    }

    public static T GetOrCreate<T>(string objectName = null)
        where T : Component
    {
        if (TryGetExisting(out T existing))
        {
            return existing;
        }

        string targetName = string.IsNullOrWhiteSpace(objectName) ? typeof(T).Name : objectName.Trim();
        GameObject root = new GameObject(targetName);
        return root.AddComponent<T>();
    }

    public static bool TryGetExisting<T>(out T component)
        where T : Component
    {
        component = FindFirstObjectByType<T>();
        return component != null;
    }

    public static T OpenWindow<T>(Action<T> beforeOpen = null, string objectName = null)
        where T : WindowBase
    {
        T target = GetOrCreate<T>(objectName);
        beforeOpen?.Invoke(target);
        target.ShowWindow();
        return target;
    }

    public static T OpenView<T>(Action<T> beforeOpen = null, string objectName = null)
        where T : ViewBase
    {
        T target = GetOrCreate<T>(objectName);
        beforeOpen?.Invoke(target);
        target.ShowView();
        return target;
    }

    public static bool CloseWindow<T>()
        where T : WindowBase
    {
        if (!TryGetExisting(out T target))
        {
            return false;
        }

        target.HideWindow();
        return true;
    }

    public static bool CloseView<T>()
        where T : ViewBase
    {
        if (!TryGetExisting(out T target))
        {
            return false;
        }

        target.HideView();
        return true;
    }

    public static bool IsWindowOpen<T>()
        where T : WindowBase
    {
        return TryGetExisting(out T target) && target.IsWindowVisible;
    }

    public static bool IsViewOpen<T>()
        where T : ViewBase
    {
        return TryGetExisting(out T target) && target.IsViewVisible;
    }

    public static bool CloseTopmostWindow()
    {
        return UiWindowService.GetOrCreate().CloseTopmost();
    }

    public static bool TrySubmitTopmostWindow()
    {
        return UiWindowService.GetOrCreate().TryHandleSubmit();
    }
}
