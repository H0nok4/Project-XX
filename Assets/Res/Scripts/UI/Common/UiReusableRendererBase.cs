using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public abstract class UiReusableRendererBase : UiWidgetBase
{
    [Header("Reusable Renderer")]
    [SerializeField] private LayoutElement layoutElement;

    private object boundData;

    public LayoutElement LayoutElement => layoutElement != null ? layoutElement : layoutElement = GetComponent<LayoutElement>();
    public object BoundData => boundData;
    public int DataIndex { get; private set; } = -1;
    public bool HasData => DataIndex >= 0;

    public virtual float PreferredHeight
    {
        get
        {
            if (LayoutElement != null && LayoutElement.preferredHeight > 0f)
            {
                return LayoutElement.preferredHeight;
            }

            if (RectTransform != null)
            {
                float preferredHeight = LayoutUtility.GetPreferredHeight(RectTransform);
                if (preferredHeight > 0f)
                {
                    return preferredHeight;
                }

                if (RectTransform.rect.height > 0f)
                {
                    return RectTransform.rect.height;
                }
            }

            return 0f;
        }
    }

    public virtual void Bind(object data, int index)
    {
        EnsureInitialized();
        boundData = data;
        DataIndex = index;
        OnBindData(data, index);
    }

    public virtual void Unbind()
    {
        if (DataIndex < 0 && boundData == null)
        {
            return;
        }

        OnUnbindData(boundData, DataIndex);
        boundData = null;
        DataIndex = -1;
    }

    protected override void OnWidgetDestroyed()
    {
        Unbind();
    }

    protected abstract void OnBindData(object data, int index);

    protected virtual void OnUnbindData(object data, int index)
    {
    }
}

public abstract class UiReusableRenderer<TData> : UiReusableRendererBase
{
    public TData Data { get; private set; }

    protected sealed override void OnBindData(object data, int index)
    {
        if (data is TData typedData)
        {
            Data = typedData;
        }
        else if (data == null)
        {
            Data = default;
        }
        else
        {
            Debug.LogWarning(
                $"[{GetType().Name}] Expected data type {typeof(TData).Name}, but received {data.GetType().Name}.",
                this);
            Data = default;
            return;
        }

        BindData(Data, index);
    }

    protected sealed override void OnUnbindData(object data, int index)
    {
        UnbindData(Data, index);
        Data = default;
    }

    protected abstract void BindData(TData data, int index);

    protected virtual void UnbindData(TData data, int index)
    {
    }
}
