using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class StudentIdCardRenderer : MonoBehaviour
{
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private MeshRenderer printSurfaceRenderer;
    [SerializeField] private StudentIdCardFaceTemplate faceTemplatePrefab;
    [SerializeField] private StudentIdCardContent content = new StudentIdCardContent();
    [SerializeField] private Vector2Int textureSize = new Vector2Int(1024, 640);
    [SerializeField] private Vector2 canvasWorldSize = new Vector2(1.024f, 0.64f);
    [SerializeField] private float renderDistance = 1f;
    [SerializeField] private bool previewInEditMode = true;
    [SerializeField] private bool refreshEveryFrame;

    private GameObject renderRigRoot;
    private Camera renderCamera;
    private Canvas renderCanvas;
    private StudentIdCardFaceTemplate faceInstance;
    private RenderTexture renderTexture;
    private MaterialPropertyBlock materialPropertyBlock;
    private Font runtimeFont;
    private bool isDirty = true;

    public StudentIdCardContent Content => content;
    public Texture PreviewTexture => renderTexture;

    public void ConfigureReferences(
        MeshRenderer configuredBodyRenderer,
        MeshRenderer configuredPrintSurfaceRenderer,
        StudentIdCardFaceTemplate configuredFaceTemplate)
    {
        bodyRenderer = configuredBodyRenderer;
        printSurfaceRenderer = configuredPrintSurfaceRenderer;
        faceTemplatePrefab = configuredFaceTemplate;
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
        canvasWorldSize.x = Mathf.Max(0.2f, canvasWorldSize.x);
        canvasWorldSize.y = Mathf.Max(0.2f, canvasWorldSize.y);
        renderDistance = Mathf.Max(0.1f, renderDistance);
        content ??= new StudentIdCardContent();
        content.Sanitize();
        MarkDirty();

#if UNITY_EDITOR
        if (!Application.isPlaying && previewInEditMode)
        {
            UnityEditor.EditorApplication.delayCall -= HandleEditorPreviewRefresh;
            UnityEditor.EditorApplication.delayCall += HandleEditorPreviewRefresh;
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
        if (printSurfaceRenderer == null || faceTemplatePrefab == null)
        {
            return;
        }

        content ??= new StudentIdCardContent();
        content.Sanitize();
        runtimeFont ??= PrototypeUiToolkit.ResolveDefaultFont();

        EnsureRenderRig();
        EnsureRenderTexture();
        if (renderCamera == null || renderCanvas == null || faceInstance == null || renderTexture == null)
        {
            return;
        }

        faceInstance.Apply(content, runtimeFont);
        Canvas.ForceUpdateCanvases();
        UpdateRenderCameraMode();
        ApplyTexture();
        isDirty = false;
    }

    private bool CanRender()
    {
        if (!isActiveAndEnabled)
        {
            return false;
        }

        if (!Application.isPlaying)
        {
            return false;
        }

#if UNITY_EDITOR
        if (UnityEditor.EditorUtility.IsPersistent(this))
        {
            return false;
        }
#endif
        return true;
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

        if (faceTemplatePrefab == null)
        {
            faceTemplatePrefab = Resources.Load<StudentIdCardFaceTemplate>("UI/StudentID/StudentIdCardFace");
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

    private void EnsureRenderRig()
    {
        if (renderRigRoot == null)
        {
            renderRigRoot = new GameObject($"{name}_StudentIdCardRenderRig");
            renderRigRoot.hideFlags = HideFlags.HideAndDontSave;
            renderRigRoot.transform.position = new Vector3(10000f, 10000f, 10000f);
            renderRigRoot.transform.rotation = Quaternion.identity;
        }

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0)
        {
            uiLayer = 0;
        }

        if (renderCamera == null)
        {
            GameObject cameraObject = new GameObject("CardFaceCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            cameraObject.transform.SetParent(renderRigRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -renderDistance);
            cameraObject.transform.localRotation = Quaternion.identity;
            cameraObject.layer = uiLayer;

            renderCamera = cameraObject.AddComponent<Camera>();
            renderCamera.orthographic = true;
            renderCamera.enabled = false;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.clear;
            renderCamera.allowHDR = false;
            renderCamera.allowMSAA = false;
            renderCamera.cullingMask = 1 << uiLayer;
            renderCamera.nearClipPlane = 0.01f;
            renderCamera.farClipPlane = 4f;
        }

        if (renderCanvas == null)
        {
            GameObject canvasObject = new GameObject("CardFaceCanvas", typeof(RectTransform));
            canvasObject.hideFlags = HideFlags.HideAndDontSave;
            canvasObject.transform.SetParent(renderRigRoot.transform, false);
            canvasObject.transform.localPosition = Vector3.zero;
            canvasObject.transform.localRotation = Quaternion.identity;
            SetLayerRecursively(canvasObject, uiLayer);

            renderCanvas = canvasObject.AddComponent<Canvas>();
            renderCanvas.renderMode = RenderMode.WorldSpace;
            renderCanvas.worldCamera = renderCamera;
            renderCanvas.pixelPerfect = true;
            renderCanvas.planeDistance = 1f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = textureSize;
        }

        UpdateRenderRigSizing();

        if (faceInstance == null && faceTemplatePrefab != null)
        {
            faceInstance = Instantiate(faceTemplatePrefab, renderCanvas.transform, false);
            faceInstance.gameObject.hideFlags = HideFlags.HideAndDontSave;
            SetLayerRecursively(faceInstance.gameObject, uiLayer);

            RectTransform faceRect = faceInstance.Root;
            if (faceRect != null)
            {
                PrototypeUiToolkit.SetStretch(faceRect, 0f, 0f, 0f, 0f);
                faceRect.localScale = Vector3.one;
                faceRect.localRotation = Quaternion.identity;
            }
        }
    }

    private void UpdateRenderRigSizing()
    {
        if (renderCanvas == null || renderCamera == null)
        {
            return;
        }

        RectTransform canvasRect = renderCanvas.transform as RectTransform;
        if (canvasRect != null)
        {
            canvasRect.sizeDelta = textureSize;
            canvasRect.localScale = new Vector3(
                canvasWorldSize.x / textureSize.x,
                canvasWorldSize.y / textureSize.y,
                1f);
        }

        renderCamera.orthographicSize = canvasWorldSize.y * 0.5f;
        renderCamera.aspect = Mathf.Max(0.01f, (float)textureSize.x / textureSize.y);
        renderCamera.transform.localPosition = new Vector3(0f, 0f, -renderDistance);
        renderCamera.transform.localRotation = Quaternion.identity;
    }

    private void EnsureRenderTexture()
    {
        if (renderTexture != null && (renderTexture.width != textureSize.x || renderTexture.height != textureSize.y))
        {
            DestroyTexture();
        }

        if (renderTexture != null)
        {
            return;
        }

        renderTexture = new RenderTexture(textureSize.x, textureSize.y, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
        {
            name = $"{name}_StudentIdCardFaceRT",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            antiAliasing = 1,
            hideFlags = HideFlags.HideAndDontSave
        };
        renderTexture.Create();
    }

    private void UpdateRenderCameraMode()
    {
        if (renderCamera == null)
        {
            return;
        }

        renderCamera.targetTexture = renderTexture;
        renderCamera.enabled = Application.isPlaying;
    }

    private void ApplyTexture()
    {
        if (printSurfaceRenderer == null || renderTexture == null)
        {
            return;
        }

        materialPropertyBlock ??= new MaterialPropertyBlock();
        printSurfaceRenderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetTexture(BaseMapId, renderTexture);
        materialPropertyBlock.SetTexture(MainTexId, renderTexture);
        printSurfaceRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void ReleaseRuntimeResources()
    {
        if (printSurfaceRenderer != null && materialPropertyBlock != null)
        {
            materialPropertyBlock.Clear();
            printSurfaceRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        faceInstance = null;
        renderCanvas = null;
        renderCamera = null;

        DestroyTexture();
        DestroyUnityObject(renderRigRoot);
        renderRigRoot = null;
    }

    private void DestroyTexture()
    {
        if (renderTexture == null)
        {
            return;
        }

        renderTexture.Release();
        DestroyUnityObject(renderTexture);
        renderTexture = null;
    }

    private void MarkDirty()
    {
        isDirty = true;
    }

#if UNITY_EDITOR
    private void HandleEditorPreviewRefresh()
    {
        UnityEditor.EditorApplication.delayCall -= HandleEditorPreviewRefresh;
        if (this == null || Application.isPlaying)
        {
            return;
        }

        TryRenderCard();
    }
#endif

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
