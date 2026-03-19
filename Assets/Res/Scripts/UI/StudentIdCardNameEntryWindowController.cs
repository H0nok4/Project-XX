using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-40)]
[DisallowMultipleComponent]
public sealed class StudentIdCardNameEntryWindowController : WindowBase
{
    private const string WindowPrefabResourcePath = "UI/StudentID/StudentIdCardNameEntryWindow";

    private static StudentIdCardNameEntryWindowController instance;

    [SerializeField] private string fieldLabel = "姓名";

    private StudentIdCardNameEntryWindowTemplate windowView;
    private Action<string> confirmCallback;
    private Action<string> valueChangedCallback;
    private Action cancelCallback;
    private Coroutine focusCoroutine;
    private bool allowCancel = true;

    public static StudentIdCardNameEntryWindowController GetOrCreate()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<StudentIdCardNameEntryWindowController>();
        if (instance != null)
        {
            return instance;
        }

        GameObject root = new GameObject("StudentIdCardNameEntryWindowController");
        return root.AddComponent<StudentIdCardNameEntryWindowController>();
    }

    public bool IsOpen => IsWindowVisible;

    protected override PrototypeUiLayer WindowLayer => PrototypeUiLayer.Modal;
    protected override bool VisibleOnAwake => false;
    protected override string WindowName => "StudentIdCardNameEntryWindow";
    protected override string WindowTitle => "填写学生证";
    protected override string WindowSubtitle => "请输入你的名字，确认后会直接印在学生证上。";
    protected override Vector2 WindowSize => new Vector2(720f, 280f);

    protected override void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        base.Awake();
    }

    protected override void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        base.OnDestroy();
    }

    public void Open(string initialName, bool canCancel, Action<string> onConfirm, Action onCancel, Action<string> onValueChanged = null)
    {
        EnsureWindow();
        if (windowView == null || windowView.NameInputField == null)
        {
            return;
        }

        allowCancel = canCancel;
        confirmCallback = onConfirm;
        valueChangedCallback = onValueChanged;
        cancelCallback = onCancel;

        HookTemplateCallbacks();
        if (windowView.FieldLabelText != null)
        {
            windowView.FieldLabelText.text = fieldLabel;
        }

        windowView.NameInputField.text = string.IsNullOrWhiteSpace(initialName) ? string.Empty : initialName.Trim();
        if (windowView.CancelButton != null)
        {
            windowView.CancelButton.gameObject.SetActive(allowCancel);
        }

        RefreshConfirmState();
        ShowWindow();

        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
        }

        focusCoroutine = StartCoroutine(FocusInputNextFrame());
    }

    public void Close()
    {
        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
            focusCoroutine = null;
        }

        confirmCallback = null;
        valueChangedCallback = null;
        cancelCallback = null;
        HideWindow();
    }

    protected override PrototypeUiToolkit.WindowChrome CreateWindowChrome()
    {
        RectTransform parent = UiManager.GetLayerRoot(WindowLayer);
        if (TryInstantiateWindowPrefab(parent, out PrototypeUiToolkit.WindowChrome chrome))
        {
            PrototypeUiToolkit.SetVisible(chrome.Root, false);
            return chrome;
        }

        windowView = null;
        return base.CreateWindowChrome();
    }

    protected override void BuildWindow(PrototypeUiToolkit.WindowChrome chrome)
    {
        if (chrome == null || chrome.Root == null)
        {
            return;
        }

        if (windowView != null && windowView.Root == chrome.Root)
        {
            HookTemplateCallbacks();
        }
    }

    protected override void OnWindowVisibilityChanged(bool visible)
    {
        base.OnWindowVisibilityChanged(visible);
        if (!visible && EventSystem.current != null && windowView != null && windowView.NameInputField != null
            && EventSystem.current.currentSelectedGameObject == windowView.NameInputField.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    protected override void OnWindowRootDestroyed()
    {
        windowView = null;
    }

    private void Update()
    {
        if (!IsOpen || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            TryConfirm();
            return;
        }

        if (allowCancel && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cancel();
        }
    }

    private bool TryInstantiateWindowPrefab(RectTransform parent, out PrototypeUiToolkit.WindowChrome chrome)
    {
        chrome = null;
        GameObject prefabAsset = Resources.Load<GameObject>(WindowPrefabResourcePath);
        if (prefabAsset == null)
        {
            return false;
        }

        GameObject instanceObject = Instantiate(prefabAsset, parent, false);
        instanceObject.name = prefabAsset.name;

        windowView = instanceObject.GetComponent<StudentIdCardNameEntryWindowTemplate>();
        if (windowView == null || windowView.Root == null)
        {
            Destroy(instanceObject);
            windowView = null;
            return false;
        }

        chrome = windowView.CreateWindowChrome();
        if (chrome == null || chrome.Root == null)
        {
            Destroy(instanceObject);
            windowView = null;
            chrome = null;
            return false;
        }

        PrototypeUiToolkit.ApplyFontRecursively(chrome.Root, RuntimeFont);
        HookTemplateCallbacks();
        return true;
    }

    private void HookTemplateCallbacks()
    {
        if (windowView == null)
        {
            return;
        }

        if (windowView.NameInputField != null)
        {
            windowView.NameInputField.onValueChanged.RemoveListener(HandleNameValueChanged);
            windowView.NameInputField.onValueChanged.AddListener(HandleNameValueChanged);
        }

        if (windowView.ConfirmButton != null)
        {
            windowView.ConfirmButton.onClick.RemoveAllListeners();
            windowView.ConfirmButton.onClick.AddListener(TryConfirm);
        }

        if (windowView.CancelButton != null)
        {
            windowView.CancelButton.onClick.RemoveAllListeners();
            windowView.CancelButton.onClick.AddListener(Cancel);
            windowView.CancelButton.gameObject.SetActive(allowCancel);
        }
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null;
        focusCoroutine = null;

        if (!IsOpen || windowView == null || windowView.NameInputField == null)
        {
            yield break;
        }

        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem != null)
        {
            currentEventSystem.SetSelectedGameObject(windowView.NameInputField.gameObject);
        }

        windowView.NameInputField.ActivateInputField();
        windowView.NameInputField.Select();
        windowView.NameInputField.caretPosition = windowView.NameInputField.text.Length;
    }

    private void TryConfirm()
    {
        if (windowView == null || windowView.NameInputField == null)
        {
            return;
        }

        string candidate = SanitizeName(windowView.NameInputField.text);
        if (string.IsNullOrWhiteSpace(candidate))
        {
            RefreshConfirmState();
            return;
        }

        Action<string> callback = confirmCallback;
        Close();
        callback?.Invoke(candidate);
    }

    private void Cancel()
    {
        if (!allowCancel)
        {
            return;
        }

        Action callback = cancelCallback;
        Close();
        callback?.Invoke();
    }

    private void HandleNameValueChanged(string value)
    {
        RefreshConfirmState();
        valueChangedCallback?.Invoke(value ?? string.Empty);
    }

    private void RefreshConfirmState()
    {
        if (windowView == null || windowView.ConfirmButton == null)
        {
            return;
        }

        string candidate = windowView.NameInputField != null ? SanitizeName(windowView.NameInputField.text) : string.Empty;
        windowView.ConfirmButton.interactable = !string.IsNullOrWhiteSpace(candidate);
    }

    private static string SanitizeName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
