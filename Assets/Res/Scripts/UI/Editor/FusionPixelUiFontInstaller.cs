using System;
using UnityEditor;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class FusionPixelUiFontInstaller
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string FontsFolder = "Assets/Resources/Fonts";
    private const string FusionPixelFolder = "Assets/Resources/Fonts/FusionPixel";
    private const string TmpFontFolder = "Assets/Resources/Fonts & Materials";
    private const string WorldTextMaterialFolder = "Assets/Res/Materials/BaseHub";
    private const string WorldTextMaterialPath = "Assets/Res/Materials/BaseHub/Mat_WorldText.mat";
    private const string WorldTextShaderPath = "Assets/Shaders/WorldTextOccluded.shader";
    private const string WorldTextShaderName = "ProjectXX/WorldTextOccluded";
    private const string SourceFontPath = "Assets/Resources/Fonts/FusionPixel/fusion-pixel-12px-proportional-zh_hans.ttf";
    private const string TmpFontAssetPath = "Assets/Resources/Fonts & Materials/FusionPixel12pxProportionalZhHans SDF.asset";
    private const string UiRootFolder = "Assets/Resources/UI";
    private const string ScenesFolder = "Assets/Scenes";

    [MenuItem("Tools/Prototype/Install Fusion Pixel UI Font")]
    public static void InstallFromMenu()
    {
        string result = TryInstallAndApply();
        if (!string.IsNullOrWhiteSpace(result))
        {
            Debug.Log(result);
        }
    }

    public static string TryInstallAndApply()
    {
        try
        {
            return InstallAndApply();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return exception.ToString();
        }
    }

    public static string InstallAndApply()
    {
        bool hasUnsavedOpenScenes = HasUnsavedOpenScenes();

        EnsureFolder(ResourcesFolder);
        EnsureFolder(FontsFolder);
        EnsureFolder(FusionPixelFolder);
        EnsureFolder(TmpFontFolder);
        EnsureFolder(WorldTextMaterialFolder);

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            return $"Fusion Pixel source font was not found at {SourceFontPath}.";
        }

        TMP_FontAsset tmpFontAsset = CreateOrUpdateTmpFontAsset(sourceFont);
        if (tmpFontAsset == null)
        {
            return "Failed to create the Fusion Pixel TMP font asset.";
        }

        ConfigureTmpSettings(tmpFontAsset);

        int updatedPrefabCount = UpdatePrefabs(sourceFont, tmpFontAsset);
        int updatedSceneCount = hasUnsavedOpenScenes ? 0 : UpdateScenes(sourceFont, tmpFontAsset);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return hasUnsavedOpenScenes
            ? $"Installed Fusion Pixel UI font. Updated {updatedPrefabCount} prefab(s). Scene updates were skipped because there are unsaved open scenes."
            : $"Installed Fusion Pixel UI font. Updated {updatedPrefabCount} prefab(s) and {updatedSceneCount} scene(s).";
    }

    private static TMP_FontAsset CreateOrUpdateTmpFontAsset(Font sourceFont)
    {
        if (sourceFont == null)
        {
            return null;
        }

        TMP_FontAsset existingAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontAssetPath);
        if (IsUsableTmpFontAsset(existingAsset) && existingAsset.sourceFontFile == sourceFont)
        {
            return existingAsset;
        }

        if (existingAsset != null)
        {
            AssetDatabase.DeleteAsset(TmpFontAssetPath);
        }

        TMP_FontAsset tmpFontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            96,
            4,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true);
        if (tmpFontAsset == null)
        {
            return null;
        }

        tmpFontAsset.name = "FusionPixel12pxProportionalZhHans SDF";
        Texture2D atlasTexture = tmpFontAsset.atlasTextures != null && tmpFontAsset.atlasTextures.Length > 0
            ? tmpFontAsset.atlasTextures[0]
            : null;
        if (atlasTexture != null)
        {
            atlasTexture.name = $"{tmpFontAsset.name} Atlas";
        }

        Material fontMaterial = tmpFontAsset.material;
        if (fontMaterial != null)
        {
            fontMaterial.name = $"{tmpFontAsset.name} Material";
        }

        AssetDatabase.CreateAsset(tmpFontAsset, TmpFontAssetPath);
        if (atlasTexture != null)
        {
            AssetDatabase.AddObjectToAsset(atlasTexture, tmpFontAsset);
        }

        if (fontMaterial != null)
        {
            AssetDatabase.AddObjectToAsset(fontMaterial, tmpFontAsset);
        }

        AssetDatabase.ImportAsset(TmpFontAssetPath, ImportAssetOptions.ForceUpdate);

        TMP_FontAsset loadedAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontAssetPath);
        if (loadedAsset != null)
        {
            EditorUtility.SetDirty(loadedAsset);
        }

        return loadedAsset;
    }

    private static bool IsUsableTmpFontAsset(TMP_FontAsset fontAsset)
    {
        return fontAsset != null
            && fontAsset.sourceFontFile != null
            && fontAsset.material != null
            && fontAsset.atlasTextures != null
            && fontAsset.atlasTextures.Length > 0
            && fontAsset.atlasTextures[0] != null;
    }

    private static void ConfigureTmpSettings(TMP_FontAsset tmpFontAsset)
    {
        if (tmpFontAsset == null)
        {
            return;
        }

        TMP_Settings settings = TMP_Settings.instance;
        if (settings == null)
        {
            return;
        }

        if (TMP_Settings.defaultFontAsset != tmpFontAsset)
        {
            TMP_Settings.defaultFontAsset = tmpFontAsset;
        }

        SerializedObject serializedSettings = new SerializedObject(settings);
        SerializedProperty defaultFontPathProperty = serializedSettings.FindProperty("m_defaultFontAssetPath");
        if (defaultFontPathProperty != null && defaultFontPathProperty.stringValue != "Fonts & Materials/")
        {
            defaultFontPathProperty.stringValue = "Fonts & Materials/";
        }

        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
    }

    private static int UpdatePrefabs(Font sourceFont, TMP_FontAsset tmpFontAsset)
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { UiRootFolder });
        int updatedCount = 0;
        for (int index = 0; index < prefabGuids.Length; index++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[index]);
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                continue;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (!ApplyFontToHierarchy(prefabRoot, sourceFont, tmpFontAsset))
                {
                    continue;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                updatedCount++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        return updatedCount;
    }

    private static int UpdateScenes(Font sourceFont, TMP_FontAsset tmpFontAsset)
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { ScenesFolder });
        if (sceneGuids.Length == 0)
        {
            return 0;
        }

        SceneSetup[] sceneSetup = EditorSceneManager.GetSceneManagerSetup();
        int updatedCount = 0;
        try
        {
            for (int index = 0; index < sceneGuids.Length; index++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[index]);
                if (string.IsNullOrWhiteSpace(scenePath))
                {
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                bool changed = false;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    changed |= ApplyFontToHierarchy(roots[rootIndex], sourceFont, tmpFontAsset);
                }

                if (!changed)
                {
                    continue;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                updatedCount++;
            }
        }
        finally
        {
            if (sceneSetup != null && sceneSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
            }
        }

        return updatedCount;
    }

    private static bool ApplyFontToHierarchy(GameObject root, Font sourceFont, TMP_FontAsset tmpFontAsset)
    {
        if (root == null || sourceFont == null || tmpFontAsset == null)
        {
            return false;
        }

        bool changed = false;
        Material worldTextMaterial = ResolveWorldTextMaterial(sourceFont) ?? sourceFont.material;

        Text[] legacyTextComponents = root.GetComponentsInChildren<Text>(true);
        for (int index = 0; index < legacyTextComponents.Length; index++)
        {
            Text label = legacyTextComponents[index];
            if (label == null || label.font == sourceFont)
            {
                continue;
            }

            label.font = sourceFont;
            EditorUtility.SetDirty(label);
            changed = true;
        }

        TMP_Text[] tmpTextComponents = root.GetComponentsInChildren<TMP_Text>(true);
        for (int index = 0; index < tmpTextComponents.Length; index++)
        {
            TMP_Text label = tmpTextComponents[index];
            if (label == null)
            {
                continue;
            }

            bool labelChanged = false;
            if (label.font != tmpFontAsset)
            {
                label.font = tmpFontAsset;
                labelChanged = true;
            }

            if (label.fontSharedMaterial != tmpFontAsset.material)
            {
                label.fontSharedMaterial = tmpFontAsset.material;
                labelChanged = true;
            }

            if (!labelChanged)
            {
                continue;
            }

            label.havePropertiesChanged = true;
            label.SetAllDirty();
            EditorUtility.SetDirty(label);
            changed = true;
        }

        TextMesh[] textMeshComponents = root.GetComponentsInChildren<TextMesh>(true);
        for (int index = 0; index < textMeshComponents.Length; index++)
        {
            TextMesh label = textMeshComponents[index];
            if (label == null)
            {
                continue;
            }

            bool labelChanged = false;
            if (label.font != sourceFont)
            {
                label.font = sourceFont;
                labelChanged = true;
            }

            MeshRenderer meshRenderer = label.GetComponent<MeshRenderer>();
            if (meshRenderer != null && worldTextMaterial != null && meshRenderer.sharedMaterial != worldTextMaterial)
            {
                meshRenderer.sharedMaterial = worldTextMaterial;
                EditorUtility.SetDirty(meshRenderer);
                labelChanged = true;
            }

            if (!labelChanged)
            {
                continue;
            }

            EditorUtility.SetDirty(label);
            changed = true;
        }

        PrototypeRuntimeUiManager[] runtimeUiManagers = root.GetComponentsInChildren<PrototypeRuntimeUiManager>(true);
        for (int index = 0; index < runtimeUiManagers.Length; index++)
        {
            PrototypeRuntimeUiManager manager = runtimeUiManagers[index];
            if (manager == null)
            {
                continue;
            }

            SerializedObject serializedObject = new SerializedObject(manager);
            SerializedProperty runtimeFontProperty = serializedObject.FindProperty("runtimeFont");
            if (runtimeFontProperty == null || runtimeFontProperty.objectReferenceValue == sourceFont)
            {
                continue;
            }

            runtimeFontProperty.objectReferenceValue = sourceFont;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
            changed = true;
        }

        return changed;
    }

    private static bool HasUnsavedOpenScenes()
    {
        int sceneCount = SceneManager.sceneCount;
        for (int index = 0; index < sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            if (scene.IsValid() && scene.isLoaded && scene.isDirty)
            {
                return true;
            }
        }

        return false;
    }

    private static Material ResolveWorldTextMaterial(Font font)
    {
        if (font == null)
        {
            return null;
        }

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(WorldTextShaderPath) ?? Shader.Find(WorldTextShaderName);
        Material fontMaterial = font.material;
        if (shader == null)
        {
            return fontMaterial;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(WorldTextMaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, WorldTextMaterialPath);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        Texture fontAtlas = fontMaterial != null ? fontMaterial.mainTexture : null;
        if (fontAtlas != null && material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", fontAtlas);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parentPath = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(parentPath) && !AssetDatabase.IsValidFolder(parentPath))
        {
            EnsureFolder(parentPath);
        }

        AssetDatabase.CreateFolder(parentPath, folderName);
    }
}
