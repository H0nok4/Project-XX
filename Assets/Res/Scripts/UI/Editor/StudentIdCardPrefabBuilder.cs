using System;
using UnityEditor;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class StudentIdCardPrefabBuilder
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string UiFolder = "Assets/Resources/UI";
    private const string StudentIdUiFolder = "Assets/Resources/UI/StudentID";
    private const string FacePrefabPath = "Assets/Resources/UI/StudentID/StudentIdCardFace.prefab";
    private const string NameEntryWindowPrefabPath = "Assets/Resources/UI/StudentID/StudentIdCardNameEntryWindow.prefab";
    private const string PrefabFolder = "Assets/Res/Prefabs/StudentID";
    private const string CardPrefabPath = "Assets/Res/Prefabs/StudentID/StudentIdCard.prefab";
    private const string MaterialFolder = "Assets/Res/Materials/SchoolCampus/StudentID";
    private const string BodyMaterialPath = "Assets/Res/Materials/SchoolCampus/StudentID/StudentIdPlasticBody.mat";
    private const string PrintMaterialPath = "Assets/Res/Materials/SchoolCampus/StudentID/StudentIdPrintOverlay.mat";
    private const string ModelAssetPath = "Assets/Res/Models/SchoolCampus/StudentID/student_id_card.fbx";

    [MenuItem("Tools/Prototype/Build Student ID Card Prefab")]
    public static void BuildPrefab()
    {
        string result = BuildPrefabAndReport();
        if (!string.IsNullOrWhiteSpace(result))
        {
            Debug.Log(result);
        }
    }

    public static string BuildPrefabAndReport()
    {
        EnsureFolder(ResourcesFolder);
        EnsureFolder(UiFolder);
        EnsureFolder(StudentIdUiFolder);
        EnsureFolder("Assets/Res/Materials");
        EnsureFolder("Assets/Res/Materials/SchoolCampus");
        EnsureFolder(MaterialFolder);
        EnsureFolder("Assets/Res/Prefabs");
        EnsureFolder(PrefabFolder);

        StudentIdCardFaceTemplate faceTemplate = BuildFaceTemplatePrefab();
        BuildNameEntryWindowPrefab();
        Material bodyMaterial = BuildBodyMaterial();
        Material printMaterial = BuildPrintMaterial();

        GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
        if (sourceModel == null)
        {
            return $"Failed to find model at {ModelAssetPath}";
        }

        GameObject root = new GameObject("StudentIdCard");
        try
        {
            GameObject modelInstance = PrefabUtility.InstantiatePrefab(sourceModel) as GameObject;
            if (modelInstance == null)
            {
                return $"Failed to instantiate {ModelAssetPath}";
            }

            modelInstance.name = "Model";
            modelInstance.transform.SetParent(root.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            MeshRenderer[] renderers = modelInstance.GetComponentsInChildren<MeshRenderer>(true);
            MeshRenderer bodyRenderer = null;
            MeshRenderer printSurfaceRenderer = null;
            for (int index = 0; index < renderers.Length; index++)
            {
                MeshRenderer renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                if (renderer.name == "StudentID_Card")
                {
                    bodyRenderer = renderer;
                    renderer.sharedMaterial = bodyMaterial;
                    continue;
                }

                if (renderer.name == "CardPrintSurface")
                {
                    printSurfaceRenderer = renderer;
                    continue;
                }
            }

            if (bodyRenderer == null)
            {
                return "StudentID_Card renderer was not found inside the imported FBX.";
            }

            if (printSurfaceRenderer == null)
            {
                printSurfaceRenderer = CreatePrintSurface(bodyRenderer, printMaterial);
            }

            ConfigurePrintSurface(printSurfaceRenderer, printMaterial);

            StudentIdCardRenderer cardRenderer = root.AddComponent<StudentIdCardRenderer>();
            cardRenderer.ConfigureReferences(bodyRenderer, printSurfaceRenderer, faceTemplate);
            cardRenderer.RefreshCard();

            CreateInteractionVolume(root.transform, bodyRenderer);
            CreateInspectCameraAnchor(root.transform, bodyRenderer);
            root.AddComponent<StudentIdCardTutorialInteractable>();

            PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
        if (prefab != null)
        {
            EditorGUIUtility.PingObject(prefab);
            return $"Built {CardPrefabPath}, {FacePrefabPath}, {NameEntryWindowPrefabPath}, {BodyMaterialPath}, {PrintMaterialPath}";
        }

        return $"Failed to create prefab at {CardPrefabPath}";
    }

    private static StudentIdCardFaceTemplate BuildFaceTemplatePrefab()
    {
        GameObject root = new GameObject("StudentIdCardFace", typeof(RectTransform));
        try
        {
            root.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1024f, 640f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = PrototypeUiToolkit.ResolveDefaultFont();
            }

            Image background = root.AddComponent<Image>();
            background.color = Color.clear;
            background.raycastTarget = false;

            Image topBand = CreateImage("TopBand", rect, Color.clear);
            PrototypeUiToolkit.SetAnchor((RectTransform)topBand.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 118f));

            Image logo = CreateImage("Logo", topBand.transform, new Color(0.08f, 0.12f, 0.16f, 0.85f));
            RectTransform logoRect = (RectTransform)logo.transform;
            PrototypeUiToolkit.SetAnchor(logoRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(82f, 82f));
            logo.preserveAspect = true;

            TMP_Text schoolName = CreateText("SchoolName", topBand.transform, font, 34, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            RectTransform schoolRect = (RectTransform)schoolName.transform;
            PrototypeUiToolkit.SetAnchor(schoolRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(128f, -18f), new Vector2(560f, 76f));

            TMP_Text cardTitle = CreateText("CardTitle", topBand.transform, font, 34, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);
            RectTransform titleRect = (RectTransform)cardTitle.transform;
            PrototypeUiToolkit.SetAnchor(titleRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -18f), new Vector2(220f, 76f));

            Image portraitFrame = CreateImage("PortraitFrame", rect, Color.clear);
            RectTransform portraitFrameRect = (RectTransform)portraitFrame.transform;
            PrototypeUiToolkit.SetAnchor(portraitFrameRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -154f), new Vector2(224f, 292f));
            portraitFrame.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            Image portrait = CreateImage("Portrait", portraitFrame.transform, Color.white);
            RectTransform portraitRect = (RectTransform)portrait.transform;
            PrototypeUiToolkit.SetStretch(portraitRect, 14f, 14f, 14f, 14f);
            portrait.preserveAspect = true;

            TMP_Text studentNumberLabel = CreateText("StudentNumberLabel", rect, font, 20, FontStyle.Normal, new Color(0.33f, 0.35f, 0.38f), TextAnchor.MiddleLeft);
            PrototypeUiToolkit.SetAnchor((RectTransform)studentNumberLabel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340f, -182f), new Vector2(144f, 32f));

            TMP_Text studentNumberValue = CreateText("StudentNumberValue", rect, font, 34, FontStyle.Normal, new Color(0.15f, 0.16f, 0.18f), TextAnchor.MiddleLeft);
            PrototypeUiToolkit.SetAnchor((RectTransform)studentNumberValue.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(510f, -170f), new Vector2(320f, 48f));

            TMP_Text nameLabel = CreateText("NameLabel", rect, font, 22, FontStyle.Normal, new Color(0.33f, 0.35f, 0.38f), TextAnchor.MiddleLeft);
            PrototypeUiToolkit.SetAnchor((RectTransform)nameLabel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340f, -282f), new Vector2(144f, 32f));

            TMP_Text nameValue = CreateText("NameValue", rect, font, 30, FontStyle.Normal, new Color(0.15f, 0.16f, 0.18f), TextAnchor.MiddleLeft);
            PrototypeUiToolkit.SetAnchor((RectTransform)nameValue.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(510f, -270f), new Vector2(320f, 48f));

            Image nameUnderline = CreateImage("NameUnderline", rect, new Color(0.55f, 0.58f, 0.62f, 0.8f));
            PrototypeUiToolkit.SetAnchor((RectTransform)nameUnderline.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(512f, -315f), new Vector2(344f, 4f));

            TMP_Text birthDateLabel = CreateText("BirthDateLabel", rect, font, 20, FontStyle.Normal, new Color(0.33f, 0.35f, 0.38f), TextAnchor.MiddleLeft);
            PrototypeUiToolkit.SetAnchor((RectTransform)birthDateLabel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340f, -388f), new Vector2(144f, 32f));

            TMP_Text birthDateValue = CreateText("BirthDateValue", rect, font, 24, FontStyle.Normal, new Color(0.15f, 0.16f, 0.18f), TextAnchor.MiddleLeft);
            PrototypeUiToolkit.SetAnchor((RectTransform)birthDateValue.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(510f, -380f), new Vector2(320f, 40f));

            TMP_Text issueDateLabel = CreateText("IssueDateLabel", rect, font, 20, FontStyle.Normal, new Color(0.33f, 0.35f, 0.38f), TextAnchor.MiddleLeft);
            PrototypeUiToolkit.SetAnchor((RectTransform)issueDateLabel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340f, -446f), new Vector2(144f, 32f));

            TMP_Text issueDateValue = CreateText("IssueDateValue", rect, font, 24, FontStyle.Normal, new Color(0.15f, 0.16f, 0.18f), TextAnchor.MiddleLeft);
            PrototypeUiToolkit.SetAnchor((RectTransform)issueDateValue.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(510f, -438f), new Vector2(320f, 40f));

            TMP_Text note = CreateText("Note", rect, font, 18, FontStyle.Normal, new Color(0.42f, 0.43f, 0.46f), TextAnchor.UpperLeft);
            PrototypeUiToolkit.SetAnchor((RectTransform)note.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340f, -504f), new Vector2(430f, 56f));

            TMP_Text issuer = CreateText("Issuer", rect, font, 20, FontStyle.Bold, new Color(0.30f, 0.31f, 0.33f), TextAnchor.LowerRight);
            PrototypeUiToolkit.SetAnchor((RectTransform)issuer.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-46f, 30f), new Vector2(340f, 44f));

            Image seal = CreateImage("Seal", rect, new Color(0.82f, 0.22f, 0.22f, 0.75f));
            RectTransform sealRect = (RectTransform)seal.transform;
            PrototypeUiToolkit.SetAnchor(sealRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-46f, 82f), new Vector2(86f, 86f));
            seal.preserveAspect = true;

            StudentIdCardFaceTemplate template = root.AddComponent<StudentIdCardFaceTemplate>();
            template.ConfigureReferences(
                rect,
                null,
                logo,
                null,
                portrait,
                seal,
                schoolName,
                cardTitle,
                studentNumberLabel,
                studentNumberValue,
                nameLabel,
                nameValue,
                birthDateLabel,
                birthDateValue,
                issueDateLabel,
                issueDateValue,
                note,
                issuer);

            PrefabUtility.SaveAsPrefabAsset(root, FacePrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FacePrefabPath);
        return prefab != null ? prefab.GetComponent<StudentIdCardFaceTemplate>() : null;
    }

    private static StudentIdCardNameEntryWindowTemplate BuildNameEntryWindowPrefab()
    {
        GameObject root = new GameObject("StudentIdCardNameEntryWindow", typeof(RectTransform));
        try
        {
            root.layer = LayerMask.NameToLayer("UI");
            RectTransform rootRect = root.GetComponent<RectTransform>();
            PrototypeUiToolkit.SetStretch(rootRect, 0f, 0f, 0f, 0f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = PrototypeUiToolkit.ResolveDefaultFont();
            }

            Image overlay = root.AddComponent<Image>();
            overlay.color = new Color(0.02f, 0.03f, 0.05f, 0.58f);
            overlay.raycastTarget = true;

            RectTransform panel = PrototypeUiToolkit.CreatePanel(
                rootRect,
                "Panel",
                new Color(0.09f, 0.11f, 0.16f, 0.97f),
                new RectOffset(24, 24, 24, 24),
                10f);
            PrototypeUiToolkit.SetAnchor(
                panel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 280f));

            TMP_Text titleText = CreateText("TitleText", panel, font, 28, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
            titleText.text = "填写学生证";
            titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;

            TMP_Text subtitleText = CreateText("SubtitleText", panel, font, 15, FontStyle.Normal, new Color(0.84f, 0.88f, 0.94f), TextAnchor.UpperLeft);
            subtitleText.text = "请输入你的名字，确认后会直接印在学生证上。";
            subtitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

            RectTransform bodyRoot = PrototypeUiToolkit.CreateRectTransform("Body", panel);
            VerticalLayoutGroup bodyLayout = bodyRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyLayout.spacing = 10f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            LayoutElement bodyLayoutElement = bodyRoot.gameObject.AddComponent<LayoutElement>();
            bodyLayoutElement.flexibleWidth = 1f;
            bodyLayoutElement.flexibleHeight = 1f;
            bodyLayoutElement.minHeight = 0f;

            TMP_Text fieldLabel = CreateText("FieldLabel", bodyRoot, font, 16, FontStyle.Bold, new Color(0.82f, 0.87f, 0.94f), TextAnchor.UpperLeft);
            fieldLabel.text = "姓名";
            fieldLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            RectTransform inputRoot = PrototypeUiToolkit.CreateRectTransform("NameInput", bodyRoot);
            LayoutElement inputLayout = inputRoot.gameObject.AddComponent<LayoutElement>();
            inputLayout.preferredHeight = 56f;
            Image inputBackground = inputRoot.gameObject.AddComponent<Image>();
            inputBackground.color = new Color(0.14f, 0.17f, 0.24f, 1f);

            TMP_InputField inputField = inputRoot.gameObject.AddComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.characterLimit = 24;
            inputField.targetGraphic = inputBackground;

            RectTransform inputTextRoot = PrototypeUiToolkit.CreateRectTransform("Text", inputRoot);
            PrototypeUiToolkit.SetStretch(inputTextRoot, 18f, 18f, 10f, 10f);
            TextMeshProUGUI inputText = inputTextRoot.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(inputText, font, 24, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, false);
            inputText.enableWordWrapping = false;

            RectTransform placeholderRoot = PrototypeUiToolkit.CreateRectTransform("Placeholder", inputRoot);
            PrototypeUiToolkit.SetStretch(placeholderRoot, 18f, 18f, 10f, 10f);
            TextMeshProUGUI placeholderText = placeholderRoot.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(placeholderText, font, 22, FontStyle.Normal, new Color(0.56f, 0.62f, 0.7f, 0.92f), TextAnchor.MiddleLeft, false);
            placeholderText.text = "请输入你的名字";
            placeholderText.enableWordWrapping = false;

            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;

            RectTransform footerRoot = PrototypeUiToolkit.CreateRectTransform("Footer", panel);
            HorizontalLayoutGroup footerLayout = footerRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 12f;
            footerLayout.childAlignment = TextAnchor.MiddleRight;
            footerLayout.childControlWidth = false;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = false;
            footerLayout.childForceExpandHeight = false;

            LayoutElement footerLayoutElement = footerRoot.gameObject.AddComponent<LayoutElement>();
            footerLayoutElement.flexibleWidth = 1f;
            footerLayoutElement.preferredHeight = 44f;

            Button confirmButton = CreateButton(
                footerRoot,
                font,
                "确认",
                null,
                new Color(0.16f, 0.63f, 0.86f, 1f),
                new Color(0.24f, 0.72f, 0.95f, 1f),
                new Color(0.11f, 0.44f, 0.64f, 1f),
                44f);
            SetButtonWidth(confirmButton, 168f);

            Button cancelButton = CreateButton(
                footerRoot,
                font,
                "取消",
                null,
                new Color(0.26f, 0.29f, 0.36f, 1f),
                new Color(0.34f, 0.38f, 0.46f, 1f),
                new Color(0.18f, 0.21f, 0.28f, 1f),
                44f);
            SetButtonWidth(cancelButton, 140f);

            StudentIdCardNameEntryWindowTemplate template = root.AddComponent<StudentIdCardNameEntryWindowTemplate>();
            template.ConfigureReferences(
                rootRect,
                panel,
                titleText,
                subtitleText,
                bodyRoot,
                footerRoot,
                fieldLabel,
                inputField,
                inputText,
                placeholderText,
                confirmButton,
                cancelButton);

            PrefabUtility.SaveAsPrefabAsset(root, NameEntryWindowPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NameEntryWindowPrefabPath);
        return prefab != null ? prefab.GetComponent<StudentIdCardNameEntryWindowTemplate>() : null;
    }

    private static Material BuildBodyMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Complex Lit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        Material material = LoadOrCreateMaterial(BodyMaterialPath, shader);
        material.name = "StudentIdPlasticBody";
        TrySetColor(material, "_BaseColor", new Color(0.97f, 0.98f, 0.995f, 1f));
        TrySetFloat(material, "_Metallic", 0f);
        TrySetFloat(material, "_Smoothness", 0.88f);
        TrySetFloat(material, "_SpecularHighlights", 1f);
        TrySetFloat(material, "_EnvironmentReflections", 1f);
        TrySetFloat(material, "_ClearCoatMask", 1f);
        TrySetFloat(material, "_ClearCoatSmoothness", 0.95f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material BuildPrintMaterial()
    {
        Material material = LoadOrCreateMaterial(PrintMaterialPath, Shader.Find("Universal Render Pipeline/Lit"));
        material.name = "StudentIdPrintOverlay";
        TrySetColor(material, "_BaseColor", Color.white);
        TrySetFloat(material, "_Metallic", 0f);
        TrySetFloat(material, "_Smoothness", 0.18f);
        TrySetFloat(material, "_Surface", 1f);
        TrySetFloat(material, "_Blend", 0f);
        TrySetFloat(material, "_AlphaClip", 0f);
        TrySetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
        TrySetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        TrySetFloat(material, "_ZWrite", 0f);
        TrySetFloat(material, "_Cull", (float)CullMode.Back);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material LoadOrCreateMaterial(string assetPath, Shader shader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material != null)
        {
            if (shader != null)
            {
                material.shader = shader;
            }

            return material;
        }

        material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(material, assetPath);
        return material;
    }

    private static MeshRenderer CreatePrintSurface(MeshRenderer bodyRenderer, Material printMaterial)
    {
        MeshFilter bodyFilter = bodyRenderer != null ? bodyRenderer.GetComponent<MeshFilter>() : null;
        Bounds bounds = bodyFilter != null && bodyFilter.sharedMesh != null
            ? bodyFilter.sharedMesh.bounds
            : new Bounds(Vector3.zero, new Vector3(0.0856f, 0.0018f, 0.054f));

        const float borderInset = 0.0044f;
        const float surfaceOffset = 0.0004f;

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "CardPrintSurface";
        quad.transform.SetParent(bodyRenderer.transform, false);
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localPosition = new Vector3(
            bounds.center.x,
            bounds.center.y + bounds.extents.y + surfaceOffset,
            bounds.center.z);
        quad.transform.localScale = new Vector3(
            Mathf.Max(0.01f, bounds.size.x - borderInset * 2f),
            Mathf.Max(0.01f, bounds.size.z - borderInset * 2f),
            1f);

        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        return renderer;
    }

    private static void ConfigurePrintSurface(MeshRenderer renderer, Material printMaterial)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.sharedMaterial = printMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private static void CreateInteractionVolume(Transform parent, MeshRenderer bodyRenderer)
    {
        if (parent == null || bodyRenderer == null)
        {
            return;
        }

        GameObject volume = new GameObject("InteractionVolume");
        volume.transform.SetParent(parent, false);

        Bounds bounds = bodyRenderer.bounds;
        volume.transform.position = bounds.center;
        volume.transform.rotation = bodyRenderer.transform.rotation;

        BoxCollider collider = volume.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(
            Mathf.Max(0.18f, bounds.size.x + 0.08f),
            Mathf.Max(0.08f, bounds.size.y + 0.06f),
            Mathf.Max(0.18f, bounds.size.z + 0.08f));
    }

    private static void CreateInspectCameraAnchor(Transform parent, MeshRenderer bodyRenderer)
    {
        if (parent == null || bodyRenderer == null)
        {
            return;
        }

        Bounds bounds = bodyRenderer.bounds;
        Vector3 center = bounds.center;
        Vector3 right = bodyRenderer.transform.right.normalized;
        Vector3 normal = bodyRenderer.transform.up.normalized;
        Vector3 top = bodyRenderer.transform.forward.normalized;

        Vector3 anchorPosition = center
            + right * Mathf.Max(bounds.extents.x * 0.22f, 0.04f)
            + normal * Mathf.Max(bounds.extents.x * 1.2f, 0.14f)
            - top * Mathf.Max(bounds.extents.z * 0.7f, 0.06f);
        Quaternion anchorRotation = Quaternion.LookRotation((center - anchorPosition).normalized, top);

        GameObject anchor = new GameObject("InspectCameraAnchor");
        anchor.transform.SetParent(parent, false);
        anchor.transform.position = anchorPosition;
        anchor.transform.rotation = anchorRotation;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        Image image = PrototypeUiToolkit.CreateImage(parent, name, color);
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        Font font,
        int fontSize,
        FontStyle fontStyle,
        Color color,
        TextAnchor alignment)
    {
        RectTransform rectTransform = PrototypeUiToolkit.CreateRectTransform(name, parent);
        TextMeshProUGUI text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        ConfigureText(text, font, fontSize, fontStyle, color, alignment, false);
        return text;
    }

    private static void ConfigureText(TMP_Text text, Font font, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment, bool richText)
    {
        TMP_FontAsset fontAsset = ResolveFontAsset(font);
        if (fontAsset != null)
        {
            text.font = fontAsset;
        }

        text.fontSize = fontSize;
        text.fontStyle = PrototypeUiToolkit.ConvertFontStyle(fontStyle);
        text.color = color;
        text.alignment = PrototypeUiToolkit.ConvertTextAlignment(alignment);
        text.richText = richText;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.text = string.Empty;
        text.margin = Vector4.zero;
    }

    private static TMP_FontAsset ResolveFontAsset(Font font)
    {
        TMP_Settings settings = TMP_Settings.instance;
        TMP_FontAsset defaultFontAsset = settings != null ? TMP_Settings.defaultFontAsset : null;
        return defaultFontAsset != null ? defaultFontAsset : PrototypeUiToolkit.ResolveTmpFontAsset(font);
    }

    private static Button CreateButton(
        Transform parent,
        Font font,
        string label,
        Action onClick,
        Color normalColor,
        Color highlightedColor,
        Color pressedColor,
        float preferredHeight)
    {
        RectTransform rectTransform = PrototypeUiToolkit.CreateRectTransform("Button", parent);
        Image background = rectTransform.gameObject.AddComponent<Image>();
        background.color = normalColor;

        Button button = rectTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.selectedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        colors.disabledColor = new Color(normalColor.r * 0.65f, normalColor.g * 0.65f, normalColor.b * 0.65f, 0.7f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.flexibleWidth = 1f;

        RectTransform labelRoot = PrototypeUiToolkit.CreateRectTransform("Label", rectTransform);
        PrototypeUiToolkit.SetStretch(labelRoot, 12f, 12f, 8f, 8f);
        TMP_Text buttonLabel = CreateText("Text", labelRoot, font, 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        buttonLabel.richText = false;
        buttonLabel.text = label ?? string.Empty;

        button.onClick.RemoveAllListeners();
        if (onClick != null)
        {
            button.onClick.AddListener(() => onClick());
        }

        return button;
    }

    private static void SetButtonWidth(Button button, float width)
    {
        if (button == null)
        {
            return;
        }

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = button.gameObject.AddComponent<LayoutElement>();
        }

        layout.minWidth = width;
        layout.preferredWidth = width;

        RectTransform rectTransform = button.transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(width, 44f);
        }
    }

    private static void TrySetFloat(Material material, string propertyName, float value)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void TrySetColor(Material material, string propertyName, Color value)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        int lastSlash = path.LastIndexOf('/');
        if (lastSlash <= 0)
        {
            return;
        }

        string parent = path.Substring(0, lastSlash);
        string folderName = path.Substring(lastSlash + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
