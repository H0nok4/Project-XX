using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StudentIdCardNameEntryView : ViewBase
{
    private const string ViewPrefabResourcePath = "UI/StudentID/StudentIdCardNameEntryWindow";

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
    private StudentIdCardNameEntryWindowTemplate viewTemplate;

    private Action<string> confirmCallback;
    private Action<string> valueChangedCallback;
    private Action cancelCallback;
    private Coroutine focusCoroutine;
    private bool allowCancel = true;

    public static StudentIdCardNameEntryView GetOrCreate()
    {
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        instance = FindFirstObjectByType<StudentIdCardNameEntryView>();
        if (instance != null)
        {
            instance.Prepare();
            return instance;
        }

        GameObject root = new GameObject("StudentIdCardNameEntryView");
        instance = root.AddComponent<StudentIdCardNameEntryView>();
        instance.Prepare();
        return instance;
    }

    public bool IsOpen => IsViewVisible;

    protected override PrototypeUiLayer Layer => PrototypeUiLayer.Modal;
    protected override bool AddTransparentRootGraphic => false;
    protected override bool VisibleOnAwake => false;
    protected override bool RootGraphicRaycastTarget => true;
    protected override bool ApplyRuntimeFontToChildren => true;
    protected override string ViewName => "StudentIdCardNameEntry";
    protected override Color RootGraphicColor => new Color(0.02f, 0.03f, 0.05f, 0.76f);

    protected override RectTransform CreateViewRoot()
    {
        RectTransform parent = UiManager.GetLayerRoot(Layer);
        GameObject prefabAsset = Resources.Load<GameObject>(ViewPrefabResourcePath);
        if (prefabAsset == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing view prefab at Resources/{ViewPrefabResourcePath}.", this);
            return null;
        }

        GameObject instanceObject = Instantiate(prefabAsset, parent, false);
        instanceObject.name = prefabAsset.name;

        viewTemplate = instanceObject.GetComponent<StudentIdCardNameEntryWindowTemplate>();
        if (viewTemplate == null || viewTemplate.Root == null)
        {
            Destroy(instanceObject);
            viewTemplate = null;
            Debug.LogWarning($"[{GetType().Name}] Student ID name entry prefab is missing {nameof(StudentIdCardNameEntryWindowTemplate)}.", this);
            return null;
        }

        return viewTemplate.Root;
    }

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

    protected override void BuildView(RectTransform root)
    {
        if (root == null)
        {
            return;
        }

        if (viewTemplate == null || viewTemplate.Root != root)
        {
            viewTemplate = root.GetComponent<StudentIdCardNameEntryWindowTemplate>();
        }

        if (viewTemplate == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Missing {nameof(StudentIdCardNameEntryWindowTemplate)} on instantiated view root.", this);
            return;
        }

        panelRoot = viewTemplate.Panel;
        titleText = viewTemplate.TitleText;
        subtitleText = viewTemplate.SubtitleText;
        fieldLabelText = viewTemplate.FieldLabelText;
        nameInputField = viewTemplate.NameInputField;
        inputText = viewTemplate.InputText;
        placeholderText = viewTemplate.PlaceholderText;
        confirmButton = viewTemplate.ConfirmButton;
        cancelButton = viewTemplate.CancelButton;

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
        viewTemplate = null;
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

    private void Update()
    {
        if (!IsViewVisible || Keyboard.current == null)
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
