using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectXX.Rendering.Editor
{
    public static class UrpPsxInstaller
    {
        private enum PsxPreset
        {
            Light,
            Medium,
            Heavy
        }

        private readonly struct PsxPresetSettings
        {
            public PsxPresetSettings(
                float pixelWidth,
                float pixelHeight,
                float colorPrecision,
                bool enableFog,
                float fogDensity,
                float fogDistance,
                Color fogColor,
                float fogNear,
                float fogFar,
                float fogAltScale,
                float fogThinning,
                float fogNoiseScale,
                float fogNoiseStrength,
                bool enableDithering,
                int ditherPatternIndex,
                float ditherThreshold,
                float ditherStrength,
                float ditherScale,
                float ambientOcclusionIntensity,
                float ambientOcclusionDirectLightingStrength,
                float bloomIntensity,
                float bloomScatter,
                float vignetteIntensity)
            {
                PixelWidth = pixelWidth;
                PixelHeight = pixelHeight;
                ColorPrecision = colorPrecision;
                EnableFog = enableFog;
                FogDensity = fogDensity;
                FogDistance = fogDistance;
                FogColor = fogColor;
                FogNear = fogNear;
                FogFar = fogFar;
                FogAltScale = fogAltScale;
                FogThinning = fogThinning;
                FogNoiseScale = fogNoiseScale;
                FogNoiseStrength = fogNoiseStrength;
                EnableDithering = enableDithering;
                DitherPatternIndex = ditherPatternIndex;
                DitherThreshold = ditherThreshold;
                DitherStrength = ditherStrength;
                DitherScale = ditherScale;
                AmbientOcclusionIntensity = ambientOcclusionIntensity;
                AmbientOcclusionDirectLightingStrength = ambientOcclusionDirectLightingStrength;
                BloomIntensity = bloomIntensity;
                BloomScatter = bloomScatter;
                VignetteIntensity = vignetteIntensity;
            }

            public float PixelWidth { get; }
            public float PixelHeight { get; }
            public float ColorPrecision { get; }
            public bool EnableFog { get; }
            public float FogDensity { get; }
            public float FogDistance { get; }
            public Color FogColor { get; }
            public float FogNear { get; }
            public float FogFar { get; }
            public float FogAltScale { get; }
            public float FogThinning { get; }
            public float FogNoiseScale { get; }
            public float FogNoiseStrength { get; }
            public bool EnableDithering { get; }
            public int DitherPatternIndex { get; }
            public float DitherThreshold { get; }
            public float DitherStrength { get; }
            public float DitherScale { get; }
            public float AmbientOcclusionIntensity { get; }
            public float AmbientOcclusionDirectLightingStrength { get; }
            public float BloomIntensity { get; }
            public float BloomScatter { get; }
            public float VignetteIntensity { get; }
        }

        private const string RequestPath = "Temp/UrpPsxInstaller.request";
        private const string ResultPath = "Temp/UrpPsxInstaller.result";

        private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
        private const string VolumeProfilePath = "Assets/Settings/SampleSceneProfile.asset";

        private const string PsxFolderPath = "Assets/Settings/PSX";
        private const string FogMaterialPath = PsxFolderPath + "/PSX_Fog.mat";
        private const string PixelationMaterialPath = PsxFolderPath + "/PSX_Pixelation.mat";
        private const string DitheringMaterialPath = PsxFolderPath + "/PSX_Dithering.mat";

        private const string FogShaderPath = "Assets/Shaders/Fog.shader";
        private const string PixelationShaderPath = "Assets/Shaders/Pixelation.shader";
        private const string DitheringShaderPath = "Assets/Shaders/Dithering.shader";

        [InitializeOnLoadMethod]
        private static void RegisterAutomation()
        {
            // Polling on update lets us apply presets without waiting for a domain reload.
            EditorApplication.update -= RunAutomatedInstallIfRequested;
            EditorApplication.update += RunAutomatedInstallIfRequested;
            EditorApplication.delayCall += RunAutomatedInstallIfRequested;
        }

        [MenuItem("Tools/Rendering/URP-PSX/Install And Apply")]
        public static void InstallAndApplyFromMenu()
        {
            Debug.Log(InstallAndApply(PsxPreset.Medium));
        }

        [MenuItem("Tools/Rendering/URP-PSX/Apply Preset/Light")]
        public static void ApplyLightPresetFromMenu()
        {
            Debug.Log(InstallAndApply(PsxPreset.Light));
        }

        [MenuItem("Tools/Rendering/URP-PSX/Apply Preset/Medium")]
        public static void ApplyMediumPresetFromMenu()
        {
            Debug.Log(InstallAndApply(PsxPreset.Medium));
        }

        [MenuItem("Tools/Rendering/URP-PSX/Apply Preset/Heavy")]
        public static void ApplyHeavyPresetFromMenu()
        {
            Debug.Log(InstallAndApply(PsxPreset.Heavy));
        }

        private static void RunAutomatedInstallIfRequested()
        {
            if (!File.Exists(RequestPath))
            {
                return;
            }

            try
            {
                var requestedPreset = ReadRequestedPreset();
                File.Delete(RequestPath);
                var summary = InstallAndApply(requestedPreset);
                Directory.CreateDirectory(Path.GetDirectoryName(ResultPath) ?? "Temp");
                File.WriteAllText(ResultPath, $"ok{Environment.NewLine}{summary}");
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ResultPath) ?? "Temp");
                File.WriteAllText(ResultPath, $"error{Environment.NewLine}{exception}");
                throw;
            }
        }

        private static PsxPreset ReadRequestedPreset()
        {
            var requestText = File.ReadAllText(RequestPath).Trim();
            return Enum.TryParse(requestText, true, out PsxPreset preset)
                ? preset
                : PsxPreset.Medium;
        }

        private static string InstallAndApply(PsxPreset preset)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureFolder(PsxFolderPath);
            var settings = GetPresetSettings(preset);

            var fogMaterial = CreateOrUpdateMaterial(
                FogMaterialPath,
                FogShaderPath,
                material =>
                {
                    material.SetFloat("_FogDensity", settings.FogDensity);
                    material.SetFloat("_FogDistance", settings.FogDistance);
                    material.SetColor("_FogColor", settings.FogColor);
                    material.SetFloat("_FogNear", settings.FogNear);
                    material.SetFloat("_FogFar", settings.FogFar);
                    material.SetFloat("_FogAltScale", settings.FogAltScale);
                    material.SetFloat("_FogThinning", settings.FogThinning);
                    material.SetFloat("_NoiseScale", settings.FogNoiseScale);
                    material.SetFloat("_NoiseStrength", settings.FogNoiseStrength);
                });

            var pixelationMaterial = CreateOrUpdateMaterial(
                PixelationMaterialPath,
                PixelationShaderPath,
                material =>
                {
                    material.SetFloat("_WidthPixelation", settings.PixelWidth);
                    material.SetFloat("_HeightPixelation", settings.PixelHeight);
                    material.SetFloat("_ColorPrecision", settings.ColorPrecision);
                });

            var ditheringMaterial = CreateOrUpdateMaterial(
                DitheringMaterialPath,
                DitheringShaderPath,
                material =>
                {
                    material.SetInt("_PatternIndex", settings.DitherPatternIndex);
                    material.SetFloat("_DitherThreshold", settings.DitherThreshold);
                    material.SetFloat("_DitherStrength", settings.DitherStrength);
                    material.SetFloat("_DitherScale", settings.DitherScale);
                });

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(PcRendererPath);
            if (rendererData == null)
            {
                throw new InvalidOperationException($"Unable to load renderer at {PcRendererPath}.");
            }

            ConfigureRendererFeature(
                rendererData,
                "PSX/Fog",
                new[] { "PSXFog" },
                fogMaterial,
                FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing,
                ScriptableRenderPassInput.Depth,
                settings.EnableFog);

            ConfigureRendererFeature(
                rendererData,
                "PSX/Pixelation",
                new[] { "PSXPixelation" },
                pixelationMaterial,
                FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing,
                ScriptableRenderPassInput.None,
                true);

            ConfigureRendererFeature(
                rendererData,
                "PSX/Dithering",
                new[] { "PSXDithering" },
                ditheringMaterial,
                FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing,
                ScriptableRenderPassInput.None,
                settings.EnableDithering);

            ApplyAmbientOcclusionSettings(rendererData, settings);
            ApplyVolumeProfileSettings(settings);

            EditorUtility.SetDirty(rendererData);
            rendererData.SetDirty();

            var updatedCameras = EnablePostProcessingOnLoadedSceneCameras();

            AssetDatabase.SaveAssets();
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorSceneManager.MarkAllScenesDirty();
                EditorSceneManager.SaveOpenScenes();
            }

            return $"URP-PSX {preset} preset installed on PC renderer and applied to {updatedCameras} loaded scene camera(s) at {DateTimeOffset.Now:O}.";
        }

        private static PsxPresetSettings GetPresetSettings(PsxPreset preset)
        {
            return preset switch
            {
                PsxPreset.Light => new PsxPresetSettings(
                    pixelWidth: 1024f,
                    pixelHeight: 576f,
                    colorPrecision: 64f,
                    enableFog: false,
                    fogDensity: 4.0f,
                    fogDistance: 18.0f,
                    fogColor: new Color(0.22f, 0.24f, 0.28f, 1f),
                    fogNear: 0f,
                    fogFar: 90f,
                    fogAltScale: 10f,
                    fogThinning: 260f,
                    fogNoiseScale: 260f,
                    fogNoiseStrength: 0.01f,
                    enableDithering: false,
                    ditherPatternIndex: 2,
                    ditherThreshold: 8f,
                    ditherStrength: 0.08f,
                    ditherScale: 2f,
                    ambientOcclusionIntensity: 0.1f,
                    ambientOcclusionDirectLightingStrength: 0.05f,
                    bloomIntensity: 0.22f,
                    bloomScatter: 0.08f,
                    vignetteIntensity: 0.08f),
                PsxPreset.Medium => new PsxPresetSettings(
                    pixelWidth: 640f,
                    pixelHeight: 360f,
                    colorPrecision: 32f,
                    enableFog: true,
                    fogDensity: 4.75f,
                    fogDistance: 20f,
                    fogColor: new Color(0.20f, 0.22f, 0.26f, 1f),
                    fogNear: 0f,
                    fogFar: 105f,
                    fogAltScale: 10f,
                    fogThinning: 220f,
                    fogNoiseScale: 220f,
                    fogNoiseStrength: 0.012f,
                    enableDithering: true,
                    ditherPatternIndex: 2,
                    ditherThreshold: 6f,
                    ditherStrength: 0.18f,
                    ditherScale: 1.5f,
                    ambientOcclusionIntensity: 0.13f,
                    ambientOcclusionDirectLightingStrength: 0.07f,
                    bloomIntensity: 0.18f,
                    bloomScatter: 0.055f,
                    vignetteIntensity: 0.14f),
                PsxPreset.Heavy => new PsxPresetSettings(
                    pixelWidth: 480f,
                    pixelHeight: 270f,
                    colorPrecision: 20f,
                    enableFog: true,
                    fogDensity: 6f,
                    fogDistance: 14f,
                    fogColor: new Color(0.17f, 0.18f, 0.22f, 1f),
                    fogNear: 0f,
                    fogFar: 72f,
                    fogAltScale: 10f,
                    fogThinning: 160f,
                    fogNoiseScale: 160f,
                    fogNoiseStrength: 0.02f,
                    enableDithering: true,
                    ditherPatternIndex: 1,
                    ditherThreshold: 4f,
                    ditherStrength: 0.28f,
                    ditherScale: 1f,
                    ambientOcclusionIntensity: 0.16f,
                    ambientOcclusionDirectLightingStrength: 0.08f,
                    bloomIntensity: 0.12f,
                    bloomScatter: 0.04f,
                    vignetteIntensity: 0.2f),
                _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null)
            };
        }

        private static void ApplyAmbientOcclusionSettings(UniversalRendererData rendererData, PsxPresetSettings settings)
        {
            var ambientOcclusion = rendererData.rendererFeatures
                .FirstOrDefault(feature => feature != null && feature.name == "ScreenSpaceAmbientOcclusion");

            if (ambientOcclusion == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(ambientOcclusion);
            SetFloatProperty(serializedObject, "m_Settings.Intensity", settings.AmbientOcclusionIntensity);
            SetFloatProperty(serializedObject, "m_Settings.DirectLightingStrength", settings.AmbientOcclusionDirectLightingStrength);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            ambientOcclusion.SetActive(true);
            EditorUtility.SetDirty(ambientOcclusion);
        }

        private static void ApplyVolumeProfileSettings(PsxPresetSettings settings)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                throw new InvalidOperationException($"Unable to load volume profile at {VolumeProfilePath}.");
            }

            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }

            if (!profile.TryGet(out Vignette vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }

            bloom.active = true;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = settings.BloomIntensity;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = settings.BloomScatter;

            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = settings.VignetteIntensity;

            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(vignette);
            EditorUtility.SetDirty(profile);
        }

        private static void SetFloatProperty(SerializedObject serializedObject, string propertyPath, float value)
        {
            var property = serializedObject.FindProperty(propertyPath);
            if (property == null)
            {
                throw new InvalidOperationException($"Unable to find serialized property '{propertyPath}'.");
            }

            property.floatValue = value;
        }

        private static void ConfigureRendererFeature(
            UniversalRendererData rendererData,
            string featureName,
            string[] aliases,
            Material material,
            FullScreenPassRendererFeature.InjectionPoint injectionPoint,
            ScriptableRenderPassInput requirements,
            bool active)
        {
            var acceptedNames = aliases
                .Append(featureName)
                .ToHashSet(StringComparer.Ordinal);

            var matchingFeatures = rendererData.rendererFeatures
                .OfType<FullScreenPassRendererFeature>()
                .Where(existing => existing != null && acceptedNames.Contains(existing.name))
                .ToList();

            var feature = matchingFeatures.FirstOrDefault(existing => existing.name == featureName)
                ?? matchingFeatures.FirstOrDefault();

            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feature.name = featureName;
                AssetDatabase.AddObjectToAsset(feature, rendererData);
                AssetDatabase.ImportAsset(PcRendererPath, ImportAssetOptions.ForceSynchronousImport);
                AppendRendererFeature(rendererData, feature);
            }
            else
            {
                feature.name = featureName;
            }

            RemoveDuplicateRendererFeatures(
                rendererData,
                matchingFeatures.Where(existing => existing != feature).Cast<ScriptableRendererFeature>().ToList());

            feature.name = featureName;
            feature.injectionPoint = injectionPoint;
            feature.fetchColorBuffer = true;
            feature.requirements = requirements;
            feature.passMaterial = material;
            feature.passIndex = 0;
            feature.bindDepthStencilAttachment = false;
            feature.SetActive(active);
            EditorUtility.SetDirty(feature);
        }

        private static void RemoveDuplicateRendererFeatures(
            UniversalRendererData rendererData,
            System.Collections.Generic.IReadOnlyCollection<ScriptableRendererFeature> duplicates)
        {
            if (duplicates == null || duplicates.Count == 0)
            {
                return;
            }

            var duplicateSet = duplicates.Where(feature => feature != null).ToHashSet();
            if (duplicateSet.Count == 0)
            {
                return;
            }

            var serializedObject = new SerializedObject(rendererData);
            var featuresProperty = serializedObject.FindProperty("m_RendererFeatures");
            var featureMapProperty = serializedObject.FindProperty("m_RendererFeatureMap");

            for (var index = featuresProperty.arraySize - 1; index >= 0; index--)
            {
                var current = featuresProperty.GetArrayElementAtIndex(index).objectReferenceValue as ScriptableRendererFeature;
                if (!duplicateSet.Contains(current))
                {
                    continue;
                }

                featuresProperty.DeleteArrayElementAtIndex(index);
                if (index < featureMapProperty.arraySize)
                {
                    featureMapProperty.DeleteArrayElementAtIndex(index);
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            foreach (var duplicate in duplicateSet)
            {
                UnityEngine.Object.DestroyImmediate(duplicate, true);
            }

            AssetDatabase.ImportAsset(PcRendererPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void AppendRendererFeature(UniversalRendererData rendererData, ScriptableRendererFeature feature)
        {
            var serializedObject = new SerializedObject(rendererData);
            var featuresProperty = serializedObject.FindProperty("m_RendererFeatures");
            var featureMapProperty = serializedObject.FindProperty("m_RendererFeatureMap");

            var index = featuresProperty.arraySize;
            featuresProperty.InsertArrayElementAtIndex(index);
            featuresProperty.GetArrayElementAtIndex(index).objectReferenceValue = feature;

            featureMapProperty.InsertArrayElementAtIndex(index);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId))
            {
                throw new InvalidOperationException($"Unable to resolve local file identifier for renderer feature {feature.name}.");
            }

            featureMapProperty.GetArrayElementAtIndex(index).longValue = localId;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material CreateOrUpdateMaterial(string materialPath, string shaderPath, Action<Material> configure)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null)
            {
                throw new InvalidOperationException($"Missing shader at {shaderPath}. Import may still be in progress.");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(materialPath) };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            configure(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static int EnablePostProcessingOnLoadedSceneCameras()
        {
            var updatedCount = 0;
            var cameras = Resources.FindObjectsOfTypeAll<Camera>()
                .Where(camera => camera != null && camera.gameObject.scene.IsValid() && camera.gameObject.scene.isLoaded);

            foreach (var camera in cameras)
            {
                var additionalData = camera.GetUniversalAdditionalCameraData();
                if (additionalData.renderPostProcessing)
                {
                    continue;
                }

                additionalData.renderPostProcessing = true;
                EditorUtility.SetDirty(additionalData);
                updatedCount++;
            }

            return updatedCount;
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            var parts = assetFolderPath.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
