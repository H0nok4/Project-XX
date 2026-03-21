using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class StudentIdCardRenderer : MonoBehaviour
{
    private const string NameOverlayResourcePath = "UI/StudentID/StudentIdCardNameOverlay";

    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private MeshRenderer printSurfaceRenderer;
    [SerializeField] private StudentIdCardNameOverlayTemplate nameOverlayTemplatePrefab;
    [SerializeField] private StudentIdCardContent content = new StudentIdCardContent();
    [SerializeField] private Vector2Int textureSize = new Vector2Int(1024, 640);
    [SerializeField] private Vector3 nameOverlayLocalPosition = new Vector3(0.00053f, 0.00103f, 0.00106f);
    [SerializeField] private Vector3 nameOverlayLocalEulerAngles = new Vector3(90f, 10.6f, 0f);
    [SerializeField] private Vector3 nameOverlayLocalScale = new Vector3(0.0001237484f, 0.0001237484f, 1f);
    [SerializeField] private bool previewInEditMode = true;
    [SerializeField] private bool refreshEveryFrame;

    private StudentIdCardNameOverlayTemplate nameOverlayInstance;
    private Font runtimeFont;
    private bool isDirty = true;
    private bool legacyPrintSurfaceWasEnabled;
    private bool legacyPrintSurfaceStateCaptured;

    public StudentIdCardContent Content => content;
    public Texture PreviewTexture => null;

    public void ConfigureReferences(
        MeshRenderer configuredBodyRenderer,
        MeshRenderer configuredPrintSurfaceRenderer,
        StudentIdCardFaceTemplate unusedFaceTemplate)
    {
        ConfigureReferences(configuredBodyRenderer, configuredPrintSurfaceRenderer, (StudentIdCardNameOverlayTemplate)null);
    }

    public void ConfigureReferences(
        MeshRenderer configuredBodyRenderer,
        MeshRenderer configuredPrintSurfaceRenderer,
        StudentIdCardNameOverlayTemplate configuredNameOverlayTemplate)
    {
        bodyRenderer = configuredBodyRenderer;
        printSurfaceRenderer = configuredPrintSurfaceRenderer;
        nameOverlayTemplatePrefab = configuredNameOverlayTemplate;
        MarkDirty();
    }

    public void RefreshCard()
    {
        MarkDirty();
        TryRenderCard();
    }

    public void SetIdentity(string studentName, string studentNumber, Sprite portrait = null)
    {
        content ??= new StudentIdCardContent();
        content.FullName = studentName;
        content.StudentNumber = studentNumber;
        if (portrait != null)
        {
            content.PortraitSprite = portrait;
        }

        RefreshCard();
    }

    private void OnEnable()
    {
        MarkDirty();
        TryRenderCard();
    }

    private void Start()
    {
        TryRenderCard();
    }

    private void LateUpdate()
    {
        if (!refreshEveryFrame && !isDirty)
        {
            return;
        }

        TryRenderCard();
    }

    private void OnValidate()
    {
        textureSize.x = Mathf.Max(256, textureSize.x);
        textureSize.y = Mathf.Max(256, textureSize.y);
        content ??= new StudentIdCardContent();
        content.Sanitize();
        MarkDirty();

#if UNITY_EDITOR
        if (!Application.isPlaying && previewInEditMode)
        {
            UnityEditor.EditorApplication.delayCall -= HandleEditorPreviewRefresh;
            UnityEditor.EditorApplication.delayCall += HandleEditorPreviewRefresh;
        }
        else if (!Application.isPlaying)
        {
            ReleaseRuntimeResources();
        }
#endif
    }

    private void OnDisable()
    {
        ReleaseRuntimeResources();
    }

    private void OnDestroy()
    {
        ReleaseRuntimeResources();
    }

    private void TryRenderCard()
    {
        if (!CanRender())
        {
            return;
        }

        TryResolveReferences();
        if (bodyRenderer == null || nameOverlayTemplatePrefab == null)
        {
            return;
        }

        content ??= new StudentIdCardContent();
        content.Sanitize();
        runtimeFont ??= PrototypeUiToolkit.ResolveDefaultFont();

        HideLegacyPrintSurface();
        EnsureNameOverlay();
        if (nameOverlayInstance == null)
        {
            return;
        }

        UpdateNameOverlayTransform();
        DisableRaycasts(nameOverlayInstance.gameObject);
        nameOverlayInstance.Apply(content.FullName, runtimeFont);
        Canvas.ForceUpdateCanvases();
        isDirty = false;
    }

    private bool CanRender()
    {
        if (!isActiveAndEnabled)
        {
            return false;
        }

        if (Application.isPlaying)
        {
            return true;
        }

#if UNITY_EDITOR
        if (UnityEditor.EditorUtility.IsPersistent(this))
        {
            return false;
        }
#endif
        return previewInEditMode;
    }

    private void TryResolveReferences()
    {
        if (bodyRenderer == null)
        {
            bodyRenderer = FindRenderer("StudentID_Card");
        }

        if (printSurfaceRenderer == null)
        {
            printSurfaceRenderer = FindRenderer("CardPrintSurface");
        }

        if (nameOverlayTemplatePrefab == null)
        {
            nameOverlayTemplatePrefab = Resources.Load<StudentIdCardNameOverlayTemplate>(NameOverlayResourcePath);
        }
    }

    private MeshRenderer FindRenderer(string rendererName)
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        for (int index = 0; index < renderers.Length; index++)
        {
            if (renderers[index] != null && renderers[index].name == rendererName)
            {
                return renderers[index];
            }
        }

        return null;
    }

    private void EnsureNameOverlay()
    {
        if (bodyRenderer == null || nameOverlayTemplatePrefab == null || nameOverlayInstance != null)
        {
            return;
        }

        nameOverlayInstance = Instantiate(nameOverlayTemplatePrefab, transform, false);
        nameOverlayInstance.name = "StudentIdCardNameOverlay";
        nameOverlayInstance.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        SetLayerRecursively(nameOverlayInstance.gameObject, bodyRenderer.gameObject.layer);
    }

    private void UpdateNameOverlayTransform()
    {
        if (nameOverlayInstance == null || bodyRenderer == null)
        {
            return;
        }

        Transform overlayTransform = nameOverlayInstance.transform;
        overlayTransform.SetParent(transform, false);
        overlayTransform.localPosition = nameOverlayLocalPosition;
        overlayTransform.localRotation = Quaternion.Euler(nameOverlayLocalEulerAngles);
        overlayTransform.localScale = nameOverlayLocalScale;

        RectTransform overlayRoot = nameOverlayInstance.Root;
        if (overlayRoot != null)
        {
            overlayRoot.sizeDelta = textureSize;
            overlayRoot.pivot = new Vector2(0.5f, 0.5f);
        }

        Canvas overlayCanvas = nameOverlayInstance.WorldCanvas;
        if (overlayCanvas == null)
        {
            return;
        }

        overlayCanvas.renderMode = RenderMode.WorldSpace;
        overlayCanvas.worldCamera = null;
        overlayCanvas.pixelPerfect = true;
        overlayCanvas.overrideSorting = false;
        overlayCanvas.sortingLayerID = bodyRenderer.sortingLayerID;
        overlayCanvas.sortingOrder = bodyRenderer.sortingOrder + 1;
    }

    private void HideLegacyPrintSurface()
    {
        if (printSurfaceRenderer == null)
        {
            return;
        }

        if (!legacyPrintSurfaceStateCaptured)
        {
            legacyPrintSurfaceWasEnabled = printSurfaceRenderer.enabled;
            legacyPrintSurfaceStateCaptured = true;
        }

        printSurfaceRenderer.enabled = false;
    }

    private void ReleaseRuntimeResources()
    {
        if (printSurfaceRenderer != null && legacyPrintSurfaceStateCaptured)
        {
            printSurfaceRenderer.enabled = legacyPrintSurfaceWasEnabled;
        }

        legacyPrintSurfaceStateCaptured = false;

        if (nameOverlayInstance != null)
        {
            DestroyUnityObject(nameOverlayInstance.gameObject);
            nameOverlayInstance = null;
        }
    }

    private void MarkDirty()
    {
        isDirty = true;
    }

#if UNITY_EDITOR
    private void HandleEditorPreviewRefresh()
    {
        UnityEditor.EditorApplication.delayCall -= HandleEditorPreviewRefresh;
        if (this == null)
        {
            return;
        }

        TryRenderCard();
    }
#endif

    private static void DisableRaycasts(GameObject rootObject)
    {
        if (rootObject == null)
        {
            return;
        }

        Graphic[] graphics = rootObject.GetComponentsInChildren<Graphic>(true);
        for (int index = 0; index < graphics.Length; index++)
        {
            if (graphics[index] != null)
            {
                graphics[index].raycastTarget = false;
            }
        }
    }

    private static void SetLayerRecursively(GameObject rootObject, int layer)
    {
        if (rootObject == null)
        {
            return;
        }

        rootObject.layer = layer;
        Transform rootTransform = rootObject.transform;
        for (int index = 0; index < rootTransform.childCount; index++)
        {
            SetLayerRecursively(rootTransform.GetChild(index).gameObject, layer);
        }
    }

    private static void DestroyUnityObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
