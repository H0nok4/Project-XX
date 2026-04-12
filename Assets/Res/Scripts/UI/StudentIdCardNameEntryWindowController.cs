using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-40)]
[DisallowMultipleComponent]
public sealed class StudentIdCardNameEntryWindowController : PrefabWindowBase<StudentIdCardNameEntryWindowTemplate>
{
    private const string WindowPrefabPath = "UI/StudentID/StudentIdCardNameEntryWindow";

    private static StudentIdCardNameEntryWindowController instance;

    [SerializeField] private string fieldLabel = "姓名";

    private Action<string> confirmCallback;
    private Action<string> valueChangedCallback;
    private Action cancelCallback;
    private Coroutine focusCoroutine;
    private bool allowCancel = true;

    public static StudentIdCardNameEntryWindowController GetOrCreate()
    {
        instance = UiRouter.GetOrCreate<StudentIdCardNameEntryWindowController>();
        return instance;
    }

    public bool IsOpen => IsWindowVisible;

    public override int ManagedInputPriority => 100;
    protected override string WindowPrefabId => "StudentIdCardNameEntryWindow";
    protected override string WindowPrefabResourcePath => WindowPrefabPath;
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
        if (Template == null || Template.NameInputField == null)
        {
            return;
        }

        allowCancel = canCancel;
        confirmCallback = onConfirm;
        valueChangedCallback = onValueChanged;
        cancelCallback = onCancel;

        HookTemplateCallbacks();
        if (Template.FieldLabelText != null)
        {
            Template.FieldLabelText.text = fieldLabel;
        }

        Template.NameInputField.text = string.IsNullOrWhiteSpace(initialName) ? string.Empty : initialName.Trim();
        if (Template.CancelButton != null)
        {
            Template.CancelButton.gameObject.SetActive(allowCancel);
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

    protected override PrototypeUiToolkit.WindowChrome CreatePrefabWindowChrome(StudentIdCardNameEntryWindowTemplate template)
    {
        return template != null ? template.CreateWindowChrome() : null;
    }

    protected override void BuildPrefabWindow(StudentIdCardNameEntryWindowTemplate template, PrototypeUiToolkit.WindowChrome chrome)
    {
        if (chrome == null || chrome.Root == null)
        {
            return;
        }

        if (template != null && template.Root == chrome.Root)
        {
            HookTemplateCallbacks();
        }
    }

    protected override void OnWindowVisibilityChanged(bool visible)
    {
        base.OnWindowVisibilityChanged(visible);
        if (!visible && EventSystem.current != null && Template != null && Template.NameInputField != null
            && EventSystem.current.currentSelectedGameObject == Template.NameInputField.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    protected override void OnWindowRootDestroyed()
    {
        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
            focusCoroutine = null;
        }

        base.OnWindowRootDestroyed();
    }

    public override bool TryHandleUiSubmit()
    {
        if (!IsOpen)
        {
            return false;
        }

        TryConfirm();
        return true;
    }

    public override bool TryHandleUiCancel()
    {
        if (!IsOpen || !allowCancel)
        {
            return false;
        }

        Cancel();
        return true;
    }

    private void HookTemplateCallbacks()
    {
        if (Template == null)
        {
            return;
        }

        if (Template.NameInputField != null)
        {
            Template.NameInputField.onValueChanged.RemoveListener(HandleNameValueChanged);
            Template.NameInputField.onValueChanged.AddListener(HandleNameValueChanged);
        }

        if (Template.ConfirmButton != null)
        {
            Template.ConfirmButton.onClick.RemoveAllListeners();
            Template.ConfirmButton.onClick.AddListener(TryConfirm);
        }

        if (Template.CancelButton != null)
        {
            Template.CancelButton.onClick.RemoveAllListeners();
            Template.CancelButton.onClick.AddListener(Cancel);
            Template.CancelButton.gameObject.SetActive(allowCancel);
        }
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null;
        focusCoroutine = null;

        if (!IsOpen || Template == null || Template.NameInputField == null)
        {
            yield break;
        }

        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem != null)
        {
            currentEventSystem.SetSelectedGameObject(Template.NameInputField.gameObject);
        }

        Template.NameInputField.ActivateInputField();
        Template.NameInputField.Select();
        Template.NameInputField.caretPosition = Template.NameInputField.text.Length;
    }

    private void TryConfirm()
    {
        if (Template == null || Template.NameInputField == null)
        {
            return;
        }

        string candidate = SanitizeName(Template.NameInputField.text);
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
        if (Template == null || Template.ConfirmButton == null)
        {
            return;
        }

        string candidate = Template.NameInputField != null ? SanitizeName(Template.NameInputField.text) : string.Empty;
        Template.ConfirmButton.interactable = !string.IsNullOrWhiteSpace(candidate);
    }

    private static string SanitizeName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
