using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StudentIdCardNameEntryView : ViewBase
{
    private static StudentIdCardNameEntryView instance;

    [SerializeField] private string title = "填写学生证";
    [SerializeField] private string subtitle = "请输入你的姓名，确认后会直接印在学生证上。";
    [SerializeField] private string fieldLabel = "姓名";
    [SerializeField] private string placeholder = "请输入你的名字";
    [SerializeField] private string confirmLabel = "确认";
    [SerializeField] private string cancelLabel = "取消";

    private RectTransform panelRoot;
    private Text titleText;
    private Text subtitleText;
    private Text fieldLabelText;
    private InputField nameInputField;
    private Text inputText;
    private Text placeholderText;
    private Button confirmButton;
    private Button cancelButton;

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
    protected override bool VisibleOnAwake => false;
    protected override bool RootGraphicRaycastTarget => true;
    protected override bool ApplyRuntimeFontToChildren => true;
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

    protected override void BuildView(RectTransform root)
    {
        panelRoot = PrototypeUiToolkit.CreatePanel(
            root,
            "Panel",
            new Color(0.09f, 0.11f, 0.16f, 0.97f),
            new RectOffset(24, 24, 24, 24),
            0f);
        PrototypeUiToolkit.SetAnchor(
            panelRoot,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(720f, 260f));

        if (panelRoot.TryGetComponent(out VerticalLayoutGroup layoutGroup))
        {
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandHeight = false;
        }

        titleText = PrototypeUiToolkit.CreateText(panelRoot, RuntimeFont, title, 28, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        SetLayoutHeight(titleText.rectTransform, 38f);

        subtitleText = PrototypeUiToolkit.CreateText(panelRoot, RuntimeFont, subtitle, 15, FontStyle.Normal, new Color(0.84f, 0.88f, 0.94f), TextAnchor.UpperLeft);
        subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
        SetLayoutHeight(subtitleText.rectTransform, 48f);

        RectTransform spacer = PrototypeUiToolkit.CreateRectTransform("Spacer", panelRoot);
        SetLayoutHeight(spacer, 10f);

        fieldLabelText = PrototypeUiToolkit.CreateText(panelRoot, RuntimeFont, fieldLabel, 16, FontStyle.Bold, new Color(0.82f, 0.87f, 0.94f), TextAnchor.UpperLeft);
        SetLayoutHeight(fieldLabelText.rectTransform, 28f);

        BuildInputField(panelRoot);
        BuildButtons(panelRoot);
    }

    private void Prepare()
    {
        EnsureView();
    }

    protected override void OnViewVisibilityChanged(bool visible)
    {
        base.OnViewVisibilityChanged(visible);
        if (!visible && EventSystem.current != null && nameInputField != null && EventSystem.current.currentSelectedGameObject == nameInputField.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
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

    private void BuildInputField(Transform parent)
    {
        RectTransform inputRoot = PrototypeUiToolkit.CreateRectTransform("NameInput", parent);
        SetLayoutHeight(inputRoot, 56f);

        Image background = inputRoot.gameObject.AddComponent<Image>();
        background.color = new Color(0.14f, 0.17f, 0.24f, 1f);

        nameInputField = inputRoot.gameObject.AddComponent<InputField>();
        nameInputField.lineType = InputField.LineType.SingleLine;
        nameInputField.characterLimit = 24;
        nameInputField.onValueChanged.AddListener(HandleNameValueChanged);

        RectTransform textRoot = PrototypeUiToolkit.CreateRectTransform("Text", inputRoot);
        PrototypeUiToolkit.SetStretch(textRoot, 18f, 18f, 10f, 10f);
        inputText = textRoot.gameObject.AddComponent<Text>();
        inputText.font = RuntimeFont;
        inputText.fontSize = 24;
        inputText.fontStyle = FontStyle.Bold;
        inputText.color = Color.white;
        inputText.alignment = TextAnchor.MiddleLeft;
        inputText.supportRichText = false;
        inputText.horizontalOverflow = HorizontalWrapMode.Overflow;
        inputText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform placeholderRoot = PrototypeUiToolkit.CreateRectTransform("Placeholder", inputRoot);
        PrototypeUiToolkit.SetStretch(placeholderRoot, 18f, 18f, 10f, 10f);
        placeholderText = placeholderRoot.gameObject.AddComponent<Text>();
        placeholderText.font = RuntimeFont;
        placeholderText.fontSize = 22;
        placeholderText.fontStyle = FontStyle.Normal;
        placeholderText.color = new Color(0.56f, 0.62f, 0.7f, 0.92f);
        placeholderText.alignment = TextAnchor.MiddleLeft;
        placeholderText.text = placeholder;
        placeholderText.supportRichText = false;

        nameInputField.textComponent = inputText;
        nameInputField.placeholder = placeholderText;
        nameInputField.targetGraphic = background;
    }

    private void BuildButtons(Transform parent)
    {
        RectTransform row = PrototypeUiToolkit.CreateRectTransform("Buttons", parent);
        SetLayoutHeight(row, 44f);

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = 44f;
        rowLayout.preferredHeight = 44f;
        rowLayout.flexibleWidth = 1f;

        confirmButton = PrototypeUiToolkit.CreateButton(
            row,
            RuntimeFont,
            confirmLabel,
            TryConfirm,
            new Color(0.16f, 0.63f, 0.86f, 1f),
            new Color(0.24f, 0.72f, 0.95f, 1f),
            new Color(0.11f, 0.44f, 0.64f, 1f),
            44f);
        SetButtonWidth(confirmButton, 168f);

        cancelButton = PrototypeUiToolkit.CreateButton(
            row,
            RuntimeFont,
            cancelLabel,
            Cancel,
            new Color(0.26f, 0.29f, 0.36f, 1f),
            new Color(0.34f, 0.38f, 0.46f, 1f),
            new Color(0.18f, 0.21f, 0.28f, 1f),
            44f);
        SetButtonWidth(cancelButton, 140f);
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

    private static void SetLayoutHeight(RectTransform rectTransform, float height)
    {
        if (rectTransform == null)
        {
            return;
        }

        LayoutElement layoutElement = rectTransform.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
        layoutElement.flexibleWidth = 1f;
    }

    private static void SetButtonWidth(Button button, float width)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rectTransform = button.transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = button.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = width;
        layoutElement.preferredWidth = width;
        rectTransform.sizeDelta = new Vector2(width, 44f);
    }
}
