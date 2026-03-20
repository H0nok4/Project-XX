using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UiTabGroup : UiWidgetBase
{
    [Serializable]
    private sealed class IntChangedEvent : UnityEvent<int>
    {
    }

    [Serializable]
    private sealed class StringChangedEvent : UnityEvent<string>
    {
    }

    [Serializable]
    public sealed class TabEntry
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private bool selectedByDefault;
        [SerializeField] private Button button;
        [SerializeField] private RectTransform pageRoot;
        [SerializeField] private UiTransitionPlayer pageTransition;

        public string Id => id;
        public bool SelectedByDefault => selectedByDefault;
        public Button Button => button;
        public RectTransform PageRoot => pageRoot;
        public UiTransitionPlayer PageTransition => pageTransition;
    }

    [Header("Tab Group")]
    [SerializeField] private List<TabEntry> tabs = new List<TabEntry>();
    [SerializeField] private int defaultTabIndex;
    [SerializeField] private bool selectDefaultOnAwake = true;
    [SerializeField] private bool instantInitialSelection = true;
    [SerializeField] private bool disableSelectedButton;
    [SerializeField] private bool deactivateInactivePages = true;
    [SerializeField] private IntChangedEvent onTabIndexChanged = new IntChangedEvent();
    [SerializeField] private StringChangedEvent onTabIdChanged = new StringChangedEvent();

    private readonly List<UnityAction> clickHandlers = new List<UnityAction>();
    private int currentIndex = -1;

    public IReadOnlyList<TabEntry> Tabs => tabs;
    public int CurrentIndex => currentIndex;
    public string CurrentId => IsValidIndex(currentIndex) ? GetResolvedTabId(currentIndex) : string.Empty;

    protected override void OnInitialize()
    {
        BindButtons();
        if (selectDefaultOnAwake && tabs.Count > 0)
        {
            Select(ResolveDefaultIndex(), false, instantInitialSelection);
        }
        else
        {
            ApplySelectionState(-1, instantInitialSelection);
        }
    }

    protected override void OnRefresh()
    {
        ApplySelectionState(currentIndex, true);
    }

    protected override void OnWidgetDestroyed()
    {
        UnbindButtons();
    }

    public void Select(int index)
    {
        Select(index, true, false);
    }

    public bool Select(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        for (int index = 0; index < tabs.Count; index++)
        {
            if (string.Equals(GetResolvedTabId(index), id.Trim(), StringComparison.Ordinal))
            {
                Select(index, true, false);
                return true;
            }
        }

        return false;
    }

    public void RefreshSelection()
    {
        ApplySelectionState(currentIndex, true);
    }

    private void Select(int index, bool notify, bool instant)
    {
        EnsureInitialized();
        if (!IsValidIndex(index))
        {
            return;
        }

        bool changed = currentIndex != index;
        currentIndex = index;
        ApplySelectionState(index, instant);

        if (notify && changed)
        {
            onTabIndexChanged?.Invoke(currentIndex);
            onTabIdChanged?.Invoke(CurrentId);
        }
    }

    private void BindButtons()
    {
        UnbindButtons();
        clickHandlers.Clear();
        for (int index = 0; index < tabs.Count; index++)
        {
            Button button = tabs[index]?.Button;
            if (button == null)
            {
                clickHandlers.Add(null);
                continue;
            }

            int capturedIndex = index;
            UnityAction action = () => Select(capturedIndex, true, false);
            button.onClick.AddListener(action);
            clickHandlers.Add(action);
        }
    }

    private void UnbindButtons()
    {
        for (int index = 0; index < tabs.Count && index < clickHandlers.Count; index++)
        {
            Button button = tabs[index]?.Button;
            UnityAction action = clickHandlers[index];
            if (button != null && action != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }

    private void ApplySelectionState(int selectedIndex, bool instant)
    {
        for (int index = 0; index < tabs.Count; index++)
        {
            ApplyTabState(index, index == selectedIndex, instant);
        }
    }

    private void ApplyTabState(int index, bool selected, bool instant)
    {
        TabEntry entry = tabs[index];
        if (entry == null)
        {
            return;
        }

        Button button = entry.Button;
        if (button != null)
        {
            button.interactable = !selected || !disableSelectedButton;

            UiAnimatedButton animatedButton = button.GetComponent<UiAnimatedButton>();
            if (animatedButton != null)
            {
                animatedButton.SetSelectedState(selected);
            }
        }

        RectTransform pageRoot = entry.PageRoot;
        if (pageRoot == null)
        {
            return;
        }

        UiTransitionPlayer transition = entry.PageTransition != null ? entry.PageTransition : pageRoot.GetComponent<UiTransitionPlayer>();
        if (selected)
        {
            if (transition != null)
            {
                transition.ShowAndActivate(instant);
            }
            else
            {
                PrototypeUiToolkit.SetVisible(pageRoot, true);
            }

            return;
        }

        if (transition != null)
        {
            if (deactivateInactivePages)
            {
                transition.HideAndDeactivate(instant);
            }
            else
            {
                transition.PlayHide(instant);
            }
        }
        else
        {
            PrototypeUiToolkit.SetVisible(pageRoot, false);
        }
    }

    private int ResolveDefaultIndex()
    {
        for (int index = 0; index < tabs.Count; index++)
        {
            if (tabs[index] != null && tabs[index].SelectedByDefault)
            {
                return index;
            }
        }

        return Mathf.Clamp(defaultTabIndex, 0, Mathf.Max(0, tabs.Count - 1));
    }

    private string GetResolvedTabId(int index)
    {
        TabEntry entry = tabs[index];
        if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
        {
            return $"Tab{index}";
        }

        return entry.Id.Trim();
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < tabs.Count;
    }
}
