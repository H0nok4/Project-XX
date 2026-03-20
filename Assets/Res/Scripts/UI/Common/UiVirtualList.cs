using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UiVirtualList : UiWidgetBase
{
    [Header("Virtual List")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private UiReusableRendererBase itemPrefab;
    [SerializeField] private bool enforceTopStretchContent = true;
    [SerializeField] private float defaultItemHeight = 96f;
    [SerializeField] private float itemSpacing = 8f;
    [SerializeField] private float topPadding = 0f;
    [SerializeField] private float bottomPadding = 0f;
    [SerializeField, Min(0)] private int extraVisibleItems = 2;
    [SerializeField] private bool resetScrollPositionOnSetItems = true;
    [SerializeField] private bool clearPoolOnDisable;

    private readonly Stack<UiReusableRendererBase> pooledItems = new Stack<UiReusableRendererBase>();
    private readonly Dictionary<int, UiReusableRendererBase> visibleItems = new Dictionary<int, UiReusableRendererBase>();
    private readonly List<int> recycleBuffer = new List<int>();
    private readonly List<float> itemHeights = new List<float>();
    private readonly List<float> itemStartOffsets = new List<float>();

    private Func<int, object> dataResolver;
    private Action<UiReusableRendererBase, object, int> afterBindAction;
    private Func<int, float> itemHeightResolver;
    private int itemCount;
    private float estimatedItemSpan;
    private float requestedContentHeight;
    private float appliedContentHeight;
    private bool isListeningToScroll;

    public ScrollRect ScrollRect => scrollRect;
    public RectTransform Viewport => viewport;
    public RectTransform ContentRoot => contentRoot;
    public UiReusableRendererBase ItemPrefab => itemPrefab;
    public int Count => itemCount;

    protected override void CacheReferences()
    {
        base.CacheReferences();
        scrollRect ??= GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            viewport ??= scrollRect.viewport;
            contentRoot ??= scrollRect.content;
        }
    }

    protected override void OnInitialize()
    {
        CacheReferences();
        PrepareTemplate();
        SubscribeToScroll();
        ApplyContentHeight();
    }

    protected override void OnWidgetShown()
    {
        SubscribeToScroll();
        ApplyContentHeight();
        RefreshVisibleItems(true);
    }

    protected override void OnWidgetHidden()
    {
        UnsubscribeFromScroll();
        ReleaseAllVisibleItems();
        if (clearPoolOnDisable)
        {
            ClearPool();
        }
    }

    protected override void OnWidgetDestroyed()
    {
        UnsubscribeFromScroll();
        ClearAllInstances();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || !IsInitialized)
        {
            return;
        }

        ApplyContentHeight();
        RefreshVisibleItems(true);
    }

    public void Configure(ScrollRect targetScrollRect, RectTransform targetContentRoot, UiReusableRendererBase targetItemPrefab)
    {
        scrollRect = targetScrollRect;
        viewport = targetScrollRect != null ? targetScrollRect.viewport : null;
        contentRoot = targetContentRoot;
        itemPrefab = targetItemPrefab;

        if (!IsInitialized)
        {
            return;
        }

        PrepareTemplate();
        SubscribeToScroll();
        ApplyContentHeight();
        RefreshVisibleItems(true);
    }

    public void SetItemPrefab(UiReusableRendererBase targetItemPrefab)
    {
        itemPrefab = targetItemPrefab;
        if (!IsInitialized)
        {
            return;
        }

        PrepareTemplate();
        RefreshVisibleItems(true);
    }

    public void SetItems<TData>(
        IReadOnlyList<TData> items,
        Action<UiReusableRendererBase, TData, int> binder = null,
        Func<TData, float> heightResolver = null,
        bool? resetScrollPosition = null,
        UiReusableRendererBase overrideItemPrefab = null)
    {
        EnsureInitialized();

        if (overrideItemPrefab != null)
        {
            itemPrefab = overrideItemPrefab;
        }

        if (items == null)
        {
            ClearItems();
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing item prefab for virtual list.", this);
            ClearItems();
            return;
        }

        PrepareTemplate();

        itemCount = items.Count;
        dataResolver = index => items[index];
        afterBindAction = binder == null
            ? null
            : (renderer, data, index) => binder(renderer, (TData)data, index);
        itemHeightResolver = heightResolver == null
            ? null
            : index => heightResolver(items[index]);

        RebuildMetrics();

        if (resetScrollPosition ?? resetScrollPositionOnSetItems)
        {
            ScrollToOffset(0f);
        }

        RefreshVisibleItems(true);
    }

    public void ClearItems()
    {
        itemCount = 0;
        dataResolver = null;
        afterBindAction = null;
        itemHeightResolver = null;
        itemHeights.Clear();
        itemStartOffsets.Clear();
        estimatedItemSpan = defaultItemHeight + itemSpacing;
        requestedContentHeight = 0f;
        ApplyContentHeight();
        ReleaseAllVisibleItems();
        ScrollToOffset(0f);
    }

    public void RefreshVisibleItems(bool forceRebind = false)
    {
        EnsureInitialized();
        if (contentRoot == null || viewport == null || itemPrefab == null || itemCount <= 0 || dataResolver == null)
        {
            ReleaseAllVisibleItems();
            return;
        }

        float scrollOffset = GetScrollOffset();
        float viewportHeight = GetViewportHeight();
        float bufferHeight = Mathf.Max(0f, extraVisibleItems) * Mathf.Max(1f, estimatedItemSpan);
        float minVisible = Mathf.Max(0f, scrollOffset - bufferHeight);
        float maxVisible = scrollOffset + viewportHeight + bufferHeight;

        int firstVisibleIndex = FindFirstVisibleIndex(minVisible);
        int lastVisibleIndex = FindLastVisibleIndex(maxVisible);
        if (firstVisibleIndex < 0 || lastVisibleIndex < firstVisibleIndex)
        {
            ReleaseAllVisibleItems();
            return;
        }

        SynchronizeVisibleItems(firstVisibleIndex, lastVisibleIndex, forceRebind);
    }

    public void ScrollToIndex(int index, float align = 0f)
    {
        if (itemCount <= 0)
        {
            ScrollToOffset(0f);
            return;
        }

        index = Mathf.Clamp(index, 0, itemCount - 1);
        float viewportHeight = GetViewportHeight();
        float normalizedAlign = Mathf.Clamp01(align);
        float targetOffset = itemStartOffsets[index] - (viewportHeight - itemHeights[index]) * normalizedAlign;
        ScrollToOffset(targetOffset);
        RefreshVisibleItems(true);
    }

    private void PrepareTemplate()
    {
        if (itemPrefab != null && itemPrefab.transform.parent == contentRoot)
        {
            itemPrefab.gameObject.SetActive(false);
        }
    }

    private void RebuildMetrics()
    {
        itemHeights.Clear();
        itemStartOffsets.Clear();

        float runningOffset = topPadding;
        float sumHeights = 0f;
        for (int index = 0; index < itemCount; index++)
        {
            itemStartOffsets.Add(runningOffset);

            float height = ResolveItemHeight(index);
            itemHeights.Add(height);
            sumHeights += height;

            runningOffset += height;
            if (index < itemCount - 1)
            {
                runningOffset += itemSpacing;
            }
        }

        requestedContentHeight = itemCount > 0 ? runningOffset + bottomPadding : topPadding + bottomPadding;
        estimatedItemSpan = itemCount > 0
            ? (sumHeights / itemCount) + itemSpacing
            : defaultItemHeight + itemSpacing;

        ApplyContentHeight();
    }

    private float ResolveItemHeight(int index)
    {
        float height = itemHeightResolver != null ? itemHeightResolver(index) : 0f;
        if (height <= 0f && itemPrefab != null)
        {
            height = itemPrefab.PreferredHeight;
        }

        if (height <= 0f)
        {
            height = defaultItemHeight;
        }

        return Mathf.Max(1f, height);
    }

    private void ApplyContentHeight()
    {
        if (contentRoot == null)
        {
            return;
        }

        if (enforceTopStretchContent)
        {
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
        }

        appliedContentHeight = Mathf.Max(requestedContentHeight, GetViewportHeight());
        Vector2 sizeDelta = contentRoot.sizeDelta;
        sizeDelta.y = appliedContentHeight;
        contentRoot.sizeDelta = sizeDelta;
    }

    private float GetViewportHeight()
    {
        if (viewport != null && viewport.rect.height > 0f)
        {
            return viewport.rect.height;
        }

        if (scrollRect != null && scrollRect.viewport != null && scrollRect.viewport.rect.height > 0f)
        {
            return scrollRect.viewport.rect.height;
        }

        return 0f;
    }

    private float GetScrollOffset()
    {
        if (contentRoot == null)
        {
            return 0f;
        }

        return Mathf.Max(0f, contentRoot.anchoredPosition.y);
    }

    private void ScrollToOffset(float offset)
    {
        if (contentRoot == null)
        {
            return;
        }

        float maxOffset = Mathf.Max(0f, appliedContentHeight - GetViewportHeight());
        float clampedOffset = Mathf.Clamp(offset, 0f, maxOffset);

        Vector2 anchoredPosition = contentRoot.anchoredPosition;
        anchoredPosition.y = clampedOffset;
        contentRoot.anchoredPosition = anchoredPosition;

        if (scrollRect != null)
        {
            if (maxOffset <= 0.001f)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
            else
            {
                scrollRect.verticalNormalizedPosition = 1f - clampedOffset / maxOffset;
            }
        }
    }

    private int FindFirstVisibleIndex(float position)
    {
        if (itemCount <= 0)
        {
            return -1;
        }

        int left = 0;
        int right = itemCount - 1;
        int result = itemCount - 1;
        while (left <= right)
        {
            int middle = left + (right - left) / 2;
            float itemBottom = itemStartOffsets[middle] + itemHeights[middle];
            if (itemBottom >= position)
            {
                result = middle;
                right = middle - 1;
            }
            else
            {
                left = middle + 1;
            }
        }

        return Mathf.Clamp(result, 0, itemCount - 1);
    }

    private int FindLastVisibleIndex(float position)
    {
        if (itemCount <= 0)
        {
            return -1;
        }

        int left = 0;
        int right = itemCount - 1;
        int result = 0;
        while (left <= right)
        {
            int middle = left + (right - left) / 2;
            if (itemStartOffsets[middle] <= position)
            {
                result = middle;
                left = middle + 1;
            }
            else
            {
                right = middle - 1;
            }
        }

        return Mathf.Clamp(result, 0, itemCount - 1);
    }

    private void SynchronizeVisibleItems(int firstVisibleIndex, int lastVisibleIndex, bool forceRebind)
    {
        recycleBuffer.Clear();
        foreach (KeyValuePair<int, UiReusableRendererBase> pair in visibleItems)
        {
            if (pair.Key < firstVisibleIndex || pair.Key > lastVisibleIndex)
            {
                recycleBuffer.Add(pair.Key);
            }
        }

        for (int index = 0; index < recycleBuffer.Count; index++)
        {
            RecycleVisibleItem(recycleBuffer[index]);
        }

        for (int index = firstVisibleIndex; index <= lastVisibleIndex; index++)
        {
            if (!visibleItems.TryGetValue(index, out UiReusableRendererBase renderer) || renderer == null)
            {
                renderer = GetOrCreateRenderer();
                visibleItems[index] = renderer;
                PositionRenderer(renderer, index);
                BindRenderer(renderer, index);
                continue;
            }

            PositionRenderer(renderer, index);
            if (forceRebind)
            {
                BindRenderer(renderer, index);
            }
        }
    }

    private UiReusableRendererBase GetOrCreateRenderer()
    {
        UiReusableRendererBase renderer = null;
        while (pooledItems.Count > 0 && renderer == null)
        {
            renderer = pooledItems.Pop();
        }

        if (renderer == null)
        {
            renderer = Instantiate(itemPrefab, contentRoot);
        }
        else
        {
            renderer.transform.SetParent(contentRoot, false);
        }

        renderer.gameObject.SetActive(true);
        renderer.EnsureInitialized();
        return renderer;
    }

    private void BindRenderer(UiReusableRendererBase renderer, int index)
    {
        if (renderer == null || dataResolver == null)
        {
            return;
        }

        object data = dataResolver(index);
        renderer.Bind(data, index);
        afterBindAction?.Invoke(renderer, data, index);
    }

    private void PositionRenderer(UiReusableRendererBase renderer, int index)
    {
        if (renderer == null || renderer.RectTransform == null || contentRoot == null)
        {
            return;
        }

        RectTransform itemRoot = renderer.RectTransform;
        itemRoot.SetParent(contentRoot, false);
        itemRoot.anchorMin = new Vector2(0f, 1f);
        itemRoot.anchorMax = new Vector2(1f, 1f);
        itemRoot.pivot = new Vector2(0.5f, 1f);
        itemRoot.anchoredPosition = new Vector2(0f, -itemStartOffsets[index]);
        itemRoot.sizeDelta = new Vector2(0f, itemHeights[index]);
        itemRoot.SetSiblingIndex(Mathf.Min(index, contentRoot.childCount - 1));
    }

    private void ReleaseAllVisibleItems()
    {
        recycleBuffer.Clear();
        foreach (int key in visibleItems.Keys)
        {
            recycleBuffer.Add(key);
        }

        for (int index = 0; index < recycleBuffer.Count; index++)
        {
            RecycleVisibleItem(recycleBuffer[index]);
        }
    }

    private void RecycleVisibleItem(int index)
    {
        if (!visibleItems.TryGetValue(index, out UiReusableRendererBase renderer))
        {
            return;
        }

        visibleItems.Remove(index);
        if (renderer == null)
        {
            return;
        }

        renderer.Unbind();
        renderer.gameObject.SetActive(false);
        pooledItems.Push(renderer);
    }

    private void ClearPool()
    {
        while (pooledItems.Count > 0)
        {
            UiReusableRendererBase renderer = pooledItems.Pop();
            if (renderer != null)
            {
                Destroy(renderer.gameObject);
            }
        }
    }

    private void ClearAllInstances()
    {
        ReleaseAllVisibleItems();
        ClearPool();
    }

    private void SubscribeToScroll()
    {
        if (scrollRect == null || isListeningToScroll)
        {
            return;
        }

        scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        isListeningToScroll = true;
    }

    private void UnsubscribeFromScroll()
    {
        if (scrollRect == null || !isListeningToScroll)
        {
            return;
        }

        scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        isListeningToScroll = false;
    }

    private void OnScrollValueChanged(Vector2 _)
    {
        RefreshVisibleItems();
    }
}

