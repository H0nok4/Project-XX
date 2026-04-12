using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StudentIdCardNameEntryView : PrefabViewBase<StudentIdCardNameEntryWindowTemplate>
{
    private const string ViewPrefabPath = "UI/StudentID/StudentIdCardNameEntryWindow";

    private static StudentIdCardNameEntryView instance;

    [SerializeField] private string title = "填写学生证";
    [SerializeField] private string subtitle = "请输入你的姓名，确认后会直接印在学生证上。";
    [SerializeField] private string fieldLabel = "姓名";
    [SerializeField] private string placeholder = "请输入你的名字";
    [SerializeField] private string confirmLabel = "确认";
    [SerializeField] private string cancelLabel = "取消";

    private RectTransform panelRoot;
    private TMP_Text titleText;
    private TMP_Text subtitleText;
    private TMP_Text fieldLabelText;
    private TMP_InputField nameInputField;
    private TMP_Text inputText;
    private TMP_Text placeholderText;
    private Button confirmButton;
    private Button cancelButton;
    private Action<string> confirmCallback;
    private Action<string> valueChangedCallback;
    private Action cancelCallback;
    private Coroutine focusCoroutine;
    private bool allowCancel = true;

    public static StudentIdCardNameEntryView GetOrCreate()
    {
        instance = UiRouter.GetOrCreate<StudentIdCardNameEntryView>();
        instance.Prepare();
        return instance;
    }

    public bool IsOpen => IsViewVisible;

    public override bool RegistersWithUiWindowService => true;
    public override int ManagedInputPriority => 100;
    protected override string ViewPrefabId => "StudentIdCardNameEntryWindow";
    protected override string ViewPrefabResourcePath => ViewPrefabPath;
    protected override PrototypeUiLayer Layer => PrototypeUiLayer.Modal;
    protected override bool AddTransparentRootGraphic => false;
    protected override bool VisibleOnAwake => false;
    protected override bool RootGraphicRaycastTarget => true;
    protected override string ViewName => "StudentIdCardNameEntry";
    protected override Color RootGraphicColor => new Color(0.02f, 0.03f, 0.05f, 0.76f);

    public void Open(string initialName, bool canCancel, Action<string> onConfirm, Action onCancel, Action<string> onValueChanged = null)
    {
        Prepare();
        allowCancel = canCancel;
        confirmCallback = onConfirm;
        valueChangedCallback = onValueChanged;
        cancelCallback = onCancel;

        if (nameInputField != null)
        {
            nameInputField.text = string.IsNullOrWhiteSpace(initialName) ? string.Empty : initialName.Trim();
        }

        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(allowCancel);
        }

        RefreshConfirmState();
        SetViewVisible(true);

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
        SetViewVisible(false);
    }

    protected override void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        base.OnDestroy();
    }

    protected override RectTransform ResolveViewRoot(StudentIdCardNameEntryWindowTemplate template)
    {
        return template != null ? template.Root : null;
    }

    protected override void BuildPrefabView(StudentIdCardNameEntryWindowTemplate template, RectTransform root)
    {
        if (template == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing {nameof(StudentIdCardNameEntryWindowTemplate)} on instantiated view root.", this);
            return;
        }

        panelRoot = template.Panel;
        titleText = template.TitleText;
        subtitleText = template.SubtitleText;
        fieldLabelText = template.FieldLabelText;
        nameInputField = template.NameInputField;
        inputText = template.InputText;
        placeholderText = template.PlaceholderText;
        confirmButton = template.ConfirmButton;
        cancelButton = template.CancelButton;

        ApplyStaticLabels();
        HookTemplateCallbacks();
        RefreshConfirmState();
    }

    protected override void OnViewVisibilityChanged(bool visible)
    {
        base.OnViewVisibilityChanged(visible);
        if (!visible && EventSystem.current != null && nameInputField != null && EventSystem.current.currentSelectedGameObject == nameInputField.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    protected override void OnViewRootDestroyed()
    {
        panelRoot = null;
        titleText = null;
        subtitleText = null;
        fieldLabelText = null;
        nameInputField = null;
        inputText = null;
        placeholderText = null;
        confirmButton = null;
        cancelButton = null;
        base.OnViewRootDestroyed();
    }

    private void Prepare()
    {
        EnsureView();
    }

    private void ApplyStaticLabels()
    {
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
            subtitleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(subtitle));
        }

        if (fieldLabelText != null)
        {
            fieldLabelText.text = fieldLabel;
        }

        if (placeholderText != null)
        {
            placeholderText.text = placeholder;
        }

        SetButtonLabel(confirmButton, confirmLabel);
        SetButtonLabel(cancelButton, cancelLabel);
    }

    private void HookTemplateCallbacks()
    {
        if (nameInputField != null)
        {
            nameInputField.lineType = TMP_InputField.LineType.SingleLine;
            nameInputField.characterLimit = 24;
            nameInputField.onValueChanged.RemoveListener(HandleNameValueChanged);
            nameInputField.onValueChanged.AddListener(HandleNameValueChanged);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(TryConfirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(Cancel);
        }
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null;
        focusCoroutine = null;

        if (!IsViewVisible || nameInputField == null)
        {
            yield break;
        }

        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem != null)
        {
            currentEventSystem.SetSelectedGameObject(nameInputField.gameObject);
        }

        nameInputField.ActivateInputField();
        nameInputField.Select();
        nameInputField.caretPosition = nameInputField.text.Length;
    }

    public override bool TryHandleUiSubmit()
    {
        if (!IsViewVisible)
        {
            return false;
        }

        TryConfirm();
        return true;
    }

    public override bool TryHandleUiCancel()
    {
        if (!IsViewVisible || !allowCancel)
        {
            return false;
        }

        Cancel();
        return true;
    }

    private void TryConfirm()
    {
        if (nameInputField == null)
        {
            return;
        }

        string candidate = SanitizeName(nameInputField.text);
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
        if (confirmButton == null)
        {
            return;
        }

        confirmButton.interactable = !string.IsNullOrWhiteSpace(SanitizeName(nameInputField != null ? nameInputField.text : string.Empty));
    }

    private static string SanitizeName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text labelText = button.GetComponentInChildren<TMP_Text>(true);
        if (labelText != null)
        {
            labelText.text = string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
        }
    }
}
